using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OperationOrderPuzzle : MonoBehaviour
{
    private const string CorrectAnswer = "4";
    private const string PuzzleRootFallbackName = "Puzzle4_OperationOrder";
    private const string AllInstrumentsFallbackName = "All_Instruments";
    private const string MethodSheetFallbackName = "OperationMethodSheet";

    [Header("Puzzle 4")]
    [SerializeField, Min(1f)] private float puzzleTimeLimit = 600f;
    [SerializeField] private bool hideMethodSheetUntilPuzzleActive = true;

    [Header("Scene References")]
    [SerializeField] private GameObject puzzleRoot;
    [SerializeField] private Transform allInstruments;
    [SerializeField] private Transform operationTable;
    [SerializeField] private OperationMethodSheetReadable methodSheet;
    [SerializeField] private OperationInstrumentSequenceManager sequenceManager;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private LightFlickerController lightFlicker;
    [Tooltip("Optional. If a CodeFragmentManager exists, Puzzle 4 will also try to find it automatically.")]
    [SerializeField] private UnityEngine.Object codeFragmentManager;

    [Header("Operation Method Image")]
    [SerializeField] private Texture operationOrderTexture;
    [SerializeField] private Sprite operationOrderSprite;

    [Header("Operation Method Placement")]
    [SerializeField] private bool useFixedMethodSheetPlacement = true;
    [SerializeField] private Vector3 methodSheetLocalPosition = new Vector3(8.53199959f, -1.26400006f, 10.3809996f);
    [SerializeField] private Vector3 methodSheetLocalRotation = new Vector3(89.0220108f, 269.99939f, 216.583206f);

    [Header("Instruments")]
    [SerializeField] private SurgicalInstrumentInteractable clampInstrument;
    [SerializeField] private SurgicalInstrumentInteractable scalpelInstrument;
    [SerializeField] private SurgicalInstrumentInteractable scissorsInstrument;
    [SerializeField] private SurgicalInstrumentInteractable retractorHookInstrument;
    [SerializeField] private SurgicalInstrumentInteractable needleHolderInstrument;
    [SerializeField] private SurgicalInstrumentInteractable stethoscopeInstrument;

    [Header("Audio")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip methodSheetReadSound;
    [SerializeField] private AudioClip clampVoiceOrSound;
    [SerializeField] private AudioClip scalpelVoiceOrSound;
    [SerializeField] private AudioClip scissorsVoiceOrSound;
    [SerializeField] private AudioClip retractorHookVoiceOrSound;
    [SerializeField] private AudioClip needleHolderVoiceOrSound;
    [SerializeField] private AudioClip stethoscopeVoiceOrSound;
    [SerializeField] private AudioClip metalSilentSound;
    [SerializeField] private AudioClip correctSequenceSound;
    [SerializeField] private AudioClip wrongSequenceSound;
    [SerializeField] private AudioClip numberRevealSound;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip wrongAnswerSound;
    [SerializeField] private AudioClip operationRoomWhisperSound;
    [SerializeField] private AudioClip lightFlickerSound;
    [SerializeField] private AudioClip monsterTriggerSound;

    [Header("Events")]
    public UnityEvent OnPuzzle4Activated = new UnityEvent();
    public UnityEvent OnMethodSheetRead = new UnityEvent();
    public UnityEvent OnInstrumentInspected = new UnityEvent();
    public UnityEvent OnCorrectInstrumentStep = new UnityEvent();
    public UnityEvent OnWrongInstrumentStep = new UnityEvent();
    public UnityEvent OnCorrectSequenceCompleted = new UnityEvent();
    public UnityEvent OnInstrumentNumberRevealed = new UnityEvent();
    public UnityEvent OnCorrectPhoneAnswer = new UnityEvent();
    public UnityEvent OnWrongPhoneAnswer = new UnityEvent();
    public UnityEvent OnPuzzle4Completed = new UnityEvent();
    public UnityEvent OnPuzzle4TimerExpired = new UnityEvent();
    public UnityEvent OnMonsterTriggered = new UnityEvent();

    public event Action Puzzle4Completed;
    public event Action Puzzle4TimerExpired;
    public event Action CorrectSequenceCompleted;
    public event Action MethodSheetRead;

    private PhonePuzzleUI ui;
    private bool isActive;
    private bool isCompleted;
    private bool timerExpired;
    private bool methodSheetWasRead;
    private bool correctSequenceWasCompleted;
    private bool monsterTriggered;
    private int wrongPhoneAttemptCount;
    private float timeRemaining;
    private Coroutine sequenceCompletedRoutine;
    private readonly HashSet<Transform> resolvedInstrumentTargets = new HashSet<Transform>();

    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool HasReadMethodSheet => methodSheetWasRead;
    public bool HasCompletedSequence => correctSequenceWasCompleted;
    public bool CanAnswerAtPhone => isActive && correctSequenceWasCompleted && !isCompleted && !timerExpired;
    public int MaxAnswerLength => 1;
    public string AnswerPrompt => "Which mouth heard what Marek denied?";

    private void Awake()
    {
        TryAssignEditorDefaultImage();
        ResolveReferences();
        ConfigureChildren();
        SetPuzzleInteractable(false);

        if (hideMethodSheetUntilPuzzleActive)
        {
            methodSheet?.SetSheetVisible(false);
        }
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultImage();
    }

    private void Update()
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.SetTime(timeRemaining);

        if (timeRemaining <= 0f)
        {
            ExpireTimer();
        }
    }

    public void ConfigureSceneReferences(
        Transform instrumentsParent,
        MonsterActivationController monster,
        LightFlickerController flicker)
    {
        if (instrumentsParent != null)
        {
            allInstruments = instrumentsParent;
        }

        if (monster != null)
        {
            monsterActivation = monster;
        }

        if (flicker != null)
        {
            lightFlicker = flicker;
        }

        ResolveReferences();
        ConfigureChildren();
    }

    public void ActivatePuzzle4()
    {
        ResolveReferences();
        ConfigureChildren();

        if (puzzleRoot != null && !puzzleRoot.activeSelf)
        {
            puzzleRoot.SetActive(true);
            Debug.Log("Puzzle4_OperationOrder activated.");
        }

        if (isCompleted)
        {
            Debug.LogWarning("Puzzle 4 activation requested, but Puzzle 4 is already completed.");
            return;
        }

        ui = PhonePuzzleUI.EnsureExists();
        isActive = true;
        timerExpired = false;
        methodSheetWasRead = false;
        correctSequenceWasCompleted = false;
        monsterTriggered = false;
        wrongPhoneAttemptCount = 0;
        timeRemaining = puzzleTimeLimit;

        sequenceManager?.ResetSequence();
        sequenceManager?.ResetWrongAttempts();
        sequenceManager?.SetSequenceEnabled(false);
        SetPuzzleInteractable(true);

        if (methodSheet != null)
        {
            methodSheet.SetSheetVisible(true);
            methodSheet.SetReadable(true);
        }
        else
        {
            Debug.LogWarning("Puzzle 4 activated without OperationMethodSheet. The instrument sequence cannot be unlocked until a sheet is assigned.");
        }

        ui.Objectives.SetObjective("Find what made the metal speak.");
        ui.Timer.SetTime(timeRemaining);
        PlayOneShot(operationRoomWhisperSound, "operationRoomWhisperSound");

        OnPuzzle4Activated.Invoke();
        Debug.Log("Puzzle 4 activated: The Six Mouths.");
    }

    public void RegisterMethodSheetRead(OperationMethodSheetReadable sheet)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        if (methodSheetWasRead)
        {
            return;
        }

        methodSheetWasRead = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Wake the six mouths correctly.");
        sequenceManager?.SetSequenceEnabled(true);

        PlayOneShot(methodSheetReadSound, "methodSheetReadSound");
        OnMethodSheetRead.Invoke();
        MethodSheetRead?.Invoke();
        Debug.Log("Puzzle 4 method sheet read. Instrument sequence is now enabled.");
    }

    public void RegisterInstrumentInspected(SurgicalInstrumentInteractable instrument)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        OnInstrumentInspected.Invoke();
        Debug.Log("Puzzle 4 instrument inspected: " + (instrument != null ? instrument.DisplayName : "unknown") + ".");
    }

    public void HandleInstrumentTouchedBeforeMethodSheet(SurgicalInstrumentInteractable instrument)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed(UnityEngine.Random.value > 0.5f ? "The metal stays silent." : "It refuses to remember.", 2.5f);
        PlayOneShot(metalSilentSound, "metalSilentSound");
        Debug.Log("Puzzle 4 instrument touched before method sheet was read: " + (instrument != null ? instrument.DisplayName : "unknown") + ".");
    }

    public void HandleCorrectInstrumentStep(SurgicalInstrumentInteractable instrument, int completedStep)
    {
        if (!isActive || isCompleted || timerExpired || instrument == null)
        {
            return;
        }

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed(instrument.InspectionSubtitle, ResolveSubtitleDuration(instrument.VoiceOrSound, 2.8f));
        PlayOneShot(instrument.VoiceOrSound, instrument.DisplayName + " voice/sound");
        RevealInstrumentNumber(instrument, 1.8f);

        OnCorrectInstrumentStep.Invoke();
        Debug.Log("Puzzle 4 correct instrument step " + completedStep + ": " + instrument.DisplayName + ".");
    }

    public void HandleWrongInstrumentStep(SurgicalInstrumentInteractable instrument, int wrongAttemptCount)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        StartCoroutine(WrongInstrumentFeedbackRoutine(instrument, wrongAttemptCount));
    }

    public void HandleInstrumentTouchedAfterSequenceCompleted(SurgicalInstrumentInteractable instrument)
    {
        if (!isActive || isCompleted || timerExpired || instrument == null)
        {
            return;
        }

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed(instrument.InspectionSubtitle, ResolveSubtitleDuration(instrument.VoiceOrSound, 2.2f));
        PlayOneShot(instrument.VoiceOrSound, instrument.DisplayName + " voice/sound");
        RevealInstrumentNumber(instrument, instrument.InstrumentId == OperationInstrumentId.Stethoscope ? 3.5f : 1.5f);
        Debug.Log("Puzzle 4 instrument touched after sequence completion: " + instrument.DisplayName + ".");
    }

    public void HandleCorrectSequenceCompleted()
    {
        if (!isActive || isCompleted || timerExpired || correctSequenceWasCompleted)
        {
            return;
        }

        correctSequenceWasCompleted = true;
        PlayOneShot(correctSequenceSound, "correctSequenceSound");

        if (sequenceCompletedRoutine != null)
        {
            StopCoroutine(sequenceCompletedRoutine);
        }

        sequenceCompletedRoutine = StartCoroutine(CorrectSequenceCompletedRoutine());
        OnCorrectSequenceCompleted.Invoke();
        CorrectSequenceCompleted?.Invoke();
        Debug.Log("Puzzle 4 correct operation sequence completed.");
    }

    public bool SubmitPhoneAnswer(string answer)
    {
        if (!CanAnswerAtPhone)
        {
            Debug.LogWarning("Puzzle 4 phone answer submitted before the puzzle was ready.");
            return false;
        }

        string normalizedAnswer = string.IsNullOrWhiteSpace(answer) ? string.Empty : answer.Trim();
        if (normalizedAnswer == CorrectAnswer)
        {
            CompletePuzzle();
            return true;
        }

        HandleWrongPhoneAnswer();
        return false;
    }

    private IEnumerator CorrectSequenceCompletedRoutine()
    {
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("All six mouths have spoken.", 2.5f);
        yield return new WaitForSeconds(2.7f);
        ui.Subtitles.ShowTimed("The last one did not hear death.", 3.2f);

        if (stethoscopeInstrument != null)
        {
            RevealInstrumentNumber(stethoscopeInstrument, 5f);
        }
        else
        {
            Debug.LogWarning("Puzzle 4 completed the sequence, but stethoscope is missing for number reveal.");
        }

        ui.Objectives.SetObjective("Return to the phone.");
        sequenceCompletedRoutine = null;
    }

    private IEnumerator WrongInstrumentFeedbackRoutine(SurgicalInstrumentInteractable instrument, int wrongAttemptCount)
    {
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed(UnityEngine.Random.value > 0.5f ? "The mouth wakes too early." : "The order breaks.", 2.6f);
        PlayOneShot(wrongSequenceSound, "wrongSequenceSound");
        OnWrongInstrumentStep.Invoke();
        Debug.Log("Puzzle 4 wrong instrument step from " + (instrument != null ? instrument.DisplayName : "unknown") + ". Attempts: " + wrongAttemptCount + ".");

        yield return StartCoroutine(FlickerLightsRoutine());

        if (wrongAttemptCount >= 3)
        {
            TriggerMonster();
        }
    }

    private IEnumerator FlickerLightsRoutine()
    {
        PlayOneShot(lightFlickerSound, "lightFlickerSound");
        if (lightFlicker == null)
        {
            lightFlicker = FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        }

        if (lightFlicker != null)
        {
            lightFlicker.Flicker();
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            Debug.LogWarning("Puzzle 4 has no LightFlickerController assigned.");
            yield return null;
        }
    }

    private void HandleWrongPhoneAnswer()
    {
        wrongPhoneAttemptCount++;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("That mouth only tasted metal.", 2.8f);

        PlayOneShot(wrongAnswerSound, "wrongAnswerSound");
        StartCoroutine(FlickerLightsRoutine());

        OnWrongPhoneAnswer.Invoke();
        Debug.Log("Puzzle 4 wrong phone answer. Attempts: " + wrongPhoneAttemptCount + ".");

        if (wrongPhoneAttemptCount >= 3)
        {
            TriggerMonster();
        }
    }

    private void CompletePuzzle()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;
        isActive = false;
        timerExpired = false;
        SetPuzzleInteractable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();
        ui.Subtitles.ShowTimed("Marek heard it too. Then he continued.", 3.2f);
        ui.Objectives.SetObjective("Return to the phone.");

        PlayOneShot(correctAnswerSound, "correctAnswerSound");
        StoreCodeFragmentIfPossible(CorrectAnswer);

        OnCorrectPhoneAnswer.Invoke();
        OnPuzzle4Completed.Invoke();
        Puzzle4Completed?.Invoke();
        Debug.Log("Puzzle 4 completed. Code fragment stored: " + CorrectAnswer + ".");
    }

    private void ExpireTimer()
    {
        if (timerExpired || isCompleted)
        {
            return;
        }

        timerExpired = true;
        isActive = false;
        timeRemaining = 0f;
        SetPuzzleInteractable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("You were too late.", 3f);
        ui.Objectives.SetObjective("Hide.");
        ui.Timer.Hide();

        OnPuzzle4TimerExpired.Invoke();
        Puzzle4TimerExpired?.Invoke();
        TriggerMonster();
        Debug.Log("Puzzle 4 timer expired.");
    }

    private void SetPuzzleInteractable(bool value)
    {
        if (methodSheet != null)
        {
            methodSheet.SetReadable(value);
        }

        if (sequenceManager != null)
        {
            sequenceManager.SetInstrumentsInteractable(value);
        }
    }

    private void RevealInstrumentNumber(SurgicalInstrumentInteractable instrument, float seconds)
    {
        if (instrument == null)
        {
            return;
        }

        PlayOneShot(numberRevealSound, "numberRevealSound");
        instrument.RevealNumber(seconds);
        OnInstrumentNumberRevealed.Invoke();
        Debug.Log("Puzzle 4 revealed number " + instrument.InstrumentNumber + " on " + instrument.DisplayName + ".");
    }

    private void TriggerMonster()
    {
        if (monsterTriggered)
        {
            return;
        }

        monsterTriggered = true;
        PlayOneShot(monsterTriggerSound, "monsterTriggerSound");

        if (monsterActivation == null)
        {
            monsterActivation = FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        }

        if (monsterActivation != null)
        {
            monsterActivation.TriggerMonster();
        }
        else
        {
            GameObject monster = GameObject.Find("Monster_Entity");
            if (monster != null)
            {
                monster.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Puzzle 4 could not find Monster_Entity or MonsterActivationController.");
            }
        }

        OnMonsterTriggered.Invoke();
        Debug.Log("Puzzle 4 monster event triggered.");
    }

    private void ResolveReferences()
    {
        ResolvePuzzleRoot();
        ResolveInstrumentsParent();
        ResolveOperationTable();
        OperationRoomPassageFix.EnsureForScene(operationTable);
        ResolveSequenceManager();
        ResolveMethodSheet();
        ResolveInstruments();

        if (monsterActivation == null)
        {
            monsterActivation = FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        }

        if (lightFlicker == null)
        {
            lightFlicker = FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        }

        EnsurePuzzleAudioSource();
    }

    private void ResolvePuzzleRoot()
    {
        if (puzzleRoot != null)
        {
            return;
        }

        Transform existingPuzzleRoot = FindExactTransform(PuzzleRootFallbackName);
        if (existingPuzzleRoot != null)
        {
            puzzleRoot = existingPuzzleRoot.gameObject;
            return;
        }

        Transform instrumentsRoot = FindExactTransform(AllInstrumentsFallbackName);
        if (instrumentsRoot != null)
        {
            puzzleRoot = instrumentsRoot.gameObject;
            return;
        }

        GameObject created = new GameObject(PuzzleRootFallbackName);
        puzzleRoot = created;
        Debug.LogWarning("Puzzle 4 could not find Puzzle4_OperationOrder or All_Instruments, so it created Puzzle4_OperationOrder as an empty parent.");
    }

    private void ResolveInstrumentsParent()
    {
        if (allInstruments == null)
        {
            allInstruments = FindExactTransform(AllInstrumentsFallbackName);
        }

        if (allInstruments == null)
        {
            Debug.LogWarning("Puzzle 4 could not find All_Instruments. Assign it in the Inspector.");
        }
    }

    private void ResolveOperationTable()
    {
        if (operationTable != null)
        {
            return;
        }

        operationTable = FindExactTransform("surgical__instrument_table");
        if (operationTable != null)
        {
            return;
        }

        operationTable = FindExactTransform("Surgical_Table_Single");
        if (operationTable != null)
        {
            return;
        }

        operationTable = FindBestTransformByHints("OperationTable", "operation table", "OperatingTable", "operating table", "surgery table", "table_operation", "surgical table", "surgicalinstrumenttable");
    }

    private void ResolveSequenceManager()
    {
        if (sequenceManager == null)
        {
            sequenceManager = GetComponent<OperationInstrumentSequenceManager>();
        }

        if (sequenceManager == null && puzzleRoot != null)
        {
            sequenceManager = puzzleRoot.GetComponent<OperationInstrumentSequenceManager>();
        }

        if (sequenceManager == null)
        {
            sequenceManager = gameObject.AddComponent<OperationInstrumentSequenceManager>();
            Debug.Log("Puzzle 4 added OperationInstrumentSequenceManager.");
        }
    }

    private void ResolveMethodSheet()
    {
        if (methodSheet == null)
        {
            Transform sheetTransform = allInstruments != null
                ? FindChildRecursiveExact(allInstruments, MethodSheetFallbackName)
                : null;

            if (sheetTransform == null)
            {
                sheetTransform = FindExactTransform(MethodSheetFallbackName);
            }

            if (sheetTransform != null)
            {
                methodSheet = sheetTransform.GetComponent<OperationMethodSheetReadable>() ?? sheetTransform.gameObject.AddComponent<OperationMethodSheetReadable>();
            }
        }

        if (methodSheet == null)
        {
            Transform parent = allInstruments != null ? allInstruments : (puzzleRoot != null ? puzzleRoot.transform : transform);
            GameObject sheetObject = new GameObject(MethodSheetFallbackName);
            sheetObject.transform.SetParent(parent, true);
            methodSheet = sheetObject.AddComponent<OperationMethodSheetReadable>();
            Debug.Log("Puzzle 4 created OperationMethodSheet near the instruments.");
        }

        EnsureMethodSheetHierarchy();
        ApplyMethodSheetPlacement();
    }

    private void EnsureMethodSheetHierarchy()
    {
        if (methodSheet == null)
        {
            return;
        }

        GameObject sheetObject = methodSheet.gameObject;
        sheetObject.name = MethodSheetFallbackName;
        sheetObject.SetActive(true);

        Transform preferredParent = allInstruments != null ? allInstruments : (puzzleRoot != null ? puzzleRoot.transform : transform);
        if (preferredParent != null && methodSheet.transform.parent != preferredParent)
        {
            methodSheet.transform.SetParent(preferredParent, true);
        }

        methodSheet.transform.SetAsLastSibling();
    }

    private void ResolveInstruments()
    {
        resolvedInstrumentTargets.Clear();
        clampInstrument = ResolveInstrument(
            clampInstrument,
            "Clamp",
            OperationInstrumentId.Clamp,
            8,
            "He held the tissue still.",
            clampVoiceOrSound,
            new[] { "Clamp", "Clamp Overholt-Geissend", "Surgical Tweezers", "Tweezers", "Forceps" });

        scalpelInstrument = ResolveInstrument(
            scalpelInstrument,
            "Scalpel",
            OperationInstrumentId.Scalpel,
            2,
            "He opened the first layer.",
            scalpelVoiceOrSound,
            new[] { "Scalpel", "Scalpel handle Nr. 3", "Scalpel handle", "Blade" });

        scissorsInstrument = ResolveInstrument(
            scissorsInstrument,
            "Surgical scissors",
            OperationInstrumentId.SurgicalScissors,
            9,
            "He cut what resisted.",
            scissorsVoiceOrSound,
            new[] { "Surgical scissors", "Dissection Scissors Metzenbaum", "Scissors", "Metzenbaum", "Ciseaux" });

        retractorHookInstrument = ResolveInstrument(
            retractorHookInstrument,
            "Retractor/Hook",
            OperationInstrumentId.RetractorHook,
            6,
            "He pulled the wound apart.",
            retractorHookVoiceOrSound,
            new[] { "Retractor/Hook", "Retractor Langenbeck", "Retractor Roux", "Retractor", "Hook" });

        needleHolderInstrument = ResolveInstrument(
            needleHolderInstrument,
            "Needle Holder",
            OperationInstrumentId.NeedleHolder,
            5,
            "He secured the final tear.",
            needleHolderVoiceOrSound,
            new[] { "Needle Holder", "Needle holder DeBakey", "Needle holder", "DeBakey" });

        stethoscopeInstrument = ResolveInstrument(
            stethoscopeInstrument,
            "stethoscope",
            OperationInstrumentId.Stethoscope,
            4,
            "He listened before stopping the heart.",
            stethoscopeVoiceOrSound,
            new[] { "stethoscope", "Stethoscope" });
    }

    private SurgicalInstrumentInteractable ResolveInstrument(
        SurgicalInstrumentInteractable current,
        string objectName,
        OperationInstrumentId id,
        int number,
        string subtitle,
        AudioClip voiceClip,
        string[] nameAliases)
    {
        Transform target = current != null ? current.transform : null;
        if (!IsAvailableInstrumentTarget(target))
        {
            target = FindInstrumentTransform(objectName, nameAliases);
        }

        if (!IsAvailableInstrumentTarget(target))
        {
            Debug.LogWarning("Puzzle 4 could not find required instrument object: " + objectName + ". Expected it under All_Instruments.");
            return current;
        }

        resolvedInstrumentTargets.Add(target);
        SurgicalInstrumentInteractable interactable = target.GetComponent<SurgicalInstrumentInteractable>();
        if (interactable == null)
        {
            interactable = target.gameObject.AddComponent<SurgicalInstrumentInteractable>();
            Debug.Log("Puzzle 4 added SurgicalInstrumentInteractable to " + objectName + ".");
        }

        interactable.Configure(id, objectName, number, subtitle, sequenceManager, voiceClip);
        sequenceManager?.RegisterInstrument(interactable);
        return interactable;
    }

    private Transform FindInstrumentTransform(string objectName, string[] nameAliases)
    {
        Transform target = allInstruments != null ? FindChildRecursiveExact(allInstruments, objectName) : null;
        if (IsAvailableInstrumentTarget(target))
        {
            return target;
        }

        target = FindExactTransform(objectName);
        if (IsAvailableInstrumentTarget(target))
        {
            return target;
        }

        target = allInstruments != null ? FindBestChildByNameHints(allInstruments, nameAliases) : null;
        if (IsAvailableInstrumentTarget(target))
        {
            return target;
        }

        return FindBestTransformByNameHints(nameAliases);
    }

    private bool IsAvailableInstrumentTarget(Transform target)
    {
        return target != null && !resolvedInstrumentTargets.Contains(target);
    }

    private Transform FindBestChildByNameHints(Transform root, string[] nameHints)
    {
        if (root == null)
        {
            return null;
        }

        return FindBestAvailableByNameHints(root.GetComponentsInChildren<Transform>(true), nameHints);
    }

    private Transform FindBestTransformByNameHints(string[] nameHints)
    {
        return FindBestAvailableByNameHints(
            FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            nameHints);
    }

    private Transform FindBestAvailableByNameHints(Transform[] candidates, string[] nameHints)
    {
        if (candidates == null || nameHints == null || nameHints.Length == 0)
        {
            return null;
        }

        Transform best = null;
        int bestScore = 0;
        float bestRenderVolume = -1f;

        for (int i = 0; i < candidates.Length; i++)
        {
            Transform candidate = candidates[i];
            if (!IsAvailableInstrumentTarget(candidate) || candidate == allInstruments || candidate == operationTable)
            {
                continue;
            }

            string normalizedName = NormalizeName(candidate.name);
            int score = ScoreNameAgainstHints(normalizedName, nameHints);
            if (score <= 0)
            {
                continue;
            }

            float renderVolume = EstimateRenderVolume(candidate);
            if (score > bestScore || (score == bestScore && renderVolume > bestRenderVolume))
            {
                best = candidate;
                bestScore = score;
                bestRenderVolume = renderVolume;
            }
        }

        return best;
    }

    private static int ScoreNameAgainstHints(string normalizedName, string[] nameHints)
    {
        if (string.IsNullOrEmpty(normalizedName) || nameHints == null)
        {
            return 0;
        }

        int bestScore = 0;
        for (int i = 0; i < nameHints.Length; i++)
        {
            string normalizedHint = NormalizeName(nameHints[i]);
            if (string.IsNullOrEmpty(normalizedHint))
            {
                continue;
            }

            if (normalizedName == normalizedHint)
            {
                bestScore = Mathf.Max(bestScore, 120 - i);
            }
            else if (normalizedName.Contains(normalizedHint))
            {
                bestScore = Mathf.Max(bestScore, 90 - i);
            }
            else if (normalizedHint.Contains(normalizedName))
            {
                bestScore = Mathf.Max(bestScore, 60 - i);
            }
        }

        return bestScore;
    }

    private static float EstimateRenderVolume(Transform candidate)
    {
        if (candidate == null)
        {
            return 0f;
        }

        Renderer renderer = candidate.GetComponentInChildren<Renderer>(true);
        if (renderer == null)
        {
            return 0f;
        }

        Bounds bounds = renderer.bounds;
        return bounds.size.x * bounds.size.y * bounds.size.z;
    }

    private void ConfigureChildren()
    {
        Texture texture = ResolveOperationTexture();
        methodSheet?.Configure(this, texture, operationOrderSprite);

        SurgicalInstrumentInteractable[] orderedInstruments =
        {
            clampInstrument,
            scalpelInstrument,
            scissorsInstrument,
            retractorHookInstrument,
            needleHolderInstrument,
            stethoscopeInstrument
        };

        sequenceManager?.Configure(this, orderedInstruments);
    }

    private void PositionCreatedMethodSheet(Transform sheetTransform)
    {
        if (sheetTransform == null)
        {
            return;
        }

        Transform anchor = operationTable != null ? operationTable : allInstruments;
        if (anchor == null)
        {
            sheetTransform.position = transform.position + new Vector3(0.6f, 0.85f, 0.2f);
            sheetTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        Bounds bounds;
        if (TryGetRenderBounds(anchor, out bounds))
        {
            sheetTransform.position = bounds.center + new Vector3(0.35f, Mathf.Max(0.04f, bounds.extents.y + 0.035f), 0.18f);
        }
        else
        {
            sheetTransform.position = anchor.position + new Vector3(0.35f, 0.08f, 0.18f);
        }

        sheetTransform.rotation = Quaternion.Euler(90f, anchor.eulerAngles.y, 0f);
    }

    private void ApplyMethodSheetPlacement()
    {
        if (methodSheet == null)
        {
            return;
        }

        Transform sheetTransform = methodSheet.transform;
        if (useFixedMethodSheetPlacement)
        {
            sheetTransform.localPosition = methodSheetLocalPosition;
            sheetTransform.localEulerAngles = methodSheetLocalRotation;
            string parentName = sheetTransform.parent != null ? sheetTransform.parent.name : "scene root";
            Debug.Log("Puzzle 4 OperationMethodSheet ready under '" + parentName + "' at local position " + sheetTransform.localPosition + ".");
            return;
        }

        PositionCreatedMethodSheet(sheetTransform);
    }

    private Texture ResolveOperationTexture()
    {
        if (operationOrderTexture != null)
        {
            return operationOrderTexture;
        }

        if (operationOrderSprite != null)
        {
            return operationOrderSprite.texture;
        }

        return null;
    }

    private void PlayOneShot(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.Log("Puzzle 4 audio clip missing: " + clipName + ".");
            return;
        }

        AudioSource source = EnsurePuzzleAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private static float ResolveSubtitleDuration(AudioClip clip, float fallbackDuration)
    {
        return clip != null ? Mathf.Max(fallbackDuration, clip.length + 0.25f) : fallbackDuration;
    }

    private AudioSource EnsurePuzzleAudioSource()
    {
        try
        {
            if (puzzleAudioSource != null)
            {
                puzzleAudioSource.playOnAwake = false;
                return puzzleAudioSource;
            }
        }
        catch (MissingComponentException)
        {
            puzzleAudioSource = null;
        }

        puzzleAudioSource = GetComponent<AudioSource>();
        if (puzzleAudioSource == null)
        {
            puzzleAudioSource = gameObject.AddComponent<AudioSource>();
        }

        puzzleAudioSource.playOnAwake = false;
        return puzzleAudioSource;
    }

    private void StoreCodeFragmentIfPossible(string fragment)
    {
        UnityEngine.Object managerObject = codeFragmentManager != null ? codeFragmentManager : FindCodeFragmentManager();
        if (managerObject == null)
        {
            Debug.Log("Puzzle 4 code fragment manager not found. Fragment " + fragment + " remains stored by Puzzle 4 completion state only.");
            return;
        }

        if (TryInvokeFragmentMethod(managerObject, fragment))
        {
            Debug.Log("Puzzle 4 stored code fragment " + fragment + " in CodeFragmentManager.");
            return;
        }

        Debug.LogWarning("Puzzle 4 found CodeFragmentManager but could not find a compatible method to store fragment " + fragment + ".");
    }

    private static UnityEngine.Object FindCodeFragmentManager()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.GetType().Name == "CodeFragmentManager")
            {
                return behaviour;
            }
        }

        return null;
    }

    private static bool TryInvokeFragmentMethod(UnityEngine.Object managerObject, string fragment)
    {
        string[] methodNames =
        {
            "StoreCodeFragment",
            "AddCodeFragment",
            "RegisterCodeFragment",
            "StoreFragment",
            "AddFragment",
            "RegisterFragment",
            "SetFragment"
        };

        Type managerType = managerObject.GetType();
        for (int i = 0; i < methodNames.Length; i++)
        {
            MethodInfo[] methods = managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
            {
                MethodInfo method = methods[methodIndex];
                if (method.Name != methodNames[i])
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    method.Invoke(managerObject, new object[] { fragment });
                    return true;
                }

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int) && int.TryParse(fragment, out int intFragment))
                {
                    method.Invoke(managerObject, new object[] { intFragment });
                    return true;
                }

                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(string))
                {
                    method.Invoke(managerObject, new object[] { "Puzzle4", fragment });
                    return true;
                }
            }
        }

        return false;
    }

    private void TryAssignEditorDefaultImage()
    {
#if UNITY_EDITOR
        if (operationOrderTexture == null)
        {
            operationOrderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PhonePuzzle/ordre_operation.png");
        }

        if (operationOrderTexture == null)
        {
            operationOrderTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Phone_Puzzles/ordre_operation.png");
        }

        if (operationOrderSprite == null)
        {
            operationOrderSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PhonePuzzle/ordre_operation.png");
        }

        if (operationOrderSprite == null)
        {
            operationOrderSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phone_Puzzles/ordre_operation.png");
        }

        if (operationOrderTexture != null || operationOrderSprite != null)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private static bool TryGetRenderBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Transform FindExactTransform(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindChildRecursiveExact(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursiveExact(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindBestTransformByHints(params string[] hints)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform best = null;
        int bestScore = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            string normalizedName = NormalizeName(candidate.name);
            for (int hintIndex = 0; hintIndex < hints.Length; hintIndex++)
            {
                string normalizedHint = NormalizeName(hints[hintIndex]);
                int score = 0;
                if (normalizedName == normalizedHint)
                {
                    score = 100 - hintIndex;
                }
                else if (normalizedName.Contains(normalizedHint))
                {
                    score = 70 - hintIndex;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty).ToLowerInvariant();
    }
}

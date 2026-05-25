using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Puzzle5MorgueController : MonoBehaviour
{
    private const string Puzzle5SceneName = "demoScene_Difficile";
#if UNITY_EDITOR
    private const string DefaultCabinetOpenCloseClipPath = "Assets/audio/cabinet-open-close.mp3";
#endif

    [Header("Puzzle 5")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private List<MorgueCabinetDoor> cabinets = new List<MorgueCabinetDoor>();
    [SerializeField, Min(1f)] private float puzzleTimeLimit = 600f;
    [SerializeField] private string correctPhoneAnswer = "5";

    [Header("Scene References")]
    [SerializeField] private GameObject puzzleRoot;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private LightFlickerController lightFlicker;
    [Tooltip("Optional. If a CodeFragmentManager exists, Puzzle 5 will also try to find it automatically.")]
    [SerializeField] private UnityEngine.Object codeFragmentManager;

    [Header("Audio")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip cabinetOpenSound;
    [SerializeField] private AudioClip cabinetCloseSound;
    [SerializeField] private AudioClip call5VoiceClip;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip wrongAnswerSound;
    [SerializeField] private AudioClip timerExpiredSound;
    [SerializeField] private AudioClip monsterTriggerSound;

    [Header("Events")]
    public UnityEvent OnPuzzle5Activated = new UnityEvent();
    public UnityEvent OnPuzzle5TimerExpired = new UnityEvent();
    public UnityEvent OnPuzzle5Completed = new UnityEvent();
    public UnityEvent OnPuzzle5WrongAnswer = new UnityEvent();

    public event Action Puzzle5Completed;
    public event Action Puzzle5TimerExpired;

    private PhonePuzzleUI ui;
    private bool callUnlocked;
    private bool isActive;
    private bool isCompleted;
    private bool timerExpired;
    private bool hasOpenedCabinet;
    private bool monsterTriggered;
    private int wrongPhoneAnswerCount;
    private float timeRemaining;

    public AudioClip Call5VoiceClip => call5VoiceClip;
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool HasOpenedCabinet => hasOpenedCabinet;
    public bool CanAnswerAtPhone => isActive && hasOpenedCabinet && !isCompleted && !timerExpired;
    public int MaxAnswerLength => 1;
    public string AnswerPrompt => "What did the contradiction leave behind?";

    private void Awake()
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            return;
        }

        ResolveReferences();
        ConfigureCabinets();
        SetCabinetInteractions(false);
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultCabinetAudio();
    }

    private void Update()
    {
        if (!IsPuzzle5Scene())
        {
            return;
        }

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

    public void ConfigureSceneReferences(Transform parent, MonsterActivationController monster, LightFlickerController flicker)
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (parent != null)
        {
            slotsParent = parent;
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
        ConfigureCabinets();
        SetCabinetInteractions(false);
    }

    public void RegisterCabinets(IEnumerable<MorgueCabinetDoor> configuredCabinets)
    {
        if (configuredCabinets == null)
        {
            return;
        }

        cabinets.Clear();
        foreach (MorgueCabinetDoor cabinet in configuredCabinets)
        {
            if (cabinet == null || cabinets.Contains(cabinet))
            {
                continue;
            }

            cabinets.Add(cabinet);
        }

        ConfigureCabinets();
    }

    public void UnlockPuzzle5Call()
    {
        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (callUnlocked)
        {
            return;
        }

        callUnlocked = true;
        Debug.Log("Puzzle 5 call unlocked.");
    }

    public void ActivatePuzzle5()
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            Debug.Log("Puzzle 5 activation ignored because it is only enabled in demoScene_Difficile.");
            return;
        }

        ResolveReferences();
        ConfigureCabinets();

        if (puzzleRoot != null && !puzzleRoot.activeSelf)
        {
            puzzleRoot.SetActive(true);
            Debug.Log("Puzzle5_MorgueCabinets activated.");
        }

        if (isCompleted)
        {
            Debug.LogWarning("Puzzle 5 activation requested, but Puzzle 5 is already completed.");
            return;
        }

        ui = PhonePuzzleUI.EnsureExists();
        isActive = true;
        timerExpired = false;
        hasOpenedCabinet = false;
        monsterTriggered = false;
        wrongPhoneAnswerCount = 0;
        timeRemaining = puzzleTimeLimit;

        SetCabinetInteractions(true);
        ui.Objectives.SetObjective("Open only the bones of the first lie.");
        ui.Timer.SetTime(timeRemaining);

        OnPuzzle5Activated.Invoke();
        Debug.Log("Puzzle 5 activated.");
    }

    public void RegisterCabinetOpened(MorgueCabinetDoor cabinet)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        if (hasOpenedCabinet)
        {
            return;
        }

        hasOpenedCabinet = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");
        Debug.Log("Player opened first cabinet.");
        Debug.Log("Phone answer mode allowed for Puzzle 5.");
    }

    public bool SubmitPhoneAnswer(string answer)
    {
        if (!CanAnswerAtPhone)
        {
            Debug.LogWarning("Puzzle 5 phone answer submitted before the puzzle was ready.");
            return false;
        }

        string normalizedAnswer = string.IsNullOrWhiteSpace(answer) ? string.Empty : answer.Trim();
        if (normalizedAnswer == correctPhoneAnswer)
        {
            CompletePuzzle();
            return true;
        }

        HandleWrongPhoneAnswer();
        return false;
    }

    private void HandleWrongPhoneAnswer()
    {
        wrongPhoneAnswerCount++;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("That shape belonged to another record.", 2.8f);
        PlayOneShot(wrongAnswerSound, "wrongAnswerSound");
        lightFlicker?.Flicker();

        OnPuzzle5WrongAnswer.Invoke();
        Debug.Log("Puzzle 5 wrong answer. Attempts: " + wrongPhoneAnswerCount + ".");

        if (wrongPhoneAnswerCount >= 3)
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
        SetCabinetInteractions(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();
        ui.Subtitles.ShowTimed("He was never stored. He was rearranged.", 3.2f);
        ui.Objectives.SetObjective("Listen to the phone.");

        PlayOneShot(correctAnswerSound, "correctAnswerSound");
        StoreCodeFragmentIfPossible(correctPhoneAnswer);

        OnPuzzle5Completed.Invoke();
        Puzzle5Completed?.Invoke();
        Debug.Log("Puzzle 5 completed.");
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
        SetCabinetInteractions(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("You were too late.", 3f);
        ui.Objectives.SetObjective("Hide.");
        ui.Timer.Hide();

        PlayOneShot(timerExpiredSound, "timerExpiredSound");
        OnPuzzle5TimerExpired.Invoke();
        Puzzle5TimerExpired?.Invoke();
        TriggerMonster();
        Debug.Log("Puzzle 5 timer expired.");
    }

    private void SetCabinetInteractions(bool enabled)
    {
        for (int i = 0; i < cabinets.Count; i++)
        {
            MorgueCabinetDoor cabinet = cabinets[i];
            if (cabinet == null)
            {
                continue;
            }

            cabinet.SetPuzzleController(this);
            cabinet.SetDefaultAudioClips(cabinetOpenSound, cabinetCloseSound);
            cabinet.SetInteractionEnabled(enabled);

            if (enabled)
            {
                Debug.Log("Cabinet " + cabinet.CabinetNumber + " interaction enabled.");
            }
        }
    }

    private void ConfigureCabinets()
    {
        ResolveCabinets();

        for (int i = 0; i < cabinets.Count; i++)
        {
            MorgueCabinetDoor cabinet = cabinets[i];
            if (cabinet == null)
            {
                continue;
            }

            cabinet.Configure(cabinet.CabinetNumber, this, cabinetOpenSound, cabinetCloseSound);
            cabinet.EnsureInteractionCollider();
        }
    }

    private void ResolveReferences()
    {
        ui ??= PhonePuzzleUI.EnsureExists();

        if (puzzleRoot == null)
        {
            Transform root = FindExactTransform("Puzzle5_MorgueCabinets");
            if (root == null)
            {
                root = FindExactTransform("morgue");
            }

            if (root == null)
            {
                root = slotsParent;
            }

            if (root != null)
            {
                puzzleRoot = root.gameObject;
            }
        }

        if (slotsParent == null)
        {
            slotsParent = ResolveSlotsParent();
        }

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

    private void ResolveCabinets()
    {
        List<MorgueCabinetDoor> orderedCabinets = new List<MorgueCabinetDoor>();

        for (int i = 0; i <= 9; i++)
        {
            Transform cabinetTransform = FindCabinetTransform(i);
            if (cabinetTransform == null)
            {
                Debug.LogWarning("Puzzle 5 missing cabinet warning: Cabinet " + i + " was not found.");
                continue;
            }

            MorgueCabinetDoor door = cabinetTransform.GetComponent<MorgueCabinetDoor>();
            if (door == null)
            {
                door = cabinetTransform.gameObject.AddComponent<MorgueCabinetDoor>();
                Debug.Log("Puzzle 5 added MorgueCabinetDoor to Cabinet " + i + ".");
            }

            door.Configure(i, this, cabinetOpenSound, cabinetCloseSound);
            orderedCabinets.Add(door);
        }

        cabinets = orderedCabinets;
    }

    private Transform ResolveSlotsParent()
    {
        if (slotsParent != null)
        {
            return slotsParent;
        }

        if (name == "slots")
        {
            return transform;
        }

        Transform childSlots = FindChildRecursiveExact(transform, "slots");
        if (childSlots != null)
        {
            return childSlots;
        }

        Transform morgue = FindExactTransform("morgue");
        if (morgue != null)
        {
            childSlots = FindChildRecursiveExact(morgue, "slots");
            if (childSlots != null)
            {
                return childSlots;
            }
        }

        return FindExactTransform("slots");
    }

    private Transform FindCabinetTransform(int number)
    {
        string cabinetName = "Cabinet " + number;

        if (slotsParent != null)
        {
            Transform child = FindChildRecursiveExact(slotsParent, cabinetName);
            if (child != null)
            {
                return child;
            }
        }

        return FindExactTransform(cabinetName);
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
                Debug.LogWarning("Puzzle 5 could not find Monster_Entity or MonsterActivationController.");
            }
        }
    }

    private AudioSource EnsurePuzzleAudioSource()
    {
        if (puzzleAudioSource == null)
        {
            puzzleAudioSource = GetComponent<AudioSource>();
        }

        if (puzzleAudioSource == null)
        {
            puzzleAudioSource = gameObject.AddComponent<AudioSource>();
        }

        puzzleAudioSource.playOnAwake = false;
        return puzzleAudioSource;
    }

    private void PlayOneShot(AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            Debug.Log("Puzzle 5 audio clip missing: " + clipName + ".");
            return;
        }

        AudioSource source = EnsurePuzzleAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private void TryAssignEditorDefaultCabinetAudio()
    {
#if UNITY_EDITOR
        bool changed = false;
        AudioClip defaultClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultCabinetOpenCloseClipPath);
        if (defaultClip == null)
        {
            return;
        }

        if (cabinetOpenSound == null)
        {
            cabinetOpenSound = defaultClip;
            changed = true;
        }

        if (cabinetCloseSound == null)
        {
            cabinetCloseSound = defaultClip;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private void StoreCodeFragmentIfPossible(string fragment)
    {
        UnityEngine.Object managerObject = codeFragmentManager != null ? codeFragmentManager : FindCodeFragmentManager();
        if (managerObject == null)
        {
            Debug.Log("Puzzle 5 code fragment manager not found. Fragment " + fragment + " remains stored by Puzzle 5 completion state only.");
            return;
        }

        if (TryInvokeFragmentMethod(managerObject, fragment))
        {
            Debug.Log("Puzzle 5 stored code fragment " + fragment + " in CodeFragmentManager.");
            return;
        }

        Debug.LogWarning("Puzzle 5 found CodeFragmentManager but could not find a compatible method to store fragment " + fragment + ".");
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
        MethodInfo[] methods = managerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < methodNames.Length; i++)
        {
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
                    method.Invoke(managerObject, new object[] { "Puzzle5", fragment });
                    return true;
                }
            }
        }

        return false;
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

    private static bool IsPuzzle5Scene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == Puzzle5SceneName;
    }
}

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class DoctorReportsPuzzle : MonoBehaviour
{
    private const string CorrectAnswer = "46";
    private const string Puzzle3RoomFallbackName = "Puzzle3_room";
    private const string DoctorDeskFallbackName = "doctor_desk";
    private const string NameplateFallbackName = "Doctor_H_Marek_Nameplate";
    private const string WheelChairFallbackName = "wheel_Chair";

    [Header("Puzzle 3")]
    [SerializeField, Min(1f)] private float puzzleTimeLimit = 240f;
    [SerializeField] private bool hideRoomUntilCall3;

    [Header("Scene References")]
    [SerializeField] private GameObject puzzle3Room;
    [SerializeField] private Transform doctorDesk;
    [SerializeField] private Transform doctorNameplate;
    [SerializeField] private Transform wheelChair;
    [SerializeField] private DoctorDeskFileGenerator fileGenerator;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private LightFlickerController lightFlicker;
    [Tooltip("Optional. If a CodeFragmentManager exists, Puzzle 3 will also try to find it automatically.")]
    [SerializeField] private UnityEngine.Object codeFragmentManager;

    [Header("Audio")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip fileOpenSound;
    [SerializeField] private AudioClip paperRustleSound;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip wrongAnswerSound;
    [SerializeField] private AudioClip officeWhisperSound;
    [SerializeField] private AudioClip lightFlickerSound;
    [SerializeField] private AudioClip monsterTriggerSound;

    [Header("Events")]
    public UnityEvent OnPuzzle3Activated = new UnityEvent();
    public UnityEvent OnFilesGenerated = new UnityEvent();
    public UnityEvent OnMedicalFileRead = new UnityEvent();
    public UnityEvent OnCorrectPhoneAnswer = new UnityEvent();
    public UnityEvent OnWrongPhoneAnswer = new UnityEvent();
    public UnityEvent OnPuzzle3Completed = new UnityEvent();
    public UnityEvent OnPuzzle3TimerExpired = new UnityEvent();
    public UnityEvent OnMonsterTriggered = new UnityEvent();

    public event Action Puzzle3Completed;
    public event Action Puzzle3TimerExpired;
    public event Action MedicalFileRead;

    private PhonePuzzleUI ui;
    private bool activationRequested;
    private bool isActive;
    private bool isCompleted;
    private bool timerExpired;
    private bool hasReadAnyReport;
    private bool monsterTriggered;
    private int wrongAttemptCount;
    private float timeRemaining;

    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool HasReadAnyReport => hasReadAnyReport;
    public bool CanAnswerAtPhone => isActive && hasReadAnyReport && !isCompleted && !timerExpired;
    public int MaxAnswerLength => 2;
    public string AnswerPrompt => "What remains after the lies?";

    private void Awake()
    {
        ResolveReferences();
        ConfigureGenerator();
        SetGeneratedFilesReadable(false);

        if (hideRoomUntilCall3 && !activationRequested && puzzle3Room != null && puzzle3Room.activeSelf)
        {
            puzzle3Room.SetActive(false);
            Debug.Log("Puzzle3_room hidden until Phone Call 3.");
        }
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

    public void ConfigureSceneReferences(Transform desk, MonsterActivationController monster, LightFlickerController flicker)
    {
        if (desk != null)
        {
            doctorDesk = desk;
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
        ConfigureGenerator();
    }

    public void ActivatePuzzle3()
    {
        activationRequested = true;
        puzzle3Room ??= gameObject;

        if (!puzzle3Room.activeSelf)
        {
            puzzle3Room.SetActive(true);
            Debug.Log("Puzzle3_room activated for Doctor Marek's Reports.");
        }

        ResolveReferences();
        ConfigureGenerator();

        if (isCompleted)
        {
            Debug.LogWarning("Puzzle 3 activation requested, but Puzzle 3 is already completed.");
            return;
        }

        ui = PhonePuzzleUI.EnsureExists();
        isActive = true;
        timerExpired = false;
        timeRemaining = puzzleTimeLimit;
        monsterTriggered = false;
        wrongAttemptCount = 0;

        GenerateFilesIfNeeded();
        SetGeneratedFilesReadable(true);

        ui.Objectives.SetObjective("Search Doctor Marek's reports.");
        ui.Timer.SetTime(timeRemaining);
        PlayOneShot(officeWhisperSound);

        OnPuzzle3Activated.Invoke();
        Debug.Log("Puzzle 3 activated: Doctor Marek's Reports.");
    }

    public void RegisterMedicalFileRead(MedicalFileReadable medicalFile)
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        hasReadAnyReport = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");

        OnMedicalFileRead.Invoke();
        MedicalFileRead?.Invoke();
        Debug.Log("Puzzle 3 registered medical file read: " + (medicalFile != null ? medicalFile.FileTitle : "unknown file") + ".");
    }

    public bool SubmitPhoneAnswer(string answer)
    {
        if (!CanAnswerAtPhone)
        {
            Debug.LogWarning("Puzzle 3 phone answer submitted before the puzzle was ready.");
            return false;
        }

        string normalizedAnswer = string.IsNullOrWhiteSpace(answer) ? string.Empty : answer.Trim();
        if (normalizedAnswer == CorrectAnswer)
        {
            CompletePuzzle();
            return true;
        }

        HandleWrongAnswer();
        return false;
    }

    public void SetGeneratedFilesReadable(bool readable)
    {
        fileGenerator?.SetFilesReadable(readable);
    }

    private void GenerateFilesIfNeeded()
    {
        if (fileGenerator == null)
        {
            Debug.LogWarning("Puzzle 3 cannot generate medical reports because DoctorDeskFileGenerator is missing.");
            return;
        }

        fileGenerator.GenerateFilesIfNeeded(this, fileOpenSound, paperRustleSound);
        OnFilesGenerated.Invoke();
        Debug.Log("Puzzle 3 medical reports generated or reused.");
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
        SetGeneratedFilesReadable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();
        ui.Subtitles.ShowTimed("He screamed at forty-six. The first hour is complete.", 3.2f);
        ui.Objectives.SetObjective("Return to the phone when it rings again.");

        PlayOneShot(correctAnswerSound);
        StoreCodeFragmentIfPossible(CorrectAnswer);

        OnCorrectPhoneAnswer.Invoke();
        OnPuzzle3Completed.Invoke();
        Puzzle3Completed?.Invoke();
        Debug.Log("Puzzle 3 completed. Code fragment stored: " + CorrectAnswer + ".");
    }

    private void HandleWrongAnswer()
    {
        wrongAttemptCount++;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("That report was not written for the night.", 2.8f);

        PlayOneShot(wrongAnswerSound);
        OnWrongPhoneAnswer.Invoke();
        Debug.Log("Puzzle 3 wrong phone answer. Attempts: " + wrongAttemptCount);

        if (wrongAttemptCount >= 2)
        {
            PlayOneShot(lightFlickerSound);
            if (lightFlicker == null)
            {
                lightFlicker = FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
            }

            if (lightFlicker != null)
            {
                lightFlicker.Flicker();
            }
            else
            {
                Debug.LogWarning("Puzzle 3 has no LightFlickerController assigned for wrong-answer escalation.");
            }
        }

        if (wrongAttemptCount >= 3)
        {
            TriggerMonster();
        }
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
        SetGeneratedFilesReadable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("You were too late.", 3f);
        ui.Objectives.SetObjective("Hide.");
        ui.Timer.Hide();

        OnPuzzle3TimerExpired.Invoke();
        Puzzle3TimerExpired?.Invoke();
        TriggerMonster();
        Debug.Log("Puzzle 3 timer expired.");
    }

    private void TriggerMonster()
    {
        if (monsterTriggered)
        {
            return;
        }

        monsterTriggered = true;
        PlayOneShot(monsterTriggerSound);

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
                Debug.LogWarning("Puzzle 3 could not find Monster_Entity or MonsterActivationController.");
            }
        }

        OnMonsterTriggered.Invoke();
        Debug.Log("Puzzle 3 monster event triggered.");
    }

    private void ResolveReferences()
    {
        puzzle3Room ??= gameObject;

        if (puzzle3Room != null && puzzle3Room.name != Puzzle3RoomFallbackName)
        {
            Debug.LogWarning("DoctorReportsPuzzle is not on an object named Puzzle3_room. This is allowed, but Puzzle3_room is the expected parent object.");
        }

        Transform roomRoot = puzzle3Room != null ? puzzle3Room.transform : transform;

        if (doctorDesk == null)
        {
            doctorDesk = FindChildRecursive(roomRoot, DoctorDeskFallbackName);
            if (doctorDesk == null)
            {
                doctorDesk = FindExactTransform(DoctorDeskFallbackName);
            }

            if (doctorDesk == null)
            {
                Debug.LogWarning("Puzzle 3 could not find doctor_desk. Assign it in the Inspector.");
            }
        }

        if (doctorNameplate == null && doctorDesk != null)
        {
            doctorNameplate = FindChildRecursive(doctorDesk, NameplateFallbackName);
            if (doctorNameplate == null)
            {
                Debug.LogWarning("Puzzle 3 could not find Doctor_H_Marek_Nameplate under doctor_desk.");
            }
        }

        if (wheelChair == null)
        {
            wheelChair = FindChildRecursive(roomRoot, WheelChairFallbackName);
            if (wheelChair == null)
            {
                Debug.LogWarning("Puzzle 3 could not find wheel_Chair. Continuing without it.");
            }
        }

        if (fileGenerator == null && doctorDesk != null)
        {
            fileGenerator = doctorDesk.GetComponent<DoctorDeskFileGenerator>();
            if (fileGenerator == null)
            {
                fileGenerator = doctorDesk.gameObject.AddComponent<DoctorDeskFileGenerator>();
                Debug.Log("Puzzle 3 added DoctorDeskFileGenerator to doctor_desk.");
            }
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

    private void ConfigureGenerator()
    {
        if (fileGenerator != null)
        {
            fileGenerator.ConfigureDesk(doctorDesk);
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = EnsurePuzzleAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
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
            Debug.Log("Puzzle 3 code fragment manager not found. Fragment " + fragment + " remains stored by Puzzle 3 completion state only.");
            return;
        }

        if (TryInvokeFragmentMethod(managerObject, fragment))
        {
            Debug.Log("Puzzle 3 stored code fragment " + fragment + " in CodeFragmentManager.");
            return;
        }

        Debug.LogWarning("Puzzle 3 found CodeFragmentManager but could not find a compatible method to store fragment " + fragment + ".");
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
                    method.Invoke(managerObject, new object[] { "Puzzle3", fragment });
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

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

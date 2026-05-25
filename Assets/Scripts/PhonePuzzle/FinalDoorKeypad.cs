using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FinalDoorKeypad : Interactable
{
    [Header("Final Code")]
    [SerializeField] private string correctCode = "024645";
    [SerializeField, Min(1)] private int maxInputLength = 6;
    [SerializeField, Min(0.1f)] private float wrongClearDelay = 0.85f;

    [Header("Scene References")]
    [SerializeField] private FinalExitDoubleDoorController exitDoorController;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private LightFlickerController lightFlicker;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keypadPressSound;
    [SerializeField] private AudioClip correctCodeSound;
    [SerializeField] private AudioClip wrongCodeSound;

    [Header("Events")]
    public UnityEvent OnCorrectFinalCode = new UnityEvent();
    public UnityEvent OnWrongFinalCode = new UnityEvent();
    public UnityEvent OnLevelEscapeReady = new UnityEvent();

    private PhoneAnswerInput answerInput;
    private PhonePuzzleUI ui;
    private Coroutine wrongClearRoutine;
    private int wrongAttemptCount;
    private bool isInputOpen;
    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;
    public int WrongAttemptCount => wrongAttemptCount;

    private void Awake()
    {
        ResolveReferences();
        EnsureInteractionCollider();
        SetKeypadEnabled(false);
    }

    private void Update()
    {
        if (!isInputOpen)
        {
            return;
        }

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)) ||
                Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i)))
            {
                PlayOneShot(keypadPressSound);
                return;
            }
        }
    }

    public void Configure(FinalExitDoubleDoorController doors, MonsterActivationController monster, LightFlickerController flicker)
    {
        if (doors != null)
        {
            exitDoorController = doors;
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
        EnsureInteractionCollider();
    }

    public void SetKeypadEnabled(bool enabled)
    {
        SetInteractable(enabled && !isUnlocked);

        if (!enabled && isInputOpen)
        {
            EndInput();
        }
    }

    public override string GetInteractPrompt()
    {
        if (isUnlocked)
        {
            return "Exit unlocked";
        }

        return "Press E to enter final code";
    }

    public override void Interact()
    {
        if (!CanInteract || isUnlocked)
        {
            return;
        }

        BeginInput();
    }

    private void BeginInput()
    {
        ResolveReferences();
        ui = PhonePuzzleUI.EnsureExists();

        if (answerInput == null)
        {
            answerInput = GetComponent<PhoneAnswerInput>() ?? gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(maxInputLength, HandleCodeSubmitted);
        isInputOpen = true;
        Debug.Log("Final door keypad input opened.");
    }

    private void EndInput()
    {
        isInputOpen = false;
        answerInput?.EndAnswerMode();
    }

    private void HandleCodeSubmitted(string submittedCode)
    {
        if (isUnlocked)
        {
            return;
        }

        string normalizedCode = string.IsNullOrWhiteSpace(submittedCode) ? string.Empty : submittedCode.Trim();
        if (normalizedCode == correctCode)
        {
            HandleCorrectCode();
            return;
        }

        HandleWrongCode();
    }

    private void HandleCorrectCode()
    {
        isUnlocked = true;
        EndInput();
        SetInteractable(false);
        PlayOneShot(correctCodeSound);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("The hospital exhales.", 3f);
        ui.Objectives.SetObjective("Escape.");

        if (exitDoorController == null)
        {
            exitDoorController = FindFirstObjectByType<FinalExitDoubleDoorController>(FindObjectsInactive.Include);
        }

        if (exitDoorController != null)
        {
            exitDoorController.OpenDoors();
        }
        else
        {
            Debug.LogWarning("FinalDoorKeypad accepted the final code, but no FinalExitDoubleDoorController was found.");
        }

        OnCorrectFinalCode.Invoke();
        OnLevelEscapeReady.Invoke();
        Debug.Log("Final door code accepted: " + correctCode + ".");
    }

    private void HandleWrongCode()
    {
        wrongAttemptCount++;
        PlayOneShot(wrongCodeSound);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("The door remembers a different body.", 2.8f);
        lightFlicker?.Flicker();

        OnWrongFinalCode.Invoke();
        Debug.Log("Final door code wrong. Attempts: " + wrongAttemptCount + ".");

        if (wrongAttemptCount >= 3)
        {
            TriggerMonster();
        }

        if (wrongClearRoutine != null)
        {
            StopCoroutine(wrongClearRoutine);
        }

        wrongClearRoutine = StartCoroutine(ClearWrongInputAfterDelay());
    }

    private IEnumerator ClearWrongInputAfterDelay()
    {
        yield return new WaitForSeconds(wrongClearDelay);
        answerInput?.ClearAnswer();
        wrongClearRoutine = null;
    }

    private void TriggerMonster()
    {
        if (monsterActivation == null)
        {
            monsterActivation = FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        }

        if (monsterActivation != null)
        {
            monsterActivation.TriggerMonster();
            return;
        }

        GameObject monster = GameObject.Find("Monster_Entity");
        if (monster != null)
        {
            monster.SetActive(true);
        }
        else
        {
            Debug.LogWarning("FinalDoorKeypad could not find Monster_Entity or MonsterActivationController.");
        }
    }

    private void ResolveReferences()
    {
        ui ??= PhonePuzzleUI.EnsureExists();

        if (exitDoorController == null)
        {
            exitDoorController = FindFirstObjectByType<FinalExitDoubleDoorController>(FindObjectsInactive.Include);
        }

        if (monsterActivation == null)
        {
            monsterActivation = FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        }

        if (lightFlicker == null)
        {
            lightFlicker = FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        }

        if (answerInput == null)
        {
            answerInput = GetComponent<PhoneAnswerInput>();
        }

        EnsureAudioSource();
    }

    private void EnsureInteractionCollider()
    {
        Collider collider = GetComponentInChildren<Collider>(true);
        if (collider != null)
        {
            collider.isTrigger = true;
            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.45f, 0.62f, 0.16f);
        box.center = Vector3.zero;
    }

    private AudioSource EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        return audioSource;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = EnsureAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }
}

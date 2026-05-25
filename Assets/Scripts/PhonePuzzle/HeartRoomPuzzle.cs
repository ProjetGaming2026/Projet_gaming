using System;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeartRoomPuzzle : MonoBehaviour
{
    [Header("Puzzle 2")]
    [SerializeField, Min(1f)] private float puzzleTimeLimit = 180f;
    [SerializeField] private bool hideRoomUntilCall2 = true;

    [Header("Scene References")]
    [SerializeField] private GameObject puzzle2Room;
    [SerializeField] private Transform skeleton;
    [SerializeField] private AnimatedHeartSequence animatedHeart;
    [SerializeField] private KeypadBoxCode keypadBox;
    [SerializeField] private TornMedicalPaperReadable tornMedicalPaper;
    [SerializeField] private GameObject lockedBox;
    [SerializeField] private Transform boxDoor;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private LightFlickerController lightFlicker;

    [Header("Audio")]
    [SerializeField] private AudioSource puzzleAudioSource;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private AudioClip heartStaticClip;
    [SerializeField] private AudioClip boxKeypadPressSound;
    [SerializeField] private AudioClip boxUnlockSound;
    [SerializeField] private AudioClip boxOpenSound;
    [SerializeField] private AudioClip boxWrongCodeSound;
    [SerializeField] private AudioClip paperPickupSound;
    [SerializeField] private AudioClip scaryWhisperSound;
    [SerializeField] private AudioClip monsterTriggerSound;

    [Header("Events")]
    public UnityEvent OnPuzzle2Activated = new UnityEvent();
    public UnityEvent OnHeartSequenceStarted = new UnityEvent();
    public UnityEvent OnHeartBeat = new UnityEvent();
    public UnityEvent OnHeartSequenceFinished = new UnityEvent();
    public UnityEvent OnCorrectBoxCode = new UnityEvent();
    public UnityEvent OnWrongBoxCode = new UnityEvent();
    public UnityEvent OnBoxOpened = new UnityEvent();
    public UnityEvent OnPaperRead = new UnityEvent();
    public UnityEvent OnPuzzle2Completed = new UnityEvent();
    public UnityEvent OnPuzzle2TimerExpired = new UnityEvent();
    public UnityEvent OnMonsterTriggered = new UnityEvent();

    public event Action Puzzle2Completed;
    public event Action PaperRead;
    public event Action Puzzle2TimerExpired;

    private PhonePuzzleUI ui;
    private bool activationRequested;
    private bool isActive;
    private bool isCompleted;
    private bool timerExpired;
    private bool paperWasRead;
    private bool monsterTriggered;
    private float timeRemaining;

    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool HasReadPaper => paperWasRead;

#if UNITY_EDITOR
    private const string DefaultHeartbeatClipPath = "Assets/audio/heartBeat.mp3";
    private const string DefaultBoxOpenSoundPath = "Assets/audio/opendoor.mp3";
#endif

    private void Awake()
    {
        TryAssignEditorDefaultAudio();
        ResolveReferences();
        ConfigureChildren();
        SetPuzzleInteractable(false);
        SetPaperVisible(false);

        if (hideRoomUntilCall2 && !activationRequested && puzzle2Room != null && puzzle2Room.activeSelf)
        {
            puzzle2Room.SetActive(false);
            Debug.Log("Puzzle2_room hidden until Phone Call 2.");
        }
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultAudio();
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

    public void ActivatePuzzle2()
    {
        activationRequested = true;
        puzzle2Room ??= gameObject;

        if (!puzzle2Room.activeSelf)
        {
            puzzle2Room.SetActive(true);
        }

        ResolveReferences();
        ConfigureChildren();

        if (isCompleted)
        {
            Debug.LogWarning("Puzzle 2 activation requested, but Puzzle 2 is already completed.");
            return;
        }

        ui = PhonePuzzleUI.EnsureExists();
        isActive = true;
        timerExpired = false;
        timeRemaining = puzzleTimeLimit;
        monsterTriggered = false;

        SetPuzzleInteractable(true);
        SetPaperVisible(false);
        ui.Objectives.SetObjective("Find the source of the beating.");
        ui.Timer.SetTime(timeRemaining);

        OnPuzzle2Activated.Invoke();
        Debug.Log("The Beating Heart Room activated.");
    }

    public void RegisterHeartSequenceStarted()
    {
        OnHeartSequenceStarted.Invoke();
        Debug.Log("Puzzle 2 heart sequence started.");
    }

    public void RegisterHeartBeat()
    {
        OnHeartBeat.Invoke();
        Debug.Log("Puzzle 2 heart beat.");
    }

    public void RegisterHeartSequenceFinished()
    {
        OnHeartSequenceFinished.Invoke();
        Debug.Log("Puzzle 2 heart sequence finished.");
    }

    public void RegisterCorrectBoxCode()
    {
        OnCorrectBoxCode.Invoke();
        Debug.Log("Puzzle 2 locked box code accepted.");
    }

    public void RegisterWrongBoxCode(int wrongAttempts)
    {
        OnWrongBoxCode.Invoke();
        lightFlicker?.Flicker();
        Debug.Log("Puzzle 2 wrong box code. Attempts: " + wrongAttempts);

        if (wrongAttempts >= 3)
        {
            TriggerMonster(false);
        }
    }

    public void RegisterBoxOpened()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;
        isActive = false;
        SetPuzzleInteractable(false);
        SetPaperVisible(true);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();
        ui.Objectives.SetObjective(tornMedicalPaper != null ? "Read the torn medical paper." : "Return to the phone.");

        OnBoxOpened.Invoke();
        OnPuzzle2Completed.Invoke();
        Puzzle2Completed?.Invoke();
        Debug.Log("Puzzle 2 box opened and timer stopped.");

        if (tornMedicalPaper == null)
        {
            RegisterPaperRead();
        }
    }

    public void RegisterPaperRead()
    {
        if (paperWasRead)
        {
            return;
        }

        paperWasRead = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");

        OnPaperRead.Invoke();
        PaperRead?.Invoke();
        Debug.Log("Puzzle 2 torn medical paper read. Next phone call unlocked.");
    }

    public void TriggerMonsterFromWrongAttempts()
    {
        TriggerMonster(false);
    }

    private void ExpireTimer()
    {
        timerExpired = true;
        isActive = false;
        timeRemaining = 0f;
        SetPuzzleInteractable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("You were too late.", 3f);
        ui.Objectives.SetObjective("Hide.");
        ui.Timer.Hide();

        PlayOneShot(scaryWhisperSound);
        TriggerMonster(true);

        OnPuzzle2TimerExpired.Invoke();
        Puzzle2TimerExpired?.Invoke();
        Debug.Log("Puzzle 2 timer expired.");
    }

    private void TriggerMonster(bool updateObjective)
    {
        if (monsterTriggered)
        {
            return;
        }

        monsterTriggered = true;
        PlayOneShot(monsterTriggerSound);

        if (updateObjective)
        {
            ui ??= PhonePuzzleUI.EnsureExists();
            ui.Objectives.SetObjective("Hide.");
        }

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
                Debug.LogWarning("Puzzle 2 could not find Monster_Entity or MonsterActivationController.");
            }
        }

        OnMonsterTriggered.Invoke();
        Debug.Log("Puzzle 2 monster event triggered.");
    }

    private void ResolveReferences()
    {
        puzzle2Room ??= gameObject;

        if (skeleton == null)
        {
            skeleton = FindChildRecursive(puzzle2Room.transform, "skeleton");
            if (skeleton == null)
            {
                Debug.LogWarning("Puzzle 2 could not find skeleton.");
            }
        }

        if (animatedHeart == null)
        {
            Transform heartTransform = FindChildRecursive(puzzle2Room.transform, "Animated_heart");
            if (heartTransform != null)
            {
                animatedHeart = heartTransform.GetComponent<AnimatedHeartSequence>() ?? heartTransform.gameObject.AddComponent<AnimatedHeartSequence>();
            }
            else
            {
                Debug.LogWarning("Puzzle 2 could not find Animated_heart.");
            }
        }

        if (lockedBox == null)
        {
            Transform boxTransform = FindChildRecursive(puzzle2Room.transform, "Lbox");
            if (boxTransform != null)
            {
                lockedBox = boxTransform.gameObject;
            }
            else
            {
                Debug.LogWarning("Puzzle 2 could not find Lbox.");
            }
        }

        if (boxDoor == null)
        {
            boxDoor = FindChildRecursive(puzzle2Room.transform, "Door");
            if (boxDoor == null)
            {
                Debug.LogWarning("Puzzle 2 could not find Door.");
            }
        }

        if (keypadBox == null)
        {
            Transform keypadTransform = FindChildRecursive(puzzle2Room.transform, "keypad_Lbox");
            if (keypadTransform != null)
            {
                keypadBox = keypadTransform.GetComponent<KeypadBoxCode>() ?? keypadTransform.gameObject.AddComponent<KeypadBoxCode>();
            }
            else
            {
                Debug.LogWarning("Puzzle 2 could not find keypad_Lbox.");
            }
        }

        if (tornMedicalPaper == null && lockedBox != null)
        {
            Transform paperTransform = FindChildRecursive(lockedBox.transform, "TornMedicalPaper");
            if (paperTransform == null)
            {
                paperTransform = FindChildRecursiveIgnoreCase(lockedBox.transform, "papierDansBox");
            }

            if (paperTransform == null)
            {
                paperTransform = FindPaperChildRecursive(lockedBox.transform);
            }

            if (paperTransform == null)
            {
                Debug.LogWarning("Puzzle 2 could not find a paper object under Lbox. Name it TornMedicalPaper or include paper/papier/medical/torn in its name.");
            }
            else
            {
                tornMedicalPaper = paperTransform.GetComponent<TornMedicalPaperReadable>() ?? paperTransform.gameObject.AddComponent<TornMedicalPaperReadable>();
                Debug.Log("Puzzle 2 found medical paper object: " + paperTransform.name + ".");
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

    private void ConfigureChildren()
    {
        animatedHeart?.Configure(this, heartbeatClip, heartStaticClip, scaryWhisperSound);
        keypadBox?.Configure(this, boxDoor, boxKeypadPressSound, boxUnlockSound, boxOpenSound, boxWrongCodeSound);
        tornMedicalPaper?.Configure(this, paperPickupSound);
    }

    private void SetPuzzleInteractable(bool value)
    {
        animatedHeart?.SetHeartInteractable(value);
        keypadBox?.SetKeypadInteractable(value);
    }

    private void SetPaperVisible(bool visible)
    {
        tornMedicalPaper?.SetPaperVisible(visible);
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

        puzzleAudioSource = gameObject.AddComponent<AudioSource>();
        try
        {
            puzzleAudioSource.playOnAwake = false;
        }
        catch (MissingComponentException)
        {
            Debug.LogWarning("HeartRoomPuzzle could not create a usable AudioSource on Puzzle2_room. Continuing without Puzzle 2 audio.");
            puzzleAudioSource = null;
        }

        return puzzleAudioSource;
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

    private static Transform FindChildRecursiveIgnoreCase(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursiveIgnoreCase(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindPaperChildRecursive(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        string normalizedName = root.name.ToLowerInvariant();
        if (normalizedName.Contains("paper") ||
            normalizedName.Contains("papier") ||
            normalizedName.Contains("medical") ||
            normalizedName.Contains("torn"))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindPaperChildRecursive(root.GetChild(i));
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void TryAssignEditorDefaultAudio()
    {
#if UNITY_EDITOR
        if (heartbeatClip != null)
        {
            if (boxOpenSound == null)
            {
                boxOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultBoxOpenSoundPath);
                if (boxOpenSound != null)
                {
                    EditorUtility.SetDirty(this);
                }
            }

            return;
        }

        heartbeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultHeartbeatClipPath);
        if (boxOpenSound == null)
        {
            boxOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultBoxOpenSoundPath);
        }

        if (heartbeatClip != null || boxOpenSound != null)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }
}

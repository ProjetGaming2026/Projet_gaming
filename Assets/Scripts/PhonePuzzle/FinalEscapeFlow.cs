using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FinalEscapeFlow : MonoBehaviour
{
#if UNITY_EDITOR
    private const string DefaultFinalPhoneVoicePath = "Assets/audio/The last voice for exit.mp3";
#endif

    [Header("Puzzle Flow")]
    [SerializeField] private Puzzle5MorgueController puzzle5MorgueController;
    [SerializeField] private FinalDoorKeypad finalDoorKeypad;
    [SerializeField, Min(0f)] private float puzzle5CompletionMessageDelay = 3.35f;

    [Header("Phone Message")]
    [SerializeField] private PhoneCallData finalPhoneCall;
    [SerializeField] private PhoneCallSequenceManager callSequenceManager;
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip finalPhoneVoiceClip;

    [Header("Events")]
    public UnityEvent OnFinalPhoneMessageStarted = new UnityEvent();
    public UnityEvent OnFinalPhoneMessageFinished = new UnityEvent();
    public UnityEvent OnLevelEscapeReady = new UnityEvent();

    private PhonePuzzleUI ui;
    private bool subscribed;
    private bool finalFlowStarted;

    private void Awake()
    {
        TryAssignEditorDefaultAudio();
        ResolveReferences();
        finalDoorKeypad?.SetKeypadEnabled(false);
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultAudio();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Configure(Puzzle5MorgueController puzzle5, FinalDoorKeypad keypad, PhoneCallSequenceManager sequenceManager, AudioSource phoneSource)
    {
        TryAssignEditorDefaultAudio();

        if (puzzle5 != null)
        {
            Unsubscribe();
            puzzle5MorgueController = puzzle5;
        }

        if (keypad != null)
        {
            finalDoorKeypad = keypad;
            finalDoorKeypad.SetKeypadEnabled(false);
        }

        if (sequenceManager != null)
        {
            callSequenceManager = sequenceManager;
        }

        if (phoneSource != null)
        {
            phoneAudioSource = phoneSource;
        }

        ResolveReferences();
        Subscribe();
    }

    public void BeginFinalEscapeFlow()
    {
        if (finalFlowStarted)
        {
            return;
        }

        finalFlowStarted = true;
        finalDoorKeypad?.SetKeypadEnabled(false);
        StartCoroutine(PlayFinalPhoneMessageRoutine());
    }

    private void HandlePuzzle5Completed()
    {
        BeginFinalEscapeFlow();
    }

    private System.Collections.IEnumerator PlayFinalPhoneMessageRoutine()
    {
        if (puzzle5CompletionMessageDelay > 0f)
        {
            yield return new WaitForSeconds(puzzle5CompletionMessageDelay);
        }

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Listen to the phone.");
        OnFinalPhoneMessageStarted.Invoke();

        if (callSequenceManager == null)
        {
            callSequenceManager = GetComponent<PhoneCallSequenceManager>() ?? FindFirstObjectByType<PhoneCallSequenceManager>(FindObjectsInactive.Include);
        }

        if (phoneAudioSource == null)
        {
            phoneAudioSource = GetComponent<AudioSource>();
        }

        if (callSequenceManager != null)
        {
            bool finished = false;
            callSequenceManager.PlayCall(finalPhoneCall, phoneAudioSource, finalPhoneVoiceClip, () => finished = true, CreateDefaultFinalPhoneLines());

            while (!finished)
            {
                yield return null;
            }
        }
        else
        {
            PhoneCallData.SubtitleLine[] lines = CreateDefaultFinalPhoneLines();
            for (int i = 0; i < lines.Length; i++)
            {
                ui.Subtitles.Show(lines[i].text);
                yield return new WaitForSeconds(lines[i].duration);
            }

            ui.Subtitles.Hide();
        }

        finalDoorKeypad?.SetKeypadEnabled(true);
        ui.Objectives.SetObjective("Go to the exit door.");
        OnFinalPhoneMessageFinished.Invoke();
        OnLevelEscapeReady.Invoke();
        Debug.Log("Final escape flow enabled the final door keypad.");
    }

    private void TryAssignEditorDefaultAudio()
    {
        PhonePuzzleAudioLibrary library = PhonePuzzleAudioLibrary.Load();
        if (library != null)
        {
            finalPhoneVoiceClip ??= library.FinalPhoneVoice;
        }

#if UNITY_EDITOR
        if (finalPhoneVoiceClip == null)
        {
            finalPhoneVoiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultFinalPhoneVoicePath);
            if (finalPhoneVoiceClip != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    private void ResolveReferences()
    {
        ui ??= PhonePuzzleUI.EnsureExists();

        if (puzzle5MorgueController == null)
        {
            puzzle5MorgueController = FindFirstObjectByType<Puzzle5MorgueController>(FindObjectsInactive.Include);
        }

        if (finalDoorKeypad == null)
        {
            finalDoorKeypad = FindFirstObjectByType<FinalDoorKeypad>(FindObjectsInactive.Include);
        }

        if (callSequenceManager == null)
        {
            callSequenceManager = GetComponent<PhoneCallSequenceManager>() ?? FindFirstObjectByType<PhoneCallSequenceManager>(FindObjectsInactive.Include);
        }

        if (phoneAudioSource == null)
        {
            phoneAudioSource = GetComponent<AudioSource>();
        }
    }

    private void Subscribe()
    {
        if (subscribed || puzzle5MorgueController == null)
        {
            return;
        }

        puzzle5MorgueController.Puzzle5Completed += HandlePuzzle5Completed;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || puzzle5MorgueController == null)
        {
            return;
        }

        puzzle5MorgueController.Puzzle5Completed -= HandlePuzzle5Completed;
        subscribed = false;
    }

    private static PhoneCallData.SubtitleLine[] CreateDefaultFinalPhoneLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "Now you have all of him.", duration = 3.2f },
            new PhoneCallData.SubtitleLine { text = "The hour, the scream, the tool, the room.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "Go to the door.", duration = 2.6f },
            new PhoneCallData.SubtitleLine { text = "Feed it the number they made from him.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "If it opens, run.", duration = 2.8f },
            new PhoneCallData.SubtitleLine { text = "If it does not... he knows you tried.", duration = 4.2f },
        };
    }
}

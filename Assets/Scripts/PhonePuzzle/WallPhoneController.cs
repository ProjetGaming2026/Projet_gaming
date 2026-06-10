using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WallPhoneController : Interactable
{
    private const float MinimumFirstRingDelay = 6f;
    private const string Puzzle5SceneName = "demoScene_Difficile";

    private enum PhoneState
    {
        Silent,
        Ringing,
        InCall,
        PuzzleActive,
        Answering,
        WaitingForSecondCall,
        RingingSecondCall,
        InSecondCall,
        Puzzle2Active,
        WaitingForThirdCall,
        RingingThirdCall,
        InThirdCall,
        Puzzle3Active,
        Puzzle3Answering,
        WaitingForFourthCall,
        RingingFourthCall,
        InFourthCall,
        Puzzle4Active,
        Puzzle4Answering,
        WaitingForFifthCall,
        RingingFifthCall,
        InFifthCall,
        Puzzle5Active,
        Puzzle5Answering,
        NextCallUnlocked,
        Completed,
        Failed
    }

    [Header("Scene References")]
    [SerializeField] private Transform handset;
    [SerializeField] private WaitingRoomPuzzle waitingRoomPuzzle;
    [SerializeField] private HeartRoomPuzzle heartRoomPuzzle;
    [SerializeField] private DoctorReportsPuzzle doctorReportsPuzzle;
    [SerializeField] private OperationOrderPuzzle operationOrderPuzzle;
    [SerializeField] private Puzzle5MorgueController puzzle5MorgueController;
    [SerializeField] private PhoneCallSequenceManager callSequenceManager;
    [SerializeField] private PhoneAnswerInput answerInput;
    [SerializeField] private LightFlickerController lightFlicker;
    [SerializeField] private MonsterActivationController monsterActivation;
    [SerializeField] private PrototypeGameManager prototypeGameManager;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float firstRingDelay = 6f;
    [SerializeField, Min(0f)] private float secondRingDelay = 2f;
    [SerializeField, Min(0f)] private float thirdRingDelay = 2f;
    [SerializeField, Min(0f)] private float fourthRingDelay = 2f;
    [SerializeField, Min(0f)] private float fifthRingDelay = 2f;
    [SerializeField, Min(0.1f)] private float postAnswerMessageDuration = 2.7f;
    [SerializeField] private bool requirePuzzleReadyForAnswer = true;

    [Header("Phone Call")]
    [SerializeField] private PhoneCallData firstPhoneCall;
    [SerializeField] private PhoneCallData secondPhoneCall;
    [SerializeField] private PhoneCallData thirdPhoneCall;
    [SerializeField] private PhoneCallData fourthPhoneCall;
    [SerializeField] private PhoneCallData fifthPhoneCall;
    [SerializeField] private string answerPromptSubtitle = "What is your answer?";
    [SerializeField] private string correctAnswerMessage = "He was not dead. Not yet.";
    [SerializeField] private string wrongAnswerMessage = "That was not him.";
    [SerializeField] private string timeoutMessage = "You were too late.";

    [Header("Audio")]
    [SerializeField] private AudioSource ringingAudioSource;
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip phoneRingingLoop;
    [SerializeField] private AudioClip phonePickupSound;
    [SerializeField] private AudioClip phoneHangupSound;
    [SerializeField] private AudioClip phoneStaticSound;
    [SerializeField] private AudioClip phoneCallVoiceClip;
    [SerializeField] private AudioClip phoneCall2VoiceClip;
    [SerializeField] private AudioClip phoneCall3VoiceClip;
    [SerializeField] private AudioClip phoneCall4VoiceClip;
    [SerializeField] private AudioClip phoneCall5VoiceClip;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip wrongAnswerSound;

    [Header("Events")]
    public UnityEvent OnPhoneStartedRinging = new UnityEvent();
    public UnityEvent OnPhoneAnswered = new UnityEvent();
    public UnityEvent OnPhoneCallFinished = new UnityEvent();
    public UnityEvent OnPuzzleActivated = new UnityEvent();
    public UnityEvent OnPuzzleCompleted = new UnityEvent();
    public UnityEvent OnWrongAnswer = new UnityEvent();
    public UnityEvent OnTimerExpired = new UnityEvent();
    public UnityEvent OnMonsterTriggered = new UnityEvent();

    private PhoneState state = PhoneState.Silent;
    private PhonePuzzleUI ui;
    private int wrongAttemptCount;
    private bool monsterTriggered;
    private bool subscribedToPuzzleEvents;
    private bool subscribedToPuzzle2Events;
    private bool subscribedToPuzzle3Events;
    private bool subscribedToPuzzle4Events;
    private bool subscribedToPuzzle5Events;
    private bool nextPhoneCallUnlocked;
    private bool fourthCallDelayStarted;
    private bool fifthCallDelayStarted;
    private bool puzzle1TaskRegistered;
    private bool puzzle2TaskRegistered;
    private bool puzzle3TaskRegistered;
    private bool puzzle4TaskRegistered;
    private bool puzzle5TaskRegistered;

    public bool IsNextPhoneCallUnlocked => nextPhoneCallUnlocked;

#if UNITY_EDITOR
    private const string DefaultRingingClipPath = "Assets/audio/ringing_voice.mp3";
    private const string DefaultPuzzleOneSpeechClipPath = "Assets/audio/puzzle1_Speech.mp3";
    private const string DefaultPuzzleTwoSpeechClipPath = "Assets/audio/Puzzle 2 voice.mp3";
    private const string DefaultPuzzleThreeSpeechClipPath = "Assets/audio/Puzzle 3 voice.mp3";
    private const string DefaultPuzzleFourSpeechClipPath = "Assets/audio/Puzzle 4 voice.mp3";
    private const string DefaultPuzzleFiveSpeechClipPath = "Assets/audio/Puzzle 5 voice.mp3";
    private const string DefaultCorrectAnswerClipPath = "Assets/audio/He was not dead, not yet...mp3";
#endif

    private void Awake()
    {
        firstRingDelay = Mathf.Max(firstRingDelay, MinimumFirstRingDelay);
        TryAssignEditorDefaultAudio();
        ui = PhonePuzzleUI.EnsureExists();
        ResolveReferences();
        ConfigureAudioSources();
        SetInteractable(true);
    }

    private void OnValidate()
    {
        firstRingDelay = Mathf.Max(firstRingDelay, MinimumFirstRingDelay);
        TryAssignEditorDefaultAudio();
    }

    private void Start()
    {
        if (waitingRoomPuzzle != null)
        {
            SubscribeToPuzzleEvents();
        }
        else
        {
            Debug.LogWarning("WallPhoneController has no WaitingRoomPuzzle assigned. Puzzle 1 will not activate.");
        }

        if (heartRoomPuzzle != null)
        {
            SubscribeToPuzzle2Events();
        }

        if (doctorReportsPuzzle != null)
        {
            SubscribeToPuzzle3Events();
        }

        if (operationOrderPuzzle != null)
        {
            SubscribeToPuzzle4Events();
        }

        if (IsPuzzle5Scene() && puzzle5MorgueController != null)
        {
            SubscribeToPuzzle5Events();
        }

        StartCoroutine(StartFirstRingAfterDelay());
    }

    private void OnDestroy()
    {
        if (waitingRoomPuzzle != null)
        {
            UnsubscribeFromPuzzleEvents();
        }

        if (heartRoomPuzzle != null)
        {
            UnsubscribeFromPuzzle2Events();
        }

        if (doctorReportsPuzzle != null)
        {
            UnsubscribeFromPuzzle3Events();
        }

        if (operationOrderPuzzle != null)
        {
            UnsubscribeFromPuzzle4Events();
        }

        if (puzzle5MorgueController != null)
        {
            UnsubscribeFromPuzzle5Events();
        }
    }

    public override string GetInteractPrompt()
    {
        switch (state)
        {
            case PhoneState.Ringing:
            case PhoneState.RingingSecondCall:
            case PhoneState.RingingThirdCall:
            case PhoneState.RingingFourthCall:
            case PhoneState.RingingFifthCall:
                return "Press E to answer";
            case PhoneState.PuzzleActive:
                return IsReadyForAnswer() ? "Press E to answer" : "Inspect the waiting room lists";
            case PhoneState.Answering:
            case PhoneState.Puzzle3Answering:
            case PhoneState.Puzzle4Answering:
            case PhoneState.Puzzle5Answering:
                return "Enter the answer";
            case PhoneState.WaitingForSecondCall:
                return "Return to the phone.";
            case PhoneState.Puzzle2Active:
                return "Find the source of the beating.";
            case PhoneState.WaitingForThirdCall:
                return "Return to the phone.";
            case PhoneState.Puzzle3Active:
                return IsPuzzle3ReadyForAnswer() ? "Press E to answer" : "Search Doctor Marek's reports.";
            case PhoneState.WaitingForFourthCall:
                return "Return to the phone.";
            case PhoneState.Puzzle4Active:
                return IsPuzzle4ReadyForAnswer() ? "Press E to answer" : "Wake the six mouths correctly.";
            case PhoneState.WaitingForFifthCall:
                return "Return to the phone.";
            case PhoneState.Puzzle5Active:
                return IsPuzzle5ReadyForAnswer() ? "Press E to answer" : "Open only the bones of the first lie.";
            case PhoneState.NextCallUnlocked:
                return "The next call is not ready yet.";
            default:
                return string.Empty;
        }
    }

    public override bool CanInteract =>
        state == PhoneState.Ringing ||
        state == PhoneState.PuzzleActive ||
        state == PhoneState.RingingSecondCall ||
        state == PhoneState.RingingThirdCall ||
        state == PhoneState.Puzzle3Active ||
        state == PhoneState.RingingFourthCall ||
        state == PhoneState.Puzzle4Active ||
        state == PhoneState.RingingFifthCall ||
        state == PhoneState.Puzzle5Active;

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        if (state == PhoneState.Ringing)
        {
            AnswerRingingPhone();
            return;
        }

        if (state == PhoneState.RingingSecondCall)
        {
            AnswerSecondRingingPhone();
            return;
        }

        if (state == PhoneState.RingingThirdCall)
        {
            AnswerThirdRingingPhone();
            return;
        }

        if (state == PhoneState.RingingFourthCall)
        {
            AnswerFourthRingingPhone();
            return;
        }

        if (state == PhoneState.RingingFifthCall)
        {
            AnswerFifthRingingPhone();
            return;
        }

        if (state == PhoneState.PuzzleActive)
        {
            TryBeginAnswerMode();
        }

        if (state == PhoneState.Puzzle3Active)
        {
            TryBeginPuzzle3AnswerMode();
        }

        if (state == PhoneState.Puzzle4Active)
        {
            TryBeginPuzzle4AnswerMode();
        }

        if (state == PhoneState.Puzzle5Active)
        {
            TryBeginPuzzle5AnswerMode();
        }
    }

    public void ConfigureSceneReferences(WaitingRoomPuzzle puzzle, LightFlickerController flicker, MonsterActivationController monster)
    {
        if (puzzle != null)
        {
            UnsubscribeFromPuzzleEvents();
            waitingRoomPuzzle = puzzle;
            waitingRoomPuzzle.ConfigurePhoneAnchor(transform);
            SubscribeToPuzzleEvents();
        }

        if (flicker != null)
        {
            lightFlicker = flicker;
        }

        if (monster != null)
        {
            monsterActivation = monster;
        }
    }

    public void ConfigurePuzzle2Reference(HeartRoomPuzzle puzzle2)
    {
        if (puzzle2 == null)
        {
            return;
        }

        UnsubscribeFromPuzzle2Events();
        heartRoomPuzzle = puzzle2;
        SubscribeToPuzzle2Events();
    }

    public void ConfigurePuzzle3Reference(DoctorReportsPuzzle puzzle3)
    {
        if (puzzle3 == null)
        {
            return;
        }

        UnsubscribeFromPuzzle3Events();
        doctorReportsPuzzle = puzzle3;
        SubscribeToPuzzle3Events();
    }

    public void ConfigurePuzzle4Reference(OperationOrderPuzzle puzzle4)
    {
        if (puzzle4 == null)
        {
            return;
        }

        UnsubscribeFromPuzzle4Events();
        operationOrderPuzzle = puzzle4;
        SubscribeToPuzzle4Events();
    }

    public void ConfigurePuzzle5Reference(Puzzle5MorgueController puzzle5)
    {
        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (puzzle5 == null)
        {
            return;
        }

        UnsubscribeFromPuzzle5Events();
        puzzle5MorgueController = puzzle5;
        SubscribeToPuzzle5Events();
    }

    public void UnlockCall5AfterPuzzle4()
    {
        PrepareFifthCallAfterPuzzle4();
    }

    public void StartRinging()
    {
        if (state != PhoneState.Silent)
        {
            return;
        }

        state = PhoneState.Ringing;

        if (ringingAudioSource != null && phoneRingingLoop != null)
        {
            ringingAudioSource.clip = phoneRingingLoop;
            ringingAudioSource.loop = true;
            ringingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Phone started ringing without a phoneRingingLoop clip. Assign it later in the Inspector.");
        }

        OnPhoneStartedRinging.Invoke();
        Debug.Log("Wall phone started ringing.");
    }

    public void StartSecondRinging()
    {
        if (state != PhoneState.WaitingForSecondCall)
        {
            return;
        }

        state = PhoneState.RingingSecondCall;

        if (ringingAudioSource != null && phoneRingingLoop != null)
        {
            ringingAudioSource.clip = phoneRingingLoop;
            ringingAudioSource.loop = true;
            ringingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Phone Call 2 started ringing without a phoneRingingLoop clip. Assign it later in the Inspector.");
        }

        OnPhoneStartedRinging.Invoke();
        Debug.Log("Wall phone started ringing for Call 2.");
    }

    public void StartThirdRinging()
    {
        if (state != PhoneState.WaitingForThirdCall)
        {
            return;
        }

        state = PhoneState.RingingThirdCall;

        if (ringingAudioSource != null && phoneRingingLoop != null)
        {
            ringingAudioSource.clip = phoneRingingLoop;
            ringingAudioSource.loop = true;
            ringingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Phone Call 3 started ringing without a phoneRingingLoop clip. Assign it later in the Inspector.");
        }

        OnPhoneStartedRinging.Invoke();
        Debug.Log("Wall phone started ringing for Call 3.");
    }

    public void StartFourthRinging()
    {
        if (state != PhoneState.WaitingForFourthCall)
        {
            return;
        }

        state = PhoneState.RingingFourthCall;
        fourthCallDelayStarted = false;

        if (ringingAudioSource != null && phoneRingingLoop != null)
        {
            ringingAudioSource.clip = phoneRingingLoop;
            ringingAudioSource.loop = true;
            ringingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Phone Call 4 started ringing without a phoneRingingLoop clip. Assign it later in the Inspector.");
        }

        OnPhoneStartedRinging.Invoke();
        Debug.Log("Wall phone started ringing for Call 4.");
    }

    public void StartFifthRinging()
    {
        if (state != PhoneState.WaitingForFifthCall)
        {
            return;
        }

        state = PhoneState.RingingFifthCall;
        fifthCallDelayStarted = false;

        if (ringingAudioSource != null && phoneRingingLoop != null)
        {
            ringingAudioSource.clip = phoneRingingLoop;
            ringingAudioSource.loop = true;
            ringingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Phone Call 5 started ringing without a phoneRingingLoop clip. Assign it later in the Inspector.");
        }

        OnPhoneStartedRinging.Invoke();
        Debug.Log("Wall phone started ringing for Call 5.");
    }

    private IEnumerator StartFirstRingAfterDelay()
    {
        float delay = Mathf.Max(firstRingDelay, MinimumFirstRingDelay);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        StartRinging();
    }

    private IEnumerator StartSecondRingAfterDelay()
    {
        float delay = postAnswerMessageDuration + secondRingDelay;
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartSecondRinging();
    }

    private IEnumerator StartThirdRingAfterDelay()
    {
        float delay = Mathf.Max(0f, thirdRingDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartThirdRinging();
    }

    private IEnumerator StartFourthRingAfterDelay()
    {
        float delay = postAnswerMessageDuration + Mathf.Max(0f, fourthRingDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartFourthRinging();
    }

    private IEnumerator StartFifthRingAfterDelay()
    {
        float delay = postAnswerMessageDuration + Mathf.Max(0f, fifthRingDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartFifthRinging();
    }

    private void AnswerRingingPhone()
    {
        state = PhoneState.InCall;

        if (ringingAudioSource != null)
        {
            ringingAudioSource.Stop();
        }

        PlayOneShot(phonePickupSound);
        PlayOneShot(phoneStaticSound);
        OnPhoneAnswered.Invoke();
        Debug.Log("Wall phone answered.");

        callSequenceManager.PlayCall(firstPhoneCall, phoneAudioSource, phoneCallVoiceClip, HandleFirstCallFinished);
    }

    private void AnswerSecondRingingPhone()
    {
        state = PhoneState.InSecondCall;

        if (ringingAudioSource != null)
        {
            ringingAudioSource.Stop();
        }

        PlayOneShot(phonePickupSound);
        PlayOneShot(phoneStaticSound);
        OnPhoneAnswered.Invoke();
        Debug.Log("Wall phone answered for Call 2.");

        callSequenceManager.PlayCall(secondPhoneCall, phoneAudioSource, phoneCall2VoiceClip, HandleSecondCallFinished, CreateDefaultPuzzleTwoLines());
    }

    private void AnswerThirdRingingPhone()
    {
        state = PhoneState.InThirdCall;

        if (ringingAudioSource != null)
        {
            ringingAudioSource.Stop();
        }

        PlayOneShot(phonePickupSound);
        PlayOneShot(phoneStaticSound);
        OnPhoneAnswered.Invoke();
        Debug.Log("Wall phone answered for Call 3.");

        callSequenceManager.PlayCall(thirdPhoneCall, phoneAudioSource, phoneCall3VoiceClip, HandleThirdCallFinished, CreateDefaultPuzzleThreeLines());
    }

    private void AnswerFourthRingingPhone()
    {
        state = PhoneState.InFourthCall;

        if (ringingAudioSource != null)
        {
            ringingAudioSource.Stop();
        }

        PlayOneShot(phonePickupSound);
        PlayOneShot(phoneStaticSound);
        OnPhoneAnswered.Invoke();
        Debug.Log("Wall phone answered for Call 4.");

        callSequenceManager.PlayCall(fourthPhoneCall, phoneAudioSource, phoneCall4VoiceClip, HandleFourthCallFinished, CreateDefaultPuzzleFourLines());
    }

    private void AnswerFifthRingingPhone()
    {
        state = PhoneState.InFifthCall;

        if (ringingAudioSource != null)
        {
            ringingAudioSource.Stop();
        }

        PlayOneShot(phonePickupSound);
        PlayOneShot(phoneStaticSound);
        OnPhoneAnswered.Invoke();
        Debug.Log("Wall phone answered for Call 5.");

        callSequenceManager.PlayCall(fifthPhoneCall, phoneAudioSource, ResolveCall5VoiceClip(), HandleFifthCallFinished, CreateDefaultPuzzleFiveLines());
    }

    private void HandleFirstCallFinished()
    {
        OnPhoneCallFinished.Invoke();
        Debug.Log("First phone call finished.");

        if (waitingRoomPuzzle == null)
        {
            Debug.LogWarning("Cannot activate Puzzle 1 because WaitingRoomPuzzle is missing.");
            return;
        }

        state = PhoneState.PuzzleActive;
        waitingRoomPuzzle.ActivatePuzzle();
        OnPuzzleActivated.Invoke();
    }

    private void HandleSecondCallFinished()
    {
        OnPhoneCallFinished.Invoke();
        Debug.Log("Phone Call 2 finished.");

        if (heartRoomPuzzle == null)
        {
            ResolveHeartRoomPuzzle();
        }

        if (heartRoomPuzzle == null)
        {
            Debug.LogWarning("Cannot activate Puzzle 2 because HeartRoomPuzzle/Puzzle2_room is missing.");
            return;
        }

        state = PhoneState.Puzzle2Active;
        heartRoomPuzzle.ActivatePuzzle2();
    }

    private void HandleThirdCallFinished()
    {
        OnPhoneCallFinished.Invoke();
        Debug.Log("Phone Call 3 finished.");

        if (doctorReportsPuzzle == null)
        {
            ResolveDoctorReportsPuzzle();
        }

        if (doctorReportsPuzzle == null)
        {
            Debug.LogWarning("Cannot activate Puzzle 3 because DoctorReportsPuzzle/Puzzle3_room is missing.");
            return;
        }

        state = PhoneState.Puzzle3Active;
        doctorReportsPuzzle.ActivatePuzzle3();
    }

    private void HandleFourthCallFinished()
    {
        OnPhoneCallFinished.Invoke();
        Debug.Log("Phone Call 4 finished.");

        if (operationOrderPuzzle == null)
        {
            ResolveOperationOrderPuzzle();
        }

        if (operationOrderPuzzle == null)
        {
            Debug.LogWarning("Cannot activate Puzzle 4 because OperationOrderPuzzle/Puzzle4_OperationOrder/All_Instruments is missing.");
            return;
        }

        state = PhoneState.Puzzle4Active;
        operationOrderPuzzle.ActivatePuzzle4();
    }

    private void HandleFifthCallFinished()
    {
        if (!IsPuzzle5Scene())
        {
            state = PhoneState.NextCallUnlocked;
            Debug.Log("Phone Call 5 ignored because Puzzle 5 is only enabled in demoScene_Difficile.");
            return;
        }

        OnPhoneCallFinished.Invoke();
        Debug.Log("Call 5 finished.");

        if (puzzle5MorgueController == null)
        {
            ResolvePuzzle5MorgueController();
        }

        if (puzzle5MorgueController == null)
        {
            Debug.LogWarning("Cannot activate Puzzle 5 because Puzzle5MorgueController/morgue/slots is missing.");
            return;
        }

        state = PhoneState.Puzzle5Active;
        puzzle5MorgueController.ActivatePuzzle5();
    }

    private void TryBeginAnswerMode()
    {
        if (!IsReadyForAnswer())
        {
            ui.Subtitles.ShowTimed("Find the erased patient.", 2.5f);
            return;
        }

        state = PhoneState.Answering;
        ui.Subtitles.ShowTimed(answerPromptSubtitle, 2.2f);

        if (answerInput == null)
        {
            answerInput = gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(3, HandleAnswerSubmitted, HandleAnswerCancelled);
        Debug.Log("Phone answer mode started.");
    }

    private void HandleAnswerSubmitted(string answer)
    {
        if (waitingRoomPuzzle == null)
        {
            Debug.LogWarning("No WaitingRoomPuzzle assigned; cannot validate answer.");
            return;
        }

        if (answer == waitingRoomPuzzle.CorrectAnswer)
        {
            HandleCorrectAnswer();
            return;
        }

        HandleWrongAnswer();
    }

    private void TryBeginPuzzle3AnswerMode()
    {
        if (doctorReportsPuzzle == null)
        {
            ResolveDoctorReportsPuzzle();
        }

        if (!IsPuzzle3ReadyForAnswer())
        {
            ui.Subtitles.ShowTimed("Search Doctor Marek's reports.", 2.5f);
            return;
        }

        state = PhoneState.Puzzle3Answering;
        ui.Subtitles.ShowTimed(doctorReportsPuzzle.AnswerPrompt, 2.2f);

        if (answerInput == null)
        {
            answerInput = gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(doctorReportsPuzzle.MaxAnswerLength, HandlePuzzle3AnswerSubmitted, HandleAnswerCancelled);
        Debug.Log("Puzzle 3 phone answer mode started.");
    }

    private void HandlePuzzle3AnswerSubmitted(string answer)
    {
        if (doctorReportsPuzzle == null)
        {
            Debug.LogWarning("No DoctorReportsPuzzle assigned; cannot validate Puzzle 3 answer.");
            return;
        }

        bool correct = doctorReportsPuzzle.SubmitPhoneAnswer(answer);
        if (correct)
        {
            answerInput?.EndAnswerMode();
            StartCoroutine(PlayHangupAfterDelay(postAnswerMessageDuration));
            PrepareFourthCallAfterPuzzle3();
            Debug.Log("Puzzle 3 correct phone answer entered.");
            return;
        }

        answerInput?.ClearAnswer();
    }

    private void TryBeginPuzzle4AnswerMode()
    {
        if (operationOrderPuzzle == null)
        {
            ResolveOperationOrderPuzzle();
        }

        if (!IsPuzzle4ReadyForAnswer())
        {
            ui.Subtitles.ShowTimed("The six mouths have not all spoken.", 2.5f);
            return;
        }

        state = PhoneState.Puzzle4Answering;
        ui.Subtitles.ShowTimed(operationOrderPuzzle.AnswerPrompt, 2.4f);

        if (answerInput == null)
        {
            answerInput = gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(operationOrderPuzzle.MaxAnswerLength, HandlePuzzle4AnswerSubmitted, HandleAnswerCancelled);
        Debug.Log("Puzzle 4 phone answer mode started.");
    }

    private void HandlePuzzle4AnswerSubmitted(string answer)
    {
        if (operationOrderPuzzle == null)
        {
            Debug.LogWarning("No OperationOrderPuzzle assigned; cannot validate Puzzle 4 answer.");
            return;
        }

        bool correct = operationOrderPuzzle.SubmitPhoneAnswer(answer);
        if (correct)
        {
            answerInput?.EndAnswerMode();
            StartCoroutine(PlayHangupAfterDelay(postAnswerMessageDuration));
            PrepareFifthCallAfterPuzzle4();
            Debug.Log("Puzzle 4 correct phone answer entered. Puzzle 5 call is unlocked.");
            return;
        }

        answerInput?.ClearAnswer();
    }

    private void TryBeginPuzzle5AnswerMode()
    {
        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (puzzle5MorgueController == null)
        {
            ResolvePuzzle5MorgueController();
        }

        if (!IsPuzzle5ReadyForAnswer())
        {
            ui.Subtitles.ShowTimed("Open only the bones of the first lie.", 2.5f);
            return;
        }

        state = PhoneState.Puzzle5Answering;
        ui.Subtitles.ShowTimed(puzzle5MorgueController.AnswerPrompt, 2.4f);

        if (answerInput == null)
        {
            answerInput = gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(puzzle5MorgueController.MaxAnswerLength, HandlePuzzle5AnswerSubmitted, HandleAnswerCancelled);
        Debug.Log("Puzzle 5 phone answer mode started.");
    }

    private void HandleAnswerCancelled()
    {
        switch (state)
        {
            case PhoneState.Answering:
                state = PhoneState.PuzzleActive;
                break;
            case PhoneState.Puzzle3Answering:
                state = PhoneState.Puzzle3Active;
                break;
            case PhoneState.Puzzle4Answering:
                state = PhoneState.Puzzle4Active;
                break;
            case PhoneState.Puzzle5Answering:
                state = PhoneState.Puzzle5Active;
                break;
        }

        Debug.Log("Phone answer mode cancelled.");
    }

    private void HandlePuzzle5AnswerSubmitted(string answer)
    {
        if (puzzle5MorgueController == null)
        {
            Debug.LogWarning("No Puzzle5MorgueController assigned; cannot validate Puzzle 5 answer.");
            return;
        }

        bool correct = puzzle5MorgueController.SubmitPhoneAnswer(answer);
        if (correct)
        {
            state = PhoneState.Completed;
            answerInput?.EndAnswerMode();
            StartCoroutine(PlayHangupAfterDelay(postAnswerMessageDuration));
            Debug.Log("Puzzle 5 correct phone answer entered.");
            return;
        }

        answerInput?.ClearAnswer();
    }

    private void HandleCorrectAnswer()
    {
        state = PhoneState.WaitingForSecondCall;
        answerInput?.EndAnswerMode();
        PlayOneShot(correctAnswerSound);
        ui.Subtitles.ShowTimed(correctAnswerMessage, postAnswerMessageDuration);
        ui.Objectives.SetObjective("Return to the phone.");
        RegisterPuzzleTaskProgress(ref puzzle1TaskRegistered, "Puzzle 1");
        waitingRoomPuzzle.MarkCompleted();
        OnPuzzleCompleted.Invoke();
        Debug.Log("Puzzle 1 correct answer entered.");
        StartCoroutine(PlayHangupAfterDelay(postAnswerMessageDuration));
        StartCoroutine(StartSecondRingAfterDelay());
    }

    private void HandleWrongAnswer()
    {
        wrongAttemptCount++;
        PuzzleFailureMonsterAlert.Alert(transform);
        answerInput?.ClearAnswer();
        PlayOneShot(wrongAnswerSound);
        ui.Subtitles.ShowTimed(wrongAnswerMessage, postAnswerMessageDuration);
        lightFlicker?.Flicker();
        OnWrongAnswer.Invoke();
        Debug.Log("Puzzle 1 wrong answer. Attempts: " + wrongAttemptCount);

        if (wrongAttemptCount >= 2)
        {
            TriggerMonster();
        }
    }

    private void HandleTimerExpired()
    {
        if (state == PhoneState.Completed)
        {
            return;
        }

        state = PhoneState.Failed;
        answerInput?.EndAnswerMode();
        ui.Subtitles.ShowTimed(timeoutMessage, postAnswerMessageDuration);
        ui.Objectives.SetObjective("Hide.");
        ui.Timer.Hide();
        OnTimerExpired.Invoke();
        TriggerMonster();
        Debug.Log("Puzzle 1 timer expired from phone controller.");
    }

    private void HandlePuzzleCompletedFromPuzzle()
    {
        RegisterPuzzleTaskProgress(ref puzzle1TaskRegistered, "Puzzle 1");

        if (state != PhoneState.WaitingForSecondCall && state != PhoneState.RingingSecondCall && state != PhoneState.InSecondCall && state != PhoneState.Puzzle2Active && state != PhoneState.NextCallUnlocked)
        {
            state = PhoneState.WaitingForSecondCall;
        }
    }

    private bool IsReadyForAnswer()
    {
        if (waitingRoomPuzzle == null)
        {
            return false;
        }

        return !requirePuzzleReadyForAnswer || waitingRoomPuzzle.IsReadyForAnswer;
    }

    private bool IsPuzzle3ReadyForAnswer()
    {
        if (doctorReportsPuzzle == null)
        {
            ResolveDoctorReportsPuzzle();
        }

        return doctorReportsPuzzle != null && doctorReportsPuzzle.CanAnswerAtPhone;
    }

    private bool IsPuzzle4ReadyForAnswer()
    {
        if (operationOrderPuzzle == null)
        {
            ResolveOperationOrderPuzzle();
        }

        return operationOrderPuzzle != null && operationOrderPuzzle.CanAnswerAtPhone;
    }

    private bool IsPuzzle5ReadyForAnswer()
    {
        if (!IsPuzzle5Scene())
        {
            return false;
        }

        if (puzzle5MorgueController == null)
        {
            ResolvePuzzle5MorgueController();
        }

        return puzzle5MorgueController != null && puzzle5MorgueController.CanAnswerAtPhone;
    }

    private void TriggerMonster()
    {
        if (monsterTriggered)
        {
            return;
        }

        monsterTriggered = true;
        monsterActivation?.TriggerMonster();
        OnMonsterTriggered.Invoke();
    }

    private void SubscribeToPuzzleEvents()
    {
        if (waitingRoomPuzzle == null || subscribedToPuzzleEvents)
        {
            return;
        }

        waitingRoomPuzzle.TimerExpired += HandleTimerExpired;
        waitingRoomPuzzle.PuzzleCompleted += HandlePuzzleCompletedFromPuzzle;
        subscribedToPuzzleEvents = true;
    }

    private void UnsubscribeFromPuzzleEvents()
    {
        if (waitingRoomPuzzle == null || !subscribedToPuzzleEvents)
        {
            return;
        }

        waitingRoomPuzzle.TimerExpired -= HandleTimerExpired;
        waitingRoomPuzzle.PuzzleCompleted -= HandlePuzzleCompletedFromPuzzle;
        subscribedToPuzzleEvents = false;
    }

    private void SubscribeToPuzzle2Events()
    {
        if (heartRoomPuzzle == null || subscribedToPuzzle2Events)
        {
            return;
        }

        heartRoomPuzzle.Puzzle2Completed += HandlePuzzle2Completed;
        heartRoomPuzzle.PaperRead += HandlePuzzle2PaperRead;
        heartRoomPuzzle.Puzzle2TimerExpired += HandlePuzzle2TimerExpired;
        subscribedToPuzzle2Events = true;
    }

    private void UnsubscribeFromPuzzle2Events()
    {
        if (heartRoomPuzzle == null || !subscribedToPuzzle2Events)
        {
            return;
        }

        heartRoomPuzzle.Puzzle2Completed -= HandlePuzzle2Completed;
        heartRoomPuzzle.PaperRead -= HandlePuzzle2PaperRead;
        heartRoomPuzzle.Puzzle2TimerExpired -= HandlePuzzle2TimerExpired;
        subscribedToPuzzle2Events = false;
    }

    private void SubscribeToPuzzle3Events()
    {
        if (doctorReportsPuzzle == null || subscribedToPuzzle3Events)
        {
            return;
        }

        doctorReportsPuzzle.Puzzle3Completed += HandlePuzzle3Completed;
        doctorReportsPuzzle.Puzzle3TimerExpired += HandlePuzzle3TimerExpired;
        subscribedToPuzzle3Events = true;
    }

    private void UnsubscribeFromPuzzle3Events()
    {
        if (doctorReportsPuzzle == null || !subscribedToPuzzle3Events)
        {
            return;
        }

        doctorReportsPuzzle.Puzzle3Completed -= HandlePuzzle3Completed;
        doctorReportsPuzzle.Puzzle3TimerExpired -= HandlePuzzle3TimerExpired;
        subscribedToPuzzle3Events = false;
    }

    private void SubscribeToPuzzle4Events()
    {
        if (operationOrderPuzzle == null || subscribedToPuzzle4Events)
        {
            return;
        }

        operationOrderPuzzle.Puzzle4Completed += HandlePuzzle4Completed;
        operationOrderPuzzle.Puzzle4TimerExpired += HandlePuzzle4TimerExpired;
        subscribedToPuzzle4Events = true;
    }

    private void UnsubscribeFromPuzzle4Events()
    {
        if (operationOrderPuzzle == null || !subscribedToPuzzle4Events)
        {
            return;
        }

        operationOrderPuzzle.Puzzle4Completed -= HandlePuzzle4Completed;
        operationOrderPuzzle.Puzzle4TimerExpired -= HandlePuzzle4TimerExpired;
        subscribedToPuzzle4Events = false;
    }

    private void SubscribeToPuzzle5Events()
    {
        if (!IsPuzzle5Scene() || puzzle5MorgueController == null || subscribedToPuzzle5Events)
        {
            return;
        }

        puzzle5MorgueController.Puzzle5Completed += HandlePuzzle5Completed;
        puzzle5MorgueController.Puzzle5TimerExpired += HandlePuzzle5TimerExpired;
        subscribedToPuzzle5Events = true;
    }

    private void UnsubscribeFromPuzzle5Events()
    {
        if (puzzle5MorgueController == null || !subscribedToPuzzle5Events)
        {
            return;
        }

        puzzle5MorgueController.Puzzle5Completed -= HandlePuzzle5Completed;
        puzzle5MorgueController.Puzzle5TimerExpired -= HandlePuzzle5TimerExpired;
        subscribedToPuzzle5Events = false;
    }

    private void HandlePuzzle2Completed()
    {
        RegisterPuzzleTaskProgress(ref puzzle2TaskRegistered, "Puzzle 2");
        Debug.Log("Wall phone received Puzzle 2 completed event.");
    }

    private void HandlePuzzle2PaperRead()
    {
        nextPhoneCallUnlocked = true;
        state = PhoneState.WaitingForThirdCall;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");
        Debug.Log("Phone Call 3 unlocked after Puzzle 2 paper was read.");
        StartCoroutine(StartThirdRingAfterDelay());
    }

    private void HandlePuzzle2TimerExpired()
    {
        state = PhoneState.Failed;
        Debug.Log("Puzzle 2 timer expired from phone controller.");
    }

    private void HandlePuzzle3Completed()
    {
        RegisterPuzzleTaskProgress(ref puzzle3TaskRegistered, "Puzzle 3");
        PrepareFourthCallAfterPuzzle3();
        Debug.Log("Wall phone received Puzzle 3 completed event. Phone Call 4 is unlocked.");
    }

    private void HandlePuzzle3TimerExpired()
    {
        state = PhoneState.Failed;
        answerInput?.EndAnswerMode();
        Debug.Log("Puzzle 3 timer expired from phone controller.");
    }

    private void HandlePuzzle4Completed()
    {
        RegisterPuzzleTaskProgress(ref puzzle4TaskRegistered, "Puzzle 4");
        PrepareFifthCallAfterPuzzle4();
        Debug.Log("Wall phone received Puzzle 4 completed event. Phone Call 5 is unlocked.");
    }

    private void HandlePuzzle4TimerExpired()
    {
        state = PhoneState.Failed;
        answerInput?.EndAnswerMode();
        Debug.Log("Puzzle 4 timer expired from phone controller.");
    }

    private void HandlePuzzle5Completed()
    {
        RegisterPuzzleTaskProgress(ref puzzle5TaskRegistered, "Puzzle 5");
        state = PhoneState.Completed;
        answerInput?.EndAnswerMode();
        Debug.Log("Wall phone received Puzzle 5 completed event.");
    }

    private void HandlePuzzle5TimerExpired()
    {
        state = PhoneState.Failed;
        answerInput?.EndAnswerMode();
        Debug.Log("Puzzle 5 timer expired from phone controller.");
    }

    private void PrepareFourthCallAfterPuzzle3()
    {
        if (state == PhoneState.Failed || state == PhoneState.Completed)
        {
            return;
        }

        if (state != PhoneState.WaitingForFourthCall &&
            state != PhoneState.RingingFourthCall &&
            state != PhoneState.InFourthCall &&
            state != PhoneState.Puzzle4Active &&
            state != PhoneState.Puzzle4Answering)
        {
            state = PhoneState.WaitingForFourthCall;
        }

        nextPhoneCallUnlocked = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");

        if (!fourthCallDelayStarted && state == PhoneState.WaitingForFourthCall)
        {
            fourthCallDelayStarted = true;
            StartCoroutine(StartFourthRingAfterDelay());
        }
    }

    private void PrepareFifthCallAfterPuzzle4()
    {
        if (state == PhoneState.Failed || state == PhoneState.Completed)
        {
            return;
        }

        if (!IsPuzzle5Scene())
        {
            state = PhoneState.NextCallUnlocked;
            nextPhoneCallUnlocked = true;
            Debug.Log("Puzzle 5 is disabled outside demoScene_Difficile.");
            return;
        }

        if (state != PhoneState.WaitingForFifthCall &&
            state != PhoneState.RingingFifthCall &&
            state != PhoneState.InFifthCall &&
            state != PhoneState.Puzzle5Active &&
            state != PhoneState.Puzzle5Answering)
        {
            state = PhoneState.WaitingForFifthCall;
        }

        nextPhoneCallUnlocked = true;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Objectives.SetObjective("Return to the phone.");

        if (puzzle5MorgueController == null)
        {
            ResolvePuzzle5MorgueController();
        }

        puzzle5MorgueController?.UnlockPuzzle5Call();

        if (!fifthCallDelayStarted && state == PhoneState.WaitingForFifthCall)
        {
            fifthCallDelayStarted = true;
            StartCoroutine(StartFifthRingAfterDelay());
        }
    }

    private IEnumerator PlayHangupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayOneShot(phoneHangupSound);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (phoneAudioSource == null)
        {
            phoneAudioSource = gameObject.AddComponent<AudioSource>();
        }

        phoneAudioSource.PlayOneShot(clip);
    }

    private void ConfigureAudioSources()
    {
        if (ringingAudioSource == null)
        {
            ringingAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (phoneAudioSource == null)
        {
            phoneAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ringingAudioSource.playOnAwake = false;
        phoneAudioSource.playOnAwake = false;
    }

    private void TryAssignEditorDefaultAudio()
    {
        PhonePuzzleAudioLibrary library = PhonePuzzleAudioLibrary.Load();
        if (library != null)
        {
            phoneRingingLoop ??= library.PhoneRingingLoop;
            phoneCallVoiceClip ??= library.PhoneCall1Voice;
            phoneCall2VoiceClip ??= library.PhoneCall2Voice;
            phoneCall3VoiceClip ??= library.PhoneCall3Voice;
            phoneCall4VoiceClip ??= library.PhoneCall4Voice;
            phoneCall5VoiceClip ??= library.PhoneCall5Voice;
            correctAnswerSound ??= library.Puzzle1CorrectAnswer;
        }

#if UNITY_EDITOR
        if (phoneRingingLoop == null)
        {
            phoneRingingLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultRingingClipPath);
            if (phoneRingingLoop != null)
            {
                EditorUtility.SetDirty(this);
            }
        }

        if (phoneCallVoiceClip == null)
        {
            phoneCallVoiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultPuzzleOneSpeechClipPath);
            if (phoneCallVoiceClip != null)
            {
                EditorUtility.SetDirty(this);
            }
        }

        TryAssignEditorAudioClip(ref phoneCall2VoiceClip, DefaultPuzzleTwoSpeechClipPath);
        TryAssignEditorAudioClip(ref phoneCall3VoiceClip, DefaultPuzzleThreeSpeechClipPath);
        TryAssignEditorAudioClip(ref phoneCall4VoiceClip, DefaultPuzzleFourSpeechClipPath);
        TryAssignEditorAudioClip(ref phoneCall5VoiceClip, DefaultPuzzleFiveSpeechClipPath);

        if (correctAnswerSound == null)
        {
            correctAnswerSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultCorrectAnswerClipPath);
            if (correctAnswerSound != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void TryAssignEditorAudioClip(ref AudioClip target, string assetPath)
    {
        if (target != null)
        {
            return;
        }

        target = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (target != null)
        {
            EditorUtility.SetDirty(this);
        }
    }
#endif

    private void ResolveReferences()
    {
        if (waitingRoomPuzzle == null)
        {
            GameObject waitingRoomWall = GameObject.Find("WaitingRoomWall");
            if (waitingRoomWall != null)
            {
                waitingRoomPuzzle = waitingRoomWall.GetComponent<WaitingRoomPuzzle>() ?? waitingRoomWall.AddComponent<WaitingRoomPuzzle>();
            }
        }

        ResolveHeartRoomPuzzle();
        ResolveDoctorReportsPuzzle();
        ResolveOperationOrderPuzzle();
        if (IsPuzzle5Scene())
        {
            ResolvePuzzle5MorgueController();
        }

        ResolvePrototypeGameManager();

        if (callSequenceManager == null)
        {
            callSequenceManager = GetComponent<PhoneCallSequenceManager>() ?? gameObject.AddComponent<PhoneCallSequenceManager>();
        }

        if (answerInput == null)
        {
            answerInput = GetComponent<PhoneAnswerInput>() ?? gameObject.AddComponent<PhoneAnswerInput>();
        }

        if (monsterActivation == null)
        {
            monsterActivation = FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        }

        if (lightFlicker == null)
        {
            lightFlicker = FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        }

        if (handset == null)
        {
            Transform possibleHandset = transform.Find("Handset");
            if (possibleHandset != null)
            {
                handset = possibleHandset;
            }
        }

        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.65f, 0.95f, 0.2f);
            box.center = Vector3.zero;
        }
    }

    private void ResolvePrototypeGameManager()
    {
        if (prototypeGameManager != null)
        {
            return;
        }

        prototypeGameManager = FindFirstObjectByType<PrototypeGameManager>(FindObjectsInactive.Include);
    }

    private void RegisterPuzzleTaskProgress(ref bool taskRegistered, string puzzleName)
    {
        if (taskRegistered)
        {
            return;
        }

        if (!IsPuzzle5Scene())
        {
            Debug.Log(puzzleName + " task progress is only counted in demoScene_Difficile.");
            taskRegistered = true;
            return;
        }

        ResolvePrototypeGameManager();
        if (prototypeGameManager == null)
        {
            Debug.LogWarning("WallPhoneController could not register " + puzzleName + " task progress because PrototypeGameManager was not found.");
            return;
        }

        prototypeGameManager.RegisterTaskSuccess();
        taskRegistered = true;
        Debug.Log(puzzleName + " counted as one task in PrototypeGameManager.");
    }

    private void ResolveHeartRoomPuzzle()
    {
        if (heartRoomPuzzle != null)
        {
            return;
        }

        heartRoomPuzzle = FindFirstObjectByType<HeartRoomPuzzle>(FindObjectsInactive.Include);
        if (heartRoomPuzzle != null)
        {
            SubscribeToPuzzle2Events();
            return;
        }

        GameObject puzzleRoom = GameObject.Find("Puzzle2_room");
        if (puzzleRoom != null)
        {
            heartRoomPuzzle = puzzleRoom.GetComponent<HeartRoomPuzzle>() ?? puzzleRoom.AddComponent<HeartRoomPuzzle>();
            SubscribeToPuzzle2Events();
        }
        else
        {
            Debug.LogWarning("WallPhoneController could not find Puzzle2_room. Assign HeartRoomPuzzle in the Inspector for Puzzle 2.");
        }
    }

    private void ResolveDoctorReportsPuzzle()
    {
        if (doctorReportsPuzzle != null)
        {
            SubscribeToPuzzle3Events();
            return;
        }

        doctorReportsPuzzle = FindFirstObjectByType<DoctorReportsPuzzle>(FindObjectsInactive.Include);
        if (doctorReportsPuzzle != null)
        {
            SubscribeToPuzzle3Events();
            return;
        }

        Transform puzzleRoom = FindTransformByExactName("Puzzle3_room");
        if (puzzleRoom != null)
        {
            doctorReportsPuzzle = puzzleRoom.GetComponent<DoctorReportsPuzzle>() ?? puzzleRoom.gameObject.AddComponent<DoctorReportsPuzzle>();
            SubscribeToPuzzle3Events();
        }
        else
        {
            Debug.LogWarning("WallPhoneController could not find Puzzle3_room. Assign DoctorReportsPuzzle in the Inspector for Puzzle 3.");
        }
    }

    private void ResolveOperationOrderPuzzle()
    {
        if (operationOrderPuzzle != null)
        {
            SubscribeToPuzzle4Events();
            return;
        }

        operationOrderPuzzle = FindFirstObjectByType<OperationOrderPuzzle>(FindObjectsInactive.Include);
        if (operationOrderPuzzle != null)
        {
            SubscribeToPuzzle4Events();
            return;
        }

        Transform puzzleRoot = FindTransformByExactName("Puzzle4_OperationOrder");
        if (puzzleRoot == null)
        {
            puzzleRoot = FindTransformByExactName("All_Instruments");
        }

        if (puzzleRoot != null)
        {
            operationOrderPuzzle = puzzleRoot.GetComponent<OperationOrderPuzzle>() ?? puzzleRoot.gameObject.AddComponent<OperationOrderPuzzle>();
            SubscribeToPuzzle4Events();
        }
        else
        {
            Debug.LogWarning("WallPhoneController could not find Puzzle4_OperationOrder or All_Instruments. Assign OperationOrderPuzzle in the Inspector for Puzzle 4.");
        }
    }

    private void ResolvePuzzle5MorgueController()
    {
        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (puzzle5MorgueController != null)
        {
            SubscribeToPuzzle5Events();
            return;
        }

        puzzle5MorgueController = FindFirstObjectByType<Puzzle5MorgueController>(FindObjectsInactive.Include);
        if (puzzle5MorgueController != null)
        {
            SubscribeToPuzzle5Events();
            return;
        }

        Transform puzzleRoot = FindTransformByExactName("Puzzle5_MorgueCabinets");
        if (puzzleRoot == null)
        {
            puzzleRoot = FindTransformByExactName("morgue");
        }

        if (puzzleRoot == null)
        {
            puzzleRoot = FindTransformByExactName("slots");
        }

        if (puzzleRoot != null)
        {
            puzzle5MorgueController = puzzleRoot.GetComponent<Puzzle5MorgueController>() ?? puzzleRoot.gameObject.AddComponent<Puzzle5MorgueController>();
            SubscribeToPuzzle5Events();
        }
        else
        {
            Debug.LogWarning("WallPhoneController could not find Puzzle5_MorgueCabinets, morgue, or slots. Assign Puzzle5MorgueController in the Inspector for Puzzle 5.");
        }
    }

    private AudioClip ResolveCall5VoiceClip()
    {
        if (!IsPuzzle5Scene())
        {
            return null;
        }

        if (phoneCall5VoiceClip != null)
        {
            return phoneCall5VoiceClip;
        }

        if (puzzle5MorgueController == null)
        {
            ResolvePuzzle5MorgueController();
        }

        return puzzle5MorgueController != null ? puzzle5MorgueController.Call5VoiceClip : null;
    }

    private static bool IsPuzzle5Scene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == Puzzle5SceneName;
    }

    private static Transform FindTransformByExactName(string objectName)
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

    private static PhoneCallData.SubtitleLine[] CreateDefaultPuzzleTwoLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "He did not speak through his mouth.", duration = 3.4f },
            new PhoneCallData.SubtitleLine { text = "He spoke through what was still beating inside him.", duration = 4.2f },
            new PhoneCallData.SubtitleLine { text = "Do not count the beats.", duration = 3.1f },
            new PhoneCallData.SubtitleLine { text = "Count the pauses between life and death.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "You have three minutes.", duration = 3f },
        };
    }

    private static PhoneCallData.SubtitleLine[] CreateDefaultPuzzleThreeLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "Marek wrote three lies and hid one truth between them.", duration = 4.2f },
            new PhoneCallData.SubtitleLine { text = "The first report ended before the body did.", duration = 3.8f },
            new PhoneCallData.SubtitleLine { text = "The second report remembers what never happened.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "The third did not describe the patient. It described the night.", duration = 4.5f },
            new PhoneCallData.SubtitleLine { text = "The hour is not beside death, and not inside memory.", duration = 4.2f },
            new PhoneCallData.SubtitleLine { text = "Find the number left when both lies are removed.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "Four minutes.", duration = 2.4f },
        };
    }

    private static PhoneCallData.SubtitleLine[] CreateDefaultPuzzleFourLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "Marek left six mouths on the table.", duration = 3.6f },
            new PhoneCallData.SubtitleLine { text = "Each one speaks only after being touched.", duration = 3.7f },
            new PhoneCallData.SubtitleLine { text = "But mouths lie when they wake before their turn.", duration = 4.2f },
            new PhoneCallData.SubtitleLine { text = "One page still remembers who screamed first.", duration = 4.1f },
            new PhoneCallData.SubtitleLine { text = "Do not ask the metal what happened.", duration = 3.5f },
            new PhoneCallData.SubtitleLine { text = "Ask what made the metal answer.", duration = 3.5f },
            new PhoneCallData.SubtitleLine { text = "The last mouth heard life where Marek wanted silence.", duration = 4.5f },
            new PhoneCallData.SubtitleLine { text = "Ten minutes.", duration = 2.4f },
        };
    }

    private static PhoneCallData.SubtitleLine[] CreateDefaultPuzzleFiveLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "The cold room keeps pieces, not bodies.", duration = 3.8f },
            new PhoneCallData.SubtitleLine { text = "Do not begin with the hour.", duration = 3.1f },
            new PhoneCallData.SubtitleLine { text = "Do not begin with the tool.", duration = 3.1f },
            new PhoneCallData.SubtitleLine { text = "Begin with the first impossibility.", duration = 3.6f },
            new PhoneCallData.SubtitleLine { text = "The one who was received, buried, and still given a bed.", duration = 5f },
            new PhoneCallData.SubtitleLine { text = "Open only the bones of that lie.", duration = 3.8f },
            new PhoneCallData.SubtitleLine { text = "If the pieces are his, they will draw where he ended.", duration = 4.8f },
            new PhoneCallData.SubtitleLine { text = "Ten minutes.", duration = 2.4f },
        };
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PatientPuzzleController : MonoBehaviour, IInteractable
{
    private enum Speaker
    {
        Player,
        Patient,
        System
    }

    public enum MessageTone
    {
        Info,
        Success,
        Warning,
        Horror
    }

    [System.Serializable]
    private class DialogueLine
    {
        public Speaker speaker = Speaker.Patient;
        [TextArea(2, 4)] public string line;
        public MessageTone tone = MessageTone.Info;
        public bool playCoughDuringLine;
    }

    [System.Serializable]
    private class PatientRequest
    {
        public string requiredItemId = "blanket";
        [TextArea(2, 4)] public string requestLine = "I am so cold... find something to cover me.";
        [TextArea(2, 4)] public string repeatRequestLine = "Find me a blanket.";
        [TextArea(2, 4)] public string successLine = "Yes... keep it on me. Now I can still talk.";
        public Transform placementPoint;
        public Vector3 placementLocalPosition;
        public Vector3 placementLocalEulerAngles;
        public Vector3 placementLocalScale = Vector3.one;
        public GameObject visualAfterUse;
        public bool useVisualAfterUseInsteadOfMovingItem = true;
        public bool playCoughAfterSuccess;
    }

    [Header("Opening Dialogue")]
    [SerializeField] private DialogueLine[] openingDialogue =
    {
        new DialogueLine
        {
            speaker = Speaker.Player,
            line = "Who are you? What happened here?",
            tone = MessageTone.Info
        },
        new DialogueLine
        {
            speaker = Speaker.Patient,
            line = "Lower your voice... it listens through the vents.",
            tone = MessageTone.Horror
        },
        new DialogueLine
        {
            speaker = Speaker.Player,
            line = "What is listening?",
            tone = MessageTone.Info
        },
        new DialogueLine
        {
            speaker = Speaker.Patient,
            line = "They opened me yesterday... and put something back inside that was still breathing.",
            tone = MessageTone.Horror,
            playCoughDuringLine = true
        },
        new DialogueLine
        {
            speaker = Speaker.Patient,
            line = "Do not look under the bed. If you see it, pretend you did not.",
            tone = MessageTone.Horror
        }
    };

    [Header("Requests")]
    [SerializeField] private PatientRequest[] requests =
    {
        new PatientRequest
        {
            requiredItemId = "blanket",
            requestLine = "I am so cold... my hands are going numb. Find me a blanket.",
            repeatRequestLine = "Find me a blanket.",
            successLine = "Yes... keep it on me. Now I can still talk."
        },
        new PatientRequest
        {
            requiredItemId = "oxygen",
            requestLine = "I still cannot breathe... find the oxygen mask.",
            repeatRequestLine = "Find the oxygen mask.",
            successLine = "Yes... air... I remember the code now.",
            playCoughAfterSuccess = true
        }
    };

    [Header("End")]
    [TextArea(2, 4)]
    [SerializeField] private string finalLine = "The code... 0417. Leave before it comes back.";
    [TextArea(2, 4)]
    [SerializeField] private string solvedRepeatLine = "The code is 0417... leave me alone. She will kill both of us.";
    [SerializeField] private bool countAsSolvedTask = true;
    [SerializeField] private UnityEvent onPuzzleSolved;

    [Header("Message UI")]
    [SerializeField] private float messageDuration = 4f;
    [SerializeField] private bool patientDialogueWaitsForNextInteraction = true;

    [Header("Audio")]
    [SerializeField] private AudioClip firstInteractionCough;
    [Range(0f, 1f)]
    [SerializeField] private float firstInteractionCoughVolume = 1f;
    [SerializeField] private AudioClip requestCough;
    [Range(0f, 1f)]
    [SerializeField] private float requestCoughVolume = 1f;
    [SerializeField] private bool enableDialogueCoughMarkers = true;
    [SerializeField] private float dialogueCoughDelay = 1.1f;

    private AudioSource audioSource;
    private Coroutine dialogueCoughRoutine;

    private static string activeSpeakerName;
    private static string activeMessage;
    private static MessageTone activeTone;
    private static float activeMessageUntil;
    private static bool activeMessageNeedsInput;
    private static string toastMessage;
    private static float toastMessageUntil;

    private int dialogueIndex;
    private int requestIndex;
    private bool currentRequestHasBeenAsked;
    private bool finalLineReady;
    private bool puzzleSolved;
    private bool taskRegistered;
    private bool firstInteractionCoughPlayed;
    private bool dialogueCoughPlayed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    public void Interact()
    {
        PlayFirstInteractionCough();

        if (puzzleSolved)
        {
            SpeakTimed(Speaker.Patient, solvedRepeatLine, MessageTone.Warning);
            return;
        }

        PatientPuzzlePickup heldItem = PatientPuzzlePickup.CurrentlyHeld;
        if (heldItem != null && PatientPuzzlePickup.LastInteractionFrame != Time.frameCount)
        {
            TryUseHeldItem(heldItem);
            return;
        }

        SpeakNextLine();
    }

    public string GetInteractPrompt()
    {
        if (puzzleSolved)
        {
            return "Press E to talk to the patient";
        }

        if (PatientPuzzlePickup.CurrentlyHeld != null)
        {
            return "Press E to help the patient";
        }

        return "Press E to talk to the patient";
    }

    public static void ShowMessage(string message, MessageTone tone)
    {
        toastMessage = message;
        toastMessageUntil = Time.unscaledTime + 2.4f;
    }

    private void SpeakNextLine()
    {
        if (dialogueIndex < openingDialogue.Length)
        {
            DialogueLine dialogueLine = openingDialogue[dialogueIndex];
            Speak(dialogueLine.speaker, dialogueLine.line, dialogueLine.tone);
            if (dialogueLine.playCoughDuringLine)
            {
                ScheduleDialogueCough();
            }

            dialogueIndex++;
            return;
        }

        if (finalLineReady)
        {
            CompletePuzzle();
            return;
        }

        PatientRequest request = GetCurrentRequest();
        if (request != null)
        {
            string line = currentRequestHasBeenAsked && !string.IsNullOrWhiteSpace(request.repeatRequestLine)
                ? request.repeatRequestLine
                : request.requestLine;

            currentRequestHasBeenAsked = true;
            SpeakTimed(Speaker.Patient, line, MessageTone.Horror);
            return;
        }

        CompletePuzzle();
    }

    private void TryUseHeldItem(PatientPuzzlePickup heldItem)
    {
        PatientRequest request = GetCurrentRequest();
        if (request == null)
        {
            SpeakTimed(Speaker.Patient, "I do not need that anymore... listen to the code.", MessageTone.Info);
            return;
        }

        if (!string.Equals(heldItem.ItemId, request.requiredItemId, System.StringComparison.OrdinalIgnoreCase))
        {
            SpeakTimed(Speaker.Patient, "No... not that. Listen to me carefully.", MessageTone.Warning);
            return;
        }

        GameObject activatedVisual = null;
        bool hasSeparateVisual = request.visualAfterUse != null && request.visualAfterUse != heldItem.gameObject;
        Transform placementTarget = request.placementPoint != null ? request.placementPoint : transform;

        if (hasSeparateVisual)
        {
            activatedVisual = ActivateStaticVisual(request.visualAfterUse, placementTarget);
        }

        if (activatedVisual != null && request.useVisualAfterUseInsteadOfMovingItem)
        {
            heldItem.ConsumeAndHide();
        }
        else
        {
            heldItem.PlaceOn(
                placementTarget,
                request.placementLocalPosition,
                request.placementLocalEulerAngles,
                request.placementLocalScale);
        }

        Speak(Speaker.Patient, request.successLine, MessageTone.Success);

        if (request.playCoughAfterSuccess)
        {
            PlayRequestCough();
        }

        if (string.Equals(request.requiredItemId, "oxygen", System.StringComparison.OrdinalIgnoreCase))
        {
            RegisterTaskProgress();
        }

        requestIndex++;

        if (requestIndex >= requests.Length)
        {
            finalLineReady = true;
        }

        currentRequestHasBeenAsked = false;
    }

    private PatientRequest GetCurrentRequest()
    {
        if (requests == null || requestIndex < 0 || requestIndex >= requests.Length)
        {
            return null;
        }

        return requests[requestIndex];
    }

    private void CompletePuzzle()
    {
        if (puzzleSolved)
        {
            return;
        }

        puzzleSolved = true;
        SpeakTimed(Speaker.Patient, finalLine, MessageTone.Success);
        onPuzzleSolved.Invoke();

        if (countAsSolvedTask && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPuzzleSolved();
        }
    }

    private void RegisterTaskProgress()
    {
        if (!countAsSolvedTask || taskRegistered)
        {
            return;
        }

        PrototypeGameManager gameManager = FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("[PatientPuzzleController] PrototypeGameManager not found, patient puzzle task progress was not registered.", this);
            return;
        }

        gameManager.RegisterTaskSuccess();
        taskRegistered = true;
        PrototypeHUD.ShowFeedback(
            GameLocalization.Text(
                "Task complete. Progress updated.",
                "Tache reussie. Progression mise a jour.",
                "Tarea completada. Progreso actualizado."),
            PrototypeHUD.FeedbackTone.Success,
            2.8f);
    }

    private void Speak(Speaker speaker, string line, MessageTone tone)
    {
        activeSpeakerName = GetSpeakerName(speaker);
        activeMessage = line;
        activeTone = tone;
        activeMessageNeedsInput = patientDialogueWaitsForNextInteraction;
        activeMessageUntil = patientDialogueWaitsForNextInteraction
            ? float.PositiveInfinity
            : Time.unscaledTime + messageDuration;
    }

    private void SpeakTimed(Speaker speaker, string line, MessageTone tone)
    {
        activeSpeakerName = GetSpeakerName(speaker);
        activeMessage = line;
        activeTone = tone;
        activeMessageNeedsInput = false;
        activeMessageUntil = Time.unscaledTime + messageDuration;
    }

    private void PlayFirstInteractionCough()
    {
        if (firstInteractionCoughPlayed || firstInteractionCough == null || audioSource == null)
        {
            return;
        }

        firstInteractionCoughPlayed = true;
        audioSource.PlayOneShot(firstInteractionCough, firstInteractionCoughVolume);
    }

    private void PlayRequestCough()
    {
        AudioClip clip = requestCough != null ? requestCough : firstInteractionCough;
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, requestCoughVolume);
    }

    private void ScheduleDialogueCough()
    {
        if (dialogueCoughRoutine != null)
        {
            StopCoroutine(dialogueCoughRoutine);
            dialogueCoughRoutine = null;
        }

        if (!enableDialogueCoughMarkers || dialogueCoughPlayed)
        {
            return;
        }

        AudioClip clip = requestCough != null ? requestCough : firstInteractionCough;
        if (clip == null || audioSource == null)
        {
            return;
        }

        dialogueCoughPlayed = true;
        dialogueCoughRoutine = StartCoroutine(PlayDialogueCoughAfterDelay(Mathf.Max(0.1f, dialogueCoughDelay), clip));
    }

    private IEnumerator PlayDialogueCoughAfterDelay(float delay, AudioClip clip)
    {
        yield return new WaitForSeconds(delay);

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, requestCoughVolume);
        }

        dialogueCoughRoutine = null;
    }

    private static GameObject ActivateStaticVisual(GameObject visual, Transform placementTarget)
    {
        if (visual == null)
        {
            return null;
        }

        GameObject visualInstance = visual;
        if (!visual.scene.IsValid())
        {
            visualInstance = Instantiate(visual);
            if (placementTarget != null)
            {
                visualInstance.transform.SetParent(placementTarget, false);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;
            }
        }

        ActivateSelfAndParents(visualInstance.transform);
        SetChildrenActive(visualInstance.transform);

        Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
        }

        PatientPuzzlePickup[] pickups = visualInstance.GetComponentsInChildren<PatientPuzzlePickup>(true);
        for (int i = 0; i < pickups.Length; i++)
        {
            pickups[i].enabled = false;
        }

        Rigidbody[] rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }

        Collider[] colliders = visualInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        return visualInstance;
    }

    private static void ActivateSelfAndParents(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static void SetChildrenActive(Transform target)
    {
        for (int i = 0; i < target.childCount; i++)
        {
            Transform child = target.GetChild(i);
            child.gameObject.SetActive(true);
            SetChildrenActive(child);
        }
    }

    private void OnGUI()
    {
        DrawToastMessage();

        if (string.IsNullOrWhiteSpace(activeMessage) || Time.unscaledTime > activeMessageUntil)
        {
            return;
        }

        GUIStyle speakerStyle = new GUIStyle(GUI.skin.label);
        speakerStyle.fontSize = 25;
        speakerStyle.fontStyle = FontStyle.Bold;
        speakerStyle.alignment = TextAnchor.MiddleLeft;
        speakerStyle.normal.textColor = GetToneColor(activeTone);

        GUIStyle lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.fontSize = 30;
        lineStyle.fontStyle = activeTone == MessageTone.Horror ? FontStyle.Bold : FontStyle.Normal;
        lineStyle.alignment = TextAnchor.MiddleLeft;
        lineStyle.wordWrap = true;
        lineStyle.normal.textColor = Color.white;

        float width = Mathf.Min(1120f, Screen.width - 64f);
        float height = 190f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - 30f;

        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(0.7f, 0.02f, 0.02f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, width, 4f), Texture2D.whiteTexture);
        GUI.color = oldColor;

        GUI.Label(new Rect(x + 32f, y + 20f, width - 64f, 34f), activeSpeakerName, speakerStyle);
        GUI.Label(new Rect(x + 32f, y + 62f, width - 64f, height - 82f), activeMessage, lineStyle);

        if (activeMessageNeedsInput)
        {
            GUIStyle continueStyle = new GUIStyle(GUI.skin.label);
            continueStyle.fontSize = 20;
            continueStyle.alignment = TextAnchor.LowerRight;
            continueStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(x + width - 200f, y + height - 38f, 168f, 26f), "Press E", continueStyle);
        }
    }

    private static void DrawToastMessage()
    {
        if (string.IsNullOrWhiteSpace(toastMessage) || Time.unscaledTime > toastMessageUntil)
        {
            return;
        }

        GUIStyle toastStyle = new GUIStyle(GUI.skin.label);
        toastStyle.fontSize = 24;
        toastStyle.fontStyle = FontStyle.Normal;
        toastStyle.alignment = TextAnchor.MiddleCenter;
        toastStyle.normal.textColor = Color.white;

        float width = Mathf.Min(620f, Screen.width - 80f);
        float height = 44f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.68f;

        GUI.Label(new Rect(x, y, width, height), toastMessage, toastStyle);
    }

    private static string GetSpeakerName(Speaker speaker)
    {
        switch (speaker)
        {
            case Speaker.Player:
                return "You";
            case Speaker.System:
                return "System";
            default:
                return "Patient";
        }
    }

    private static Color GetToneColor(MessageTone tone)
    {
        switch (tone)
        {
            case MessageTone.Success:
                return new Color(0.65f, 1f, 0.72f);
            case MessageTone.Warning:
                return new Color(1f, 0.86f, 0.42f);
            case MessageTone.Horror:
                return new Color(1f, 0.28f, 0.28f);
            default:
                return Color.white;
        }
    }
}

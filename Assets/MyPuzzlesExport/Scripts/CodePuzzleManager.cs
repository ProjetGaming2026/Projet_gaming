using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CodePuzzleManager : MonoBehaviour, IInteractable
{
    private const string ExpectedPuzzleCode = "6334";

    public static CodePuzzleManager Instance { get; private set; }
    public bool IsPuzzleSolved => puzzleSolved;

    [Header("Code")]
    public string secretCode = ExpectedPuzzleCode;
    public string currentInput = "";

    [Header("External Scene Door")]
    [Tooltip("Assign the Animator on the existing scene door. Leave empty only if using External Door Controller or Door Interactable.")]
    public Animator doorAnimator;
    [Tooltip("Assign a DoorInteractable from this export if the scene door uses it.")]
    public DoorInteractable doorInteractable;
    [Tooltip("Optional fallback for existing scene door scripts. The puzzle will send OpenDoor/Interact messages without needing to modify the scene door script.")]
    public Component externalDoorController;
    [Tooltip("Optional final fallback if the scene door has no Animator or open script. Assign the hinge/pivot transform to rotate it open.")]
    public Transform externalDoorHinge;

    [Header("Door Open Settings")]
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string alternateOpenTriggerName = "OpenDoor";
    [SerializeField] private string openBoolName = "IsOpen";
    [SerializeField] private float fallbackHingeOpenAngle = 95f;
    [SerializeField] private bool invertFallbackHingeDirection;
    [SerializeField] private bool lockExternalDoorUntilSolved = true;

    [Header("Scene References")]
    public GameObject monsterPrefab;
    public Transform monsterSpawnPoint;
    public Light roomLight;
    public AudioClip successSound;
    public AudioClip horrorSound;
    public AudioClip tensionSound;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private float puzzleDurationSeconds = 90f;

    private AudioSource audioSource;
    private bool isBusy;
    private bool puzzleActive;
    private bool puzzleSolved;
    private bool keypadDisabled;
    private float timeRemaining;
    private Quaternion externalDoorClosedRotation;
    private bool externalDoorRotationCaptured;
    private AudioClip generatedSuccessClip;
    private AudioClip generatedErrorClip;
    private AudioClip generatedTensionClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Force the expected code at runtime so old scene-serialized values cannot override it.
        secretCode = ExpectedPuzzleCode;
        CaptureExternalDoorClosedRotation();
    }

    private void Start()
    {
        CaptureExternalDoorClosedRotation();
        if (lockExternalDoorUntilSolved && !puzzleSolved)
        {
            LockAssignedDoor(true);
        }
    }

    private void Update()
    {
        if (puzzleActive && !keypadDisabled && !puzzleSolved)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            KeypadUIManager.Instance?.UpdateTimer(timeRemaining);

            if (timeRemaining <= 0f)
            {
                HandleTimeExpired();
                return;
            }
        }

        if (KeypadUIManager.IsUiOpen)
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
            return;
        }

        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer == null)
        {
            return;
        }

        float distance = Vector3.Distance(viewer.position, transform.position);
        if (distance <= interactionDistance)
        {
            KeypadUIManager.Instance?.ShowInteractionPrompt("Appuyer E pour interagir", this);
        }
        else
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
        }
    }

    public void Interact()
    {
        if (isBusy || keypadDisabled || puzzleSolved)
        {
            return;
        }

        OpenKeypad();
    }

    public void OpenKeypad()
    {
        if (KeypadUIManager.Instance == null)
        {
            Debug.LogWarning("[CodePuzzleManager] KeypadUIManager is missing.");
            return;
        }

        currentInput = string.Empty;
        StartPuzzleTimer();
        KeypadUIManager.Instance.ShowKeypadScreen(
            currentInput,
            AddDigit,
            DeleteDigit,
            ValidateCode,
            CloseUi,
            BuildRiddleText());
        Debug.Log("[CodePuzzleManager] Opening keypad UI with integrated riddle.");
    }

    public void ShowCodeScreen()
    {
        KeypadUIManager.Instance?.ShowKeypadScreen(
            currentInput,
            AddDigit,
            DeleteDigit,
            ValidateCode,
            CloseUi);
        Debug.Log("[CodePuzzleManager] Opening keypad screen.");
    }

    public void AddDigit(string digit)
    {
        if (isBusy || currentInput.Length >= 4)
        {
            return;
        }

        currentInput += digit;
        KeypadUIManager.Instance?.UpdateCodeDisplay(currentInput);
        Debug.Log($"[CodePuzzleManager] Digit added: {digit}");

        // Auto-validate on the fourth digit as a fallback if the validate button is missed.
        if (currentInput.Length == 4)
        {
            ValidateCode();
        }
    }

    public void DeleteDigit()
    {
        if (isBusy || string.IsNullOrEmpty(currentInput))
        {
            return;
        }

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        KeypadUIManager.Instance?.UpdateCodeDisplay(currentInput);
        Debug.Log("[CodePuzzleManager] Deleted last digit.");
    }

    public void ValidateCode()
    {
        if (isBusy)
        {
            return;
        }

        if (currentInput.Length != 4)
        {
            KeypadUIManager.Instance?.ShowStatus("ENTREZ 4 CHIFFRES", new Color(0.95f, 0.78f, 0.25f, 1f));
            return;
        }

        Debug.Log($"[CodePuzzleManager] Validating code: {currentInput}");
        if (currentInput == secretCode)
        {
            StartCoroutine(SuccessSequence());
            return;
        }

        StartCoroutine(IncorrectCodeRoutine());
    }

    // Kept for backward compatibility with old clue objects still present in the project.
    public void AddClue(string clueText, string clueDescription)
    {
        Debug.Log($"[CodePuzzleManager] Ignoring legacy clue '{clueDescription}' because this puzzle now uses a direct riddle.");
        KeypadUIManager.Instance?.ShowToast("Cette enigme n'utilise plus d'indices caches.", 1.5f);
    }

    public IEnumerator SuccessSequence()
    {
        isBusy = true;
        StopPuzzleTimer();
        puzzleSolved = true;
        LockAssignedDoor(false);
        PlayClip(successSound);
        KeypadUIManager.Instance?.ShowStatus("ACCES AUTORISE", Color.green);
        TryOpenDoor();
        yield return new WaitForSecondsRealtime(1.5f);
        currentInput = "";
        CloseUi();
        isBusy = false;
    }

    public IEnumerator HorrorSequence()
    {
        isBusy = true;
        StopPuzzleTimer();
        PlayClip(horrorSound);
        KeypadUIManager.Instance?.ShowStatus("ACCES REFUSE", Color.red);
        yield return StartCoroutine(BlinkLightRoutine());
        SpawnMonster();
        yield return new WaitForSecondsRealtime(1.5f);
        currentInput = "";
        CloseUi();
        isBusy = false;
    }

    // Wrong codes keep the player inside the keypad UI, but clear the current input.
    private IEnumerator IncorrectCodeRoutine()
    {
        isBusy = true;
        PlayClip(horrorSound);
        KeypadUIManager.Instance?.ShowStatus("CODE INCORRECT", Color.red);
        yield return new WaitForSecondsRealtime(1f);
        currentInput = string.Empty;
        KeypadUIManager.Instance?.UpdateCodeDisplay(currentInput);
        KeypadUIManager.Instance?.ShowStatus(string.Empty, Color.white);
        isBusy = false;
    }

    private IEnumerator BlinkLightRoutine()
    {
        if (roomLight == null)
        {
            yield break;
        }

        for (int i = 0; i < 5; i++)
        {
            roomLight.enabled = false;
            yield return new WaitForSecondsRealtime(0.12f);
            roomLight.enabled = true;
            yield return new WaitForSecondsRealtime(0.12f);
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null || monsterSpawnPoint == null)
        {
            Debug.LogWarning("[CodePuzzleManager] Monster prefab or spawn point missing.");
            return;
        }

        Instantiate(monsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
        Debug.Log("[CodePuzzleManager] Monster spawned.");
    }

    private void TryOpenDoor()
    {
        UnlockAssignedDoor();

        if (TryOpenDoorInteractable())
        {
            return;
        }

        if (TryOpenExternalController())
        {
            return;
        }

        if (TryOpenAnimator())
        {
            return;
        }

        if (TryOpenFallbackHinge())
        {
            return;
        }

        Debug.LogWarning("[CodePuzzleManager] No external door reference is assigned. Assign Door Animator, Door Interactable, External Door Controller, or External Door Hinge on the KeypadDoorPuzzle instance.", this);
    }

    private bool TryOpenDoorInteractable()
    {
        if (doorInteractable == null)
        {
            return false;
        }

        doorInteractable.SetLocked(false);
        doorInteractable.SetInteractionLocked(false);
        if (!doorInteractable.IsOpen)
        {
            doorInteractable.OpenDoor();
        }

        return true;
    }

    private bool TryOpenExternalController()
    {
        if (externalDoorController == null)
        {
            return false;
        }

        Animator controllerAnimator = externalDoorController as Animator;
        if (controllerAnimator != null)
        {
            doorAnimator = controllerAnimator;
            return false;
        }

        GameObject target = externalDoorController.gameObject;
        target.SendMessage("SetLocked", false, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("SetInteractionLocked", false, SendMessageOptions.DontRequireReceiver);

        if (InvokeDoorMethod(externalDoorController, "OpenDoor") ||
            InvokeDoorMethod(externalDoorController, "Open") ||
            InvokeDoorMethod(externalDoorController, "UnlockAndOpen") ||
            InvokeDoorMethod(externalDoorController, "Interact"))
        {
            return true;
        }

        Debug.LogWarning($"[CodePuzzleManager] External Door Controller '{externalDoorController.name}' has no OpenDoor/Open/Interact method. Falling back to Animator or Hinge if assigned.", externalDoorController);
        return false;
    }

    private bool InvokeDoorMethod(Component target, string methodName)
    {
        if (target == null)
        {
            return false;
        }

        System.Reflection.MethodInfo method = target.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            System.Type.EmptyTypes,
            null);
        if (method == null)
        {
            return false;
        }

        method.Invoke(target, null);
        return true;
    }

    private bool TryOpenAnimator()
    {
        if (doorAnimator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in doorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                (parameter.name == openTriggerName || parameter.name == alternateOpenTriggerName))
            {
                doorAnimator.SetTrigger(parameter.name);
                return true;
            }

            if (parameter.type == AnimatorControllerParameterType.Bool &&
                (parameter.name == openBoolName || parameter.name == openTriggerName))
            {
                doorAnimator.SetBool(parameter.name, true);
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(openTriggerName))
        {
            doorAnimator.SetTrigger(openTriggerName);
            return true;
        }

        return false;
    }

    private bool TryOpenFallbackHinge()
    {
        if (externalDoorHinge == null)
        {
            return false;
        }

        CaptureExternalDoorClosedRotation();
        int direction = invertFallbackHingeDirection ? -1 : 1;
        externalDoorHinge.localRotation = externalDoorClosedRotation * Quaternion.Euler(0f, direction * fallbackHingeOpenAngle, 0f);
        return true;
    }

    private void UnlockAssignedDoor()
    {
        LockAssignedDoor(false);
    }

    private void LockAssignedDoor(bool locked)
    {
        SendDoorLockMessages(doorInteractable, locked);
        SendDoorLockMessages(externalDoorController, locked);
        SendDoorLockMessages(doorAnimator, locked);

        if (externalDoorHinge != null)
        {
            SendDoorLockMessages(externalDoorHinge, locked);
        }
    }

    private void SendDoorLockMessages(Component targetComponent, bool locked)
    {
        if (targetComponent == null)
        {
            return;
        }

        GameObject target = targetComponent.gameObject;
        target.SendMessage("SetLocked", locked, SendMessageOptions.DontRequireReceiver);
        target.SendMessage("SetInteractionLocked", locked, SendMessageOptions.DontRequireReceiver);
        target.SendMessageUpwards("SetLocked", locked, SendMessageOptions.DontRequireReceiver);
        target.SendMessageUpwards("SetInteractionLocked", locked, SendMessageOptions.DontRequireReceiver);
        target.BroadcastMessage("SetLocked", locked, SendMessageOptions.DontRequireReceiver);
        target.BroadcastMessage("SetInteractionLocked", locked, SendMessageOptions.DontRequireReceiver);
    }

    private void CloseUi()
    {
        StopPuzzleTimer();
        currentInput = string.Empty;
        KeypadUIManager.Instance?.CloseAll();
    }

    private void CancelPuzzle()
    {
        Debug.Log("[CodePuzzleManager] Puzzle canceled by player.");
        CloseUi();
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            return;
        }

        if (clip == null)
        {
            clip = GetFallbackOneShotClip();
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private string BuildRiddleText()
    {
        return
            "Je suis compose de quatre chiffres.\n\n"
            + "Le premier est le nombre de cotes d'un triangle additionne a 3.\n"
            + "Le second est le double de 2 moins 1.\n"
            + "Le troisieme est le nombre de saisons auquel on retire 1.\n"
            + "Le dernier est le nombre de cotes d'un carre.\n\n"
            + "Assemble-les dans l'ordre pour ouvrir la porte.";
    }

    private void StartPuzzleTimer()
    {
        StopPuzzleTimer();
        puzzleActive = true;
        timeRemaining = puzzleDurationSeconds;
        KeypadUIManager.Instance?.SetTimerVisible(true);
        KeypadUIManager.Instance?.UpdateTimer(timeRemaining);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = tensionSound != null ? tensionSound : GetFallbackTensionClip();
            audioSource.loop = true;
            audioSource.volume = 0.45f;
            audioSource.Play();
        }
    }

    private void StopPuzzleTimer()
    {
        puzzleActive = false;

        if (audioSource != null && audioSource.loop)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
            audioSource.volume = 1f;
        }

        KeypadUIManager.Instance?.SetTimerVisible(false);
    }

    private void CaptureExternalDoorClosedRotation()
    {
        if (externalDoorHinge == null || externalDoorRotationCaptured)
        {
            return;
        }

        externalDoorClosedRotation = externalDoorHinge.localRotation;
        externalDoorRotationCaptured = true;
    }

    private void HandleTimeExpired()
    {
        if (keypadDisabled)
        {
            return;
        }

        isBusy = true;
        keypadDisabled = true;
        StopPuzzleTimer();
        PlayClip(horrorSound);
        KeypadUIManager.Instance?.ShowToast("TEMPS ECOULE", 2f);
        KeypadUIManager.Instance?.CloseAll();

        Collider keypadCollider = GetComponent<Collider>();
        if (keypadCollider != null)
        {
            keypadCollider.enabled = false;
        }

        currentInput = string.Empty;
        isBusy = false;
        Debug.Log("[CodePuzzleManager] Puzzle failed because the timer expired.");
    }

    private AudioClip GetFallbackOneShotClip()
    {
        if (generatedSuccessClip == null)
        {
            generatedSuccessClip = CreateToneClip("PuzzleSuccessFallback", 880f, 0.18f, 0.12f);
        }

        if (generatedErrorClip == null)
        {
            generatedErrorClip = CreateToneClip("PuzzleErrorFallback", 140f, 0.35f, 0.18f);
        }

        return puzzleSolved ? generatedSuccessClip : generatedErrorClip;
    }

    private AudioClip GetFallbackTensionClip()
    {
        if (generatedTensionClip == null)
        {
            generatedTensionClip = CreateToneClip("PuzzleTensionFallback", 220f, 0.5f, 0.04f);
        }

        return generatedTensionClip;
    }

    // Generates a very small fallback tone so the puzzle still gives audio feedback without inspector setup.
    private AudioClip CreateToneClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * amplitude;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

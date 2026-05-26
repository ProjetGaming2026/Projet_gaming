using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class CodePuzzleManager : MonoBehaviour, IInteractable
{
    private const string ExpectedPuzzleCode = "6334";
    private const string DefaultTargetDoorPath = "_map/asylumLevel/door3a/door3a";
    private const string MediumSceneName = "demoScene_Moyen";
    private const string KeypadObjectName = "SM_Pin_Pad";

    public static CodePuzzleManager Instance { get; private set; }
    public bool IsPuzzleSolved => puzzleSolved;
    private static bool sceneLoadedRegistered;

    public static bool ShouldBlockTargetDoorInteraction(DoorInteractable door)
    {
        if (door == null || SceneManager.GetActiveScene().name != MediumSceneName)
        {
            return false;
        }

        if (!IsTargetPuzzleDoor(door))
        {
            return false;
        }

        if (Instance != null && Instance.IsPuzzleSolved)
        {
            return false;
        }

        door.ForceClosed();
        return true;
    }

    [Header("Code")]
    public string secretCode = ExpectedPuzzleCode;
    public string currentInput = "";

    [Header("Scene References")]
    public Animator doorAnimator;
    public DoorInteractable doorInteractable;
    public GameObject monsterPrefab;
    public Transform monsterSpawnPoint;
    public Light roomLight;
    public AudioClip successSound;
    public AudioClip horrorSound;
    public AudioClip tensionSound;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private float puzzleDurationSeconds = 90f;
    [SerializeField] private float controlledDoorRadius = 8f;
    [SerializeField] private string targetDoorPath = DefaultTargetDoorPath;

    [Header("Task Progression")]
    [SerializeField] private bool countAsTask = true;

    private AudioSource audioSource;
    private bool isBusy;
    private bool puzzleActive;
    private bool puzzleSolved;
    private bool keypadDisabled;
    private bool taskRegistered;
    private float timeRemaining;
    private Coroutine timerRoutine;
    private readonly List<DoorInteractable> controlledDoors = new List<DoorInteractable>();
    private AudioClip generatedSuccessClip;
    private AudioClip generatedErrorClip;
    private AudioClip generatedTensionClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMediumSceneKeypadPuzzle()
    {
        if (!sceneLoadedRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadedRegistered = true;
        }

        ConfigureSceneKeypad(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneKeypad(scene);
    }

    private static void ConfigureSceneKeypad(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != MediumSceneName)
        {
            return;
        }

        if (FindSceneCodePuzzle(scene) != null)
        {
            return;
        }

        GameObject keypad = FindSceneKeypad(scene);
        if (keypad == null)
        {
            return;
        }

        EnsureKeypadCollider(keypad);
        keypad.AddComponent<CodePuzzleManager>();
    }

    private static CodePuzzleManager FindSceneCodePuzzle(Scene scene)
    {
        CodePuzzleManager[] managers = FindObjectsByType<CodePuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].gameObject.scene == scene)
            {
                return managers[i];
            }
        }

        return null;
    }

    private static GameObject FindSceneKeypad(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.name, KeypadObjectName, StringComparison.Ordinal) ||
                    candidate.name.IndexOf("Pin_Pad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    candidate.name.IndexOf("Keypad", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static void EnsureKeypadCollider(GameObject keypad)
    {
        if (keypad == null || keypad.GetComponent<Collider>() != null)
        {
            return;
        }

        BoxCollider collider = keypad.AddComponent<BoxCollider>();
        if (!TryGetLocalRendererBounds(keypad, out Bounds bounds))
        {
            collider.center = new Vector3(0f, 0f, 0f);
            collider.size = new Vector3(0.45f, 0.12f, 0.65f);
            return;
        }

        collider.center = bounds.center;
        collider.size = bounds.size;
    }

    private static bool TryGetLocalRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bool initialized = false;
        Bounds localBounds = default;
        Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Bounds rendererBounds = renderers[rendererIndex].bounds;
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;

            Vector3[] corners =
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z),
            };

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corners[cornerIndex]);
                if (!initialized)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        bounds = localBounds;
        return initialized;
    }

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
        ResolveSceneReferences();
    }

    private void Start()
    {
        // Delay one frame so runtime bootstrap scripts have time to add door components.
        StartCoroutine(InitializePuzzleNextFrame());
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
        ResolveSceneReferences();
        LockPuzzleDoor(false);
        RegisterTaskProgress();
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
        LockPuzzleDoor(true);
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
        if (OpenTargetPuzzleDoors())
        {
            return;
        }

        if (doorInteractable == null || controlledDoors.Count == 0)
        {
            ResolveSceneReferences();
            doorInteractable = FindNearestControlledDoor();
        }

        if (doorInteractable != null)
        {
            doorInteractable.ForceOpen();
            return;
        }

        if (doorAnimator == null)
        {
            doorAnimator = FindNearestDoorAnimator();
        }

        if (doorAnimator == null)
        {
            Debug.LogWarning("[CodePuzzleManager] Missing door animator and no DoorInteractable was found.");
            return;
        }

        foreach (AnimatorControllerParameter parameter in doorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                (parameter.name == "Open" || parameter.name == "OpenDoor"))
            {
                doorAnimator.SetTrigger(parameter.name);
                return;
            }

            if (parameter.type == AnimatorControllerParameterType.Bool &&
                (parameter.name == "Open" || parameter.name == "IsOpen"))
            {
                doorAnimator.SetBool(parameter.name, true);
                return;
            }
        }

        doorAnimator.SetTrigger("Open");
    }

    private void RegisterTaskProgress()
    {
        if (!countAsTask || taskRegistered)
        {
            return;
        }

        PrototypeGameManager gameManager = FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("[CodePuzzleManager] PrototypeGameManager not found, keypad task progress was not registered.", this);
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

    private bool OpenTargetPuzzleDoors()
    {
        bool openedAnyDoor = false;
        DoorInteractable[] allDoors = FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < allDoors.Length; i++)
        {
            DoorInteractable candidate = allDoors[i];
            if (!IsTargetPuzzleDoor(candidate))
            {
                continue;
            }

            candidate.SetInteractionLocked(false);
            candidate.ForceOpen();
            doorInteractable = candidate;

            if (!controlledDoors.Contains(candidate))
            {
                controlledDoors.Add(candidate);
            }

            openedAnyDoor = true;
        }

        if (!openedAnyDoor)
        {
            Debug.LogWarning("[CodePuzzleManager] Target keypad door was not found for automatic opening.");
        }

        return openedAnyDoor;
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
        puzzleActive = false;
        timeRemaining = 0f;
        KeypadUIManager.Instance?.SetTimerVisible(false);
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

    private IEnumerator InitializePuzzleNextFrame()
    {
        yield return null;
        ResolveSceneReferences();
        EnsureDoorStartsClosed();
        LockPuzzleDoor(true);
    }

    // Uses the scene path first so the keypad controls the intended asylum door, then falls back to nearby doors.
    private void ResolveSceneReferences()
    {
        controlledDoors.Clear();

        DoorInteractable exactDoor = FindDoorByHierarchyPath(targetDoorPath) ?? FindDoorByHierarchyPathWithoutSceneRoot(targetDoorPath);
        if (exactDoor != null)
        {
            controlledDoors.Add(exactDoor);
            doorInteractable = exactDoor;

            if (doorAnimator == null)
            {
                doorAnimator = exactDoor.GetComponent<Animator>() ?? exactDoor.GetComponentInChildren<Animator>(true);
            }

            return;
        }

        DoorInteractable[] allDoors = FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DoorInteractable candidate in allDoors)
        {
            if (candidate == null || candidate.gameObject == gameObject)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance <= controlledDoorRadius)
            {
                controlledDoors.Add(candidate);
            }
        }

        if (doorInteractable == null)
        {
            doorInteractable = FindNearestControlledDoor();
        }

        if (doorAnimator == null)
        {
            doorAnimator = FindNearestDoorAnimator();
        }
    }

    private DoorInteractable FindNearestControlledDoor()
    {
        DoorInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (DoorInteractable candidate in controlledDoors)
        {
            if (candidate == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private DoorInteractable FindDoorByHierarchyPath(string hierarchyPath)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
        {
            return null;
        }

        string[] pathParts = hierarchyPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length == 0)
        {
            return null;
        }

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null || !NameMatches(candidate, pathParts[pathParts.Length - 1]))
            {
                continue;
            }

            if (!HasMatchingAncestorPath(candidate, pathParts))
            {
                continue;
            }

            DoorInteractable door = candidate.GetComponent<DoorInteractable>();
            if (door == null)
            {
                door = candidate.GetComponentInChildren<DoorInteractable>(true);
            }

            if (door != null)
            {
                return door;
            }
        }

        return null;
    }

    private DoorInteractable FindDoorByHierarchyPathWithoutSceneRoot(string hierarchyPath)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
        {
            return null;
        }

        int firstSeparator = hierarchyPath.IndexOf('/');
        if (firstSeparator < 0 || firstSeparator >= hierarchyPath.Length - 1)
        {
            return null;
        }

        return FindDoorByHierarchyPath(hierarchyPath.Substring(firstSeparator + 1));
    }

    private static bool MatchesTargetDoorPath(Transform candidate, string hierarchyPath)
    {
        if (candidate == null || string.IsNullOrWhiteSpace(hierarchyPath))
        {
            return false;
        }

        string[] pathParts = hierarchyPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        return pathParts.Length > 0 && HasMatchingAncestorPath(candidate, pathParts);
    }

    private static bool MatchesTargetDoorPathWithoutSceneRoot(Transform candidate, string hierarchyPath)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
        {
            return false;
        }

        int firstSeparator = hierarchyPath.IndexOf('/');
        if (firstSeparator < 0 || firstSeparator >= hierarchyPath.Length - 1)
        {
            return false;
        }

        return MatchesTargetDoorPath(candidate, hierarchyPath.Substring(firstSeparator + 1));
    }

    private static bool HasMatchingAncestorPath(Transform candidate, string[] pathParts)
    {
        Transform current = candidate;
        for (int partIndex = pathParts.Length - 1; partIndex >= 0; partIndex--)
        {
            if (current == null || !NameMatches(current, pathParts[partIndex]))
            {
                return false;
            }

            current = current.parent;
        }

        return true;
    }

    private static bool IsTargetPuzzleDoor(DoorInteractable door)
    {
        return door != null &&
               (MatchesTargetDoorPath(door.transform, DefaultTargetDoorPath) ||
                MatchesTargetDoorPathWithoutSceneRoot(door.transform, DefaultTargetDoorPath));
    }

    private static bool NameMatches(Transform transformToCheck, string expectedName)
    {
        return transformToCheck != null &&
               string.Equals(transformToCheck.name, expectedName, StringComparison.Ordinal);
    }

    private DoorInteractable FindNearestDoorInteractable()
    {
        DoorInteractable[] doors = FindObjectsByType<DoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        DoorInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (DoorInteractable candidate in doors)
        {
            if (candidate == null || candidate.gameObject == gameObject)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private Animator FindNearestDoorAnimator()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Animator closest = null;
        float closestDistance = float.MaxValue;

        foreach (Animator candidate in animators)
        {
            if (candidate == null || candidate.gameObject == gameObject)
            {
                continue;
            }

            string lowerName = candidate.name.ToLowerInvariant();
            if (!lowerName.Contains("door"))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }

    private void EnsureDoorStartsClosed()
    {
        foreach (DoorInteractable targetDoor in controlledDoors)
        {
            if (targetDoor != null)
            {
                targetDoor.CloseDoor();
            }
        }

        if (doorAnimator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in doorAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool &&
                (parameter.name == "Open" || parameter.name == "IsOpen"))
            {
                doorAnimator.SetBool(parameter.name, false);
            }
        }
    }

    private void LockPuzzleDoor(bool locked)
    {
        if (controlledDoors.Count == 0)
        {
            ResolveSceneReferences();
        }

        foreach (DoorInteractable targetDoor in controlledDoors)
        {
            if (targetDoor == null)
            {
                continue;
            }

            targetDoor.SetInteractionLocked(locked);
            if (locked)
            {
                targetDoor.CloseDoor();
            }
        }
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
        LockPuzzleDoor(true);
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

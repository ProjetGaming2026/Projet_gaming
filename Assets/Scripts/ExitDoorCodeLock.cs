using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
#endif

public class ExitDoorCodeLock : MonoBehaviour, IInteractable
{
    public const string DefaultExitDoorName = "connectorDoor1-1-a";
    public const string DefaultExitDoorSecondLeafName = "connectorDoor1-1-b";
    private const string DefaultExitDoorSectionName = "section1";
    private const string DefaultExitCode = "1946";
    private const string MediumSceneName = "demoScene_Moyen";
    private const string MediumExitDoorName = "porte1";
    private const string MediumExitDoorSecondLeafName = "porte2";
    private const string MediumExitDoorSectionName = "asylumLevel";
    private const string MediumExitCode = "1234";

    private static readonly ExitDoorProfile[] ExitDoorProfiles =
    {
        new ExitDoorProfile(MediumSceneName, MediumExitDoorName, MediumExitDoorSecondLeafName, MediumExitDoorSectionName, MediumExitCode),
        new ExitDoorProfile(null, DefaultExitDoorName, DefaultExitDoorSecondLeafName, DefaultExitDoorSectionName, DefaultExitCode),
    };

    [Header("Access Code")]
    [SerializeField] private string exitCode = "1946";
    [SerializeField] private int codeLength = 4;

    [Header("Task Progression")]
    [SerializeField] private bool countAsTask = true;

    [Header("Door Links")]
    [SerializeField] private DoorInteractable[] linkedDoorInteractables;
    [SerializeField] private DoorController[] linkedDoorControllers;
    [SerializeField] private bool autoFindSiblingDoorLeaves = true;

    [Header("Victory")]
    [SerializeField] private bool createVictoryTrigger = true;
    [SerializeField] private Vector3 victoryTriggerPadding = new Vector3(0.65f, 0.35f, 1.45f);

    [Header("UI Style")]
    [SerializeField] private string facilityName = "CONTROLE DE SORTIE";
    [SerializeField] private Color panelColor = new Color(0.025f, 0.03f, 0.035f, 0.96f);
    [SerializeField] private Color accentColor = new Color(0.1f, 0.85f, 0.58f, 1f);
    [SerializeField] private Color grantedTextColor = new Color(0.28f, 1f, 0.42f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color textColor = new Color(0.88f, 0.95f, 0.9f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip keySound;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip grantedSound;

    private Canvas canvas;
    private GameObject panelObject;
    private TMP_Text statusText;
    private TMP_Text codeText;
    private TMP_Text hintText;
    private AudioSource audioSource;
    private string currentInput = string.Empty;
    private bool isBusy;
    private bool codeValidated;
    private bool taskRegistered;
    private PrototypeGameManager gameManager;
    private ExitDoorVictoryTrigger victoryTrigger;
    private static readonly Dictionary<DoorInteractable, ExitDoorCodeLock> LocksByDoor = new Dictionary<DoorInteractable, ExitDoorCodeLock>();
    private static bool isOpeningLinkedDoors;

    public static bool IsOpeningLinkedDoors => isOpeningLinkedDoors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDoorRegistry()
    {
        LocksByDoor.Clear();
        isOpeningLinkedDoors = false;
    }

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ResolveDoors();
        RegisterLinkedDoors();
        SetLinkedDoorInteractionLocked(true);
        ConfigureVictoryTrigger(false);
        BuildCanvas();
        HidePanel();
    }

    private void OnDestroy()
    {
        UnregisterLinkedDoors();
    }

    private void Update()
    {
        if (canvas == null || !canvas.gameObject.activeSelf || isBusy)
        {
            return;
        }

        HandleKeyboardInput();
    }

    public void Interact()
    {
        ResolveDoors();
        RegisterLinkedDoors();
        SetLinkedDoorInteractionLocked(true);

        if (codeValidated && CanOpenExitNow())
        {
            OpenLinkedDoors();
            return;
        }

        if (codeValidated)
        {
            PrototypeHUD.ShowFeedback(GameLocalization.Text(
                "Valid code. Finish all puzzles before leaving.",
                "Code valide. Termine tous les puzzles avant de sortir.",
                "Codigo valido. Termina todos los puzzles antes de salir."),
                PrototypeHUD.FeedbackTone.Info, 2.8f);
            return;
        }

        ShowPanel();
    }

    public string GetInteractPrompt()
    {
        if (codeValidated && CanOpenExitNow())
        {
            return GameLocalization.Text("Press E to open the exit", "Appuie sur E pour ouvrir la sortie", "Pulsa E para abrir la salida");
        }

        if (codeValidated)
        {
            return GameLocalization.Text("Valid code. Finish all puzzles", "Code valide. Termine tous les puzzles", "Codigo valido. Termina todos los puzzles");
        }

        return GameLocalization.Text("Press E to enter the exit code", "Appuie sur E pour entrer le code de sortie", "Pulsa E para introducir el codigo de salida");
    }

    public static bool TryGetLockForDoor(DoorInteractable door, out ExitDoorCodeLock codeLock)
    {
        codeLock = null;

        if (door == null)
        {
            return false;
        }

        if (!LocksByDoor.TryGetValue(door, out codeLock))
        {
            return false;
        }

        if (codeLock == null || !codeLock.isActiveAndEnabled)
        {
            LocksByDoor.Remove(door);
            codeLock = null;
            return false;
        }

        return true;
    }

    public static bool TryGetOrCreateDefaultLockForDoor(DoorInteractable door, out ExitDoorCodeLock codeLock)
    {
        if (TryGetLockForDoor(door, out codeLock))
        {
            return true;
        }

        codeLock = null;
        if (!TryFindProfileForDoor(door, out ExitDoorProfile profile))
        {
            return false;
        }

        if (!TryFindProfileDoors(profile, out DoorInteractable firstDoor, out DoorInteractable secondDoor))
        {
            return false;
        }

        codeLock = firstDoor.GetComponent<ExitDoorCodeLock>();
        if (codeLock == null)
        {
            codeLock = firstDoor.gameObject.AddComponent<ExitDoorCodeLock>();
        }

        codeLock.ConfigureExitDoors(firstDoor, secondDoor, profile);
        return codeLock != null && codeLock.isActiveAndEnabled;
    }

    public static bool TryFindDefaultExitDoors(out DoorInteractable firstDoor, out DoorInteractable secondDoor)
    {
        ExitDoorProfile profile;
        return TryFindDefaultExitDoorProfile(out firstDoor, out secondDoor, out profile);
    }

    public void ConfigureDefaultExitDoors(DoorInteractable firstDoor, DoorInteractable secondDoor)
    {
        if (!TryFindProfileForDoor(firstDoor, out ExitDoorProfile profile) &&
            !TryFindProfileForDoor(secondDoor, out profile))
        {
            profile = ExitDoorProfiles[ExitDoorProfiles.Length - 1];
        }

        ConfigureExitDoors(firstDoor, secondDoor, profile);
    }

    private void ConfigureExitDoors(DoorInteractable firstDoor, DoorInteractable secondDoor, ExitDoorProfile profile)
    {
        if (firstDoor == null || secondDoor == null)
        {
            return;
        }

        exitCode = profile.ExitCode;
        codeLength = 4;
        countAsTask = true;
        autoFindSiblingDoorLeaves = true;
        createVictoryTrigger = true;
        linkedDoorInteractables = new[] { firstDoor, secondDoor };
        linkedDoorControllers = new DoorController[0];
        RegisterLinkedDoors();
        SetLinkedDoorInteractionLocked(true);
    }

    private static bool TryFindDefaultExitDoorProfile(out DoorInteractable firstDoor, out DoorInteractable secondDoor, out ExitDoorProfile profile)
    {
        for (int i = 0; i < ExitDoorProfiles.Length; i++)
        {
            profile = ExitDoorProfiles[i];
            if (!profile.MatchesActiveScene())
            {
                continue;
            }

            if (TryFindProfileDoors(profile, out firstDoor, out secondDoor))
            {
                return true;
            }
        }

        firstDoor = null;
        secondDoor = null;
        profile = default(ExitDoorProfile);
        return false;
    }

    private static bool TryFindProfileDoors(ExitDoorProfile profile, out DoorInteractable firstDoor, out DoorInteractable secondDoor)
    {
        firstDoor = FindNamedDoorInteractableInSection(profile.FirstDoorName, profile.AncestorName);
        secondDoor = FindNamedDoorInteractableInSection(profile.SecondDoorName, profile.AncestorName);
        return firstDoor != null && secondDoor != null;
    }

    private static bool TryFindProfileForDoor(DoorInteractable door, out ExitDoorProfile profile)
    {
        if (door == null)
        {
            profile = default(ExitDoorProfile);
            return false;
        }

        for (int i = 0; i < ExitDoorProfiles.Length; i++)
        {
            profile = ExitDoorProfiles[i];
            if (!profile.MatchesActiveScene() ||
                !profile.MatchesDoorName(door.name) ||
                !HasAncestorNamed(door.transform, profile.AncestorName))
            {
                continue;
            }

            return true;
        }

        profile = default(ExitDoorProfile);
        return false;
    }

    private static DoorInteractable FindNamedDoorInteractableInSection(string doorName, string sectionName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != doorName)
            {
                continue;
            }

            if (!HasAncestorNamed(candidate, sectionName))
            {
                continue;
            }

            return EnsureDoorInteractable(candidate.gameObject);
        }

        return null;
    }

    private static DoorInteractable EnsureDoorInteractable(GameObject doorObject)
    {
        if (doorObject == null)
        {
            return null;
        }

        DoorInteractable door = doorObject.GetComponent<DoorInteractable>();
        if (door == null)
        {
            door = doorObject.AddComponent<DoorInteractable>();
        }

        if (!HospitalDoorUtility.HasAnyCollider(doorObject) &&
            HospitalDoorUtility.TryGetLocalBounds(doorObject, out Bounds localBounds))
        {
            BoxCollider collider = doorObject.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }

        return door;
    }

    private static bool HasAncestorNamed(Transform transformToCheck, string ancestorName)
    {
        Transform current = transformToCheck;
        while (current != null)
        {
            if (current.name == ancestorName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private struct ExitDoorProfile
    {
        public readonly string SceneName;
        public readonly string FirstDoorName;
        public readonly string SecondDoorName;
        public readonly string AncestorName;
        public readonly string ExitCode;

        public ExitDoorProfile(string sceneName, string firstDoorName, string secondDoorName, string ancestorName, string exitCode)
        {
            SceneName = sceneName;
            FirstDoorName = firstDoorName;
            SecondDoorName = secondDoorName;
            AncestorName = ancestorName;
            ExitCode = exitCode;
        }

        public bool MatchesActiveScene()
        {
            if (string.IsNullOrEmpty(SceneName) &&
                string.Equals(SceneManager.GetActiveScene().name, MediumSceneName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.IsNullOrEmpty(SceneName) ||
                string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.OrdinalIgnoreCase);
        }

        public bool MatchesDoorName(string doorName)
        {
            return string.Equals(doorName, FirstDoorName, StringComparison.Ordinal) ||
                string.Equals(doorName, SecondDoorName, StringComparison.Ordinal);
        }
    }

    private void ResolveDoors()
    {
        List<DoorInteractable> interactables = new List<DoorInteractable>();
        List<DoorController> controllers = new List<DoorController>();

        AddDoorInteractables(interactables, linkedDoorInteractables);
        AddDoorInteractables(interactables, GetComponents<DoorInteractable>());
        AddDoorInteractables(interactables, GetComponentsInChildren<DoorInteractable>(true));

        AddDoorControllers(controllers, linkedDoorControllers);
        AddDoorControllers(controllers, GetComponents<DoorController>());
        AddDoorControllers(controllers, GetComponentsInChildren<DoorController>(true));

        if (autoFindSiblingDoorLeaves && transform.parent != null)
        {
            string doorGroupPrefix = GetDoorGroupPrefix(name);
            if (!string.IsNullOrEmpty(doorGroupPrefix))
            {
                foreach (Transform sibling in transform.parent)
                {
                    if (!sibling.name.StartsWith(doorGroupPrefix))
                    {
                        continue;
                    }

                    AddDoorInteractables(interactables, sibling.GetComponents<DoorInteractable>());
                    AddDoorInteractables(interactables, sibling.GetComponentsInChildren<DoorInteractable>(true));
                    AddDoorControllers(controllers, sibling.GetComponents<DoorController>());
                    AddDoorControllers(controllers, sibling.GetComponentsInChildren<DoorController>(true));
                }
            }
        }

        linkedDoorInteractables = interactables.ToArray();
        linkedDoorControllers = controllers.ToArray();
        Debug.Log("ExitDoorCodeLock: linked " + linkedDoorInteractables.Length + " DoorInteractable(s) and " + linkedDoorControllers.Length + " DoorController(s).");
    }

    private void RegisterLinkedDoors()
    {
        for (int i = 0; i < linkedDoorInteractables.Length; i++)
        {
            DoorInteractable door = linkedDoorInteractables[i];
            if (door != null)
            {
                LocksByDoor[door] = this;
            }
        }
    }

    private void UnregisterLinkedDoors()
    {
        if (linkedDoorInteractables == null)
        {
            return;
        }

        for (int i = 0; i < linkedDoorInteractables.Length; i++)
        {
            DoorInteractable door = linkedDoorInteractables[i];
            if (door != null && LocksByDoor.TryGetValue(door, out ExitDoorCodeLock registeredLock) && registeredLock == this)
            {
                LocksByDoor.Remove(door);
            }
        }
    }

    private void SetLinkedDoorInteractionLocked(bool locked)
    {
        if (linkedDoorInteractables == null)
        {
            return;
        }

        for (int i = 0; i < linkedDoorInteractables.Length; i++)
        {
            if (linkedDoorInteractables[i] != null)
            {
                linkedDoorInteractables[i].SetInteractionLocked(locked);
            }
        }
    }

    private void AddDoorInteractables(List<DoorInteractable> list, DoorInteractable[] doors)
    {
        if (doors == null)
        {
            return;
        }

        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null && !list.Contains(doors[i]))
            {
                list.Add(doors[i]);
            }
        }
    }

    private void AddDoorControllers(List<DoorController> list, DoorController[] controllers)
    {
        if (controllers == null)
        {
            return;
        }

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null && !list.Contains(controllers[i]))
            {
                list.Add(controllers[i]);
            }
        }
    }

    private string GetDoorGroupPrefix(string objectName)
    {
        int lastDash = objectName.LastIndexOf('-');
        if (lastDash <= 0)
        {
            return null;
        }

        return objectName.Substring(0, lastDash + 1);
    }

    private void BuildCanvas()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("ExitDoorCodeCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject dimObject = CreateUiObject("Dim", canvasObject.transform);
        Image dim = dimObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.68f);
        Stretch(dim.rectTransform);

        panelObject = CreateUiObject("AccessPanel", canvasObject.transform);
        Image panel = panelObject.AddComponent<Image>();
        panel.color = panelColor;
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(620f, 760f);
        panelRect.anchoredPosition = Vector2.zero;

        AddOutline(panelObject, accentColor, new Vector2(2f, -2f));

        TMP_Text title = CreateText("Title", panelObject.transform, GameLocalization.Text("EXIT CONTROL", facilityName, "CONTROL DE SALIDA"), 30f, textColor);
        SetRect(title.rectTransform, new Vector2(32f, -36f), new Vector2(-32f, -94f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        title.alignment = TextAlignmentOptions.Center;

        statusText = CreateText("Status", panelObject.transform, GameLocalization.Text("EXIT LOCKED", "SORTIE VERROUILLEE", "SALIDA BLOQUEADA"), 24f, accentColor);
        SetRect(statusText.rectTransform, new Vector2(32f, -112f), new Vector2(-32f, -166f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        statusText.alignment = TextAlignmentOptions.Center;

        codeText = CreateText("CodeDisplay", panelObject.transform, BuildCodeDisplay(), 54f, accentColor);
        SetRect(codeText.rectTransform, new Vector2(50f, -188f), new Vector2(-50f, -282f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        codeText.alignment = TextAlignmentOptions.Center;
        AddDisplayBackground(codeText.transform);

        CreateKeypad(panelObject.transform);

        hintText = CreateText("Hint", panelObject.transform, GameLocalization.Text("ENTER: SUBMIT   BACKSPACE: DELETE   ESC: CANCEL", "ENTRER: VALIDER   RETOUR: EFFACER   ESC: ANNULER", "ENTER: VALIDAR   RETROCESO: BORRAR   ESC: CANCELAR"), 18f, new Color(0.62f, 0.7f, 0.68f, 1f));
        SetRect(hintText.rectTransform, new Vector2(32f, 34f), new Vector2(-32f, 82f), Vector2.zero, Vector2.one);
        hintText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateKeypad(Transform parent)
    {
        GameObject gridObject = CreateUiObject("Keypad", parent);
        GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(118f, 78f);
        grid.spacing = new Vector2(14f, 14f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        SetRect(gridRect, new Vector2(100f, -318f), new Vector2(-100f, -648f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        string[] keys =
        {
            "1", "2", "3",
            "4", "5", "6",
            "7", "8", "9",
            "CLR", "0", "ENT"
        };

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            Button button = CreateButton(key, gridObject.transform);
            button.onClick.AddListener(() => HandleButton(key));
        }
    }

    private Button CreateButton(string label, Transform parent)
    {
        GameObject buttonObject = CreateUiObject("Key_" + label, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.095f, 0.1f, 1f);
        AddOutline(buttonObject, new Color(0.22f, 0.28f, 0.29f, 1f), new Vector2(1f, -1f));

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.12f, 0.16f, 0.16f, 1f);
        colors.pressedColor = accentColor;
        colors.selectedColor = image.color;
        button.colors = colors;

        TMP_Text text = CreateText("Label", buttonObject.transform, label, 28f, textColor);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private void ShowPanel()
    {
        currentInput = string.Empty;
        UpdateDisplay(GameLocalization.Text("EXIT LOCKED", "SORTIE VERROUILLEE", "SALIDA BLOQUEADA"), accentColor);
        canvas.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("ExitDoorCodeLock: code panel opened.");
    }

    private void HidePanel()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleKeyboardInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                AddDigit(i.ToString());
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveLastDigit();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ClearInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitCode();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HidePanel();
        }
    }

    private void HandleButton(string key)
    {
        if (isBusy)
        {
            return;
        }

        if (key == "CLR")
        {
            ClearInput();
            return;
        }

        if (key == "ENT")
        {
            SubmitCode();
            return;
        }

        AddDigit(key);
    }

    private void AddDigit(string digit)
    {
        if (currentInput.Length >= codeLength)
        {
            return;
        }

        currentInput += digit;
        PlaySound(keySound);
        UpdateDisplay(GameLocalization.Text("ENTER EXIT CODE", "ENTRE LE CODE DE SORTIE", "INTRODUCE CODIGO DE SALIDA"), accentColor);
    }

    private void RemoveLastDigit()
    {
        if (currentInput.Length == 0)
        {
            return;
        }

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        PlaySound(keySound);
        UpdateDisplay(GameLocalization.Text("ENTER EXIT CODE", "ENTRE LE CODE DE SORTIE", "INTRODUCE CODIGO DE SALIDA"), accentColor);
    }

    private void ClearInput()
    {
        currentInput = string.Empty;
        PlaySound(keySound);
        UpdateDisplay(GameLocalization.Text("ENTER EXIT CODE", "ENTRE LE CODE DE SORTIE", "INTRODUCE CODIGO DE SALIDA"), accentColor);
    }

    private void SubmitCode()
    {
        if (isBusy)
        {
            return;
        }

        if (currentInput == exitCode)
        {
            StartCoroutine(GrantAccess());
            return;
        }

        StartCoroutine(DenyAccess());
    }

    private IEnumerator GrantAccess()
    {
        isBusy = true;

        if (!CanEnterExitCodeNow())
        {
            PlaySound(deniedSound);
            UpdateDisplay(GameLocalization.Text("TASKS REQUIRED", "TACHES REQUISES", "TAREAS REQUERIDAS"), dangerColor);
            yield return new WaitForSecondsRealtime(0.9f);
            currentInput = string.Empty;
            UpdateDisplay(GameLocalization.Text("EXIT LOCKED", "SORTIE VERROUILLEE", "SALIDA BLOQUEADA"), accentColor);
            isBusy = false;
            yield break;
        }

        codeValidated = true;

        if (countAsTask && !taskRegistered)
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<PrototypeGameManager>();
            }

            if (gameManager != null)
            {
                gameManager.RegisterTaskSuccess();
                taskRegistered = true;

                if (gameManager.CompletedTasks >= gameManager.RequiredTasks &&
                    gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape &&
                    gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Win &&
                    gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Lose)
                {
                    gameManager.SetPhase(PrototypeGameManager.GamePhase.Escape);
                }
            }
        }

        if (CanOpenExitNow())
        {
            Debug.Log("ExitDoorCodeLock: code accepted and exit ready. Opening linked doors.");
            SetLinkedDoorInteractionLocked(false);
            PlaySound(grantedSound);
            UpdateDisplay(GameLocalization.Text("ACCESS GRANTED", "ACCES AUTORISE", "ACCESO AUTORIZADO"), grantedTextColor);
            yield return new WaitForSecondsRealtime(0.65f);
            HidePanel();
            OpenLinkedDoors();
            isBusy = false;
            yield break;
        }

        SetLinkedDoorInteractionLocked(true);
        Debug.Log("ExitDoorCodeLock: code accepted but exit still waiting. Progress is " + GetProgressDebugText() + ".");
        PlaySound(grantedSound);
        UpdateDisplay(GameLocalization.Text("VALID CODE", "CODE VALIDE", "CODIGO VALIDO"), grantedTextColor);
        yield return new WaitForSecondsRealtime(0.65f);
        HidePanel();
        PrototypeHUD.ShowFeedback(GameLocalization.Text(
            "Valid code. Finish all puzzles before leaving.",
            "Code valide. Termine tous les puzzles avant de sortir.",
            "Codigo valido. Termina todos los puzzles antes de salir."), PrototypeHUD.FeedbackTone.Info, 3f);
        isBusy = false;
    }

    private IEnumerator DenyAccess()
    {
        isBusy = true;
        PlaySound(deniedSound);
        PuzzleFailureMonsterAlert.Alert(transform);
        UpdateDisplay(GameLocalization.Text("ACCESS DENIED", "ACCES REFUSE", "ACCESO DENEGADO"), dangerColor);
        yield return new WaitForSecondsRealtime(0.9f);
        currentInput = string.Empty;
        UpdateDisplay(GameLocalization.Text("EXIT LOCKED", "SORTIE VERROUILLEE", "SALIDA BLOQUEADA"), accentColor);
        isBusy = false;
    }

    private void OpenLinkedDoors()
    {
        if (!CanOpenExitNow())
        {
            SetLinkedDoorInteractionLocked(true);
            PrototypeHUD.ShowFeedback(GameLocalization.Text(
                "The exit stays locked until every puzzle is solved.",
                "La sortie reste verrouillee tant que tous les puzzles ne sont pas valides.",
                "La salida sigue bloqueada hasta resolver todos los puzzles."),
                PrototypeHUD.FeedbackTone.Warning, 3f);
            return;
        }

        SetLinkedDoorInteractionLocked(false);

        isOpeningLinkedDoors = true;
        try
        {
            for (int i = 0; i < linkedDoorInteractables.Length; i++)
            {
                if (linkedDoorInteractables[i] != null && !linkedDoorInteractables[i].IsOpen)
                {
                    linkedDoorInteractables[i].OpenDoor();
                }
            }
        }
        finally
        {
            isOpeningLinkedDoors = false;
        }

        for (int i = 0; i < linkedDoorControllers.Length; i++)
        {
            if (linkedDoorControllers[i] != null)
            {
                linkedDoorControllers[i].OpenDoor();
            }
        }

        ConfigureVictoryTrigger(true);
    }

    private bool CanOpenExitNow()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        return gameManager != null &&
            gameManager.CompletedTasks >= gameManager.RequiredTasks &&
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape;
    }

    private bool CanEnterExitCodeNow()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (gameManager == null)
        {
            return true;
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Win ||
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose)
        {
            return false;
        }

        int requiredTasksBeforeCode = countAsTask
            ? Mathf.Max(0, gameManager.RequiredTasks - 1)
            : gameManager.RequiredTasks;

        return gameManager.CompletedTasks >= requiredTasksBeforeCode;
    }

    private string GetProgressDebugText()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (gameManager == null)
        {
            return "no game manager";
        }

        return gameManager.CompletedTasks + "/" + gameManager.RequiredTasks + " phase=" + gameManager.CurrentPhase;
    }

    private void ConfigureVictoryTrigger(bool active)
    {
        if (!createVictoryTrigger)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (victoryTrigger == null)
        {
            Bounds bounds = CalculateLinkedDoorBounds();
            GameObject triggerObject = new GameObject("ExitDoorVictoryTrigger");
            triggerObject.transform.SetParent(transform.parent != null ? transform.parent : transform, true);
            triggerObject.transform.position = bounds.center;
            triggerObject.transform.rotation = transform.rotation;

            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(
                Mathf.Max(0.5f, bounds.size.x + victoryTriggerPadding.x),
                Mathf.Max(1.8f, bounds.size.y + victoryTriggerPadding.y),
                Mathf.Max(1.2f, bounds.size.z + victoryTriggerPadding.z));

            victoryTrigger = triggerObject.AddComponent<ExitDoorVictoryTrigger>();
            victoryTrigger.Configure(gameManager);
        }

        victoryTrigger.Configure(gameManager);
        victoryTrigger.gameObject.SetActive(active);
    }

    private Bounds CalculateLinkedDoorBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one);

        if (linkedDoorInteractables != null)
        {
            for (int i = 0; i < linkedDoorInteractables.Length; i++)
            {
                DoorInteractable door = linkedDoorInteractables[i];
                if (door == null)
                {
                    continue;
                }

                Collider doorCollider = door.GetComponent<Collider>();
                if (doorCollider != null)
                {
                    EncapsulateBounds(ref bounds, doorCollider.bounds, ref hasBounds);
                    continue;
                }

                Renderer doorRenderer = door.GetComponent<Renderer>();
                if (doorRenderer != null)
                {
                    EncapsulateBounds(ref bounds, doorRenderer.bounds, ref hasBounds);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(transform.position, new Vector3(2.6f, 2.8f, 1.4f));
        }

        return bounds;
    }

    private static void EncapsulateBounds(ref Bounds targetBounds, Bounds newBounds, ref bool hasBounds)
    {
        if (!hasBounds)
        {
            targetBounds = newBounds;
            hasBounds = true;
            return;
        }

        targetBounds.Encapsulate(newBounds);
    }

    private void UpdateDisplay(string status, Color color)
    {
        statusText.text = status;
        statusText.color = color;
        codeText.text = BuildCodeDisplay();
        codeText.color = color;
    }

    private string BuildCodeDisplay()
    {
        string value = string.Empty;
        for (int i = 0; i < codeLength; i++)
        {
            value += i < currentInput.Length ? "*" : "_";
            if (i < codeLength - 1)
            {
                value += " ";
            }
        }

        return value;
    }

    private void AddDisplayBackground(Transform codeDisplay)
    {
        GameObject backgroundObject = CreateUiObject("DisplayBackground", panelObject.transform);
        backgroundObject.transform.SetSiblingIndex(codeDisplay.GetSiblingIndex());
        Image image = backgroundObject.AddComponent<Image>();
        image.color = new Color(0.005f, 0.015f, 0.012f, 1f);
        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(50f, -188f), new Vector2(-50f, -282f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        AddOutline(backgroundObject, accentColor, new Vector2(1f, -1f));
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        gameObject.AddComponent<RectTransform>();
        return gameObject;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void SetRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}

public class ExitDoorVictoryTrigger : MonoBehaviour
{
    private PrototypeGameManager gameManager;

    public void Configure(PrototypeGameManager newGameManager)
    {
        gameManager = newGameManager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (gameManager != null && gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape)
        {
            gameManager.TriggerWin();
        }
    }
}

public static class ExitDoorCodeLockBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallDefaultExitDoorLock()
    {
        if (!ExitDoorCodeLock.TryFindDefaultExitDoors(out DoorInteractable firstDoor, out DoorInteractable secondDoor))
        {
            return;
        }

        ExitDoorCodeLock codeLock = firstDoor.GetComponent<ExitDoorCodeLock>();
        if (codeLock == null)
        {
            codeLock = firstDoor.gameObject.AddComponent<ExitDoorCodeLock>();
            Debug.Log("ExitDoorCodeLockBootstrap: installed code lock on " + firstDoor.name + ".");
        }

        codeLock.ConfigureDefaultExitDoors(firstDoor, secondDoor);
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public static class ExitDoorCodeLockEditorBootstrap
{
    private static int attemptsRemaining;

    static ExitDoorCodeLockEditorBootstrap()
    {
        ScheduleConfigureOpenSceneExitDoors();
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        ScheduleConfigureOpenSceneExitDoors();
    }

    private static void ScheduleConfigureOpenSceneExitDoors()
    {
        attemptsRemaining = 20;
        EditorApplication.update -= TryConfigureOpenSceneExitDoors;
        EditorApplication.update += TryConfigureOpenSceneExitDoors;
    }

    private static void TryConfigureOpenSceneExitDoors()
    {
        if (attemptsRemaining <= 0)
        {
            EditorApplication.update -= TryConfigureOpenSceneExitDoors;
            return;
        }

        attemptsRemaining--;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!ExitDoorCodeLock.TryFindDefaultExitDoors(out DoorInteractable firstDoor, out DoorInteractable secondDoor))
        {
            return;
        }

        ExitDoorCodeLock codeLock = firstDoor.GetComponent<ExitDoorCodeLock>();
        if (codeLock == null)
        {
            codeLock = firstDoor.gameObject.AddComponent<ExitDoorCodeLock>();
        }

        codeLock.ConfigureDefaultExitDoors(firstDoor, secondDoor);
        EditorUtility.SetDirty(firstDoor);
        EditorUtility.SetDirty(secondDoor);
        EditorUtility.SetDirty(codeLock);
        EditorSceneManager.MarkSceneDirty(firstDoor.gameObject.scene);
        Debug.Log("ExitDoorCodeLockEditorBootstrap: configured " + firstDoor.name + " and " + secondDoor.name + " as coded exit doors.");
        EditorApplication.update -= TryConfigureOpenSceneExitDoors;
    }
}
#endif

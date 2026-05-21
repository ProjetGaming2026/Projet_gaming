using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExitDoorCodeLock : MonoBehaviour, IInteractable
{
    [Header("Access Code")]
    [SerializeField] private string exitCode = "1946";
    [SerializeField] private int codeLength = 4;

    [Header("Task Progression")]
    [SerializeField] private bool countAsTask;

    [Header("Door Links")]
    [SerializeField] private DoorInteractable[] linkedDoorInteractables;
    [SerializeField] private DoorController[] linkedDoorControllers;
    [SerializeField] private bool autoFindSiblingDoorLeaves = true;

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
    private static readonly Dictionary<DoorInteractable, ExitDoorCodeLock> LocksByDoor = new Dictionary<DoorInteractable, ExitDoorCodeLock>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDoorRegistry()
    {
        LocksByDoor.Clear();
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

        if (codeValidated && CanOpenExitNow())
        {
            OpenLinkedDoors();
            return;
        }

        if (codeValidated)
        {
if (codeValidated)
{
    SetLinkedDoorInteractionLocked(true);

    PrototypeHUD.ShowFeedback(GameLocalization.Text(
        "Valid code. Finish all puzzles before leaving.",
        "Code valide. Termine tous les puzzles avant de sortir.",
        "Codigo valido. Termina todos los puzzles antes de salir."),
        PrototypeHUD.FeedbackTone.Info, 2.8f);

    return;
}
            return;
        }

        SetLinkedDoorInteractionLocked(true);
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
            }
        }

        if (CanOpenExitNow())
        {
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
            return;
        }

        SetLinkedDoorInteractionLocked(false);

        for (int i = 0; i < linkedDoorInteractables.Length; i++)
        {
            if (linkedDoorInteractables[i] != null && !linkedDoorInteractables[i].IsOpen)
            {
                linkedDoorInteractables[i].Interact();
            }
        }

        for (int i = 0; i < linkedDoorControllers.Length; i++)
        {
            if (linkedDoorControllers[i] != null)
            {
                linkedDoorControllers[i].OpenDoor();
            }
        }
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

public static class ExitDoorCodeLockBootstrap
{
    private const string DefaultExitDoorName = "connectorDoor1-1-a";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallDefaultExitDoorLock()
    {
        GameObject door = GameObject.Find(DefaultExitDoorName);
        if (door == null)
        {
            return;
        }

        if (door.GetComponent<ExitDoorCodeLock>() == null)
        {
            door.AddComponent<ExitDoorCodeLock>();
            Debug.Log("ExitDoorCodeLockBootstrap: installed code lock on " + DefaultExitDoorName + ".");
        }
    }
}

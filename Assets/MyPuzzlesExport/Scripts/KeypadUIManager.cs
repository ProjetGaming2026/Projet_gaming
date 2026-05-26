using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Plugins.ButtonSoundsEditor;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class KeypadUIManager : MonoBehaviour
{
    public static KeypadUIManager Instance { get; private set; }
    public static bool IsUiOpen => Instance != null && Instance.isUiOpen;

    private Canvas rootCanvas;
    private GameObject promptPanel;
    private TMP_Text promptText;
    private GameObject toastPanel;
    private TMP_Text toastText;
    private GameObject keypadWindow;
    private GameObject timerPanel;
    private TMP_Text timerText;
    private TMP_Text leftTitleText;
    private TMP_Text riddleText;
    private TMP_Text rightTitleText;
    private TMP_Text codeDisplayText;
    private TMP_Text statusText;
    private UnityEngine.Object promptOwner;
    private AudioSource buttonAudioSource;
    private AudioClip keypadClickClip;
    private AudioClip generatedFallbackClickClip;
    private bool isUiOpen;
    private float toastHideAt;
    private Action<string> addDigitAction;
    private Action deleteDigitAction;
    private Action validateAction;
    private Action closeAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        EnsureEventSystemExists();

        GameObject root = new GameObject("KeypadUIRoot");
        root.AddComponent<KeypadUIManager>();
    }

    private static void EnsureEventSystemExists()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        CloseAll();
    }

    private void Update()
    {
        if (toastPanel != null && toastPanel.activeSelf && Time.unscaledTime >= toastHideAt)
        {
            toastPanel.SetActive(false);
        }

        if (isUiOpen)
        {
            HandleKeyboardInput();
        }

        if (isUiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            closeAction?.Invoke();
        }
    }

    public void ShowInteractionPrompt(string message, UnityEngine.Object owner)
    {
        if (isUiOpen || promptPanel == null || promptText == null)
        {
            return;
        }

        promptOwner = owner;
        promptText.text = message;
        promptPanel.SetActive(true);
    }

    public void HideInteractionPrompt(UnityEngine.Object owner)
    {
        if (promptOwner != owner || promptPanel == null)
        {
            return;
        }

        promptOwner = null;
        promptPanel.SetActive(false);
    }

    public void ShowToast(string message, float duration)
    {
        if (toastPanel == null || toastText == null)
        {
            return;
        }

        toastText.text = message;
        toastPanel.SetActive(true);
        toastHideAt = Time.unscaledTime + duration;
    }

    // Legacy entry point kept only so older scripts still compile.
    public void ShowClueScreen(string clues, Action onContinue, Action onCancel)
    {
        ShowKeypadScreen(string.Empty, null, null, null, onCancel, clues);
    }

    public void ShowKeypadScreen(
        string currentInput,
        Action<string> addDigit,
        Action deleteDigit,
        Action validate,
        Action close,
        string riddle = null)
    {
        addDigitAction = addDigit;
        deleteDigitAction = deleteDigit;
        validateAction = validate;
        closeAction = close;

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        keypadWindow.SetActive(true);
        isUiOpen = true;
        UnlockPlayerControls();

        leftTitleText.text = "ENIGME D'ACCES";
        rightTitleText.text = "SAISIE DU CODE";
        riddleText.text = string.IsNullOrWhiteSpace(riddle) ? string.Empty : riddle;
        UpdateCodeDisplay(currentInput);
        ShowStatus(string.Empty, Color.white);
    }

    public void UpdateCodeDisplay(string currentInput)
    {
        if (codeDisplayText == null)
        {
            return;
        }

        string padded = currentInput.PadRight(4, '_');
        codeDisplayText.text = string.Join(" ", padded.ToCharArray());
    }

    public void ShowStatus(string message, Color color)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = color;
    }

    public void SetTimerVisible(bool visible)
    {
        if (timerPanel == null)
        {
            return;
        }

        timerPanel.SetActive(visible);
    }

    public void UpdateTimer(float secondsRemaining)
    {
        if (timerText == null)
        {
            return;
        }

        int safeSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        int minutes = safeSeconds / 60;
        int seconds = safeSeconds % 60;
        timerText.text = $"TEMPS RESTANT : {minutes:00}:{seconds:00}";
    }

    public void CloseAll()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (toastPanel != null)
        {
            toastPanel.SetActive(false);
        }

        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }

        if (keypadWindow != null)
        {
            keypadWindow.SetActive(false);
        }

        isUiOpen = false;
        LockPlayerControls();
    }

    private void BuildUi()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 700;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        buttonAudioSource = gameObject.AddComponent<AudioSource>();
        buttonAudioSource.playOnAwake = false;
        buttonAudioSource.spatialBlend = 0f;
        buttonAudioSource.volume = 1f;
        buttonAudioSource.ignoreListenerPause = true;
        buttonAudioSource.ignoreListenerVolume = false;
        buttonAudioSource.loop = false;
        keypadClickClip = ResolveKeypadClickClip();

        promptPanel = CreateAnchoredPanel("InteractionPrompt", transform, new Vector2(0.5f, 0.12f), new Vector2(360f, 54f), new Color(0f, 0f, 0f, 0.72f));
        promptText = CreateStretchText(promptPanel.transform, "PromptText", 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);

        toastPanel = CreateAnchoredPanel("ToastPanel", transform, new Vector2(0.5f, 0.2f), new Vector2(520f, 60f), new Color(0.05f, 0.05f, 0.05f, 0.9f));
        toastText = CreateStretchText(toastPanel.transform, "ToastText", 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);

        keypadWindow = CreateAnchoredPanel("KeypadWindow", transform, new Vector2(0.5f, 0.5f), new Vector2(1120f, 640f), new Color(0.02f, 0.02f, 0.02f, 0.95f));

        // Timer bar centered at the top of the main puzzle window.
        timerPanel = CreateChildPanel("TimerPanel", keypadWindow.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, 44f), new Vector2(0f, -34f), new Color(0f, 0f, 0f, 0.45f));
        timerText = CreateStretchText(timerPanel.transform, "TimerText", 24f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);

        GameObject leftPanel = CreateChildPanel("LeftPanel", keypadWindow.transform, new Vector2(0f, 0f), new Vector2(0.57f, 1f), new Vector2(0f, 0.5f), new Vector2(-70f, -120f), new Vector2(28f, -26f), new Color(0f, 0f, 0f, 0.20f));
        GameObject rightPanel = CreateChildPanel("RightPanel", keypadWindow.transform, new Vector2(0.57f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-56f, -120f), new Vector2(-28f, -26f), new Color(0f, 0f, 0f, 0.20f));

        leftTitleText = CreateTopText(leftPanel.transform, "LeftTitleText", "ENIGME D'ACCES", 36f, 60f);
        riddleText = CreateBodyText(leftPanel.transform, "RiddleText", 28f, new Vector2(20f, 92f), new Vector2(-20f, -20f));

        rightTitleText = CreateTopText(rightPanel.transform, "RightTitleText", "SAISIE DU CODE", 34f, 60f);
        statusText = CreateFixedText(rightPanel.transform, "StatusText", 24f, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, 42f), new Vector2(0f, -98f));
        GameObject codePanel = CreateChildPanel("CodePanel", rightPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(260f, 64f), new Vector2(0f, -176f), new Color(0f, 0f, 0f, 0.35f));
        codeDisplayText = CreateStretchText(codePanel.transform, "CodeDisplayText", 40f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);

        GameObject keypadGrid = new GameObject("KeypadGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        keypadGrid.transform.SetParent(rightPanel.transform, false);
        RectTransform gridRect = keypadGrid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(280f, 270f);
        gridRect.anchoredPosition = new Vector2(0f, -74f);

        GridLayoutGroup grid = keypadGrid.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.cellSize = new Vector2(80f, 58f);
        grid.spacing = new Vector2(10f, 10f);
        grid.childAlignment = TextAnchor.MiddleCenter;

        for (int digit = 1; digit <= 9; digit++)
        {
            string label = digit.ToString();
            CreateDigitButton(keypadGrid.transform, $"Digit_{label}", label);
        }

        CreateDigitButton(keypadGrid.transform, "Digit_Blank_Left", string.Empty, false);
        CreateDigitButton(keypadGrid.transform, "Digit_0", "0");
        CreateDigitButton(keypadGrid.transform, "Digit_Blank_Right", string.Empty, false);

        CreateActionButton(rightPanel.transform, "EffacerButton", "EFFACER", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 44f), new Vector2(-84f, 26f), () => deleteDigitAction?.Invoke());
        CreateActionButton(rightPanel.transform, "ValiderButton", "VALIDER", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 44f), new Vector2(84f, 26f), () => validateAction?.Invoke());

        SetTimerVisible(false);
    }

    // Lets the player enter the code from the keyboard in addition to mouse clicks.
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            PlayKeypadClickSound();
            validateAction?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            PlayKeypadClickSound();
            deleteDigitAction?.Invoke();
        }

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = KeyCode.Alpha0 + digit;
            KeyCode keypadKey = KeyCode.Keypad0 + digit;

            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                PlayKeypadClickSound();
                addDigitAction?.Invoke(digit.ToString());
                return;
            }
        }
    }

    private GameObject CreateAnchoredPanel(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private GameObject CreateChildPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TMP_Text CreateStretchText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private TMP_Text CreateTopText(Transform parent, string name, string initialText, float fontSize, float height)
    {
        TMP_Text text = CreateFixedText(parent, name, fontSize, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-24f, height), new Vector2(0f, -10f));
        text.text = initialText;
        text.fontStyle = FontStyles.Bold;
        return text;
    }

    private TMP_Text CreateBodyText(Transform parent, string name, float fontSize, Vector2 offsetMin, Vector2 offsetMax)
    {
        TMP_Text text = CreateStretchText(parent, name, fontSize, TextAlignmentOptions.TopLeft, offsetMin, offsetMax);
        text.margin = new Vector4(10f, 8f, 10f, 8f);
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    // Fixed text blocks are used for the code and status so they stay aligned in the right column.
    private TMP_Text CreateFixedText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private void CreateDigitButton(Transform parent, string name, string label, bool interactable = true)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        Button button = buttonObject.GetComponent<Button>();
        image.color = interactable ? new Color(0.16f, 0.16f, 0.16f, 0.98f) : new Color(0f, 0f, 0f, 0f);
        button.interactable = interactable;

        if (interactable)
        {
            button.onClick.AddListener(PlayKeypadClickSound);
            button.onClick.AddListener(() => addDigitAction?.Invoke(label));
            ConfigureButtonClickSound(buttonObject);
        }

        TMP_Text text = CreateStretchText(buttonObject.transform, $"{name}_Text", 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        text.text = label;
        text.color = interactable ? Color.white : new Color(0f, 0f, 0f, 0f);
    }

    private void CreateActionButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Action onClick)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 0.98f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(PlayKeypadClickSound);
        button.onClick.AddListener(() => onClick?.Invoke());
        ConfigureButtonClickSound(buttonObject);

        TMP_Text text = CreateStretchText(buttonObject.transform, $"{name}_Text", 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
    }

    // Reuses the imported Button Sound Editor script so every keypad button
    // gets the same click behavior without changing the keypad gameplay logic.
    private void ConfigureButtonClickSound(GameObject buttonObject)
    {
        if (buttonAudioSource == null)
        {
            return;
        }

        ButtonClickSound clickSound = buttonObject.GetComponent<ButtonClickSound>();
        if (clickSound == null)
        {
            clickSound = buttonObject.AddComponent<ButtonClickSound>();
        }

        clickSound.AudioSource = buttonAudioSource;
        clickSound.ClickSound = keypadClickClip != null ? keypadClickClip : GetFallbackClickClip();
    }

    private void PlayKeypadClickSound()
    {
        if (buttonAudioSource == null)
        {
            return;
        }

        AudioClip clipToPlay = keypadClickClip != null ? keypadClickClip : GetFallbackClickClip();
        if (clipToPlay == null)
        {
            return;
        }

        if (!clipToPlay.preloadAudioData)
        {
            clipToPlay.LoadAudioData();
        }

        buttonAudioSource.PlayOneShot(clipToPlay);
    }

    // Prefers click1.wav for a softer keypad feel, then falls back to click2.wav.
    private AudioClip ResolveKeypadClickClip()
    {
        AudioClip loadedClip = FindLoadedAudioClip("click1") ?? FindLoadedAudioClip("click2");
        if (loadedClip != null)
        {
            return loadedClip;
        }

#if UNITY_EDITOR
        loadedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Plugins/ButtonSoundsEditor/Examples/click1.wav")
            ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Plugins/ButtonSoundsEditor/Examples/click2.wav");
#endif

        return loadedClip;
    }

    private AudioClip FindLoadedAudioClip(string clipName)
    {
        AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
        for (int i = 0; i < clips.Length; i++)
        {
            if (string.Equals(clips[i].name, clipName, StringComparison.OrdinalIgnoreCase))
            {
                return clips[i];
            }
        }

        return null;
    }

    private AudioClip GetFallbackClickClip()
    {
        if (generatedFallbackClickClip != null)
        {
            return generatedFallbackClickClip;
        }

        const int sampleRate = 44100;
        const float duration = 0.055f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-26f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 1300f * t) * envelope * 0.32f;
        }

        generatedFallbackClickClip = AudioClip.Create("GeneratedKeypadClick", sampleCount, 1, sampleRate, false);
        generatedFallbackClickClip.SetData(samples, 0);
        return generatedFallbackClickClip;
    }

    private void UnlockPlayerControls()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockPlayerControls()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}

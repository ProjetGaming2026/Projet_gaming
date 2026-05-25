using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhonePuzzleUI : MonoBehaviour
{
    public static PhonePuzzleUI Instance { get; private set; }
    public static bool IsReadableOverlayOpen => Instance != null && Instance.readableOverlay != null && Instance.readableOverlay.activeSelf;

    [Header("UI Components")]
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField] private ObjectiveUI objectiveUI;
    [SerializeField] private TimerUI timerUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    [Header("Answer Input")]
    [SerializeField] private GameObject answerInputRoot;
    [SerializeField] private TMP_Text answerInputText;
    [SerializeField] private Button[] digitButtons = new Button[10];
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button submitButton;

    [Header("Readable Overlay")]
    [SerializeField] private GameObject readableOverlay;
    [SerializeField] private CanvasGroup readableOverlayGroup;
    [SerializeField] private RectTransform readableDocumentRect;
    [SerializeField] private TMP_Text readableTitleText;
    [SerializeField] private TMP_Text readableBodyText;
    [SerializeField] private RawImage readableImage;
    [SerializeField] private AspectRatioFitter readableImageAspect;

    private Coroutine readableOverlayRoutine;
    private int readableOverlayOpenedFrame = -1;
    private float readableCloseAllowedAt;
    private BasicFirstPersonController lockedPlayerController;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private bool readableOverlayLocked;
    private bool uiBuilt;

    public SubtitleUI Subtitles => subtitleUI;
    public ObjectiveUI Objectives => objectiveUI;
    public TimerUI Timer => timerUI;
    public InteractionPromptUI Prompt => interactionPromptUI;
    public TMP_Text AnswerInputText => answerInputText;
    public Button[] DigitButtons => digitButtons;
    public Button DeleteButton => deleteButton;
    public Button SubmitButton => submitButton;

    private void Awake()
    {
        Instance = this;
        BuildIfNeeded();
        HideStartupElements();
    }

    private void Update()
    {
        TryCloseReadableTextFromKeyboard();
    }

    public bool TryCloseReadableTextFromKeyboard()
    {
        if (readableOverlay == null || !readableOverlay.activeSelf)
        {
            return false;
        }

        if (Time.frameCount == readableOverlayOpenedFrame || Time.unscaledTime < readableCloseAllowedAt)
        {
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            HideReadableText();
            return true;
        }

        return false;
    }

    public static PhonePuzzleUI EnsureExists()
    {
        PhonePuzzleUI existing = FindFirstObjectByType<PhonePuzzleUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.uiBuilt)
            {
                existing.BuildIfNeeded();
            }

            Instance = existing;
            return existing;
        }

        Canvas canvas = FindCanvas();
        GameObject root = new GameObject("PhonePuzzleUI");
        root.transform.SetParent(canvas.transform, false);
        PhonePuzzleUI ui = root.AddComponent<PhonePuzzleUI>();
        ui.BuildIfNeeded();
        Instance = ui;
        return ui;
    }

    public void HideStartupElements()
    {
        subtitleUI?.Hide();
        timerUI?.Hide();
        interactionPromptUI?.Hide();
        SetAnswerInputVisible(false);
        HideReadableText();
    }

    public void SetAnswerInputVisible(bool visible)
    {
        if (answerInputRoot != null)
        {
            answerInputRoot.SetActive(visible);
        }
    }

    public void SetAnswerText(string answer)
    {
        if (answerInputText != null)
        {
            answerInputText.text = string.IsNullOrEmpty(answer) ? "___" : answer;
        }
    }

    public void ShowReadableText(string title, string body)
    {
        if (readableOverlay == null)
        {
            BuildReadableOverlay(FindCanvas().transform);
        }

        SetReadableImageOnlyMode(false);
        SetReadableImageVisible(false);
        if (readableBodyText != null)
        {
            readableBodyText.gameObject.SetActive(true);
        }

        if (readableTitleText != null)
        {
            readableTitleText.text = title;
        }

        if (readableBodyText != null)
        {
            readableBodyText.text = body;
        }

        readableOverlayOpenedFrame = Time.frameCount;
        readableCloseAllowedAt = Time.unscaledTime + 0.18f;
        EnsureReadableOverlayGroup();
        readableOverlay.SetActive(true);
        ApplyReadableOverlayLock(true);

        if (readableOverlayRoutine != null)
        {
            StopCoroutine(readableOverlayRoutine);
        }

        readableOverlayRoutine = StartCoroutine(AnimateReadableOverlay(true));
    }

    public void ShowReadableImage(string title, Texture texture, string fallbackBody)
    {
        if (texture == null)
        {
            ShowReadableText(title, fallbackBody);
            return;
        }

        if (readableOverlay == null)
        {
            BuildReadableOverlay(FindCanvas().transform);
        }

        SetReadableImageOnlyMode(true);
        if (readableTitleText != null)
        {
            readableTitleText.text = title;
        }

        if (readableBodyText != null)
        {
            readableBodyText.gameObject.SetActive(false);
        }

        EnsureReadableImage();
        if (readableImage != null)
        {
            readableImage.texture = texture;
            readableImage.color = Color.white;
            readableImage.gameObject.SetActive(true);
        }

        if (readableImageAspect != null && texture.width > 0 && texture.height > 0)
        {
            readableImageAspect.aspectRatio = (float)texture.width / texture.height;
        }

        readableOverlayOpenedFrame = Time.frameCount;
        readableCloseAllowedAt = Time.unscaledTime + 0.18f;
        EnsureReadableOverlayGroup();
        readableOverlay.SetActive(true);
        ApplyReadableOverlayLock(true);

        if (readableOverlayRoutine != null)
        {
            StopCoroutine(readableOverlayRoutine);
        }

        readableOverlayRoutine = StartCoroutine(AnimateReadableOverlay(true));
    }

    public void HideReadableText()
    {
        if (readableOverlay == null || !readableOverlay.activeSelf)
        {
            return;
        }

        if (readableOverlayRoutine != null)
        {
            StopCoroutine(readableOverlayRoutine);
        }

        readableOverlayRoutine = StartCoroutine(AnimateReadableOverlay(false));
    }

    public void BuildIfNeeded()
    {
        Canvas canvas = FindCanvas();
        Transform parent = canvas.transform;

        subtitleUI = EnsureSubtitle(parent);
        objectiveUI = EnsureObjective(parent);
        timerUI = EnsureTimer(parent);
        interactionPromptUI = EnsurePrompt(parent);
        EnsureAnswerInput(parent);
        BuildReadableOverlay(parent);
        uiBuilt = true;
    }

    private static Canvas FindCanvas()
    {
        GameObject canvasObject = GameObject.Find("Canvas_PhonePuzzle");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

        if (canvas == null && canvasObject != null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (canvas == null)
        {
            canvasObject = new GameObject("Canvas_PhonePuzzle", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        return canvas;
    }

    private SubtitleUI EnsureSubtitle(Transform parent)
    {
        GameObject obj = FindOrCreateUiObject(parent, "UI_PhoneSubtitles");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 90f);
        rect.sizeDelta = new Vector2(1320f, 130f);

        TMP_Text text = EnsureText(obj, 32f, TextAlignmentOptions.Center, new Color(0.94f, 0.94f, 0.88f, 1f));
        text.textWrappingMode = TextWrappingModes.Normal;

        SubtitleUI component = obj.GetComponent<SubtitleUI>() ?? obj.AddComponent<SubtitleUI>();
        component.Configure(text);
        return component;
    }

    private ObjectiveUI EnsureObjective(Transform parent)
    {
        GameObject obj = FindOrCreateUiObject(parent, "UI_ObjectiveText");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(42f, -36f);
        rect.sizeDelta = new Vector2(720f, 80f);

        TMP_Text text = EnsureText(obj, 27f, TextAlignmentOptions.Left, new Color(0.93f, 0.91f, 0.82f, 1f));
        ObjectiveUI component = obj.GetComponent<ObjectiveUI>() ?? obj.AddComponent<ObjectiveUI>();
        component.Configure(text);
        return component;
    }

    private TimerUI EnsureTimer(Transform parent)
    {
        GameObject obj = FindOrCreateUiObject(parent, "UI_TimerText");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-46f, -34f);
        rect.sizeDelta = new Vector2(240f, 80f);

        TMP_Text text = EnsureText(obj, 34f, TextAlignmentOptions.Right, new Color(0.9f, 0.12f, 0.08f, 1f));
        TimerUI component = obj.GetComponent<TimerUI>() ?? obj.AddComponent<TimerUI>();
        component.Configure(text);
        return component;
    }

    private InteractionPromptUI EnsurePrompt(Transform parent)
    {
        GameObject obj = FindOrCreateUiObject(parent, "UI_InteractionPrompt");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 240f);
        rect.sizeDelta = new Vector2(720f, 70f);

        TMP_Text text = EnsureText(obj, 28f, TextAlignmentOptions.Center, new Color(0.93f, 0.91f, 0.82f, 1f));
        InteractionPromptUI component = obj.GetComponent<InteractionPromptUI>() ?? obj.AddComponent<InteractionPromptUI>();
        component.Configure(text);
        return component;
    }

    private void EnsureAnswerInput(Transform parent)
    {
        answerInputRoot = FindOrCreateUiObject(parent, "UI_AnswerInput");
        Image background = answerInputRoot.GetComponent<Image>() ?? answerInputRoot.AddComponent<Image>();
        background.color = new Color(0.02f, 0.02f, 0.018f, 0.86f);

        RectTransform rect = answerInputRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(460f, 520f);

        GameObject displayObject = FindOrCreateUiObject(answerInputRoot.transform, "UI_AnswerDisplay");
        RectTransform displayRect = displayObject.GetComponent<RectTransform>();
        displayRect.anchorMin = new Vector2(0.5f, 1f);
        displayRect.anchorMax = new Vector2(0.5f, 1f);
        displayRect.pivot = new Vector2(0.5f, 1f);
        displayRect.anchoredPosition = new Vector2(0f, -28f);
        displayRect.sizeDelta = new Vector2(360f, 82f);
        answerInputText = EnsureText(displayObject, 46f, TextAlignmentOptions.Center, new Color(0.92f, 0.04f, 0.03f, 1f));
        answerInputText.text = "___";

        for (int i = 0; i < 10; i++)
        {
            digitButtons[i] = EnsureButton(answerInputRoot.transform, "UI_DigitButton_" + i, i.ToString(), DigitButtonPosition(i));
        }

        deleteButton = EnsureButton(answerInputRoot.transform, "UI_DeleteButton", "DEL", new Vector2(-90f, -424f));
        submitButton = EnsureButton(answerInputRoot.transform, "UI_SubmitButton", "OK", new Vector2(90f, -424f));
    }

    private void BuildReadableOverlay(Transform parent)
    {
        Transform existingOverlay = parent.Find("UI_ReadableListOverlay");
        bool wasActive = existingOverlay != null && existingOverlay.gameObject.activeSelf;
        readableOverlay = FindOrCreateUiObject(parent, "UI_ReadableListOverlay");
        Image background = readableOverlay.GetComponent<Image>() ?? readableOverlay.AddComponent<Image>();
        background.color = new Color(0.01f, 0.009f, 0.008f, 0.78f);
        background.raycastTarget = true;

        EnsureReadableOverlayGroup();

        RectTransform rect = readableOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        GameObject shadowObject = FindOrCreateUiObject(readableOverlay.transform, "ReadableDocumentShadow");
        Image shadow = shadowObject.GetComponent<Image>() ?? shadowObject.AddComponent<Image>();
        shadow.color = new Color(0f, 0f, 0f, 0.52f);
        shadow.raycastTarget = false;
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.anchoredPosition = new Vector2(28f, -32f);
        shadowRect.sizeDelta = new Vector2(800f, 860f);
        shadowRect.localRotation = Quaternion.Euler(0f, 0f, -1.5f);

        GameObject documentObject = FindOrCreateUiObject(readableOverlay.transform, "ReadableDocument");
        Image document = documentObject.GetComponent<Image>() ?? documentObject.AddComponent<Image>();
        document.color = new Color(0.72f, 0.66f, 0.49f, 1f);
        document.raycastTarget = false;
        readableDocumentRect = documentObject.GetComponent<RectTransform>();
        readableDocumentRect.anchorMin = new Vector2(0.5f, 0.5f);
        readableDocumentRect.anchorMax = new Vector2(0.5f, 0.5f);
        readableDocumentRect.pivot = new Vector2(0.5f, 0.5f);
        readableDocumentRect.anchoredPosition = new Vector2(0f, 6f);
        readableDocumentRect.sizeDelta = new Vector2(760f, 830f);
        readableDocumentRect.localRotation = Quaternion.Euler(0f, 0f, -1.5f);

        MoveLegacyReadableChild("ReadableTitle", readableDocumentRect);
        MoveLegacyReadableChild("ReadableBody", readableDocumentRect);
        MoveLegacyReadableChild("ReadableHint", readableDocumentRect);

        AddReadableDocumentWear(readableDocumentRect);

        GameObject titleObject = FindOrCreateUiObject(readableDocumentRect, "ReadableTitle");
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -74f);
        titleRect.sizeDelta = new Vector2(590f, 82f);
        readableTitleText = EnsureText(titleObject, 44f, TextAlignmentOptions.Center, new Color(0.07f, 0.048f, 0.03f, 1f));
        readableTitleText.fontStyle = FontStyles.Bold;

        GameObject bodyObject = FindOrCreateUiObject(readableDocumentRect, "ReadableBody");
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, -16f);
        bodyRect.sizeDelta = new Vector2(540f, 520f);
        readableBodyText = EnsureText(bodyObject, 38f, TextAlignmentOptions.TopLeft, new Color(0.08f, 0.055f, 0.035f, 1f));
        readableBodyText.fontStyle = FontStyles.Bold;
        readableBodyText.enableAutoSizing = true;
        readableBodyText.fontSizeMin = 28f;
        readableBodyText.fontSizeMax = 38f;
        readableBodyText.textWrappingMode = TextWrappingModes.Normal;
        readableBodyText.overflowMode = TextOverflowModes.Truncate;

        EnsureReadableImage();

        GameObject hintObject = FindOrCreateUiObject(readableDocumentRect, "ReadableHint");
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 34f);
        hintRect.sizeDelta = new Vector2(520f, 46f);
        TMP_Text hintText = EnsureText(hintObject, 20f, TextAlignmentOptions.Center, new Color(0.18f, 0.12f, 0.075f, 0.82f));
        hintText.text = "E / Esc fermer";

        readableOverlay.SetActive(wasActive);
    }

    private void MoveLegacyReadableChild(string childName, RectTransform newParent)
    {
        if (readableOverlay == null || newParent == null)
        {
            return;
        }

        Transform existing = readableOverlay.transform.Find(childName);
        if (existing != null && existing.parent != newParent)
        {
            existing.SetParent(newParent, false);
        }
    }

    private void AddReadableDocumentWear(RectTransform documentRoot)
    {
        EnsureWearPanel(documentRoot, "Readable_Top_Grime", new Color(0.13f, 0.08f, 0.035f, 0.16f), new Vector2(0f, 318f), new Vector2(620f, 58f), -1f);
        EnsureWearPanel(documentRoot, "Readable_Left_Stain", new Color(0.11f, 0.065f, 0.025f, 0.18f), new Vector2(-266f, -8f), new Vector2(54f, 560f), 2f);
        EnsureWearPanel(documentRoot, "Readable_Blood_Drag", new Color(0.42f, 0.004f, 0.002f, 0.38f), new Vector2(182f, 178f), new Vector2(210f, 28f), -12f);
        EnsureWearPanel(documentRoot, "Readable_Blood_Drop", new Color(0.34f, 0.002f, 0.001f, 0.46f), new Vector2(-214f, -162f), new Vector2(70f, 126f), 4f);
        EnsureWearPanel(documentRoot, "Readable_Fold_Line", new Color(0.05f, 0.035f, 0.022f, 0.13f), new Vector2(36f, 0f), new Vector2(5f, 670f), 7f);
    }

    private void EnsureReadableImage()
    {
        if (readableDocumentRect == null)
        {
            return;
        }

        GameObject imageObject = FindOrCreateUiObject(readableDocumentRect, "ReadableImage");
        readableImage = imageObject.GetComponent<RawImage>() ?? imageObject.AddComponent<RawImage>();
        readableImage.raycastTarget = false;

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, -12f);
        imageRect.sizeDelta = new Vector2(610f, 610f);

        readableImageAspect = imageObject.GetComponent<AspectRatioFitter>() ?? imageObject.AddComponent<AspectRatioFitter>();
        readableImageAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        imageObject.SetActive(false);
    }

    private void SetReadableImageVisible(bool visible)
    {
        if (readableImage != null)
        {
            readableImage.gameObject.SetActive(visible);
        }
    }

    private void SetReadableImageOnlyMode(bool imageOnly)
    {
        if (readableOverlay != null)
        {
            Image overlayBackground = readableOverlay.GetComponent<Image>();
            if (overlayBackground != null)
            {
                overlayBackground.color = imageOnly
                    ? new Color(0f, 0f, 0f, 0f)
                    : new Color(0.01f, 0.009f, 0.008f, 0.78f);
            }

            SetReadableChildActive(readableOverlay.transform, "ReadableDocumentShadow", !imageOnly);
        }

        if (readableDocumentRect == null)
        {
            return;
        }

        Image documentBackground = readableDocumentRect.GetComponent<Image>();
        if (documentBackground != null)
        {
            documentBackground.enabled = !imageOnly;
        }

        readableDocumentRect.localRotation = imageOnly
            ? Quaternion.identity
            : Quaternion.Euler(0f, 0f, -1.5f);
        readableDocumentRect.sizeDelta = imageOnly
            ? new Vector2(820f, 900f)
            : new Vector2(760f, 830f);

        SetReadableChildActive(readableDocumentRect, "ReadableTitle", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "ReadableBody", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "ReadableHint", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "Readable_Top_Grime", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "Readable_Left_Stain", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "Readable_Blood_Drag", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "Readable_Blood_Drop", !imageOnly);
        SetReadableChildActive(readableDocumentRect, "Readable_Fold_Line", !imageOnly);

        if (readableImage != null)
        {
            RectTransform imageRect = readableImage.rectTransform;
            if (imageOnly)
            {
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.anchoredPosition = Vector2.zero;
                imageRect.sizeDelta = new Vector2(820f, 900f);
            }
            else
            {
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);
                imageRect.anchoredPosition = new Vector2(0f, -12f);
                imageRect.sizeDelta = new Vector2(610f, 610f);
            }
        }
    }

    private static void SetReadableChildActive(Transform parent, string childName, bool active)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = parent.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static void EnsureWearPanel(Transform parent, string objectName, Color color, Vector2 position, Vector2 size, float rotation)
    {
        GameObject obj = FindOrCreateUiObject(parent, objectName);
        Image image = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private IEnumerator AnimateReadableOverlay(bool opening)
    {
        float duration = opening ? 0.18f : 0.12f;
        float elapsed = 0f;

        CanvasGroup overlayGroup = EnsureReadableOverlayGroup();
        if (overlayGroup == null)
        {
            yield break;
        }

        Vector3 startScale = readableDocumentRect != null ? readableDocumentRect.localScale : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : Vector3.one * 0.94f;
        float startAlpha = GetReadableOverlayAlpha(overlayGroup, opening ? 0f : 1f);
        float endAlpha = opening ? 1f : 0f;

        if (opening)
        {
            startScale = Vector3.one * 0.82f;
            startAlpha = 0f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            overlayGroup = SetReadableOverlayAlpha(overlayGroup, Mathf.Lerp(startAlpha, endAlpha, t));
            if (overlayGroup == null)
            {
                yield break;
            }

            if (readableDocumentRect != null)
            {
                readableDocumentRect.localScale = Vector3.Lerp(startScale, endScale, t);
            }

            yield return null;
        }

        SetReadableOverlayAlpha(overlayGroup, endAlpha);
        if (readableDocumentRect != null)
        {
            readableDocumentRect.localScale = endScale;
        }

        if (!opening)
        {
            readableOverlay.SetActive(false);
            readableOverlayOpenedFrame = -1;
            ApplyReadableOverlayLock(false);
        }

        readableOverlayRoutine = null;
    }

    private CanvasGroup EnsureReadableOverlayGroup()
    {
        if (readableOverlay == null)
        {
            return null;
        }

        CanvasGroup group = readableOverlay.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = readableOverlay.AddComponent<CanvasGroup>();
        }

        try
        {
            group.blocksRaycasts = true;
            group.interactable = false;
        }
        catch (MissingComponentException)
        {
            readableOverlayGroup = null;
            group = readableOverlay.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            group.interactable = false;
        }

        readableOverlayGroup = group;
        return group;
    }

    private float GetReadableOverlayAlpha(CanvasGroup group, float fallback)
    {
        if (group == null)
        {
            group = EnsureReadableOverlayGroup();
        }

        if (group == null)
        {
            return fallback;
        }

        try
        {
            return group.alpha;
        }
        catch (MissingComponentException)
        {
            readableOverlayGroup = null;
            group = EnsureReadableOverlayGroup();
            if (group == null)
            {
                return fallback;
            }

            try
            {
                return group.alpha;
            }
            catch (MissingComponentException)
            {
                readableOverlayGroup = null;
                return fallback;
            }
        }
    }

    private CanvasGroup SetReadableOverlayAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            group = EnsureReadableOverlayGroup();
        }

        if (group == null)
        {
            return null;
        }

        try
        {
            group.alpha = alpha;
            return group;
        }
        catch (MissingComponentException)
        {
            readableOverlayGroup = null;
            group = EnsureReadableOverlayGroup();
            if (group == null)
            {
                return null;
            }

            try
            {
                group.alpha = alpha;
                return group;
            }
            catch (MissingComponentException)
            {
                readableOverlayGroup = null;
                return null;
            }
        }
    }

    private void ApplyReadableOverlayLock(bool locked)
    {
        if (locked)
        {
            if (readableOverlayLocked)
            {
                return;
            }

            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            lockedPlayerController = FindFirstObjectByType<BasicFirstPersonController>();
            if (lockedPlayerController != null)
            {
                lockedPlayerController.SetExternalControlLock(true);
            }

            readableOverlayLocked = true;
            return;
        }

        if (!readableOverlayLocked)
        {
            return;
        }

        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisible;

        if (lockedPlayerController != null)
        {
            lockedPlayerController.SetExternalControlLock(false);
        }

        readableOverlayLocked = false;
        lockedPlayerController = null;
    }

    private static GameObject FindOrCreateUiObject(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TMP_Text EnsureText(GameObject obj, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        TMP_Text text = obj.GetComponent<TMP_Text>() ?? obj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button EnsureButton(Transform parent, string objectName, string label, Vector2 anchoredPosition)
    {
        GameObject obj = FindOrCreateUiObject(parent, objectName);
        Image image = obj.GetComponent<Image>() ?? obj.AddComponent<Image>();
        image.color = new Color(0.12f, 0.115f, 0.1f, 0.96f);

        Button button = obj.GetComponent<Button>() ?? obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.2f, 0.19f, 0.16f, 1f);
        colors.pressedColor = new Color(0.36f, 0.04f, 0.03f, 1f);
        button.colors = colors;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(84f, 70f);

        GameObject labelObject = FindOrCreateUiObject(obj.transform, "Label");
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text text = EnsureText(labelObject, 27f, TextAlignmentOptions.Center, new Color(0.92f, 0.89f, 0.78f, 1f));
        text.text = label;
        return button;
    }

    private static Vector2 DigitButtonPosition(int digit)
    {
        if (digit == 0)
        {
            return new Vector2(0f, -354f);
        }

        int index = digit - 1;
        int row = index / 3;
        int column = index % 3;
        return new Vector2(-96f + (column * 96f), -128f - (row * 76f));
    }
}

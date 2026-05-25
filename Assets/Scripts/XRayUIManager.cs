using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XRayUIManager : MonoBehaviour
{
    public static XRayUIManager Instance { get; private set; }
    public static bool IsUiOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

    public GameObject panel;
    public Image xrayImage;
    public TMP_Dropdown patientDropdown;
    public Button validateButton;

    private Canvas rootCanvas;
    private TMP_Text titleText;
    private TMP_Text promptText;
    private TMP_Text statusText;
    private GameObject toastPanel;
    private TMP_Text toastText;
    private float toastHideAt;
    private XRayItem currentXRay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("XRayUIRoot");
        root.AddComponent<XRayUIManager>();
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
        BuildUiIfNeeded();

        validateButton.onClick.RemoveListener(OnValidate);
        validateButton.onClick.AddListener(OnValidate);
        Hide();
    }

    private void Update()
    {
        if (toastPanel != null && toastPanel.activeSelf && Time.unscaledTime >= toastHideAt)
        {
            toastPanel.SetActive(false);
        }

        if (IsUiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void Show(XRayItem xray)
    {
        if (xray == null)
        {
            return;
        }

        BuildUiIfNeeded();
        XRayPuzzleManager.Instance?.RefreshReferences();

        currentXRay = xray;
        panel.SetActive(true);
        xrayImage.sprite = xray.xraySprite;
        xrayImage.preserveAspect = true;
        statusText.text = string.Empty;
        FillPatientDropdown();
        SetCursorUnlocked();
    }

    public void OnValidate()
    {
        if (currentXRay == null)
        {
            return;
        }

        int selected = patientDropdown.value + 1;
        if (selected == currentXRay.correctPatientID)
        {
            bool wasSolved = currentXRay.isSolved;
            currentXRay.MarkSolvedVisual();
            if (!wasSolved)
            {
                XRayPuzzleManager.Instance?.OnCorrect();
            }

            Hide();
            return;
        }

        ShowToast("MAUVAISE ASSOCIATION", new Color(1f, 0.25f, 0.25f, 1f), 2f);
        XRayPuzzleManager.Instance?.TriggerHorror();
        Hide();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        currentXRay = null;
        SetCursorLocked();
    }

    public void ShowToast(string message, Color color, float duration)
    {
        BuildUiIfNeeded();
        toastText.text = message;
        toastText.color = color;
        toastPanel.SetActive(true);
        toastHideAt = Time.unscaledTime + duration;
    }

    private void FillPatientDropdown()
    {
        patientDropdown.ClearOptions();
        PatientFolder[] folders = XRayPuzzleManager.Instance != null
            ? XRayPuzzleManager.Instance.allFolders.Where(folder => folder != null).OrderBy(folder => folder.patientID).ToArray()
            : FindObjectsByType<PatientFolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(folder => folder.patientID).ToArray();

        patientDropdown.AddOptions(folders
            .Select(folder => $"Patient {folder.patientID:00} — {folder.patientName}")
            .ToList());

        patientDropdown.value = 0;
        patientDropdown.RefreshShownValue();
    }

    private void BuildUiIfNeeded()
    {
        if (rootCanvas != null && panel != null && xrayImage != null && patientDropdown != null && validateButton != null)
        {
            return;
        }

        rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 630;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (panel == null)
        {
            panel = CreatePanel(rootCanvas.transform, "XRayPanel", new Vector2(920f, 620f));
        }

        if (titleText == null)
        {
            titleText = CreateText(panel.transform, "TitleText", "RADIOGRAPHIE", 32f, FontStyles.Bold, new Vector2(0.05f, -26f), new Vector2(0.95f, -70f), TextAlignmentOptions.Center);
        }

        if (xrayImage == null)
        {
            GameObject imageObject = new GameObject("XRayImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(panel.transform, false);
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0f, 0f);
            imageRect.anchorMax = new Vector2(0.54f, 1f);
            imageRect.offsetMin = new Vector2(28f, 90f);
            imageRect.offsetMax = new Vector2(-18f, -90f);
            xrayImage = imageObject.GetComponent<Image>();
            xrayImage.color = Color.white;
        }

        if (promptText == null)
        {
            promptText = CreateText(
                panel.transform,
                "PromptText",
                "A quel patient appartient cette radio ?",
                24f,
                FontStyles.Bold,
                new Vector2(0.58f, -132f),
                new Vector2(0.96f, -182f),
                TextAlignmentOptions.Center);
        }

        if (patientDropdown == null)
        {
            patientDropdown = CreateDropdown(panel.transform);
        }

        if (validateButton == null)
        {
            validateButton = CreateButton(panel.transform, "ValidateButton", "VALIDER", new Vector2(0.77f, 0f), new Vector2(200f, 50f), new Vector2(0f, 42f));
        }

        if (statusText == null)
        {
            statusText = CreateText(panel.transform, "StatusText", string.Empty, 24f, FontStyles.Bold, new Vector2(0.58f, -468f), new Vector2(0.96f, -516f), TextAlignmentOptions.Center);
        }

        if (toastPanel == null)
        {
            toastPanel = CreatePanel(rootCanvas.transform, "XRayToast", new Vector2(520f, 62f));
            RectTransform toastRect = toastPanel.GetComponent<RectTransform>();
            toastRect.anchoredPosition = new Vector2(0f, -260f);
            toastText = CreateText(toastPanel.transform, "ToastText", string.Empty, 24f, FontStyles.Bold, new Vector2(0.02f, -8f), new Vector2(0.98f, -54f), TextAlignmentOptions.Center);
            toastPanel.SetActive(false);
        }
    }

    private GameObject CreatePanel(Transform parent, string objectName, Vector2 size)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.03f, 0.94f);
        return panelObject;
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string initialText,
        float fontSize,
        FontStyles style,
        Vector2 normalizedTopLeft,
        Vector2 normalizedBottomRight,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(normalizedTopLeft.x, 1f);
        rect.anchorMax = new Vector2(normalizedBottomRight.x, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, normalizedBottomRight.y);
        rect.offsetMax = new Vector2(0f, normalizedTopLeft.y);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private TMP_Dropdown CreateDropdown(Transform parent)
    {
        GameObject dropdownObject = new GameObject("PatientDropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        dropdownObject.transform.SetParent(parent, false);
        RectTransform rect = dropdownObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.6f, 0.5f);
        rect.anchorMax = new Vector2(0.94f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 52f);
        rect.anchoredPosition = new Vector2(0f, 14f);
        dropdownObject.GetComponent<Image>().color = new Color(0.17f, 0.17f, 0.17f, 1f);

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(dropdownObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 8f);
        labelRect.offsetMax = new Vector2(-38f, -8f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = 22f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        dropdown.captionText = label;

        GameObject arrowObject = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        arrowObject.transform.SetParent(dropdownObject.transform, false);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(30f, 0f);
        arrowRect.anchoredPosition = new Vector2(-6f, 0f);
        TextMeshProUGUI arrow = arrowObject.GetComponent<TextMeshProUGUI>();
        arrow.text = "v";
        arrow.fontSize = 22f;
        arrow.color = Color.white;
        arrow.alignment = TextAlignmentOptions.Center;

        GameObject templateObject = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateObject.transform.SetParent(dropdownObject.transform, false);
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 220f);
        templateRect.anchoredPosition = new Vector2(0f, -56f);
        Image templateImage = templateObject.GetComponent<Image>();
        templateImage.color = new Color(0.09f, 0.09f, 0.09f, 0.98f);
        templateObject.SetActive(false);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(templateObject.transform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject itemObject = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(CanvasRenderer), typeof(Image));
        itemObject.transform.SetParent(contentObject.transform, false);
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 42f);
        itemObject.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);

        GameObject itemLabelObject = new GameObject("Item Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        itemLabelObject.transform.SetParent(itemObject.transform, false);
        RectTransform itemLabelRect = itemLabelObject.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(12f, 4f);
        itemLabelRect.offsetMax = new Vector2(-12f, -4f);
        TextMeshProUGUI itemLabel = itemLabelObject.GetComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 20f;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.Left;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.targetGraphic = dropdownObject.GetComponent<Image>();

        return dropdown;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
        Button button = buttonObject.GetComponent<Button>();

        GameObject textObject = new GameObject($"{objectName}_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private void SetCursorUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetCursorLocked()
    {
        if (!PatientUIManager.IsUiOpen && !KeypadUIManager.IsUiOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

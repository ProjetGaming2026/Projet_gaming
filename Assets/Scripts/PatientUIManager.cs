using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatientUIManager : MonoBehaviour
{
    public static PatientUIManager Instance { get; private set; }
    public static bool IsUiOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI ageText;
    public TextMeshProUGUI diseaseText;
    public TextMeshProUGUI notesText;
    public Button closeButton;

    private Canvas rootCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("PatientUIRoot");
        root.AddComponent<PatientUIManager>();
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

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    public void Show(PatientFolder patient)
    {
        if (patient == null)
        {
            return;
        }

        BuildUiIfNeeded();

        patient.isRead = true;
        nameText.text = $"Nom : {patient.patientName}";
        ageText.text = $"Age : {patient.patientAge} ans";
        diseaseText.text = $"Maladie : {patient.disease}";
        notesText.text = $"Notes :\n{patient.medicalNotes}";

        panel.SetActive(true);
        SetCursorUnlocked();
        Debug.Log($"[PatientUIManager] Showing patient file #{patient.patientID:00}.", this);
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        SetCursorLocked();
    }

    // Legacy compatibility for the old patient-folder implementation.
    public void ShowPatient(PatientFolder patient, Transform viewer)
    {
        Show(patient);
    }

    // Legacy compatibility for older callers.
    public void HidePatient()
    {
        Hide();
    }

    private void BuildUiIfNeeded()
    {
        if (rootCanvas != null && panel != null && nameText != null && notesText != null && closeButton != null)
        {
            return;
        }

        rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 620;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (panel == null)
        {
            panel = CreatePanel(rootCanvas.transform, "PatientPanel", new Vector2(700f, 500f));
        }

        if (nameText == null)
        {
            nameText = CreateText(panel.transform, "NameText", 28f, FontStyles.Bold, new Vector2(24f, -26f), new Vector2(-24f, -68f));
        }

        if (ageText == null)
        {
            ageText = CreateText(panel.transform, "AgeText", 24f, FontStyles.Normal, new Vector2(24f, -82f), new Vector2(-24f, -118f));
        }

        if (diseaseText == null)
        {
            diseaseText = CreateText(panel.transform, "DiseaseText", 24f, FontStyles.Normal, new Vector2(24f, -128f), new Vector2(-24f, -184f));
        }

        if (notesText == null)
        {
            notesText = CreateText(panel.transform, "NotesText", 22f, FontStyles.Normal, new Vector2(24f, -194f), new Vector2(-24f, -86f), TextAlignmentOptions.TopLeft);
            notesText.enableWordWrapping = true;
            notesText.overflowMode = TextOverflowModes.Overflow;
        }

        if (closeButton == null)
        {
            closeButton = CreateButton(panel.transform, "CloseButton", "FERMER", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(180f, 48f), new Vector2(0f, 28f));
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

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.03f, 0.03f, 0.03f, 0.94f);
        return panelObject;
    }

    private TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        float fontSize,
        FontStyles style,
        Vector2 topLeftOffset,
        Vector2 bottomRightOffset,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(topLeftOffset.x, bottomRightOffset.y);
        rect.offsetMax = new Vector2(bottomRightOffset.x, topLeftOffset.y);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        return text;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);
        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject($"{objectName}_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private void SetCursorUnlocked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetCursorLocked()
    {
        if (!KeypadUIManager.IsUiOpen && !XRayUIManager.IsUiOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

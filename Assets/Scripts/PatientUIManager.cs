using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatientUIManager : MonoBehaviour
{
    private static readonly Vector2 PanelSize = new Vector2(1120f, 680f);
    private static readonly Vector2 CardSize = new Vector2(490f, 210f);
    private const float CardTextSize = 24f;
    private const float HeaderTextSize = 34f;
    public static PatientUIManager Instance { get; private set; }
    public static bool IsUiOpen => Instance != null && Instance.panel != null && Instance.panel.activeSelf;

    [Header("UI")]
    public GameObject panel;
    public Button closeButton;

    private Canvas rootCanvas;
    private TextMeshProUGUI headerText;
    private readonly TextMeshProUGUI[] patientCards = new TextMeshProUGUI[4];

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

    private void Update()
    {
        if (IsUiOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    // Legacy single-patient entry point kept for compatibility, but now the gameplay
    // shows the four patient dossiers together so the player can memorize the order.
    public void Show(PatientFolder patient)
    {
        ShowAllOrdered();
    }

    public void ShowAllOrdered()
    {
        BuildUiIfNeeded();

        PatientFolder[] folders = FindObjectsByType<PatientFolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PatientFolder folder in folders)
        {
            if (folder != null)
            {
                folder.isRead = true;
            }
        }

        XRayPuzzleManager.Instance?.MarkFoldersRead();

        for (int i = 0; i < patientCards.Length; i++)
        {
            patientCards[i].text = BuildPatientCardText(i + 1);
        }

        panel.SetActive(true);
        SetCursorUnlocked();
        Debug.Log("[PatientUIManager] Showing ordered patient dossier board.", this);
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        SetCursorLocked();
    }

    public void ShowPatient(PatientFolder patient, Transform viewer)
    {
        ShowAllOrdered();
    }

    public void HidePatient()
    {
        Hide();
    }

    private void BuildUiIfNeeded()
    {
        if (rootCanvas != null && panel != null && closeButton != null && headerText != null && patientCards.All(card => card != null))
        {
            return;
        }

        rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 620;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (panel == null)
        {
            panel = CreatePanel(rootCanvas.transform, "PatientPanel", PanelSize);
        }
        else
        {
            panel.GetComponent<RectTransform>().sizeDelta = PanelSize;
        }

        if (headerText == null)
        {
            headerText = CreateText(
                panel.transform,
                "HeaderText",
                HeaderTextSize,
                FontStyles.Bold,
                new Vector2(32f, -24f),
                new Vector2(-32f, -78f),
                TextAlignmentOptions.Center);
            headerText.text = "DOSSIERS PATIENTS";
        }
        else
        {
            headerText.fontSize = HeaderTextSize;
        }

        EnsurePatientCards(panel.transform);

        if (closeButton == null)
        {
            closeButton = CreateButton(
                panel.transform,
                "CloseButton",
                "FERMER",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(220f, 56f),
                new Vector2(0f, 34f));
        }
    }

    private void EnsurePatientCards(Transform parent)
    {
        Vector2[] positions =
        {
            new Vector2(-260f, 120f),
            new Vector2(260f, 120f),
            new Vector2(-260f, -115f),
            new Vector2(260f, -115f),
        };

        for (int i = 0; i < patientCards.Length; i++)
        {
            string cardName = $"PatientCard_{i + 1:00}";
            Transform existing = parent.Find(cardName);
            GameObject cardPanel = existing != null ? existing.gameObject : CreateCardPanel(parent, cardName, CardSize, positions[i]);

            RectTransform cardRect = cardPanel.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = CardSize;
            cardRect.anchoredPosition = positions[i];

            TextMeshProUGUI cardText = cardPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (cardText == null)
            {
                cardText = CreateCardText(cardPanel.transform, $"{cardName}_Text");
            }
            else
            {
                cardText.fontSize = CardTextSize;
                RectTransform textRect = cardText.GetComponent<RectTransform>();
                textRect.offsetMin = new Vector2(28f, 24f);
                textRect.offsetMax = new Vector2(-28f, -24f);
            }

            patientCards[i] = cardText;
        }
    }

    private string BuildPatientCardText(int patientIndex)
    {
        return patientIndex switch
        {
            1 => "DOSSIER 01\nPatient A-17\nSymptome : respiration difficile\nZone : cage thoracique\nOrdre : 1",
            2 => "DOSSIER 02\nPatient B-09\nSymptome : incapacite de marcher\nZone : pied\nOrdre : 2",
            3 => "DOSSIER 03\nPatient C-31\nSymptome : perte de memoire\nZone : crane\nOrdre : 3",
            4 => "DOSSIER 04\nPatient D-44\nSymptome : dos brise\nZone : colonne vertebrale\nOrdre : 4",
            _ => string.Empty
        };
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
        image.color = new Color(0.02f, 0.02f, 0.02f, 0.95f);
        return panelObject;
    }

    private GameObject CreateCardPanel(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject cardObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = cardObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        return cardObject;
    }

    private TextMeshProUGUI CreateCardText(Transform parent, string objectName)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(28f, 24f);
        rect.offsetMax = new Vector2(-28f, -24f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = CardTextSize;
        text.fontStyle = FontStyles.Normal;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
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
        text.textWrappingMode = TextWrappingModes.Normal;
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
        labelText.fontSize = 18f;
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

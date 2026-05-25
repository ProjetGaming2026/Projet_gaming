using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PatientDossier : MonoBehaviour, IInteractable
{
    [Header("Patient Record")]
    [SerializeField] private string patientName = "UNKNOWN";
    [SerializeField] private string patientId = "000000";
    [SerializeField] private string diagnosis = "Non renseigne";
    [SerializeField] private string riskLevel = "Observation";
    [SerializeField] private string observations = "Aucune note disponible.";
    [SerializeField] private string treatment = "Aucun traitement note.";
    [SerializeField] private string administrativeNote = "Archive incomplete.";
    [SerializeField] private bool criticalFile;

    [Header("Interaction")]
    [SerializeField] private string prompt = "Appuie sur E pour lire le dossier";

    private GameObject dossierView;
    private BasicFirstPersonController playerController;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;
    private bool isOpen;
    private int openedFrame = -1;
    private float closeAllowedAt;
    private static float interactionBlockedUntil;

    public static bool IsAnyDossierOpen { get; private set; }
    public static bool BlocksInteraction => IsAnyDossierOpen || Time.unscaledTime < interactionBlockedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        IsAnyDossierOpen = false;
        interactionBlockedUntil = 0f;
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            IsAnyDossierOpen = false;
            isOpen = false;
        }
    }

    private void Start()
    {
        EnsureCollider();
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Time.frameCount == openedFrame || Time.unscaledTime < closeAllowedAt)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDossier();
        }
    }

    public void Configure(
        string newPatientName,
        string newPatientId,
        string newDiagnosis,
        string newRiskLevel,
        string newObservations,
        string newTreatment,
        string newAdministrativeNote,
        bool isCriticalFile)
    {
        patientName = newPatientName;
        patientId = newPatientId;
        diagnosis = newDiagnosis;
        riskLevel = newRiskLevel;
        observations = newObservations;
        treatment = newTreatment;
        administrativeNote = newAdministrativeNote;
        criticalFile = isCriticalFile;
    }

    public void Interact()
    {
        Debug.Log("PatientDossier: opening dossier for " + patientName + ".");

        if (isOpen)
        {
            CloseDossier();
            return;
        }

        OpenDossier();
    }

    public string GetInteractPrompt()
    {
        return prompt;
    }

    private void OpenDossier()
    {
        Transform cameraTransform = ResolveCameraTransform();
        if (cameraTransform == null)
        {
            Debug.LogWarning("PatientDossier: no camera found for dossier view.");
            return;
        }

        EnsureDossierView(cameraTransform);
        dossierView.SetActive(true);
        isOpen = true;
        IsAnyDossierOpen = true;
        openedFrame = Time.frameCount;
        closeAllowedAt = Time.unscaledTime + 0.15f;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (playerController != null)
        {
            playerController.SetExternalControlLock(true);
        }
    }

    private void CloseDossier()
    {
        isOpen = false;
        IsAnyDossierOpen = false;
        openedFrame = -1;
        interactionBlockedUntil = Time.unscaledTime + 0.18f;

        if (dossierView != null)
        {
            dossierView.SetActive(false);
        }

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        if (playerController != null)
        {
            playerController.SetExternalControlLock(false);
        }
    }

    private void EnsureDossierView(Transform cameraTransform)
    {
        if (dossierView != null)
        {
            dossierView.transform.SetParent(cameraTransform, false);
            PositionDossierView(dossierView.transform);
            RefreshDossierTexts();
            return;
        }

        dossierView = new GameObject("Held_Patient_Dossier_View", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        dossierView.transform.SetParent(cameraTransform, false);
        PositionDossierView(dossierView.transform);

        Canvas canvas = dossierView.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 235;
        canvas.worldCamera = Camera.main;

        RectTransform canvasRect = dossierView.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(980f, 700f);

        CanvasScaler scaler = dossierView.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2.7f;

        GameObject shadow = CreatePanel("Shadow", dossierView.transform, new Color(0f, 0f, 0f, 0.42f));
        SetRect(shadow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, -22f), new Vector2(840f, 555f));

        GameObject folder = CreatePanel("Folder_Back", dossierView.transform, new Color(0.5f, 0.39f, 0.22f, 1f));
        SetRect(folder.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 560f));

        GameObject spine = CreatePanel("Folder_Spine_Shadow", folder.transform, new Color(0.18f, 0.1f, 0.04f, 0.38f));
        SetRect(spine.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(22f, -24f));

        GameObject bottomWear = CreatePanel("Folder_Bottom_Wear", folder.transform, new Color(0.12f, 0.07f, 0.03f, 0.26f));
        SetRect(bottomWear.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-54f, 18f));

        GameObject tab = CreatePanel("Archive_Tab", folder.transform, criticalFile ? new Color(0.45f, 0.05f, 0.04f, 1f) : new Color(0.62f, 0.5f, 0.31f, 1f));
        SetRect(tab.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -8f), new Vector2(230f, 54f));

        TMP_Text tabText = CreateText(tab.transform, "Tab_Text", "PSY-ARCHIVE", 22f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetRect(tabText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        GameObject paper = CreatePanel("Medical_File_Page", folder.transform, new Color(0.78f, 0.75f, 0.66f, 1f));
        SetRect(paper.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(760f, 470f));

        AddDocumentWear(folder.transform, paper.transform, criticalFile);

        GameObject topRule = CreatePanel("Top_Rule", paper.transform, new Color(0.17f, 0.12f, 0.09f, 1f));
        SetRect(topRule.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(0f, 3f));

        TMP_Text facility = CreateText(paper.transform, "Facility", "HOPITAL DEMO NIVEAU MOYEN - SERVICE PSYCHIATRIQUE", 19f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.09f, 0.07f, 0.05f, 1f));
        SetRect(facility.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(-72f, 32f));

        TMP_Text title = CreateText(paper.transform, "Title", "DOSSIER MEDICAL CONFIDENTIEL", 33f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.1f, 0.07f, 0.05f, 1f));
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-72f, 44f));

        TMP_Text patientBlock = CreateText(paper.transform, "Patient_Block", string.Empty, 23f, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.08f, 0.05f, 1f));
        SetRect(patientBlock.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(-72f, 78f));

        GameObject idStrip = CreatePanel("Patient_Id_Strip", paper.transform, criticalFile ? new Color(0.38f, 0.02f, 0.015f, 0.18f) : new Color(0.14f, 0.1f, 0.06f, 0.13f));
        SetRect(idStrip.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -111f), new Vector2(205f, 66f));

        TMP_Text idText = CreateText(idStrip.transform, "Id_Block", string.Empty, 25f, FontStyles.Bold, TextAlignmentOptions.Center, criticalFile ? new Color(0.42f, 0.015f, 0.01f, 1f) : new Color(0.12f, 0.08f, 0.05f, 1f));
        SetRect(idText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text diagnosisText = CreateText(paper.transform, "Diagnosis", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.13f, 0.09f, 0.06f, 1f));
        SetRect(diagnosisText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -194f), new Vector2(-72f, 118f));

        TMP_Text observationsText = CreateText(paper.transform, "Observations", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.08f, 0.05f, 1f));
        SetRect(observationsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -316f), new Vector2(-72f, 106f));

        TMP_Text adminText = CreateText(paper.transform, "Administrative_Note", string.Empty, 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft, criticalFile ? new Color(0.35f, 0.02f, 0.015f, 1f) : new Color(0.15f, 0.1f, 0.06f, 1f));
        SetRect(adminText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(-72f, 58f));

        TMP_Text stamp = CreateText(paper.transform, "Stamp", criticalFile ? "CORRESPONDANCE" : "ARCHIVE", 28f, FontStyles.Bold, TextAlignmentOptions.Center, criticalFile ? new Color(0.55f, 0f, 0f, 0.68f) : new Color(0.15f, 0.11f, 0.08f, 0.42f));
        SetRect(stamp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-156f, -128f), new Vector2(260f, 54f));
        stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);

        TMP_Text hint = CreateText(folder.transform, "Hint", "E / Esc fermer", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.18f, 0.11f, 0.06f, 1f));
        SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(320f, 34f));

        RefreshDossierTexts();
        dossierView.SetActive(false);
    }

    private void RefreshDossierTexts()
    {
        if (dossierView == null)
        {
            return;
        }

        SetText("Patient_Block", "<b>PATIENT</b>\nNom : " + patientName + "\nNiveau de risque : " + riskLevel);
        SetText("Id_Block", "ID DOSSIER\n" + patientId);
        SetText("Diagnosis", "<b>DIAGNOSTIC PSYCHIATRIQUE</b>\n" + diagnosis + "\n\n<b>TRAITEMENT PRESCRIT</b>\n" + treatment);
        SetText("Observations", "<b>OBSERVATIONS CLINIQUES</b>\n" + observations);
        SetText("Administrative_Note", "<b>NOTE ADMINISTRATIVE</b>\n" + administrativeNote);
    }

    private void SetText(string objectName, string value)
    {
        TMP_Text text = FindText(dossierView.transform, objectName);
        if (text != null)
        {
            text.text = value;
        }
    }

    private TMP_Text FindText(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current.GetComponent<TMP_Text>();
        }

        foreach (Transform child in current)
        {
            TMP_Text found = FindText(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void PositionDossierView(Transform dossierTransform)
    {
        dossierTransform.localPosition = new Vector3(0.02f, -0.04f, 0.62f);
        dossierTransform.localRotation = Quaternion.Euler(4f, -1.5f, 0f);
        dossierTransform.localScale = Vector3.one * 0.00102f;
    }

    private static Transform ResolveCameraTransform()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        Camera camera = FindFirstObjectByType<Camera>();
        return camera != null ? camera.transform : null;
    }

    private void EnsureCollider()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        boxCollider.size = new Vector3(0.62f, 0.22f, 0.46f);
        boxCollider.center = new Vector3(0f, 0.08f, 0f);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void AddDocumentWear(Transform folder, Transform paper, bool critical)
    {
        GameObject folderStain = CreatePanel("Folder_Water_Stain", folder, new Color(0.06f, 0.035f, 0.015f, 0.18f));
        SetRect(folderStain.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, -138f), new Vector2(260f, 110f));
        folderStain.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);

        GameObject leftDirt = CreatePanel("Paper_Left_Grime", paper, new Color(0.16f, 0.09f, 0.035f, 0.16f));
        SetRect(leftDirt.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(44f, -22f));

        GameObject topDirt = CreatePanel("Paper_Top_Grime", paper, new Color(0.18f, 0.11f, 0.045f, 0.13f));
        SetRect(topDirt.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-28f, 32f));

        for (int i = 0; i < 9; i++)
        {
            float x = Mathf.Lerp(-300f, 300f, PseudoRandom(700, i * 5));
            float y = Mathf.Lerp(-178f, 176f, PseudoRandom(700, i * 5 + 1));
            float width = Mathf.Lerp(34f, 112f, PseudoRandom(700, i * 5 + 2));
            float height = Mathf.Lerp(8f, 38f, PseudoRandom(700, i * 5 + 3));
            float alpha = Mathf.Lerp(0.06f, 0.18f, PseudoRandom(700, i * 5 + 4));

            GameObject stain = CreatePanel("Paper_Age_Stain_" + i, paper, new Color(0.18f, 0.095f, 0.035f, alpha));
            SetRect(stain.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(width, height));
            stain.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-28f, 28f, PseudoRandom(900, i)));
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject crease = CreatePanel("Paper_Crease_" + i, paper, new Color(0.08f, 0.055f, 0.035f, 0.12f));
            float x = Mathf.Lerp(-260f, 260f, PseudoRandom(1020, i));
            float height = Mathf.Lerp(150f, 360f, PseudoRandom(1030, i));
            SetRect(crease.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, Mathf.Lerp(-20f, 20f, PseudoRandom(1040, i))), new Vector2(3f, height));
            crease.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-16f, 16f, PseudoRandom(1050, i)));
        }

        if (critical)
        {
            GameObject bloodSmear = CreatePanel("Critical_Red_Smear", paper, new Color(0.45f, 0.005f, 0f, 0.22f));
            SetRect(bloodSmear.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-58f, 78f), new Vector2(190f, 22f));
            bloodSmear.transform.localRotation = Quaternion.Euler(0f, 0f, -7f);
        }
    }

    private static float PseudoRandom(int seed, int index)
    {
        float value = Mathf.Sin(seed * 12.9898f + index * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

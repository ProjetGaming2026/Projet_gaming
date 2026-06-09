using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class XRayPuzzleManager : MonoBehaviour
{
    public static XRayPuzzleManager Instance { get; private set; }

    [Header("References")]
    public PatientFolder[] allFolders;
    public XRayItem[] allXRays;
    public Animator exitDoorAnimator;
    public Light roomLight;

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip horrorSound;

    [Header("Horror")]
    public GameObject monsterPrefab;
    public Transform monsterSpawnPoint;

    [Header("Task Progression")]
    [SerializeField] private bool countAsTask = true;
    public int solvedCount = 0;
    public bool horrorActive = false;
    public bool puzzleSolved = false;
    public bool foldersRead = false;

    private AudioSource audioSource;
    private bool taskRegistered;
    private readonly List<int> selectionOrder = new List<int>(4);
    private static readonly int[] ExpectedOrder = { 2, 3, 1, 4 };
    private const float PuzzleDurationSeconds = 60f;
    private const float TimerTriggerDistance = 2.75f;

    private Canvas timerCanvas;
    private GameObject timerPanel;
    private TextMeshProUGUI timerText;
    private bool timerRunning;
    private bool timerPrimed;
    private bool waitingForRoomReentry;
    private float timeRemaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("XRayPuzzleManager");
        root.AddComponent<XRayPuzzleManager>();
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
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        RefreshReferences();
        // Keep the XRay transforms exactly as placed in the scene.
        // The setup tool is responsible for editor-time placement;
        // runtime must not move them when Play starts.
        EnsureXRayVisuals();
        EnsureAccentLight();
        BuildTimerUiIfNeeded();
        HideTimer();
    }

    private void Update()
    {
        RefreshReferences();

        if (!puzzleSolved && CodePuzzleManager.Instance != null && CodePuzzleManager.Instance.IsPuzzleSolved)
        {
            TryStartTimerWhenPlayerEntersRoom();
        }

        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();

            if (timeRemaining <= 0f)
            {
                HandleTimeExpired();
            }
        }
    }

    public void RefreshReferences()
    {
        allFolders = FindObjectsByType<PatientFolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderBy(folder => folder.patientID)
            .ToArray();

        allXRays = FindObjectsByType<XRayItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderBy(xray => xray.xrayID)
            .ToArray();
    }

    public bool HasAnyFolderBeenRead()
    {
        RefreshReferences();
        if (foldersRead)
        {
            return true;
        }

        foldersRead = allFolders != null && allFolders.Any(folder => folder != null && folder.isRead);
        return foldersRead;
    }

    public void MarkFoldersRead()
    {
        foldersRead = true;
        Debug.Log("Patient folders read");
    }

    private void EnsureXRayVisuals()
    {
        if (allXRays == null || allXRays.Length == 0)
        {
            return;
        }

        foreach (XRayItem xray in allXRays)
        {
            if (xray == null || xray.xraySprite == null)
            {
                continue;
            }

            Renderer renderer = xray.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            Material runtimeMaterial = renderer.material;
            runtimeMaterial.shader = ResolveXRayShader(runtimeMaterial.shader);

            Texture texture = xray.xraySprite.texture;
            runtimeMaterial.mainTexture = texture;

            if (runtimeMaterial.HasProperty("_BaseMap"))
            {
                runtimeMaterial.SetTexture("_BaseMap", texture);
            }

            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                runtimeMaterial.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 1f));
            }

            if (runtimeMaterial.HasProperty("_Color"))
            {
                runtimeMaterial.color = new Color(0.95f, 0.98f, 1f, 1f);
            }

            if (runtimeMaterial.HasProperty("_Cull"))
            {
                runtimeMaterial.SetFloat("_Cull", 0f);
            }

            if (runtimeMaterial.HasProperty("_CullMode"))
            {
                runtimeMaterial.SetFloat("_CullMode", 0f);
            }

            if (runtimeMaterial.HasProperty("_EmissionColor"))
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor("_EmissionColor", new Color(0.22f, 0.24f, 0.28f, 1f));
            }

            if (runtimeMaterial.HasProperty("_Smoothness"))
            {
                runtimeMaterial.SetFloat("_Smoothness", 0f);
            }

            if (runtimeMaterial.HasProperty("_Metallic"))
            {
                runtimeMaterial.SetFloat("_Metallic", 0f);
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void EnsureAccentLight()
    {
        if (allXRays == null || allXRays.Length == 0)
        {
            return;
        }

        GameObject lightObject = GameObject.Find("XRayPuzzleAccentLight");
        if (lightObject == null)
        {
            lightObject = new GameObject("XRayPuzzleAccentLight");
            Light accentLight = lightObject.AddComponent<Light>();
            accentLight.type = LightType.Point;
            accentLight.range = 3.5f;
            accentLight.intensity = 0.55f;
            accentLight.color = new Color(0.82f, 0.9f, 1f, 1f);
        }

        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (XRayItem xray in allXRays)
        {
            if (xray == null)
            {
                continue;
            }

            center += xray.transform.position;
            count++;
        }

        if (count == 0)
        {
            return;
        }

        center /= count;
        lightObject.transform.position = center + new Vector3(0.35f, 0f, 0f);
    }

    private Shader ResolveXRayShader(Shader currentShader)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Standard");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Texture");
        if (shader != null)
        {
            return shader;
        }

        return currentShader;
    }

    public void SelectXRay(XRayItem xray)
    {
        if (xray == null || puzzleSolved || horrorActive || !timerRunning)
        {
            return;
        }

        RefreshReferences();

        if (xray.isSolved)
        {
            XRayUIManager.Instance?.ShowToast("Deja selectionne", new Color(0.9f, 0.9f, 0.9f, 1f), 1.2f);
            return;
        }

        int expectedStep = selectionOrder.Count;
        if (expectedStep >= ExpectedOrder.Length)
        {
            return;
        }

        if (xray.xrayID == ExpectedOrder[expectedStep])
        {
            selectionOrder.Add(xray.xrayID);
            xray.MarkSolvedVisual();
            OnCorrect();
            return;
        }

        StartCoroutine(HandleWrongOrder(xray));
    }

    public void OnCorrect()
    {
        if (puzzleSolved)
        {
            return;
        }

        RefreshReferences();
        solvedCount = selectionOrder.Count;
        PlayOneShot(successSound);
        XRayUIManager.Instance?.ShowToast($"{solvedCount}/4 correctes", new Color(0.35f, 1f, 0.4f, 1f), 1.6f);

        if (solvedCount >= 4)
        {
            StartCoroutine(SuccessSequence());
        }
    }

    public void TriggerHorror()
    {
        if (horrorActive || puzzleSolved)
        {
            return;
        }

        StartCoroutine(HorrorSequence());
    }

    public void ResetSequence()
    {
        selectionOrder.Clear();
        RefreshReferences();

        if (allXRays == null)
        {
            return;
        }

        foreach (XRayItem xray in allXRays)
        {
            if (xray == null)
            {
                continue;
            }

            xray.isSolved = false;
            xray.ResetVisual();
        }

        solvedCount = 0;
    }

    private IEnumerator HandleWrongOrder(XRayItem wrongXRay)
    {
        if (wrongXRay == null || horrorActive || puzzleSolved)
        {
            yield break;
        }

        horrorActive = true;
        wrongXRay.MarkErrorVisual();
        PlayOneShot(horrorSound);
        XRayUIManager.Instance?.ShowToast("Mauvais ordre", new Color(1f, 0.25f, 0.25f, 1f), 1f);
        yield return new WaitForSeconds(1f);
        ResetSequence();
        horrorActive = false;
    }

    public IEnumerator SuccessSequence()
    {
        puzzleSolved = true;
        RegisterTaskProgress();
        timerRunning = false;
        timerPrimed = false;
        HideTimer();
        PlayOneShot(successSound);
        XRayUIManager.Instance?.ShowToast("PUZZLE RESOLU !", new Color(0.35f, 1f, 0.4f, 1f), 2.5f);

        if (roomLight != null)
        {
            float elapsed = 0f;
            float startIntensity = Mathf.Max(0.2f, roomLight.intensity);
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                roomLight.intensity = Mathf.Lerp(startIntensity, 2f, elapsed / 2f);
                yield return null;
            }

            roomLight.intensity = 2f;
        }

        yield return new WaitForSeconds(1f);

        if (exitDoorAnimator != null)
        {
            exitDoorAnimator.SetTrigger("Open");
        }
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
            Debug.LogWarning("[XRayPuzzleManager] PrototypeGameManager not found, XRay task progress was not registered.", this);
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
    public IEnumerator HorrorSequence()
    {
        horrorActive = true;
        PlayOneShot(horrorSound);
        XRayUIManager.Instance?.ShowToast("MAUVAISE ASSOCIATION", new Color(1f, 0.25f, 0.25f, 1f), 2f);

        if (roomLight != null)
        {
            for (int i = 0; i < 6; i++)
            {
                roomLight.enabled = false;
                yield return new WaitForSeconds(0.1f);
                roomLight.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return StartCoroutine(ShakeCamera(0.5f, 0.1f));
        yield return new WaitForSeconds(0.3f);

        if (monsterPrefab != null && monsterSpawnPoint != null)
        {
            Instantiate(monsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
        }

        yield return new WaitForSeconds(10f);
        horrorActive = false;
    }

    private void TryStartTimerWhenPlayerEntersRoom()
    {
        if (timerRunning || timerPrimed || allXRays == null || allXRays.Length == 0 || Camera.main == null)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (XRayItem xray in allXRays)
        {
            if (xray == null)
            {
                continue;
            }

            center += xray.transform.position;
            count++;
        }

        if (count == 0)
        {
            return;
        }

        center /= count;
        float distance = Vector3.Distance(Camera.main.transform.position, center);
        if (distance > TimerTriggerDistance)
        {
            waitingForRoomReentry = false;
            return;
        }

        if (waitingForRoomReentry)
        {
            return;
        }

        timerPrimed = true;
        timerRunning = true;
        timeRemaining = PuzzleDurationSeconds;
        ShowTimer();
        UpdateTimerDisplay();
    }

    private void HandleTimeExpired()
    {
        timerRunning = false;
        timerPrimed = false;
        waitingForRoomReentry = true;
        timeRemaining = 0f;
        UpdateTimerDisplay(true);
        ResetSequence();
        XRayUIManager.Instance?.ShowToast("TEMPS ECOULE", new Color(1f, 0.3f, 0.3f, 1f), 2.2f);
        StartCoroutine(HideTimerAfterDelay());
    }

    private IEnumerator HideTimerAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (!puzzleSolved)
        {
            HideTimer();
        }
    }

    private void BuildTimerUiIfNeeded()
    {
        if (timerCanvas != null && timerPanel != null && timerText != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("XRayTimerCanvas");
        if (existing != null)
        {
            timerCanvas = existing.GetComponent<Canvas>();
            timerPanel = existing.transform.Find("TimerPanel")?.gameObject;
            timerText = timerPanel != null ? timerPanel.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (timerCanvas != null && timerPanel != null && timerText != null)
            {
                return;
            }
        }

        GameObject canvasObject = new GameObject("XRayTimerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        timerCanvas = canvasObject.GetComponent<Canvas>();
        timerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        timerCanvas.sortingOrder = 640;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("TimerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        timerPanel = panelObject;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(280f, 46f);
        panelRect.anchoredPosition = new Vector2(0f, -18f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.03f, 0.03f, 0.78f);

        GameObject textObject = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        timerText = textObject.GetComponent<TextMeshProUGUI>();

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        timerText.alignment = TextAlignmentOptions.Center;
        timerText.fontSize = 20f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        timerText.text = "TEMPS RESTANT : 01:00";
    }

    private void UpdateTimerDisplay(bool expired = false)
    {
        if (timerPanel == null || timerText == null)
        {
            return;
        }

        timerPanel.SetActive(true);

        if (expired)
        {
            timerText.text = "TEMPS ECOULE";
            timerText.color = new Color(1f, 0.32f, 0.32f, 1f);
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"TEMPS RESTANT : {minutes:00}:{seconds:00}";
        timerText.color = totalSeconds <= 10
            ? new Color(1f, 0.35f, 0.35f, 1f)
            : new Color(0.92f, 0.92f, 0.92f, 1f);
    }

    private void ShowTimer()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
        }
    }

    private void HideTimer()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (Camera.main == null)
        {
            yield break;
        }

        Transform target = Camera.main.transform;
        Vector3 originalLocalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * magnitude;
            target.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        target.localPosition = originalLocalPosition;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}

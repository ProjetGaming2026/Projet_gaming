using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class TornMedicalPaperReadable : Interactable
{
    private const string PaperTitle = "PATIENT 317";
    private const string PaperText =
        "PATIENT 317\n" +
        "HEURE D'ADMISSION: 02:__\n" +
        "MEDECIN: H. MAREK\n" +
        "NOTE:\n" +
        "Le patient repetait sans arret:\n" +
        "\"J'etais reveille quand ils m'ont declare mort.\"";

    [Header("Paper")]
    [SerializeField] private bool hideAtSceneStart = true;
    [SerializeField] private bool showTextOnPaper = true;
    [SerializeField] private Canvas paperTextCanvas;
    [SerializeField] private TMP_Text paperWorldText;
    [SerializeField, Min(0.001f)] private float textSurfaceOffset = 0.006f;
    [SerializeField] private Vector2 textCanvasSize = new Vector2(420f, 560f);
    [SerializeField, Min(0.0001f)] private float textWorldScale = 0.0012f;
    [SerializeField, Min(8f)] private float worldTextFontSize = 24f;
    [SerializeField] private Color worldTextColor = new Color(0.055f, 0.035f, 0.022f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip paperPickupSound;

    [Header("Events")]
    public UnityEvent OnPaperRead = new UnityEvent();

    private HeartRoomPuzzle puzzle;
    private bool isVisible;
    private bool hasBeenRead;
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    public bool HasBeenRead => hasBeenRead;

    private void Awake()
    {
        EnsureVisualAndCollider();
        EnsurePaperWorldText();
        if (hideAtSceneStart)
        {
            SetPaperVisible(false);
        }
    }

    public void Configure(HeartRoomPuzzle owner, AudioClip pickupSound)
    {
        puzzle = owner;
        paperPickupSound ??= pickupSound;
        EnsureVisualAndCollider();
        EnsurePaperWorldText();
    }

    public void SetPaperVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
        SetInteractable(visible);
        SetPromptText("Appuie sur E pour lire");

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = visible;
            }
        }

        cachedColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
            {
                cachedColliders[i].enabled = visible;
            }
        }

        if (paperTextCanvas != null)
        {
            paperTextCanvas.gameObject.SetActive(visible && showTextOnPaper);
        }
    }

    public override bool CanInteract => isVisible && base.CanInteract;

    public override string GetInteractPrompt()
    {
        return "Appuie sur E pour lire";
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        hasBeenRead = true;
        PlayOneShot(paperPickupSound);
        PhonePuzzleUI.EnsureExists().ShowReadableText(PaperTitle, PaperText);
        OnPaperRead.Invoke();
        puzzle?.RegisterPaperRead();
        Debug.Log("Puzzle 2 torn medical paper read. Fragment found: 02.");
    }

    private void EnsureVisualAndCollider()
    {
        if (GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.35f, 0.46f, 0.05f);
            box.center = Vector3.zero;
            Debug.LogWarning("TornMedicalPaper had no Collider, so a fallback BoxCollider was added.");
        }

        EnsureAudioSource();
    }

    private void EnsurePaperWorldText()
    {
        if (!showTextOnPaper)
        {
            return;
        }

        if (paperTextCanvas == null)
        {
            Transform existing = transform.Find("TornMedicalPaper_TextCanvas");
            if (existing != null)
            {
                paperTextCanvas = existing.GetComponent<Canvas>();
            }
        }

        if (paperTextCanvas == null)
        {
            GameObject canvasObject = new GameObject("TornMedicalPaper_TextCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, true);
            paperTextCanvas = canvasObject.GetComponent<Canvas>();
            paperTextCanvas.renderMode = RenderMode.WorldSpace;
            paperTextCanvas.overrideSorting = true;
            paperTextCanvas.sortingOrder = 85;
        }

        RectTransform canvasRect = paperTextCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = textCanvasSize;
        paperTextCanvas.transform.localScale = Vector3.one * textWorldScale;
        PositionTextCanvas(canvasRect);

        CanvasScaler scaler = paperTextCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 16f;
        }

        if (paperWorldText == null)
        {
            Transform existingText = paperTextCanvas.transform.Find("MedicalPaper_Text");
            if (existingText != null)
            {
                paperWorldText = existingText.GetComponent<TMP_Text>();
            }
        }

        if (paperWorldText == null)
        {
            GameObject textObject = new GameObject("MedicalPaper_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(paperTextCanvas.transform, false);
            paperWorldText = textObject.GetComponent<TMP_Text>();
        }

        RectTransform textRect = paperWorldText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(34f, 42f);
        textRect.offsetMax = new Vector2(-34f, -42f);

        paperWorldText.text = PaperText;
        paperWorldText.fontSize = worldTextFontSize;
        paperWorldText.enableAutoSizing = true;
        paperWorldText.fontSizeMin = 10f;
        paperWorldText.fontSizeMax = worldTextFontSize;
        paperWorldText.alignment = TextAlignmentOptions.TopLeft;
        paperWorldText.color = worldTextColor;
        paperWorldText.fontStyle = FontStyles.Bold;
        paperWorldText.textWrappingMode = TextWrappingModes.Normal;
        paperWorldText.overflowMode = TextOverflowModes.Truncate;
        paperWorldText.raycastTarget = false;
    }

    private void PositionTextCanvas(RectTransform canvasRect)
    {
        Renderer paperRenderer = FindBestPaperRenderer();
        if (paperRenderer == null || canvasRect == null)
        {
            paperTextCanvas.transform.localPosition = Vector3.zero;
            paperTextCanvas.transform.localRotation = Quaternion.identity;
            return;
        }

        Bounds bounds = paperRenderer.bounds;
        Transform paperTransform = paperRenderer.transform;
        Vector3 paperNormal = ResolvePaperNormal(paperRenderer, paperTransform);
        Vector3 paperUp = ResolvePaperUp(paperTransform, paperNormal);

        paperTextCanvas.transform.position = bounds.center + paperNormal * textSurfaceOffset;
        paperTextCanvas.transform.rotation = Quaternion.LookRotation(-paperNormal, paperUp);
    }

    private Renderer FindBestPaperRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Renderer bestRenderer = null;
        float bestArea = -1f;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || targetRenderer.GetComponent<TMP_Text>() != null)
            {
                continue;
            }

            if (paperTextCanvas != null && targetRenderer.transform.IsChildOf(paperTextCanvas.transform))
            {
                continue;
            }

            Bounds bounds = targetRenderer.bounds;
            float area = Mathf.Max(bounds.size.x * bounds.size.y, bounds.size.x * bounds.size.z, bounds.size.y * bounds.size.z);
            if (area > bestArea)
            {
                bestArea = area;
                bestRenderer = targetRenderer;
            }
        }

        return bestRenderer;
    }

    private static Vector3 ResolvePaperNormal(Renderer paperRenderer, Transform paperTransform)
    {
        Vector3 localSize = ResolveRendererLocalSize(paperRenderer);
        Vector3 scale = paperTransform.lossyScale;
        Vector3 scaledSize = new Vector3(Mathf.Abs(localSize.x * scale.x), Mathf.Abs(localSize.y * scale.y), Mathf.Abs(localSize.z * scale.z));

        Vector3 normal = paperTransform.forward;
        if (scaledSize.x <= scaledSize.y && scaledSize.x <= scaledSize.z)
        {
            normal = paperTransform.right;
        }
        else if (scaledSize.y <= scaledSize.x && scaledSize.y <= scaledSize.z)
        {
            normal = paperTransform.up;
        }

        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer != null && Vector3.Dot(normal, viewer.position - paperRenderer.bounds.center) < 0f)
        {
            normal = -normal;
        }

        return normal.sqrMagnitude > 0.001f ? normal.normalized : paperTransform.forward;
    }

    private static Vector3 ResolvePaperUp(Transform paperTransform, Vector3 paperNormal)
    {
        Vector3 projectedUp = Vector3.ProjectOnPlane(paperTransform.up, paperNormal);
        if (projectedUp.sqrMagnitude < 0.001f)
        {
            projectedUp = Vector3.ProjectOnPlane(Vector3.up, paperNormal);
        }

        return projectedUp.sqrMagnitude > 0.001f ? projectedUp.normalized : Vector3.up;
    }

    private static Vector3 ResolveRendererLocalSize(Renderer paperRenderer)
    {
        if (paperRenderer == null)
        {
            return Vector3.one;
        }

        MeshFilter meshFilter = paperRenderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds.size;
        }

        return paperRenderer.localBounds.size;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.Log("TornMedicalPaper audio clip missing; continuing without audio.");
            return;
        }

        AudioSource source = EnsureAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private AudioSource EnsureAudioSource()
    {
        try
        {
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                return audioSource;
            }
        }
        catch (MissingComponentException)
        {
            audioSource = null;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        return audioSource;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ReadableWallList : Interactable
{
    [Header("Readable List")]
    [SerializeField] private string listTitle = "HOSPITAL LIST";
    [SerializeField, TextArea(4, 12)] private string listBody;
    [SerializeField] private bool hideUntilActive = true;
    [SerializeField] private bool showOverlayOnInspect = true;
    [SerializeField] private WaitingRoomPuzzle waitingRoomPuzzle;

    [Header("World Text")]
    [SerializeField] private Canvas textCanvas;
    [SerializeField] private TMP_Text listText;
    [SerializeField] private bool autoCorrectPaperTextLayout = true;
    [SerializeField, Min(8f)] private float fontSize = 48f;
    [SerializeField] private Vector2 canvasPixelSize = new Vector2(640f, 860f);
    [SerializeField, Range(0.1f, 1.2f)] private float canvasFill = 0.92f;
    [SerializeField] private Vector2 textPadding = new Vector2(70f, 88f);
    [SerializeField] private float surfaceOffset = 0.018f;
    [Tooltip("Toggle this if a specific paper mesh displays the text mirrored from the readable side.")]
    [SerializeField] private bool flipReadableSide = true;

    [Header("Blood Stains")]
    [SerializeField] private bool addBloodStains = true;
    [SerializeField] private Color darkBlood = new Color(0.16f, 0.002f, 0.001f, 0.82f);
    [SerializeField] private Color wetBlood = new Color(0.52f, 0.006f, 0.002f, 0.68f);

    [Header("Events")]
    public UnityEvent OnInspected = new UnityEvent();

    private bool isReadable;
    private bool hasBeenInspected;
    private RectTransform canvasRect;
    private GameObject bloodRoot;
    private Bounds cachedPaperBounds;

    public bool HasBeenInspected => hasBeenInspected;
    public string ListTitle => listTitle;
    public string ListBody => listBody;

    private void Awake()
    {
        EnsureVisuals();
        SetReadable(false);
    }

    private void Start()
    {
        if (waitingRoomPuzzle == null)
        {
            waitingRoomPuzzle = GetComponentInParent<WaitingRoomPuzzle>();
        }
    }

    public void Configure(string title, string body, WaitingRoomPuzzle puzzle)
    {
        listTitle = title;
        listBody = body;
        waitingRoomPuzzle = puzzle;
        EnsureVisuals();
        RefreshText();
    }

    public void SetReadable(bool value)
    {
        isReadable = value;
        SetInteractable(value);
        EnsureVisuals();
        RefreshText();

        if (hideUntilActive)
        {
            SetRenderersVisible(gameObject, value);
        }

        if (textCanvas != null)
        {
            textCanvas.gameObject.SetActive(value || !hideUntilActive);
        }

        if (bloodRoot != null)
        {
            bloodRoot.SetActive(value || !hideUntilActive);
        }

        if (value)
        {
            PositionCanvasOnPaper();
        }
    }

    public override bool CanInteract => isReadable && base.CanInteract;

    public override string GetInteractPrompt()
    {
        return "Press E to inspect";
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        hasBeenInspected = true;
        OnInspected.Invoke();

        if (waitingRoomPuzzle == null)
        {
            waitingRoomPuzzle = GetComponentInParent<WaitingRoomPuzzle>();
        }

        waitingRoomPuzzle?.RegisterListInspected(this);

        if (showOverlayOnInspect)
        {
            PhonePuzzleUI.EnsureExists().ShowReadableText(listTitle, listBody);
        }

        Debug.Log("Readable wall list inspected: " + listTitle);
    }

    private void EnsureVisuals()
    {
        NormalizeLayoutSettings();
        DisableLegacyGeneratedVisuals();
        EnsureCanvas();
        EnsureBloodStains();
        EnsureCollider();
        RefreshText();
    }

    private void EnsureCanvas()
    {
        if (textCanvas == null)
        {
            Transform existing = transform.Find("Puzzle1_List_ReadableCanvas");
            if (existing != null)
            {
                textCanvas = existing.GetComponent<Canvas>();
            }
        }

        if (textCanvas == null)
        {
            GameObject canvasObject = new GameObject("Puzzle1_List_ReadableCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, true);
            textCanvas = canvasObject.GetComponent<Canvas>();
            textCanvas.renderMode = RenderMode.WorldSpace;
            textCanvas.overrideSorting = true;
            textCanvas.sortingOrder = 80;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 14f;
        }

        canvasRect = textCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasPixelSize;

        if (listText == null)
        {
            Transform existingText = textCanvas.transform.Find("List_Text");
            if (existingText != null)
            {
                listText = existingText.GetComponent<TMP_Text>();
            }
        }

        if (listText == null)
        {
            GameObject textObject = new GameObject("List_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(textCanvas.transform, false);
            listText = textObject.GetComponent<TMP_Text>();
        }

        RectTransform textRect = listText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textPadding;
        textRect.offsetMax = -textPadding;

        listText.fontSize = fontSize;
        listText.enableAutoSizing = true;
        listText.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
        listText.fontSizeMax = fontSize;
        listText.alignment = TextAlignmentOptions.TopLeft;
        listText.color = new Color(0.035f, 0.026f, 0.018f, 1f);
        listText.fontStyle = FontStyles.Bold;
        listText.textWrappingMode = TextWrappingModes.Normal;
        listText.overflowMode = TextOverflowModes.Truncate;
        listText.raycastTarget = false;
    }

    private void EnsureBloodStains()
    {
        if (!addBloodStains)
        {
            return;
        }

        if (bloodRoot == null)
        {
            Transform existing = textCanvas != null ? textCanvas.transform.Find("Blood_Stains") : null;
            if (existing != null)
            {
                bloodRoot = existing.gameObject;
            }
        }

        if (bloodRoot != null)
        {
            return;
        }

        bloodRoot = new GameObject("Blood_Stains", typeof(RectTransform));
        bloodRoot.transform.SetParent(textCanvas.transform, false);
        bloodRoot.transform.SetSiblingIndex(0);

        RectTransform rootRect = bloodRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        int seed = Mathf.Abs(name.GetHashCode());
        CreateBloodImage("Blood_Smear_Left", new Vector2(-210f, 120f), new Vector2(190f, 70f), -14f, seed + 11);
        CreateBloodImage("Blood_Smear_Bottom", new Vector2(170f, -230f), new Vector2(170f, 62f), 22f, seed + 31);
        CreateBloodImage("Blood_Drip", new Vector2(-260f, -40f), new Vector2(58f, 240f), 3f, seed + 53);
        CreateBloodImage("Blood_Fingerprints", new Vector2(245f, 245f), new Vector2(92f, 120f), 8f, seed + 79);
    }

    private void CreateBloodImage(string objectName, Vector2 anchoredPosition, Vector2 size, float rotation, int seed)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(bloodRoot.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = CreateBloodTexture(seed);
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void RefreshText()
    {
        if (listText != null)
        {
            listText.text = string.IsNullOrEmpty(listBody) ? listTitle : listBody.Replace("\n", "\n\n");
            listText.fontSize = fontSize;
            listText.fontSizeMax = fontSize;
            listText.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
        }
    }

    private void NormalizeLayoutSettings()
    {
        if (!autoCorrectPaperTextLayout)
        {
            return;
        }

        if (fontSize > 58f || fontSize < 34f)
        {
            fontSize = 48f;
        }

        if (canvasPixelSize.x < 600f || canvasPixelSize.y < 800f)
        {
            canvasPixelSize = new Vector2(640f, 860f);
        }

        if (canvasFill > 1f || canvasFill < 0.72f)
        {
            canvasFill = 0.92f;
        }

        if (textPadding.x < 55f || textPadding.y < 65f)
        {
            textPadding = new Vector2(70f, 88f);
        }

        if (surfaceOffset > 0.03f || surfaceOffset < 0.006f)
        {
            surfaceOffset = 0.018f;
        }

        flipReadableSide = true;
    }

    private void PositionCanvasOnPaper()
    {
        if (textCanvas == null || canvasRect == null)
        {
            return;
        }

        Renderer paperRenderer = FindPaperRenderer();
        cachedPaperBounds = paperRenderer != null ? paperRenderer.bounds : ResolvePaperBounds();
        Vector3 center = cachedPaperBounds.center;
        Transform paperTransform = paperRenderer != null ? paperRenderer.transform : transform;
        Vector3 viewerDirection = ResolveCameraDirection(center);
        Vector3 paperNormal = ResolvePaperNormalFacingViewer(paperRenderer, paperTransform, viewerDirection);
        Vector3 paperUp = ResolvePaperUp(paperRenderer, paperTransform, paperNormal);
        Vector3 canvasForward = flipReadableSide ? -paperNormal : paperNormal;

        textCanvas.transform.position = center + paperNormal * surfaceOffset;
        textCanvas.transform.rotation = Quaternion.LookRotation(canvasForward, paperUp);

        float paperWidth = ResolvePaperDimension(cachedPaperBounds, textCanvas.transform.right, 0.5f);
        float paperHeight = ResolvePaperDimension(cachedPaperBounds, textCanvas.transform.up, 0.65f);
        float scale = Mathf.Min(paperWidth / canvasPixelSize.x, paperHeight / canvasPixelSize.y) * canvasFill;
        textCanvas.transform.localScale = Vector3.one * scale;
    }

    private Renderer FindPaperRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Renderer bestRenderer = null;
        float bestArea = -1f;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || ShouldIgnoreRenderer(targetRenderer))
            {
                continue;
            }

            Bounds bounds = targetRenderer.bounds;
            float areaXY = bounds.size.x * bounds.size.y;
            float areaXZ = bounds.size.x * bounds.size.z;
            float areaYZ = bounds.size.y * bounds.size.z;
            float area = Mathf.Max(areaXY, areaXZ, areaYZ);

            if (area > bestArea)
            {
                bestArea = area;
                bestRenderer = targetRenderer;
            }
        }

        return bestRenderer;
    }

    private static Vector3 ResolvePaperNormalFacingViewer(Renderer paperRenderer, Transform paperTransform, Vector3 viewerDirection)
    {
        Vector3 localSize = ResolveRendererLocalSize(paperRenderer);
        Vector3 scale = paperTransform.lossyScale;
        Vector3 scaledSize = new Vector3(
            Mathf.Abs(localSize.x * scale.x),
            Mathf.Abs(localSize.y * scale.y),
            Mathf.Abs(localSize.z * scale.z));

        Vector3 normal = paperTransform.forward;
        if (scaledSize.x <= scaledSize.y && scaledSize.x <= scaledSize.z)
        {
            normal = paperTransform.right;
        }
        else if (scaledSize.y <= scaledSize.x && scaledSize.y <= scaledSize.z)
        {
            normal = paperTransform.up;
        }

        if (Vector3.Dot(normal, viewerDirection) < 0f)
        {
            normal = -normal;
        }

        return normal.sqrMagnitude > 0.001f ? normal.normalized : viewerDirection;
    }

    private static Vector3 ResolvePaperUp(Renderer paperRenderer, Transform paperTransform, Vector3 paperNormal)
    {
        Vector3 localSize = ResolveRendererLocalSize(paperRenderer);
        Vector3 scale = paperTransform.lossyScale;
        Vector3 scaledSize = new Vector3(
            Mathf.Abs(localSize.x * scale.x),
            Mathf.Abs(localSize.y * scale.y),
            Mathf.Abs(localSize.z * scale.z));

        Vector3[] candidates =
        {
            paperTransform.up,
            paperTransform.right,
            paperTransform.forward,
            Vector3.up,
        };

        Vector3 bestUp = Vector3.zero;
        float bestScore = -1f;
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 projected = Vector3.ProjectOnPlane(candidates[i], paperNormal);
            if (projected.sqrMagnitude > 0.001f)
            {
                projected.Normalize();
                if (Vector3.Dot(projected, Vector3.up) < 0f)
                {
                    projected = -projected;
                }

                float sizeScore = i == 0 ? scaledSize.y : i == 1 ? scaledSize.x : i == 2 ? scaledSize.z : 0.1f;
                float worldUpScore = Mathf.Abs(Vector3.Dot(projected, Vector3.up));
                float score = worldUpScore * 2f + sizeScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUp = projected;
                }
            }
        }

        return bestUp.sqrMagnitude > 0.001f ? bestUp.normalized : Vector3.up;
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

    private static float ResolvePaperDimension(Bounds bounds, Vector3 axis, float minimum)
    {
        axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        float dimension = bounds.size.x * axis.x + bounds.size.y * axis.y + bounds.size.z * axis.z;
        return Mathf.Max(dimension, minimum);
    }

    private Bounds ResolvePaperBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || ShouldIgnoreRenderer(targetRenderer))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        return new Bounds(transform.position, new Vector3(0.7f, 0.95f, 0.04f));
    }

    private bool ShouldIgnoreRenderer(Renderer targetRenderer)
    {
        Transform current = targetRenderer.transform;
        while (current != null && current != transform)
        {
            string objectName = current.name;
            if (objectName.StartsWith("Puzzle1_", System.StringComparison.OrdinalIgnoreCase) ||
                objectName.StartsWith("Blood_", System.StringComparison.OrdinalIgnoreCase) ||
                objectName.StartsWith("PhysicalDocument_", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return targetRenderer.GetComponent<TMP_Text>() != null;
    }

    private Vector3 ResolveCameraDirection(Vector3 center)
    {
        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer == null)
        {
            GameObject player = GameObject.Find("Player");
            viewer = player != null ? player.transform : null;
        }

        if (viewer != null)
        {
            Vector3 direction = viewer.position - center;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }
        }

        return -transform.forward;
    }

    private void DisableLegacyGeneratedVisuals()
    {
        string[] legacyNames =
        {
            "Puzzle1_List_Text_Front",
            "Puzzle1_List_Text_Back",
            "PhysicalDocument_" + name,
            "DocumentVisual_" + name,
            "WorldText_" + name,
        };

        for (int i = 0; i < legacyNames.Length; i++)
        {
            Transform legacy = transform.Find(legacyNames[i]);
            if (legacy != null)
            {
                legacy.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            box = gameObject.AddComponent<BoxCollider>();
        }

        Bounds bounds = ResolvePaperBounds();
        Bounds localBounds = WorldBoundsToLocalBounds(bounds);
        Vector3 localSize = localBounds.size;
        box.center = localBounds.center;
        box.size = new Vector3(
            Mathf.Max(Mathf.Abs(localSize.x), 0.25f),
            Mathf.Max(Mathf.Abs(localSize.y), 0.25f),
            Mathf.Max(Mathf.Abs(localSize.z), 0.08f));
    }

    private Bounds WorldBoundsToLocalBounds(Bounds worldBounds)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                    min = Vector3.Min(min, localCorner);
                    max = Vector3.Max(max, localCorner);
                }
            }
        }

        Bounds localBounds = new Bounds((min + max) * 0.5f, max - min);
        return localBounds;
    }

    private void SetRenderersVisible(GameObject target, bool visible)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !ShouldIgnoreRenderer(renderers[i]))
            {
                renderers[i].enabled = visible;
            }
        }
    }

    private static Texture2D CreateBloodTexture(int seed)
    {
        const int size = 160;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Color dark = new Color(0.13f, 0.001f, 0.001f, 0.92f);
        Color wet = new Color(0.48f, 0.004f, 0.002f, 0.76f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1) * 2f - 1f;
                float v = y / (float)(size - 1) * 2f - 1f;
                float distance = Mathf.Sqrt(u * u * 1.45f + v * v * 0.8f);
                float noise = Mathf.PerlinNoise(x * 0.08f + seed * 0.013f, y * 0.08f + seed * 0.021f);
                float mask = Mathf.Clamp01(1f - distance + (noise - 0.42f) * 0.58f);
                mask = Mathf.Pow(mask, 1.45f);

                Color color = Color.Lerp(dark, wet, noise);
                color.a *= mask;
                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, false);
        return texture;
    }
}

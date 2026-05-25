using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class TelevisionVideoBootstrap : MonoBehaviour
{
    [SerializeField] private string screenObjectName = "TV_Screen";
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private RenderTexture targetTexture;
    [SerializeField] private Material screenMaterial;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;
    [SerializeField, Range(0f, 1f)] private float volume = 0.01f;
    [SerializeField] private Vector2 textureScale = Vector2.one;
    [SerializeField] private Vector2 textureOffset = Vector2.zero;
    [SerializeField] private bool generateScreenUvs = true;
    [SerializeField] private bool flipGeneratedUvY = true;
    [SerializeField] private bool useOverlaySurface = false;
    [SerializeField] private Vector2 overlaySize = new Vector2(1.42f, 0.62f);
    [SerializeField] private Vector3 overlayLocalPosition = new Vector3(0f, 0f, -0.01f);
    [SerializeField] private Vector3 overlayLocalEulerAngles = Vector3.zero;
    [SerializeField] private Color poweredOffColor = Color.black;
    [SerializeField] private Color poweredOnEmissionColor = Color.white;
    [SerializeField] private UnityEvent onVideoFinished = new UnityEvent();

    private VideoPlayer player;
    private bool hasFinished;

    public bool IsPlaying => player != null && player.isPlaying;
    public bool HasFinished => hasFinished;
    public UnityEvent OnVideoFinished => onVideoFinished;

    private void Awake()
    {
        ConfigureTelevision();
    }

    private void Start()
    {
        if (playOnStart && player != null && player.clip != null && !player.isPlaying)
        {
            SetScreenPowered(true);
            player.SetDirectAudioVolume(0, volume);
            player.Play();
        }
        else
        {
            StopAndMute();
        }
    }

    public void PlayTVVideo()
    {
        if (player == null)
        {
            ConfigureVideoPlayer();
        }

        if (player == null || player.clip == null)
        {
            Debug.LogError("TelevisionVideoBootstrap: VideoPlayer is not ready.");
            return;
        }

        if (player.isPlaying)
        {
            return;
        }

        hasFinished = false;
        player.isLooping = loop;
        player.frame = 0;
        player.time = 0d;
        player.SetDirectAudioVolume(0, volume);
        SetScreenPowered(true);
        player.Play();
        Debug.Log("TelevisionVideoBootstrap: TV video started.");
    }

    private void ConfigureTelevision()
    {
        if (videoClip == null || targetTexture == null || screenMaterial == null)
        {
            Debug.LogWarning("TelevisionVideoBootstrap: references video, texture ou materiau manquantes.");
            return;
        }

        targetTexture.Release();
        targetTexture.wrapMode = TextureWrapMode.Clamp;
        ClearRenderTexture(poweredOffColor);
        ApplyRenderTextureToMaterial();
        Renderer screenRenderer = ApplyMaterialToScreen();
        if (generateScreenUvs && screenRenderer != null)
        {
            GenerateScreenUvs(screenRenderer);
        }

        if (useOverlaySurface && screenRenderer != null)
        {
            CreateOrUpdateOverlaySurface(screenRenderer);
        }

        ConfigureVideoPlayer();
        SetScreenPowered(playOnStart);
    }

    private void ApplyRenderTextureToMaterial()
    {
        screenMaterial.mainTexture = targetTexture;
        screenMaterial.mainTextureScale = textureScale;
        screenMaterial.mainTextureOffset = textureOffset;
        screenMaterial.SetTexture("_EmissionMap", targetTexture);
        screenMaterial.SetTextureScale("_EmissionMap", textureScale);
        screenMaterial.SetTextureOffset("_EmissionMap", textureOffset);
        screenMaterial.SetColor("_EmissionColor", poweredOffColor);
        screenMaterial.EnableKeyword("_EMISSION");
    }

    private void SetScreenPowered(bool powered)
    {
        if (screenMaterial == null)
        {
            return;
        }

        if (!powered)
        {
            ClearRenderTexture(poweredOffColor);
        }

        Color screenColor = powered ? Color.white : poweredOffColor;
        Color emissionColor = powered ? poweredOnEmissionColor : poweredOffColor;
        if (screenMaterial.HasProperty("_Color"))
        {
            screenMaterial.color = screenColor;
        }

        screenMaterial.SetColor("_EmissionColor", emissionColor);
    }

    private void ClearRenderTexture(Color clearColor)
    {
        if (targetTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = targetTexture;
        GL.Clear(true, true, clearColor);
        RenderTexture.active = previous;
    }

    private Renderer ApplyMaterialToScreen()
    {
        GameObject screenObject = FindSceneObjectByName(SceneManager.GetActiveScene(), screenObjectName);
        if (screenObject == null)
        {
            Debug.LogWarning("TelevisionVideoBootstrap: objet introuvable: " + screenObjectName);
            return null;
        }

        Renderer renderer = screenObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = screenObject.GetComponentInChildren<Renderer>(true);
        }

        if (renderer == null)
        {
            Debug.LogWarning("TelevisionVideoBootstrap: aucun Renderer trouve sur " + screenObjectName);
            return null;
        }

        renderer.sharedMaterial = screenMaterial;
        return renderer;
    }

    private void GenerateScreenUvs(Renderer screenRenderer)
    {
        MeshFilter meshFilter = screenRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("TelevisionVideoBootstrap: aucun MeshFilter trouve pour regenerer les UV.");
            return;
        }

        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        mesh.name = meshFilter.sharedMesh.name + "_VideoUV";

        Vector3[] vertices = mesh.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            return;
        }

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Length; i++)
        {
            bounds.Encapsulate(vertices[i]);
        }

        Vector3 size = bounds.size;
        int depthAxis = GetSmallestAxis(size);
        int horizontalAxis = GetLargestAxis(size, depthAxis);
        int verticalAxis = GetRemainingAxis(depthAxis, horizontalAxis);

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            float u = InverseLerpAxis(bounds.min, bounds.max, vertices[i], horizontalAxis);
            float v = InverseLerpAxis(bounds.min, bounds.max, vertices[i], verticalAxis);
            if (flipGeneratedUvY)
            {
                v = 1f - v;
            }

            uvs[i] = new Vector2(u, v);
        }

        mesh.uv = uvs;
        meshFilter.sharedMesh = mesh;
    }

    private static int GetSmallestAxis(Vector3 size)
    {
        if (size.x <= size.y && size.x <= size.z)
        {
            return 0;
        }

        return size.y <= size.z ? 1 : 2;
    }

    private static int GetLargestAxis(Vector3 size, int excludedAxis)
    {
        int largestAxis = -1;
        float largestSize = float.MinValue;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == excludedAxis)
            {
                continue;
            }

            float axisSize = GetAxis(size, axis);
            if (axisSize > largestSize)
            {
                largestSize = axisSize;
                largestAxis = axis;
            }
        }

        return largestAxis;
    }

    private static int GetRemainingAxis(int firstAxis, int secondAxis)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis != firstAxis && axis != secondAxis)
            {
                return axis;
            }
        }

        return 1;
    }

    private static float InverseLerpAxis(Vector3 min, Vector3 max, Vector3 value, int axis)
    {
        float minValue = GetAxis(min, axis);
        float maxValue = GetAxis(max, axis);
        if (Mathf.Approximately(minValue, maxValue))
        {
            return 0f;
        }

        return Mathf.InverseLerp(minValue, maxValue, GetAxis(value, axis));
    }

    private static float GetAxis(Vector3 value, int axis)
    {
        if (axis == 0)
        {
            return value.x;
        }

        return axis == 1 ? value.y : value.z;
    }

    private void CreateOrUpdateOverlaySurface(Renderer screenRenderer)
    {
        const string overlayName = "TV_Video_Surface";
        Transform parent = screenRenderer.transform;
        Transform existingOverlay = parent.Find(overlayName);
        GameObject overlay = existingOverlay != null ? existingOverlay.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);

        overlay.name = overlayName;
        overlay.transform.SetParent(parent, false);
        overlay.transform.localPosition = overlayLocalPosition;
        overlay.transform.localRotation = Quaternion.Euler(overlayLocalEulerAngles);
        overlay.transform.localScale = new Vector3(overlaySize.x, overlaySize.y, 1f);

        Renderer overlayRenderer = overlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            overlayRenderer.sharedMaterial = screenMaterial;
        }

        Collider overlayCollider = overlay.GetComponent<Collider>();
        if (overlayCollider != null)
        {
            Destroy(overlayCollider);
        }
    }

    private void ConfigureVideoPlayer()
    {
        player = GetComponent<VideoPlayer>();
        if (player == null)
        {
            player = gameObject.AddComponent<VideoPlayer>();
        }

        player.loopPointReached -= HandleVideoFinished;
        player.source = VideoSource.VideoClip;
        player.clip = videoClip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = targetTexture;
        player.playOnAwake = playOnStart;
        player.isLooping = loop;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.SetDirectAudioVolume(0, playOnStart ? volume : 0f);
        player.loopPointReached += HandleVideoFinished;
    }

    private void StopAndMute()
    {
        if (player != null)
        {
            player.Stop();
            player.SetDirectAudioVolume(0, 0f);
            player.time = 0d;
        }

        SetScreenPowered(false);
    }

    private void HandleVideoFinished(VideoPlayer finishedPlayer)
    {
        if (loop)
        {
            return;
        }

        hasFinished = true;
        Debug.Log("TelevisionVideoBootstrap: TV video finished.");
        onVideoFinished.Invoke();
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform found = FindChildByName(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildByName(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

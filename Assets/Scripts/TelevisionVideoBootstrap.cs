using UnityEngine;
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
    [SerializeField] private Vector2 textureScale = new Vector2(3f, -3f);
    [SerializeField] private Vector2 textureOffset = new Vector2(-1.5f, 2f);
    [SerializeField] private bool useOverlaySurface = false;
    [SerializeField] private Vector2 overlaySize = new Vector2(1.42f, 0.62f);
    [SerializeField] private Vector3 overlayLocalPosition = new Vector3(0f, 0f, -0.01f);
    [SerializeField] private Vector3 overlayLocalEulerAngles = Vector3.zero;

    private void Awake()
    {
        ConfigureTelevision();
    }

    private void Start()
    {
        VideoPlayer player = GetComponent<VideoPlayer>();
        if (playOnStart && player != null && player.clip != null && !player.isPlaying)
        {
            player.Play();
        }
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
        ApplyRenderTextureToMaterial();
        Renderer screenRenderer = ApplyMaterialToScreen();
        if (useOverlaySurface && screenRenderer != null)
        {
            CreateOrUpdateOverlaySurface(screenRenderer);
        }

        ConfigureVideoPlayer();
    }

    private void ApplyRenderTextureToMaterial()
    {
        screenMaterial.mainTexture = targetTexture;
        screenMaterial.mainTextureScale = textureScale;
        screenMaterial.mainTextureOffset = textureOffset;
        screenMaterial.SetTexture("_EmissionMap", targetTexture);
        screenMaterial.SetTextureScale("_EmissionMap", textureScale);
        screenMaterial.SetTextureOffset("_EmissionMap", textureOffset);
        screenMaterial.SetColor("_EmissionColor", Color.white);
        screenMaterial.EnableKeyword("_EMISSION");
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
        VideoPlayer player = GetComponent<VideoPlayer>();
        if (player == null)
        {
            player = gameObject.AddComponent<VideoPlayer>();
        }

        player.source = VideoSource.VideoClip;
        player.clip = videoClip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = targetTexture;
        player.playOnAwake = playOnStart;
        player.isLooping = loop;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.SetDirectAudioVolume(0, volume);
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

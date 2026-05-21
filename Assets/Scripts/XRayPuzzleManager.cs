using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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

    public int solvedCount = 0;
    public bool horrorActive = false;
    public bool puzzleSolved = false;

    private AudioSource audioSource;
    private readonly List<int> selectionOrder = new List<int>(4);
    private static readonly int[] ExpectedOrder = { 2, 3, 1, 4 };

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
        EnsureXRayVisuals();
        EnsureAccentLight();
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
        return allFolders != null && allFolders.Any(folder => folder != null && folder.isRead);
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
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
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
        if (xray == null || puzzleSolved || horrorActive)
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

        ResetSequence();
        TriggerHorror();
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

    public IEnumerator SuccessSequence()
    {
        puzzleSolved = true;
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

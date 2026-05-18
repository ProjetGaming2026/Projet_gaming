using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(Canvas))]
public class WorldSpaceCanvasLightAdapter : MonoBehaviour
{
    [Header("Lighting Response")]
    [SerializeField, Range(0f, 1f)] private float minBrightness = 0.08f;
    [SerializeField, Range(0f, 1f)] private float maxBrightness = 1f;
    [SerializeField, Range(0f, 1f)] private float ambientInfluence = 0.15f;
    [SerializeField, Range(0f, 3f)] private float nearbyLightInfluence = 0.55f;
    [SerializeField, Range(0f, 1f)] private float directionalInfluence = 0.03f;
    [SerializeField] private float sampleInterval = 0.08f;
    [SerializeField] private bool useOcclusionChecks = true;
    [SerializeField] private LayerMask occlusionMask = ~0;
    [SerializeField] private bool includeInactiveGraphics = true;
    [SerializeField] private bool debugLogs;

    private readonly List<CanvasRenderer> targetRenderers = new List<CanvasRenderer>();
    private Canvas worldCanvas;
    private Light[] cachedLights = new Light[0];
    private float nextSampleTime;
    private float nextLightRefreshTime;

    private void Awake()
    {
        ResolveCanvas();
        RefreshTargets();
    }

    private void OnEnable()
    {
        ResolveCanvas();
        RefreshTargets();
        RefreshLights();
        ApplyLightingTint();

        if (debugLogs)
        {
            Debug.Log("WorldSpaceCanvasLightAdapter: linked to " + name + " with " + targetRenderers.Count + " UI renderers.");
        }
    }

    private void OnDisable()
    {
        ResetTint();
    }

    private void OnValidate()
    {
        minBrightness = Mathf.Clamp01(minBrightness);
        maxBrightness = Mathf.Clamp(maxBrightness, minBrightness, 1f);
        ambientInfluence = Mathf.Clamp01(ambientInfluence);
        nearbyLightInfluence = Mathf.Max(0f, nearbyLightInfluence);
        directionalInfluence = Mathf.Clamp01(directionalInfluence);
        sampleInterval = Mathf.Max(0.02f, sampleInterval);

        ResolveCanvas();
        RefreshTargets();
        ApplyLightingTint();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshTargets();
    }

    private void Update()
    {
        if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        float now = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

        if (now >= nextLightRefreshTime)
        {
            RefreshLights();
            nextLightRefreshTime = now + 1.5f;
        }

        if (now >= nextSampleTime)
        {
            ApplyLightingTint();
            nextSampleTime = now + sampleInterval;
        }
    }

    private void ResolveCanvas()
    {
        if (worldCanvas == null)
        {
            worldCanvas = GetComponent<Canvas>();
        }
    }

    private void RefreshTargets()
    {
        targetRenderers.Clear();

        Graphic[] graphics = GetComponentsInChildren<Graphic>(includeInactiveGraphics);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null || graphics[i].canvasRenderer == null)
            {
                continue;
            }

            targetRenderers.Add(graphics[i].canvasRenderer);
        }
    }

    private void RefreshLights()
    {
#if UNITY_2023_1_OR_NEWER
        cachedLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        cachedLights = FindObjectsOfType<Light>();
#endif
    }

    private void ApplyLightingTint()
    {
        if (targetRenderers.Count == 0)
        {
            RefreshTargets();
        }

        float brightness = EvaluateBrightness(transform.position);
        Color tint = new Color(brightness, brightness, brightness, 1f);

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null)
            {
                targetRenderers[i].SetColor(tint);
            }
        }
    }

    private float EvaluateBrightness(Vector3 samplePosition)
    {
        float ambient = RenderSettings.ambientLight.grayscale * ambientInfluence;
        float lightAmount = 0f;

        for (int i = 0; i < cachedLights.Length; i++)
        {
            Light sceneLight = cachedLights[i];
            if (sceneLight == null || !sceneLight.enabled || sceneLight.intensity <= 0f)
            {
                continue;
            }

            if (sceneLight.type == LightType.Directional)
            {
                lightAmount += EvaluateDirectionalLight(sceneLight, samplePosition);
                continue;
            }

            lightAmount += EvaluateLocalLight(sceneLight, samplePosition);
        }

        float normalizedLight = 1f - Mathf.Exp(-lightAmount);
        return Mathf.Clamp(ambient + normalizedLight, minBrightness, maxBrightness);
    }

    private float EvaluateLocalLight(Light sceneLight, Vector3 samplePosition)
    {
        Vector3 lightPosition = sceneLight.transform.position;
        Vector3 lightToCanvas = samplePosition - lightPosition;
        float distance = lightToCanvas.magnitude;
        float range = Mathf.Max(0.01f, sceneLight.range);

        if (distance > range)
        {
            return 0f;
        }

        float attenuation = 1f - Mathf.Clamp01(distance / range);
        attenuation *= attenuation;

        if (sceneLight.type == LightType.Spot)
        {
            float halfAngle = Mathf.Max(0.01f, sceneLight.spotAngle * 0.5f);
            float angle = Vector3.Angle(sceneLight.transform.forward, lightToCanvas.normalized);

            if (angle > halfAngle)
            {
                return 0f;
            }

            float coneFade = 1f - Mathf.Clamp01(angle / halfAngle);
            attenuation *= coneFade * coneFade;
        }

        if (useOcclusionChecks && IsBlocked(samplePosition, lightPosition, distance))
        {
            attenuation *= 0.12f;
        }

        return sceneLight.intensity * attenuation * nearbyLightInfluence;
    }

    private float EvaluateDirectionalLight(Light sceneLight, Vector3 samplePosition)
    {
        float contribution = sceneLight.intensity * directionalInfluence;

        if (useOcclusionChecks)
        {
            Vector3 rayOrigin = samplePosition - sceneLight.transform.forward * 0.05f;
            if (Physics.Raycast(rayOrigin, -sceneLight.transform.forward, 25f, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                contribution *= 0.12f;
            }
        }

        return contribution;
    }

    private bool IsBlocked(Vector3 samplePosition, Vector3 lightPosition, float distance)
    {
        Vector3 direction = lightPosition - samplePosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Ray ray = new Ray(samplePosition, direction.normalized);
        return Physics.Raycast(ray, distance - 0.03f, occlusionMask, QueryTriggerInteraction.Ignore);
    }

    private void ResetTint()
    {
        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i] != null)
            {
                targetRenderers[i].SetColor(Color.white);
            }
        }
    }
}

public static class WorldSpaceCanvasLightAdapterBootstrap
{
    private static bool subscribedToSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        AddAdaptersToScene(SceneManager.GetActiveScene());

        if (!subscribedToSceneLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribedToSceneLoaded = true;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddAdaptersToScene(scene);
    }

    private static void AddAdaptersToScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(true);
            for (int j = 0; j < canvases.Length; j++)
            {
                Canvas canvas = canvases[j];
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                {
                    continue;
                }

                if (canvas.name == "MorseChartCanvas" && canvas.GetComponent<WorldSpaceCanvasLightAdapter>() == null)
                {
                    canvas.gameObject.AddComponent<WorldSpaceCanvasLightAdapter>();
                    Debug.Log("WorldSpaceCanvasLightAdapter: runtime adapter added to MorseChartCanvas.");
                }
            }
        }
    }
}

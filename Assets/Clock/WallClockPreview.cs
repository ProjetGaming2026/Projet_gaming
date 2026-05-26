using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WallClockPreview : MonoBehaviour
{
    [SerializeField] private GameObject sourceModel;
    [SerializeField] private string instanceName = "Wall Clock Instance";
    [SerializeField] private string pendulumPath = "wall_clock/wall_clock_pendulum";
    [SerializeField] private float pendulumAngle = 14f;
    [SerializeField] private float pendulumSpeed = 1.3f;
    [SerializeField] private Vector3 fillLightLocalPosition = new Vector3(0f, 1.7f, -1.8f);
    [SerializeField] private Vector3 fillLightLocalEulerAngles = new Vector3(12f, 0f, 0f);
    [SerializeField] private float fillLightIntensity = 2.5f;
    [SerializeField] private float fillLightRange = 8f;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        if (sourceModel == null)
        {
            return;
        }

        Transform existing = transform.Find(instanceName);
        if (existing == null)
        {
            GameObject instance;
#if UNITY_EDITOR
            instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel, transform);
#else
            instance = Instantiate(sourceModel, transform);
#endif
            instance.name = instanceName;
        }

        EnsureFillLight();
    }

    private void Update()
    {
        Transform pendulum = transform.Find(instanceName + "/" + pendulumPath);
        if (pendulum == null)
        {
            return;
        }

        float time = GetTimeSeconds();
        float swing = Mathf.Sin(time * pendulumSpeed) * pendulumAngle;
        pendulum.localRotation = Quaternion.Euler(0f, 0f, swing);
    }

    private void EnsureFillLight()
    {
        Transform lightTransform = transform.Find("Face Fill Light");
        Light fillLight;

        if (lightTransform == null)
        {
            GameObject lightObject = new GameObject("Face Fill Light");
            lightObject.transform.SetParent(transform, false);
            lightTransform = lightObject.transform;
            fillLight = lightObject.AddComponent<Light>();
            fillLight.type = LightType.Spot;
            fillLight.shadows = LightShadows.None;
        }
        else
        {
            fillLight = lightTransform.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = lightTransform.gameObject.AddComponent<Light>();
                fillLight.type = LightType.Spot;
                fillLight.shadows = LightShadows.None;
            }
        }

        lightTransform.localPosition = fillLightLocalPosition;
        lightTransform.localRotation = Quaternion.Euler(fillLightLocalEulerAngles);
        fillLight.color = Color.white;
        fillLight.intensity = fillLightIntensity;
        fillLight.range = fillLightRange;
        fillLight.spotAngle = 70f;
        fillLight.renderMode = LightRenderMode.Auto;
    }

    private float GetTimeSeconds()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return (float)EditorApplication.timeSinceStartup;
        }
#endif
        return Time.time;
    }
}

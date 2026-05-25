using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LightFlickerController : MonoBehaviour
{
    [SerializeField] private Light[] lightsToFlicker;
    [SerializeField, Min(0.1f)] private float flickerDuration = 1.4f;
    [SerializeField, Min(0.02f)] private float flickerInterval = 0.08f;
    [SerializeField] private Vector2 intensityMultiplierRange = new Vector2(0.1f, 1.15f);

    public UnityEvent OnFlickerStarted = new UnityEvent();
    public UnityEvent OnFlickerFinished = new UnityEvent();

    private Coroutine flickerRoutine;
    private float[] originalIntensities;
    private bool[] originalEnabled;

    public void Flicker()
    {
        if (lightsToFlicker == null || lightsToFlicker.Length == 0)
        {
            lightsToFlicker = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        if (lightsToFlicker == null || lightsToFlicker.Length == 0)
        {
            Debug.LogWarning("LightFlickerController has no lights to flicker.");
            return;
        }

        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            RestoreLights();
        }

        flickerRoutine = StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        CacheOriginalLightState();
        OnFlickerStarted.Invoke();

        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            for (int i = 0; i < lightsToFlicker.Length; i++)
            {
                if (lightsToFlicker[i] == null)
                {
                    continue;
                }

                lightsToFlicker[i].enabled = Random.value > 0.28f;
                lightsToFlicker[i].intensity = originalIntensities[i] * Random.Range(intensityMultiplierRange.x, intensityMultiplierRange.y);
            }

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        RestoreLights();
        OnFlickerFinished.Invoke();
        flickerRoutine = null;
    }

    private void CacheOriginalLightState()
    {
        originalIntensities = new float[lightsToFlicker.Length];
        originalEnabled = new bool[lightsToFlicker.Length];

        for (int i = 0; i < lightsToFlicker.Length; i++)
        {
            if (lightsToFlicker[i] == null)
            {
                continue;
            }

            originalIntensities[i] = lightsToFlicker[i].intensity;
            originalEnabled[i] = lightsToFlicker[i].enabled;
        }
    }

    private void RestoreLights()
    {
        if (lightsToFlicker == null || originalIntensities == null || originalEnabled == null)
        {
            return;
        }

        for (int i = 0; i < lightsToFlicker.Length; i++)
        {
            if (lightsToFlicker[i] == null)
            {
                continue;
            }

            lightsToFlicker[i].intensity = originalIntensities[i];
            lightsToFlicker[i].enabled = originalEnabled[i];
        }
    }
}

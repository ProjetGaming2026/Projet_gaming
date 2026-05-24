using UnityEngine;

/// <summary>
/// Attach to any Light in the scene for a horror hospital flicker effect.
/// Works great on fluorescent ceiling lights.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float minIntensity = 0.0f;
    public float maxIntensity = 1.2f;
    public float flickerSpeed = 0.1f;

    [Header("Flicker Pattern")]
    public bool randomFlicker = true;
    public float flickerChance = 0.05f; // chance to flicker every frame (0 to 1)

    private Light lightComponent;
    private float baseIntensity;
    private float targetIntensity;
    private float flickerTimer;

    void Start()
    {
        lightComponent = GetComponent<Light>();
        if (lightComponent != null)
        {
            baseIntensity = lightComponent.intensity;
            targetIntensity = baseIntensity;
        }
    }

    void Update()
    {
        if (lightComponent == null) return;

        if (randomFlicker)
        {
            // Random chance to flicker each frame
            if (Random.value < flickerChance)
            {
                // Quick on/off flicker
                lightComponent.intensity = Random.value < 0.5f ? minIntensity : maxIntensity;
            }
            else
            {
                // Return to base intensity
                lightComponent.intensity = Mathf.Lerp(lightComponent.intensity, baseIntensity, Time.deltaTime * 10f);
            }
        }
        else
        {
            // Smooth sine-wave flicker
            flickerTimer += Time.deltaTime * flickerSpeed;
            lightComponent.intensity = Mathf.Lerp(minIntensity, maxIntensity, (Mathf.Sin(flickerTimer) + 1) / 2);
        }
    }

    /// <summary>
    /// Call this to trigger a panic flicker (wrong organ placed)
    /// </summary>
    public void TriggerPanicFlicker()
    {
        StartCoroutine(PanicFlicker());
    }

    System.Collections.IEnumerator PanicFlicker()
    {
        float elapsed = 0f;
        float duration = 1.5f;
        while (elapsed < duration)
        {
            lightComponent.intensity = Random.Range(minIntensity, maxIntensity);
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        lightComponent.intensity = baseIntensity;
    }
}

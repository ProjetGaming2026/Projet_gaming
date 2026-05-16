using UnityEngine;

[RequireComponent(typeof(BasicMonsterController))]
public class MonsterPresenceEffects : MonoBehaviour
{
    [SerializeField] private float influenceRadius = 14f;
    [SerializeField] private float flickerDistance = 8f;
    [SerializeField] private float apparitionDistance = 6f;
    [SerializeField] private float lightRefreshInterval = 2.5f;
    [SerializeField] private float blackoutPulseDuration = 0.28f;
    [SerializeField] private int maxAffectedLights = 7;

    private BasicMonsterController monsterController;
    private PrototypeGameManager gameManager;
    private Transform playerTarget;
    private Light[] sceneLights;
    private float[] baseIntensities;
    private float[] noiseOffsets;
    private float refreshTimer;
    private float blackoutPulseTimer;
    private bool wasThreatening;

    private void Awake()
    {
        monsterController = GetComponent<BasicMonsterController>();
        gameManager = FindFirstObjectByType<PrototypeGameManager>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }

        CacheLights();
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f || sceneLights == null || sceneLights.Length == 0)
        {
            CacheLights();
        }

        if (playerTarget == null || monsterController == null || gameManager == null || sceneLights == null)
        {
            return;
        }

        bool activePhase =
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Chase ||
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape;

        if (!activePhase)
        {
            RestoreLights();
            wasThreatening = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        bool threatening =
            (monsterController.IsHuntingState || monsterController.IsInvestigatingState) &&
            distance <= influenceRadius;

        if (threatening && !wasThreatening && distance <= apparitionDistance)
        {
            blackoutPulseTimer = blackoutPulseDuration;
        }

        if (blackoutPulseTimer > 0f)
        {
            blackoutPulseTimer -= Time.deltaTime;
        }

        float threat01 = Mathf.Clamp01(1f - (distance / Mathf.Max(influenceRadius, 0.1f)));
        float flicker01 = Mathf.Clamp01(1f - (distance / Mathf.Max(flickerDistance, 0.1f)));
        UpdateNearbyLights(threatening, threat01, flicker01);
        wasThreatening = threatening;
    }

    private void CacheLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        int validCount = 0;
        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] != null && allLights[i].type != LightType.Directional)
            {
                validCount++;
            }
        }

        sceneLights = new Light[validCount];
        baseIntensities = new float[validCount];
        noiseOffsets = new float[validCount];

        int writeIndex = 0;
        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] == null || allLights[i].type == LightType.Directional)
            {
                continue;
            }

            sceneLights[writeIndex] = allLights[i];
            baseIntensities[writeIndex] = allLights[i].intensity;
            noiseOffsets[writeIndex] = Random.Range(0f, 1000f);
            writeIndex++;
        }

        refreshTimer = lightRefreshInterval;
    }

    private void UpdateNearbyLights(bool threatening, float threat01, float flicker01)
    {
        int affectedCount = 0;

        for (int i = 0; i < sceneLights.Length; i++)
        {
            Light lightSource = sceneLights[i];
            if (lightSource == null)
            {
                continue;
            }

            float baseIntensity = baseIntensities[i];
            float distanceToPlayer = Vector3.Distance(lightSource.transform.position, playerTarget.position);
            float distanceToMonster = Vector3.Distance(lightSource.transform.position, transform.position);
            bool inRange = distanceToPlayer <= influenceRadius || distanceToMonster <= influenceRadius;

            if (!threatening || !inRange || affectedCount >= maxAffectedLights)
            {
                lightSource.intensity = Mathf.MoveTowards(lightSource.intensity, baseIntensity, Time.deltaTime * 4.5f);
                continue;
            }

            affectedCount++;

            float localThreat = Mathf.Clamp01(1f - (Mathf.Min(distanceToPlayer, distanceToMonster) / Mathf.Max(influenceRadius, 0.1f)));
            float flickerNoise = Mathf.PerlinNoise(noiseOffsets[i], Time.time * (8f + localThreat * 14f));
            float flicker = Mathf.Lerp(0.2f, 1f, flickerNoise);
            float dimFactor = Mathf.Lerp(1f, 0.4f, threat01 * 0.55f + localThreat * 0.35f);
            float targetIntensity = baseIntensity * dimFactor;

            if (blackoutPulseTimer > 0f)
            {
                targetIntensity *= Mathf.Lerp(0.04f, 1f, 1f - (blackoutPulseTimer / blackoutPulseDuration));
            }
            else
            {
                targetIntensity *= Mathf.Lerp(1f, flicker, flicker01 * 0.9f);
            }

            lightSource.intensity = Mathf.MoveTowards(lightSource.intensity, targetIntensity, Time.deltaTime * 12f);
        }
    }

    private void RestoreLights()
    {
        if (sceneLights == null)
        {
            return;
        }

        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = Mathf.MoveTowards(sceneLights[i].intensity, baseIntensities[i], Time.deltaTime * 4.5f);
            }
        }
    }
}

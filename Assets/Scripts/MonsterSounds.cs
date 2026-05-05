using UnityEngine;

public class MonsterSounds : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource actionSource;

    [Header("Sons du monstre")]
    [SerializeField] private AudioClip[] idleGrowls;
    [SerializeField] private float minGrowlInterval = 4f;
    [SerializeField] private float maxGrowlInterval = 10f;
    [SerializeField] private float growlDistance = 10f;
    [SerializeField] private float proximityRoarDistance = 6f;
    [SerializeField] private float sightRoarCooldown = 9f;
    [SerializeField] private float nearLoopDistance = 12f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float growlVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float proximityLoopVolume = 0.38f;

    private Transform playerTarget;
    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private float growlTimer;
    private float roarCooldownTimer;
    private bool wasThreatening;
    private AudioLowPassFilter ambientLowPass;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }

        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        monsterController = GetComponent<BasicMonsterController>();
        growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);

        if (ambientSource != null)
        {
            ambientLowPass = ambientSource.GetComponent<AudioLowPassFilter>();
            if (ambientLowPass == null)
            {
                ambientLowPass = ambientSource.gameObject.AddComponent<AudioLowPassFilter>();
            }

            ambientLowPass.cutoffFrequency = 22000f;
        }
    }

    private void Update()
    {
        if (gameManager == null || playerTarget == null)
        {
            return;
        }

        bool isActivePhase =
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Chase ||
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape;

        if (!isActivePhase)
        {
            UpdatePresenceLoop(999f, false);
            wasThreatening = false;
            return;
        }

        if (roarCooldownTimer > 0f)
        {
            roarCooldownTimer -= Time.deltaTime;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        bool isClose = distance <= growlDistance;
        bool isThreatening =
            monsterController != null &&
            (monsterController.IsHuntingState || monsterController.IsInvestigatingState) &&
            distance <= proximityRoarDistance;

        UpdatePresenceLoop(distance, isThreatening);
        TryPlayThreatRoar(distance, isThreatening);

        if (!isClose)
        {
            wasThreatening = isThreatening;
            return;
        }

        growlTimer -= Time.deltaTime;
        if (growlTimer <= 0f)
        {
            float distanceRatio = Mathf.InverseLerp(growlDistance, 1.5f, distance);
            float interval = Mathf.Lerp(maxGrowlInterval, minGrowlInterval * 0.45f, distanceRatio);
            float pitch = Mathf.Lerp(0.86f, 1.14f, Random.value);

            PlayRandomClip(ambientSource, idleGrowls, growlVolume * Mathf.Lerp(0.75f, 1f, distanceRatio), pitch);
            growlTimer = interval + Random.Range(0.15f, 1.1f);
        }

        wasThreatening = isThreatening;
    }

    private void UpdatePresenceLoop(float distance, bool isThreatening)
    {
        if (ambientSource == null)
        {
            return;
        }

        float proximity01 = Mathf.Clamp01(1f - (distance / Mathf.Max(nearLoopDistance, 0.1f)));
        float targetVolume = proximityLoopVolume * proximity01;
        if (isThreatening)
        {
            targetVolume *= 1.35f;
        }

        ambientSource.volume = Mathf.MoveTowards(ambientSource.volume, targetVolume, Time.deltaTime * 0.7f);

        if (ambientLowPass != null)
        {
            float targetCutoff = Mathf.Lerp(22000f, 1200f, proximity01 * (isThreatening ? 1f : 0.55f));
            ambientLowPass.cutoffFrequency = Mathf.MoveTowards(ambientLowPass.cutoffFrequency, targetCutoff, Time.deltaTime * 16000f);
        }
    }

    private void TryPlayThreatRoar(float distance, bool isThreatening)
    {
        if (!isThreatening || wasThreatening || roarCooldownTimer > 0f)
        {
            return;
        }

        float volume = Mathf.Lerp(growlVolume * 0.65f, growlVolume, Mathf.InverseLerp(proximityRoarDistance, 1f, distance));
        float pitch = Random.Range(0.82f, 0.96f);
        PlayRandomClip(actionSource != null ? actionSource : ambientSource, idleGrowls, volume, pitch);
        roarCooldownTimer = sightRoarCooldown;
    }

    private void PlayRandomClip(AudioSource source, AudioClip[] clips, float volume, float pitch = 1f)
    {
        if (source == null || clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }

        float previousPitch = source.pitch;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        source.pitch = previousPitch;
    }
}

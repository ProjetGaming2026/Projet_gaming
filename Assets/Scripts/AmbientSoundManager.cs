using UnityEngine;

public class AmbientSoundManager : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambianceSource;

    [Header("Ambiance de fond")]
    [SerializeField] private AudioClip backroomsAmbiance;
    [SerializeField, Range(0f, 1f)] private float ambianceVolume = 0.3f;

    [Header("Musique de chase")]
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float fadeSpeed = 1.5f;
    [SerializeField] private float silenceDistance = 5.5f;
    [SerializeField] private float confinedProbeDistance = 7f;
    [SerializeField] private LayerMask environmentProbeMask = Physics.DefaultRaycastLayers;

    [Header("Musique fin")]
    [SerializeField] private AudioClip winMusic;
    [SerializeField] private AudioClip loseMusic;

    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private Transform playerTransform;
    private Transform monsterTransform;
    private PrototypeGameManager.GamePhase lastPhase;
    private AudioLowPassFilter ambianceLowPass;
    private AudioLowPassFilter musicLowPass;
    private AudioReverbFilter ambianceReverb;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        monsterController = FindFirstObjectByType<BasicMonsterController>();
        if (monsterController != null)
        {
            monsterTransform = monsterController.transform;
        }

        EnsureFilters();
    }

    private void Start()
    {
        if (ambianceSource != null && backroomsAmbiance != null)
        {
            ambianceSource.clip = backroomsAmbiance;
            ambianceSource.loop = true;
            ambianceSource.volume = ambianceVolume * AudioManager.AmbienceVolume;
            ambianceSource.Play();
        }

        if (musicSource != null)
        {
            musicSource.volume = 0f;
        }
    }

    private void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        PrototypeGameManager.GamePhase phase = gameManager.CurrentPhase;

        if (phase == PrototypeGameManager.GamePhase.Win && lastPhase != phase)
        {
            StopChaseMusic();
            PlayEndMusic(winMusic);
            lastPhase = phase;
            return;
        }

        if (phase == PrototypeGameManager.GamePhase.Lose && lastPhase != phase)
        {
            StopChaseMusic();
            PlayEndMusic(loseMusic);
            lastPhase = phase;
            return;
        }

        lastPhase = phase;

        bool isActivePhase =
            phase == PrototypeGameManager.GamePhase.Chase ||
            phase == PrototypeGameManager.GamePhase.Escape;

        if (!isActivePhase)
        {
            RestoreAmbientBed();
            FadeOutChaseMusic();
            return;
        }

        if (playerTransform == null || monsterTransform == null)
        {
            return;
        }

        bool sameFloor = monsterController == null || monsterController.IsWithinVerticalSenseRange(playerTransform);
        float distance = sameFloor
            ? Vector3.Distance(playerTransform.position, monsterTransform.position)
            : float.PositiveInfinity;
        bool monsterIsClose = distance <= chaseDistance;
        bool monsterThreatening = sameFloor &&
            monsterController != null &&
            (monsterController.IsHuntingState || monsterController.IsInvestigatingState || monsterController.IsGuardingExitState);

        if (monsterIsClose)
        {
            FadeInChaseMusic(distance, monsterThreatening);
        }
        else
        {
            FadeOutChaseMusic();
        }

        UpdateAmbientSpace(distance, monsterThreatening);
    }

    private void FadeInChaseMusic(float distance, bool monsterThreatening)
    {
        if (musicSource == null || chaseMusic == null)
        {
            return;
        }

        if (!musicSource.isPlaying || musicSource.clip != chaseMusic)
        {
            musicSource.clip = chaseMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        float targetVolume = Mathf.InverseLerp(chaseDistance, 2f, distance);
        if (monsterThreatening)
        {
            targetVolume = Mathf.Lerp(targetVolume, 1f, 0.28f);
        }

        targetVolume *= AudioManager.MusicVolume;
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }

    private void FadeOutChaseMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0f, fadeSpeed * Time.deltaTime);

        if (musicSource.volume <= 0f && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    private void StopChaseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.volume = 0f;
        }
    }

    private void PlayEndMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = false;
        musicSource.volume = AudioManager.MusicVolume;
        musicSource.Play();
    }

    private void UpdateAmbientSpace(float distanceToMonster, bool monsterThreatening)
    {
        if (ambianceSource == null || playerTransform == null)
        {
            return;
        }

        float confinement = EvaluateConfinement();
        float silence01 = Mathf.Clamp01(1f - (distanceToMonster / Mathf.Max(silenceDistance, 0.1f)));
        if (monsterThreatening)
        {
            silence01 = Mathf.Clamp01(silence01 + 0.2f);
        }

        float targetAmbianceVolume = ambianceVolume * AudioManager.AmbienceVolume * Mathf.Lerp(1f, 0.24f, silence01);
        targetAmbianceVolume *= Mathf.Lerp(0.92f, 1.06f, confinement);
        ambianceSource.volume = Mathf.MoveTowards(ambianceSource.volume, targetAmbianceVolume, Time.deltaTime * 0.85f);
        ambianceSource.pitch = Mathf.MoveTowards(ambianceSource.pitch, Mathf.Lerp(0.96f, 1.04f, confinement), Time.deltaTime * 0.4f);

        if (ambianceLowPass != null)
        {
            float targetCutoff = Mathf.Lerp(22000f, 1800f, silence01 * Mathf.Lerp(0.35f, 1f, confinement));
            ambianceLowPass.cutoffFrequency = Mathf.MoveTowards(ambianceLowPass.cutoffFrequency, targetCutoff, Time.deltaTime * 18000f);
        }

        if (musicLowPass != null && musicSource != null && musicSource.isPlaying)
        {
            float targetMusicCutoff = Mathf.Lerp(22000f, 2600f, silence01 * 0.7f);
            musicLowPass.cutoffFrequency = Mathf.MoveTowards(musicLowPass.cutoffFrequency, targetMusicCutoff, Time.deltaTime * 18000f);
        }

        if (ambianceReverb != null)
        {
            ambianceReverb.decayTime = Mathf.Lerp(0.9f, 1.9f, confinement);
            ambianceReverb.reflectionsLevel = Mathf.Lerp(-3000f, -800f, confinement);
            ambianceReverb.reverbLevel = Mathf.Lerp(-2200f, -900f, confinement);
        }
    }

    private void RestoreAmbientBed()
    {
        if (ambianceSource != null)
        {
            ambianceSource.volume = Mathf.MoveTowards(ambianceSource.volume, ambianceVolume * AudioManager.AmbienceVolume, Time.deltaTime * 0.8f);
            ambianceSource.pitch = Mathf.MoveTowards(ambianceSource.pitch, 1f, Time.deltaTime * 0.5f);
        }

        if (ambianceLowPass != null)
        {
            ambianceLowPass.cutoffFrequency = Mathf.MoveTowards(ambianceLowPass.cutoffFrequency, 22000f, Time.deltaTime * 18000f);
        }

        if (musicLowPass != null)
        {
            musicLowPass.cutoffFrequency = Mathf.MoveTowards(musicLowPass.cutoffFrequency, 22000f, Time.deltaTime * 18000f);
        }
    }

    private float EvaluateConfinement()
    {
        if (playerTransform == null)
        {
            return 0f;
        }

        Vector3 origin = playerTransform.position + Vector3.up * 1.45f;
        Vector3[] directions =
        {
            playerTransform.forward,
            -playerTransform.forward,
            playerTransform.right,
            -playerTransform.right
        };

        float normalizedTotal = 0f;
        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(origin, directions[i], out RaycastHit hit, confinedProbeDistance, environmentProbeMask, QueryTriggerInteraction.Ignore))
            {
                normalizedTotal += 1f - Mathf.Clamp01(hit.distance / confinedProbeDistance);
            }
        }

        return Mathf.Clamp01(normalizedTotal / directions.Length);
    }

    private void EnsureFilters()
    {
        if (ambianceSource != null)
        {
            ambianceLowPass = ambianceSource.GetComponent<AudioLowPassFilter>();
            if (ambianceLowPass == null)
            {
                ambianceLowPass = ambianceSource.gameObject.AddComponent<AudioLowPassFilter>();
            }

            ambianceReverb = ambianceSource.GetComponent<AudioReverbFilter>();
            if (ambianceReverb == null)
            {
                ambianceReverb = ambianceSource.gameObject.AddComponent<AudioReverbFilter>();
            }
        }

        if (musicSource != null)
        {
            musicLowPass = musicSource.GetComponent<AudioLowPassFilter>();
            if (musicLowPass == null)
            {
                musicLowPass = musicSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
        }
    }
}

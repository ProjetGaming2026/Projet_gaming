using UnityEngine;

public class AmbientSoundManager : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambianceSource;

    [Header("Ambiance de fond (joue tout le long)")]
    [SerializeField] private AudioClip backroomsAmbiance;
    [SerializeField, Range(0f, 1f)] private float ambianceVolume = 0.3f;

    [Header("Musique de chase (seulement si monstre court derrière)")]
    [SerializeField] private AudioClip chaseMusic;
    [SerializeField] private float chaseDistance = 10f; // distance pour déclencher la musique
    [SerializeField] private float fadeSpeed = 1.5f;

    [Header("Musique fin")]
    [SerializeField] private AudioClip winMusic;
    [SerializeField] private AudioClip loseMusic;

    private PrototypeGameManager gameManager;
    private Transform playerTransform;
    private Transform monsterTransform;
    private bool isChoseMusicPlaying;
    private PrototypeGameManager.GamePhase lastPhase;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        BasicMonsterController monster = FindFirstObjectByType<BasicMonsterController>();
        if (monster != null) monsterTransform = monster.transform;
    }

    private void Start()
    {
        // Ambiance Backrooms joue tout le long du jeu
        if (ambianceSource != null && backroomsAmbiance != null)
        {
            ambianceSource.clip = backroomsAmbiance;
            ambianceSource.loop = true;
            ambianceSource.volume = ambianceVolume;
            ambianceSource.Play();
        }

        // Musique de chase commence arrêtée
        if (musicSource != null)
        {
            musicSource.volume = 0f;
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        PrototypeGameManager.GamePhase phase = gameManager.CurrentPhase;

        // Musique de fin
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

        // Musique chase seulement pendant Chase ou Escape
        bool isActivePhase =
            phase == PrototypeGameManager.GamePhase.Chase ||
            phase == PrototypeGameManager.GamePhase.Escape;

        if (!isActivePhase)
        {
            FadeOutChaseMusic();
            return;
        }

        // Vérifie si le monstre court derrière le joueur
        if (playerTransform == null || monsterTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, monsterTransform.position);
        bool monsterIsClose = distance <= chaseDistance;

        if (monsterIsClose)
        {
            FadeInChaseMusic();
        }
        else
        {
            FadeOutChaseMusic();
        }
    }

    private void FadeInChaseMusic()
    {
        if (musicSource == null || chaseMusic == null) return;

        if (!musicSource.isPlaying || musicSource.clip != chaseMusic)
        {
            musicSource.clip = chaseMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        musicSource.volume = Mathf.MoveTowards(musicSource.volume, 1f, fadeSpeed * Time.deltaTime);
    }

    private void FadeOutChaseMusic()
    {
        if (musicSource == null) return;
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
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = false;
        musicSource.volume = 1f;
        musicSource.Play();
    }
}

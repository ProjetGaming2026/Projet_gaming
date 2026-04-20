using UnityEngine;

public class MonsterSounds : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource actionSource;

    [Header("Sons du monstre (quand proche)")]
    [SerializeField] private AudioClip[] idleGrowls;
    [SerializeField] private float minGrowlInterval = 4f;
    [SerializeField] private float maxGrowlInterval = 10f;
    [SerializeField] private float growlDistance = 10f; // distance pour entendre les grognements

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float growlVolume = 0.8f;

    private Transform playerTarget;
    private PrototypeGameManager gameManager;
    private float growlTimer;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;

        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
    }

    private void Update()
    {
        if (gameManager == null || playerTarget == null) return;

        // Sons actifs seulement pendant Chase ou Escape
        bool isActivePhase =
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Chase ||
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape;

        if (!isActivePhase) return;

        // Vérifie la distance avec le joueur
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        bool isClose = distance <= growlDistance;

        if (!isClose) return;

        // Joue un grognement si le monstre est proche
        growlTimer -= Time.deltaTime;
        if (growlTimer <= 0f)
        {
            PlayRandomClip(ambientSource, idleGrowls, growlVolume);
            growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
        }
    }

    private void PlayRandomClip(AudioSource source, AudioClip[] clips, float volume)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, volume);
    }
}

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MonsterSounds : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource actionSource;
    [SerializeField] private AudioSource footstepSource;

    [Header("Sons du monstre (quand proche)")]
    [SerializeField] private AudioClip[] idleGrowls;
    [SerializeField] private float minGrowlInterval = 4f;
    [SerializeField] private float maxGrowlInterval = 10f;
    [SerializeField] private float growlDistance = 10f;

    [Header("Pas du monstre")]
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] chaseFootsteps;
    [SerializeField] private float walkStepInterval = 0.65f;
    [SerializeField] private float chaseStepInterval = 0.38f;
    [SerializeField] private float movementThreshold = 0.2f;
    [SerializeField] private float runSpeedThreshold = 4.5f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float growlVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.75f;

    private Transform playerTarget;
    private PrototypeGameManager gameManager;
    private CharacterController controller;
    private float growlTimer;
    private float stepTimer;
    private bool wasRunning;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (footstepSource == null)
        {
            footstepSource = actionSource != null ? actionSource : ambientSource;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }

        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
    }

    private void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        bool isActivePhase =
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Chase ||
            gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape;

        if (!isActivePhase)
        {
            stepTimer = 0f;
            wasRunning = false;

            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }

            return;
        }

        HandleFootsteps();
        HandleGrowls();
    }

    private void HandleFootsteps()
    {
        if (controller == null)
        {
            return;
        }

        Vector3 planarVelocity = controller.velocity;
        planarVelocity.y = 0f;
        float planarSpeed = planarVelocity.magnitude;
        bool isMoving = planarSpeed > movementThreshold;

        if (!isMoving)
        {
            stepTimer = 0f;
            wasRunning = false;

            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }

            return;
        }

        bool isRunning = planarSpeed >= runSpeedThreshold;
        if (isRunning != wasRunning)
        {
            stepTimer = 0f;
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }

        float interval = isRunning ? chaseStepInterval : walkStepInterval;
        AudioClip[] clips = isRunning && chaseFootsteps != null && chaseFootsteps.Length > 0
            ? chaseFootsteps
            : walkFootsteps;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayRandomClip(footstepSource, clips, footstepVolume);
            stepTimer = interval;
        }

        wasRunning = isRunning;
    }

    private void HandleGrowls()
    {
        if (playerTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        bool isClose = distance <= growlDistance;

        if (!isClose)
        {
            return;
        }

        growlTimer -= Time.deltaTime;
        if (growlTimer <= 0f)
        {
            PlayRandomClip(ambientSource, idleGrowls, growlVolume);
            growlTimer = Random.Range(minGrowlInterval, maxGrowlInterval);
        }
    }

    private void PlayRandomClip(AudioSource source, AudioClip[] clips, float volume)
    {
        if (source == null || clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayClip(source, clip, volume);
    }

    private void PlayClip(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
        {
            return;
        }

        if (source.isPlaying)
        {
            return;
        }

        source.PlayOneShot(clip, volume);
    }
}

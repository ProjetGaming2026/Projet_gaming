using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerSounds : MonoBehaviour
{
    [Header("Sources Audio")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource actionSource;

    [Header("Sons de pas")]
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] sprintFootsteps;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;

    [Header("Sons d'action")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.6f;

    private CharacterController controller;
    private float stepTimer;
    private bool wasGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleFootsteps();
        HandleLanding();
    }

    private void HandleFootsteps()
    {
        // Vérifie si le joueur est au sol
        if (!controller.isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        // Vérifie si le joueur bouge vraiment
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f;

        // Si le joueur s'arrête -> stop immédiatement les sons
        if (!isMoving)
        {
            stepTimer = 0f;
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
            return;
        }

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float interval = isSprinting ? sprintStepInterval : walkStepInterval;
        AudioClip[] clips = isSprinting ? sprintFootsteps : walkFootsteps;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayRandomClip(footstepSource, clips, footstepVolume);
            stepTimer = interval;
        }
    }

    private void HandleLanding()
    {
        bool isGrounded = controller.isGrounded;

        if (!wasGrounded && isGrounded)
        {
            PlayClip(actionSource, landSound, footstepVolume);
        }

        wasGrounded = isGrounded;
    }

    public void PlayJumpSound()
    {
        PlayClip(actionSource, jumpSound, footstepVolume);
    }

    private void PlayRandomClip(AudioSource source, AudioClip[] clips, float volume)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        PlayClip(source, clip, volume);
    }

    private void PlayClip(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, volume * AudioManager.SfxVolume);
    }
}

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
    private BasicFirstPersonController playerController;
    private float stepTimer;
    private bool wasGrounded;
    private bool wasSprinting;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<BasicFirstPersonController>();
    }

    private void Update()
    {
        HandleFootsteps();
        HandleLanding();
    }

    private void HandleFootsteps()
    {
        if (!controller.isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        float planarSpeed = playerController != null
            ? playerController.CurrentPlanarSpeed
            : new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        bool isMoving = planarSpeed > 0.1f;

        if (!isMoving)
        {
            stepTimer = 0f;
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }

            wasSprinting = false;
            return;
        }

        bool isSprinting = playerController != null
            ? playerController.IsSprinting
            : Input.GetKey(KeyCode.LeftShift);

        if (isSprinting != wasSprinting)
        {
            stepTimer = 0f;
            if (footstepSource != null && footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }

        float interval = isSprinting ? sprintStepInterval : walkStepInterval;
        AudioClip[] clips = isSprinting && sprintFootsteps != null && sprintFootsteps.Length > 0
            ? sprintFootsteps
            : walkFootsteps;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayRandomClip(footstepSource, clips, footstepVolume);
            stepTimer = interval;
        }

        wasSprinting = isSprinting;
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

        if (source.isPlaying)
        {
            return;
        }

        source.PlayOneShot(clip, volume);
    }
}

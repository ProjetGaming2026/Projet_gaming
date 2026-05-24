using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform hinge;
    [SerializeField] private AudioSource audioSource;

    [Header("Motion")]
    [SerializeField] private float openAngle = 95f;
    [SerializeField] private float openSpeed = 6f;
    [SerializeField] private bool invertOpenDirection;
    [SerializeField] private bool startsOpen;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private float soundVolume = 0.8f;

    [Header("Lock")]
    [SerializeField] private bool isLocked;

    private Quaternion closedLocalRotation;
    private Quaternion targetLocalRotation;
    private bool isOpen;

    private void Awake()
    {
        if (hinge == null)
        {
            hinge = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        closedLocalRotation = hinge.localRotation;
        isOpen = startsOpen;
        targetLocalRotation = startsOpen
            ? closedLocalRotation * Quaternion.Euler(0f, ResolveOpenDirection() * openAngle, 0f)
            : closedLocalRotation;

        if (startsOpen)
        {
            hinge.localRotation = targetLocalRotation;
        }
    }

    private void Update()
    {
        if (hinge == null)
        {
            return;
        }

        hinge.localRotation = Quaternion.Slerp(
            hinge.localRotation,
            targetLocalRotation,
            Time.deltaTime * openSpeed
        );
    }

    public void Interact()
    {
        if (isLocked)
        {
            return;
        }

        if (hinge == null)
        {
            hinge = transform;
            closedLocalRotation = hinge.localRotation;
        }

        if (isOpen)
        {
            targetLocalRotation = closedLocalRotation;
            isOpen = false;
            PlaySound(closeSound);
            return;
        }

        int openDirection = ResolveOpenDirection();
        targetLocalRotation = closedLocalRotation * Quaternion.Euler(0f, openDirection * openAngle, 0f);
        isOpen = true;
        PlaySound(openSound);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    // Compatibility wrapper for older systems that still use the previous
    // interaction-lock API name.
    public void SetInteractionLocked(bool locked)
    {
        SetLocked(locked);
    }

    // Legacy compatibility helper for runtime bootstrap scripts that wire
    // existing door audio at scene load.
    public void AssignAudioClips(AudioClip openClip, AudioClip closeClip)
    {
        if (openClip != null)
        {
            openSound = openClip;
        }

        if (closeClip != null)
        {
            closeSound = closeClip;
        }
    }

    public void ForceClosed()
    {
        if (hinge == null)
        {
            hinge = transform;
        }

        targetLocalRotation = closedLocalRotation;
        hinge.localRotation = closedLocalRotation;
        isOpen = false;
    }

    // Compatibility property for legacy callers that expect a property.
    public bool IsOpen => isOpen;

    // Compatibility property for scripts that still query the previous
    // interaction-lock state name.
    public bool IsInteractionLocked => isLocked;

    // Compatibility helper for legacy systems that want to force the door open
    // through an explicit method call.
    public void OpenDoor()
    {
        if (!isOpen)
        {
            Interact();
        }
    }

    private int ResolveOpenDirection()
    {
        Transform playerTransform = null;

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        int direction = 1;
        if (playerTransform != null && hinge != null)
        {
            Vector3 toPlayer = playerTransform.position - hinge.position;
            float side = Vector3.Dot(hinge.right, toPlayer);
            direction = side >= 0f ? -1 : 1;
        }

        if (invertOpenDirection)
        {
            direction *= -1;
        }

        return direction;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, soundVolume);
    }
}

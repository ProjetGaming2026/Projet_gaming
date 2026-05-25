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
    [SerializeField] private bool interactionLocked;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private float soundVolume = 0.8f;

    private Quaternion closedLocalRotation;
    private Quaternion targetLocalRotation;
    private bool isOpen;
    private bool isInitialized;

    public bool IsOpen => isOpen;
    public bool IsInteractionLocked => interactionLocked;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void Update()
    {
        InitializeIfNeeded();

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
        InitializeIfNeeded();

        if (!ExitDoorCodeLock.IsOpeningLinkedDoors &&
            ExitDoorCodeLock.TryGetOrCreateDefaultLockForDoor(this, out ExitDoorCodeLock exitDoorLock))
        {
            Debug.Log("DoorInteractable: " + name + " is controlled by ExitDoorCodeLock.");
            exitDoorLock.Interact();
            return;
        }

        if (interactionLocked)
        {
            Debug.Log("DoorInteractable: interaction locked on " + name + ".");
            return;
        }

        ToggleDoor();
    }

    public void OpenDoor()
    {
        InitializeIfNeeded();

        if (isOpen)
        {
            return;
        }

        int openDirection = ResolveOpenDirection();
        targetLocalRotation = closedLocalRotation * Quaternion.Euler(0f, openDirection * openAngle, 0f);
        isOpen = true;
        PlaySound(openSound);
    }

    public void CloseDoor()
    {
        InitializeIfNeeded();

        if (!isOpen)
        {
            return;
        }

        targetLocalRotation = closedLocalRotation;
        isOpen = false;
        PlaySound(closeSound);
    }

    public void ForceClosed()
    {
        InitializeIfNeeded();

        targetLocalRotation = closedLocalRotation;
        isOpen = false;

        if (hinge != null)
        {
            hinge.localRotation = closedLocalRotation;
        }
    }

    public void SetLocked(bool locked)
    {
        SetInteractionLocked(locked);
    }

    public void SetInteractionLocked(bool locked)
    {
        InitializeIfNeeded();
        interactionLocked = locked;

        if (interactionLocked && isOpen)
        {
            targetLocalRotation = closedLocalRotation;
            isOpen = false;
        }
    }

    private void ToggleDoor()
    {
        if (isOpen)
        {
            CloseDoor();
            return;
        }

        OpenDoor();
    }

    public void AssignAudioClips(AudioClip newOpenSound, AudioClip newCloseSound)
    {
        if (newOpenSound != null)
        {
            openSound = newOpenSound;
        }

        if (newCloseSound != null)
        {
            closeSound = newCloseSound;
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
        InitializeIfNeeded();

        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, soundVolume);
    }

    private void InitializeIfNeeded()
    {
        if (hinge == null)
        {
            hinge = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (isInitialized || hinge == null)
        {
            return;
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

        isInitialized = true;
    }
}

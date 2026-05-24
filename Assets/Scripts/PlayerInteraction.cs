using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float interactRadius = 0.14f;
    [SerializeField] private Transform interactionOrigin;

    private PlayerInteractor modernInteractor;

    private void Awake()
    {
        modernInteractor = GetComponent<PlayerInteractor>();
        RefreshInteractionOrigin();
    }

    private void Update()
    {
        if (modernInteractor != null && modernInteractor.enabled)
        {
            return;
        }

        RefreshInteractionOrigin();

        if (interactionOrigin == null)
        {
            return;
        }

        Debug.DrawRay(interactionOrigin.position, interactionOrigin.forward * interactRange, Color.red);

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        IInteractable interactable = FindInteractableInFront();
        if (interactable == null)
        {
            return;
        }

        interactable.Interact();
    }

    private void RefreshInteractionOrigin()
    {
        if (interactionOrigin != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            interactionOrigin = Camera.main.transform;
            return;
        }

        Camera fallbackCamera = GetComponentInChildren<Camera>(true);
        if (fallbackCamera != null)
        {
            interactionOrigin = fallbackCamera.transform;
        }
    }

    private IInteractable FindInteractableInFront()
    {
        Ray ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            interactRadius,
            interactRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = ResolveInteractable(hits[i].collider);
            if (interactable == null)
            {
                continue;
            }

            if (interactable is Behaviour behaviour && !behaviour.isActiveAndEnabled)
            {
                continue;
            }

            return interactable;
        }

        return null;
    }

    private static IInteractable ResolveInteractable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        KeyDoor keyDoor = hitCollider.GetComponent<KeyDoor>();
        if (keyDoor != null)
        {
            return keyDoor;
        }

        keyDoor = hitCollider.GetComponentInParent<KeyDoor>();
        if (keyDoor != null)
        {
            return keyDoor;
        }

        keyDoor = hitCollider.GetComponentInChildren<KeyDoor>();
        if (keyDoor != null)
        {
            return keyDoor;
        }

        ExitDoorCodeLock exitDoorLock = hitCollider.GetComponent<ExitDoorCodeLock>();
        if (exitDoorLock != null)
        {
            return exitDoorLock;
        }

        exitDoorLock = hitCollider.GetComponentInParent<ExitDoorCodeLock>();
        if (exitDoorLock != null)
        {
            return exitDoorLock;
        }

        exitDoorLock = hitCollider.GetComponentInChildren<ExitDoorCodeLock>();
        if (exitDoorLock != null)
        {
            return exitDoorLock;
        }

        DoorInteractable linkedDoor = hitCollider.GetComponent<DoorInteractable>();
        if (linkedDoor == null)
        {
            linkedDoor = hitCollider.GetComponentInParent<DoorInteractable>();
        }

        if (linkedDoor == null)
        {
            linkedDoor = hitCollider.GetComponentInChildren<DoorInteractable>();
        }

        if (ExitDoorCodeLock.TryGetOrCreateDefaultLockForDoor(linkedDoor, out exitDoorLock))
        {
            return exitDoorLock;
        }

        PadlockPuzzle padlockPuzzle = hitCollider.GetComponent<PadlockPuzzle>();
        if (padlockPuzzle != null)
        {
            return padlockPuzzle;
        }

        padlockPuzzle = hitCollider.GetComponentInParent<PadlockPuzzle>();
        if (padlockPuzzle != null)
        {
            return padlockPuzzle;
        }

        padlockPuzzle = hitCollider.GetComponentInChildren<PadlockPuzzle>();
        if (padlockPuzzle != null)
        {
            return padlockPuzzle;
        }

        IInteractable onCollider = hitCollider.GetComponent<IInteractable>();
        if (onCollider != null)
        {
            return onCollider;
        }

        IInteractable inParent = hitCollider.GetComponentInParent<IInteractable>();
        if (inParent != null)
        {
            return inParent;
        }

        return hitCollider.GetComponentInChildren<IInteractable>();
    }
}

using System;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform interactionOrigin;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float interactRadius = 0.16f;

    public bool HasInteractableTarget => currentInteractable != null;
    public string CurrentPromptText => currentPromptText;

    private IInteractable currentInteractable;
    private Collider currentInteractableCollider;
    private string currentPromptText;

    private void Awake()
    {
        RefreshInteractionOrigin();
    }

    private void Update()
    {
        if (CipherTerminal.IsAnyTerminalOpen)
        {
            currentInteractable = null;
            currentInteractableCollider = null;
            currentPromptText = string.Empty;
            return;
        }

        UpdateCurrentInteractable();

        if (currentInteractable is ExitDoorCodeLock)
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }

            return;
        }

        if (currentInteractable is PadlockButton padlockButton && padlockButton.TryHandlePromptKeyDown())
        {
            return;
        }

        if (currentInteractable is PadlockController padlockController && padlockController.TryHandleKeyboardInput())
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        UpdateCurrentInteractable();
        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact();
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

    private void UpdateCurrentInteractable()
    {
        RefreshInteractionOrigin();
        currentInteractable = null;
        currentInteractableCollider = null;
        currentPromptText = string.Empty;

        if (interactionOrigin == null)
        {
            return;
        }

        Ray ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            interactRadius,
            interactDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = ResolveInteractable(hits[i].collider);
            if (interactable == null || !IsInteractableUsable(interactable))
            {
                continue;
            }

            currentInteractable = interactable;
            currentInteractableCollider = hits[i].collider;
            currentPromptText = BuildPromptText(interactable, currentInteractableCollider);
            return;
        }
    }

    private static bool IsInteractableUsable(IInteractable interactable)
    {
        if (interactable is Behaviour behaviour)
        {
            return behaviour.isActiveAndEnabled;
        }

        return true;
    }

    private static string BuildPromptText(IInteractable interactable, Collider sourceCollider)
    {
        if (interactable is ExitDoorCodeLock exitDoorCodeLock)
        {
            return exitDoorCodeLock.GetInteractPrompt();
        }

        if (interactable is KeyDoor keyDoor)
        {
            return keyDoor.GetInteractPrompt();
        }

        if (interactable is DoorInteractable door)
        {
            if (door.IsInteractionLocked)
            {
                return "Appuie sur E pour entrer le code de sortie";
            }

            return door.IsOpen
                ? "Appuie sur E pour fermer"
                : "Appuie sur E pour ouvrir";
        }

        if (interactable is TaskTerminal)
        {
            return "Appuie sur E pour utiliser le terminal";
        }

        if (interactable is PadlockButton padlockButton)
        {
            return padlockButton.GetInteractPrompt();
        }

        if (interactable is PadlockController padlockController)
        {
            return padlockController.GetInteractPrompt();
        }

        if (interactable is PadlockPuzzle)
        {
            return "Appuie sur E pour interagir avec le cadenas";
        }

        if (interactable is RadioInteraction)
        {
            return "Appuie sur E pour activer la radio";
        }

        if (interactable is KeyPickup keyPickup)
        {
            return keyPickup.GetInteractPrompt();
        }

        if (interactable is OrganPickup organPickup)
        {
            return organPickup.GetInteractPrompt();
        }

        if (interactable is DocumentPickup documentPickup)
        {
            return documentPickup.GetInteractPrompt();
        }

        string targetName = sourceCollider != null ? sourceCollider.name : "objet";
        return "Appuie sur E pour interagir avec " + targetName;
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

        if (ExitDoorCodeLock.TryGetLockForDoor(linkedDoor, out exitDoorLock))
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

        IInteractable interactable = hitCollider.GetComponent<IInteractable>();
        if (interactable != null)
        {
            return interactable;
        }

        interactable = hitCollider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            return interactable;
        }

        return hitCollider.GetComponentInChildren<IInteractable>();
    }
}

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
            TryFindVisibleLaboratoryKey(ray);
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

        TryFindVisibleLaboratoryKey(ray);
    }

    private void TryFindVisibleLaboratoryKey(Ray ray)
    {
        KeyControl[] keys = FindObjectsByType<KeyControl>(FindObjectsSortMode.None);
        KeyControl bestKey = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < keys.Length; i++)
        {
            KeyControl key = keys[i];
            if (key == null || !key.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 toKey = key.transform.position - ray.origin;
            float forwardDistance = Vector3.Dot(ray.direction, toKey);
            if (forwardDistance < 0f || forwardDistance > interactDistance)
            {
                continue;
            }

            Vector3 closestPoint = ray.origin + ray.direction * forwardDistance;
            float sideDistance = Vector3.Distance(closestPoint, key.transform.position);
            if (sideDistance > Mathf.Max(interactRadius * 4f, 0.65f))
            {
                continue;
            }

            float score = forwardDistance + sideDistance;
            if (score < bestScore)
            {
                bestScore = score;
                bestKey = key;
            }
        }

        if (bestKey == null)
        {
            return;
        }

        currentInteractable = bestKey;
        currentInteractableCollider = bestKey.GetComponent<Collider>();
        currentPromptText = bestKey.GetInteractPrompt();
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
                return GameLocalization.Text("Press E to enter the exit code", "Appuie sur E pour entrer le code de sortie", "Pulsa E para introducir el codigo de salida");
            }

            return door.IsOpen
                ? GameLocalization.Text("Press E to close", "Appuie sur E pour fermer", "Pulsa E para cerrar")
                : GameLocalization.Text("Press E to open", "Appuie sur E pour ouvrir", "Pulsa E para abrir");
        }

        if (interactable is TaskTerminal)
        {
            return GameLocalization.Text("Press E to use the terminal", "Appuie sur E pour utiliser le terminal", "Pulsa E para usar el terminal");
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
            return GameLocalization.Text("Press E to interact with the padlock", "Appuie sur E pour interagir avec le cadenas", "Pulsa E para interactuar con el candado");
        }

        if (interactable is RadioInteraction)
        {
            return GameLocalization.Text("Press E to activate the radio", "Appuie sur E pour activer la radio", "Pulsa E para activar la radio");
        }

        if (interactable is ChemistryPuzzleManager chemistryPuzzle)
        {
            return chemistryPuzzle.GetInteractPrompt();
        }

        if (interactable is FlaskClick flask)
        {
            return flask.GetInteractPrompt();
        }

        if (interactable is KeyControl labKey)
        {
            return labKey.GetInteractPrompt();
        }

        if (interactable is LockedDoor lockedDoor)
        {
            return lockedDoor.GetInteractPrompt();
        }

        if (interactable is KeyPickup keyPickup)
        {
            return keyPickup.GetInteractPrompt();
        }

        string targetName = sourceCollider != null ? sourceCollider.name : "objet";
        return GameLocalization.Text(
            "Press E to interact with " + targetName,
            "Appuie sur E pour interagir avec " + targetName,
            "Pulsa E para interactuar con " + targetName);
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

using System;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    private const string NonInteractiveObjectName = "bab";

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
        if (PhonePuzzleUI.IsReadableOverlayOpen)
        {
            PhonePuzzleUI.Instance?.TryCloseReadableTextFromKeyboard();
            currentInteractable = null;
            currentInteractableCollider = null;
            currentPromptText = string.Empty;
            SetPromptVisible(string.Empty);
            return;
        }

        if (NotebookPickup.IsAnyNotebookOpen || TVCodePuzzleController.IsAnyCodeUiOpen || PatientDossier.BlocksInteraction || PhoneAnswerInput.IsAnyAnswerInputOpen)
        {
            currentInteractable = null;
            currentInteractableCollider = null;
            currentPromptText = string.Empty;
            SetPromptVisible(string.Empty);
            return;
        }

        if (CipherTerminal.IsAnyTerminalOpen)
        {
            currentInteractable = null;
            currentInteractableCollider = null;
            currentPromptText = string.Empty;
            SetPromptVisible(string.Empty);
            return;
        }

        UpdateCurrentInteractable();
        SetPromptVisible(currentPromptText);

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

        if (TrySelectReadableWallList(ray, hits))
        {
            return;
        }

        IInteractable bestInteractable = null;
        Collider bestCollider = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (IsNonInteractiveNamedObjectCollider(hitCollider))
            {
                return;
            }

            IInteractable interactable = ResolveInteractable(hitCollider);
            if (interactable == null || !IsInteractableUsable(interactable))
            {
                continue;
            }

            float score = CalculateInteractionScore(ray, hits[i], interactable);
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestInteractable = interactable;
            bestCollider = hits[i].collider;
        }

        if (bestInteractable == null)
        {
            return;
        }

        currentInteractable = bestInteractable;
        currentInteractableCollider = bestCollider;
        currentPromptText = BuildPromptText(bestInteractable, currentInteractableCollider);
    }

    private bool TrySelectReadableWallList(Ray ray, RaycastHit[] hits)
    {
        ReadableWallList bestList = null;
        Collider bestCollider = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            ReadableWallList list = ResolveReadableWallList(hits[i].collider);
            if (list == null || !IsInteractableUsable(list))
            {
                continue;
            }

            Vector3 targetPoint = hits[i].collider.bounds.center;
            float distanceFromViewCenter = Vector3.Cross(ray.direction, targetPoint - ray.origin).magnitude;
            float score = distanceFromViewCenter * 10f + hits[i].distance * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                bestList = list;
                bestCollider = hits[i].collider;
            }
        }

        if (bestList == null)
        {
            return false;
        }

        currentInteractable = bestList;
        currentInteractableCollider = bestCollider;
        currentPromptText = BuildPromptText(bestList, bestCollider);
        return true;
    }

    private static ReadableWallList ResolveReadableWallList(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        ReadableWallList list = hitCollider.GetComponent<ReadableWallList>();
        if (list != null)
        {
            return list;
        }

        return hitCollider.GetComponentInParent<ReadableWallList>();
    }

    private static bool IsInteractableUsable(IInteractable interactable)
    {
        if (interactable is Interactable baseInteractable && !baseInteractable.CanInteract)
        {
            return false;
        }

        if (interactable is Behaviour behaviour)
        {
            return behaviour.isActiveAndEnabled;
        }

        return true;
    }

    private static float CalculateInteractionScore(Ray ray, RaycastHit hit, IInteractable interactable)
    {
        if (interactable is SurgicalInstrumentInteractable instrument)
        {
            if (!instrument.CanInspectFrom(ray.origin))
            {
                return float.PositiveInfinity;
            }

            return instrument.GetInteractionScore(ray, hit);
        }

        if (hit.collider == null)
        {
            return hit.distance;
        }

        Bounds bounds = hit.collider.bounds;
        Vector3 closestPointOnRay = ClosestPointOnRay(ray, bounds.center);
        float rayToBounds = Mathf.Sqrt(bounds.SqrDistance(closestPointOnRay));
        return rayToBounds * 12f + hit.distance;
    }

    private static Vector3 ClosestPointOnRay(Ray ray, Vector3 point)
    {
        float distance = Vector3.Dot(point - ray.origin, ray.direction);
        return ray.origin + ray.direction * Mathf.Max(0f, distance);
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

        if (interactable is KeyPickup keyPickup)
        {
            return keyPickup.GetInteractPrompt();
        }

        if (interactable is PatientPuzzleController patientPuzzle)
        {
            return patientPuzzle.GetInteractPrompt();
        }

        if (interactable is PatientPuzzlePickup patientPuzzlePickup)
        {
            return patientPuzzlePickup.GetInteractPrompt();
        }

        if (interactable is TVCodePuzzleController tvCodePuzzle)
        {
            return tvCodePuzzle.GetInteractPrompt();
        }

        if (interactable is BedtableFlashlightController bedtableFlashlight)
        {
            return bedtableFlashlight.GetInteractPrompt();
        }

        if (interactable is OldFlashlightPickup oldFlashlight)
        {
            return oldFlashlight.GetInteractPrompt();
        }

        if (interactable is NotebookPickup notebookPickup)
        {
            return notebookPickup.GetInteractPrompt();
        }

        if (interactable is PatientDossier patientDossier)
        {
            return patientDossier.GetInteractPrompt();
        }

        if (interactable is Interactable baseInteractable)
        {
            return baseInteractable.GetInteractPrompt();
        }
        string targetName = sourceCollider != null ? sourceCollider.name : "objet";
        return GameLocalization.Text(
            "Press E to interact with " + targetName,
            "Appuie sur E pour interagir avec " + targetName,
            "Pulsa E para interactuar con " + targetName);
    }

    private static void SetPromptVisible(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.Hide();
            }

            return;
        }

        PhonePuzzleUI.EnsureExists().Prompt.Show(message);
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

        KeypadBoxCode keypadBox = hitCollider.GetComponent<KeypadBoxCode>();
        if (keypadBox != null)
        {
            return keypadBox;
        }

        keypadBox = hitCollider.GetComponentInParent<KeypadBoxCode>();
        if (keypadBox != null)
        {
            return keypadBox;
        }

        keypadBox = hitCollider.GetComponentInChildren<KeypadBoxCode>();
        if (keypadBox != null)
        {
            return keypadBox;
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

        return null;
    }

    private static bool IsNonInteractiveNamedObjectCollider(Collider hitCollider)
    {
        return hitCollider != null && HasAncestorNamed(hitCollider.transform, NonInteractiveObjectName);
    }

    private static bool HasAncestorNamed(Transform transformToCheck, string ancestorName)
    {
        Transform current = transformToCheck;
        while (current != null)
        {
            if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}

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
    private IInteractable currentInteractable;
    private string currentPromptText;

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

        if (PhonePuzzleUI.IsReadableOverlayOpen)
        {
            PhonePuzzleUI.Instance?.TryCloseReadableTextFromKeyboard();
            currentInteractable = null;
            currentPromptText = string.Empty;
            SetPromptVisible(string.Empty);
            return;
        }

        if (PhoneAnswerInput.IsAnyAnswerInputOpen)
        {
            currentInteractable = null;
            currentPromptText = string.Empty;
            SetPromptVisible(string.Empty);
            return;
        }

        RefreshInteractionOrigin();

        if (interactionOrigin == null)
        {
            SetPromptVisible(string.Empty);
            return;
        }

        Debug.DrawRay(interactionOrigin.position, interactionOrigin.forward * interactRange, Color.red);
        currentInteractable = FindInteractableInFront();
        currentPromptText = BuildPromptText(currentInteractable);
        SetPromptVisible(currentPromptText);

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

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

        IInteractable readableList = SelectReadableWallList(ray, hits);
        if (readableList != null)
        {
            return readableList;
        }

        IInteractable bestInteractable = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = ResolveInteractable(hits[i].collider);
            if (interactable == null)
            {
                continue;
            }

            if (interactable is Interactable baseInteractable && !baseInteractable.CanInteract)
            {
                continue;
            }

            if (interactable is Behaviour behaviour && !behaviour.isActiveAndEnabled)
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
        }

        return bestInteractable;
    }

    private static IInteractable SelectReadableWallList(Ray ray, RaycastHit[] hits)
    {
        ReadableWallList bestList = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            ReadableWallList list = ResolveReadableWallList(hits[i].collider);
            if (list == null || !list.CanInteract)
            {
                continue;
            }

            if (!list.isActiveAndEnabled)
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
            }
        }

        return bestList;
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

        return null;
    }

    private static string BuildPromptText(IInteractable interactable)
    {
        if (interactable is Interactable baseInteractable)
        {
            return baseInteractable.GetInteractPrompt();
        }

        return interactable != null ? "Press E to inspect" : string.Empty;
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
}

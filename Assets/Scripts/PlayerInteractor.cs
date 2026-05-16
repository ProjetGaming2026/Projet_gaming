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
private void Update()
{
    if (CipherTerminal.IsAnyTerminalOpen)
    {
        return;
    }

    UpdateCurrentInteractable();

    if (Input.GetKeyDown(interactKey))
    {
        TryInteract();
    }
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
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable interactable = hits[i].collider.GetComponentInParent<IInteractable>();
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
        if (interactable is DoorInteractable door)
        {
            return door.IsOpen
                ? "Appuie sur E pour fermer"
                : "Appuie sur E pour ouvrir";
        }

        if (interactable is TaskTerminal)
        {
            return "Appuie sur E pour utiliser le terminal";
        }

        string targetName = sourceCollider != null ? sourceCollider.name : "objet";
        return "Appuie sur E pour interagir avec " + targetName;
    }
}

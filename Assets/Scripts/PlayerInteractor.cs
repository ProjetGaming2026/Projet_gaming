using UnityEngine;
using System;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform interactionOrigin;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactDistance = 2.5f;

    private void Awake()
    {
        if (interactionOrigin == null && Camera.main != null)
        {
            interactionOrigin = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (KeypadUIManager.IsUiOpen || PatientUIManager.IsUiOpen || XRayUIManager.IsUiOpen)
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
        if (interactionOrigin == null)
        {
            Debug.LogWarning("[PlayerInteractor] Missing interaction origin.");
            return;
        }

        Ray ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.18f, interactDistance);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            PatientFolder patientFolder = hit.collider.GetComponentInParent<PatientFolder>();
            if (patientFolder != null)
            {
                Debug.Log($"[PlayerInteractor] Interacting with hit patient folder {hit.collider.name}.");
                patientFolder.Interact();
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                Debug.Log($"[PlayerInteractor] Interacting with {hit.collider.name}.");
                interactable.Interact();
                return;
            }
        }

        Vector3 overlapCenter = interactionOrigin.position + interactionOrigin.forward * (interactDistance * 0.65f);
        Collider[] nearbyColliders = Physics.OverlapSphere(overlapCenter, 0.85f);
        Array.Sort(nearbyColliders, (left, right) =>
        {
            float leftDot = Vector3.Dot(interactionOrigin.forward, (left.bounds.center - interactionOrigin.position).normalized);
            float rightDot = Vector3.Dot(interactionOrigin.forward, (right.bounds.center - interactionOrigin.position).normalized);
            return rightDot.CompareTo(leftDot);
        });

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            Vector3 toCollider = (nearbyCollider.bounds.center - interactionOrigin.position).normalized;
            if (Vector3.Dot(interactionOrigin.forward, toCollider) < 0.72f)
            {
                continue;
            }

            PatientFolder patientFolder = nearbyCollider.GetComponentInParent<PatientFolder>();
            if (patientFolder != null)
            {
                Debug.Log($"[PlayerInteractor] Interacting with nearby patient folder collider {nearbyCollider.name}.");
                patientFolder.Interact();
                return;
            }

            IInteractable interactable = nearbyCollider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                Debug.Log($"[PlayerInteractor] Interacting with nearby object {nearbyCollider.name}.");
                interactable.Interact();
                return;
            }
        }

        Debug.Log("[PlayerInteractor] No interactable found in range.");
    }
}

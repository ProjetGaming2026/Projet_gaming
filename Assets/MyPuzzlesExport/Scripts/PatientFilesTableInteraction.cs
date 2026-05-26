using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PatientFilesTableInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private float interactionDistance = 2.5f;

    private Collider tableCollider;

    private void Awake()
    {
        tableCollider = GetComponent<Collider>();
        if (tableCollider != null)
        {
            tableCollider.isTrigger = false;
        }
    }

    private void Update()
    {
        if (PatientUIManager.IsUiOpen || !IsTableUnlocked())
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
            return;
        }

        if (ShouldShowPrompt())
        {
            KeypadUIManager.Instance?.ShowInteractionPrompt("Appuyer E pour interagir", this);
        }
        else
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
        }
    }

    public void Interact()
    {
        if (!IsTableUnlocked())
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
            return;
        }

        if (!CanInteractFromMainCamera())
        {
            Debug.Log("[PatientFilesTableInteraction] Table is too far away.");
            return;
        }

        if (PatientUIManager.Instance == null)
        {
            Debug.LogWarning("[PatientFilesTableInteraction] No PatientUIManager found.");
            return;
        }

        Debug.Log("[PatientFilesTableInteraction] Opening patient files board.");
        PatientUIManager.Instance.ShowAllOrdered();
    }

    private bool ShouldShowPrompt()
    {
        if (Camera.main == null || !IsTableUnlocked())
        {
            return false;
        }

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 toTable = (transform.position - cameraPosition).normalized;
        return Vector3.Distance(cameraPosition, transform.position) <= interactionDistance
            && Vector3.Dot(Camera.main.transform.forward, toTable) > 0.82f;
    }

    private bool CanInteractFromMainCamera()
    {
        if (Camera.main == null)
        {
            return true;
        }

        return Vector3.Distance(Camera.main.transform.position, transform.position) <= interactionDistance;
    }

    private bool IsTableUnlocked()
    {
        return CodePuzzleManager.Instance != null && CodePuzzleManager.Instance.IsPuzzleSolved;
    }
}

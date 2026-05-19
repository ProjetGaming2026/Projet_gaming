using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PatientFolder : MonoBehaviour, IInteractable
{
    [Header("Patient Data")]
    public int patientID;
    public string patientName;
    public int patientAge;
    public string disease;
    [TextArea(4, 8)] public string medicalNotes;
    public bool isRead = false;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2.5f;

    private Collider folderCollider;

    private void Awake()
    {
        folderCollider = GetComponent<Collider>();
        if (folderCollider != null)
        {
            folderCollider.isTrigger = false;
        }
    }

    public void Interact()
    {
        if (!CanInteractFromMainCamera())
        {
            Debug.Log($"[PatientFolder] {name} is too far to read.");
            return;
        }

        if (PatientUIManager.Instance == null)
        {
            Debug.LogWarning($"[PatientFolder] No PatientUIManager available for {name}.", this);
            return;
        }

        Debug.Log($"[PatientFolder] Opening patient folder #{patientID:00} ({patientName}).", this);
        PatientUIManager.Instance.Show(this);
    }

    public bool CanInteractFromMainCamera()
    {
        if (Camera.main == null)
        {
            return true;
        }

        Vector3 cameraPosition = Camera.main.transform.position;
        return Vector3.Distance(cameraPosition, transform.position) <= interactionDistance;
    }
}

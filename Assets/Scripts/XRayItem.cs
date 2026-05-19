using UnityEngine;

[RequireComponent(typeof(Collider))]
public class XRayItem : MonoBehaviour, IInteractable
{
    [Header("XRay Data")]
    public int xrayID;
    public string bodyPartName;
    public Sprite xraySprite;
    public int correctPatientID;
    public bool isSolved = false;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2.5f;

    private Renderer cachedRenderer;
    private Color originalColor = Color.white;

    private void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            originalColor = cachedRenderer.material.color;
        }

        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
        {
            itemCollider.isTrigger = false;
        }
    }

    public void Interact()
    {
        if (!CanInteractFromMainCamera())
        {
            Debug.Log($"[XRayItem] {name} is too far to inspect.");
            return;
        }

        if (XRayPuzzleManager.Instance == null)
        {
            Debug.LogWarning($"[XRayItem] No XRayPuzzleManager available for {name}.", this);
            return;
        }

        if (!XRayPuzzleManager.Instance.HasAnyFolderBeenRead())
        {
            XRayUIManager.Instance?.ShowToast("Lisez d'abord les dossiers !", new Color(1f, 0.35f, 0.35f, 1f), 2f);
            Debug.Log("[XRayItem] Player tried an X-ray before reading any folder.");
            return;
        }

        XRayUIManager.Instance?.Show(this);
    }

    public void MarkSolvedVisual()
    {
        isSolved = true;
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            cachedRenderer.material.color = new Color(0.25f, 0.9f, 0.35f, 1f);
        }
    }

    public void ResetVisual()
    {
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            cachedRenderer.material.color = originalColor;
        }
    }

    private bool CanInteractFromMainCamera()
    {
        if (Camera.main == null)
        {
            return true;
        }

        return Vector3.Distance(Camera.main.transform.position, transform.position) <= interactionDistance;
    }
}

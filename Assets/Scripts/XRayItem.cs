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
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private float lookDotThreshold = 0.94f;

    private Renderer cachedRenderer;
    private Color originalColor = Color.white;
    private bool promptVisible;

    private void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            originalColor = cachedRenderer.material.color;
        }

        EnsureUsableCollider();
    }

    private void Update()
    {
        if (KeypadUIManager.IsUiOpen || PatientUIManager.IsUiOpen || XRayUIManager.IsUiOpen)
        {
            if (promptVisible)
            {
                KeypadUIManager.Instance?.HideInteractionPrompt(this);
                promptVisible = false;
            }

            return;
        }

        if (ShouldShowPrompt())
        {
            if (!promptVisible)
            {
                Debug.Log($"Looking at XRay: {name}");
                KeypadUIManager.Instance?.ShowInteractionPrompt("Appuyer E pour selectionner la radio", this);
                promptVisible = true;
            }
        }
        else if (promptVisible)
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
            promptVisible = false;
        }
    }

    public void Interact()
    {
        Debug.Log($"Pressed E on XRay: {name}");

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

        Debug.Log("foldersRead = " + XRayPuzzleManager.Instance.foldersRead);

        if (!XRayPuzzleManager.Instance.HasAnyFolderBeenRead())
        {
            XRayUIManager.Instance?.ShowToast("Lisez d'abord les dossiers !", new Color(1f, 0.35f, 0.35f, 1f), 2f);
            Debug.Log("[XRayItem] Player tried an X-ray before reading any folder.");
            return;
        }

        Debug.Log($"XRay selected: {bodyPartName}");
        XRayPuzzleManager.Instance.SelectXRay(this);
    }

    public void MarkSolvedVisual()
    {
        isSolved = true;
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            cachedRenderer.material.color = new Color(0.25f, 0.9f, 0.35f, 1f);
        }

        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_BaseColor"))
        {
            cachedRenderer.material.SetColor("_BaseColor", new Color(0.25f, 0.9f, 0.35f, 1f));
        }
    }

    public void MarkErrorVisual()
    {
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            cachedRenderer.material.color = new Color(1f, 0.25f, 0.25f, 1f);
        }

        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_BaseColor"))
        {
            cachedRenderer.material.SetColor("_BaseColor", new Color(1f, 0.25f, 0.25f, 1f));
        }
    }

    public void ResetVisual()
    {
        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_Color"))
        {
            cachedRenderer.material.color = originalColor;
        }

        if (cachedRenderer != null && cachedRenderer.material.HasProperty("_BaseColor"))
        {
            cachedRenderer.material.SetColor("_BaseColor", originalColor);
        }
    }

    private bool CanInteractFromMainCamera()
    {
        if (Camera.main == null)
        {
            return true;
        }

        Vector3 targetPoint = cachedRenderer != null ? cachedRenderer.bounds.center : transform.position;
        return Vector3.Distance(Camera.main.transform.position, targetPoint) <= interactionDistance;
    }

    private bool ShouldShowPrompt()
    {
        if (Camera.main == null)
        {
            return false;
        }

        Vector3 targetPoint = cachedRenderer != null ? cachedRenderer.bounds.center : transform.position;
        Vector3 toTarget = (targetPoint - Camera.main.transform.position).normalized;
        float distance = Vector3.Distance(Camera.main.transform.position, targetPoint);
        float dot = Vector3.Dot(Camera.main.transform.forward, toTarget);
        return distance <= interactionDistance && dot >= lookDotThreshold;
    }

    private void EnsureUsableCollider()
    {
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider == null)
        {
            itemCollider = gameObject.AddComponent<BoxCollider>();
        }

        itemCollider.isTrigger = false;

        BoxCollider boxCollider = itemCollider as BoxCollider;
        if (boxCollider == null)
        {
            return;
        }

        if (cachedRenderer == null)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(1f, 1f, 0.05f);
            return;
        }

        Bounds localBounds = TransformBoundsToLocal(cachedRenderer.bounds);
        boxCollider.center = localBounds.center;
        Vector3 size = localBounds.size;
        size.z = Mathf.Max(size.z, 0.05f);
        boxCollider.size = size;
    }

    private Bounds TransformBoundsToLocal(Bounds worldBounds)
    {
        Vector3 center = transform.InverseTransformPoint(worldBounds.center);
        Vector3 extents = worldBounds.extents;

        Vector3 axisX = transform.InverseTransformVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = transform.InverseTransformVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = transform.InverseTransformVector(new Vector3(0f, 0f, extents.z));
        Vector3 localExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));

        return new Bounds(center, localExtents * 2f);
    }
}

using UnityEngine;

/// <summary>
/// Attach this to each Slot child object on the Tray.
/// Each slot expects a specific organ (by tag) and a specific order number.
/// </summary>
public class SlotController : MonoBehaviour
{
    [Header("Slot Configuration")]
    public string expectedOrganTag = "Heart"; // Must match the organ's tag
    public int slotNumber = 1;                // Order position on the tray (1, 2, 3...)

    [Header("Visual Feedback")]
    public GameObject glowEffect;             // Optional: glow/highlight when organ is placed
    public Material emptyMaterial;            // Material when slot is empty
    public Material filledMaterial;           // Material when slot is correctly filled
    public Renderer slotRenderer;
    public float placementSurfacePadding = 0.002f;

    [Header("Sound")]
    public AudioClip correctPlaceSound;
    public AudioClip wrongPlaceSound;

    private bool isOccupied = false;
    private AudioSource audioSource;

    // Reference to the TrayManager that owns this slot
    private TrayManager trayManager;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        trayManager = GetComponentInParent<TrayManager>();

        if (glowEffect != null)
            glowEffect.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Placement is handled explicitly from OrganPickup.Drop().
        // This avoids organs snapping into trays at scene start just because they overlap a trigger.
    }

    public bool TryPlaceOrgan(OrganPickup organ)
    {
        if (isOccupied || organ == null || organ.IsPlaced())
            return false;

        if (organ.GetOrganName() == expectedOrganTag)
        {
            // CORRECT organ!
            Debug.Log($"[Slot {slotNumber}] Correct! {expectedOrganTag} placed.");
            PlaceOrganCorrectly(organ);
            return true;
        }

        // WRONG organ
        Debug.Log($"[Slot {slotNumber}] Wrong organ! Expected {expectedOrganTag}, got {organ.GetOrganName()}");
        if (trayManager != null)
            trayManager.ShowPlacementFeedback("Wrong organ. Check the paper and use tray " + slotNumber + ".");

        PuzzleFailureMonsterAlert.Alert(transform);
        PlayWrongSound();
        // Optionally: flash the slot red
        StartCoroutine(FlashRed());
        return false;
    }

    void PlaceOrganCorrectly(OrganPickup organ)
    {
        isOccupied = true;

        // Snap organ to this slot
        organ.PlaceInSlot(this, placementSurfacePadding);

        // Visual feedback
        if (glowEffect != null)
            glowEffect.SetActive(true);

        if (slotRenderer != null && filledMaterial != null)
            slotRenderer.material = filledMaterial;

        // Sound
        if (correctPlaceSound != null)
            audioSource.PlayOneShot(correctPlaceSound);

        // Notify the TrayManager
        if (trayManager != null)
            trayManager.OnSlotFilled(slotNumber);
    }

    void PlayWrongSound()
    {
        if (wrongPlaceSound != null)
            audioSource.PlayOneShot(wrongPlaceSound);
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (slotRenderer != null)
        {
            Color original = slotRenderer.material.color;
            slotRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            slotRenderer.material.color = original;
        }
    }

    public bool IsOccupied() => isOccupied;
    public string GetExpectedOrgan() => expectedOrganTag;

    public Vector3 GetSurfacePoint()
    {
        Vector3 normal = transform.up;
        Bounds bounds;
        if (TryGetSlotBounds(out bounds))
            return bounds.center + normal.normalized * GetExtentAlong(bounds, normal);

        return transform.position;
    }

    private bool TryGetSlotBounds(out Bounds bounds)
    {
        if (slotRenderer != null)
        {
            bounds = slotRenderer.bounds;
            return true;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        Collider slotCollider = GetComponent<Collider>();
        if (slotCollider == null)
            slotCollider = GetComponentInChildren<Collider>();

        if (slotCollider != null)
        {
            bounds = slotCollider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static float GetExtentAlong(Bounds bounds, Vector3 axis)
    {
        axis.Normalize();
        return Mathf.Abs(Vector3.Dot(axis, Vector3.right)) * bounds.extents.x
             + Mathf.Abs(Vector3.Dot(axis, Vector3.up)) * bounds.extents.y
             + Mathf.Abs(Vector3.Dot(axis, Vector3.forward)) * bounds.extents.z;
    }
}

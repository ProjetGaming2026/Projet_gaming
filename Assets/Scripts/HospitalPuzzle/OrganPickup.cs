using UnityEngine;

/// <summary>
/// Attach this to each Organ GameObject (Heart, Hand, Liver, etc.)
/// Allows the player to pick up and carry the organ.
/// Each organ must have a unique Tag matching its name: "Heart", "Hand", "Liver", "Eye", "Lung"
/// </summary>
public class OrganPickup : MonoBehaviour, IInteractable
{
    [Header("Organ Info")]
    public string organName = "Heart"; // Must match the Tag on this GameObject
    public int orderInDocument = 1;    // The order mentioned in the document (1 = first)

    [Header("Settings")]
    public float interactDistance = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    public float placementProbeRadius = 0.35f;
    public bool keepStaticUntilPickedUp = true;

    [Header("Carry Settings")]
    public Vector3 holdOffset = new Vector3(0.5f, -0.3f, 1.0f); // position relative to camera
    public float carriedCollisionRadius = 0.18f;
    public float carriedWallPadding = 0.05f;
    public LayerMask carriedBlockerMask = Physics.DefaultRaycastLayers;

    [Header("Visual Feedback")]
    public GameObject interactPrompt; // "Press E to pick up" label
    public string trayPromptFormat = "Press E to put the organ in tray {0}";
    public bool preserveInitialRotationOnPlacement = true;
    public float placementHeightOffset = 0.01f;
    public Vector3 placementOffset;

    private bool playerNearby = false;
    private bool isHeld = false;
    private bool isPlaced = false;

    private Transform playerCamera;
    private Rigidbody rb;
    private Collider col;
    private Collider[] organColliders;
    private int lastPickupFrame = -1;
    private SlotController nearbySlot;
    private Quaternion initialRotation;

    // Static reference to track what the player is currently holding
    public static OrganPickup CurrentlyHeld = null;
    public static int LastInteractionFrame = -1;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        organColliders = GetComponentsInChildren<Collider>(true);
        initialRotation = transform.rotation;

        // Set tag automatically if not set
        if (!string.IsNullOrEmpty(organName))
            gameObject.tag = organName;

        // Find player camera
        Camera cam = Camera.main;
        if (cam != null)
            playerCamera = cam.transform;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        ConfigureRestingPhysics();
    }

    void Update()
    {
        if (isPlaced) return;

        // If being held, follow the camera
        if (isHeld)
        {
            if (playerCamera != null)
            {
                transform.position = GetBlockedCarryPosition();
                transform.rotation = playerCamera.rotation;
            }

            nearbySlot = FindNearestAvailableSlot();

            // Drop with E key again
            if (Input.GetKeyDown(interactKey) && Time.frameCount != lastPickupFrame)
            {
                Drop();
            }
            return;
        }

        // Check distance
        if (playerCamera != null)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.position);
            playerNearby = distance <= interactDistance;
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(playerNearby);

        nearbySlot = null;
    }

    public void Interact()
    {
        if (isPlaced)
            return;

        if (isHeld)
        {
            Drop();
            return;
        }

        if (CurrentlyHeld != null)
            CurrentlyHeld.Drop();

        PickUp();
    }

    public void PickUp()
    {
        isHeld = true;
        CurrentlyHeld = this;
        lastPickupFrame = Time.frameCount;
        LastInteractionFrame = Time.frameCount;

        // Disable physics while held
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetCollidersEnabled(false);

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log($"[OrganPickup] Picked up: {organName}");
    }

    public void Drop()
    {
        LastInteractionFrame = Time.frameCount;

        SlotController targetSlot = FindNearestAvailableSlot();
        if (targetSlot != null)
        {
            if (targetSlot.TryPlaceOrgan(this))
            {
                Debug.Log($"[OrganPickup] Placed in tray: {organName}");
                return;
            }

            KeepInHand();
            return;
        }

        ReleaseToWorld();
        Debug.Log($"[OrganPickup] Dropped: {organName}");
    }

    private void ReleaseToWorld()
    {
        isHeld = false;
        CurrentlyHeld = null;

        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = keepStaticUntilPickedUp;
            rb.useGravity = !keepStaticUntilPickedUp;
        }

        SetCollidersEnabled(true);
        SetCollidersAsTriggers(true);
    }

    /// <summary>
    /// Called by the SlotController when organ is correctly placed
    /// </summary>
    public void PlaceInSlot(Transform slotTransform)
    {
        PlaceInSlot(slotTransform, 0.03f);
    }

    public void PlaceInSlot(Transform slotTransform, float surfacePadding)
    {
        Vector3 surfacePoint = slotTransform.position;
        Vector3 surfaceNormal = slotTransform.up;
        PlaceAtSurface(surfacePoint, surfaceNormal, slotTransform, surfacePadding);
    }

    public void PlaceInSlot(SlotController slot, float surfacePadding)
    {
        if (slot == null)
            return;

        PlaceAtSurface(slot.GetSurfacePoint(), slot.transform.up, slot.transform, surfacePadding);
    }

    private void PlaceAtSurface(Vector3 surfacePoint, Vector3 surfaceNormal, Transform slotTransform, float surfacePadding)
    {
        isHeld = false;
        isPlaced = true;
        CurrentlyHeld = null;

        // Snap visually to the top of the slot while keeping the organ's flat scene rotation.
        Quaternion placementRotation = preserveInitialRotationOnPlacement ? initialRotation : transform.rotation;
        transform.rotation = placementRotation;
        transform.position = surfacePoint;
        Physics.SyncTransforms();

        transform.position = surfacePoint
            + surfaceNormal.normalized * (placementHeightOffset + Mathf.Min(surfacePadding, 0.002f))
            + slotTransform.TransformDirection(placementOffset);

        // Freeze in place
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetCollidersEnabled(false);

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log($"[OrganPickup] {organName} placed successfully in slot!");
    }

    public bool IsHeld() => isHeld;
    public bool IsPlaced() => isPlaced;
    public string GetOrganName() => organName;
    public string GetInteractPrompt() => isHeld ? "Appuie sur E pour poser " + organName : "Appuie sur E pour prendre " + organName;

    public float GetExtentAlong(Vector3 axis)
    {
        Bounds bounds;
        if (col != null)
        {
            bounds = col.bounds;
        }
        else
        {
            Renderer organRenderer = GetComponentInChildren<Renderer>();
            bounds = organRenderer != null ? organRenderer.bounds : new Bounds(transform.position, Vector3.one * 0.15f);
        }

        axis.Normalize();
        return Mathf.Abs(Vector3.Dot(axis, Vector3.right)) * bounds.extents.x
             + Mathf.Abs(Vector3.Dot(axis, Vector3.up)) * bounds.extents.y
             + Mathf.Abs(Vector3.Dot(axis, Vector3.forward)) * bounds.extents.z;
    }

    public float GetVisualExtentAlong(Vector3 axis)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
            return GetExtentAlong(axis);

        axis.Normalize();
        return Mathf.Abs(Vector3.Dot(axis, Vector3.right)) * bounds.extents.x
             + Mathf.Abs(Vector3.Dot(axis, Vector3.up)) * bounds.extents.y
             + Mathf.Abs(Vector3.Dot(axis, Vector3.forward)) * bounds.extents.z;
    }

    public float GetVisualBottomOffsetAlong(Vector3 axis)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        axis.Normalize();

        bool hasBounds = false;
        float minProjection = 0f;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Bounds bounds = renderers[i].bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );

                        float projection = Vector3.Dot(corner - transform.position, axis);
                        if (!hasBounds || projection < minProjection)
                        {
                            minProjection = projection;
                            hasBounds = true;
                        }
                    }
                }
            }
        }

        if (!hasBounds)
            return GetExtentAlong(axis);

        return Mathf.Max(0f, -minProjection);
    }

    private void KeepInHand()
    {
        isHeld = true;
        isPlaced = false;
        CurrentlyHeld = this;
        lastPickupFrame = Time.frameCount;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetCollidersEnabled(false);
    }

    private void ConfigureRestingPhysics()
    {
        if (rb != null && keepStaticUntilPickedUp)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        SetCollidersAsTriggers(true);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (organColliders == null || organColliders.Length == 0)
        {
            if (col != null)
                col.enabled = enabled;
            return;
        }

        for (int i = 0; i < organColliders.Length; i++)
        {
            if (organColliders[i] != null)
                organColliders[i].enabled = enabled;
        }
    }

    private void SetCollidersAsTriggers(bool isTrigger)
    {
        if (organColliders == null || organColliders.Length == 0)
        {
            if (col != null)
                col.isTrigger = isTrigger;
            return;
        }

        for (int i = 0; i < organColliders.Length; i++)
        {
            if (organColliders[i] != null)
                organColliders[i].isTrigger = isTrigger;
        }
    }

    private Vector3 GetBlockedCarryPosition()
    {
        Vector3 desiredPosition = playerCamera.position + playerCamera.TransformDirection(holdOffset);
        Vector3 castVector = desiredPosition - playerCamera.position;
        float castDistance = castVector.magnitude;
        if (castDistance <= 0.001f)
            return desiredPosition;

        Vector3 castDirection = castVector / castDistance;
        float radius = Mathf.Max(0.01f, carriedCollisionRadius);
        Ray ray = new Ray(playerCamera.position, castDirection);
        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            radius,
            castDistance,
            carriedBlockerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return desiredPosition;

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            if (playerCamera.root != null && hitCollider.transform.IsChildOf(playerCamera.root))
                continue;

            float blockedDistance = Mathf.Max(0.1f, hits[i].distance - carriedWallPadding);
            return playerCamera.position + castDirection * blockedDistance;
        }

        return desiredPosition;
    }

    private SlotController FindNearestAvailableSlot()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, placementProbeRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return null;

        SlotController bestSlot = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            SlotController slot = hits[i].GetComponent<SlotController>();
            if (slot == null)
                slot = hits[i].GetComponentInParent<SlotController>();

            if (slot == null || slot.IsOccupied())
                continue;

            float distance = Vector3.Distance(transform.position, slot.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }

    private void OnGUI()
    {
        if (!isHeld || nearbySlot == null)
            return;

        GUIStyle promptStyle = new GUIStyle(GUI.skin.label);
        promptStyle.fontSize = 18;
        promptStyle.normal.textColor = Color.white;
        promptStyle.alignment = TextAnchor.MiddleCenter;

        float promptWidth = 420f;
        float promptHeight = 32f;
        string message = string.Format(trayPromptFormat, nearbySlot.slotNumber);
        GUI.Label(
            new Rect(Screen.width / 2f - promptWidth / 2f, Screen.height * 0.68f, promptWidth, promptHeight),
            message,
            promptStyle
        );
    }
}

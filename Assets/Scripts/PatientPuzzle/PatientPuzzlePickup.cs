using UnityEngine;

public class PatientPuzzlePickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private string itemId = "blanket";
    [SerializeField] private string itemLabel = "blanket";

    [Header("Carry")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0.45f, -0.35f, 0.95f);
    [SerializeField] private Vector3 holdEulerOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private bool keepStableUntilPicked = true;

    public static PatientPuzzlePickup CurrentlyHeld { get; private set; }
    public static int LastInteractionFrame { get; private set; } = -1;

    public string ItemId => itemId;
    public string ItemLabel => itemLabel;
    public bool IsHeld => isHeld;
    public bool IsUsed => isUsed;

    private Transform playerCamera;
    private Rigidbody itemRigidbody;
    private Collider[] itemColliders;
    private bool isHeld;
    private bool isUsed;

    private void Awake()
    {
        itemRigidbody = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>(true);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCamera = mainCamera.transform;
        }

        if (keepStableUntilPicked && itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.useGravity = false;
        }
    }

    private void Update()
    {
        if (!isHeld || isUsed)
        {
            return;
        }

        RefreshCamera();
        if (playerCamera == null)
        {
            return;
        }

        transform.position = playerCamera.TransformPoint(holdOffset);
        transform.rotation = playerCamera.rotation * Quaternion.Euler(holdEulerOffset);
    }

    public void Interact()
    {
        if (isUsed)
        {
            return;
        }

        if (isHeld)
        {
            Drop();
            return;
        }

        PickUp();
    }

    public void PickUp()
    {
        if (CurrentlyHeld != null && CurrentlyHeld != this)
        {
            CurrentlyHeld.Drop();
        }

        RefreshCamera();
        isHeld = true;
        CurrentlyHeld = this;
        LastInteractionFrame = Time.frameCount;

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.useGravity = false;
        }

        SetCollidersEnabled(false);
        PatientPuzzleController.ShowMessage("You picked up " + itemLabel + ".", PatientPuzzleController.MessageTone.Info);
    }

    public void Drop()
    {
        if (!isHeld || isUsed)
        {
            return;
        }

        isHeld = false;
        LastInteractionFrame = Time.frameCount;

        if (CurrentlyHeld == this)
        {
            CurrentlyHeld = null;
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = false;
            itemRigidbody.useGravity = true;
        }

        SetCollidersEnabled(true);
        PatientPuzzleController.ShowMessage("You dropped " + itemLabel + ".", PatientPuzzleController.MessageTone.Info);
    }

    public void PlaceOn(Transform placementPoint)
    {
        PlaceOn(placementPoint, Vector3.zero, Vector3.zero, Vector3.one);
    }

    public void PlaceOn(Transform placementPoint, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        isUsed = true;
        isHeld = false;

        if (CurrentlyHeld == this)
        {
            CurrentlyHeld = null;
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = true;
            itemRigidbody.useGravity = false;
        }

        SetCollidersEnabled(false);

        if (placementPoint != null)
        {
            if (localScale == Vector3.zero)
            {
                localScale = Vector3.one;
            }

            transform.SetParent(placementPoint, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(localEulerAngles);
            transform.localScale = localScale;
        }
    }

    public void ConsumeAndHide()
    {
        isUsed = true;
        isHeld = false;

        if (CurrentlyHeld == this)
        {
            CurrentlyHeld = null;
        }

        gameObject.SetActive(false);
    }

    public string GetInteractPrompt()
    {
        if (isHeld)
        {
            return "Press E to drop " + itemLabel;
        }

        return "Press E to pick up " + itemLabel;
    }

    private void RefreshCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCamera = mainCamera.transform;
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (itemColliders == null)
        {
            return;
        }

        for (int i = 0; i < itemColliders.Length; i++)
        {
            if (itemColliders[i] != null)
            {
                itemColliders[i].enabled = enabled;
            }
        }
    }
}

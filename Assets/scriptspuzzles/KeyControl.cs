using UnityEngine;

public class KeyControl : MonoBehaviour, IInteractable
{
    public static bool hasLaboratoryKey = false;
    public float pickupRadius = 0.65f;
    [SerializeField] private bool countAsTask = true;

    private bool taskRegistered;
    private PrototypeGameManager gameManager;

    private void Awake()
    {
        EnsurePickupCollider();
    }

    private void Start()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
    }

    private void OnMouseDown()
    {
        PickUpKey();
    }

    public void Interact()
    {
        PickUpKey();
    }

    public string GetInteractPrompt()
    {
        return "E : prendre la cle";
    }

    private void PickUpKey()
    {
        if (hasLaboratoryKey)
        {
            return;
        }

        hasLaboratoryKey = true;
        RegisterTaskIfNeeded();
        gameObject.SetActive(false);

        Debug.Log("Cle recuperee !");
    }

    private void RegisterTaskIfNeeded()
    {
        if (!countAsTask || taskRegistered)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (gameManager == null)
        {
            return;
        }

        taskRegistered = true;
        gameManager.RegisterTaskSuccess();
    }

    private void EnsurePickupCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
        {
            if (existingCollider is SphereCollider existingSphere)
            {
                existingSphere.radius = Mathf.Max(existingSphere.radius, pickupRadius);
                existingSphere.isTrigger = true;
            }

            return;
        }

        SphereCollider pickupCollider = gameObject.AddComponent<SphereCollider>();
        pickupCollider.radius = pickupRadius;
        pickupCollider.isTrigger = true;
    }
}

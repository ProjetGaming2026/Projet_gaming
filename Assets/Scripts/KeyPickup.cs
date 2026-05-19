using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Task Progression")]
    [SerializeField] private bool countAsTask = true;

    private bool isPickedUp;
    private PrototypeGameManager gameManager;

    private void Start()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        EnsurePickupCollider();
    }

    public void Interact()
    {
        if (isPickedUp)
        {
            return;
        }

        isPickedUp = true;
        EnsureInventorySystem();
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.PickupLowerFloorRoomKey();
        }

        if (countAsTask)
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<PrototypeGameManager>();
            }

            if (gameManager != null)
            {
                gameManager.RegisterTaskSuccess();
            }
        }

        gameObject.SetActive(false);
        Debug.Log("KeyPickup: lower floor room key picked up.");
    }

    public string GetInteractPrompt()
    {
        return "Appuie sur E pour ramasser la cle de chambre";
    }

    public void ShowPrompt()
    {
    }

    public void HidePrompt()
    {
    }

    private void EnsureInventorySystem()
    {
        if (!InventorySystem.ShouldShowInventoryInScene(SceneManager.GetActiveScene()))
        {
            return;
        }

        if (InventorySystem.Instance != null)
        {
            return;
        }

        GameObject inventoryObject = new GameObject("InventorySystem_Runtime");
        inventoryObject.AddComponent<InventorySystem>();
    }

    private void EnsurePickupCollider()
    {
        if (GetComponent<Collider>() != null)
        {
            return;
        }

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false;
    }
}

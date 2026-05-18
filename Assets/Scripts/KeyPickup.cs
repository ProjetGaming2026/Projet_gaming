using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    private bool isPickedUp;

    private void Start()
    {
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
        InventorySystem.Instance.PickupLowerFloorRoomKey();
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

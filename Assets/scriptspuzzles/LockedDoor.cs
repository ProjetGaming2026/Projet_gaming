using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    public Vector3 closedRotation;
    public Vector3 openRotation;
    public bool isOpen = false;

    private void Start()
    {
        if (!isOpen)
        {
            closedRotation = transform.eulerAngles;
        }
    }

    private void OnMouseDown()
    {
        TryOpenDoor();
    }

    public void Interact()
    {
        TryOpenDoor();
    }

    public string GetInteractPrompt()
    {
        return "E : ouvrir la porte";
    }

    public void ForceClose()
    {
        transform.eulerAngles = closedRotation;
        isOpen = false;
    }

    private void TryOpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        transform.eulerAngles = openRotation;
        isOpen = true;

        Debug.Log("Porte ouverte !");
    }
}

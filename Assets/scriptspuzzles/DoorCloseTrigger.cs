using UnityEngine;

public class DoorCloseTrigger : MonoBehaviour
{
    public GameObject door;
    public Transform player;
    public Vector3 closedRotation;
    public float closeDistance = 2.5f;
    public AudioSource doorCloseSound;

    private bool hasClosed = false;

    private void Start()
    {
        FindMissingReferences();
    }

    private void Update()
    {
        FindMissingReferences();

        if (hasClosed || player == null)
        {
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        if (distance <= closeDistance)
        {
            CloseDoor();
        }
    }

    private void FindMissingReferences()
    {
        if (player == null)
        {
            BasicFirstPersonController controller = FindFirstObjectByType<BasicFirstPersonController>();
            if (controller != null)
            {
                player = controller.transform;
            }
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (door == null)
        {
            LockedDoor lockedDoor = FindFirstObjectByType<LockedDoor>();
            if (lockedDoor != null)
            {
                door = lockedDoor.gameObject;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasClosed)
        {
            return;
        }

        if (other.CompareTag("Player") || other.GetComponentInParent<BasicFirstPersonController>() != null)
        {
            CloseDoor();
        }
    }

    private void CloseDoor()
    {
        if (hasClosed)
        {
            return;
        }

        hasClosed = true;

        LockedDoor lockedDoor = door != null ? door.GetComponent<LockedDoor>() : null;
        if (lockedDoor != null)
        {
            if (closedRotation == Vector3.zero)
            {
                closedRotation = lockedDoor.transform.eulerAngles;
            }

            lockedDoor.closedRotation = closedRotation;
            lockedDoor.ForceClose();
        }
        else if (door != null)
        {
            if (closedRotation == Vector3.zero)
            {
                closedRotation = door.transform.eulerAngles;
            }

            door.transform.eulerAngles = closedRotation;
        }

        if (doorCloseSound != null)
        {
            doorCloseSound.Play();
        }

        Debug.Log("La porte s'est fermee.");
    }
}

using UnityEngine;

public class OldFlashlightPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Appuie sur E pour prendre la vieille lampe";

    private bool pickedUp;
    public bool IsPickedUp => pickedUp;

    private void Start()
    {
        EnsurePickupCollider();
    }

    public void Interact()
    {
        if (pickedUp)
        {
            return;
        }

        PlayerHeldFlashlight holder = FindFirstObjectByType<PlayerHeldFlashlight>();
        if (holder == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                holder = playerObject.AddComponent<PlayerHeldFlashlight>();
            }
        }

        if (holder == null)
        {
            Debug.LogWarning("OldFlashlightPickup: no PlayerHeldFlashlight holder available.");
            return;
        }

        pickedUp = true;
        DisableAnimators();
        holder.Equip(gameObject);
        SetCollidersEnabled(false);
        enabled = false;
    }

    public string GetInteractPrompt()
    {
        return prompt;
    }

    public void PrepareForDrawer()
    {
        PrepareForWorldPickup();
    }

    public void PrepareForWorldPickup()
    {
        pickedUp = false;
        gameObject.SetActive(true);
        enabled = true;
        DisableAnimators();
        EnsurePickupCollider();
        SetCollidersEnabled(true);
        SetRenderersEnabled(true);
    }

    private void EnsurePickupCollider()
    {
        if (GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Bounds bounds = ResolveRendererBounds();
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false;

        if (bounds.size != Vector3.zero)
        {
            boxCollider.center = transform.InverseTransformPoint(bounds.center);
            boxCollider.size = new Vector3(
                Mathf.Max(bounds.size.x, 0.18f),
                Mathf.Max(bounds.size.y, 0.08f),
                Mathf.Max(bounds.size.z, 0.18f));
        }
        else
        {
            boxCollider.size = new Vector3(0.26f, 0.12f, 0.26f);
        }
    }

    private Bounds ResolveRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void SetCollidersEnabled(bool enabled)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider pickupCollider in colliders)
        {
            pickupCollider.enabled = enabled;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer pickupRenderer in renderers)
        {
            pickupRenderer.enabled = enabled;
        }
    }

    private void DisableAnimators()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.enabled = false;
            animator.runtimeAnimatorController = null;
        }
    }
}

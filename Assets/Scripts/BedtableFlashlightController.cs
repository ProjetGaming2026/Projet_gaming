using System.Collections;
using UnityEngine;

public class BedtableFlashlightController : MonoBehaviour, IInteractable
{
    [Header("Scene References")]
    [SerializeField] private Transform drawer;
    [SerializeField] private GameObject oldFlashlight;

    [Header("Drawer Motion")]
    [SerializeField] private float openDistance = 0.62f;
    [SerializeField] private float openDuration = 0.65f;
    [SerializeField] private Vector3 openLocalAxis = Vector3.forward;

    [Header("Flashlight Placement")]
    [SerializeField] private Vector3 flashlightLocalPosition = new Vector3(0f, 0.08f, -0.22f);
    [SerializeField] private Vector3 flashlightLocalEuler = new Vector3(0f, -90f, 0f);
    [SerializeField] private Vector3 flashlightLocalScale = Vector3.one * 0.14f;

    private const string MovingDrawerName = "Moving_bedtableDrawer";
    private const string OldRuntimeDrawerName = "Runtime_Open_Drawer";
    private const string OldRuntimeCavityName = "Runtime_Drawer_Dark_Cavity";
    private const string OldAnimatedDrawerName = "Animated_bedtableDrawer";

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private bool isOpen;
    private bool isAnimating;
    private bool flashlightTaken;
    private bool initialized;
    private Coroutine openRoutine;

    public void Configure(Transform newDrawer, GameObject newOldFlashlight)
    {
        drawer = newDrawer;
        oldFlashlight = newOldFlashlight;
        InitializePuzzle();
    }

    private void Start()
    {
        if (!initialized)
        {
            InitializePuzzle();
        }
    }

    public void Interact()
    {
        if (isAnimating)
        {
            return;
        }

        if (!isOpen)
        {
            OpenDrawer();
        }
    }

    public void NotifyFlashlightTaken()
    {
        flashlightTaken = true;
        if (oldFlashlight != null)
        {
            oldFlashlight.SetActive(true);
        }

        enabled = false;
    }

    public string GetInteractPrompt()
    {
        if (!isOpen)
        {
            return "Appuie sur E pour ouvrir le tiroir";
        }

        return IsFlashlightAvailable()
            ? "Appuie sur E pour prendre la vieille lampe"
            : string.Empty;
    }

    private void InitializePuzzle()
    {
        ResolveReferences();
        CleanupOldRuntimeObjects();
        PrepareSelectedDrawerForRuntime();
        CachePositions();
        PrepareFlashlight(false);
        initialized = true;
    }

    private void ResolveReferences()
    {
        if (drawer == null)
        {
            drawer = FindChildByName(transform, "bedtableDrawer");
            if (drawer == null)
            {
                drawer = FindChildContainingName(transform, "drawer");
            }
        }

        if (oldFlashlight == null)
        {
            GameObject foundFlashlight = GameObject.Find("old_flashlight");
            if (foundFlashlight != null)
            {
                oldFlashlight = foundFlashlight;
            }
        }

        if (oldFlashlight != null && oldFlashlight.GetComponent<OldFlashlightPickup>() == null)
        {
            oldFlashlight.AddComponent<OldFlashlightPickup>();
        }
    }

    private void CleanupOldRuntimeObjects()
    {
        DestroyChildIfExists(MovingDrawerName);
        DestroyChildIfExists(OldRuntimeDrawerName);
        DestroyChildIfExists(OldRuntimeCavityName);
        DestroyChildIfExists(OldAnimatedDrawerName);
    }

    private void PrepareSelectedDrawerForRuntime()
    {
        if (drawer == null)
        {
            Debug.LogWarning("BedtableFlashlightController: bedtableDrawer reference missing.");
            return;
        }

        drawer.gameObject.SetActive(true);
        SetRenderersEnabled(drawer.gameObject, true);
        SetStaticRecursively(drawer.gameObject, false);
        SetRendererDynamic(drawer.gameObject);
        DisableAnimators(drawer.gameObject);

        Debug.Log("BedtableFlashlightController: selected bedtableDrawer ready for direct animation.");
    }

    private void CachePositions()
    {
        Transform targetDrawer = GetDrawerMotionTarget();
        if (targetDrawer == null)
        {
            return;
        }

        closedLocalPosition = targetDrawer.localPosition;
        Vector3 axis = openLocalAxis.sqrMagnitude > 0.001f ? openLocalAxis.normalized : Vector3.forward;
        openLocalPosition = closedLocalPosition + axis * openDistance;
    }

    private void PrepareFlashlight(bool visible)
    {
        if (oldFlashlight == null)
        {
            return;
        }

        Transform parent = GetDrawerMotionTarget();
        if (parent == null)
        {
            return;
        }

        DisableAnimators(oldFlashlight);
        oldFlashlight.transform.SetParent(parent, false);
        oldFlashlight.transform.localPosition = flashlightLocalPosition;
        oldFlashlight.transform.localRotation = Quaternion.Euler(flashlightLocalEuler);
        oldFlashlight.transform.localScale = flashlightLocalScale;
        SetRenderersEnabled(oldFlashlight, visible);
        oldFlashlight.SetActive(visible);

        OldFlashlightPickup pickup = oldFlashlight.GetComponent<OldFlashlightPickup>();
        if (pickup != null && visible)
        {
            pickup.PrepareForDrawer();
        }

        if (visible)
        {
            SetCollidersEnabled(oldFlashlight, false);
        }
    }

    private void OpenDrawer()
    {
        isOpen = true;
        PrepareFlashlight(true);
        Debug.Log("BedtableFlashlightController: opening selected bedtableDrawer with flashlight attached.");

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(AnimateDrawerOpen());
    }

    private IEnumerator AnimateDrawerOpen()
    {
        isAnimating = true;
        Transform targetDrawer = GetDrawerMotionTarget();
        if (targetDrawer == null)
        {
            isAnimating = false;
            yield break;
        }

        Vector3 startPosition = targetDrawer.localPosition;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(openDuration, 0.01f)));
            targetDrawer.localPosition = Vector3.Lerp(startPosition, openLocalPosition, t);
            targetDrawer.hasChanged = true;
            yield return null;
        }

        targetDrawer.localPosition = openLocalPosition;
        targetDrawer.hasChanged = true;
        if (oldFlashlight != null && oldFlashlight.activeInHierarchy)
        {
            SetCollidersEnabled(oldFlashlight, true);
        }

        Debug.Log("BedtableFlashlightController: selected bedtableDrawer opened to " + openLocalPosition + ".");
        isAnimating = false;
        openRoutine = null;

        // The flashlight owns pickup interaction after the drawer is open.
        enabled = false;
    }

    private Transform GetDrawerMotionTarget()
    {
        return drawer;
    }

    private bool IsFlashlightAvailable()
    {
        if (flashlightTaken || oldFlashlight == null || !oldFlashlight.activeInHierarchy)
        {
            return false;
        }

        OldFlashlightPickup pickup = oldFlashlight.GetComponent<OldFlashlightPickup>();
        return pickup == null || !pickup.IsPickedUp;
    }

    private void DestroyChildIfExists(string childName)
    {
        Transform child = FindChildByName(transform, childName);
        if (child != null)
        {
            if (oldFlashlight != null && oldFlashlight.transform.IsChildOf(child))
            {
                oldFlashlight.transform.SetParent(transform, true);
            }

            DestroyRuntimeObject(child.gameObject);
        }
    }

    private static void DestroyRuntimeObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private static void DisableAnimators(GameObject target)
    {
        Animator[] animators = target.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            animator.enabled = false;
            animator.runtimeAnimatorController = null;
        }
    }

    private static void SetRendererDynamic(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.allowOcclusionWhenDynamic = false;
            targetRenderer.enabled = true;
        }
    }

    private static void SetRenderersEnabled(GameObject target, bool enabled)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.enabled = enabled;
        }
    }

    private static void SetCollidersEnabled(GameObject target, bool enabled)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider targetCollider in colliders)
        {
            targetCollider.enabled = enabled;
        }
    }

    private static void SetStaticRecursively(GameObject target, bool isStatic)
    {
        if (target == null)
        {
            return;
        }

        target.isStatic = isStatic;
        foreach (Transform child in target.transform)
        {
            SetStaticRecursively(child.gameObject, isStatic);
        }
    }

    private static Transform FindChildByName(Transform current, string childName)
    {
        if (current == null)
        {
            return null;
        }

        if (current.name == childName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildByName(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildContainingName(Transform current, string namePart)
    {
        if (current == null)
        {
            return null;
        }

        if (current.name.ToLowerInvariant().Contains(namePart.ToLowerInvariant()))
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildContainingName(child, namePart);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

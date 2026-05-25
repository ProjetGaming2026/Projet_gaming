using UnityEngine;
using UnityEngine.SceneManagement;

public static class BedtableFlashlightBootstrap
{
    private const string FlashlightName = "old_flashlight";
    private static readonly string[] LegacyRuntimeObjectNames =
    {
        "Moving_bedtableDrawer",
        "Runtime_Open_Drawer",
        "Runtime_Drawer_Dark_Cavity",
        "Animated_bedtableDrawer"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ConfigureScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        DisableLegacyBedtableControllers(scene);
        CleanupLegacyRuntimeObjects(scene);

        GameObject flashlight = FindSceneObjectByName(scene, FlashlightName);
        if (flashlight == null)
        {
            Debug.LogWarning("BedtableFlashlightBootstrap: old_flashlight not found. Put the flashlight object on the table and name it old_flashlight.");
            return;
        }

        ConfigureWorldFlashlightPickup(flashlight);

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null && playerObject.GetComponent<PlayerHeldFlashlight>() == null)
        {
            playerObject.AddComponent<PlayerHeldFlashlight>();
        }

        Debug.Log("BedtableFlashlightBootstrap: old flashlight table pickup connected.");
    }

    private static void ConfigureWorldFlashlightPickup(GameObject flashlight)
    {
        flashlight.SetActive(true);
        SetStaticRecursively(flashlight, false);
        DisableAnimators(flashlight);
        SetRenderersEnabled(flashlight, true);

        OldFlashlightPickup pickup = flashlight.GetComponent<OldFlashlightPickup>();
        if (pickup == null)
        {
            pickup = flashlight.AddComponent<OldFlashlightPickup>();
        }

        pickup.PrepareForWorldPickup();
    }

    private static void DisableLegacyBedtableControllers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            BedtableFlashlightController[] controllers = root.GetComponentsInChildren<BedtableFlashlightController>(true);
            foreach (BedtableFlashlightController controller in controllers)
            {
                controller.enabled = false;
            }
        }
    }

    private static void CleanupLegacyRuntimeObjects(Scene scene)
    {
        foreach (string legacyObjectName in LegacyRuntimeObjectNames)
        {
            GameObject legacyObject = FindSceneObjectByName(scene, legacyObjectName);
            if (legacyObject != null)
            {
                Object.Destroy(legacyObject);
            }
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

    private static void SetRenderersEnabled(GameObject target, bool enabled)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.enabled = enabled;
        }
    }

    private static void SetStaticRecursively(GameObject target, bool isStatic)
    {
        target.isStatic = isStatic;
        foreach (Transform child in target.transform)
        {
            SetStaticRecursively(child.gameObject, isStatic);
        }
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildByName(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildByName(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

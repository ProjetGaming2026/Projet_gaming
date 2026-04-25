using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HospitalDoorRuntimeBootstrap : MonoBehaviour
{
    private static bool isRegistered;
    private static readonly string[] DoorKeywords = { "door", "cabinet", "closet", "locker", "wardrobe", "cupboard", "armoire" };
    private static readonly string[] ExcludedKeywords = { "frame", "drawer", "body" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapDoors()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        ConfigureSceneDoors(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneDoors(scene);
    }

    private static void ConfigureSceneDoors(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        AudioClip openClip = Resources.Load<AudioClip>("audio/opendoor");
        AudioClip closeClip = Resources.Load<AudioClip>("audio/closedoor");

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (!IsDoorLeaf(candidate))
                {
                    continue;
                }

                EnsureDoorSetup(candidate.gameObject, openClip, closeClip);
            }
        }
    }

    private static void EnsureDoorSetup(GameObject doorObject, AudioClip openClip, AudioClip closeClip)
    {
        DoorInteractable doorInteractable = doorObject.GetComponent<DoorInteractable>();
        if (doorInteractable == null)
        {
            doorInteractable = doorObject.AddComponent<DoorInteractable>();
        }

        AudioSource audioSource = doorObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = doorObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 12f;
        }

        if (openClip != null || closeClip != null)
        {
            AssignClipIfPresent(doorInteractable, "openSound", openClip);
            AssignClipIfPresent(doorInteractable, "closeSound", closeClip);
        }

        if (!HasAnyCollider(doorObject))
        {
            BoxCollider collider = doorObject.AddComponent<BoxCollider>();
            if (TryGetLocalBounds(doorObject, out Bounds bounds))
            {
                collider.center = bounds.center;
                collider.size = bounds.size;
            }
        }
    }

    private static void AssignClipIfPresent(DoorInteractable doorInteractable, string fieldName, AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        var field = typeof(DoorInteractable).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(doorInteractable, clip);
        }
    }

    private static bool IsDoorLeaf(Transform candidate)
    {
        if (!IsDoorRelated(candidate))
        {
            return false;
        }

        if (IsFrameRelated(candidate))
        {
            return false;
        }

        if (!TryGetWorldBounds(candidate.gameObject, out Bounds candidateBounds))
        {
            return false;
        }

        if (!LooksLikeDoorPanel(candidate, candidateBounds))
        {
            return false;
        }

        foreach (Transform child in candidate)
        {
            if (IsDoorLeafChildCandidate(child))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikeDoor(string name)
    {
        return HasKeyword(name, DoorKeywords) && !HasKeyword(name, ExcludedKeywords);
    }

    private static bool IsDoorRelated(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (LooksLikeDoor(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsFrameRelated(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (HasKeyword(current.name, ExcludedKeywords))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsDoorLeafChildCandidate(Transform candidate)
    {
        if (!IsDoorRelated(candidate) || IsFrameRelated(candidate))
        {
            return false;
        }

        return TryGetWorldBounds(candidate.gameObject, out Bounds bounds) && LooksLikeDoorPanel(candidate, bounds);
    }

    private static bool HasAnyCollider(GameObject doorObject)
    {
        return doorObject.GetComponentInChildren<Collider>(true) != null;
    }

    private static bool LooksLikeDoorPanel(Transform candidate, Bounds bounds)
    {
        Vector3 size = bounds.size;
        float horizontalMax = Mathf.Max(size.x, size.z);
        float horizontalMin = Mathf.Min(size.x, size.z);
        bool furnitureDoor = IsFurnitureDoor(candidate);

        float minHeight = furnitureDoor ? 0.35f : 1.2f;
        float maxHeight = furnitureDoor ? 2.6f : 3.5f;
        float minWidth = furnitureDoor ? 0.18f : 0.35f;
        float maxWidth = furnitureDoor ? 1.8f : 2.5f;
        float maxThickness = furnitureDoor ? 0.9f : 0.8f;

        if (size.y < minHeight || size.y > maxHeight)
        {
            return false;
        }

        if (horizontalMax < minWidth || horizontalMax > maxWidth)
        {
            return false;
        }

        if (horizontalMin > maxThickness)
        {
            return false;
        }

        return true;
    }

    private static bool IsFurnitureDoor(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if ((lowerName.Contains("cabinet") ||
                 lowerName.Contains("closet") ||
                 lowerName.Contains("locker") ||
                 lowerName.Contains("wardrobe") ||
                 lowerName.Contains("cupboard") ||
                 lowerName.Contains("armoire")) &&
                !lowerName.Contains("body") &&
                !lowerName.Contains("drawer"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasKeyword(string source, string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (source.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static bool TryGetLocalBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bool initialized = false;
        Bounds localBounds = default;
        Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;

        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.bounds;
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;

            Vector3[] corners =
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z),
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                if (!initialized)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        bounds = localBounds;
        return initialized;
    }
}

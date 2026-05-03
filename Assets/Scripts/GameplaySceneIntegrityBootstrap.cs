using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySceneIntegrityBootstrap
{
    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        Apply(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    public static void Apply(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        EnsurePlayerAndCameraTags(scene);
        EnsureGameplayObjects(scene);
    }

    private static void EnsurePlayerAndCameraTags(Scene scene)
    {
        BasicFirstPersonController player = FindInScene<BasicFirstPersonController>(scene);
        if (player != null && !player.CompareTag("Player"))
        {
            player.gameObject.tag = "Player";
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject.scene == scene)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        Camera fallbackCamera = player.GetComponentInChildren<Camera>(true);
        if (fallbackCamera != null)
        {
            fallbackCamera.tag = "MainCamera";
        }
    }

    private static void EnsureGameplayObjects(Scene scene)
    {
        PrototypeGameManager gameManager = FindInScene<PrototypeGameManager>(scene);
        if (gameManager == null)
        {
            return;
        }

        TaskTerminal[] terminals = FindAllInScene<TaskTerminal>(scene);
        for (int i = 0; i < terminals.Length; i++)
        {
            terminals[i].RefreshRuntimeReferences();
            EnsureTerminalCollider(terminals[i]);
        }

        ExitZone[] exits = FindAllInScene<ExitZone>(scene);
        for (int i = 0; i < exits.Length; i++)
        {
            exits[i].RefreshRuntimeReferences();
            EnsureExitSetup(exits[i]);
        }

        if (exits.Length > 0)
        {
            gameManager.AssignExitZones(exits);
        }
    }

    private static void EnsureTerminalCollider(TaskTerminal terminal)
    {
        if (terminal == null)
        {
            return;
        }

        Collider collider = terminal.GetComponentInChildren<Collider>(true);
        if (collider != null)
        {
            collider.isTrigger = false;
            return;
        }

        BoxCollider fallbackCollider = terminal.gameObject.GetComponent<BoxCollider>();
        if (fallbackCollider == null)
        {
            fallbackCollider = terminal.gameObject.AddComponent<BoxCollider>();
        }

        if (TryGetLocalBounds(terminal.gameObject, out Bounds bounds))
        {
            fallbackCollider.center = bounds.center;
            fallbackCollider.size = bounds.size;
        }
        else
        {
            fallbackCollider.center = new Vector3(0f, 0.9f, 0f);
            fallbackCollider.size = new Vector3(1f, 1.8f, 1f);
        }
    }

    private static void EnsureExitSetup(ExitZone exitZone)
    {
        if (exitZone == null)
        {
            return;
        }

        if (exitZone.VisualRoot == null)
        {
            exitZone.AssignVisualRoot(exitZone.gameObject);
        }

        Collider triggerZone = exitZone.TriggerZone;
        if (triggerZone == null)
        {
            triggerZone = exitZone.GetComponent<Collider>();

            if (triggerZone == null)
            {
                triggerZone = exitZone.GetComponentInChildren<Collider>(true);
            }

            if (triggerZone == null)
            {
                BoxCollider newTrigger = exitZone.gameObject.AddComponent<BoxCollider>();
                newTrigger.center = new Vector3(0f, 1.1f, 0f);
                newTrigger.size = new Vector3(2.2f, 2.4f, 2.2f);
                triggerZone = newTrigger;
            }

            exitZone.AssignTriggerZone(triggerZone);
        }

        triggerZone.isTrigger = true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == scene)
            {
                return all[i];
            }
        }

        return null;
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : Component
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        List<T> filtered = new List<T>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == scene)
            {
                filtered.Add(all[i]);
            }
        }

        return filtered.ToArray();
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

        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds rendererBounds = renderers[i].bounds;
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

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corners[cornerIndex]);
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

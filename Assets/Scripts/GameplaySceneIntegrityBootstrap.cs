using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySceneIntegrityBootstrap
{
    private const string EasyScene = "demoScene_Facile";
    private const string MediumScene = "demoScene_Moyen";
    private const string HardScene = "demoScene_Difficile";

    private static readonly Vector3 FallbackPlayerPosition = new Vector3(20f, 1f, -2f);
    private static readonly Vector3 FallbackMonsterPosition = new Vector3(-7.55f, 1f, 20f);

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
        EnsureMinimumGameplayRoots(scene);
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

    private static void EnsureMinimumGameplayRoots(Scene scene)
    {
        if (!IsHospitalGameplayScene(scene))
        {
            return;
        }

        BasicFirstPersonController player = FindInScene<BasicFirstPersonController>(scene);
        Camera sceneCamera = FindInScene<Camera>(scene);

        if (player == null)
        {
            player = CreateFallbackPlayer(scene);
            sceneCamera = player.GetComponentInChildren<Camera>(true);
        }
        else if (!player.CompareTag("Player"))
        {
            player.gameObject.tag = "Player";
        }

        player.gameObject.SetActive(true);

        if (sceneCamera == null)
        {
            sceneCamera = CreateFallbackCamera(scene, player.transform);
        }

        if (sceneCamera != null)
        {
            sceneCamera.gameObject.SetActive(true);
            sceneCamera.gameObject.tag = "MainCamera";
            sceneCamera.enabled = true;
        }

        if (FindInScene<PrototypeGameManager>(scene) == null)
        {
            CreateFallbackGameManager(scene);
        }

        if (FindInScene<BasicMonsterController>(scene) == null)
        {
            CreateFallbackMonster(scene);
        }

        if (FindInScene<Light>(scene) == null)
        {
            CreateFallbackLight(scene);
        }
    }

    private static bool IsHospitalGameplayScene(Scene scene)
    {
        return scene.name == EasyScene || scene.name == MediumScene || scene.name == HardScene;
    }

    private static BasicFirstPersonController CreateFallbackPlayer(Scene scene)
    {
        GameObject playerObject = new GameObject("Player_RuntimeFallback");
        SceneManager.MoveGameObjectToScene(playerObject, scene);
        playerObject.tag = "Player";
        playerObject.transform.position = FallbackPlayerPosition;

        CharacterController characterController = playerObject.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.45f;
        characterController.center = new Vector3(0f, 1f, 0f);

        CreateFallbackCamera(scene, playerObject.transform);

        playerObject.AddComponent<PlayerInteractor>();
        playerObject.AddComponent<PlayerInteraction>();
        return playerObject.AddComponent<BasicFirstPersonController>();
    }

    private static Camera CreateFallbackCamera(Scene scene, Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        cameraObject.tag = "MainCamera";

        if (parent != null)
        {
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            cameraObject.transform.position = FallbackPlayerPosition + new Vector3(0f, 1.6f, 0f);
            cameraObject.transform.rotation = Quaternion.identity;
        }

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 500f;

        if (Object.FindFirstObjectByType<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        return camera;
    }

    private static void CreateFallbackGameManager(Scene scene)
    {
        GameObject gameManagerObject = new GameObject("GameManager_RuntimeFallback");
        SceneManager.MoveGameObjectToScene(gameManagerObject, scene);
        gameManagerObject.AddComponent<PrototypeGameManager>();
    }

    private static void CreateFallbackMonster(Scene scene)
    {
        GameObject monsterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        SceneManager.MoveGameObjectToScene(monsterObject, scene);
        monsterObject.name = "Entity_RuntimeFallback";
        monsterObject.transform.position = FallbackMonsterPosition;
        monsterObject.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        CharacterController characterController = monsterObject.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.45f;
        characterController.center = new Vector3(0f, 1f, 0f);

        monsterObject.AddComponent<BasicMonsterController>();
        monsterObject.AddComponent<EntityCapture>();
    }

    private static void CreateFallbackLight(Scene scene)
    {
        GameObject lightObject = new GameObject("Directional Light_RuntimeFallback");
        SceneManager.MoveGameObjectToScene(lightObject, scene);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
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
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

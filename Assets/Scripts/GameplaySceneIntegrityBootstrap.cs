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

        if (IsHospitalGameplayScene(scene))
        {
            EnsurePlayerAndCameraTags(scene);
            EnsureMinimumGameplayRoots(scene);
        }

        EnsureGameplayObjects(scene);
        GameplayDifficultyBalanceBootstrap.ApplyForScene(scene);
    }

    private static void EnsurePlayerAndCameraTags(Scene scene)
    {
        BasicFirstPersonController player = FindAuthoritativePlayer(scene);
        ApplyPlayerCameraAuthority(scene, player);
    }

    private static void EnsureMinimumGameplayRoots(Scene scene)
    {
        if (!IsHospitalGameplayScene(scene))
        {
            return;
        }

        BasicFirstPersonController player = FindAuthoritativePlayer(scene);
        Camera sceneCamera = player != null ? player.GetComponentInChildren<Camera>(true) : FindInScene<Camera>(scene);

        if (player == null)
        {
            player = CreateFallbackPlayer(scene);
            sceneCamera = player.GetComponentInChildren<Camera>(true);
        }
        else if (!player.CompareTag("Player"))
        {
            player.gameObject.tag = "Player";
        }

        ApplyPlayerCameraAuthority(scene, player);
        sceneCamera = player.GetComponentInChildren<Camera>(true);

        if (sceneCamera == null)
        {
            sceneCamera = CreateFallbackCamera(scene, player.transform);
        }

        ApplyPlayerCameraAuthority(scene, player);

        if (FindInScene<PrototypeGameManager>(scene) == null)
        {
            CreateFallbackGameManager(scene);
        }

        PrototypeGameManager gameManager = FindInScene<PrototypeGameManager>(scene);
        if (gameManager != null)
        {
            gameManager.AssignPlayerController(player);
        }

        if (FindInScene<BasicMonsterController>(scene) == null)
        {
            CreateFallbackMonster(scene);
        }

        RetargetGameplayToPlayer(scene, player);

        if (FindInScene<Light>(scene) == null)
        {
            CreateFallbackLight(scene);
        }

        if (FindInScene<GameplayPauseOptionsMenu>(scene) == null)
        {
            CreatePauseOptionsMenu(scene);
        }
    }

    private static bool IsHospitalGameplayScene(Scene scene)
    {
        return scene.name == EasyScene || scene.name == MediumScene || scene.name == HardScene;
    }

    private static BasicFirstPersonController FindAuthoritativePlayer(Scene scene)
    {
        BasicFirstPersonController[] players = FindAllInScene<BasicFirstPersonController>(scene);
        if (players.Length == 0)
        {
            return null;
        }

        BasicFirstPersonController best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < players.Length; i++)
        {
            BasicFirstPersonController candidate = players[i];
            if (candidate == null)
            {
                continue;
            }

            int score = 0;
            if (candidate.gameObject.activeSelf)
            {
                score += 20;
            }

            if (candidate.gameObject.activeInHierarchy)
            {
                score += 20;
            }

            if (candidate.CompareTag("Player"))
            {
                score += 10;
            }

            Camera candidateCamera = candidate.GetComponentInChildren<Camera>(true);
            if (candidateCamera != null)
            {
                score += 35;
            }

            if (scene.name == EasyScene && candidate.name.Trim() == "Player (1)")
            {
                score += 100;
            }

            if (candidate.name.Trim() == "Player_RuntimeFallback")
            {
                score -= 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static void ApplyPlayerCameraAuthority(Scene scene, BasicFirstPersonController player)
    {
        if (player == null)
        {
            return;
        }

        player.gameObject.SetActive(true);
        player.gameObject.tag = "Player";
        DisablePlayerBodyRenderers(player);

        BasicFirstPersonController[] players = FindAllInScene<BasicFirstPersonController>(scene);
        for (int i = 0; i < players.Length; i++)
        {
            BasicFirstPersonController candidate = players[i];
            if (candidate == null || candidate == player)
            {
                continue;
            }

            candidate.SetExternalControlLock(true);
            candidate.gameObject.tag = "Untagged";
            candidate.gameObject.SetActive(false);
        }

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        Camera[] cameras = FindAllInScene<Camera>(scene);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            bool isPlayerCamera = camera == playerCamera;
            camera.gameObject.tag = isPlayerCamera ? "MainCamera" : "Untagged";
            camera.enabled = isPlayerCamera;

            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = isPlayerCamera;
            }

            if (isPlayerCamera)
            {
                camera.gameObject.SetActive(true);
            }
        }
    }

    private static void DisablePlayerBodyRenderers(BasicFirstPersonController player)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    private static void RetargetGameplayToPlayer(Scene scene, BasicFirstPersonController player)
    {
        if (player == null)
        {
            return;
        }

        BasicMonsterController[] monsters = FindAllInScene<BasicMonsterController>(scene);
        for (int i = 0; i < monsters.Length; i++)
        {
            monsters[i].AssignPlayerTarget(player.transform);
        }

        EntityCapture[] captures = FindAllInScene<EntityCapture>(scene);
        for (int i = 0; i < captures.Length; i++)
        {
            captures[i].AssignPlayerTarget(player.transform);
        }
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

    private static void CreatePauseOptionsMenu(Scene scene)
    {
        GameObject pauseObject = new GameObject("GameplayPauseOptionsMenu_Runtime");
        SceneManager.MoveGameObjectToScene(pauseObject, scene);
        pauseObject.AddComponent<GameplayPauseOptionsMenu>();
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

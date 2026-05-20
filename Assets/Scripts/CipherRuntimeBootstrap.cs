using UnityEngine;
using UnityEngine.SceneManagement;

public class CipherRuntimeBootstrap : MonoBehaviour
{
    private static bool isRegistered;
    private const string PersistentTerminalName = "ordinateur";
    private const string CipherAnchorTerminalName = "Terminal_01";
    private const string EasySceneName = "demoScene_Facile";

    private static readonly string[] TerminalNames =
    {
        "Terminal_01",
        "Terminal_02",
        "Terminal_03"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapCipher()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        ConfigureScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (scene.name != EasySceneName)
        {
            RemoveEasyOnlyCipherObjects(scene);
            PreservePrototypeTerminal(scene);
            return;
        }

        CipherTerminal terminal = PrepareCipherTerminal(scene);
        if (terminal == null)
        {
            Debug.LogWarning("CipherRuntimeBootstrap: aucun terminal trouve dans " + scene.name);
            return;
        }

        CipherPuzzle puzzle = Object.FindFirstObjectByType<CipherPuzzle>();
        if (puzzle == null)
        {
            GameObject puzzleObject = new GameObject("CipherRuntimeManager");
            SceneManager.MoveGameObjectToScene(puzzleObject, scene);
            puzzle = puzzleObject.AddComponent<CipherPuzzle>();
        }

        terminal.SetCipherPuzzle(puzzle);
    }

    private static void PreservePrototypeTerminal(Scene scene)
    {
        GameObject prototypeTerminal = FindSceneObjectByName(scene, PersistentTerminalName);
        if (prototypeTerminal == null)
        {
            return;
        }

        Object.DontDestroyOnLoad(prototypeTerminal);
    }

    private static CipherTerminal PrepareCipherTerminal(Scene scene)
    {
        CipherTerminal persistentTerminal = FindPersistentCipherTerminal();
        GameObject anchorObject = FindSceneObjectByName(scene, CipherAnchorTerminalName);

        if (persistentTerminal != null && anchorObject != null)
        {
            PositionTerminalAtAnchor(persistentTerminal.gameObject, anchorObject.transform);
            DisableAnchorTerminal(anchorObject);
            return persistentTerminal;
        }

        if (persistentTerminal != null)
        {
            return persistentTerminal;
        }

        return FindOrCreateCipherTerminal(scene);
    }

    private static CipherTerminal FindPersistentCipherTerminal()
    {
        CipherTerminal[] terminals = Object.FindObjectsByType<CipherTerminal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CipherTerminal terminal in terminals)
        {
            if (terminal != null && terminal.name == PersistentTerminalName)
            {
                return terminal;
            }
        }

        return null;
    }

    private static void PositionTerminalAtAnchor(GameObject terminalObject, Transform anchorTransform)
    {
        terminalObject.transform.position = anchorTransform.position + Vector3.down * 0.08f;
        terminalObject.transform.rotation = anchorTransform.rotation * Quaternion.Euler(-90f, 0f, 0f);
        terminalObject.transform.localScale = Vector3.one;
    }

    private static void DisableAnchorTerminal(GameObject anchorObject)
    {
        foreach (Renderer renderer in anchorObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (Collider collider in anchorObject.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        TaskTerminal taskTerminal = anchorObject.GetComponent<TaskTerminal>();
        if (taskTerminal != null)
        {
            taskTerminal.enabled = false;
        }
    }

    private static CipherTerminal FindOrCreateCipherTerminal(Scene scene)
    {
        foreach (string terminalName in TerminalNames)
        {
            GameObject terminalObject = FindSceneObjectByName(scene, terminalName);
            if (terminalObject == null)
            {
                continue;
            }

            CipherTerminal terminal = terminalObject.GetComponent<CipherTerminal>();
            if (terminal == null)
            {
                terminal = terminalObject.AddComponent<CipherTerminal>();
            }

            return terminal;
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static void RemoveEasyOnlyCipherObjects(Scene scene)
    {
        DestroySceneObjectByName(scene, "AutoCipherWall");
        DestroySceneObjectByName(scene, "CipherRuntimeManager");
        DestroyAllObjectsNamed("AutoCipherWall");
        DestroyAllObjectsNamed("CipherWallText");
        DestroyAllObjectsNamed("CipherRuntimeManager");
        DestroyAllCipherWalls();
    }

    private static void DestroySceneObjectByName(Scene scene, string objectName)
    {
        GameObject objectToDestroy = FindSceneObjectByName(scene, objectName);
        if (objectToDestroy != null)
        {
            Object.Destroy(objectToDestroy);
        }
    }

    private static void DestroyAllObjectsNamed(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == objectName)
            {
                Object.Destroy(candidate.gameObject);
            }
        }
    }

    private static void DestroyAllCipherWalls()
    {
        CipherWall[] cipherWalls = Object.FindObjectsByType<CipherWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cipherWalls.Length; i++)
        {
            CipherWall wall = cipherWalls[i];
            if (wall != null)
            {
                Object.Destroy(wall.gameObject);
            }
        }
    }
}

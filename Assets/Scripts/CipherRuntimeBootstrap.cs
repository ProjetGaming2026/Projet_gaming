using UnityEngine;
using UnityEngine.SceneManagement;

public class CipherRuntimeBootstrap : MonoBehaviour
{
    private static bool isRegistered;
    private const string PersistentTerminalName = "ordinateur";
    private const string CipherAnchorTerminalName = "Terminal_01";
    private static readonly string[] SceneNames =
    {
        "demoScene_Facile",
        "demoScene_Moyen",
        "demoScene_Difficile"
    };

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

        if (!IsHospitalScene(scene.name))
        {
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

    private static bool IsHospitalScene(string sceneName)
    {
        for (int i = 0; i < SceneNames.Length; i++)
        {
            if (sceneName == SceneNames[i])
            {
                return true;
            }
        }

        return false;
    }
}

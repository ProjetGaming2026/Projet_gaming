using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KeypadPuzzleSetupTool
{
    private const string TargetScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string KeypadPrefabPath = "Assets/Hospital/_Prefabs/props/keypad/SM_Pin_Pad.fbx";

    [MenuItem("Tools/Gameplay/Setup Keypad Puzzle In DemoScene Moyen")]
    public static void SetupKeypadPuzzleInDemoSceneMoyen()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[KeypadPuzzleSetupTool] Unable to open demoScene_Moyen.");
            return;
        }

        GameObject keypadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KeypadPrefabPath);
        if (keypadPrefab == null)
        {
            Debug.LogWarning("[KeypadPuzzleSetupTool] Unable to load keypad prefab.");
            return;
        }

        GameObject existing = FindRoot(scene, "SM_Pin_Pad");
        if (existing == null)
        {
            existing = PrefabUtility.InstantiatePrefab(keypadPrefab, scene) as GameObject;
        }

        if (existing == null)
        {
            Debug.LogWarning("[KeypadPuzzleSetupTool] Unable to instantiate keypad prefab.");
            return;
        }

        existing.name = "SM_Pin_Pad";
        existing.transform.position = new Vector3(-0.61f, -5.441f, -4.992f);
        existing.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        existing.transform.localScale = new Vector3(2.012108f, 1f, 2.0482774f);

        Collider keypadCollider = existing.GetComponent<Collider>();
        if (keypadCollider == null)
        {
            keypadCollider = existing.AddComponent<BoxCollider>();
        }

        keypadCollider.isTrigger = false;

        if (existing.GetComponent<AudioSource>() == null)
        {
            AudioSource audioSource = existing.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        if (existing.GetComponent<CodePuzzleManager>() == null)
        {
            existing.AddComponent<CodePuzzleManager>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[KeypadPuzzleSetupTool] Keypad puzzle placed in demoScene_Moyen.");
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
            {
                return root;
            }
        }

        return null;
    }
}

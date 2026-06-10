using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DoctorDeskSceneRepair
{
    private const string SceneName = "demoScene_Difficile";
    private const string PuzzleRoomName = "Puzzle3_room";
    private const string DeskName = "doctor_desk";
    private const string DeskAssetPath = "Assets/Phone_Puzzles/office_desk_-_18mb.glb";

    private static readonly Vector3 DeskLocalPosition = new Vector3(2.25169f, 0f, 0.28600025f);
    private static readonly Quaternion DeskLocalRotation = new Quaternion(-0.5f, -0.5f, -0.5f, 0.5f);

    static DoctorDeskSceneRepair()
    {
        EditorApplication.delayCall += RepairActiveSceneIfNeeded;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
    }

    [MenuItem("Tools/Phone Puzzle/Repair Doctor Desk")]
    public static void RepairActiveSceneIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != SceneName)
        {
            return;
        }

        Transform puzzleRoom = FindSceneTransform(scene, PuzzleRoomName);
        if (puzzleRoom == null)
        {
            Debug.LogWarning("Doctor desk repair could not find Puzzle3_room.");
            return;
        }

        GameObject currentDesk = FindDirectChild(puzzleRoom, DeskName);
        bool currentDeskIsMissingPrefab = currentDesk != null &&
            (PrefabUtility.GetPrefabAssetType(currentDesk) == PrefabAssetType.MissingAsset ||
             PrefabUtility.GetPrefabInstanceStatus(currentDesk) == PrefabInstanceStatus.MissingAsset);

        if (currentDesk != null && !currentDeskIsMissingPrefab)
        {
            EnsureDeskComponents(currentDesk);
            return;
        }

        AssetDatabase.ImportAsset(
            DeskAssetPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

        GameObject deskAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DeskAssetPath);
        if (deskAsset == null)
        {
            Debug.LogWarning("Doctor desk repair could not load " + DeskAssetPath + ".");
            return;
        }

        if (currentDesk != null)
        {
            Object.DestroyImmediate(currentDesk);
        }

        GameObject repairedDesk = PrefabUtility.InstantiatePrefab(deskAsset, scene) as GameObject;
        if (repairedDesk == null)
        {
            Debug.LogWarning("Doctor desk repair could not instantiate the office desk asset.");
            return;
        }

        repairedDesk.name = DeskName;
        repairedDesk.transform.SetParent(puzzleRoom, false);
        repairedDesk.transform.localPosition = DeskLocalPosition;
        repairedDesk.transform.localRotation = DeskLocalRotation;
        repairedDesk.transform.localScale = Vector3.one;

        EnsureDeskComponents(repairedDesk);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = repairedDesk;
        Debug.Log("Repaired and permanently saved doctor_desk in demoScene_Difficile.");
    }

    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += RepairActiveSceneIfNeeded;
    }

    private static void EnsureDeskComponents(GameObject desk)
    {
        bool changed = false;

        if (desk.GetComponent<DoctorDeskNameplate>() == null)
        {
            desk.AddComponent<DoctorDeskNameplate>();
            changed = true;
        }

        if (desk.GetComponent<DoctorDeskFileGenerator>() == null)
        {
            desk.AddComponent<DoctorDeskFileGenerator>();
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(desk);
            EditorSceneManager.MarkSceneDirty(desk.scene);
        }
    }

    private static GameObject FindDirectChild(Transform parent, string objectName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == objectName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Transform FindSceneTransform(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform result = FindRecursive(roots[i].transform, objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindRecursive(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

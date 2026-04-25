using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MonsterPatrolSetupTool
{
    [MenuItem("Tools/Gameplay/Setup Monster Patrol Route")]
    public static void SetupMonsterPatrolRoute()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Monster Patrol", "Aucune scene chargee.", "OK");
            return;
        }

        GameObject existingRoot = GameObject.Find("MonsterPatrolRoute");
        if (existingRoot != null)
        {
            Undo.DestroyObjectImmediate(existingRoot);
        }

        GameObject routeRoot = new GameObject("MonsterPatrolRoute");
        Undo.RegisterCreatedObjectUndo(routeRoot, "Create Monster Patrol Route");
        SceneManager.MoveGameObjectToScene(routeRoot, activeScene);

        CreatePoint(routeRoot.transform, "Patrol_01_Room", new Vector3(-7.55f, 1f, 20f));
        CreatePoint(routeRoot.transform, "Patrol_02_WestHall", new Vector3(-10f, 1f, 8f));
        CreatePoint(routeRoot.transform, "Patrol_03_Morgue", new Vector3(-5.1f, 1f, 0f));
        CreatePoint(routeRoot.transform, "Patrol_04_Corridor", new Vector3(18.2f, 1f, -5f));
        CreatePoint(routeRoot.transform, "Patrol_05_BathRoom", new Vector3(18.15f, 1f, -15f));
        CreatePoint(routeRoot.transform, "Patrol_06_Center", new Vector3(20f, 1f, -2f));
        CreatePoint(routeRoot.transform, "Patrol_07_Stairs", new Vector3(23.7f, 1f, 0.9f));
        CreatePoint(routeRoot.transform, "Patrol_08_Room2", new Vector3(18.2f, 1f, 5.1f));

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorUtility.DisplayDialog(
            "Monster Patrol",
            "La route de patrouille du monstre a ete creee dans la scene active.",
            "OK");
    }

    private static void CreatePoint(Transform parent, string pointName, Vector3 position)
    {
        GameObject point = new GameObject(pointName);
        Undo.RegisterCreatedObjectUndo(point, "Create Patrol Point");
        point.transform.SetParent(parent);
        point.transform.position = position;
    }
}

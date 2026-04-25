using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoSceneGameplayBuilder
{
    private const string DemoScenePath = "Assets/Hospital/scenes/demoScene.unity";
    private const string PrototypeScenePath = "Assets/PrototypeScene.unity";

    [MenuItem("Tools/Gameplay/Build Demo Scene From Prototype")]
    public static void BuildDemoSceneFromPrototype()
    {
        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Scene prototypeScene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);

        try
        {
            GameObject oldFpsController = FindRoot(demoScene, "FPSController");
            Transform spawnTransform = oldFpsController != null ? oldFpsController.transform : null;
            GameObject oldCamera = FindAnyInScene(demoScene, "FirstPersonCharacter");
            Component oldPostProcessLayer = FindComponentByTypeName(oldCamera, "PostProcessLayer");

            RemoveRootIfExists(demoScene, "Player");
            RemoveRootIfExists(demoScene, "Entity");
            RemoveRootIfExists(demoScene, "GameManager");
            RemoveRootIfExists(demoScene, "SoundManager");
            RemoveRootIfExists(demoScene, "HUDManager");
            RemoveRootIfExists(demoScene, "StaticTrapPrefab");
            RemoveRootIfExists(demoScene, "Terminal_01");
            RemoveRootIfExists(demoScene, "Terminal_02");
            RemoveRootIfExists(demoScene, "Terminal_03");
            RemoveRootIfExists(demoScene, "Exit_A");
            RemoveRootIfExists(demoScene, "Exit_B");

            if (oldFpsController != null)
            {
                Object.DestroyImmediate(oldFpsController);
            }

            var clonedRoots = new Dictionary<string, GameObject>();
            foreach (string rootName in GameplayRootNames())
            {
                GameObject sourceRoot = FindRoot(prototypeScene, rootName);
                if (sourceRoot == null)
                {
                    throw new System.InvalidOperationException("Missing root in prototype scene: " + rootName);
                }

                GameObject clone = Object.Instantiate(sourceRoot);
                clone.name = sourceRoot.name;
                SceneManager.MoveGameObjectToScene(clone, demoScene);
                clonedRoots[rootName] = clone;
            }

            if (spawnTransform != null)
            {
                clonedRoots["Player"].transform.SetPositionAndRotation(
                    spawnTransform.position,
                    spawnTransform.rotation
                );
            }

            PositionAtAnchor(clonedRoots["Entity"], FindRoot(demoScene, "section1"), 1f);
            PositionAtAnchor(clonedRoots["Terminal_01"], FindRoot(demoScene, "room1"), 1f);
            PositionAtAnchor(clonedRoots["Terminal_02"], FindRoot(demoScene, "room2"), 1f);
            PositionAtAnchor(clonedRoots["Terminal_03"], FindRoot(demoScene, "bathRoom"), 1f);
            PositionAtAnchor(clonedRoots["Exit_A"], FindRoot(demoScene, "morgue"), 1f);
            PositionAtAnchor(clonedRoots["Exit_B"], FindRoot(demoScene, "stairs"), 1f);
            clonedRoots["StaticTrapPrefab"].transform.position = new Vector3(0f, -100f, 0f);

            RewireGameplayReferences(clonedRoots);
            CopyPostProcessLayerIfPresent(oldPostProcessLayer, clonedRoots["Player"]);

            EditorSceneManager.MarkSceneDirty(demoScene);
            EditorSceneManager.SaveScene(demoScene);
            EditorUtility.DisplayDialog(
                "Demo Scene Ready",
                "The prototype gameplay has been rebuilt into demoScene.",
                "OK"
            );
        }
        finally
        {
            EditorSceneManager.CloseScene(prototypeScene, true);
        }
    }

    private static IEnumerable<string> GameplayRootNames()
    {
        yield return "Player";
        yield return "Entity";
        yield return "GameManager";
        yield return "SoundManager";
        yield return "HUDManager";
        yield return "StaticTrapPrefab";
        yield return "Terminal_01";
        yield return "Terminal_02";
        yield return "Terminal_03";
        yield return "Exit_A";
        yield return "Exit_B";
    }

    private static void RewireGameplayReferences(Dictionary<string, GameObject> roots)
    {
        GameObject player = roots["Player"];
        GameObject entity = roots["Entity"];
        GameObject trapPrefab = roots["StaticTrapPrefab"];
        PrototypeGameManager gameManager = roots["GameManager"].GetComponent<PrototypeGameManager>();
        ExitZone exitA = roots["Exit_A"].GetComponent<ExitZone>();
        ExitZone exitB = roots["Exit_B"].GetComponent<ExitZone>();

        Transform cameraTransform = player.transform.Find("Main Camera");
        var playerController = player.GetComponent<BasicFirstPersonController>();
        var playerInteractor = player.GetComponent<PlayerInteractor>();
        var monsterController = entity.GetComponent<BasicMonsterController>();
        var entityCapture = entity.GetComponent<EntityCapture>();
        var incubationVisual = entity.GetComponent<EntityIncubationVisual>();
        var hud = roots["HUDManager"].GetComponent<PrototypeHUD>();

        if (playerInteractor != null && cameraTransform != null)
        {
            SetSerializedField(playerInteractor, "interactionOrigin", cameraTransform);
        }

        if (playerController != null)
        {
            if (cameraTransform != null)
            {
                SetSerializedField(playerController, "cameraHolder", cameraTransform);
            }

            SetSerializedField(playerController, "staticTrapPrefab", trapPrefab);
        }

        if (monsterController != null)
        {
            SetSerializedField(monsterController, "playerTarget", player.transform);
            SetSerializedField(monsterController, "gameManager", gameManager);
            SetSerializedField(monsterController, "playerController", playerController);
        }

        if (entityCapture != null)
        {
            SetSerializedField(entityCapture, "playerTarget", player.transform);
        }

        if (incubationVisual != null)
        {
            SetSerializedField(incubationVisual, "gameManager", gameManager);

            Transform prisonVisual = entity.transform.Find("EntityPrisonVisual");
            if (prisonVisual != null)
            {
                SetSerializedField(incubationVisual, "prisonVisual", prisonVisual.gameObject);
            }
        }

        if (hud != null)
        {
            SetSerializedField(hud, "gameManager", gameManager);
        }

        if (gameManager != null)
        {
            SetSerializedField(gameManager, "exitZones", new[] { exitA, exitB });
        }
    }

    private static void CopyPostProcessLayerIfPresent(Component sourceLayer, GameObject playerRoot)
    {
        if (sourceLayer == null)
        {
            return;
        }

        Transform cameraTransform = playerRoot.transform.Find("Main Camera");
        if (cameraTransform == null)
        {
            return;
        }

        Component existing = FindComponentByTypeName(cameraTransform.gameObject, sourceLayer.GetType().Name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        ComponentUtility.CopyComponent(sourceLayer);
        ComponentUtility.PasteComponentAsNew(cameraTransform.gameObject);

        Component pastedLayer = FindComponentByTypeName(cameraTransform.gameObject, sourceLayer.GetType().Name);
        if (pastedLayer != null)
        {
            SerializedObject serialized = new SerializedObject(pastedLayer);
            SerializedProperty volumeTrigger = serialized.FindProperty("volumeTrigger");
            if (volumeTrigger != null)
            {
                volumeTrigger.objectReferenceValue = cameraTransform;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void PositionAtAnchor(GameObject target, GameObject anchor, float height)
    {
        if (target == null || anchor == null)
        {
            return;
        }

        Vector3 position = anchor.transform.position;
        target.transform.position = new Vector3(position.x, height, position.z);
    }

    private static Component FindComponentByTypeName(GameObject gameObject, string typeName)
    {
        if (gameObject == null)
        {
            return null;
        }

        return gameObject
            .GetComponents<Component>()
            .FirstOrDefault(component => component != null && component.GetType().Name == typeName);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene
            .GetRootGameObjects()
            .FirstOrDefault(root => root.name == name);
    }

    private static GameObject FindAnyInScene(Scene scene, string name)
    {
        return scene
            .GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(transform => transform.gameObject)
            .FirstOrDefault(gameObject => gameObject.name == name);
    }

    private static void RemoveRootIfExists(Scene scene, string rootName)
    {
        GameObject root = FindRoot(scene, rootName);
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedField(Object target, string fieldName, Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property != null && property.isArray)
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

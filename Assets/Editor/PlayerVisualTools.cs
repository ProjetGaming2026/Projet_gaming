using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerVisualTools
{
    private static readonly string[] SupportedModelPaths =
    {
        "Assets/Characters/ip.Fbx",
        "Assets/Characters/ip.fbx",
        "Assets/Characters/ip.obj",
    };

    private const string VisualRootName = "PlayerVisualModel";
    private const float TargetWorldHeight = 1.68f;
    private const float BackwardOffset = -0.45f;

    [MenuItem("Tools/Gameplay/Replace Player Visual With Ip Man")]
    public static void ReplacePlayerVisualWithIpMan()
    {
        var player = FindRootByName("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Ip Man",
                "Impossible de trouver l'objet 'Player' dans les scenes ouvertes.",
                "OK");
            return;
        }

        var chosenAssetPath = ResolveAvailableModelPath();
        var modelPrefab = chosenAssetPath == null ? null : LoadModelPrefab(chosenAssetPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Ip Man",
                "Unity n'a trouve aucun modele utilisable pour 'Ip Man'.\n\n" +
                "Verifie que 'Assets/Characters/ip.Fbx' apparait bien comme un modele, puis relance l'outil.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Player Visual");

        var previousVisual = player.transform.Find(VisualRootName);
        if (previousVisual != null)
        {
            Undo.DestroyObjectImmediate(previousVisual.gameObject);
        }

        GameObject visualInstance;
        var prefabSourceType = PrefabUtility.GetPrefabAssetType(modelPrefab);
        if (prefabSourceType != PrefabAssetType.NotAPrefab)
        {
            visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.scene);
        }
        else
        {
            visualInstance = Object.Instantiate(modelPrefab);
            SceneManager.MoveGameObjectToScene(visualInstance, player.scene);
        }

        Undo.RegisterCreatedObjectUndo(visualInstance, "Create Player Visual");
        visualInstance.name = VisualRootName;
        visualInstance.transform.SetParent(player.transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        ApplyLayer(visualInstance, player.layer);
        DisableVisualColliders(visualInstance);
        DisablePlaceholderRenderer(player);
        AdjustVisualDimensions(player, visualInstance);

        EditorSceneManager.MarkSceneDirty(player.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorUtility.DisplayDialog(
            "Ip Man",
            "Le visuel du 'Player' a ete remplace et sa taille a ete ajustee automatiquement.",
            "OK");
    }

    [MenuItem("Tools/Gameplay/Adjust Player Visual Dimensions")]
    public static void AdjustPlayerVisualDimensions()
    {
        var player = FindRootByName("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Ip Man",
                "Impossible de trouver l'objet 'Player' dans les scenes ouvertes.",
                "OK");
            return;
        }

        var visual = player.transform.Find(VisualRootName);
        if (visual == null)
        {
            EditorUtility.DisplayDialog(
                "Ip Man",
                "Aucun visuel de joueur n'est actuellement attache. Lance d'abord 'Replace Player Visual With Ip Man'.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Adjust Player Visual Dimensions");
        AdjustVisualDimensions(player, visual.gameObject);
        EditorSceneManager.MarkSceneDirty(player.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorUtility.DisplayDialog(
            "Ip Man",
            "Les dimensions de 'PlayerVisualModel' ont ete reajustees.",
            "OK");
    }

    private static void AdjustVisualDimensions(GameObject player, GameObject visualInstance)
    {
        Undo.RecordObject(visualInstance.transform, "Adjust Player Visual");
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        if (TryGetCombinedRendererBounds(visualInstance, out var bounds) && bounds.size.y > 0.0001f)
        {
            var scale = TargetWorldHeight / bounds.size.y;
            visualInstance.transform.localScale = Vector3.one * scale;

            if (TryGetCombinedRendererBounds(visualInstance, out bounds))
            {
                var desiredGroundY = player.transform.position.y - 1f;
                var offsetY = desiredGroundY - bounds.min.y;
                visualInstance.transform.position += new Vector3(0f, offsetY, 0f);
            }
        }

        visualInstance.transform.localPosition += new Vector3(0f, 0f, BackwardOffset);
    }

    private static GameObject FindRootByName(string rootName)
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }
        }

        return null;
    }

    private static string ResolveAvailableModelPath()
    {
        foreach (var assetPath in SupportedModelPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
            {
                return assetPath;
            }

            if (AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<GameObject>().Any())
            {
                return assetPath;
            }
        }

        return null;
    }

    private static GameObject LoadModelPrefab(string assetPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            return prefab;
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<GameObject>().FirstOrDefault();
    }

    private static void DisablePlaceholderRenderer(GameObject root)
    {
        var renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Disable Player Placeholder");
            renderer.enabled = false;
        }
    }

    private static void DisableVisualColliders(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable Player Visual Colliders");
            collider.enabled = false;
        }

        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            Undo.RecordObject(animator, "Disable Imported Animator");
            animator.enabled = false;
        }
    }

    private static void ApplyLayer(GameObject root, int layer)
    {
        foreach (var item in root.GetComponentsInChildren<Transform>(true))
        {
            Undo.RecordObject(item.gameObject, "Apply Player Layer");
            item.gameObject.layer = layer;
        }
    }

    private static bool TryGetCombinedRendererBounds(GameObject root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();

        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }
}

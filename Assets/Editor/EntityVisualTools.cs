using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EntityVisualTools
{
    private const string DesktopGlbPath = @"C:\Users\pc\Desktop\CI 1\S2\Projet gaming\ghostly_woman.glb";
    private const string ProjectGlbPath = "Assets/Characters/ghostly_woman.glb";
    private const string ZombieModelPath = "Assets/Characters/FemaleZombie1/zombie-1-Walking.fbx";
    private static readonly string[] SupportedModelPaths =
    {
        "Assets/Characters/Womann/Womann.obj",
        "Assets/Characters/Womann/Womann.fbx",
        "Assets/Characters/ghostly_woman.fbx",
        "Assets/Characters/ghostly_woman.obj",
        ProjectGlbPath,
        ZombieModelPath,
    };
    private const string VisualRootName = "EntityVisualModel";
    private const float TargetWorldHeight = 1.8f;

    [MenuItem("Tools/Gameplay/Import Ghostly Woman GLB")]
    public static void ImportGhostlyWomanGlb()
    {
        if (!File.Exists(DesktopGlbPath))
        {
            EditorUtility.DisplayDialog(
                "Ghostly Woman",
                "Le fichier source est introuvable sur le bureau.\n\n" + DesktopGlbPath,
                "OK");
            return;
        }

        var projectDirectory = Path.GetDirectoryName(ProjectGlbPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            EditorUtility.DisplayDialog(
                "Ghostly Woman",
                "Impossible de determiner le dossier de destination dans le projet.",
                "OK");
            return;
        }

        Directory.CreateDirectory(projectDirectory);
        File.Copy(DesktopGlbPath, ProjectGlbPath, overwrite: true);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Ghostly Woman",
            "Le fichier GLB a ete copie dans Assets/Characters.\n\nTu peux maintenant lancer 'Tools > Gameplay > Replace Entity Visual With Ghostly Woman'.",
            "OK");
    }

    [MenuItem("Tools/Gameplay/Replace Entity Visual With Ghostly Woman")]
    public static void ReplaceEntityVisualWithGhostlyWoman()
    {
        ReplaceEntityVisual("Ghostly Woman", null);
    }

    [MenuItem("Tools/Gameplay/Replace Entity Visual With Mixamo Zombie")]
    public static void ReplaceEntityVisualWithMixamoZombie()
    {
        ReplaceEntityVisual("Mixamo Zombie", ZombieModelPath);
    }

    private static void ReplaceEntityVisual(string displayName, string preferredAssetPath)
    {
        var entity = FindEntityInLoadedScenes();
        if (entity == null)
        {
            EditorUtility.DisplayDialog(
                displayName,
                "Impossible de trouver l'objet 'Entity' dans les scenes ouvertes.",
                "OK");
            return;
        }

        var chosenAssetPath = ResolveAvailableModelPath(preferredAssetPath);
        var modelPrefab = chosenAssetPath == null ? null : LoadModelPrefab(chosenAssetPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog(
                displayName,
                "Unity n'a trouve aucun modele utilisable pour ce monstre.\n\n" +
                "Place un fichier importe comme modele dans l'un de ces chemins:\n" +
                "- Assets/Characters/ghostly_woman.fbx\n" +
                "- Assets/Characters/ghostly_woman.obj\n" +
                "- Assets/Characters/ghostly_woman.glb (si un importeur glTF est installe)\n" +
                "- Assets/Characters/FemaleZombie1/zombie-1-Walking.fbx\n\n" +
                "Puis relance l'outil.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Entity Visual");

        var previousVisual = entity.transform.Find(VisualRootName);
        if (previousVisual != null)
        {
            Undo.DestroyObjectImmediate(previousVisual.gameObject);
        }

        GameObject visualInstance;
        var prefabSourceType = PrefabUtility.GetPrefabAssetType(modelPrefab);
        if (prefabSourceType != PrefabAssetType.NotAPrefab)
        {
            visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, entity.scene);
        }
        else
        {
            visualInstance = Object.Instantiate(modelPrefab);
            SceneManager.MoveGameObjectToScene(visualInstance, entity.scene);
        }

        Undo.RegisterCreatedObjectUndo(visualInstance, "Create Entity Visual");
        visualInstance.name = VisualRootName;
        visualInstance.transform.SetParent(entity.transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        ApplyEntityLayer(visualInstance, entity.layer);
        ConfigureVisualComponents(visualInstance);
        DisableEntityPlaceholderRenderer(entity);
        AssignAnimatedVisualReference(entity, visualInstance.transform);

        if (TryGetCombinedRendererBounds(visualInstance, out var bounds) && bounds.size.y > 0.0001f)
        {
            var scale = TargetWorldHeight / bounds.size.y;
            visualInstance.transform.localScale = Vector3.one * scale;

            if (TryGetCombinedRendererBounds(visualInstance, out bounds))
            {
                var desiredGroundY = CalculateEntityGroundY(entity);
                var offsetY = desiredGroundY - bounds.min.y;
                visualInstance.transform.position += new Vector3(0f, offsetY, 0f);
            }
        }

        EditorSceneManager.MarkSceneDirty(entity.scene);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorUtility.DisplayDialog(
            displayName,
            "Le visuel de 'Entity' a ete remplace et sa taille a ete ajustee automatiquement.",
            "OK");
    }

    private static GameObject FindEntityInLoadedScenes()
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
                if (root.name == "Entity")
                {
                    return root;
                }
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

    private static string ResolveAvailableModelPath(string preferredAssetPath)
    {
        if (!string.IsNullOrWhiteSpace(preferredAssetPath) &&
            (AssetDatabase.LoadAssetAtPath<GameObject>(preferredAssetPath) != null ||
             AssetDatabase.LoadAllAssetsAtPath(preferredAssetPath).OfType<GameObject>().Any()))
        {
            return preferredAssetPath;
        }

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

    private static void DisableEntityPlaceholderRenderer(GameObject entity)
    {
        var renderer = entity.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Disable Entity Placeholder");
            renderer.enabled = false;
        }
    }

    private static void ConfigureVisualComponents(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable Visual Colliders");
            collider.enabled = false;
        }

        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            Undo.RecordObject(animator, "Configure Monster Animator");
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    private static void ApplyEntityLayer(GameObject root, int layer)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            Undo.RecordObject(transform.gameObject, "Apply Entity Layer");
            transform.gameObject.layer = layer;
        }
    }

    private static void AssignAnimatedVisualReference(GameObject entity, Transform visualTransform)
    {
        var monsterController = entity.GetComponent<BasicMonsterController>();
        if (monsterController == null)
        {
            return;
        }

        Undo.RecordObject(monsterController, "Assign Monster Animated Visual");
        var serialized = new SerializedObject(monsterController);
        var property = serialized.FindProperty("animatedVisual");
        if (property != null)
        {
            property.objectReferenceValue = visualTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static float CalculateEntityGroundY(GameObject entity)
    {
        var controller = entity.GetComponent<CharacterController>();
        if (controller == null)
        {
            return entity.transform.position.y;
        }

        var scaleY = Mathf.Abs(entity.transform.lossyScale.y);
        if (scaleY <= 0.0001f)
        {
            scaleY = 1f;
        }

        return entity.transform.position.y +
            controller.center.y * scaleY -
            controller.height * 0.5f * scaleY;
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

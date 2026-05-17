using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EntityVisualTools
{
    private const string DesktopGlbPath = @"C:\Users\pc\Desktop\CI 1\S2\Projet gaming\ghostly_woman.glb";
    private const string ProjectGlbPath = "Assets/Characters/ghostly_woman.glb";
    private const string FuseFemaleZombieModelPath = "Assets/Characters/FuseFemaleZombie/zombie-1-Walking.fbx";
    private const string MixamoAvatarModelPath = "Assets/Characters/Mixamo/Idle_with_skin.fbx";
    private const string MixamoWalkPath = "Assets/Characters/Mixamo/Zombie Walk.fbx";
    private const string MixamoRunPath = "Assets/Characters/Mixamo/Zombie Running.fbx";
    private const string MixamoTurnPath = "Assets/Characters/Mixamo/Zombie Turn.fbx";
    private const string MixamoAttackPath = "Assets/Characters/Mixamo/Zombie Attack.fbx";
    private const string MonsterAnimatorControllerPath = "Assets/Characters/Mixamo/MonsterMixamo.controller";
    private static readonly string[] DemoScenePaths =
    {
        "Assets/Hospital/scenes/demoScene_Facile.unity",
        "Assets/Hospital/scenes/demoScene_Moyen.unity",
        "Assets/Hospital/scenes/demoScene_Difficile.unity",
    };
    private static readonly string[] SupportedModelPaths =
    {
        MixamoAvatarModelPath,
        FuseFemaleZombieModelPath,
        "Assets/Characters/Womann/Womann.obj",
        "Assets/Characters/Womann/Womann.fbx",
        "Assets/Characters/ghostly_woman.fbx",
        "Assets/Characters/ghostly_woman.obj",
        ProjectGlbPath,
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
        ReplaceEntityVisual("Ghostly Woman", ResolveAvailableModelPath());
    }

    [MenuItem("Tools/Gameplay/Replace Entity Visual With Female Zombie")]
    public static void ReplaceEntityVisualWithFemaleZombie()
    {
        ReplaceEntityVisual("Female Zombie", MixamoAvatarModelPath);
    }

    [MenuItem("Tools/Gameplay/Apply Female Zombie To Demo Scenes")]
    public static void ApplyFemaleZombieToDemoScenes()
    {
        var originalScene = SceneManager.GetActiveScene();
        bool appliedAtLeastOnce = false;

        foreach (var scenePath in DemoScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var entity = FindEntityInLoadedScenes();
            if (entity == null)
            {
                continue;
            }

            if (ReplaceEntityVisualOnRoot(entity, "Female Zombie", MixamoAvatarModelPath))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                appliedAtLeastOnce = true;
            }
        }

        if (originalScene.IsValid() && !string.IsNullOrEmpty(originalScene.path))
        {
            EditorSceneManager.OpenScene(originalScene.path, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Female Zombie",
            appliedAtLeastOnce
                ? "La zombie a ete appliquee aux scenes Facile, Moyen et Difficile."
                : "Aucune scene n'a pu etre mise a jour. Verifie l'import du modele puis relance l'outil.",
            "OK");
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

        if (!ReplaceEntityVisualOnRoot(entity, displayName, preferredAssetPath))
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(entity.scene);
        EditorUtility.DisplayDialog(
            displayName,
            "Le visuel de 'Entity' a ete remplace et sa taille a ete ajustee automatiquement.",
            "OK");
    }

    private static bool ReplaceEntityVisualOnRoot(GameObject entity, string displayName, string preferredAssetPath)
    {
        var chosenAssetPath = ResolveModelPath(preferredAssetPath);
        var modelPrefab = chosenAssetPath == null ? null : LoadModelPrefab(chosenAssetPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog(
                displayName,
                "Unity n'a trouve aucun modele utilisable pour ce monstre.\n\n" +
                "- " + MixamoAvatarModelPath + "\n" +
                "- " + FuseFemaleZombieModelPath + "\n" +
                "- Assets/Characters/ghostly_woman.fbx\n" +
                "- Assets/Characters/ghostly_woman.obj\n" +
                "- Assets/Characters/ghostly_woman.glb (si un importeur glTF est installe)\n\n" +
                "Puis relance l'outil.",
                "OK");
            return false;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Entity Visual");

        RemoveExistingEntityVisuals(entity);

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
        DisableVisualColliders(visualInstance);
        DisableEntityPlaceholderRenderer(entity);
        ConfigureWalkAnimation(entity, visualInstance, chosenAssetPath);

        if (TryGetCombinedRendererBounds(visualInstance, out var bounds) && bounds.size.y > 0.0001f)
        {
            var scale = TargetWorldHeight / bounds.size.y;
            visualInstance.transform.localScale = Vector3.one * scale;

            if (TryGetCombinedRendererBounds(visualInstance, out bounds))
            {
                var desiredGroundY = entity.transform.position.y - 1f;
                var offsetY = desiredGroundY - bounds.min.y;
                visualInstance.transform.position += new Vector3(0f, offsetY, 0f);
            }
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        return true;
    }

    private static void RemoveExistingEntityVisuals(GameObject entity)
    {
        var directChildrenToRemove = entity.transform
            .Cast<Transform>()
            .Where(child =>
                child != null &&
                child.name != "EntityPrisonVisual" &&
                (
                    child.name == VisualRootName ||
                    child.GetComponentInChildren<Animator>(true) != null ||
                    child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                ))
            .ToArray();

        foreach (var child in directChildrenToRemove)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
        }
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

    private static string ResolveModelPath(string preferredAssetPath)
    {
        if (!string.IsNullOrWhiteSpace(preferredAssetPath))
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(preferredAssetPath) != null)
            {
                return preferredAssetPath;
            }

            if (AssetDatabase.LoadAllAssetsAtPath(preferredAssetPath).OfType<GameObject>().Any())
            {
                return preferredAssetPath;
            }
        }

        return ResolveAvailableModelPath();
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

    private static void DisableVisualColliders(GameObject root)
    {
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(collider, "Disable Visual Colliders");
            collider.enabled = false;
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

    private static void ConfigureWalkAnimation(GameObject entity, GameObject visualRoot, string assetPath)
    {
        var idleClip = LoadIdleClip(assetPath);
        var walkClip = LoadClipFromAssetPath(MixamoWalkPath);
        var runClip = LoadClipFromAssetPath(MixamoRunPath);
        var turnClip = LoadClipFromAssetPath(MixamoTurnPath);
        var attackClip = LoadClipFromAssetPath(MixamoAttackPath);
        var animator = visualRoot.GetComponentInChildren<Animator>(true);

        var driver = entity.GetComponent<MonsterWalkAnimationDriver>();
        if (driver == null)
        {
            driver = Undo.AddComponent<MonsterWalkAnimationDriver>(entity);
        }

        if (animator != null)
        {
            Undo.RecordObject(animator, "Configure Monster Animator");
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = EnsureMonsterAnimatorController(idleClip, walkClip, runClip, attackClip);
        }

        driver.ConfigureInEditor(
            entity.GetComponent<BasicMonsterController>(),
            entity.GetComponent<CharacterController>(),
            animator,
            walkClip,
            idleClip,
            walkClip,
            runClip,
            turnClip,
            attackClip);
        EditorUtility.SetDirty(driver);
    }

    private static AnimationClip LoadIdleClip(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var clips = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(clip =>
                clip != null &&
                !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var idleClip = clips.FirstOrDefault(clip =>
                clip != null &&
                clip.name.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (idleClip != null)
        {
            return idleClip;
        }

        var defaultNamedClip = clips.FirstOrDefault(clip =>
                clip != null &&
                clip.name.Equals("mixamo.com", System.StringComparison.OrdinalIgnoreCase));

        return defaultNamedClip ?? clips.FirstOrDefault();
    }

    private static AnimationClip LoadClipFromAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        var clips = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(clip =>
                clip != null &&
                !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return clips.FirstOrDefault();
    }

    private static RuntimeAnimatorController EnsureMonsterAnimatorController(
        AnimationClip idleClip,
        AnimationClip walkClip,
        AnimationClip runClip,
        AnimationClip attackClip)
    {
        if (idleClip == null || walkClip == null || runClip == null)
        {
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(MonsterAnimatorControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(MonsterAnimatorControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(MonsterAnimatorControllerPath);
        controller.AddParameter("LocomotionSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.anyStateTransitions
            .ToList()
            .ForEach(transition => stateMachine.RemoveAnyStateTransition(transition));

        BlendTree locomotionTree = new BlendTree
        {
            name = "LocomotionTree",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "LocomotionSpeed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(locomotionTree, controller);
        locomotionTree.AddChild(idleClip, 0f);
        locomotionTree.AddChild(walkClip, 1f);
        locomotionTree.AddChild(runClip, 2f);

        AnimatorState locomotionState = stateMachine.AddState("Locomotion");
        locomotionState.motion = locomotionTree;
        stateMachine.defaultState = locomotionState;

        if (attackClip != null)
        {
            AnimatorState attackState = stateMachine.AddState("Attack");
            attackState.motion = attackClip;

            AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.hasFixedDuration = true;
            anyToAttack.duration = 0.05f;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            AnimatorStateTransition attackToLocomotion = attackState.AddTransition(locomotionState);
            attackToLocomotion.hasExitTime = true;
            attackToLocomotion.exitTime = 0.92f;
            attackToLocomotion.hasFixedDuration = true;
            attackToLocomotion.duration = 0.08f;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(MonsterAnimatorControllerPath, ImportAssetOptions.ForceUpdate);
        return controller;
    }
}

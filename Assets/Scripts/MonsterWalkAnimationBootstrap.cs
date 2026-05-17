using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public static class MonsterWalkAnimationBootstrap
{
    private const string VisualRootName = "EntityVisualModel";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyToLoadedScenes();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToScene(scene);
    }

    private static void ApplyToLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            ApplyToScene(SceneManager.GetSceneAt(i));
        }
    }

    private static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null || root.name != "Entity")
            {
                continue;
            }

            CleanupDuplicateVisuals(root);

            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                continue;
            }

            MonsterWalkAnimationDriver driver = root.GetComponent<MonsterWalkAnimationDriver>();
            if (driver == null)
            {
                driver = root.AddComponent<MonsterWalkAnimationDriver>();
            }

            driver.RefreshConfiguration();
        }
    }

    private static void CleanupDuplicateVisuals(GameObject entity)
    {
        var visualChildren = entity.transform
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

        if (visualChildren.Length <= 1)
        {
            return;
        }

        Transform visualToKeep = visualChildren
            .FirstOrDefault(child => child.name == VisualRootName) ?? visualChildren[0];

        foreach (Transform child in visualChildren)
        {
            if (child == visualToKeep)
            {
                continue;
            }

            Object.Destroy(child.gameObject);
        }
    }
}

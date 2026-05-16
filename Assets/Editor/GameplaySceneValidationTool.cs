using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplaySceneValidationTool
{
    [MenuItem("Tools/Gameplay/Validate Loaded Gameplay Scenes")]
    public static void ValidateLoadedGameplayScenes()
    {
        int updatedCount = 0;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameplaySceneIntegrityBootstrap.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            updatedCount++;
        }

        EditorUtility.DisplayDialog(
            "Gameplay Validation",
            updatedCount == 0
                ? "Aucune scene chargee a valider."
                : $"Validation terminee sur {updatedCount} scene(s) chargee(s).",
            "OK");
    }
}

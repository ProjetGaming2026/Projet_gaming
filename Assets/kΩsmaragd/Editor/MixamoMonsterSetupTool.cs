using System.IO;
using UnityEditor;
using UnityEngine;

public static class MixamoMonsterSetupTool
{
    private const string MixamoFolder = "Assets/Resources/Mixamo";
    private const string ZombieModelPath = "Assets/Characters/FemaleZombie1/zombie-1-Walking.fbx";

    [MenuItem("Tools/Gameplay/Setup Mixamo Monster Animations")]
    public static void SetupMixamoMonsterAnimations()
    {
        int configuredAssets = 0;

        if (ConfigureModelImporter(ZombieModelPath, false))
        {
            configuredAssets++;
        }

        if (Directory.Exists(MixamoFolder))
        {
            foreach (string fullPath in Directory.GetFiles(MixamoFolder, "*.fbx", SearchOption.TopDirectoryOnly))
            {
                string assetPath = fullPath.Replace('\\', '/');
                if (ConfigureModelImporter(assetPath, true))
                {
                    configuredAssets++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EntityVisualTools.ReplaceEntityVisualWithMixamoZombie();

        EditorUtility.DisplayDialog(
            "Mixamo Monster",
            "Configuration Mixamo terminee.\n\n" +
            configuredAssets + " FBX prepares en Humanoid.\n" +
            "Le script BasicMonsterController charge maintenant Idle, Walk, Run, Turn et Attack depuis Assets/Resources/Mixamo.",
            "OK");
    }

    private static bool ConfigureModelImporter(string assetPath, bool configureClipLooping)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            return false;
        }

        bool changed = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        if (configureClipLooping)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            bool shouldLoop = ShouldLoop(assetPath);

            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                clip.loopTime = shouldLoop;
                clip.loopPose = shouldLoop;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = false;
                clip.keepOriginalPositionY = false;
                clip.keepOriginalPositionXZ = false;
                clips[i] = clip;
            }

            importer.clipAnimations = clips;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }

        return true;
    }

    private static bool ShouldLoop(string assetPath)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        return name.Contains("idle") ||
            name.Contains("walk") ||
            name.Contains("run") ||
            name.Contains("turn");
    }
}

using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MixamoRigAutoConfigurator
{
    private const string AvatarModelPath = "Assets/Characters/Mixamo/Idle_with_skin.fbx";
    private static readonly string[] MixamoAnimationPaths =
    {
        "Assets/Characters/Mixamo/Zombie Walk.fbx",
        "Assets/Characters/Mixamo/Zombie Running.fbx",
        "Assets/Characters/Mixamo/Zombie Turn.fbx",
        "Assets/Characters/Mixamo/Zombie Attack.fbx",
    };

    static MixamoRigAutoConfigurator()
    {
        EditorApplication.delayCall += ConfigureOnceAfterReload;
    }

    [MenuItem("Tools/Gameplay/Configure Mixamo Humanoid Rigs")]
    public static void ConfigureMenu()
    {
        ConfigureAll(showDialog: true);
    }

    private static void ConfigureOnceAfterReload()
    {
        EditorApplication.delayCall -= ConfigureOnceAfterReload;
        ConfigureAll(showDialog: false);
    }

    private static void ConfigureAll(bool showDialog)
    {
        bool changedAnything = false;

        if (!TryConfigureMainAvatarModel(out Avatar zombieAvatar))
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Mixamo Rig",
                    "Impossible de configurer le modele principal du monstre.\n\nVerifie que le fichier existe :\n" + AvatarModelPath,
                    "OK");
            }

            return;
        }

        foreach (string animationPath in MixamoAnimationPaths)
        {
            if (ConfigureMixamoAnimation(animationPath, zombieAvatar))
            {
                changedAnything = true;
            }
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Mixamo Rig",
                changedAnything
                    ? "Les fichiers Mixamo ont ete configures en Humanoid avec l'avatar du zombie."
                    : "Les fichiers Mixamo etaient deja correctement configures.",
                "OK");
        }
    }

    private static bool TryConfigureMainAvatarModel(out Avatar zombieAvatar)
    {
        zombieAvatar = null;

        if (!(AssetImporter.GetAtPath(AvatarModelPath) is ModelImporter importer))
        {
            return false;
        }

        bool needsReimport =
            importer.animationType != ModelImporterAnimationType.Human ||
            importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

        if (needsReimport)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
        }

        zombieAvatar = LoadAvatarAtPath(AvatarModelPath);
        return zombieAvatar != null;
    }

    private static bool ConfigureMixamoAnimation(string animationPath, Avatar sourceAvatar)
    {
        if (sourceAvatar == null || !(AssetImporter.GetAtPath(animationPath) is ModelImporter importer))
        {
            return false;
        }

        bool needsReimport =
            importer.animationType != ModelImporterAnimationType.Human ||
            importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther ||
            importer.sourceAvatar != sourceAvatar;

        if (!needsReimport)
        {
            return false;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;
        importer.SaveAndReimport();
        return true;
    }

    private static Avatar LoadAvatarAtPath(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Avatar>()
            .FirstOrDefault(avatar => avatar != null && avatar.isValid);
    }
}

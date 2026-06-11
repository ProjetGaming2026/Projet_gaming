using UnityEngine;

public static class CampaignProgression
{
    public const string EasySceneName = "demoScene_Facile";
    public const string MediumSceneName = "demoScene_Moyen";
    public const string HardSceneName = "demoScene_Difficile";

    public const string EasyScenePath = "Assets/Hospital/scenes/demoScene_Facile.unity";
    public const string MediumScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    public const string HardScenePath = "Assets/Hospital/scenes/demoScene_Difficile.unity";

    private const string HighestUnlockedLevelKey = "Campaign.HighestUnlockedLevel";

    public static int HighestUnlockedLevel
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0), 0, 2);
        private set
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, Mathf.Clamp(value, 0, 2));
            PlayerPrefs.Save();
        }
    }

    public static bool IsLevelUnlocked(string sceneName)
    {
        if (sceneName == EasySceneName)
        {
            return true;
        }

        if (sceneName == MediumSceneName)
        {
            return HighestUnlockedLevel >= 1;
        }

        if (sceneName == HardSceneName)
        {
            return HighestUnlockedLevel >= 2;
        }

        return true;
    }

    public static void MarkLevelCompleted(string sceneName)
    {
        if (sceneName == EasySceneName)
        {
            HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel, 1);
            return;
        }

        if (sceneName == MediumSceneName)
        {
            HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel, 2);
        }
    }

    public static bool TryGetNextLevel(
        string currentSceneName,
        out string nextScenePath,
        out string nextSceneName,
        out DifficultyWarningTransition.VoiceClip voiceClip)
    {
        if (currentSceneName == EasySceneName)
        {
            nextScenePath = MediumScenePath;
            nextSceneName = MediumSceneName;
            voiceClip = DifficultyWarningTransition.VoiceClip.Medium;
            return true;
        }

        if (currentSceneName == MediumSceneName)
        {
            nextScenePath = HardScenePath;
            nextSceneName = HardSceneName;
            voiceClip = DifficultyWarningTransition.VoiceClip.Hard;
            return true;
        }

        nextScenePath = string.Empty;
        nextSceneName = string.Empty;
        voiceClip = DifficultyWarningTransition.VoiceClip.Easy;
        return false;
    }

    public static string GetLockedLabel(string sceneName)
    {
        if (sceneName == MediumSceneName)
        {
            return "Termine le niveau Facile";
        }

        if (sceneName == HardSceneName)
        {
            return "Termine le niveau Moyen";
        }

        return "Verrouille";
    }
}

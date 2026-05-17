using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayDifficultyBalanceBootstrap
{
    private const string EasyScene = "demoScene_Facile";
    private const string MediumScene = "demoScene_Moyen";
    private const string HardScene = "demoScene_Difficile";
    private const string DefaultScene = "demoScene";

    private static bool isRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!isRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            isRegistered = true;
        }

        ApplyForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene);
    }

    private static void ApplyForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (!TryGetProfile(scene.name, out BalanceProfile profile))
        {
            return;
        }

        BasicFirstPersonController player = Object.FindFirstObjectByType<BasicFirstPersonController>();
        if (player != null)
        {
            player.ApplyMovementTuning(profile.playerWalkSpeed, profile.playerSprintSpeed, profile.playerOverclockSpeed);
        }

        BasicMonsterController monster = Object.FindFirstObjectByType<BasicMonsterController>();
        if (monster != null)
        {
            monster.ApplyMovementTuning(
                profile.monsterChaseSpeed,
                profile.monsterSlowedSpeed,
                profile.monsterPatrolSpeed,
                profile.monsterInvestigationSpeed,
                profile.hardPatrolSpeedMultiplier
            );
        }

        PrototypeGameManager gameManager = Object.FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager != null)
        {
            gameManager.ApplyLevelTimeLimit(profile.levelDuration);
            gameManager.ApplyRequiredTasks(profile.requiredTasks);
        }
    }

    private static bool TryGetProfile(string sceneName, out BalanceProfile profile)
    {
        switch (sceneName)
        {
            case EasyScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 5.4f,
                    playerSprintSpeed: 8.8f,
                    playerOverclockSpeed: 13.2f,
                    monsterChaseSpeed: 4.6f,
                    monsterSlowedSpeed: 2.4f,
                    monsterPatrolSpeed: 1.45f,
                    monsterInvestigationSpeed: 2.1f,
                    hardPatrolSpeedMultiplier: 1f,
                    levelDuration: 780f,
                    requiredTasks: 5
                );
                return true;

            case MediumScene:
            case DefaultScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 5f,
                    playerSprintSpeed: 8f,
                    playerOverclockSpeed: 12f,
                    monsterChaseSpeed: 5.4f,
                    monsterSlowedSpeed: 2.8f,
                    monsterPatrolSpeed: 1.65f,
                    monsterInvestigationSpeed: 2.45f,
                    hardPatrolSpeedMultiplier: 1.02f,
                    levelDuration: 600f,
                    requiredTasks: 7
                );
                return true;

            case HardScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 4.8f,
                    playerSprintSpeed: 7.5f,
                    playerOverclockSpeed: 11.5f,
                    monsterChaseSpeed: 6.9f,
                    monsterSlowedSpeed: 3.3f,
                    monsterPatrolSpeed: 1.9f,
                    monsterInvestigationSpeed: 3f,
                    hardPatrolSpeedMultiplier: 1.1f,
                    levelDuration: 480f,
                    requiredTasks: 9
                );
                return true;

            default:
                profile = default;
                return false;
        }
    }

    private readonly struct BalanceProfile
    {
        public readonly float playerWalkSpeed;
        public readonly float playerSprintSpeed;
        public readonly float playerOverclockSpeed;
        public readonly float monsterChaseSpeed;
        public readonly float monsterSlowedSpeed;
        public readonly float monsterPatrolSpeed;
        public readonly float monsterInvestigationSpeed;
        public readonly float hardPatrolSpeedMultiplier;
        public readonly float levelDuration;
        public readonly int requiredTasks;

        public BalanceProfile(
            float playerWalkSpeed,
            float playerSprintSpeed,
            float playerOverclockSpeed,
            float monsterChaseSpeed,
            float monsterSlowedSpeed,
            float monsterPatrolSpeed,
            float monsterInvestigationSpeed,
            float hardPatrolSpeedMultiplier,
            float levelDuration,
            int requiredTasks)
        {
            this.playerWalkSpeed = playerWalkSpeed;
            this.playerSprintSpeed = playerSprintSpeed;
            this.playerOverclockSpeed = playerOverclockSpeed;
            this.monsterChaseSpeed = monsterChaseSpeed;
            this.monsterSlowedSpeed = monsterSlowedSpeed;
            this.monsterPatrolSpeed = monsterPatrolSpeed;
            this.monsterInvestigationSpeed = monsterInvestigationSpeed;
            this.hardPatrolSpeedMultiplier = hardPatrolSpeedMultiplier;
            this.levelDuration = levelDuration;
            this.requiredTasks = requiredTasks;
        }
    }
}

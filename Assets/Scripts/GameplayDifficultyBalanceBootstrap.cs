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

    public static void ApplyForScene(Scene scene)
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
            player.ApplyMovementTuning(profile.playerWalkSpeed, profile.playerSprintSpeed);
        }

        BasicMonsterController[] monsters = Object.FindObjectsByType<BasicMonsterController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        for (int i = 0; i < monsters.Length; i++)
        {
            monsters[i].ApplyMovementTuning(
                profile.monsterChaseSpeed,
                profile.monsterSlowedSpeed,
                profile.monsterPatrolSpeed,
                profile.monsterInvestigationSpeed,
                profile.hardPatrolSpeedMultiplier
            );
            monsters[i].ApplyBehaviorTuning(
                profile.monsterDetectionRadius,
                profile.monsterHearingRadius,
                profile.monsterSprintHearingRadius,
                profile.monsterMemoryDuration,
                profile.monsterForcedSearchDuration,
                profile.monsterPuzzleAlertDuration,
                profile.monsterPuzzleAlertSpeedMultiplier,
                profile.monsterSearchRadius,
                profile.monsterRouteRevisitDelay
            );
        }

        PrototypeGameManager gameManager = Object.FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager != null)
        {
            gameManager.ApplyRequiredTasks(profile.requiredTasks);
            gameManager.ApplyLevelTimeLimit(profile.levelDuration);
        }
    }

    private static bool TryGetProfile(string sceneName, out BalanceProfile profile)
    {
        switch (sceneName)
        {
            case EasyScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 3f,
                    playerSprintSpeed: 5f,
                    monsterChaseSpeed: 2f,
                    monsterSlowedSpeed: 1f,
                    monsterPatrolSpeed: 2f,
                    monsterInvestigationSpeed: 2f,
                    hardPatrolSpeedMultiplier: 1f,
                    monsterDetectionRadius: 5f,
                    monsterHearingRadius: 0f,
                    monsterSprintHearingRadius: 0f,
                    monsterMemoryDuration: 4f,
                    monsterForcedSearchDuration: 3f,
                    monsterPuzzleAlertDuration: 60f,
                    monsterPuzzleAlertSpeedMultiplier: 1f,
                    monsterSearchRadius: 3.8f,
                    monsterRouteRevisitDelay: 65f,
                    requiredTasks: 5,
                    levelDuration: 1800f
                );
                return true;

            case MediumScene:
            case DefaultScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 3f,
                    playerSprintSpeed: 5f,
                    monsterChaseSpeed: 2f,
                    monsterSlowedSpeed: 1f,
                    monsterPatrolSpeed: 2f,
                    monsterInvestigationSpeed: 2f,
                    hardPatrolSpeedMultiplier: 1f,
                    monsterDetectionRadius: 5f,
                    monsterHearingRadius: 7f,
                    monsterSprintHearingRadius: 11f,
                    monsterMemoryDuration: 8f,
                    monsterForcedSearchDuration: 5f,
                    monsterPuzzleAlertDuration: 60f,
                    monsterPuzzleAlertSpeedMultiplier: 1f,
                    monsterSearchRadius: 5.5f,
                    monsterRouteRevisitDelay: 45f,
                    requiredTasks: 7,
                    levelDuration: 1800f
                );
                return true;

            case HardScene:
                profile = new BalanceProfile(
                    playerWalkSpeed: 3f,
                    playerSprintSpeed: 5f,
                    monsterChaseSpeed: 2f,
                    monsterSlowedSpeed: 1f,
                    monsterPatrolSpeed: 2f,
                    monsterInvestigationSpeed: 2f,
                    hardPatrolSpeedMultiplier: 1f,
                    monsterDetectionRadius: 5f,
                    monsterHearingRadius: 9f,
                    monsterSprintHearingRadius: 14f,
                    monsterMemoryDuration: 12f,
                    monsterForcedSearchDuration: 7f,
                    monsterPuzzleAlertDuration: 60f,
                    monsterPuzzleAlertSpeedMultiplier: 1f,
                    monsterSearchRadius: 7f,
                    monsterRouteRevisitDelay: 30f,
                    requiredTasks: 5,
                    levelDuration: 2400f
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
        public readonly float monsterChaseSpeed;
        public readonly float monsterSlowedSpeed;
        public readonly float monsterPatrolSpeed;
        public readonly float monsterInvestigationSpeed;
        public readonly float hardPatrolSpeedMultiplier;
        public readonly float monsterDetectionRadius;
        public readonly float monsterHearingRadius;
        public readonly float monsterSprintHearingRadius;
        public readonly float monsterMemoryDuration;
        public readonly float monsterForcedSearchDuration;
        public readonly float monsterPuzzleAlertDuration;
        public readonly float monsterPuzzleAlertSpeedMultiplier;
        public readonly float monsterSearchRadius;
        public readonly float monsterRouteRevisitDelay;
        public readonly int requiredTasks;
        public readonly float levelDuration;

        public BalanceProfile(
            float playerWalkSpeed,
            float playerSprintSpeed,
            float monsterChaseSpeed,
            float monsterSlowedSpeed,
            float monsterPatrolSpeed,
            float monsterInvestigationSpeed,
            float hardPatrolSpeedMultiplier,
            float monsterDetectionRadius,
            float monsterHearingRadius,
            float monsterSprintHearingRadius,
            float monsterMemoryDuration,
            float monsterForcedSearchDuration,
            float monsterPuzzleAlertDuration,
            float monsterPuzzleAlertSpeedMultiplier,
            float monsterSearchRadius,
            float monsterRouteRevisitDelay,
            int requiredTasks,
            float levelDuration)
        {
            this.playerWalkSpeed = playerWalkSpeed;
            this.playerSprintSpeed = playerSprintSpeed;
            this.monsterChaseSpeed = monsterChaseSpeed;
            this.monsterSlowedSpeed = monsterSlowedSpeed;
            this.monsterPatrolSpeed = monsterPatrolSpeed;
            this.monsterInvestigationSpeed = monsterInvestigationSpeed;
            this.hardPatrolSpeedMultiplier = hardPatrolSpeedMultiplier;
            this.monsterDetectionRadius = monsterDetectionRadius;
            this.monsterHearingRadius = monsterHearingRadius;
            this.monsterSprintHearingRadius = monsterSprintHearingRadius;
            this.monsterMemoryDuration = monsterMemoryDuration;
            this.monsterForcedSearchDuration = monsterForcedSearchDuration;
            this.monsterPuzzleAlertDuration = monsterPuzzleAlertDuration;
            this.monsterPuzzleAlertSpeedMultiplier = monsterPuzzleAlertSpeedMultiplier;
            this.monsterSearchRadius = monsterSearchRadius;
            this.monsterRouteRevisitDelay = monsterRouteRevisitDelay;
            this.requiredTasks = requiredTasks;
            this.levelDuration = levelDuration;
        }
    }
}

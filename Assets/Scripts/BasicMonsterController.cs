using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(CharacterController))]
public class BasicMonsterController : MonoBehaviour
{
    private const string NurseSceneVisualName = "NurseSceneVisual";
    private const string NurseRuntimeVisualName = "NurseRuntimeVisual";

    private enum MonsterState
    {
        Dormant,
        Roaming,
        Investigating,
        Hunting,
        GuardingExit,
        Capturing
    }

    private enum IntelligenceLevel
    {
        Easy,
        Medium,
        Hard
    }

    private static readonly Vector3[] DefaultHospitalRoute =
    {
        new Vector3(-7.55f, 1f, 20f),
        new Vector3(-10f, 1f, 8f),
        new Vector3(-5.1f, 1f, 0f),
        new Vector3(18.2f, 1f, -5f),
        new Vector3(18.15f, 1f, -15f),
        new Vector3(20f, 1f, -2f),
        new Vector3(23.7f, 1f, 0.9f),
        new Vector3(18.2f, 1f, 5.1f)
    };

    private static readonly Dictionary<string, NavMeshDataInstance> RuntimeNavMeshInstances = new();
    private static readonly Dictionary<string, NavMeshData> RuntimeNavMeshData = new();

    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PrototypeGameManager gameManager;
    [SerializeField] private BasicFirstPersonController playerController;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField, Range(20f, 180f)] private float visionAngle = 110f;
    [SerializeField] private float closeRangeDetectionRadius = 2f;
    [SerializeField] private float maxVerticalSenseDistance = 2f;
    [SerializeField] private float hearingRadius = 7f;
    [SerializeField] private float sprintHearingRadius = 11f;
    [SerializeField] private float movementNoiseThreshold = 4f;
    [SerializeField] private float memoryDuration = 6f;
    [SerializeField] private float forcedSearchDuration = 4f;
    [SerializeField] private float huntPersistenceDuration = 0.65f;
    [SerializeField] private LayerMask sightBlockerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private string hardSceneName = "demoScene_Difficile";
    [SerializeField] private string mediumSceneName = "demoScene_Moyen";
    [SerializeField] private string easySceneName = "demoScene_Facile";
    [SerializeField] private float taskProgressDetectionBonus = 4f;
    [SerializeField] private float taskProgressHearingBonus = 5f;
    [SerializeField] private float taskProgressMemoryBonus = 3f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float slowedMoveSpeed = 3f;
    [SerializeField] private float investigationMoveSpeed = 2.8f;
    [SerializeField] private float taskProgressChaseSpeedBonus = 2f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 13f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float patrolMoveSpeed = 1.8f;
    [SerializeField] private float patrolStoppingDistance = 0.75f;
    [SerializeField] private float patrolWaitTime = 0.4f;
    [SerializeField] private float roamRadius = 2.5f;
    [SerializeField] private float roamRetargetInterval = 2.2f;
    [SerializeField, Range(0f, 1f)] private float localWanderChance = 0.4f;
    [SerializeField] private float roamPauseMin = 0.15f;
    [SerializeField] private float roamPauseMax = 0.7f;
    [SerializeField] private float obstacleCheckDistance = 1.25f;
    [SerializeField] private float obstacleCheckRadius = 0.35f;
    [SerializeField] private float obstacleAvoidanceStrength = 0.9f;
    [SerializeField] private float obstacleSideCheckAngle = 35f;
    [SerializeField] private float wallSlideStrength = 0.75f;
    [SerializeField] private float avoidanceLockDuration = 0.45f;
    [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 10f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float hardPatrolSpeedMultiplier = 1.05f;
    [SerializeField] private float hardTurnSpeedMultiplier = 1.35f;

    [Header("Exploration")]
    [SerializeField] private bool expandPatrolRouteFromNavMesh = true;
    [SerializeField] private float explorationPointSpacing = 6.5f;
    [SerializeField] private int maxGeneratedExplorationPoints = 56;
    [SerializeField] private bool prioritizeUnvisitedExploration = true;
    [SerializeField] private float routeRevisitDelay = 45f;

    [Header("Search")]
    [SerializeField] private float maxPredictionTime = 1.1f;
    [SerializeField] private float predictionStrength = 0.9f;
    [SerializeField] private float searchRadius = 5.5f;
    [SerializeField] private float searchRetargetInterval = 1.25f;
    [SerializeField] private int searchPointCount = 6;
    [SerializeField] private float puzzleAlertMinimumDuration = 60f;
    [SerializeField] private float puzzleAlertTravelBuffer = 12f;
    [SerializeField] private float puzzleAlertMinimumTravelDuration = 60f;
    [SerializeField] private float puzzleAlertMaximumTravelDuration = 180f;
    [SerializeField] private float puzzleAlertSpeedMultiplier = 1.18f;
    [SerializeField] private float puzzleAlertSearchRadiusMultiplier = 1.45f;
    [SerializeField] private float exitGuardDistance = 3.5f;
    [SerializeField] private float exitGuardRetargetInterval = 1f;

    [Header("Recovery")]
    [SerializeField] private float stuckRetargetDelay = 0.95f;
    [SerializeField] private float corridorRecoveryDistance = 1.9f;
    [SerializeField] private float stuckEscapeProbeDistance = 3.2f;
    [SerializeField] private float stuckEscapeDuration = 1.25f;
    [SerializeField] private float stuckMinimumProgressRatio = 0.18f;
    [SerializeField] private float wedgedContactAcceleration = 1.35f;
    [SerializeField] private float physicalDetourDistance = 3.5f;
    [SerializeField] private float physicalDetourDuration = 4f;
    [SerializeField] private float physicalDetourVerticalTolerance = 1.25f;

    [Header("Intelligence")]
    [SerializeField] private bool buildRuntimeNavMesh = true;
    [SerializeField] private LayerMask runtimeNavMeshLayerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private Vector3 runtimeNavMeshBoundsCenter = new Vector3(7f, 2f, 2f);
    [SerializeField] private Vector3 runtimeNavMeshBoundsSize = new Vector3(90f, 18f, 90f);
    [SerializeField] private float navMeshRepathInterval = 0.25f;
    [SerializeField] private float navMeshSampleDistance = 4f;
    [SerializeField] private float navMeshWaypointReachDistance = 0.55f;
    [SerializeField] private float routeWaypointReachDistance = 1.05f;
    [SerializeField] private float routeLineOfSightHeight = 0.9f;
    [SerializeField] private float routeLineOfSightRadius = 0.28f;
    [SerializeField] private float easyBlockedRouteDistance = 5f;
    [SerializeField] private float stairStepOffset = 0.45f;
    [SerializeField] private float stairSlopeLimit = 52f;
    [SerializeField] private float sightingAggressionBuildRate = 0.35f;
    [SerializeField] private float hearingAggressionBuildRate = 0.18f;
    [SerializeField] private float puzzleAlertAggressionBoost = 0.75f;
    [SerializeField] private float aggressionDecayRate = 0.045f;
    [SerializeField] private float aggressionChaseSpeedBonus = 0.18f;
    [SerializeField] private float aggressionInvestigationSpeedBonus = 0.12f;
    [SerializeField] private float aggressionDetectionBonus = 2.5f;

    [Header("Visual Motion")]
    [SerializeField] private Transform animatedVisual;
    [SerializeField] private bool useScaryNurseVisual = true;
    [SerializeField] private string nurseModelAssetPath = "Assets/mixamo/Scary Zombie Pack/Nurse.fbx";
    [SerializeField] private string scaryZombieAnimationAssetFolder = "Assets/mixamo/Scary Zombie Pack";
    [SerializeField] private float nurseTargetVisualHeight = 1.95f;
    [SerializeField] private Vector3 nurseVisualLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 nurseVisualLocalEulerAngles = Vector3.zero;
    [SerializeField] private bool alignVisualToGround = true;
    [SerializeField] private float visualGroundOffset = 0f;
    [SerializeField] private float visualGroundingSmoothing = 24f;
    [SerializeField] private float walkBobAmplitude = 0.02f;
    [SerializeField] private float runBobAmplitude = 0.075f;
    [SerializeField] private float walkBobFrequency = 2.3f;
    [SerializeField] private float runBobFrequency = 6.2f;
    [SerializeField] private float maxRollAngle = 6f;
    [SerializeField] private float maxPitchAngle = 4f;
    [SerializeField] private float visualSmoothing = 10f;

    [Header("Capture")]
    [SerializeField] private float captureTurnSpeedMultiplier = 1.8f;

    [Header("Mixamo Animation")]
    [SerializeField] private string mixamoResourcesFolder = "Mixamo";
    [SerializeField] private string idleClipResourceName = "Idle_with_skin";
    [SerializeField] private string walkClipResourceName = "Zombie Walk";
    [SerializeField] private string runClipResourceName = "Zombie Running";
    [SerializeField] private string turnClipResourceName = "Zombie Turn";
    [SerializeField] private string attackClipResourceName = "Zombie Attack";
    [SerializeField] private float animationBlendDuration = 0.18f;
    [SerializeField] private float animationMoveThreshold = 0.12f;
    [SerializeField] private float turnAnimationAngularSpeedThreshold = 80f;
    [SerializeField] private float turnAnimationMaxMoveSpeed = 0.2f;

    public bool IsSlowed => slowTimer > 0f;
    public float SlowTimeRemaining => Mathf.Max(0f, slowTimer);
    public bool IsHuntingState => currentState == MonsterState.Hunting;
    public bool IsInvestigatingState => currentState == MonsterState.Investigating;
    public bool IsGuardingExitState => currentState == MonsterState.GuardingExit;
    public bool IsRoamingState => currentState == MonsterState.Roaming;
    public bool IsCaptureInProgress => captureTarget != null;
    public float MaxVerticalSenseDistance => maxVerticalSenseDistance;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float forcedRevealTimer;
    private float slowTimer;
    private float memoryTimer;
    private float searchRetargetTimer;
    private float patrolWaitTimer;
    private float roamRetargetTimer;
    private float roamPauseTimer;
    private float stuckTimer;
    private int repeatedStuckCount;
    private float huntPersistenceTimer;
    private float avoidanceLockTimer;
    private float puzzleAlertTimer;
    private float aggressionLevel;
    private int patrolPointIndex;
    private int searchPointIndex;
    private int lastAvoidanceSign = 1;
    private int guardOrbitSign = 1;
    private IntelligenceLevel intelligenceLevel = IntelligenceLevel.Medium;
    private MonsterState currentState = MonsterState.Dormant;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private float visualCycle;
    private Vector3 lastPosition;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 puzzleAlertOrigin;
    private Vector3 investigationTarget;
    private Vector3 roamTarget;
    private Vector3 guardTarget;
    private Vector3 avoidanceLockDirection;
    private Vector3 stuckEscapeTarget;
    private Transform captureTarget;
    private Vector3[] navigationRoute;
    private float[] routeLastVisitTimes;
    private int[] routeVisitCounts;
    private NavMeshPath currentNavMeshPath;
    private Vector3 currentNavMeshTarget;
    private float navMeshRepathTimer;
    private bool runtimeNavMeshReady;
    private bool hasInvestigationTarget;
    private bool hasRoamTarget;
    private float stuckEscapeTimer;
    private Animator visualAnimator;
    private PlayableGraph animationGraph;
    private AnimationMixerPlayable animationMixer;
    private AnimationClipPlayable activeAnimationPlayable;
    private AnimationClipPlayable transitionAnimationPlayable;
    private AnimationClip activeAnimationClip;
    private AnimationClip transitionAnimationClip;
    private bool activeAnimationLoops;
    private bool transitionAnimationLoops;
    private bool animationSystemReady;
    private bool animationTransitionActive;
    private float animationBlendTimer;
    private float lastYaw;
    private AnimationClip idleAnimationClip;
    private AnimationClip walkAnimationClip;
    private AnimationClip runAnimationClip;
    private AnimationClip turnAnimationClip;
    private AnimationClip attackAnimationClip;
    private readonly Collider[] overlapBuffer = new Collider[16];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        ConfigureIntelligenceForScene(SceneManager.GetActiveScene().name);
        ConfigureControllerForStairs();
        EnsureRuntimeNavMesh();
        currentNavMeshPath = new NavMeshPath();

        TryAssignPlayerTarget();

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        ConfigureScaryNurseVisual();

        if (animatedVisual == null)
        {
            animatedVisual = FindAnimatedVisual();
        }

        if (animatedVisual != null)
        {
            visualBaseLocalPosition = animatedVisual.localPosition;
            visualBaseLocalRotation = animatedVisual.localRotation;
            AlignAnimatedVisualToGround(true);
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = FindPatrolPointsInScene();
        }

        navigationRoute = BuildNavigationRoute();
        InitializeRouteVisitMemory();

        lastPosition = transform.position;
        lastYaw = transform.eulerAngles.y;
        TryInitializeMixamoAnimations();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !useScaryNurseVisual)
        {
            return;
        }

        ApplyScaryNurseAnimationNames();
        EditorApplication.delayCall -= EnsureNurseSceneVisualAfterValidation;
        EditorApplication.delayCall += EnsureNurseSceneVisualAfterValidation;
    }

    [InitializeOnLoadMethod]
    private static void EnsureNurseVisualsAfterScriptReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            BasicMonsterController[] monsters = Object.FindObjectsByType<BasicMonsterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < monsters.Length; i++)
            {
                BasicMonsterController monster = monsters[i];
                if (monster != null && monster.useScaryNurseVisual)
                {
                    monster.EnsureNurseSceneVisual();
                }
            }
        };
    }

    private void EnsureNurseSceneVisualAfterValidation()
    {
        if (this == null || Application.isPlaying || !useScaryNurseVisual || gameObject == null || !gameObject.scene.IsValid())
        {
            return;
        }

        EnsureNurseSceneVisual();
    }
#endif

    private void Update()
    {
        TickTimers();
        HandleMovement();
        UpdateMixamoAnimation();
    }

    private void LateUpdate()
    {
        if (animationSystemReady)
        {
            AlignAnimatedVisualToGround(false);
            return;
        }

        UpdateVisualMotion();
    }

    private void OnDestroy()
    {
        if (animationGraph.IsValid())
        {
            animationGraph.Destroy();
        }
    }

    public void RevealPlayerPosition(float duration)
    {
        InvestigatePlayerPositionSnapshot(duration);
    }

    public void AssignPlayerTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        playerTarget = target;
        playerController = target.GetComponent<BasicFirstPersonController>();
    }

    private bool TryAssignPlayerTarget()
    {
        if (playerTarget != null)
        {
            if (playerController == null)
            {
                playerController = playerTarget.GetComponent<BasicFirstPersonController>();
            }

            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            playerController = player.GetComponent<BasicFirstPersonController>();
            return true;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (playerController != null)
        {
            playerTarget = playerController.transform;
            return true;
        }

        return false;
    }

    public bool IsWithinVerticalSenseRange(Transform target)
    {
        return target != null && IsWithinVerticalSenseRange(target.position);
    }

    public bool IsWithinVerticalSenseRange(Vector3 worldPosition)
    {
        return Mathf.Abs(worldPosition.y - transform.position.y) <= maxVerticalSenseDistance;
    }

    public void HuntPlayerAfterPuzzleFailure(float duration)
    {
        HuntPlayerAfterPuzzleFailure(duration, Vector3.zero, false);
    }

    public void HuntPlayerAfterPuzzleFailure(float duration, Vector3 fallbackHuntPosition)
    {
        HuntPlayerAfterPuzzleFailure(duration, fallbackHuntPosition, true);
    }

    private void HuntPlayerAfterPuzzleFailure(float duration, Vector3 fallbackHuntPosition, bool hasFallbackHuntPosition)
    {
        TryAssignPlayerTarget();

        float effectiveDuration = Mathf.Max(duration, puzzleAlertMinimumDuration, forcedSearchDuration, memoryDuration);
        Vector3 alertPosition = playerTarget != null
            ? playerTarget.position
            : hasFallbackHuntPosition ? fallbackHuntPosition : transform.position;

        InvestigatePosition(alertPosition, effectiveDuration, true);
    }

    public void InvestigatePlayerPositionSnapshot(float duration)
    {
        TryAssignPlayerTarget();

        if (playerTarget == null)
        {
            Debug.LogWarning("L'Entite ne peut pas enqueter: aucun joueur trouve.");
            return;
        }

        InvestigatePosition(playerTarget.position, duration, true);
    }

    public void InvestigatePosition(Vector3 worldPosition, float duration, bool highPriority)
    {
        float baseDuration = highPriority
            ? Mathf.Max(duration, puzzleAlertMinimumDuration, forcedSearchDuration, memoryDuration)
            : Mathf.Max(duration, forcedSearchDuration);
        float effectiveDuration = highPriority
            ? GetPuzzleAlertDurationFor(worldPosition, baseDuration)
            : baseDuration;

        // An alert records one position only. It never reveals the player's future movement.
        forcedRevealTimer = 0f;
        huntPersistenceTimer = 0f;
        puzzleAlertTimer = Mathf.Max(puzzleAlertTimer, effectiveDuration);
        if (highPriority)
        {
            aggressionLevel = Mathf.Max(aggressionLevel, puzzleAlertAggressionBoost);
        }

        puzzleAlertOrigin = worldPosition;
        roamPauseTimer = 0f;
        patrolWaitTimer = 0f;
        searchRetargetTimer = 0f;
        stuckTimer = 0f;
        hasRoamTarget = false;
        hasInvestigationTarget = false;

        if (currentNavMeshPath != null)
        {
            currentNavMeshPath.ClearCorners();
        }

        currentNavMeshTarget = Vector3.positiveInfinity;

        RememberPlayerPosition(worldPosition, effectiveDuration);
        investigationTarget = worldPosition;
        hasInvestigationTarget = true;

        Debug.Log("L'Entite enquete sur la derniere position connue pendant " + effectiveDuration.ToString("0.0") + " secondes.");
    }

    public void ApplyStaticTrap(float duration)
    {
        slowTimer = Mathf.Max(slowTimer, duration);
        Debug.Log("L'Entite est ralentie pendant " + duration + " secondes.");
    }

    public void BeginCapture(Transform target)
    {
        if (target == null)
        {
            return;
        }

        captureTarget = target;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Mathf.Min(verticalVelocity, 0f);
        roamPauseTimer = 0f;
        patrolWaitTimer = 0f;
        searchRetargetTimer = 0f;
        forcedRevealTimer = 0f;
    }

    public void ApplyMovementTuning(
        float newMoveSpeed,
        float newSlowedMoveSpeed,
        float newPatrolMoveSpeed,
        float newInvestigationMoveSpeed,
        float newHardPatrolSpeedMultiplier
    )
    {
        moveSpeed = Mathf.Max(0.1f, newMoveSpeed);
        slowedMoveSpeed = Mathf.Clamp(newSlowedMoveSpeed, 0.1f, moveSpeed);
        patrolMoveSpeed = Mathf.Clamp(newPatrolMoveSpeed, 0.1f, moveSpeed);
        investigationMoveSpeed = Mathf.Clamp(newInvestigationMoveSpeed, patrolMoveSpeed, moveSpeed);
        hardPatrolSpeedMultiplier = Mathf.Max(0.5f, newHardPatrolSpeedMultiplier);
    }

    public void ApplyBehaviorTuning(
        float newDetectionRadius,
        float newHearingRadius,
        float newSprintHearingRadius,
        float newMemoryDuration,
        float newForcedSearchDuration,
        float newPuzzleAlertMinimumDuration,
        float newPuzzleAlertSpeedMultiplier,
        float newSearchRadius,
        float newRouteRevisitDelay
    )
    {
        detectionRadius = Mathf.Max(closeRangeDetectionRadius, newDetectionRadius);
        hearingRadius = Mathf.Max(0f, newHearingRadius);
        sprintHearingRadius = Mathf.Max(hearingRadius, newSprintHearingRadius);
        memoryDuration = Mathf.Max(0.5f, newMemoryDuration);
        forcedSearchDuration = Mathf.Max(0.5f, newForcedSearchDuration);
        puzzleAlertMinimumDuration = Mathf.Max(forcedSearchDuration, newPuzzleAlertMinimumDuration);
        puzzleAlertSpeedMultiplier = Mathf.Max(1f, newPuzzleAlertSpeedMultiplier);
        searchRadius = Mathf.Max(1f, newSearchRadius);
        routeRevisitDelay = Mathf.Max(5f, newRouteRevisitDelay);
    }

    private void ConfigureIntelligenceForScene(string sceneName)
    {
        if (sceneName == easySceneName)
        {
            intelligenceLevel = IntelligenceLevel.Easy;
        }
        else if (sceneName == hardSceneName)
        {
            intelligenceLevel = IntelligenceLevel.Hard;
        }
        else if (sceneName == mediumSceneName || sceneName == "demoScene")
        {
            intelligenceLevel = IntelligenceLevel.Medium;
        }
        else
        {
            intelligenceLevel = IntelligenceLevel.Medium;
        }
    }

    private void ConfigureControllerForStairs()
    {
        if (controller == null)
        {
            return;
        }

        controller.stepOffset = Mathf.Max(controller.stepOffset, stairStepOffset);
        controller.slopeLimit = Mathf.Max(controller.slopeLimit, stairSlopeLimit);
    }

    private void EnsureRuntimeNavMesh()
    {
        if (!buildRuntimeNavMesh)
        {
            runtimeNavMeshReady = HasNavMeshNear(transform.position);
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (RuntimeNavMeshInstances.ContainsKey(sceneName))
        {
            runtimeNavMeshReady = true;
            return;
        }

        Bounds bounds = new Bounds(runtimeNavMeshBoundsCenter, runtimeNavMeshBoundsSize);
        List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

        NavMeshBuilder.CollectSources(
            bounds,
            runtimeNavMeshLayerMask,
            NavMeshCollectGeometry.PhysicsColliders,
            0,
            markups,
            sources
        );

        if (sources.Count == 0)
        {
            NavMeshBuilder.CollectSources(
                bounds,
                runtimeNavMeshLayerMask,
                NavMeshCollectGeometry.RenderMeshes,
                0,
                markups,
                sources
            );
        }

        if (sources.Count == 0)
        {
            runtimeNavMeshReady = false;
            Debug.LogWarning("Aucune source trouvee pour construire la navigation du zombie.");
            return;
        }

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        float scaleY = controller != null ? Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f) : 1f;
        if (controller != null)
        {
            settings.agentRadius = Mathf.Max(0.18f, controller.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)));
            settings.agentHeight = Mathf.Max(0.8f, controller.height * scaleY);
            settings.agentClimb = Mathf.Max(settings.agentClimb, stairStepOffset);
            settings.agentSlope = Mathf.Max(settings.agentSlope, stairSlopeLimit);
        }

        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            settings,
            sources,
            bounds,
            Vector3.zero,
            Quaternion.identity
        );

        if (navMeshData == null)
        {
            runtimeNavMeshReady = false;
            Debug.LogWarning("Impossible de construire la navigation du zombie.");
            return;
        }

        RuntimeNavMeshData[sceneName] = navMeshData;
        RuntimeNavMeshInstances[sceneName] = NavMesh.AddNavMeshData(navMeshData);
        runtimeNavMeshReady = true;
        Debug.Log("Navigation runtime du zombie construite pour " + sceneName + ".");
    }

    private bool HasNavMeshNear(Vector3 worldPosition)
    {
        return NavMesh.SamplePosition(worldPosition, out _, navMeshSampleDistance, NavMesh.AllAreas);
    }

    public void NotifySuspiciousActivity(Vector3 worldPosition, float duration, bool highPriority)
    {
        InvestigatePosition(worldPosition, duration, highPriority);
    }

    private void TickTimers()
    {
        if (forcedRevealTimer > 0f)
        {
            forcedRevealTimer -= Time.deltaTime;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
        }

        if (memoryTimer > 0f)
        {
            memoryTimer -= Time.deltaTime;
            if (memoryTimer <= 0f)
            {
                hasInvestigationTarget = false;
            }
        }

        if (searchRetargetTimer > 0f)
        {
            searchRetargetTimer -= Time.deltaTime;
        }

        if (roamRetargetTimer > 0f)
        {
            roamRetargetTimer -= Time.deltaTime;
        }

        if (roamPauseTimer > 0f)
        {
            roamPauseTimer -= Time.deltaTime;
        }

        if (huntPersistenceTimer > 0f)
        {
            huntPersistenceTimer -= Time.deltaTime;
        }

        if (puzzleAlertTimer > 0f)
        {
            puzzleAlertTimer -= Time.deltaTime;
        }

        if (aggressionLevel > 0f)
        {
            aggressionLevel = Mathf.Max(0f, aggressionLevel - aggressionDecayRate * Time.deltaTime);
        }

        if (avoidanceLockTimer > 0f)
        {
            avoidanceLockTimer -= Time.deltaTime;
        }

        if (stuckEscapeTimer > 0f)
        {
            stuckEscapeTimer -= Time.deltaTime;
        }

        if (navMeshRepathTimer > 0f)
        {
            navMeshRepathTimer -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        bool canMove =
            gameManager != null &&
            (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Chase ||
             gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape);

        if (captureTarget != null)
        {
            currentState = MonsterState.Capturing;
            HandleCaptureMovement();
            return;
        }

        MonsterState nextState = ResolveState(canMove, out Vector3 rawVelocity, out bool isChasing, out bool isInvestigating);
        currentState = nextState;

        Vector3 targetVelocity = canMove ? ApplyObstacleAvoidance(rawVelocity, isChasing) : Vector3.zero;

        if (targetVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetVelocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                GetCurrentTurnSpeed() * Time.deltaTime
            );
        }

        float currentAcceleration = targetVelocity.sqrMagnitude > horizontalVelocity.sqrMagnitude
            ? acceleration
            : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, currentAcceleration * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);

        UpdateNavigationRecovery(targetVelocity, isChasing, isInvestigating);
    }

    private void HandleCaptureMovement()
    {
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration * Time.deltaTime);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 toTarget = captureTarget.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                GetCurrentTurnSpeed() * captureTurnSpeedMultiplier * Time.deltaTime
            );
        }

        Vector3 finalVelocity = Vector3.zero;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
        lastPosition = transform.position;
    }

    private MonsterState ResolveState(bool canMove, out Vector3 velocity, out bool isChasing, out bool isInvestigating)
    {
        velocity = Vector3.zero;
        isChasing = false;
        isInvestigating = false;

        if (!canMove)
        {
            return MonsterState.Dormant;
        }

        if (playerTarget == null)
        {
            TryAssignPlayerTarget();
        }

        if (playerTarget == null)
        {
            if ((CanInvestigateMemory() || puzzleAlertTimer > 0f) && memoryTimer > 0f)
            {
                velocity = GetInvestigationVelocity();
                isInvestigating = velocity.sqrMagnitude > 0.001f;
                return MonsterState.Investigating;
            }

            if (CanGuardExit() && TryGetExitGuardVelocity(out velocity))
            {
                isInvestigating = true;
                return MonsterState.GuardingExit;
            }

            velocity = GetRoamingVelocity();
            return MonsterState.Roaming;
        }

        Vector3 offsetToPlayer = playerTarget.position - transform.position;
        Vector3 flatOffset = new Vector3(offsetToPlayer.x, 0f, offsetToPlayer.z);
        float distanceToPlayer = flatOffset.magnitude;
        bool hasForcedReveal = forcedRevealTimer > 0f;
        bool canSeePlayer = CanSeePlayer(distanceToPlayer);
        bool canHearPlayer = CanUseHearing() && CanHearPlayer(distanceToPlayer);

        // Priorite: chasse si le joueur est vu, enquete si un indice recent existe,
        // puis garde de sortie ou errance selon la phase et la difficulte.
        if (hasForcedReveal || canSeePlayer)
        {
            RaiseAggression((hasForcedReveal ? sightingAggressionBuildRate * 0.6f : sightingAggressionBuildRate) * Time.deltaTime);
            huntPersistenceTimer = huntPersistenceDuration;
            RememberPlayerPosition(
                CanPredictPlayerMovement() ? GetPredictedPlayerPosition(distanceToPlayer) : playerTarget.position,
                hasForcedReveal ? forcedSearchDuration : GetCurrentMemoryDuration()
            );

            velocity = GetChaseVelocity(distanceToPlayer);
            isChasing = true;
            return MonsterState.Hunting;
        }

        if (canHearPlayer)
        {
            RaiseAggression(hearingAggressionBuildRate * Time.deltaTime);
            RememberPlayerPosition(
                CanPredictPlayerMovement() ? GetPredictedPlayerPosition(distanceToPlayer) : playerTarget.position,
                GetCurrentMemoryDuration() * 0.85f
            );
            velocity = GetInvestigationVelocity();
            isInvestigating = velocity.sqrMagnitude > 0.001f;
            return MonsterState.Investigating;
        }

        if (huntPersistenceTimer > 0f && memoryTimer > 0f)
        {
            velocity = GetChaseVelocity(distanceToPlayer);
            isChasing = true;
            return MonsterState.Hunting;
        }

        if ((CanInvestigateMemory() || puzzleAlertTimer > 0f) && memoryTimer > 0f)
        {
            velocity = GetInvestigationVelocity();
            isInvestigating = velocity.sqrMagnitude > 0.001f;
            return MonsterState.Investigating;
        }

        if (CanGuardExit() && TryGetExitGuardVelocity(out velocity))
        {
            isInvestigating = true;
            return MonsterState.GuardingExit;
        }

        velocity = GetRoamingVelocity();
        return MonsterState.Roaming;
    }

    private Vector3 GetChaseVelocity(float distanceToPlayer)
    {
        if (distanceToPlayer <= stoppingDistance)
        {
            return Vector3.zero;
        }

        Vector3 chaseTarget = CanInvestigateMemory() ? lastKnownPlayerPosition : playerTarget.position;
        return GetIntelligentVelocityTo(chaseTarget, GetCurrentChaseSpeed(), true, stoppingDistance);
    }

    private Vector3 GetInvestigationVelocity()
    {
        if (memoryTimer <= 0f)
        {
            hasInvestigationTarget = false;
            return Vector3.zero;
        }

        Vector3 anchor = puzzleAlertTimer > 0f && !CanInvestigateMemory()
            ? puzzleAlertOrigin
            : lastKnownPlayerPosition;
        float distanceToAnchor = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(anchor));
        if (distanceToAnchor > patrolStoppingDistance + 0.35f)
        {
            hasInvestigationTarget = false;
            return GetIntelligentVelocityTo(anchor, GetCurrentInvestigationSpeed(), false, patrolStoppingDistance);
        }

        if (!hasInvestigationTarget ||
            searchRetargetTimer <= 0f ||
            Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(investigationTarget)) <= patrolStoppingDistance)
        {
            PickNextSearchTarget();
        }

        if (!hasInvestigationTarget)
        {
            return Vector3.zero;
        }

        return GetIntelligentVelocityTo(investigationTarget, GetCurrentInvestigationSpeed(), false, patrolStoppingDistance);
    }

    private Vector3 GetRoamingVelocity()
    {
        if (roamPauseTimer > 0f)
        {
            return Vector3.zero;
        }

        if (!hasRoamTarget || roamRetargetTimer <= 0f)
        {
            PickNextRoamTarget();
        }

        if (!hasRoamTarget)
        {
            return Vector3.zero;
        }

        float distanceToRoamTarget = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(roamTarget));
        if (distanceToRoamTarget <= patrolStoppingDistance)
        {
            hasRoamTarget = false;
            roamPauseTimer = Random.Range(roamPauseMin, roamPauseMax);
            return Vector3.zero;
        }

        return GetIntelligentVelocityTo(roamTarget, GetCurrentRoamSpeed(), false, patrolStoppingDistance);
    }

    private void PickNextRoamTarget()
    {
        bool hasAnchor = TryGetCurrentPatrolAnchorPosition(out Vector3 anchorPosition);
        Vector3 planarPosition = GetPlanarPosition(transform.position);
        Vector3 planarAnchor = GetPlanarPosition(anchorPosition);

        if (hasAnchor && Vector3.Distance(planarPosition, planarAnchor) <= patrolStoppingDistance)
        {
            MarkCurrentRoutePointVisited();
            patrolWaitTimer += Time.deltaTime;
            float effectiveWaitTime = GetEffectivePatrolWaitTime();
            if (patrolWaitTimer >= effectiveWaitTime)
            {
                patrolWaitTimer = 0f;
                AdvancePatrolPoint();
                hasAnchor = TryGetCurrentPatrolAnchorPosition(out anchorPosition);
                planarAnchor = GetPlanarPosition(anchorPosition);
            }
        }
        else
        {
            patrolWaitTimer = 0f;
        }

        bool wanderNearAnchor = CanWanderAroundPatrolAnchors() && Random.value < GetEffectiveWanderChance();
        Vector2 jitter2D = wanderNearAnchor ? Random.insideUnitCircle * roamRadius : Vector2.zero;
        roamTarget = new Vector3(planarAnchor.x + jitter2D.x, transform.position.y, planarAnchor.z + jitter2D.y);
        hasRoamTarget = true;
        roamRetargetTimer = GetEffectiveRoamRetargetInterval() * Random.Range(0.85f, 1.2f);
        searchRetargetTimer = 0f;
    }

    private bool TryGetCurrentPatrolAnchorPosition(out Vector3 anchorPosition)
    {
        if (navigationRoute != null && navigationRoute.Length > 0)
        {
            patrolPointIndex = Mathf.Clamp(patrolPointIndex, 0, navigationRoute.Length - 1);
            anchorPosition = navigationRoute[patrolPointIndex];
            return true;
        }

        Transform patrolAnchor = GetCurrentPatrolAnchor();
        if (patrolAnchor != null)
        {
            anchorPosition = patrolAnchor.position;
            return true;
        }

        anchorPosition = transform.position;
        return false;
    }

    private Transform GetCurrentPatrolAnchor()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return null;
        }

        if (patrolPoints[patrolPointIndex] == null)
        {
            AdvancePatrolPoint();
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return null;
            }
        }

        return patrolPoints[patrolPointIndex];
    }

    private bool TryGetExitGuardVelocity(out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (gameManager == null ||
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape ||
            gameManager.ActiveExitZone == null ||
            gameManager.ActiveExitZone.transform == null)
        {
            return false;
        }

        Vector3 exitPosition = gameManager.ActiveExitZone.transform.position;
        Vector3 planarExit = GetPlanarPosition(exitPosition);
        float distanceToExit = Vector3.Distance(GetPlanarPosition(transform.position), planarExit);

        if (searchRetargetTimer <= 0f || guardTarget == Vector3.zero)
        {
            Vector3 toExit = planarExit - GetPlanarPosition(transform.position);
            Vector3 tangent = toExit.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, toExit.normalized) * guardOrbitSign
                : Vector3.right * guardOrbitSign;

            float orbitRadius = Random.Range(exitGuardDistance * 0.6f, exitGuardDistance * 1.1f);
            guardOrbitSign *= -1;
            guardTarget = exitPosition + tangent * orbitRadius;
            guardTarget.y = transform.position.y;
            searchRetargetTimer = exitGuardRetargetInterval * Random.Range(0.8f, 1.2f);
        }

        if (distanceToExit > exitGuardDistance * 1.1f)
        {
            velocity = GetIntelligentVelocityTo(exitPosition, GetCurrentInvestigationSpeed(), false, patrolStoppingDistance);
            return velocity.sqrMagnitude > 0.001f;
        }

        velocity = GetIntelligentVelocityTo(guardTarget, GetCurrentInvestigationSpeed(), false, patrolStoppingDistance);
        return velocity.sqrMagnitude > 0.001f;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredVelocity, bool urgent)
    {
        if (desiredVelocity.sqrMagnitude <= 0.001f)
        {
            avoidanceLockTimer = 0f;
            return desiredVelocity;
        }

        Vector3 desiredDirection = desiredVelocity.normalized;
        float desiredSpeed = desiredVelocity.magnitude;
        Vector3 origin = GetNavigationProbeOrigin();

        if (avoidanceLockTimer > 0f &&
            avoidanceLockDirection.sqrMagnitude > 0.001f &&
            GetDirectionClearance(origin, avoidanceLockDirection.normalized) > obstacleCheckDistance * 0.45f)
        {
            Vector3 lockedDirection = Vector3.Slerp(
                desiredDirection,
                avoidanceLockDirection.normalized,
                Mathf.Clamp01(obstacleAvoidanceStrength)
            ).normalized;
            return lockedDirection * desiredSpeed;
        }

        float forwardClearance = GetDirectionClearance(origin, desiredDirection);
        if (forwardClearance >= obstacleCheckDistance * 0.92f)
        {
            return desiredVelocity;
        }

        // On evalue plusieurs angles plutot que de pivoter brutalement,
        // ce qui stabilise la navigation dans les couloirs, coins et escaliers.
        float[] probeAngles =
        {
            0f,
            -obstacleSideCheckAngle,
            obstacleSideCheckAngle,
            -obstacleSideCheckAngle * 1.8f,
            obstacleSideCheckAngle * 1.8f,
            -75f,
            75f,
            -110f,
            110f
        };

        Vector3 bestDirection = desiredDirection;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < probeAngles.Length; i++)
        {
            Vector3 probeDirection = Quaternion.Euler(0f, probeAngles[i], 0f) * desiredDirection;
            float clearance = GetDirectionClearance(origin, probeDirection);
            float alignment = Vector3.Dot(desiredDirection, probeDirection);
            float urgencyBias = urgent ? Mathf.Max(0f, alignment) * 0.4f : 0f;
            float score = clearance * 1.3f + alignment + urgencyBias;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = probeDirection;
            }
        }

        if (bestScore > forwardClearance + 0.05f)
        {
            if (Vector3.Dot(bestDirection, transform.right) >= 0f)
            {
                lastAvoidanceSign = 1;
            }
            else
            {
                lastAvoidanceSign = -1;
            }

            avoidanceLockDirection = bestDirection;
            avoidanceLockTimer = avoidanceLockDuration;

            Vector3 steeredDirection = Vector3.Slerp(
                desiredDirection,
                bestDirection,
                Mathf.Clamp01(obstacleAvoidanceStrength)
            ).normalized;
            return steeredDirection * desiredSpeed;
        }

        if (Physics.SphereCast(
            origin,
            obstacleCheckRadius,
            desiredDirection,
            out RaycastHit hit,
            obstacleCheckDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore) &&
            IsBlockingHit(hit))
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(desiredDirection, hit.normal).normalized;
            if (slideDirection.sqrMagnitude > 0.001f)
            {
                avoidanceLockDirection = slideDirection;
                avoidanceLockTimer = avoidanceLockDuration * 0.85f;
                return Vector3.Slerp(desiredDirection, slideDirection, wallSlideStrength).normalized * desiredSpeed;
            }
        }

        return desiredDirection * desiredSpeed * 0.35f;
    }

    private float GetDirectionClearance(Vector3 origin, Vector3 direction)
    {
        float totalClearance = 0f;
        int validSamples = 0;
        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);

        float[] heightOffsets =
        {
            Mathf.Max(controller.height * scaleY * 0.32f, 0.65f),
            Mathf.Max(controller.height * scaleY * 0.62f, 1.15f)
        };

        for (int i = 0; i < heightOffsets.Length; i++)
        {
            Vector3 sampleOrigin = transform.position + Vector3.up * heightOffsets[i];
            if (Physics.SphereCast(
                sampleOrigin,
                obstacleCheckRadius,
                direction,
                out RaycastHit hit,
                obstacleCheckDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                if (IsBlockingHit(hit))
                {
                    totalClearance += Mathf.Max(0f, hit.distance);
                    validSamples++;
                }
                else
                {
                    totalClearance += obstacleCheckDistance;
                    validSamples++;
                }
            }
            else
            {
                totalClearance += obstacleCheckDistance;
                validSamples++;
            }
        }

        return validSamples > 0 ? totalClearance / validSamples : 0f;
    }

    private bool IsBlockingHit(RaycastHit hit)
    {
        if (hit.transform == null)
        {
            return false;
        }

        if (hit.transform.IsChildOf(transform))
        {
            return false;
        }

        if (playerTarget != null && hit.transform.IsChildOf(playerTarget))
        {
            return false;
        }

        if (hit.normal.y > 0.45f)
        {
            return false;
        }

        return true;
    }

    private bool TryGetDepenetrationEscapeDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (controller == null)
        {
            return false;
        }

        GetControllerWorldCapsule(transform.position, out Vector3 pointA, out Vector3 pointB, out float radius);
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            pointA,
            pointB,
            radius * 1.04f,
            overlapBuffer,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapBuffer[i];
            overlapBuffer[i] = null;
            if (other == null ||
                other.transform == null ||
                other.transform.IsChildOf(transform) ||
                (playerTarget != null && other.transform.IsChildOf(playerTarget)))
            {
                continue;
            }

            if (!Physics.ComputePenetration(
                controller,
                transform.position,
                transform.rotation,
                other,
                other.transform.position,
                other.transform.rotation,
                out Vector3 pushDirection,
                out float pushDistance))
            {
                continue;
            }

            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            direction += pushDirection.normalized * Mathf.Max(pushDistance, 0.08f);
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        direction.Normalize();
        return true;
    }

    private bool HasBodySpaceAt(Vector3 worldPosition)
    {
        if (controller == null)
        {
            return true;
        }

        GetControllerWorldCapsule(worldPosition, out Vector3 pointA, out Vector3 pointB, out float radius);
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            pointA,
            pointB,
            radius * 0.96f,
            overlapBuffer,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = overlapBuffer[i];
            overlapBuffer[i] = null;
            if (other == null ||
                other.transform == null ||
                other.transform.IsChildOf(transform) ||
                (playerTarget != null && other.transform.IsChildOf(playerTarget)))
            {
                continue;
            }

            if (!Physics.ComputePenetration(
                controller,
                worldPosition,
                transform.rotation,
                other,
                other.transform.position,
                other.transform.rotation,
                out Vector3 pushDirection,
                out _))
            {
                return false;
            }

            if (pushDirection.y > 0.45f)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void GetControllerWorldCapsule(Vector3 worldPosition, out Vector3 pointA, out Vector3 pointB, out float radius)
    {
        Vector3 lossyScale = transform.lossyScale;
        float scaleX = Mathf.Abs(lossyScale.x);
        float scaleY = Mathf.Abs(lossyScale.y);
        float scaleZ = Mathf.Abs(lossyScale.z);
        float horizontalScale = Mathf.Max(Mathf.Max(scaleX, scaleZ), 0.0001f);

        radius = Mathf.Max(controller.radius * horizontalScale, 0.05f);
        float height = Mathf.Max(controller.height * Mathf.Max(scaleY, 0.0001f), radius * 2f);
        Vector3 center = worldPosition + transform.TransformVector(controller.center);
        Vector3 up = transform.up;
        float capsuleHalf = Mathf.Max(0f, height * 0.5f - radius);

        pointA = center + up * capsuleHalf;
        pointB = center - up * capsuleHalf;
    }

    private void UpdateNavigationRecovery(Vector3 targetVelocity, bool isChasing, bool isInvestigating)
    {
        Vector3 planarDelta = transform.position - lastPosition;
        planarDelta.y = 0f;
        lastPosition = transform.position;

        if (targetVelocity.sqrMagnitude <= 0.04f)
        {
            stuckTimer = 0f;
            return;
        }

        Vector3 targetDirection = targetVelocity.normalized;
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude <= 0.001f)
        {
            stuckTimer = 0f;
            return;
        }

        targetDirection.Normalize();
        float forwardProgress = Vector3.Dot(planarDelta, targetDirection);
        float expectedProgress = targetVelocity.magnitude * Time.deltaTime;
        float minimumProgress = Mathf.Max(0.012f, expectedProgress * Mathf.Clamp01(stuckMinimumProgressRatio));
        bool touchingSide = (controller.collisionFlags & CollisionFlags.Sides) != 0;

        if (forwardProgress >= minimumProgress && (!touchingSide || planarDelta.magnitude >= minimumProgress * 1.6f))
        {
            stuckTimer = 0f;
            repeatedStuckCount = 0;
            return;
        }

        stuckTimer += Time.deltaTime * (touchingSide ? Mathf.Max(1f, wedgedContactAcceleration) : 1f);
        float effectiveStuckDelay = isChasing ? stuckRetargetDelay * 0.8f : stuckRetargetDelay;

        if (stuckTimer < effectiveStuckDelay)
        {
            return;
        }

        stuckTimer = 0f;
        repeatedStuckCount++;
        ClearCurrentNavMeshPath();

        Vector3 fallbackDirection = Vector3.Cross(Vector3.up, targetVelocity.normalized) * lastAvoidanceSign;
        if (fallbackDirection.sqrMagnitude < 0.001f)
        {
            fallbackDirection = transform.right * lastAvoidanceSign;
        }

        if (TryBeginPhysicalDetour(targetVelocity.normalized, out Vector3 detourDirection))
        {
            fallbackDirection = detourDirection;
            lastAvoidanceSign = Vector3.Dot(detourDirection, transform.right) >= 0f ? 1 : -1;
            avoidanceLockDirection = detourDirection;
            avoidanceLockTimer = avoidanceLockDuration * 1.5f;
            Debug.Log("L'Entite contourne physiquement un obstacle par le chemin libre.");
            return;
        }

        if (TryGetDepenetrationEscapeDirection(out Vector3 depenetrationDirection))
        {
            fallbackDirection = depenetrationDirection;
        }

        lastAvoidanceSign *= -1;
        avoidanceLockDirection = fallbackDirection.normalized;
        avoidanceLockTimer = avoidanceLockDuration * 1.5f;

        if (isChasing)
        {
            investigationTarget = transform.position + fallbackDirection.normalized * corridorRecoveryDistance;
            investigationTarget.y = transform.position.y;
            hasInvestigationTarget = true;
            searchRetargetTimer = 0.3f;
            return;
        }

        if (isInvestigating && CanInvestigateMemory())
        {
            PickNextSearchTarget();
            return;
        }

        AdvancePatrolPoint();
    }

    private bool TryBeginPhysicalDetour(Vector3 desiredDirection, out Vector3 detourDirection)
    {
        detourDirection = Vector3.zero;
        if (!runtimeNavMeshReady || controller == null || desiredDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        desiredDirection.y = 0f;
        desiredDirection.Normalize();
        float preferredSide = lastAvoidanceSign >= 0 ? 1f : -1f;
        float[] angles =
        {
            65f * preferredSide,
            -65f * preferredSide,
            95f * preferredSide,
            -95f * preferredSide,
            130f * preferredSide,
            -130f * preferredSide,
            180f
        };
        float[] distances =
        {
            Mathf.Max(corridorRecoveryDistance, physicalDetourDistance * 0.65f),
            Mathf.Max(corridorRecoveryDistance, physicalDetourDistance),
            Mathf.Max(corridorRecoveryDistance, physicalDetourDistance * 1.25f)
        };

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        Vector3 bestPoint = transform.position;
        float bestScore = float.NegativeInfinity;
        bool foundPoint = false;
        NavMeshPath detourPath = new NavMeshPath();

        for (int distanceIndex = 0; distanceIndex < distances.Length; distanceIndex++)
        {
            for (int angleIndex = 0; angleIndex < angles.Length; angleIndex++)
            {
                Vector3 direction = Quaternion.Euler(0f, angles[angleIndex], 0f) * desiredDirection;
                Vector3 probe = transform.position + direction.normalized * distances[distanceIndex];
                if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    continue;
                }

                Vector3 candidate = hit.position;
                if (Mathf.Abs(candidate.y - transform.position.y) > physicalDetourVerticalTolerance)
                {
                    continue;
                }

                if (!HasBodySpaceAt(candidate))
                {
                    continue;
                }

                detourPath.ClearCorners();
                if (!NavMesh.CalculatePath(startHit.position, candidate, NavMesh.AllAreas, detourPath) ||
                    detourPath.status != NavMeshPathStatus.PathComplete ||
                    detourPath.corners == null ||
                    detourPath.corners.Length < 2)
                {
                    continue;
                }

                float pathLength = GetPathLength(detourPath);
                if (pathLength < corridorRecoveryDistance * 0.7f)
                {
                    continue;
                }

                float sidePreference = Mathf.Sign(Vector3.Dot(direction, transform.right)) == Mathf.Sign(preferredSide) ? 0.8f : 0f;
                float targetProgress = Vector3.Dot(direction.normalized, desiredDirection);
                float candidateDistance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(candidate));
                float score = sidePreference + targetProgress * 0.35f + candidateDistance * 0.12f - pathLength * 0.04f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = candidate;
                    foundPoint = true;
                }
            }
        }

        if (!foundPoint)
        {
            return false;
        }

        stuckEscapeTarget = bestPoint;
        stuckEscapeTimer = Mathf.Max(stuckEscapeDuration, physicalDetourDuration);
        detourDirection = GetFlatDirectionTo(bestPoint);
        ClearCurrentNavMeshPath();
        return true;
    }

    private float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return length;
    }

    private float GetPuzzleAlertDurationFor(Vector3 worldPosition, float requestedDuration)
    {
        float travelDistance = EstimateTravelDistanceTo(worldPosition);
        float travelSpeed = Mathf.Max(GetCurrentInvestigationSpeed(), 0.1f);
        float travelDuration = travelDistance / travelSpeed + puzzleAlertTravelBuffer;
        float minimumDuration = Mathf.Max(requestedDuration, puzzleAlertMinimumTravelDuration);
        float maximumDuration = Mathf.Max(minimumDuration, puzzleAlertMaximumTravelDuration);

        return Mathf.Clamp(Mathf.Max(minimumDuration, travelDuration), minimumDuration, maximumDuration);
    }

    private float EstimateTravelDistanceTo(Vector3 worldPosition)
    {
        float planarDistance = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(worldPosition));
        if (!runtimeNavMeshReady)
        {
            return planarDistance;
        }

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, navMeshSampleDistance, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(worldPosition, out NavMeshHit targetHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return planarDistance;
        }

        NavMeshPath estimatedPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, estimatedPath) ||
            estimatedPath.status != NavMeshPathStatus.PathComplete)
        {
            return planarDistance;
        }

        return Mathf.Max(planarDistance, GetPathLength(estimatedPath));
    }

    private bool IsVisibleFromPlayer()
    {
        if (playerTarget == null)
        {
            return false;
        }

        Vector3 playerEye = playerTarget.position + Vector3.up * 1.5f;
        Vector3 monsterCenter = transform.position + Vector3.up * GetNavigationLineCheckHeight();
        Vector3 toMonster = monsterCenter - playerEye;
        float distance = toMonster.magnitude;
        if (distance <= 0.05f)
        {
            return true;
        }

        Vector3 playerForward = playerTarget.forward;
        Camera playerCamera = playerTarget.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
        {
            playerForward = playerCamera.transform.forward;
            playerEye = playerCamera.transform.position;
            toMonster = monsterCenter - playerEye;
            distance = toMonster.magnitude;
        }

        if (Vector3.Angle(playerForward, toMonster.normalized) > 70f)
        {
            return false;
        }

        if (Physics.Linecast(playerEye, monsterCenter, out RaycastHit hit, sightBlockerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform != null && hit.transform.IsChildOf(transform);
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private Transform FindAnimatedVisual()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "EntityPrisonVisual")
            {
                continue;
            }

            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                return child;
            }
        }

        return null;
    }

    private void ConfigureScaryNurseVisual()
    {
        if (!useScaryNurseVisual)
        {
            return;
        }

        ApplyScaryNurseAnimationNames();

        Transform existingNurse = FindNurseVisual();
        if (existingNurse != null)
        {
            DisablePreviousMonsterVisuals();
            EnsureNurseAnimator(existingNurse.gameObject);
            animatedVisual = existingNurse;
            return;
        }

#if UNITY_EDITOR
        GameObject nurseObject = CreateNurseVisual(NurseRuntimeVisualName, true);
        if (nurseObject == null)
        {
            return;
        }

        animatedVisual = nurseObject.transform;
#endif
    }

    private void ApplyScaryNurseAnimationNames()
    {
        idleClipResourceName = "zombie idle";
        walkClipResourceName = "zombie walk";
        runClipResourceName = "zombie run";
        attackClipResourceName = "zombie attack";
        if (string.IsNullOrWhiteSpace(turnClipResourceName) || NameMatches(turnClipResourceName, "Zombie Turn"))
        {
            turnClipResourceName = string.Empty;
        }
    }

    private Transform FindNurseVisual()
    {
        Transform sceneVisual = transform.Find(NurseSceneVisualName);
        if (sceneVisual != null)
        {
            return sceneVisual;
        }

        Transform runtimeVisual = transform.Find(NurseRuntimeVisualName);
        if (runtimeVisual != null)
        {
            return runtimeVisual;
        }

        return null;
    }

#if UNITY_EDITOR
    private void EnsureNurseSceneVisual()
    {
        ApplyScaryNurseAnimationNames();

        Transform existingNurse = transform.Find(NurseSceneVisualName);
        Transform runtimeNurse = transform.Find(NurseRuntimeVisualName);
        if (existingNurse == null && runtimeNurse != null)
        {
            runtimeNurse.name = NurseSceneVisualName;
            existingNurse = runtimeNurse;
        }

        GameObject nurseObject = existingNurse != null
            ? existingNurse.gameObject
            : CreateNurseVisual(NurseSceneVisualName, false);

        if (nurseObject == null)
        {
            return;
        }

        ConfigureNurseVisualObject(nurseObject);
        DisablePreviousMonsterVisuals();
        animatedVisual = nurseObject.transform;
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private GameObject CreateNurseVisual(string visualName, bool runtimeInstance)
    {
        EnsureHumanoidImporter(nurseModelAssetPath, false);
        GameObject nursePrefab = LoadAssetAtPathWithImport<GameObject>(nurseModelAssetPath);
        if (nursePrefab == null)
        {
            Debug.LogWarning("Nurse FBX introuvable: " + nurseModelAssetPath);
            return null;
        }

        DisablePreviousMonsterVisuals();

        GameObject nurseObject = runtimeInstance
            ? Instantiate(nursePrefab, transform)
            : PrefabUtility.InstantiatePrefab(nursePrefab, transform) as GameObject;
        if (nurseObject == null)
        {
            nurseObject = Instantiate(nursePrefab, transform);
        }

        nurseObject.name = visualName;
        if (!runtimeInstance)
        {
            Undo.RegisterCreatedObjectUndo(nurseObject, "Add Nurse Visual");
        }

        ConfigureNurseVisualObject(nurseObject);
        return nurseObject;
    }

    private void ConfigureNurseVisualObject(GameObject nurseObject)
    {
        if (nurseObject == null)
        {
            return;
        }

        nurseObject.transform.localPosition = nurseVisualLocalPosition;
        nurseObject.transform.localRotation = Quaternion.Euler(nurseVisualLocalEulerAngles);
        nurseObject.transform.localScale = Vector3.one;
        ScaleVisualToTargetHeight(nurseObject.transform, nurseTargetVisualHeight);
        AssignNurseTextures(nurseObject);
        EnsureNurseAnimator(nurseObject);
    }
#endif

    private void DisablePreviousMonsterVisuals()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null ||
                child.name == "EntityPrisonVisual" ||
                child.name == NurseSceneVisualName ||
                child.name == NurseRuntimeVisualName)
            {
                continue;
            }

            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void ScaleVisualToTargetHeight(Transform visual, float targetHeight)
    {
        if (visual == null || targetHeight <= 0f || !TryGetRendererBounds(visual, out Bounds bounds) || bounds.size.y <= 0.001f)
        {
            return;
        }

        float scale = targetHeight / bounds.size.y;
        visual.localScale *= scale;
    }

    private bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void AssignNurseTextures(GameObject nurseObject)
    {
#if UNITY_EDITOR
        if (nurseObject == null)
        {
            return;
        }

        Texture2D bodyTexture = LoadAssetAtPathWithImport<Texture2D>("Assets/mixamo/Scary Zombie Pack/body.png");
        Texture2D headTexture = LoadAssetAtPathWithImport<Texture2D>("Assets/mixamo/Scary Zombie Pack/head.png");
        Renderer[] renderers = nurseObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterials == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                {
                    continue;
                }

                string materialName = NormalizeAnimationName(material.name);
                Texture2D texture = materialName.Contains("head") ? headTexture : bodyTexture;
                if (texture != null && material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
                else if (texture != null && material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }
            }
        }
#endif
    }

    private void EnsureNurseAnimator(GameObject nurseObject)
    {
#if UNITY_EDITOR
        if (nurseObject == null)
        {
            return;
        }

        Animator nurseAnimator = nurseObject.GetComponent<Animator>();
        if (nurseAnimator == null)
        {
            nurseAnimator = nurseObject.GetComponentInChildren<Animator>(true);
        }

        if (nurseAnimator == null)
        {
            nurseAnimator = nurseObject.AddComponent<Animator>();
        }

        Avatar nurseAvatar = LoadAvatarFromModel(nurseModelAssetPath);
        if (nurseAvatar != null && nurseAvatar.isValid && nurseAvatar.isHuman)
        {
            nurseAnimator.avatar = nurseAvatar;
        }

        nurseAnimator.enabled = true;
        nurseAnimator.runtimeAnimatorController = null;
        nurseAnimator.applyRootMotion = false;
        nurseAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
#endif
    }

#if UNITY_EDITOR
    private Avatar LoadAvatarFromModel(string modelPath)
    {
        Object[] assets = LoadAllAssetsAtPathWithImport(modelPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar avatar && avatar.isValid && avatar.isHuman)
            {
                return avatar;
            }
        }

        return null;
    }
#endif

    private Transform[] FindPatrolPointsInScene()
    {
        GameObject routeRoot = GameObject.Find("MonsterPatrolRoute");
        if (routeRoot == null)
        {
            return null;
        }

        Transform[] points = new Transform[routeRoot.transform.childCount];
        for (int i = 0; i < routeRoot.transform.childCount; i++)
        {
            points[i] = routeRoot.transform.GetChild(i);
        }

        return points;
    }

    private Vector3[] BuildNavigationRoute()
    {
        List<Vector3> route = new List<Vector3>();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    AddRoutePointIfUseful(route, patrolPoints[i].position, patrolStoppingDistance);
                }
            }
        }
        else
        {
            for (int i = 0; i < DefaultHospitalRoute.Length; i++)
            {
                AddRoutePointIfUseful(route, DefaultHospitalRoute[i], patrolStoppingDistance);
            }
        }

        AddGeneratedExplorationPoints(route);

        if (route.Count == 0)
        {
            route.Add(transform.position);
        }

        return OrderRouteByNearestNeighbor(route);
    }

    private void AddGeneratedExplorationPoints(List<Vector3> route)
    {
        if (!expandPatrolRouteFromNavMesh || !runtimeNavMeshReady || maxGeneratedExplorationPoints <= 0)
        {
            return;
        }

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null ||
            triangulation.indices == null ||
            triangulation.vertices.Length == 0 ||
            triangulation.indices.Length < 3)
        {
            return;
        }

        int triangleCount = triangulation.indices.Length / 3;
        int addedCount = 0;
        float minimumSpacing = Mathf.Max(explorationPointSpacing, patrolStoppingDistance * 2f);
        int stride = Mathf.Max(1, triangleCount / Mathf.Max(1, maxGeneratedExplorationPoints));

        for (int offset = 0; offset < stride && addedCount < maxGeneratedExplorationPoints; offset++)
        {
            for (int triangleIndex = offset; triangleIndex < triangleCount && addedCount < maxGeneratedExplorationPoints; triangleIndex += stride)
            {
                int index = triangleIndex * 3;
                Vector3 candidate = (
                    triangulation.vertices[triangulation.indices[index]] +
                    triangulation.vertices[triangulation.indices[index + 1]] +
                    triangulation.vertices[triangulation.indices[index + 2]]
                ) / 3f;

                int previousCount = route.Count;
                AddRoutePointIfUseful(route, candidate, minimumSpacing);
                if (route.Count > previousCount)
                {
                    addedCount++;
                }
            }
        }
    }

    private void AddRoutePointIfUseful(List<Vector3> route, Vector3 candidate, float minimumSpacing)
    {
        if (runtimeNavMeshReady &&
            NavMesh.SamplePosition(candidate, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            candidate = navMeshHit.position;
        }

        if (!HasBodySpaceAt(candidate))
        {
            return;
        }

        float minimumSqrDistance = minimumSpacing * minimumSpacing;
        for (int i = 0; i < route.Count; i++)
        {
            if (Vector3.SqrMagnitude(GetPlanarPosition(route[i]) - GetPlanarPosition(candidate)) < minimumSqrDistance)
            {
                return;
            }
        }

        route.Add(candidate);
    }

    private Vector3[] OrderRouteByNearestNeighbor(List<Vector3> route)
    {
        Vector3[] orderedRoute = new Vector3[route.Count];
        bool[] used = new bool[route.Count];
        Vector3 currentPosition = transform.position;

        for (int orderedIndex = 0; orderedIndex < orderedRoute.Length; orderedIndex++)
        {
            int bestIndex = -1;
            float bestDistance = float.PositiveInfinity;

            for (int candidateIndex = 0; candidateIndex < route.Count; candidateIndex++)
            {
                if (used[candidateIndex])
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(
                    GetPlanarPosition(route[candidateIndex]) - GetPlanarPosition(currentPosition)
                );

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = candidateIndex;
                }
            }

            if (bestIndex < 0)
            {
                break;
            }

            used[bestIndex] = true;
            orderedRoute[orderedIndex] = route[bestIndex];
            currentPosition = route[bestIndex];
        }

        return orderedRoute;
    }

    private void InitializeRouteVisitMemory()
    {
        int routeLength = navigationRoute != null ? navigationRoute.Length : 0;
        routeLastVisitTimes = new float[routeLength];
        routeVisitCounts = new int[routeLength];

        for (int i = 0; i < routeLastVisitTimes.Length; i++)
        {
            routeLastVisitTimes[i] = -routeRevisitDelay;
        }
    }

    private Vector3 GetIntelligentVelocityTo(Vector3 worldTarget, float speed, bool urgent, float targetStoppingDistance)
    {
        float distanceToTarget = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(worldTarget));
        if (distanceToTarget <= targetStoppingDistance)
        {
            return Vector3.zero;
        }

        if (stuckEscapeTimer > 0f &&
            Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(stuckEscapeTarget)) > routeWaypointReachDistance * 0.65f)
        {
            if (TryGetNavMeshVelocityTo(stuckEscapeTarget, speed, routeWaypointReachDistance * 0.45f, out Vector3 detourVelocity))
            {
                return detourVelocity;
            }

            return GetFlatDirectionTo(stuckEscapeTarget) * speed;
        }

        if (TryGetNavMeshVelocityTo(worldTarget, speed, targetStoppingDistance, out Vector3 navMeshVelocity))
        {
            return navMeshVelocity;
        }

        if (!ShouldRouteAroundMap(worldTarget, urgent, distanceToTarget) ||
            !TryGetRouteWaypoint(worldTarget, out Vector3 waypoint))
        {
            if (!HasClearNavigationLine(worldTarget) && TryFindOpenEscapePoint(worldTarget, out Vector3 escapePoint))
            {
                if (TryGetNavMeshVelocityTo(escapePoint, speed, routeWaypointReachDistance * 0.45f, out Vector3 escapeVelocity))
                {
                    return escapeVelocity;
                }

                return GetFlatDirectionTo(escapePoint) * speed;
            }

            return GetFlatDirectionTo(worldTarget) * speed;
        }

        return GetFlatDirectionTo(waypoint) * speed;
    }

    private bool TryGetNavMeshVelocityTo(Vector3 worldTarget, float speed, float targetStoppingDistance, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (!runtimeNavMeshReady || currentNavMeshPath == null)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMesh.SamplePosition(worldTarget, out NavMeshHit targetHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }

        float targetDelta = Vector3.Distance(GetPlanarPosition(currentNavMeshTarget), GetPlanarPosition(targetHit.position));
        bool shouldRepath =
            navMeshRepathTimer <= 0f ||
            currentNavMeshPath.status != NavMeshPathStatus.PathComplete ||
            currentNavMeshPath.corners == null ||
            currentNavMeshPath.corners.Length == 0 ||
            targetDelta > 0.8f;

        if (shouldRepath)
        {
            if (!NavMesh.CalculatePath(startHit.position, targetHit.position, NavMesh.AllAreas, currentNavMeshPath) ||
                currentNavMeshPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            currentNavMeshTarget = targetHit.position;
            navMeshRepathTimer = navMeshRepathInterval;
        }

        Vector3 waypoint = targetHit.position;
        Vector3[] corners = currentNavMeshPath.corners;
        if (corners != null && corners.Length > 1)
        {
            for (int i = 1; i < corners.Length; i++)
            {
                float distanceToCorner = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(corners[i]));
                if (distanceToCorner > navMeshWaypointReachDistance)
                {
                    waypoint = corners[i];
                    break;
                }
            }
        }

        float distanceToTarget = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(targetHit.position));
        if (distanceToTarget <= targetStoppingDistance)
        {
            return true;
        }

        Vector3 direction = GetFlatDirectionTo(waypoint);
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = GetFlatDirectionTo(targetHit.position);
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        velocity = direction * speed;
        return true;
    }

    private bool ShouldRouteAroundMap(Vector3 worldTarget, bool urgent, float distanceToTarget)
    {
        if (navigationRoute == null || navigationRoute.Length == 0)
        {
            return false;
        }

        if (HasClearNavigationLine(worldTarget))
        {
            return false;
        }

        if (intelligenceLevel == IntelligenceLevel.Easy)
        {
            return urgent || distanceToTarget >= easyBlockedRouteDistance;
        }

        return true;
    }

    private bool TryGetRouteWaypoint(Vector3 worldTarget, out Vector3 waypoint)
    {
        waypoint = worldTarget;

        if (navigationRoute == null || navigationRoute.Length == 0)
        {
            return false;
        }

        int currentIndex = FindNearestRouteIndex(transform.position);
        int targetIndex = FindNearestRouteIndex(worldTarget);
        if (currentIndex < 0 || targetIndex < 0)
        {
            return false;
        }

        float distanceToCurrent = Vector3.Distance(
            GetPlanarPosition(transform.position),
            GetPlanarPosition(navigationRoute[currentIndex])
        );

        if (distanceToCurrent > routeWaypointReachDistance && !HasClearNavigationLine(navigationRoute[currentIndex]))
        {
            return TryGetVisibleRouteWaypoint(worldTarget, out waypoint);
        }

        if (currentIndex == targetIndex)
        {
            waypoint = distanceToCurrent > routeWaypointReachDistance
                ? navigationRoute[currentIndex]
                : worldTarget;
            return true;
        }

        int direction = GetShortestRouteDirection(currentIndex, targetIndex);
        int waypointIndex = distanceToCurrent <= routeWaypointReachDistance
            ? WrapRouteIndex(currentIndex + direction)
            : currentIndex;

        waypoint = navigationRoute[waypointIndex];
        return true;
    }

    private bool TryGetVisibleRouteWaypoint(Vector3 worldTarget, out Vector3 waypoint)
    {
        waypoint = worldTarget;

        if (navigationRoute == null || navigationRoute.Length == 0)
        {
            return false;
        }

        float bestScore = float.PositiveInfinity;
        bool foundWaypoint = false;
        Vector3 planarSelf = GetPlanarPosition(transform.position);
        Vector3 planarTarget = GetPlanarPosition(worldTarget);

        for (int i = 0; i < navigationRoute.Length; i++)
        {
            Vector3 candidate = navigationRoute[i];
            if (!HasClearNavigationLine(candidate))
            {
                continue;
            }

            float distanceFromSelf = Vector3.Distance(planarSelf, GetPlanarPosition(candidate));
            float distanceToTarget = Vector3.Distance(GetPlanarPosition(candidate), planarTarget);
            float targetWeight = intelligenceLevel == IntelligenceLevel.Hard ? 1f : 1.25f;
            float score = distanceFromSelf * 0.35f + distanceToTarget * targetWeight;

            if (score < bestScore)
            {
                bestScore = score;
                waypoint = candidate;
                foundWaypoint = true;
            }
        }

        return foundWaypoint;
    }

    private bool TryFindOpenEscapePoint(Vector3 worldTarget, out Vector3 escapePoint)
    {
        escapePoint = transform.position;
        Vector3 desiredDirection = GetFlatDirectionTo(worldTarget);
        if (desiredDirection.sqrMagnitude <= 0.001f)
        {
            desiredDirection = transform.forward;
        }

        Vector3 origin = GetNavigationProbeOrigin();
        float[] probeAngles =
        {
            0f,
            -35f,
            35f,
            -70f,
            70f,
            -110f,
            110f,
            180f,
            160f,
            -160f
        };

        float bestScore = float.NegativeInfinity;
        bool foundPoint = false;

        for (int i = 0; i < probeAngles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, probeAngles[i], 0f) * desiredDirection;
            float clearance = GetDirectionClearance(origin, direction);
            if (clearance < obstacleCheckRadius * 2.2f)
            {
                continue;
            }

            Vector3 candidate = transform.position + direction.normalized * Mathf.Min(clearance, stuckEscapeProbeDistance);
            if (runtimeNavMeshReady &&
                NavMesh.SamplePosition(candidate, out NavMeshHit navMeshHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                candidate = navMeshHit.position;
            }

            if (!HasBodySpaceAt(candidate))
            {
                continue;
            }

            float targetProgress = Vector3.Dot(desiredDirection.normalized, direction.normalized);
            float score = clearance + targetProgress * (intelligenceLevel == IntelligenceLevel.Hard ? 1.2f : 0.65f);

            if (score > bestScore)
            {
                bestScore = score;
                escapePoint = candidate;
                foundPoint = true;
            }
        }

        return foundPoint;
    }

    private int FindNearestRouteIndex(Vector3 worldPosition)
    {
        if (navigationRoute == null || navigationRoute.Length == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestScore = float.PositiveInfinity;
        Vector3 planarPosition = GetPlanarPosition(worldPosition);

        for (int i = 0; i < navigationRoute.Length; i++)
        {
            Vector3 planarRoutePoint = GetPlanarPosition(navigationRoute[i]);
            float score = Vector3.SqrMagnitude(planarRoutePoint - planarPosition);
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private int GetShortestRouteDirection(int currentIndex, int targetIndex)
    {
        int routeLength = navigationRoute != null ? navigationRoute.Length : 0;
        if (routeLength <= 1)
        {
            return 1;
        }

        int forwardSteps = (targetIndex - currentIndex + routeLength) % routeLength;
        int backwardSteps = (currentIndex - targetIndex + routeLength) % routeLength;
        return forwardSteps <= backwardSteps ? 1 : -1;
    }

    private int WrapRouteIndex(int index)
    {
        int routeLength = navigationRoute != null ? navigationRoute.Length : 0;
        if (routeLength <= 0)
        {
            return 0;
        }

        return (index % routeLength + routeLength) % routeLength;
    }

    private bool HasClearNavigationLine(Vector3 worldTarget)
    {
        Vector3 start = transform.position + Vector3.up * GetNavigationLineCheckHeight();
        Vector3 end = worldTarget + Vector3.up * GetNavigationLineCheckHeight();
        Vector3 offset = end - start;
        float distance = offset.magnitude;

        if (distance <= 0.05f)
        {
            return true;
        }

        Vector3 direction = offset / distance;
        if (!Physics.SphereCast(
            start,
            routeLineOfSightRadius,
            direction,
            out RaycastHit hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return !IsBlockingHit(hit);
    }

    private float GetNavigationLineCheckHeight()
    {
        if (controller == null)
        {
            return routeLineOfSightHeight;
        }

        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
        return Mathf.Max(routeLineOfSightHeight, controller.height * scaleY * 0.45f);
    }

    private void AdvancePatrolPoint()
    {
        if (navigationRoute != null && navigationRoute.Length > 0)
        {
            patrolPointIndex = prioritizeUnvisitedExploration
                ? FindNextExplorationRouteIndex()
                : (patrolPointIndex + 1) % navigationRoute.Length;
            patrolWaitTimer = 0f;
            roamRetargetTimer = 0f;
            hasRoamTarget = false;
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            patrolPointIndex = (patrolPointIndex + 1) % patrolPoints.Length;
        }

        patrolWaitTimer = 0f;
        roamRetargetTimer = 0f;
        hasRoamTarget = false;
    }

    private void MarkCurrentRoutePointVisited()
    {
        if (navigationRoute == null ||
            routeLastVisitTimes == null ||
            routeVisitCounts == null ||
            patrolPointIndex < 0 ||
            patrolPointIndex >= navigationRoute.Length ||
            patrolPointIndex >= routeLastVisitTimes.Length ||
            patrolPointIndex >= routeVisitCounts.Length)
        {
            return;
        }

        routeLastVisitTimes[patrolPointIndex] = Time.time;
        routeVisitCounts[patrolPointIndex]++;
    }

    private int FindNextExplorationRouteIndex()
    {
        if (navigationRoute == null || navigationRoute.Length == 0)
        {
            return 0;
        }

        if (routeLastVisitTimes == null ||
            routeVisitCounts == null ||
            routeLastVisitTimes.Length != navigationRoute.Length ||
            routeVisitCounts.Length != navigationRoute.Length)
        {
            InitializeRouteVisitMemory();
        }

        int bestIndex = WrapRouteIndex(patrolPointIndex + 1);
        float bestScore = float.NegativeInfinity;
        Vector3 planarSelf = GetPlanarPosition(transform.position);

        for (int i = 0; i < navigationRoute.Length; i++)
        {
            if (i == patrolPointIndex && navigationRoute.Length > 1)
            {
                continue;
            }

            float distance = Vector3.Distance(planarSelf, GetPlanarPosition(navigationRoute[i]));
            float timeSinceVisit = Time.time - routeLastVisitTimes[i];
            float unvisitedBonus = routeVisitCounts[i] == 0 ? routeRevisitDelay * 2f : 0f;
            float lineBonus = HasClearNavigationLine(navigationRoute[i]) ? 3f : 0f;
            float score = timeSinceVisit + unvisitedBonus + lineBonus - distance * 0.65f - routeVisitCounts[i] * 5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ClearCurrentNavMeshPath()
    {
        if (currentNavMeshPath != null)
        {
            currentNavMeshPath.ClearCorners();
        }

        currentNavMeshTarget = Vector3.positiveInfinity;
        navMeshRepathTimer = 0f;
    }

    private void UpdateVisualMotion()
    {
        if (animatedVisual == null)
        {
            animatedVisual = FindAnimatedVisual();
            if (animatedVisual == null)
            {
                return;
            }

            visualBaseLocalPosition = animatedVisual.localPosition;
            visualBaseLocalRotation = animatedVisual.localRotation;
            AlignAnimatedVisualToGround(true);
        }

        Vector3 planarVelocity = controller.velocity;
        planarVelocity.y = 0f;

        float currentSpeed = planarVelocity.magnitude;
        float speedRatio = moveSpeed > 0f ? Mathf.Clamp01(currentSpeed / moveSpeed) : 0f;
        bool isMoving = speedRatio > 0.05f;

        Vector3 targetPosition = visualBaseLocalPosition;
        Quaternion targetRotation = visualBaseLocalRotation;

        if (isMoving)
        {
            float bobAmplitude = Mathf.Lerp(walkBobAmplitude, runBobAmplitude, speedRatio);
            float bobFrequency = Mathf.Lerp(walkBobFrequency, runBobFrequency, speedRatio);
            float rollAngle = Mathf.Sin(visualCycle) * maxRollAngle * speedRatio;
            float pitchAngle = Mathf.Abs(Mathf.Cos(visualCycle)) * maxPitchAngle * speedRatio;
            float bobOffset = Mathf.Abs(Mathf.Sin(visualCycle)) * bobAmplitude;

            visualCycle += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
            targetPosition += Vector3.up * bobOffset;
            targetRotation = visualBaseLocalRotation * Quaternion.Euler(pitchAngle, 0f, rollAngle);
        }
        else
        {
            visualCycle = Mathf.Lerp(visualCycle, 0f, Time.deltaTime * visualSmoothing);
        }

        animatedVisual.localPosition = Vector3.Lerp(
            animatedVisual.localPosition,
            targetPosition,
            Time.deltaTime * visualSmoothing
        );
        animatedVisual.localRotation = Quaternion.Slerp(
            animatedVisual.localRotation,
            targetRotation,
            Time.deltaTime * visualSmoothing
        );

        AlignAnimatedVisualToGround(false);
    }

    private void AlignAnimatedVisualToGround(bool immediate)
    {
        if (!alignVisualToGround || animatedVisual == null || !TryGetVisualRendererBounds(out Bounds bounds))
        {
            return;
        }

        float deltaWorldY = GetVisualGroundY() - bounds.min.y;
        if (Mathf.Abs(deltaWorldY) <= 0.002f)
        {
            return;
        }

        float parentScaleY = 1f;
        if (animatedVisual.parent != null)
        {
            parentScaleY = animatedVisual.parent.lossyScale.y;
        }

        if (Mathf.Abs(parentScaleY) <= 0.0001f)
        {
            parentScaleY = 1f;
        }

        Vector3 targetBasePosition = visualBaseLocalPosition;
        targetBasePosition.y += deltaWorldY / parentScaleY;

        visualBaseLocalPosition = immediate
            ? targetBasePosition
            : Vector3.Lerp(visualBaseLocalPosition, targetBasePosition, Time.deltaTime * visualGroundingSmoothing);

        if (animationSystemReady || immediate)
        {
            animatedVisual.localPosition = visualBaseLocalPosition;
        }
    }

    private float GetVisualGroundY()
    {
        if (controller == null)
        {
            return transform.position.y + visualGroundOffset;
        }

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY <= 0.0001f)
        {
            scaleY = 1f;
        }

        return transform.position.y +
            controller.center.y * scaleY -
            controller.height * 0.5f * scaleY +
            visualGroundOffset;
    }

    private bool TryGetVisualRendererBounds(out Bounds bounds)
    {
        if (animatedVisual == null)
        {
            bounds = default;
            return false;
        }

        Renderer[] renderers = animatedVisual.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer.transform.name == "EntityPrisonVisual")
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void TryInitializeMixamoAnimations()
    {
        if (animatedVisual == null)
        {
            animatedVisual = FindAnimatedVisual();
        }

        if (animatedVisual == null)
        {
            return;
        }

        visualAnimator = animatedVisual.GetComponent<Animator>();
        if (visualAnimator == null)
        {
            visualAnimator = animatedVisual.GetComponentInChildren<Animator>(true);
        }

        if (visualAnimator == null || visualAnimator.avatar == null || !visualAnimator.avatar.isValid)
        {
            Debug.LogWarning("Animator humanoide introuvable sur le visuel du zombie. Verifie que le FBX du monstre est importe en Humanoid.");
            return;
        }

        visualAnimator.enabled = true;
        visualAnimator.applyRootMotion = false;
        visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        idleAnimationClip = LoadMixamoClip(idleClipResourceName, "idle");
        walkAnimationClip = LoadMixamoClip(walkClipResourceName, "walk", "walking");
        runAnimationClip = LoadMixamoClip(runClipResourceName, "run", "running");
        turnAnimationClip = LoadMixamoClip(turnClipResourceName, "turn");
        attackAnimationClip = LoadMixamoClip(attackClipResourceName, "attack");

        if (idleAnimationClip == null || walkAnimationClip == null || runAnimationClip == null)
        {
            Debug.LogWarning("Clips Mixamo incomplets. Verifie Assets/mixamo/Scary Zombie Pack et les noms de clips dans BasicMonsterController.");
            return;
        }

        animationGraph = PlayableGraph.Create(name + "_MixamoGraph");
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        animationMixer = AnimationMixerPlayable.Create(animationGraph, 2);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "ZombieAnimation", visualAnimator);
        output.SetSourcePlayable(animationMixer);

        animationGraph.Play();
        animationSystemReady = true;
        PlayClipImmediate(idleAnimationClip, true);
    }

    private AnimationClip LoadMixamoClip(string clipResourceName, params string[] fallbackKeywords)
    {
#if UNITY_EDITOR
        AnimationClip editorClip = LoadAnimationClipFromAssetFolder(clipResourceName, fallbackKeywords);
        if (editorClip != null)
        {
            return editorClip;
        }
#endif

        AnimationClip[] clips = string.IsNullOrWhiteSpace(clipResourceName)
            ? System.Array.Empty<AnimationClip>()
            : Resources.LoadAll<AnimationClip>(mixamoResourcesFolder + "/" + clipResourceName);

        AnimationClip clipFromExactResource = FindUsableClip(clips);
        if (clipFromExactResource != null)
        {
            return clipFromExactResource;
        }

        clips = Resources.LoadAll<AnimationClip>(mixamoResourcesFolder);
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (!IsUsableAnimationClip(clip))
            {
                continue;
            }

            if (NameMatches(clip.name, clipResourceName) || NameMatches(clip.name, fallbackKeywords))
            {
                return clip;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private AnimationClip LoadAnimationClipFromAssetFolder(string clipResourceName, params string[] fallbackKeywords)
    {
        string animationFolder = NormalizeAssetPath(scaryZombieAnimationAssetFolder);
        if (string.IsNullOrWhiteSpace(animationFolder) || !Directory.Exists(animationFolder))
        {
            return null;
        }

        string[] fbxPaths = Directory.GetFiles(animationFolder, "*.fbx", SearchOption.TopDirectoryOnly);
        AnimationClip clip = FindClipInFbxFiles(fbxPaths, clipResourceName, true);
        if (clip != null)
        {
            return clip;
        }

        clip = FindClipInFbxFiles(fbxPaths, clipResourceName, false);
        if (clip != null)
        {
            return clip;
        }

        if (fallbackKeywords != null)
        {
            for (int i = 0; i < fallbackKeywords.Length; i++)
            {
                clip = FindClipInFbxFiles(fbxPaths, fallbackKeywords[i], false);
                if (clip != null)
                {
                    return clip;
                }
            }
        }

        return null;
    }

    private AnimationClip FindClipInFbxFiles(string[] fbxPaths, string expectedName, bool exactFileNameOnly)
    {
        if (fbxPaths == null || string.IsNullOrWhiteSpace(expectedName))
        {
            return null;
        }

        string normalizedExpected = NormalizeAnimationName(expectedName);
        for (int i = 0; i < fbxPaths.Length; i++)
        {
            string path = NormalizeAssetPath(fbxPaths[i]);
            string fileName = NormalizeAnimationName(Path.GetFileNameWithoutExtension(path));
            bool fileMatches = exactFileNameOnly
                ? fileName == normalizedExpected
                : fileName == normalizedExpected || fileName.Contains(normalizedExpected);
            if (!fileMatches)
            {
                continue;
            }

            EnsureHumanoidImporter(path, true);
            Object[] assets = LoadAllAssetsAtPathWithImport(path);
            for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
            {
                if (assets[assetIndex] is AnimationClip animationClip && IsUsableAnimationClip(animationClip))
                {
                    return animationClip;
                }
            }
        }

        return null;
    }

    private T LoadAssetAtPathWithImport<T>(string path) where T : Object
    {
        string normalizedPath = NormalizeAssetPath(path);
        T asset = AssetDatabase.LoadAssetAtPath<T>(normalizedPath);
        if (asset == null && File.Exists(normalizedPath))
        {
            AssetDatabase.ImportAsset(normalizedPath, ImportAssetOptions.Default);
            asset = AssetDatabase.LoadAssetAtPath<T>(normalizedPath);
        }

        return asset;
    }

    private Object[] LoadAllAssetsAtPathWithImport(string path)
    {
        string normalizedPath = NormalizeAssetPath(path);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(normalizedPath);
        if ((assets == null || assets.Length == 0) && File.Exists(normalizedPath))
        {
            AssetDatabase.ImportAsset(normalizedPath, ImportAssetOptions.Default);
            assets = AssetDatabase.LoadAllAssetsAtPath(normalizedPath);
        }

        return assets ?? System.Array.Empty<Object>();
    }

    private void EnsureHumanoidImporter(string path, bool importAnimation)
    {
        string normalizedPath = NormalizeAssetPath(path);
        ModelImporter importer = AssetImporter.GetAtPath(normalizedPath) as ModelImporter;
        if (importer == null)
        {
            return;
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

        if (importer.importAnimation != importAnimation)
        {
            importer.importAnimation = importAnimation;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private string NormalizeAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Trim().Replace('\\', '/');
        int assetsIndex = normalizedPath.IndexOf("/Assets/", System.StringComparison.OrdinalIgnoreCase);
        if (assetsIndex >= 0)
        {
            normalizedPath = normalizedPath.Substring(assetsIndex + 1);
        }

        return normalizedPath;
    }
#endif

    private AnimationClip FindUsableClip(AnimationClip[] clips)
    {
        if (clips == null)
        {
            return null;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (IsUsableAnimationClip(clip))
            {
                return clip;
            }
        }

        return null;
    }

    private bool IsUsableAnimationClip(AnimationClip clip)
    {
        return clip != null &&
            !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool NameMatches(string clipName, params string[] expectedNames)
    {
        if (string.IsNullOrWhiteSpace(clipName) || expectedNames == null)
        {
            return false;
        }

        string normalizedClipName = NormalizeAnimationName(clipName);
        for (int i = 0; i < expectedNames.Length; i++)
        {
            string expectedName = expectedNames[i];
            if (string.IsNullOrWhiteSpace(expectedName))
            {
                continue;
            }

            string normalizedExpectedName = NormalizeAnimationName(expectedName);
            if (normalizedClipName == normalizedExpectedName ||
                normalizedClipName.Contains(normalizedExpectedName))
            {
                return true;
            }
        }

        return false;
    }

    private string NormalizeAnimationName(string value)
    {
        return value
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    private void UpdateMixamoAnimation()
    {
        if (!animationSystemReady)
        {
            return;
        }

        AnimationClip desiredClip = GetDesiredAnimationClip(out bool shouldLoop);
        if (desiredClip != null)
        {
            CrossFadeToClip(desiredClip, shouldLoop);
        }

        UpdateAnimationPlayback(activeAnimationPlayable, activeAnimationClip, activeAnimationLoops);
        if (animationTransitionActive)
        {
            UpdateAnimationPlayback(transitionAnimationPlayable, transitionAnimationClip, transitionAnimationLoops);
            animationBlendTimer += Time.deltaTime;

            float blend01 = animationBlendDuration <= 0f
                ? 1f
                : Mathf.Clamp01(animationBlendTimer / animationBlendDuration);
            animationMixer.SetInputWeight(0, 1f - blend01);
            animationMixer.SetInputWeight(1, blend01);

            if (blend01 >= 1f)
            {
                CompleteAnimationTransition();
            }
        }

        lastYaw = transform.eulerAngles.y;
    }

    private AnimationClip GetDesiredAnimationClip(out bool shouldLoop)
    {
        shouldLoop = true;

        Vector3 planarVelocity = controller != null ? controller.velocity : Vector3.zero;
        planarVelocity.y = 0f;
        float planarSpeed = planarVelocity.magnitude;
        float yawSpeed = Mathf.Abs(Mathf.DeltaAngle(lastYaw, transform.eulerAngles.y)) / Mathf.Max(Time.deltaTime, 0.0001f);

        if (captureTarget != null && attackAnimationClip != null)
        {
            shouldLoop = false;
            return attackAnimationClip;
        }

        if (gameManager != null && gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Incubation)
        {
            return idleAnimationClip;
        }

        if (planarSpeed <= turnAnimationMaxMoveSpeed &&
            yawSpeed >= turnAnimationAngularSpeedThreshold &&
            turnAnimationClip != null &&
            currentState != MonsterState.Dormant)
        {
            return turnAnimationClip;
        }

        if (currentState == MonsterState.Hunting && planarSpeed > animationMoveThreshold)
        {
            return runAnimationClip;
        }

        if (planarSpeed > animationMoveThreshold)
        {
            return walkAnimationClip;
        }

        return idleAnimationClip;
    }

    private void PlayClipImmediate(AnimationClip clip, bool loops)
    {
        if (clip == null)
        {
            return;
        }

        if (activeAnimationPlayable.IsValid())
        {
            animationMixer.DisconnectInput(0);
            activeAnimationPlayable.Destroy();
        }

        activeAnimationPlayable = CreatePlayableForClip(clip);
        activeAnimationClip = clip;
        activeAnimationLoops = loops;
        animationMixer.ConnectInput(0, activeAnimationPlayable, 0);
        animationMixer.SetInputWeight(0, 1f);

        if (transitionAnimationPlayable.IsValid())
        {
            animationMixer.DisconnectInput(1);
            transitionAnimationPlayable.Destroy();
            transitionAnimationPlayable = default;
        }

        animationMixer.SetInputWeight(1, 0f);
        animationTransitionActive = false;
        animationBlendTimer = 0f;
    }

    private void CrossFadeToClip(AnimationClip clip, bool loops)
    {
        if (clip == null)
        {
            return;
        }

        if (activeAnimationClip == clip && !animationTransitionActive)
        {
            return;
        }

        if (!activeAnimationPlayable.IsValid())
        {
            PlayClipImmediate(clip, loops);
            return;
        }

        if (animationTransitionActive && transitionAnimationClip == clip)
        {
            return;
        }

        if (transitionAnimationPlayable.IsValid())
        {
            animationMixer.DisconnectInput(1);
            transitionAnimationPlayable.Destroy();
        }

        transitionAnimationPlayable = CreatePlayableForClip(clip);
        transitionAnimationClip = clip;
        transitionAnimationLoops = loops;
        animationMixer.ConnectInput(1, transitionAnimationPlayable, 0);
        animationMixer.SetInputWeight(0, 1f);
        animationMixer.SetInputWeight(1, 0f);
        animationBlendTimer = 0f;
        animationTransitionActive = true;
    }

    private AnimationClipPlayable CreatePlayableForClip(AnimationClip clip)
    {
        AnimationClipPlayable playable = AnimationClipPlayable.Create(animationGraph, clip);
        playable.SetTime(0d);
        playable.SetSpeed(1d);
        playable.SetApplyFootIK(true);
        return playable;
    }

    private void UpdateAnimationPlayback(AnimationClipPlayable playable, AnimationClip clip, bool loops)
    {
        if (!playable.IsValid() || clip == null || !loops || clip.length <= 0f)
        {
            return;
        }

        double clipTime = playable.GetTime();
        if (clipTime < clip.length)
        {
            return;
        }

        playable.SetTime(clipTime % clip.length);
    }

    private void CompleteAnimationTransition()
    {
        if (!transitionAnimationPlayable.IsValid())
        {
            animationTransitionActive = false;
            return;
        }

        if (activeAnimationPlayable.IsValid())
        {
            animationMixer.DisconnectInput(0);
            activeAnimationPlayable.Destroy();
        }

        activeAnimationPlayable = transitionAnimationPlayable;
        activeAnimationClip = transitionAnimationClip;
        activeAnimationLoops = transitionAnimationLoops;

        animationMixer.DisconnectInput(1);
        animationMixer.ConnectInput(0, activeAnimationPlayable, 0);
        animationMixer.SetInputWeight(0, 1f);
        animationMixer.SetInputWeight(1, 0f);

        transitionAnimationPlayable = default;
        transitionAnimationClip = null;
        transitionAnimationLoops = false;
        animationTransitionActive = false;
        animationBlendTimer = 0f;
    }

    private bool CanSeePlayer(float distanceToPlayer)
    {
        if (playerTarget == null ||
            !IsWithinVerticalSenseRange(playerTarget) ||
            distanceToPlayer > GetCurrentDetectionRadius())
        {
            return false;
        }

        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);

        if (distanceToPlayer > closeRangeDetectionRadius)
        {
            Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            float angle = Vector3.Angle(flatForward, flatToPlayer.normalized);
            if (angle > visionAngle * 0.5f)
            {
                return false;
            }
        }

        Vector3 eyeOrigin = transform.position + Vector3.up * Mathf.Max(controller.height * 0.7f, 1.2f);
        Vector3 playerChest = playerTarget.position + Vector3.up * 1.2f;

        if (Physics.Linecast(eyeOrigin, playerChest, out RaycastHit hit, sightBlockerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != null &&
                !hit.transform.IsChildOf(playerTarget) &&
                !hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanHearPlayer(float distanceToPlayer)
    {
        if (playerController == null || !IsWithinVerticalSenseRange(playerTarget))
        {
            return false;
        }

        float playerSpeed = playerController.CurrentPlanarSpeed;
        bool isLoud = playerController.IsSprinting;

        if (!isLoud && playerSpeed < movementNoiseThreshold)
        {
            return false;
        }

        float hearingRange = isLoud ? GetCurrentSprintHearingRadius() : GetCurrentHearingRadius();
        float speedRatio = Mathf.Clamp01(playerSpeed / Mathf.Max(movementNoiseThreshold, 0.1f));
        hearingRange *= Mathf.Lerp(0.9f, 1.2f, speedRatio);

        return distanceToPlayer <= hearingRange;
    }

    private Vector3 GetPredictedPlayerPosition(float distanceToPlayer)
    {
        if (playerTarget == null)
        {
            return transform.position;
        }

        Vector3 predictedPosition = playerTarget.position;
        if (playerController != null)
        {
            float predictionTime = Mathf.Clamp(
                distanceToPlayer / Mathf.Max(GetCurrentChaseSpeed(), 0.1f),
                0.1f,
                maxPredictionTime
            );

            predictedPosition += playerController.PlanarVelocity * predictionTime * predictionStrength;
        }

        predictedPosition.y = playerTarget.position.y;
        return predictedPosition;
    }

    private void RememberPlayerPosition(Vector3 worldPosition, float duration)
    {
        lastKnownPlayerPosition = worldPosition;
        memoryTimer = Mathf.Max(memoryTimer, duration);
        searchRetargetTimer = 0f;
    }

    private void RaiseAggression(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        aggressionLevel = Mathf.Clamp01(aggressionLevel + amount);
    }

    private void PickNextSearchTarget()
    {
        Vector3 anchor = GetPlanarPosition(lastKnownPlayerPosition);
        float stepAngle = 360f / Mathf.Max(1, searchPointCount);
        float angle = stepAngle * searchPointIndex + Random.Range(-20f, 20f);
        float effectiveSearchRadius = searchRadius * (puzzleAlertTimer > 0f ? puzzleAlertSearchRadiusMultiplier : 1f);
        float radius = Random.Range(effectiveSearchRadius * 0.45f, effectiveSearchRadius);
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

        searchPointIndex = (searchPointIndex + 1) % Mathf.Max(1, searchPointCount);
        investigationTarget = anchor + offset;
        investigationTarget.y = transform.position.y;
        hasInvestigationTarget = true;
        searchRetargetTimer = searchRetargetInterval;
    }

    private Vector3 GetFlatDirectionTo(Vector3 worldPosition)
    {
        Vector3 flatOffset = GetPlanarPosition(worldPosition) - GetPlanarPosition(transform.position);
        return flatOffset.sqrMagnitude > 0.001f ? flatOffset.normalized : Vector3.zero;
    }

    private Vector3 GetPlanarPosition(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, 0f, worldPosition.z);
    }

    private Vector3 GetNavigationProbeOrigin()
    {
        float scaleY = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
        return transform.position + Vector3.up * Mathf.Max(controller.height * scaleY * 0.45f, 0.95f);
    }

    private float GetCurrentChaseSpeed()
    {
        return IsSlowed ? slowedMoveSpeed : moveSpeed;
    }

    private float GetCurrentInvestigationSpeed()
    {
        float roamSpeed = patrolMoveSpeed * GetPatrolSpeedMultiplier();
        float targetSpeed = Mathf.Max(investigationMoveSpeed, roamSpeed);
        targetSpeed *= 1f + aggressionLevel * aggressionInvestigationSpeedBonus;
        targetSpeed = Mathf.Min(targetSpeed, moveSpeed);
        return IsSlowed ? Mathf.Min(slowedMoveSpeed, targetSpeed) : targetSpeed;
    }

    private float GetCurrentRoamSpeed()
    {
        float roamSpeed = Mathf.Min(patrolMoveSpeed * GetPatrolSpeedMultiplier(), moveSpeed);
        return IsSlowed ? Mathf.Min(slowedMoveSpeed, roamSpeed) : roamSpeed;
    }

    private float GetCurrentTurnSpeed()
    {
        return turnSpeed * GetTurnSpeedMultiplier();
    }

    private float GetCurrentDetectionRadius()
    {
        return detectionRadius;
    }

    private float GetCurrentHearingRadius()
    {
        return hearingRadius + GetTaskProgress01() * taskProgressHearingBonus * GetIntelligenceBonusScale();
    }

    private float GetCurrentSprintHearingRadius()
    {
        return sprintHearingRadius + GetTaskProgress01() * taskProgressHearingBonus * GetIntelligenceBonusScale();
    }

    private float GetCurrentMemoryDuration()
    {
        return memoryDuration + GetTaskProgress01() * taskProgressMemoryBonus * GetIntelligenceBonusScale();
    }

    private float GetTaskProgress01()
    {
        if (!CanInvestigateMemory() || gameManager == null || gameManager.RequiredTasks <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)gameManager.CompletedTasks / gameManager.RequiredTasks);
    }

    private bool CanUseHearing()
    {
        return intelligenceLevel != IntelligenceLevel.Easy;
    }

    private bool CanInvestigateMemory()
    {
        return true;
    }

    private bool CanGuardExit()
    {
        return intelligenceLevel == IntelligenceLevel.Hard;
    }

    private bool CanPredictPlayerMovement()
    {
        return intelligenceLevel == IntelligenceLevel.Hard;
    }

    private bool CanWanderAroundPatrolAnchors()
    {
        return true;
    }

    private float GetIntelligenceBonusScale()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return 1f;
            case IntelligenceLevel.Medium:
                return 0.45f;
            default:
                return 0f;
        }
    }

    private float GetPatrolSpeedMultiplier()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return hardPatrolSpeedMultiplier;
            case IntelligenceLevel.Medium:
                return Mathf.Lerp(1f, hardPatrolSpeedMultiplier, 0.35f);
            default:
                return 0.92f;
        }
    }

    private float GetTurnSpeedMultiplier()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return hardTurnSpeedMultiplier;
            case IntelligenceLevel.Medium:
                return Mathf.Lerp(1f, hardTurnSpeedMultiplier, 0.55f);
            default:
                return 0.9f;
        }
    }

    private float GetEffectivePatrolWaitTime()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return patrolWaitTime * 0.3f;
            case IntelligenceLevel.Medium:
                return patrolWaitTime * 0.65f;
            default:
                return patrolWaitTime;
        }
    }

    private float GetEffectiveWanderChance()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return Mathf.Clamp01(localWanderChance + 0.25f);
            case IntelligenceLevel.Medium:
                return localWanderChance;
            default:
                return 0f;
        }
    }

    private float GetEffectiveRoamRetargetInterval()
    {
        switch (intelligenceLevel)
        {
            case IntelligenceLevel.Hard:
                return roamRetargetInterval * 0.75f;
            case IntelligenceLevel.Medium:
                return roamRetargetInterval;
            default:
                return roamRetargetInterval * 1.25f;
        }
    }
}

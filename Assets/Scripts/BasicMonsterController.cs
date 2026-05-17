using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class BasicMonsterController : MonoBehaviour
{
    private enum MonsterState
    {
        Dormant,
        Roaming,
        Investigating,
        Hunting,
        GuardingExit
    }

    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PrototypeGameManager gameManager;
    [SerializeField] private BasicFirstPersonController playerController;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField, Range(20f, 180f)] private float visionAngle = 110f;
    [SerializeField] private float closeRangeDetectionRadius = 2f;
    [SerializeField] private float hearingRadius = 7f;
    [SerializeField] private float sprintHearingRadius = 11f;
    [SerializeField] private float movementNoiseThreshold = 4f;
    [SerializeField] private float memoryDuration = 6f;
    [SerializeField] private float forcedSearchDuration = 4f;
    [SerializeField] private float huntPersistenceDuration = 0.65f;
    [SerializeField] private LayerMask sightBlockerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private string hardSceneName = "demoScene_Difficile";
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
    [SerializeField, Range(1, 5)] private int roomExploreBurstsMin = 1;
    [SerializeField, Range(1, 6)] private int roomExploreBurstsMax = 3;
    [SerializeField, Range(1, 6)] private int patrolRecentMemory = 3;
    [SerializeField] private float revisitPenaltyMultiplier = 0.12f;
    [SerializeField] private float samePointPenaltyMultiplier = 0.05f;
    [SerializeField] private float farPointWeightBoost = 1.4f;

    [Header("Search")]
    [SerializeField] private float maxPredictionTime = 1.1f;
    [SerializeField] private float predictionStrength = 0.9f;
    [SerializeField] private float searchRadius = 5.5f;
    [SerializeField] private float searchRetargetInterval = 1.25f;
    [SerializeField] private int searchPointCount = 6;
    [SerializeField] private float exitGuardDistance = 3.5f;
    [SerializeField] private float exitGuardRetargetInterval = 1f;

    [Header("Recovery")]
    [SerializeField] private float stuckRetargetDelay = 0.95f;
    [SerializeField] private float corridorRecoveryDistance = 1.9f;

    [Header("Visual Motion")]
    [SerializeField] private Transform animatedVisual;
    [SerializeField] private float walkBobAmplitude = 0.02f;
    [SerializeField] private float runBobAmplitude = 0.075f;
    [SerializeField] private float walkBobFrequency = 2.3f;
    [SerializeField] private float runBobFrequency = 6.2f;
    [SerializeField] private float maxRollAngle = 6f;
    [SerializeField] private float maxPitchAngle = 4f;
    [SerializeField] private float visualSmoothing = 10f;

    public bool IsSlowed => slowTimer > 0f;
    public float SlowTimeRemaining => Mathf.Max(0f, slowTimer);
    public bool IsHuntingState => currentState == MonsterState.Hunting;
    public bool IsInvestigatingState => currentState == MonsterState.Investigating;
    public bool IsGuardingExitState => currentState == MonsterState.GuardingExit;
    public bool IsRoamingState => currentState == MonsterState.Roaming;
    public float CurrentPlanarSpeed => new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
    public bool HasMovementIntent => currentState != MonsterState.Dormant && CurrentPlanarSpeed > 0.02f;

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
    private float huntPersistenceTimer;
    private float avoidanceLockTimer;
    private int patrolPointIndex;
    private int searchPointIndex;
    private int lastAvoidanceSign = 1;
    private int guardOrbitSign = 1;
    private int remainingRoomExploreBursts = -1;
    private MonsterState currentState = MonsterState.Dormant;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private float visualCycle;
    private Vector3 lastPosition;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigationTarget;
    private Vector3 roamTarget;
    private Vector3 guardTarget;
    private Vector3 avoidanceLockDirection;
    private MonsterWalkAnimationDriver walkAnimationDriver;
    private bool hardIntelligenceEnabled;
    private bool hasInvestigationTarget;
    private bool hasRoamTarget;
    private readonly Queue<int> recentPatrolIndices = new Queue<int>();

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        hardIntelligenceEnabled = SceneManager.GetActiveScene().name == hardSceneName;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        walkAnimationDriver = GetComponent<MonsterWalkAnimationDriver>();

        if (animatedVisual == null)
        {
            animatedVisual = FindAnimatedVisual();
        }

        if (animatedVisual != null)
        {
            visualBaseLocalPosition = animatedVisual.localPosition;
            visualBaseLocalRotation = animatedVisual.localRotation;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            patrolPoints = FindPatrolPointsInScene();
        }

        ResetRoomExploreBudget();
        RegisterRecentPatrolIndex(patrolPointIndex);

        lastPosition = transform.position;
    }

    private void Update()
    {
        TickTimers();
        HandleMovement();
    }

    private void LateUpdate()
    {
        UpdateVisualMotion();
    }

    public void RevealPlayerPosition(float duration)
    {
        forcedRevealTimer = Mathf.Max(forcedRevealTimer, duration);
        Debug.Log("Position du joueur revelee a l'Entite pendant " + duration + " secondes.");
    }

    public void ApplyStaticTrap(float duration)
    {
        slowTimer = Mathf.Max(slowTimer, duration);
        Debug.Log("L'Entite est ralentie pendant " + duration + " secondes.");
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

    public void NotifySuspiciousActivity(Vector3 worldPosition, float duration, bool highPriority)
    {
        if (!hardIntelligenceEnabled)
        {
            return;
        }

        float effectiveDuration = highPriority ? Mathf.Max(duration, forcedSearchDuration) : duration;
        RememberPlayerPosition(worldPosition, effectiveDuration);
        investigationTarget = worldPosition;
        hasInvestigationTarget = true;
        searchRetargetTimer = highPriority ? 0f : searchRetargetInterval * 0.6f;
        roamPauseTimer = 0f;
        patrolWaitTimer = 0f;
        stuckTimer = 0f;
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

        if (avoidanceLockTimer > 0f)
        {
            avoidanceLockTimer -= Time.deltaTime;
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

    private MonsterState ResolveState(bool canMove, out Vector3 velocity, out bool isChasing, out bool isInvestigating)
    {
        velocity = Vector3.zero;
        isChasing = false;
        isInvestigating = false;

        if (!canMove || playerTarget == null)
        {
            return MonsterState.Dormant;
        }

        Vector3 offsetToPlayer = playerTarget.position - transform.position;
        Vector3 flatOffset = new Vector3(offsetToPlayer.x, 0f, offsetToPlayer.z);
        float distanceToPlayer = flatOffset.magnitude;
        bool playerHidden = playerController != null && playerController.IsHiddenActive;
        bool hasForcedReveal = forcedRevealTimer > 0f;
        bool canSeePlayer = !playerHidden && CanSeePlayer(distanceToPlayer);
        bool canHearPlayer = hardIntelligenceEnabled && !playerHidden && CanHearPlayer(distanceToPlayer);

        // Priorite: chasse si le joueur est vu, enquete si un indice recent existe,
        // puis garde de sortie ou errance selon la phase et la difficulte.
        if (hasForcedReveal || canSeePlayer)
        {
            huntPersistenceTimer = huntPersistenceDuration;
            RememberPlayerPosition(
                hardIntelligenceEnabled ? GetPredictedPlayerPosition(distanceToPlayer) : playerTarget.position,
                hasForcedReveal ? forcedSearchDuration : GetCurrentMemoryDuration()
            );

            velocity = GetChaseVelocity(distanceToPlayer);
            isChasing = true;
            return MonsterState.Hunting;
        }

        if (canHearPlayer)
        {
            RememberPlayerPosition(GetPredictedPlayerPosition(distanceToPlayer), GetCurrentMemoryDuration() * 0.85f);
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

        if (hardIntelligenceEnabled && memoryTimer > 0f)
        {
            velocity = GetInvestigationVelocity();
            isInvestigating = velocity.sqrMagnitude > 0.001f;
            return MonsterState.Investigating;
        }

        if (hardIntelligenceEnabled && TryGetExitGuardVelocity(out velocity))
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

        Vector3 chaseTarget = hardIntelligenceEnabled ? lastKnownPlayerPosition : playerTarget.position;
        Vector3 chaseDirection = GetFlatDirectionTo(chaseTarget);
        return chaseDirection * GetCurrentChaseSpeed();
    }

    private Vector3 GetInvestigationVelocity()
    {
        if (memoryTimer <= 0f)
        {
            hasInvestigationTarget = false;
            return Vector3.zero;
        }

        Vector3 anchor = lastKnownPlayerPosition;
        float distanceToAnchor = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(anchor));
        if (distanceToAnchor > patrolStoppingDistance + 0.35f)
        {
            hasInvestigationTarget = false;
            return GetFlatDirectionTo(anchor) * GetCurrentInvestigationSpeed();
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

        return GetFlatDirectionTo(investigationTarget) * GetCurrentInvestigationSpeed();
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

        return GetFlatDirectionTo(roamTarget) * GetCurrentRoamSpeed();
    }

    private void PickNextRoamTarget()
    {
        Transform patrolAnchor = GetCurrentPatrolAnchor();
        Vector3 anchorPosition = patrolAnchor != null ? patrolAnchor.position : transform.position;
        Vector3 planarPosition = GetPlanarPosition(transform.position);
        Vector3 planarAnchor = GetPlanarPosition(anchorPosition);

        if (patrolAnchor != null && Vector3.Distance(planarPosition, planarAnchor) <= patrolStoppingDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            float effectiveWaitTime = hardIntelligenceEnabled ? patrolWaitTime * 0.35f : patrolWaitTime;
            if (patrolWaitTimer >= effectiveWaitTime)
            {
                patrolWaitTimer = 0f;
                if (ConsumeRoomExploreBurst())
                {
                    roamTarget = BuildExplorationTarget(planarAnchor);
                    hasRoamTarget = true;
                    roamRetargetTimer = roamRetargetInterval * Random.Range(0.8f, 1.15f);
                    searchRetargetTimer = 0f;
                    return;
                }

                AdvancePatrolPoint();
                patrolAnchor = GetCurrentPatrolAnchor();
                anchorPosition = patrolAnchor != null ? patrolAnchor.position : transform.position;
                planarAnchor = GetPlanarPosition(anchorPosition);
            }
        }
        else
        {
            patrolWaitTimer = 0f;
        }

        bool wanderNearAnchor = hardIntelligenceEnabled && Random.value < localWanderChance;
        Vector2 jitter2D = wanderNearAnchor ? Random.insideUnitCircle * roamRadius : Vector2.zero;
        roamTarget = new Vector3(planarAnchor.x + jitter2D.x, transform.position.y, planarAnchor.z + jitter2D.y);
        hasRoamTarget = true;
        roamRetargetTimer = roamRetargetInterval * Random.Range(0.85f, 1.2f);
        searchRetargetTimer = 0f;
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
            velocity = GetFlatDirectionTo(exitPosition) * GetCurrentInvestigationSpeed();
            return velocity.sqrMagnitude > 0.001f;
        }

        velocity = GetFlatDirectionTo(guardTarget) * GetCurrentInvestigationSpeed();
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

        float[] heightOffsets =
        {
            Mathf.Max(controller.height * 0.32f, 0.65f),
            Mathf.Max(controller.height * 0.62f, 1.15f)
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

        if (planarDelta.sqrMagnitude >= 0.0004f)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.deltaTime;
        float effectiveStuckDelay = isChasing ? stuckRetargetDelay * 0.8f : stuckRetargetDelay;

        if (stuckTimer < effectiveStuckDelay)
        {
            return;
        }

        stuckTimer = 0f;
        Vector3 fallbackDirection = Vector3.Cross(Vector3.up, targetVelocity.normalized) * lastAvoidanceSign;
        if (fallbackDirection.sqrMagnitude < 0.001f)
        {
            fallbackDirection = transform.right * lastAvoidanceSign;
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

        if (isInvestigating && hardIntelligenceEnabled)
        {
            PickNextSearchTarget();
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            AdvancePatrolPoint();
        }
        else
        {
            hasRoamTarget = false;
        }
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

    private void AdvancePatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        patrolPointIndex = SelectNextPatrolPointIndex();
        RegisterRecentPatrolIndex(patrolPointIndex);
        ResetRoomExploreBudget();
        patrolWaitTimer = 0f;
        roamRetargetTimer = 0f;
        hasRoamTarget = false;
    }

    private int SelectNextPatrolPointIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return patrolPointIndex;
        }

        Vector3 currentPlanar = GetPlanarPosition(transform.position);
        float totalWeight = 0f;
        float[] weights = new float[patrolPoints.Length];

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform point = patrolPoints[i];
            if (point == null)
            {
                weights[i] = 0f;
                continue;
            }

            float distance = Vector3.Distance(currentPlanar, GetPlanarPosition(point.position));
            float distanceWeight = Mathf.Lerp(0.85f, farPointWeightBoost, Mathf.InverseLerp(0f, 24f, distance));
            float weight = distanceWeight;

            if (i == patrolPointIndex)
            {
                weight *= samePointPenaltyMultiplier;
            }

            if (recentPatrolIndices.Contains(i))
            {
                weight *= revisitPenaltyMultiplier;
            }

            weights[i] = Mathf.Max(0.001f, weight);
            totalWeight += weights[i];
        }

        if (totalWeight <= 0.001f)
        {
            return (patrolPointIndex + 1) % patrolPoints.Length;
        }

        float pick = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (pick <= cumulative)
            {
                return i;
            }
        }

        return patrolPointIndex;
    }

    private void RegisterRecentPatrolIndex(int index)
    {
        if (index < 0)
        {
            return;
        }

        recentPatrolIndices.Enqueue(index);
        while (recentPatrolIndices.Count > Mathf.Max(1, patrolRecentMemory))
        {
            recentPatrolIndices.Dequeue();
        }
    }

    private void ResetRoomExploreBudget()
    {
        int minBursts = Mathf.Max(1, roomExploreBurstsMin);
        int maxBursts = Mathf.Max(minBursts, roomExploreBurstsMax);
        remainingRoomExploreBursts = Random.Range(minBursts, maxBursts + 1);
    }

    private bool ConsumeRoomExploreBurst()
    {
        if (remainingRoomExploreBursts <= 0)
        {
            return false;
        }

        remainingRoomExploreBursts--;
        return true;
    }

    private Vector3 BuildExplorationTarget(Vector3 planarAnchor)
    {
        float explorationRadius = roamRadius * Random.Range(0.8f, 1.35f);
        Vector2 jitter2D = Random.insideUnitCircle * explorationRadius;
        return new Vector3(planarAnchor.x + jitter2D.x, transform.position.y, planarAnchor.z + jitter2D.y);
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
        }

        if (walkAnimationDriver == null)
        {
            walkAnimationDriver = GetComponent<MonsterWalkAnimationDriver>();
        }

        if (walkAnimationDriver != null && walkAnimationDriver.IsUsingWalkAnimation)
        {
            animatedVisual.localPosition = Vector3.Lerp(
                animatedVisual.localPosition,
                visualBaseLocalPosition,
                Time.deltaTime * visualSmoothing
            );
            animatedVisual.localRotation = Quaternion.Slerp(
                animatedVisual.localRotation,
                visualBaseLocalRotation,
                Time.deltaTime * visualSmoothing
            );
            return;
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
    }

    private bool CanSeePlayer(float distanceToPlayer)
    {
        if (playerTarget == null || distanceToPlayer > GetCurrentDetectionRadius())
        {
            return false;
        }

        Vector3 toPlayer = playerTarget.position - transform.position;
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);

        if (distanceToPlayer > closeRangeDetectionRadius)
        {
            float angle = Vector3.Angle(transform.forward, flatToPlayer.normalized);
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
        if (playerController == null)
        {
            return false;
        }

        float playerSpeed = playerController.CurrentPlanarSpeed;
        bool isLoud = playerController.IsSprinting || playerController.IsOverclockActive;

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

    private void PickNextSearchTarget()
    {
        Vector3 anchor = GetPlanarPosition(lastKnownPlayerPosition);
        float stepAngle = 360f / Mathf.Max(1, searchPointCount);
        float angle = stepAngle * searchPointIndex + Random.Range(-20f, 20f);
        float radius = Random.Range(searchRadius * 0.45f, searchRadius);
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
        return transform.position + Vector3.up * Mathf.Max(controller.height * 0.45f, 0.95f);
    }

    private float GetCurrentChaseSpeed()
    {
        float chaseSpeed = moveSpeed + GetTaskProgress01() * taskProgressChaseSpeedBonus;
        return IsSlowed ? slowedMoveSpeed : chaseSpeed;
    }

    private float GetCurrentInvestigationSpeed()
    {
        float roamSpeed = patrolMoveSpeed * (hardIntelligenceEnabled ? hardPatrolSpeedMultiplier : 1f);
        float targetSpeed = Mathf.Max(investigationMoveSpeed, roamSpeed);
        return IsSlowed ? Mathf.Min(slowedMoveSpeed, targetSpeed) : targetSpeed;
    }

    private float GetCurrentRoamSpeed()
    {
        float roamSpeed = patrolMoveSpeed * (hardIntelligenceEnabled ? hardPatrolSpeedMultiplier : 1f);
        return IsSlowed ? Mathf.Min(slowedMoveSpeed, roamSpeed) : roamSpeed;
    }

    private float GetCurrentTurnSpeed()
    {
        return turnSpeed * (hardIntelligenceEnabled ? hardTurnSpeedMultiplier : 1f);
    }

    private float GetCurrentDetectionRadius()
    {
        if (!hardIntelligenceEnabled)
        {
            return detectionRadius;
        }

        return detectionRadius + GetTaskProgress01() * taskProgressDetectionBonus;
    }

    private float GetCurrentHearingRadius()
    {
        if (!hardIntelligenceEnabled)
        {
            return hearingRadius;
        }

        return hearingRadius + GetTaskProgress01() * taskProgressHearingBonus;
    }

    private float GetCurrentSprintHearingRadius()
    {
        if (!hardIntelligenceEnabled)
        {
            return sprintHearingRadius;
        }

        return sprintHearingRadius + GetTaskProgress01() * taskProgressHearingBonus;
    }

    private float GetCurrentMemoryDuration()
    {
        if (!hardIntelligenceEnabled)
        {
            return memoryDuration;
        }

        return memoryDuration + GetTaskProgress01() * taskProgressMemoryBonus;
    }

    private float GetTaskProgress01()
    {
        if (!hardIntelligenceEnabled || gameManager == null || gameManager.RequiredTasks <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)gameManager.CompletedTasks / gameManager.RequiredTasks);
    }
}

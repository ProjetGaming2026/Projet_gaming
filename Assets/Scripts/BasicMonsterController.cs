using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class BasicMonsterController : MonoBehaviour
{
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
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float patrolMoveSpeed = 1.8f;
    [SerializeField] private float patrolStoppingDistance = 0.75f;
    [SerializeField] private float patrolWaitTime = 0.4f;
    [SerializeField] private float obstacleCheckDistance = 1.25f;
    [SerializeField] private float obstacleCheckRadius = 0.35f;
    [SerializeField] private float obstacleAvoidanceStrength = 0.9f;
    [SerializeField] private float obstacleSideCheckAngle = 35f;
    [SerializeField] private LayerMask obstacleMask = Physics.DefaultRaycastLayers;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 10f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float hardPatrolSpeedMultiplier = 1.05f;
    [SerializeField] private float hardTurnSpeedMultiplier = 1.35f;

    [Header("Search")]
    [SerializeField] private float maxPredictionTime = 1.1f;
    [SerializeField] private float predictionStrength = 0.9f;
    [SerializeField] private float searchRadius = 5.5f;
    [SerializeField] private float searchRetargetInterval = 1.25f;
    [SerializeField] private int searchPointCount = 6;
    [SerializeField] private float exitGuardDistance = 3.5f;
    [SerializeField] private float exitGuardRetargetInterval = 1f;

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

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float forcedRevealTimer;
    private float slowTimer;
    private float memoryTimer;
    private float searchRetargetTimer;
    private int patrolPointIndex;
    private float patrolWaitTimer;
    private float stuckTimer;
    private int searchPointIndex;
    private int lastAvoidanceSign = 1;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private float visualCycle;
    private Vector3 lastPosition;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 investigationTarget;
    private Vector3 guardTarget;
    private bool hardIntelligenceEnabled;
    private bool hasInvestigationTarget;

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

        lastPosition = transform.position;
    }

    private void Update()
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

        Vector3 targetVelocity = Vector3.zero;
        bool isChasing = false;
        bool isInvestigating = false;

        if (canMove && playerTarget != null)
        {
            Vector3 offsetToPlayer = playerTarget.position - transform.position;
            Vector3 flatOffset = new Vector3(offsetToPlayer.x, 0f, offsetToPlayer.z);
            float distanceToPlayer = flatOffset.magnitude;

            bool playerHidden = playerController != null && playerController.IsHiddenActive;
            bool hasForcedReveal = forcedRevealTimer > 0f;
            bool canSeePlayer = !playerHidden && CanSeePlayer(distanceToPlayer);
            bool canHearPlayer = hardIntelligenceEnabled && !playerHidden && CanHearPlayer(distanceToPlayer);

            if (hasForcedReveal || canSeePlayer)
            {
                RememberPlayerPosition(
                    hardIntelligenceEnabled ? GetPredictedPlayerPosition(distanceToPlayer) : playerTarget.position,
                    hasForcedReveal ? forcedSearchDuration : GetCurrentMemoryDuration()
                );

                if (distanceToPlayer > stoppingDistance)
                {
                    float currentMoveSpeed = GetCurrentChaseSpeed();
                    Vector3 chaseTarget = hardIntelligenceEnabled ? lastKnownPlayerPosition : playerTarget.position;
                    Vector3 chaseDirection = GetFlatDirectionTo(chaseTarget);
                    targetVelocity = chaseDirection * currentMoveSpeed;
                }

                isChasing = true;
            }
            else if (canHearPlayer)
            {
                RememberPlayerPosition(GetPredictedPlayerPosition(distanceToPlayer), GetCurrentMemoryDuration() * 0.8f);
            }
        }

        if (canMove && !isChasing)
        {
            if (hardIntelligenceEnabled && TryGetInvestigationVelocity(out targetVelocity))
            {
                isInvestigating = true;
            }
            else if (hardIntelligenceEnabled && TryGetExitGuardVelocity(out targetVelocity))
            {
                isInvestigating = true;
            }
            else
            {
                targetVelocity = GetPatrolVelocity();
            }
        }

        if (canMove)
        {
            targetVelocity = ApplyObstacleAvoidance(targetVelocity);
        }

        if (targetVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetVelocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                GetCurrentTurnSpeed() * Time.deltaTime
            );
        }

        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
        UpdateNavigationRecovery(targetVelocity, isChasing, isInvestigating);
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

    private Vector3 GetPatrolVelocity()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return Vector3.zero;
        }

        Transform currentPatrolPoint = patrolPoints[patrolPointIndex];
        if (currentPatrolPoint == null)
        {
            AdvancePatrolPoint();
            return Vector3.zero;
        }

        Vector3 offsetToPatrolPoint = currentPatrolPoint.position - transform.position;
        Vector3 flatOffset = new Vector3(offsetToPatrolPoint.x, 0f, offsetToPatrolPoint.z);
        float distanceToPatrolPoint = flatOffset.magnitude;

        if (distanceToPatrolPoint <= patrolStoppingDistance)
        {
            patrolWaitTimer += Time.deltaTime;
            float effectiveWaitTime = hardIntelligenceEnabled ? patrolWaitTime * 0.35f : patrolWaitTime;
            if (patrolWaitTimer >= effectiveWaitTime)
            {
                patrolWaitTimer = 0f;
                AdvancePatrolPoint();
            }

            return Vector3.zero;
        }

        patrolWaitTimer = 0f;
        float patrolSpeed = patrolMoveSpeed * (hardIntelligenceEnabled ? hardPatrolSpeedMultiplier : 1f);
        float currentPatrolSpeed = IsSlowed ? Mathf.Min(slowedMoveSpeed, patrolSpeed) : patrolSpeed;
        Vector3 patrolDirection = flatOffset.normalized;

        return patrolDirection * currentPatrolSpeed;
    }

    private bool TryGetInvestigationVelocity(out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (memoryTimer <= 0f)
        {
            hasInvestigationTarget = false;
            return false;
        }

        Vector3 searchAnchor = lastKnownPlayerPosition;
        float distanceToAnchor = Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(searchAnchor));
        if (distanceToAnchor > patrolStoppingDistance + 0.35f)
        {
            velocity = GetFlatDirectionTo(searchAnchor) * GetCurrentInvestigationSpeed();
            hasInvestigationTarget = false;
            return true;
        }

        if (!hasInvestigationTarget || searchRetargetTimer <= 0f ||
            Vector3.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(investigationTarget)) <= patrolStoppingDistance)
        {
            PickNextSearchTarget();
        }

        if (!hasInvestigationTarget)
        {
            return false;
        }

        velocity = GetFlatDirectionTo(investigationTarget) * GetCurrentInvestigationSpeed();
        return velocity.sqrMagnitude > 0.001f;
    }

    private bool IsObstacleAhead(Vector3 origin, Vector3 direction)
    {
        if (Physics.SphereCast(origin, obstacleCheckRadius, direction, out RaycastHit hit, obstacleCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform != null && !hit.transform.IsChildOf(transform);
        }

        return false;
    }

    private void AdvancePatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        patrolPointIndex = (patrolPointIndex + 1) % patrolPoints.Length;
        patrolWaitTimer = 0f;
        stuckTimer = 0f;
    }

    private void UpdateNavigationRecovery(Vector3 targetVelocity, bool isChasing, bool isInvestigating)
    {
        Vector3 planarDelta = transform.position - lastPosition;
        planarDelta.y = 0f;
        lastPosition = transform.position;

        if (isChasing)
        {
            stuckTimer = 0f;
            return;
        }

        if (targetVelocity.sqrMagnitude > 0.04f && planarDelta.sqrMagnitude < 0.0004f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= (hardIntelligenceEnabled ? 0.9f : 1.5f))
            {
                if (isInvestigating && hardIntelligenceEnabled)
                {
                    PickNextSearchTarget();
                }
                else if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    AdvancePatrolPoint();
                }
            }
        }
        else
        {
            stuckTimer = 0f;
        }
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
        stuckTimer = 0f;
    }

    private void PickNextSearchTarget()
    {
        Vector3 anchor = GetPlanarPosition(lastKnownPlayerPosition);
        float stepAngle = 360f / Mathf.Max(1, searchPointCount);
        float angle = stepAngle * searchPointIndex + Random.Range(-18f, 18f);
        float radius = Random.Range(searchRadius * 0.45f, searchRadius);
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

        searchPointIndex = (searchPointIndex + 1) % Mathf.Max(1, searchPointCount);
        investigationTarget = anchor + offset;
        investigationTarget.y = transform.position.y;
        hasInvestigationTarget = true;
        searchRetargetTimer = searchRetargetInterval;
        stuckTimer = 0f;
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

    private float GetCurrentChaseSpeed()
    {
        float chaseSpeed = moveSpeed + GetTaskProgress01() * taskProgressChaseSpeedBonus;
        return IsSlowed ? slowedMoveSpeed : chaseSpeed;
    }

    private float GetCurrentInvestigationSpeed()
    {
        float patrolSpeed = patrolMoveSpeed * (hardIntelligenceEnabled ? hardPatrolSpeedMultiplier : 1f);
        float targetSpeed = Mathf.Max(investigationMoveSpeed, patrolSpeed);
        return IsSlowed ? Mathf.Min(slowedMoveSpeed, targetSpeed) : targetSpeed;
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

        if (searchRetargetTimer <= 0f || guardTarget == Vector3.zero)
        {
            Vector3 exitPosition = gameManager.ActiveExitZone.transform.position;
            Vector3 toExit = GetPlanarPosition(exitPosition) - GetPlanarPosition(transform.position);
            Vector3 sideOffset = toExit.sqrMagnitude > 0.001f
                ? Vector3.Cross(Vector3.up, toExit.normalized) * Random.Range(-exitGuardDistance, exitGuardDistance)
                : Vector3.right * Random.Range(-exitGuardDistance, exitGuardDistance);

            guardTarget = exitPosition + sideOffset;
            guardTarget.y = transform.position.y;
            searchRetargetTimer = exitGuardRetargetInterval;
        }

        Vector3 anchor = GetPlanarPosition(gameManager.ActiveExitZone.transform.position);
        if (Vector3.Distance(GetPlanarPosition(transform.position), anchor) <= exitGuardDistance * 0.55f)
        {
            velocity = GetFlatDirectionTo(guardTarget) * GetCurrentInvestigationSpeed();
            return velocity.sqrMagnitude > 0.001f;
        }

        velocity = GetFlatDirectionTo(gameManager.ActiveExitZone.transform.position) * GetCurrentInvestigationSpeed();
        return velocity.sqrMagnitude > 0.001f;
    }

    private Vector3 ApplyObstacleAvoidance(Vector3 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude <= 0.001f)
        {
            return desiredVelocity;
        }

        Vector3 desiredDirection = desiredVelocity.normalized;
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(controller.height * 0.4f, 0.8f);

        if (!IsObstacleAhead(origin, desiredDirection))
        {
            return desiredVelocity;
        }

        Vector3 leftDirection = Quaternion.Euler(0f, -obstacleSideCheckAngle, 0f) * desiredDirection;
        Vector3 rightDirection = Quaternion.Euler(0f, obstacleSideCheckAngle, 0f) * desiredDirection;

        bool leftBlocked = IsObstacleAhead(origin, leftDirection);
        bool rightBlocked = IsObstacleAhead(origin, rightDirection);

        Vector3 avoidanceDirection;
        if (leftBlocked && !rightBlocked)
        {
            avoidanceDirection = transform.right;
            lastAvoidanceSign = 1;
        }
        else if (!leftBlocked && rightBlocked)
        {
            avoidanceDirection = -transform.right;
            lastAvoidanceSign = -1;
        }
        else
        {
            avoidanceDirection = transform.right * lastAvoidanceSign;
            lastAvoidanceSign *= -1;
        }

        Vector3 steeredDirection = (desiredDirection + avoidanceDirection * obstacleAvoidanceStrength).normalized;
        return steeredDirection * desiredVelocity.magnitude;
    }
}

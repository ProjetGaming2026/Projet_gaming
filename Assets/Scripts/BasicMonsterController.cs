using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BasicMonsterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PrototypeGameManager gameManager;
    [SerializeField] private BasicFirstPersonController playerController;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float slowedMoveSpeed = 3f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float patrolMoveSpeed = 2.4f;
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
    private int patrolPointIndex;
    private float patrolWaitTimer;
    private float stuckTimer;
    private int lastAvoidanceSign = 1;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private float visualCycle;
    private Vector3 lastPosition;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

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

        if (canMove && playerTarget != null)
        {
            Vector3 offsetToPlayer = playerTarget.position - transform.position;
            Vector3 flatOffset = new Vector3(offsetToPlayer.x, 0f, offsetToPlayer.z);
            float distanceToPlayer = flatOffset.magnitude;

            bool playerHidden = playerController != null && playerController.IsHiddenActive;
            bool canDetectNormally = distanceToPlayer <= detectionRadius && !playerHidden;
            bool hasForcedReveal = forcedRevealTimer > 0f;

            if ((canDetectNormally || hasForcedReveal) && distanceToPlayer > stoppingDistance)
            {
                float currentMoveSpeed = IsSlowed ? slowedMoveSpeed : moveSpeed;
                Vector3 chaseDirection = flatOffset.normalized;
                targetVelocity = chaseDirection * currentMoveSpeed;
                isChasing = true;
            }
        }

        if (canMove && !isChasing)
        {
            targetVelocity = GetPatrolVelocity();
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
                turnSpeed * Time.deltaTime
            );
        }

        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
        UpdatePatrolRecovery(targetVelocity, isChasing);
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
            if (patrolWaitTimer >= patrolWaitTime)
            {
                patrolWaitTimer = 0f;
                AdvancePatrolPoint();
            }

            return Vector3.zero;
        }

        patrolWaitTimer = 0f;
        float currentPatrolSpeed = IsSlowed ? Mathf.Min(slowedMoveSpeed, patrolMoveSpeed) : patrolMoveSpeed;
        Vector3 patrolDirection = flatOffset.normalized;

        return patrolDirection * currentPatrolSpeed;
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

    private void UpdatePatrolRecovery(Vector3 targetVelocity, bool isChasing)
    {
        Vector3 planarDelta = transform.position - lastPosition;
        planarDelta.y = 0f;
        lastPosition = transform.position;

        if (isChasing || patrolPoints == null || patrolPoints.Length == 0)
        {
            stuckTimer = 0f;
            return;
        }

        if (targetVelocity.sqrMagnitude > 0.04f && planarDelta.sqrMagnitude < 0.0004f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 1.5f)
            {
                AdvancePatrolPoint();
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
}

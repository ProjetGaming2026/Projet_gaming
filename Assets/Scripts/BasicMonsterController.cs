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
    [SerializeField] private float moveSpeed = 9f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 10f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float forcedRevealTimer;

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
    }

    private void Update()
    {
        if (forcedRevealTimer > 0f)
        {
            forcedRevealTimer -= Time.deltaTime;
        }

        HandleMovement();
    }

    public void RevealPlayerPosition(float duration)
    {
        forcedRevealTimer = Mathf.Max(forcedRevealTimer, duration);
        Debug.Log("Position du joueur revelee a l'Entite pendant " + duration + " secondes.");
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
                Vector3 chaseDirection = flatOffset.normalized;
                targetVelocity = chaseDirection * moveSpeed;

                Quaternion targetRotation = Quaternion.LookRotation(chaseDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }

        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

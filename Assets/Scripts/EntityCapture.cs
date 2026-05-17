using UnityEngine;
using System.Collections;

public class EntityCapture : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float captureDistance = 1.5f;

    private PrototypeGameManager gameManager;
    private MonsterWalkAnimationDriver animationDriver;
    private BasicMonsterController monsterController;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        animationDriver = GetComponent<MonsterWalkAnimationDriver>();
        monsterController = GetComponent<BasicMonsterController>();

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    private void Update()
    {
        if (gameManager == null || playerTarget == null)
        {
            return;
        }

        if (gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= captureDistance)
        {
            Debug.Log("Le joueur a ete capture par le monster.");
            enabled = false;
            StartCoroutine(HandleCaptureSequence());
        }
    }

    private IEnumerator HandleCaptureSequence()
    {
        if (monsterController != null)
        {
            monsterController.enabled = false;
        }

        float delay = 0f;
        if (animationDriver != null)
        {
            animationDriver.PlayCaptureAttack();
            delay = animationDriver.GetRecommendedAttackLeadTime();
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (gameManager != null)
        {
            gameManager.TriggerLoss(PrototypeGameManager.LossReason.Captured);
        }
    }
}

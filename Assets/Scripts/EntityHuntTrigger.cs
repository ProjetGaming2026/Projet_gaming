using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EntityHuntTrigger : MonoBehaviour
{
    [SerializeField] private GameObject entity;
    [SerializeField] private BasicMonsterController monsterController;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float huntDuration = 10f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private AudioSource scareAudio;
    [SerializeField] private Animator entityAnimator;
    [SerializeField] private string huntBoolName = "IsHunting";
    [SerializeField] private Transform fallbackReturnPoint;

    private float lastHuntTime = -999f;
    private Coroutine activeHunt;

    public void Configure(GameObject newEntity, BasicMonsterController newMonsterController)
    {
        entity = newEntity;
        monsterController = newMonsterController;

        if (entity != null)
        {
            agent = entity.GetComponent<NavMeshAgent>();
            entityAnimator = entity.GetComponentInChildren<Animator>();
        }
    }

    public void TriggerHunt(Vector3 playerPosition)
    {
        if (Time.time < lastHuntTime + cooldown)
        {
            Debug.Log("EntityHuntTrigger: hunt ignored because cooldown is active.");
            return;
        }

        ResolveMissingReferences();
        lastHuntTime = Time.time;

        PrototypeGameManager gameManager = FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager != null &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Win &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Lose)
        {
            gameManager.SetPhase(PrototypeGameManager.GamePhase.Chase);
        }

        if (entity != null && !entity.activeSelf)
        {
            entity.SetActive(true);
        }

        if (scareAudio != null)
        {
            scareAudio.Play();
        }

        if (entityAnimator != null && !string.IsNullOrEmpty(huntBoolName))
        {
            entityAnimator.SetBool(huntBoolName, true);
        }

        bool huntMovementTriggered = false;
        if (monsterController != null)
        {
            monsterController.InvestigatePosition(playerPosition, huntDuration, true);
            huntMovementTriggered = true;
            Debug.Log("EntityHuntTrigger: BasicMonsterController investigating the wrong TV code position.");
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(playerPosition);
            huntMovementTriggered = true;
            Debug.Log("EntityHuntTrigger: entity hunting last player position " + playerPosition);
        }

        if (!huntMovementTriggered)
        {
            Debug.LogWarning("EntityHuntTrigger: no BasicMonsterController or NavMeshAgent assigned. Hunt feedback played, but entity cannot move.");
        }

        if (activeHunt != null)
        {
            StopCoroutine(activeHunt);
        }

        activeHunt = StartCoroutine(FinishHuntAfterDelay());
    }

    private void ResolveMissingReferences()
    {
        if (monsterController == null)
        {
            monsterController = FindFirstObjectByType<BasicMonsterController>();
        }

        if (entity == null && monsterController != null)
        {
            entity = monsterController.gameObject;
        }

        if (agent == null && entity != null)
        {
            agent = entity.GetComponent<NavMeshAgent>();
        }

        if (entityAnimator == null && entity != null)
        {
            entityAnimator = entity.GetComponentInChildren<Animator>();
        }
    }

    private IEnumerator FinishHuntAfterDelay()
    {
        yield return new WaitForSeconds(huntDuration);

        if (agent != null)
        {
            if (fallbackReturnPoint != null)
            {
                agent.isStopped = false;
                agent.SetDestination(fallbackReturnPoint.position);
                Debug.Log("EntityHuntTrigger: entity returning to fallback point.");
            }
            else
            {
                agent.isStopped = true;
                Debug.Log("EntityHuntTrigger: entity hunt stopped.");
            }
        }

        if (entityAnimator != null && !string.IsNullOrEmpty(huntBoolName))
        {
            entityAnimator.SetBool(huntBoolName, false);
        }

        activeHunt = null;
    }
}

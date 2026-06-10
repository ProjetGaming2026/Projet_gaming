using UnityEngine;
using UnityEngine.AI;

public static class PuzzleFailureMonsterAlert
{
    private const float DefaultHuntDuration = 60f;

    public static void Alert(Transform failureSource, float huntDuration = DefaultHuntDuration)
    {
        Alert(null, failureSource, huntDuration);
    }

    public static void Alert(GameObject explicitMonster, Transform failureSource, float huntDuration = DefaultHuntDuration)
    {
        PrototypeGameManager gameManager = Object.FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager != null &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Escape &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Win &&
            gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Lose)
        {
            gameManager.SetPhase(PrototypeGameManager.GamePhase.Chase);
        }

        BasicMonsterController monsterController = explicitMonster != null
            ? explicitMonster.GetComponentInChildren<BasicMonsterController>(true)
            : null;

        if (monsterController == null)
        {
            monsterController = Object.FindFirstObjectByType<BasicMonsterController>(FindObjectsInactive.Include);
        }

        if (monsterController != null)
        {
            ActivateHierarchy(explicitMonster != null ? explicitMonster : monsterController.gameObject);
            ActivateHierarchy(monsterController.gameObject);

            monsterController.enabled = true;
            if (!monsterController.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("PuzzleFailureMonsterAlert: monstre trouve, mais sa hierarchie reste inactive.");
            }

            if (failureSource != null)
            {
                monsterController.HuntPlayerAfterPuzzleFailure(huntDuration, failureSource.position);
            }
            else
            {
                monsterController.HuntPlayerAfterPuzzleFailure(huntDuration);
            }

            return;
        }

        AlertLegacyNavMeshMonster(explicitMonster);
    }

    private static void ActivateHierarchy(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Transform current = target.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }

    private static void AlertLegacyNavMeshMonster(GameObject explicitMonster)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("PuzzleFailureMonsterAlert: aucun joueur trouve.");
            return;
        }

        NavMeshAgent agent = explicitMonster != null
            ? explicitMonster.GetComponent<NavMeshAgent>()
            : Object.FindFirstObjectByType<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogWarning("PuzzleFailureMonsterAlert: aucun monstre trouve.");
            return;
        }

        Vector3 lastKnownPlayerPosition = player.transform.position;
        agent.SetDestination(lastKnownPlayerPosition);
        Debug.Log("PuzzleFailureMonsterAlert: legacy monster sent to the player's last known position.");
    }
}

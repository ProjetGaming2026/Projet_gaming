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
            ? explicitMonster.GetComponent<BasicMonsterController>()
            : null;

        if (monsterController == null)
        {
            monsterController = Object.FindFirstObjectByType<BasicMonsterController>();
        }

        if (monsterController != null)
        {
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

        agent.SetDestination(player.transform.position);
    }
}

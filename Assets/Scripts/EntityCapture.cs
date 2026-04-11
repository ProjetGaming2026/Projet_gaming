using UnityEngine;

public class EntityCapture : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float captureDistance = 1.5f;

    private PrototypeGameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();

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
            Debug.Log("Le joueur a ete capture par l'Entite.");
            gameManager.TriggerLoss();
            enabled = false;
        }
    }
}

using UnityEngine;

public class EntityCapture : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float captureDistance = 1.5f;
    [SerializeField] private float captureDelay = 0.95f;
    [SerializeField] private bool lockPlayerDuringCapture = true;

    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private BasicFirstPersonController playerController;
    private bool captureTriggered;
    private float captureTimer;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        monsterController = GetComponent<BasicMonsterController>();

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

        if (captureTriggered)
        {
            captureTimer -= Time.deltaTime;
            if (captureTimer <= 0f)
            {
                Debug.Log("Le joueur a ete capture par le monster.");
                gameManager.TriggerLoss(
                    "L'Entite",
                    "L'Entite t'a dechiquete. Tu n'as pas reussi a sortir vivant de l'hopital."
                );
                enabled = false;
            }

            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        if (distance <= captureDistance)
        {
            captureTriggered = true;
            captureTimer = captureDelay;

            if (monsterController != null)
            {
                monsterController.BeginCapture(playerTarget);
            }

            if (lockPlayerDuringCapture && playerController != null)
            {
                playerController.SetExternalControlLock(true);
            }
        }
    }
}

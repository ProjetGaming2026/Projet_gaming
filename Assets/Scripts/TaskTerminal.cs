using UnityEngine;
using UnityEngine.Events;

public class TaskTerminal : MonoBehaviour, IInteractable
{
    [Header("Task")]
    [SerializeField, Range(0f, 1f)] private float successChance = 0.6f;
    [SerializeField] private bool disableAfterSuccess = true;
    [SerializeField] private float revealDurationOnFailure = 3f;
    [SerializeField] private float overclockDurationOnSuccess = 5f;
    [SerializeField] private float hiddenDurationOnSuccess = 5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFailure;

    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private BasicFirstPersonController playerController;
    private bool completed;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        monsterController = FindFirstObjectByType<BasicMonsterController>();
        playerController = FindFirstObjectByType<BasicFirstPersonController>();
    }

    public void Interact()
    {
        if (completed)
        {
            return;
        }

        if (gameManager == null || gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase)
        {
            Debug.Log("Le terminal ne peut etre utilise qu'en phase CHASE.");
            return;
        }

        bool success = Random.value <= successChance;

        if (success)
        {
            completed = true;
            gameManager.RegisterTaskSuccess();

            if (playerController != null)
            {
                playerController.GiveRandomGlitch(overclockDurationOnSuccess, hiddenDurationOnSuccess);
            }

            onSuccess.Invoke();
            Debug.Log("Terminal reussi. Glitch aleatoire obtenu.");

            if (disableAfterSuccess)
            {
                enabled = false;
            }
        }
        else
        {
            onFailure.Invoke();

            if (monsterController != null)
            {
                monsterController.RevealPlayerPosition(revealDurationOnFailure);
            }

            Debug.Log("Terminal echoue. Position revelee temporairement.");
        }
    }
}

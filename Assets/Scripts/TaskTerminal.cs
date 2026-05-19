using UnityEngine;
using UnityEngine.Events;

public class TaskTerminal : MonoBehaviour, IInteractable
{
    [Header("Task")]
    [SerializeField, Range(0f, 1f)] private float successChance = 0.6f;
    [SerializeField] private float revealDurationOnFailure = 3f;
    [SerializeField] private float suspicionDurationOnUse = 5f;
    [SerializeField] private float failureSuspicionDuration = 8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFailure;

    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private bool firstSuccessCompleted;

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    public void RefreshRuntimeReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        if (monsterController == null)
        {
            monsterController = FindFirstObjectByType<BasicMonsterController>();
        }
    }

    public void Interact()
    {
        if (gameManager == null || gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase)
        {
            PrototypeHUD.ShowFeedback("Le terminal n'est pas disponible pour l'instant.", PrototypeHUD.FeedbackTone.Warning, 2.2f);
            Debug.Log("Le terminal ne peut etre utilise qu'en phase CHASE.");
            return;
        }

        bool success = Random.value <= successChance;

        if (success)
        {
            gameManager.RegisterTaskSuccess();

            if (monsterController != null)
            {
                monsterController.NotifySuspiciousActivity(transform.position, suspicionDurationOnUse, false);
            }

            if (!firstSuccessCompleted)
            {
                firstSuccessCompleted = true;
                onSuccess.Invoke();
            }

            Debug.Log("Terminal reussi. Progression mise a jour.");
            PrototypeHUD.ShowFeedback("Tache reussie. Progression mise a jour.", PrototypeHUD.FeedbackTone.Success, 2.8f);
        }
        else
        {
            onFailure.Invoke();

            if (monsterController != null)
            {
                monsterController.RevealPlayerPosition(revealDurationOnFailure);
                monsterController.NotifySuspiciousActivity(transform.position, failureSuspicionDuration, true);
            }

            Debug.Log("Terminal echoue. Position revelee temporairement.");
            PrototypeHUD.ShowFeedback("Echec du terminal. L'entite a ete alertee.", PrototypeHUD.FeedbackTone.Failure, 2.8f);
        }
    }
}

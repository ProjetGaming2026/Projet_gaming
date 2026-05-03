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
    [SerializeField] private float suspicionDurationOnUse = 5f;
    [SerializeField] private float failureSuspicionDuration = 8f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFailure;

    private PrototypeGameManager gameManager;
    private BasicMonsterController monsterController;
    private BasicFirstPersonController playerController;
    private bool completed;

    public bool IsCompleted => completed;

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

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }
    }

    public void Interact()
    {
        if (completed)
        {
            PrototypeHUD.ShowFeedback("Ce terminal est deja termine.", PrototypeHUD.FeedbackTone.Info, 2.1f);
            return;
        }

        if (gameManager == null || gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase)
        {
            PrototypeHUD.ShowFeedback("Le terminal n'est pas disponible pour l'instant.", PrototypeHUD.FeedbackTone.Warning, 2.2f);
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

            if (monsterController != null)
            {
                monsterController.NotifySuspiciousActivity(transform.position, suspicionDurationOnUse, false);
            }

            onSuccess.Invoke();
            Debug.Log("Terminal reussi. Glitch aleatoire obtenu.");
            string successMessage = playerController != null
                ? "Tache reussie. Glitch obtenu : " + playerController.LastReceivedGlitch
                : "Tache reussie.";
            PrototypeHUD.ShowFeedback(successMessage, PrototypeHUD.FeedbackTone.Success, 2.8f);

            if (disableAfterSuccess)
            {
                DisableInteractionColliders();
                enabled = false;
            }
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

    private void DisableInteractionColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}

using UnityEngine;

public class PrototypeGameManager : MonoBehaviour
{
    public enum GamePhase
    {
        Incubation,
        Chase,
        Escape,
        Win,
        Lose
    }

    [Header("Phase Settings")]
    [SerializeField] private float incubationDuration = 60f;

    [Header("Objectives")]
    [SerializeField] private int requiredTasks = 5;
    [SerializeField] private ExitZone[] exitZones;

    public GamePhase CurrentPhase { get; private set; }
    public int CompletedTasks { get; private set; }
    public int RequiredTasks => requiredTasks;
    public ExitZone ActiveExitZone { get; private set; }

    private float phaseTimer;

    private void Start()
    {
        DisableAllExits();
        SetPhase(GamePhase.Incubation);
    }

    private void Update()
    {
        if (CurrentPhase == GamePhase.Incubation)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f)
            {
                SetPhase(GamePhase.Chase);
            }
        }
    }

    public void RegisterTaskSuccess()
    {
        if (CurrentPhase != GamePhase.Chase)
        {
            return;
        }

        CompletedTasks++;
        Debug.Log("Tâches réussies : " + CompletedTasks + " / " + requiredTasks);

        if (CompletedTasks >= requiredTasks)
        {
            ActivateRandomExit();
            SetPhase(GamePhase.Escape);
        }
    }

    public void TriggerLoss()
    {
        SetPhase(GamePhase.Lose);
    }

    public void TriggerWin()
    {
        SetPhase(GamePhase.Win);
    }

    public void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        switch (CurrentPhase)
        {
            case GamePhase.Incubation:
                phaseTimer = incubationDuration;
                Debug.Log("Phase actuelle : INCUBATION");
                break;

            case GamePhase.Chase:
                Debug.Log("Phase actuelle : CHASE");
                break;

            case GamePhase.Escape:
                Debug.Log("Phase actuelle : ESCAPE");
                break;

            case GamePhase.Win:
                Debug.Log("Phase actuelle : WIN");
                break;

            case GamePhase.Lose:
                Debug.Log("Phase actuelle : LOSE");
                break;
        }
    }

    private void ActivateRandomExit()
    {
        DisableAllExits();

        if (exitZones == null || exitZones.Length == 0)
        {
            Debug.LogWarning("Aucune sortie assignée au GameManager.");
            return;
        }

        int randomIndex = Random.Range(0, exitZones.Length);
        ActiveExitZone = exitZones[randomIndex];
        ActiveExitZone.ActivateExit();

        Debug.Log("Une sortie aléatoire a été activée : " + ActiveExitZone.name);
    }

    private void DisableAllExits()
    {
        if (exitZones == null)
        {
            return;
        }

        foreach (ExitZone exitZone in exitZones)
        {
            if (exitZone != null)
            {
                exitZone.DeactivateExit();
            }
        }

        ActiveExitZone = null;
    }
}

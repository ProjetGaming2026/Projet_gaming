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

    [Header("Session")]
    [SerializeField] private float totalLevelDuration = 0f;

    [Header("Objectives")]
    [SerializeField] private int requiredTasks = 5;
    [SerializeField] private ExitZone[] exitZones;

    public GamePhase CurrentPhase { get; private set; }
    public int CompletedTasks { get; private set; }
    public int RequiredTasks => requiredTasks;
    public ExitZone ActiveExitZone { get; private set; }
    public float PhaseTimeRemaining => Mathf.Max(0f, phaseTimer);
    public bool HasTimeLimit => totalLevelDuration > 0f;
    public float TimeRemaining => Mathf.Max(0f, levelTimer);
    public bool HasConfiguredExitZones => exitZones != null && exitZones.Length > 0;

    private float phaseTimer;
    private float levelTimer;

    private void Start()
    {
        levelTimer = totalLevelDuration;
        DisableAllExits();
        SetPhase(GamePhase.Incubation);
    }

    private void Update()
    {
        UpdateLevelTimer();

        if (CurrentPhase == GamePhase.Incubation)
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f)
            {
                SetPhase(GamePhase.Chase);
            }
        }
    }

    public void ApplyLevelTimeLimit(float duration)
    {
        totalLevelDuration = Mathf.Max(0f, duration);
        levelTimer = totalLevelDuration;
    }

    public void AssignExitZones(ExitZone[] newExitZones)
    {
        exitZones = newExitZones;
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

    private void UpdateLevelTimer()
    {
        if (!HasTimeLimit || CurrentPhase == GamePhase.Win || CurrentPhase == GamePhase.Lose)
        {
            return;
        }

        levelTimer -= Time.deltaTime;
        if (levelTimer > 0f)
        {
            return;
        }

        levelTimer = 0f;
        Debug.Log("Temps ecoule. Partie perdue.");
        PrototypeHUD.ShowFeedback("Temps ecoule. L'entite te rattrape.", PrototypeHUD.FeedbackTone.Failure, 3f);
        TriggerLoss();
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

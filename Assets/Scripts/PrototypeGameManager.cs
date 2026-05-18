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
    [SerializeField] private float totalLevelDuration;

    [Header("Objectives")]
    [SerializeField] private int requiredTasks = 5;
    [SerializeField] private ExitZone[] exitZones;

    [Header("End State")]
    [SerializeField] private bool freezeTimeOnEnd = true;

    public GamePhase CurrentPhase { get; private set; }
    public int CompletedTasks { get; private set; }
    public int RequiredTasks => requiredTasks;
    public ExitZone ActiveExitZone { get; private set; }
    public float PhaseTimeRemaining => Mathf.Max(0f, phaseTimer);
    public bool HasTimeLimit => totalLevelDuration > 0f;
    public float TimeRemaining => Mathf.Max(0f, levelTimer);
    public bool HasConfiguredExitZones => exitZones != null && exitZones.Length > 0;
    public string LastLossKillerName { get; private set; } = "L'Entite";
    public string LastLossMessage { get; private set; } = "L'Entite t'a rattrape.";
    public string LastWinMessage { get; private set; } = "Tu as survecu.";

    private float phaseTimer;
    private float levelTimer;
    private BasicFirstPersonController playerController;

    private void Awake()
    {
        Time.timeScale = 1f;
        playerController = FindFirstObjectByType<BasicFirstPersonController>();
    }

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

    public void ApplyRequiredTasks(int taskCount)
    {
        requiredTasks = Mathf.Max(1, taskCount);
    }

    public void AssignExitZones(ExitZone[] newExitZones)
    {
        exitZones = newExitZones;
    }

    public void RegisterTaskSuccess()
    {
        if (CurrentPhase == GamePhase.Escape ||
            CurrentPhase == GamePhase.Win ||
            CurrentPhase == GamePhase.Lose)
        {
            return;
        }

        CompletedTasks = Mathf.Min(requiredTasks, CompletedTasks + 1);
        Debug.Log("Taches reussies : " + CompletedTasks + " / " + requiredTasks);

        if (CurrentPhase == GamePhase.Chase && CompletedTasks >= requiredTasks)
        {
            ActivateRandomExit();
            SetPhase(GamePhase.Escape);
        }
    }

    public void TriggerLoss(string killerName = "L'Entite", string message = null)
    {
        LastLossKillerName = string.IsNullOrWhiteSpace(killerName) ? "L'Entite" : killerName;
        LastLossMessage = string.IsNullOrWhiteSpace(message)
            ? LastLossKillerName + " t'a massacre dans l'hopital."
            : message;
        SetPhase(GamePhase.Lose);
    }

    public void TriggerWin(string message = null)
    {
        LastWinMessage = string.IsNullOrWhiteSpace(message)
            ? "Tu as survecu. L'evacuation est reussie."
            : message;
        SetPhase(GamePhase.Win);
    }

    public void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        ApplyEndStateLock(false);
        if (CurrentPhase != GamePhase.Win && CurrentPhase != GamePhase.Lose)
        {
            Time.timeScale = 1f;
        }

        switch (CurrentPhase)
        {
            case GamePhase.Incubation:
                phaseTimer = incubationDuration;
                Debug.Log("Phase actuelle : INCUBATION");
                break;

            case GamePhase.Chase:
                Debug.Log("Phase actuelle : CHASE");
                if (CompletedTasks >= requiredTasks)
                {
                    ActivateRandomExit();
                    SetPhase(GamePhase.Escape);
                    return;
                }
                break;

            case GamePhase.Escape:
                Debug.Log("Phase actuelle : ESCAPE");
                break;

            case GamePhase.Win:
                Debug.Log("Phase actuelle : WIN");
                ApplyEndStateLock(true);
                break;

            case GamePhase.Lose:
                Debug.Log("Phase actuelle : LOSE");
                ApplyEndStateLock(true);
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
        TriggerLoss("L'Entite", "Le temps est ecoule. L'Entite t'a retrouve et t'a acheve.");
    }

    private void ActivateRandomExit()
    {
        DisableAllExits();

        if (exitZones == null || exitZones.Length == 0)
        {
            Debug.LogWarning("Aucune sortie assignee au GameManager.");
            return;
        }

        int randomIndex = Random.Range(0, exitZones.Length);
        ActiveExitZone = exitZones[randomIndex];
        ActiveExitZone.ActivateExit();

        Debug.Log("Une sortie aleatoire a ete activee : " + ActiveExitZone.name);
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

    private void ApplyEndStateLock(bool isEnding)
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (playerController != null)
        {
            playerController.SetExternalControlLock(isEnding);
        }

        if (freezeTimeOnEnd)
        {
            Time.timeScale = isEnding ? 0f : 1f;
        }
    }
}

using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    public enum FeedbackTone
    {
        Info,
        Success,
        Warning,
        Failure
    }

    [SerializeField] private PrototypeGameManager gameManager;

    private static PrototypeHUD instance;

    private BasicFirstPersonController playerController;
    private PlayerInteractor playerInteractor;
    private BasicMonsterController monsterController;
    private Transform playerTransform;
    private Transform monsterTransform;

    private GUIStyle textStyle;
    private GUIStyle glitchStyle;
    private GUIStyle dangerStyle;
    private GUIStyle endStyle;
    private GUIStyle objectiveTitleStyle;
    private GUIStyle objectiveBodyStyle;
    private GUIStyle promptStyle;
    private GUIStyle feedbackStyle;

    private string feedbackMessage;
    private FeedbackTone feedbackTone;
    private float feedbackUntilTime;

    private void Awake()
    {
        instance = this;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        playerController = FindFirstObjectByType<BasicFirstPersonController>();
        playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        monsterController = FindFirstObjectByType<BasicMonsterController>();

        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }

        if (monsterController != null)
        {
            monsterTransform = monsterController.transform;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void ShowFeedback(string message, FeedbackTone tone, float duration = 2.4f)
    {
        if (instance == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        instance.feedbackMessage = message;
        instance.feedbackTone = tone;
        instance.feedbackUntilTime = Time.unscaledTime + Mathf.Max(0.5f, duration);
    }

    private void OnGUI()
    {
        if (gameManager == null)
        {
            return;
        }

        InitializeStyles();

        string hudText =
            "Phase : " + gameManager.CurrentPhase + "\n" +
            "Taches : " + gameManager.CompletedTasks + " / " + gameManager.RequiredTasks;

        if (gameManager.HasTimeLimit)
        {
            hudText += "\nTemps restant : " + FormatDuration(gameManager.TimeRemaining);
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Incubation)
        {
            hudText += "\nReveil de l'entite : " + gameManager.PhaseTimeRemaining.ToString("F0") + "s";
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape &&
            gameManager.ActiveExitZone != null &&
            gameManager.ActiveExitZone.IsActive)
        {
            if (gameManager.ActiveExitZone.PlayerInside)
            {
                hudText += "\nExtraction : " + gameManager.ActiveExitZone.EscapeTimeRemaining.ToString("F1") + "s";
            }
            else
            {
                hudText += "\nExtraction : trouvez et tenez la sortie";
            }
        }

        if (playerController != null)
        {
            if (playerController.IsPhaseActive)
            {
                hudText += "\nGlitch actif : PhaseCharge " + playerController.PhaseTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsOverclockActive)
            {
                hudText += "\nGlitch actif : Overclock " + playerController.OverclockTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.IsHiddenActive)
            {
                hudText += "\nGlitch actif : Hidden " + playerController.HiddenTimeRemaining.ToString("F1") + "s";
            }
            else if (playerController.HasPhaseCharge)
            {
                hudText += "\nGlitch en stock : PhaseCharge x" + playerController.PhaseChargeCount + " (F)";
            }
            else if (playerController.HasStaticTrap)
            {
                hudText += "\nGlitch en stock : StaticTrap x" + playerController.StaticTrapCount + " (G)";
            }
            else
            {
                hudText += "\nGlitch : Aucun";
            }
        }

        GUI.Box(new Rect(20, 20, 540, 250), "");
        GUI.Label(new Rect(35, 35, 500, 190), hudText, textStyle);
        DrawObjectivePanel();

        if (playerController != null && playerController.LastReceivedGlitch != BasicFirstPersonController.GlitchType.None)
        {
            string lastGlitchText = "Glitch recu : " + playerController.LastReceivedGlitch;
            GUI.Label(new Rect(Screen.width / 2 - 180, 40, 360, 50), lastGlitchText, glitchStyle);
        }

        DrawInteractionPrompt();
        DrawFeedbackMessage();
        DrawDangerWarning();
        DrawEndMessage();
    }

    private void InitializeStyles()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 28;
            textStyle.normal.textColor = Color.white;
        }

        if (glitchStyle == null)
        {
            glitchStyle = new GUIStyle(GUI.skin.label);
            glitchStyle.fontSize = 32;
            glitchStyle.normal.textColor = Color.yellow;
            glitchStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (dangerStyle == null)
        {
            dangerStyle = new GUIStyle(GUI.skin.label);
            dangerStyle.fontSize = 36;
            dangerStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (endStyle == null)
        {
            endStyle = new GUIStyle(GUI.skin.label);
            endStyle.fontSize = 56;
            endStyle.fontStyle = FontStyle.Bold;
            endStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (objectiveTitleStyle == null)
        {
            objectiveTitleStyle = new GUIStyle(GUI.skin.label);
            objectiveTitleStyle.fontSize = 24;
            objectiveTitleStyle.fontStyle = FontStyle.Bold;
            objectiveTitleStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
        }

        if (objectiveBodyStyle == null)
        {
            objectiveBodyStyle = new GUIStyle(GUI.skin.label);
            objectiveBodyStyle.fontSize = 22;
            objectiveBodyStyle.wordWrap = true;
            objectiveBodyStyle.normal.textColor = Color.white;
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label);
            promptStyle.fontSize = 24;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
        }

        if (feedbackStyle == null)
        {
            feedbackStyle = new GUIStyle(GUI.skin.label);
            feedbackStyle.fontSize = 24;
            feedbackStyle.fontStyle = FontStyle.Bold;
            feedbackStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void DrawDangerWarning()
    {
        if (playerTransform == null || monsterTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, monsterTransform.position);

        if (distance <= 4f)
        {
            dangerStyle.normal.textColor = Color.red;
            GUI.Label(
                new Rect(Screen.width / 2 - 220, Screen.height - 120, 440, 50),
                "DANGER TRES PROCHE",
                dangerStyle
            );
        }
        else if (distance <= 8f)
        {
            dangerStyle.normal.textColor = Color.yellow;
            GUI.Label(
                new Rect(Screen.width / 2 - 180, Screen.height - 120, 360, 50),
                "Danger proche",
                dangerStyle
            );
        }
    }

    private void DrawObjectivePanel()
    {
        string objectiveText = BuildObjectiveText();
        GUI.Box(new Rect(20, 285, 540, 120), "");
        GUI.Label(new Rect(35, 298, 180, 28), "Objectif Actuel", objectiveTitleStyle);
        GUI.Label(new Rect(35, 332, 500, 58), objectiveText, objectiveBodyStyle);
    }

    private string BuildObjectiveText()
    {
        switch (gameManager.CurrentPhase)
        {
            case PrototypeGameManager.GamePhase.Incubation:
                return "Survis jusqu'au reveil de l'entite et prepare-toi a explorer l'hopital.";

            case PrototypeGameManager.GamePhase.Chase:
            {
                int remainingTasks = Mathf.Max(0, gameManager.RequiredTasks - gameManager.CompletedTasks);
                return remainingTasks > 0
                    ? "Active les terminaux restants. Encore " + remainingTasks + " tache(s) a valider."
                    : "Toutes les taches sont completees. Cherche la sortie active.";
            }

            case PrototypeGameManager.GamePhase.Escape:
                return gameManager.ActiveExitZone != null && gameManager.ActiveExitZone.IsActive
                    ? "Rejoins la sortie active et reste dans la zone jusqu'a l'extraction."
                    : "Trouve la sortie active pour t'echapper.";

            case PrototypeGameManager.GamePhase.Win:
                return "Tu as survecu. L'evacuation est reussie.";

            case PrototypeGameManager.GamePhase.Lose:
                return "L'entite t'a rattrape. Relance une partie pour tenter une autre approche.";

            default:
                return "Explore l'hopital et reste en vie.";
        }
    }

    private void DrawInteractionPrompt()
    {
        if (playerInteractor == null || !playerInteractor.HasInteractableTarget || string.IsNullOrWhiteSpace(playerInteractor.CurrentPromptText))
        {
            return;
        }

        Rect promptRect = new Rect(Screen.width * 0.5f - 240f, Screen.height - 180f, 480f, 42f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.Box(new Rect(promptRect.x - 10f, promptRect.y - 8f, promptRect.width + 20f, promptRect.height + 16f), GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(promptRect, playerInteractor.CurrentPromptText, promptStyle);
    }

    private void DrawFeedbackMessage()
    {
        if (string.IsNullOrWhiteSpace(feedbackMessage) || Time.unscaledTime > feedbackUntilTime)
        {
            return;
        }

        feedbackStyle.normal.textColor = ResolveFeedbackColor(feedbackTone);

        Rect messageRect = new Rect(Screen.width * 0.5f - 280f, 95f, 560f, 36f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.52f);
        GUI.Box(new Rect(messageRect.x - 12f, messageRect.y - 8f, messageRect.width + 24f, messageRect.height + 16f), GUIContent.none);
        GUI.color = previousColor;
        GUI.Label(messageRect, feedbackMessage, feedbackStyle);
    }

    private static Color ResolveFeedbackColor(FeedbackTone tone)
    {
        switch (tone)
        {
            case FeedbackTone.Success:
                return new Color(0.45f, 1f, 0.55f);
            case FeedbackTone.Warning:
                return new Color(1f, 0.84f, 0.28f);
            case FeedbackTone.Failure:
                return new Color(1f, 0.4f, 0.4f);
            default:
                return Color.white;
        }
    }

    private static string FormatDuration(float duration)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, duration));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void DrawEndMessage()
    {
        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Win)
        {
            endStyle.normal.textColor = Color.green;
            GUI.Label(
                new Rect(Screen.width / 2 - 250, Screen.height / 2 - 50, 500, 100),
                "VICTOIRE",
                endStyle
            );
        }
        else if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose)
        {
            endStyle.normal.textColor = Color.red;
            GUI.Label(
                new Rect(Screen.width / 2 - 250, Screen.height / 2 - 50, 500, 100),
                "DEFAITE",
                endStyle
            );
        }
    }
}

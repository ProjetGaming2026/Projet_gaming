using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    public enum FeedbackTone
    {
        Info,
        Warning,
        Danger,
        Success,
        Failure
    }

    [SerializeField] private PrototypeGameManager gameManager;

    private static PrototypeHUD activeInstance;
    private static string feedbackMessage = string.Empty;
    private static FeedbackTone feedbackTone = FeedbackTone.Info;
    private static float feedbackUntilTime;

    private BasicFirstPersonController playerController;
    private BasicMonsterController monsterController;
    private Transform playerTransform;
    private Transform monsterTransform;

    private GUIStyle textStyle;
    private GUIStyle glitchStyle;
    private GUIStyle dangerStyle;
    private GUIStyle endStyle;

    private void Awake()
    {
        activeInstance = this;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        playerController = FindFirstObjectByType<BasicFirstPersonController>();
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

        GUI.Box(new Rect(14, 14, 300, 120), "");
        GUI.Label(new Rect(24, 24, 270, 96), hudText, textStyle);

        if (playerController != null && playerController.LastReceivedGlitch != BasicFirstPersonController.GlitchType.None)
        {
            string lastGlitchText = "Glitch recu : " + playerController.LastReceivedGlitch;
            GUI.Label(new Rect(Screen.width / 2 - 180, 40, 360, 50), lastGlitchText, glitchStyle);
        }

        DrawDangerWarning();
        DrawFeedback();
        DrawEndMessage();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void InitializeStyles()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 16;
            textStyle.normal.textColor = Color.white;
        }

        if (glitchStyle == null)
        {
            glitchStyle = new GUIStyle(GUI.skin.label);
            glitchStyle.fontSize = 22;
            glitchStyle.normal.textColor = Color.yellow;
            glitchStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (dangerStyle == null)
        {
            dangerStyle = new GUIStyle(GUI.skin.label);
            dangerStyle.fontSize = 24;
            dangerStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (endStyle == null)
        {
            endStyle = new GUIStyle(GUI.skin.label);
            endStyle.fontSize = 42;
            endStyle.fontStyle = FontStyle.Bold;
            endStyle.alignment = TextAnchor.MiddleCenter;
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

    private void DrawFeedback()
    {
        if (string.IsNullOrEmpty(feedbackMessage) || Time.unscaledTime > feedbackUntilTime)
        {
            return;
        }

        Color color = Color.white;
        switch (feedbackTone)
        {
            case FeedbackTone.Warning:
                color = Color.yellow;
                break;
            case FeedbackTone.Success:
                color = Color.green;
                break;
            case FeedbackTone.Failure:
            case FeedbackTone.Danger:
                color = Color.red;
                break;
        }

        dangerStyle.normal.textColor = color;
        GUI.Label(
            new Rect(Screen.width / 2 - 360, Screen.height - 180, 720, 44),
            feedbackMessage,
            dangerStyle
        );
    }

    // Compatibility API used by older gameplay systems merged from main.
    public static void ShowFeedback(string message, FeedbackTone tone, float duration)
    {
        feedbackMessage = message;
        feedbackTone = tone;
        feedbackUntilTime = Time.unscaledTime + Mathf.Max(0.1f, duration);

        if (activeInstance != null)
        {
            Debug.Log("[PrototypeHUD] " + message);
        }
    }

    // Backward-compatible helper for puzzles that still pass an explicit color.
    public void ShowTemporaryStatus(string message, Color color, float duration)
    {
        FeedbackTone tone = FeedbackTone.Info;

        if (color == Color.red)
        {
            tone = FeedbackTone.Failure;
        }
        else if (color == Color.green)
        {
            tone = FeedbackTone.Success;
        }
        else if (color == Color.yellow)
        {
            tone = FeedbackTone.Warning;
        }

        ShowFeedback(message, tone, duration);
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

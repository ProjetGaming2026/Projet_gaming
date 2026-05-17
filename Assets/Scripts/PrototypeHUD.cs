using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    private struct BloodDrip
    {
        public float normalizedX;
        public float stemWidth;
        public float baseLength;
        public float speed;
        public float phaseOffset;
        public float swayAmount;
        public float alpha;
    }

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
    private GUIStyle horrorEndStyle;
    private GUIStyle horrorSubStyle;

    private string feedbackMessage;
    private FeedbackTone feedbackTone;
    private float feedbackUntilTime;
    private float loseOverlayStartTime = -1f;
    private Texture2D cachedWhiteTexture;
    private BloodDrip[] bloodDrips;

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
        UpdateLoseOverlayState();

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

        if (horrorEndStyle == null)
        {
            horrorEndStyle = new GUIStyle(GUI.skin.label);
            horrorEndStyle.fontSize = 70;
            horrorEndStyle.fontStyle = FontStyle.Bold;
            horrorEndStyle.alignment = TextAnchor.MiddleCenter;
            horrorEndStyle.wordWrap = true;
        }

        if (horrorSubStyle == null)
        {
            horrorSubStyle = new GUIStyle(GUI.skin.label);
            horrorSubStyle.fontSize = 24;
            horrorSubStyle.fontStyle = FontStyle.BoldAndItalic;
            horrorSubStyle.alignment = TextAnchor.MiddleCenter;
            horrorSubStyle.wordWrap = true;
        }

        if (cachedWhiteTexture == null)
        {
            cachedWhiteTexture = Texture2D.whiteTexture;
        }

        if (bloodDrips == null || bloodDrips.Length == 0)
        {
            bloodDrips = CreateBloodDrips();
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
        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose &&
            gameManager.CurrentLossReason == PrototypeGameManager.LossReason.Captured)
        {
            DrawCapturedLossScreen();
            return;
        }

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

    private void UpdateLoseOverlayState()
    {
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose)
        {
            if (loseOverlayStartTime < 0f)
            {
                loseOverlayStartTime = Time.unscaledTime;
            }
        }
        else
        {
            loseOverlayStartTime = -1f;
        }
    }

    private void DrawCapturedLossScreen()
    {
        float elapsed = loseOverlayStartTime >= 0f ? Time.unscaledTime - loseOverlayStartTime : 0f;
        float overlayT = Mathf.Clamp01(elapsed / 0.85f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
        float jitterX = Mathf.Sin(Time.unscaledTime * 23f) * 2.5f;
        float jitterY = Mathf.Cos(Time.unscaledTime * 19f) * 1.6f;

        DrawTintedRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.08f, 0f, 0f, Mathf.Lerp(0.25f, 0.72f, overlayT)));
        DrawBloodEdges(overlayT, pulse);

        string title = "L'ENTITE T'A PRISE";
        string subtitle = "Ton souffle s'arrete.\nIl n'y a plus de sortie.";

        Color shadowColor = new Color(0.12f, 0f, 0f, 0.95f);
        Color mainColor = new Color(1f, 0.78f, 0.78f, 1f);
        Color accentColor = new Color(0.78f, 0f, 0f, 0.9f + pulse * 0.1f);

        horrorEndStyle.normal.textColor = mainColor;
        horrorSubStyle.normal.textColor = new Color(1f, 0.86f, 0.86f, 0.94f);

        Rect titleRect = new Rect(Screen.width * 0.5f - 420f, Screen.height * 0.5f - 125f, 840f, 120f);
        Rect subRect = new Rect(Screen.width * 0.5f - 300f, Screen.height * 0.5f + 6f, 600f, 90f);

        DrawGlitchLabel(titleRect, title, horrorEndStyle, shadowColor, accentColor, jitterX, jitterY);
        DrawGlitchLabel(subRect, subtitle, horrorSubStyle, shadowColor, accentColor * 0.85f, jitterX * 0.45f, jitterY * 0.45f);
    }

    private void DrawBloodEdges(float overlayT, float pulse)
    {
        float topHeight = Mathf.Lerp(24f, 130f, overlayT);
        float sideWidth = Mathf.Lerp(18f, 82f, overlayT);
        float bottomHeight = Mathf.Lerp(14f, 72f, overlayT);

        Color deepBlood = new Color(0.35f, 0f, 0f, 0.92f);
        Color darkBlood = new Color(0.2f, 0f, 0f, 0.78f + pulse * 0.08f);
        Color smearBlood = new Color(0.55f, 0.02f, 0.02f, 0.2f + overlayT * 0.18f);

        DrawTintedRect(new Rect(0f, 0f, Screen.width, topHeight), deepBlood);
        DrawTintedRect(new Rect(0f, Screen.height - bottomHeight, Screen.width, bottomHeight), darkBlood);
        DrawTintedRect(new Rect(0f, 0f, sideWidth, Screen.height), darkBlood);
        DrawTintedRect(new Rect(Screen.width - sideWidth, 0f, sideWidth, Screen.height), darkBlood);

        for (int i = 0; i < 8; i++)
        {
            float x = (Screen.width / 7f) * i + Mathf.Sin(i * 1.7f) * 16f;
            float dripWidth = 18f + (i % 3) * 8f;
            float dripHeight = topHeight + 60f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * (1.3f + i * 0.07f))) * 34f;
            DrawTintedRect(new Rect(x, 0f, dripWidth, dripHeight), smearBlood);
        }

        DrawAnimatedBloodDrips(overlayT, pulse);
    }

    private void DrawGlitchLabel(Rect rect, string text, GUIStyle style, Color shadowColor, Color accentColor, float jitterX, float jitterY)
    {
        Color previousColor = GUI.color;

        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + 6f, rect.y + 6f, rect.width, rect.height), text, style);

        style.normal.textColor = accentColor;
        GUI.Label(new Rect(rect.x - 3f + jitterX, rect.y + jitterY, rect.width, rect.height), text, style);

        style.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y - 1f, rect.width, rect.height), text, style);

        GUI.color = previousColor;
    }

    private void DrawTintedRect(Rect rect, Color color)
    {
        if (Event.current.type != EventType.Repaint || cachedWhiteTexture == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, cachedWhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }

    private void DrawAnimatedBloodDrips(float overlayT, float pulse)
    {
        if (bloodDrips == null || bloodDrips.Length == 0)
        {
            return;
        }

        float elapsed = Mathf.Max(0f, Time.unscaledTime - Mathf.Max(0f, loseOverlayStartTime));
        float verticalLimit = Screen.height * 0.58f;

        for (int i = 0; i < bloodDrips.Length; i++)
        {
            BloodDrip drip = bloodDrips[i];
            float travel = Mathf.Repeat(elapsed * drip.speed + drip.phaseOffset, 1f);
            float easedTravel = travel * travel * (2.35f - travel);
            float stemLength = (drip.baseLength + verticalLimit * easedTravel) * overlayT;
            float x = Screen.width * drip.normalizedX + Mathf.Sin(elapsed * (1.1f + drip.phaseOffset * 2.4f)) * drip.swayAmount;
            float width = drip.stemWidth * Mathf.Lerp(0.82f, 1.16f, pulse);

            Color stemColor = new Color(0.42f, 0f, 0f, drip.alpha * Mathf.Lerp(0.42f, 0.9f, overlayT));
            Color bulbColor = new Color(0.62f, 0.03f, 0.03f, Mathf.Min(1f, stemColor.a + 0.08f));

            Rect stemRect = new Rect(x, 0f, width, stemLength);
            DrawTintedRect(stemRect, stemColor);

            float bulbSize = width * 1.85f;
            float bulbY = stemLength - bulbSize * 0.38f;
            DrawTintedRect(new Rect(x - bulbSize * 0.18f, bulbY, bulbSize, bulbSize), bulbColor);
            DrawTintedRect(new Rect(x + bulbSize * 0.16f, bulbY + bulbSize * 0.24f, bulbSize * 0.52f, bulbSize * 0.52f), bulbColor);
        }
    }

    private static BloodDrip[] CreateBloodDrips()
    {
        return new[]
        {
            new BloodDrip { normalizedX = 0.08f, stemWidth = 12f, baseLength = 85f, speed = 0.14f, phaseOffset = 0.05f, swayAmount = 2f, alpha = 0.84f },
            new BloodDrip { normalizedX = 0.16f, stemWidth = 9f, baseLength = 70f, speed = 0.1f, phaseOffset = 0.33f, swayAmount = 3f, alpha = 0.72f },
            new BloodDrip { normalizedX = 0.27f, stemWidth = 14f, baseLength = 92f, speed = 0.17f, phaseOffset = 0.61f, swayAmount = 2.4f, alpha = 0.9f },
            new BloodDrip { normalizedX = 0.39f, stemWidth = 10f, baseLength = 64f, speed = 0.12f, phaseOffset = 0.18f, swayAmount = 2.8f, alpha = 0.76f },
            new BloodDrip { normalizedX = 0.53f, stemWidth = 13f, baseLength = 88f, speed = 0.16f, phaseOffset = 0.47f, swayAmount = 1.7f, alpha = 0.88f },
            new BloodDrip { normalizedX = 0.67f, stemWidth = 11f, baseLength = 76f, speed = 0.11f, phaseOffset = 0.74f, swayAmount = 2.1f, alpha = 0.78f },
            new BloodDrip { normalizedX = 0.81f, stemWidth = 15f, baseLength = 96f, speed = 0.18f, phaseOffset = 0.27f, swayAmount = 2.6f, alpha = 0.92f },
            new BloodDrip { normalizedX = 0.91f, stemWidth = 10f, baseLength = 72f, speed = 0.13f, phaseOffset = 0.89f, swayAmount = 1.9f, alpha = 0.74f },
        };
    }
}

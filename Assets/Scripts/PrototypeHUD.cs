using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    private struct BloodDrop
    {
        public float xNormalized;
        public float startYNormalized;
        public float lengthNormalized;
        public float widthPixels;
        public float speed;
        public float wobble;
        public float phase;
        public float opacity;
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

    private PlayerInteractor playerInteractor;
    private BasicMonsterController monsterController;
    private Transform playerTransform;
    private Transform monsterTransform;

    private GUIStyle textStyle;
    private GUIStyle dangerStyle;
    private GUIStyle endStyle;
    private GUIStyle statusStyle;
    private GUIStyle objectiveTitleStyle;
    private GUIStyle objectiveBodyStyle;
    private GUIStyle promptStyle;
    private GUIStyle feedbackStyle;
    private GUIStyle horrorTitleStyle;
    private GUIStyle horrorBodyStyle;
    private GUIStyle horrorFooterStyle;
    private GUIStyle horrorSystemStyle;
    private GUIStyle horrorCornerStyle;

    private string temporaryStatusMessage;
    private Color temporaryStatusColor = Color.white;
    private float temporaryStatusUntil;
    private string feedbackMessage;
    private FeedbackTone feedbackTone;
    private float feedbackUntilTime;
    private BloodDrop[] loseBloodDrops;
    private bool loseBloodDropsInitialized;
    private Texture2D solidTexture;
    private Texture2D dropBlobTexture;
    private Texture2D bloodEdgeTexture;
    private Texture2D bloodSplatterTexture;
    private Texture2D scanlineTexture;
    private Texture2D vignetteTexture;

    private void Awake()
    {
        instance = this;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<PrototypeGameManager>();
        }

        BasicFirstPersonController playerController = FindFirstObjectByType<BasicFirstPersonController>();
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

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Lose)
        {
            DrawLoseScreen();
            return;
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Win)
        {
            DrawWinScreen();
            return;
        }

        string hudText =
            GameLocalization.Text("Phase: ", "Phase : ", "Fase: ") + GameLocalization.PhaseName(gameManager.CurrentPhase) + "\n" +
            GameLocalization.Text("Tasks: ", "Taches : ", "Tareas: ") + gameManager.CompletedTasks + " / " + gameManager.RequiredTasks;

        if (gameManager.HasTimeLimit)
        {
            hudText += "\n" + GameLocalization.Text("Time left: ", "Temps restant : ", "Tiempo restante: ") + FormatDuration(gameManager.TimeRemaining);
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Incubation)
        {
            hudText += "\n" + GameLocalization.Text("Entity awakening: ", "Reveil de l'entite : ", "Despertar de la entidad: ") + gameManager.PhaseTimeRemaining.ToString("F0") + "s";
        }

        if (gameManager.CurrentPhase == PrototypeGameManager.GamePhase.Escape &&
            gameManager.ActiveExitZone != null &&
            gameManager.ActiveExitZone.IsActive)
        {
            if (gameManager.ActiveExitZone.PlayerInside)
            {
                hudText += "\n" + GameLocalization.Text("Extraction: ", "Extraction : ", "Extraccion: ") + gameManager.ActiveExitZone.EscapeTimeRemaining.ToString("F1") + "s";
            }
            else
            {
                hudText += "\n" + GameLocalization.Text("Extraction: find and hold the exit", "Extraction : trouvez et tenez la sortie", "Extraccion: encuentra y manten la salida");
            }
        }

        GUI.Box(new Rect(20, 20, 540, 250), "");
        GUI.Label(new Rect(35, 35, 500, 190), hudText, textStyle);
        DrawObjectivePanel();

        DrawTemporaryStatus();
        DrawInteractionPrompt();
        DrawFeedbackMessage();
        DrawDangerWarning();
    }

    private void InitializeStyles()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 28;
            textStyle.normal.textColor = Color.white;
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

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.fontSize = 34;
            statusStyle.fontStyle = FontStyle.Bold;
            statusStyle.alignment = TextAnchor.MiddleCenter;
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

        if (horrorTitleStyle == null)
        {
            horrorTitleStyle = new GUIStyle(GUI.skin.label);
            horrorTitleStyle.fontSize = 62;
            horrorTitleStyle.fontStyle = FontStyle.Bold;
            horrorTitleStyle.alignment = TextAnchor.MiddleCenter;
            horrorTitleStyle.normal.textColor = new Color(1f, 0.14f, 0.14f);
        }

        if (horrorBodyStyle == null)
        {
            horrorBodyStyle = new GUIStyle(GUI.skin.label);
            horrorBodyStyle.fontSize = 26;
            horrorBodyStyle.wordWrap = true;
            horrorBodyStyle.alignment = TextAnchor.MiddleCenter;
            horrorBodyStyle.normal.textColor = new Color(1f, 0.9f, 0.9f);
        }

        if (horrorFooterStyle == null)
        {
            horrorFooterStyle = new GUIStyle(GUI.skin.label);
            horrorFooterStyle.fontSize = 22;
            horrorFooterStyle.fontStyle = FontStyle.Bold;
            horrorFooterStyle.alignment = TextAnchor.MiddleCenter;
            horrorFooterStyle.normal.textColor = new Color(0.95f, 0.4f, 0.4f);
        }

        if (horrorSystemStyle == null)
        {
            horrorSystemStyle = new GUIStyle(GUI.skin.label);
            horrorSystemStyle.fontSize = 16;
            horrorSystemStyle.fontStyle = FontStyle.Bold;
            horrorSystemStyle.richText = true;
            horrorSystemStyle.wordWrap = true;
            horrorSystemStyle.normal.textColor = new Color(0.78f, 1f, 0.95f, 0.92f);
        }

        if (horrorCornerStyle == null)
        {
            horrorCornerStyle = new GUIStyle(GUI.skin.label);
            horrorCornerStyle.fontSize = 14;
            horrorCornerStyle.fontStyle = FontStyle.Bold;
            horrorCornerStyle.alignment = TextAnchor.MiddleCenter;
            horrorCornerStyle.normal.textColor = new Color(1f, 0.14f, 0.14f, 0.9f);
        }

        if (solidTexture == null)
        {
            solidTexture = BuildSolidTexture();
        }

        if (dropBlobTexture == null)
        {
            dropBlobTexture = BuildDropBlobTexture();
        }

        if (bloodEdgeTexture == null)
        {
            bloodEdgeTexture = BuildBloodEdgeTexture();
        }

        if (bloodSplatterTexture == null)
        {
            bloodSplatterTexture = BuildBloodSplatterTexture();
        }

        if (scanlineTexture == null)
        {
            scanlineTexture = BuildScanlineTexture();
        }

        if (vignetteTexture == null)
        {
            vignetteTexture = BuildVignetteTexture();
        }
    }

    public void ShowTemporaryStatus(string message, Color color, float duration)
    {
        temporaryStatusMessage = message;
        temporaryStatusColor = color;
        temporaryStatusUntil = Time.time + Mathf.Max(0.1f, duration);
    }

    private void DrawTemporaryStatus()
    {
        if (string.IsNullOrEmpty(temporaryStatusMessage) || Time.time > temporaryStatusUntil)
        {
            return;
        }

        statusStyle.normal.textColor = temporaryStatusColor;
        GUI.Label(
            new Rect(Screen.width / 2 - 240, 90, 480, 50),
            temporaryStatusMessage,
            statusStyle
        );
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
                GameLocalization.Text("DANGER VERY CLOSE", "DANGER TRES PROCHE", "PELIGRO MUY CERCA"),
                dangerStyle
            );
        }
        else if (distance <= 8f)
        {
            dangerStyle.normal.textColor = Color.yellow;
            GUI.Label(
                new Rect(Screen.width / 2 - 180, Screen.height - 120, 360, 50),
                GameLocalization.Text("Danger close", "Danger proche", "Peligro cerca"),
                dangerStyle
            );
        }
    }

    private void DrawObjectivePanel()
    {
        string objectiveText = BuildObjectiveText();
        GUI.Box(new Rect(20, 285, 540, 120), "");
        GUI.Label(new Rect(35, 298, 240, 28), GameLocalization.Text("Current Objective", "Objectif Actuel", "Objetivo Actual"), objectiveTitleStyle);
        GUI.Label(new Rect(35, 332, 500, 58), objectiveText, objectiveBodyStyle);
    }

    private string BuildObjectiveText()
    {
        switch (gameManager.CurrentPhase)
        {
            case PrototypeGameManager.GamePhase.Incubation:
                return GameLocalization.Text(
                    "Survive until the entity wakes up and prepare to explore the hospital.",
                    "Survis jusqu'au reveil de l'entite et prepare-toi a explorer l'hopital.",
                    "Sobrevive hasta que despierte la entidad y preparate para explorar el hospital.");

            case PrototypeGameManager.GamePhase.Chase:
            {
                int remainingTasks = Mathf.Max(0, gameManager.RequiredTasks - gameManager.CompletedTasks);
                return remainingTasks > 0
                    ? GameLocalization.Text(
                        "Activate the remaining terminals. " + remainingTasks + " task(s) left.",
                        "Active les terminaux restants. Encore " + remainingTasks + " tache(s) a valider.",
                        "Activa los terminales restantes. Quedan " + remainingTasks + " tarea(s).")
                    : GameLocalization.Text(
                        "All tasks are complete. Find the active exit.",
                        "Toutes les taches sont completees. Cherche la sortie active.",
                        "Todas las tareas estan completas. Busca la salida activa.");
            }

            case PrototypeGameManager.GamePhase.Escape:
                return gameManager.ActiveExitZone != null && gameManager.ActiveExitZone.IsActive
                    ? GameLocalization.Text(
                        "Reach the active exit and stay in the zone until extraction.",
                        "Rejoins la sortie active et reste dans la zone jusqu'a l'extraction.",
                        "Llega a la salida activa y quedate en la zona hasta la extraccion.")
                    : GameLocalization.Text(
                        "Find the active exit to escape.",
                        "Trouve la sortie active pour t'echapper.",
                        "Encuentra la salida activa para escapar.");

            case PrototypeGameManager.GamePhase.Win:
                return GameLocalization.Text(
                    "You survived. Evacuation complete.",
                    "Tu as survecu. L'evacuation est reussie.",
                    "Has sobrevivido. Evacuacion completada.");

            case PrototypeGameManager.GamePhase.Lose:
                return GameLocalization.Text(
                    "The entity caught you. Restart and try another approach.",
                    "L'entite t'a rattrape. Relance une partie pour tenter une autre approche.",
                    "La entidad te atrapo. Reinicia e intenta otra estrategia.");

            default:
                return GameLocalization.Text(
                    "Explore the hospital and stay alive.",
                    "Explore l'hopital et reste en vie.",
                    "Explora el hospital y sigue con vida.");
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

    private void DrawLoseScreen()
    {
        string killerName = string.IsNullOrWhiteSpace(gameManager.LastLossKillerName)
            ? "L'Entite"
            : gameManager.LastLossKillerName;
        string lossMessage = string.IsNullOrWhiteSpace(gameManager.LastLossMessage)
            ? GameLocalization.Text(
                killerName + " slaughtered you.",
                killerName + " t'a massacre.",
                killerName + " te masacro.")
            : gameManager.LastLossMessage;

        float pulse = 0.86f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.9f)) * 0.14f;
        DrawFullscreenRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0f, 0f, 0.24f));
        DrawTintedTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignetteTexture, new Color(0f, 0f, 0f, 0.92f));
        DrawTintedTexture(new Rect(0f, 0f, Screen.width, Screen.height), scanlineTexture, new Color(0.75f, 0.02f, 0.02f, 0.08f));
        DrawBloodFrame();
        DrawAnimatedBloodDrops();

        float titleWidth = Mathf.Min(980f, Screen.width - 90f);
        Rect titleRect = new Rect((Screen.width - titleWidth) * 0.5f, Screen.height * 0.31f, titleWidth, 128f);
        Rect subtitleRect = new Rect((Screen.width - 640f) * 0.5f, titleRect.yMax + 50f, 640f, 90f);

        DrawGlitchTitle(GameLocalization.Text("YOU FAILED", "VOUS AVEZ ECHOUE", "HAS FALLADO"), titleRect, pulse);
        DrawGlitchSubtitle("GAME OVER", subtitleRect, pulse);

        DrawOffsetLabel(
            GameLocalization.Text("YOU WERE KILLED BY ", "TU AS ETE TUE PAR ", "TE MATO ") + killerName.ToUpperInvariant(),
            new Rect(Screen.width * 0.5f - 330f, titleRect.y - 42f, 660f, 28f),
            horrorCornerStyle,
            Vector2.zero,
            new Color(0.98f, 0.22f, 0.22f, 0.9f)
        );

        string body =
            GameLocalization.Text(
                "Blood was spilled. " + killerName + " found you before the exit.",
                "Le sang a coule. " + killerName + " t'a trouve avant la sortie.",
                "La sangre corrio. " + killerName + " te encontro antes de la salida.") + "\n\n" +
            lossMessage;
        GUI.Label(
            new Rect(Screen.width * 0.5f - 360f, subtitleRect.yMax + 34f, 720f, 118f),
            body,
            horrorBodyStyle
        );

        DrawOffsetLabel(
            GameLocalization.Text("GAME FINISHED", "PARTIE TERMINEE", "PARTIDA TERMINADA"),
            new Rect(Screen.width * 0.5f - 240f, Screen.height - 92f, 480f, 38f),
            horrorCornerStyle,
            Vector2.zero,
            new Color(0.95f, 0.18f, 0.18f, 0.86f)
        );
    }

    private void DrawWinScreen()
    {
        DrawFullscreenRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0.06f, 0.02f, 0.9f));

        float panelWidth = Mathf.Min(780f, Screen.width - 120f);
        float panelHeight = Mathf.Min(320f, Screen.height - 120f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        DrawFullscreenRect(panelRect, new Color(0f, 0f, 0f, 0.6f));
        GUI.Box(panelRect, GUIContent.none);

        endStyle.normal.textColor = new Color(0.45f, 1f, 0.55f);
        GUI.Label(new Rect(panelRect.x, panelRect.y + 48f, panelRect.width, 90f), GameLocalization.Text("VICTORY", "VICTOIRE", "VICTORIA"), endStyle);
        string winMessage = string.IsNullOrWhiteSpace(gameManager.LastWinMessage)
            ? GameLocalization.Text("You survived. Evacuation complete.", "Tu as survecu. L'evacuation est reussie.", "Has sobrevivido. Evacuacion completada.")
            : gameManager.LastWinMessage;
        GUI.Label(
            new Rect(panelRect.x + 48f, panelRect.y + 150f, panelRect.width - 96f, 90f),
            winMessage,
            horrorBodyStyle
        );
    }

    private void DrawBloodFrame()
    {
        float topHeight = Mathf.Min(265f, Screen.height * 0.31f);
        float bottomHeight = Mathf.Min(165f, Screen.height * 0.19f);
        float sideWidth = Mathf.Min(190f, Screen.width * 0.17f);

        DrawTintedTexture(new Rect(0f, 0f, Screen.width, topHeight), bloodEdgeTexture, new Color(0.9f, 0.05f, 0.05f, 0.98f));
        DrawTintedTexture(new Rect(0f, Screen.height - bottomHeight, Screen.width, bottomHeight), bloodEdgeTexture, new Color(0.6f, 0.02f, 0.02f, 0.62f));
        DrawTintedTexture(new Rect(0f, 0f, sideWidth, Screen.height), bloodSplatterTexture, new Color(0.85f, 0.05f, 0.05f, 0.95f));
        DrawTintedTexture(new Rect(Screen.width - sideWidth, 0f, sideWidth, Screen.height), bloodSplatterTexture, new Color(0.85f, 0.05f, 0.05f, 0.95f));
        DrawTintedTexture(new Rect(0f, Screen.height - 240f, Screen.width * 0.28f, 240f), bloodSplatterTexture, new Color(0.62f, 0.02f, 0.02f, 0.9f));
        DrawTintedTexture(new Rect(Screen.width * 0.72f, Screen.height - 250f, Screen.width * 0.28f, 250f), bloodSplatterTexture, new Color(0.62f, 0.02f, 0.02f, 0.9f));
    }

    private void DrawAnimatedBloodDrops()
    {
        EnsureLoseBloodDrops();

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float time = Time.unscaledTime;

        for (int i = 0; i < loseBloodDrops.Length; i++)
        {
            BloodDrop drop = loseBloodDrops[i];
            float headTravel = Mathf.Repeat(time * drop.speed + drop.phase, 1f + drop.lengthNormalized);
            float headY = (drop.startYNormalized + headTravel) * screenHeight;
            float tailY = Mathf.Max(0f, headY - drop.lengthNormalized * screenHeight);
            float x = drop.xNormalized * screenWidth + Mathf.Sin(time * (0.8f + drop.wobble) + drop.phase * 6f) * (4f + drop.wobble * 8f);
            float width = drop.widthPixels;

            if (tailY > screenHeight + 20f)
            {
                continue;
            }

            Color trailColor = new Color(0.5f, 0.01f, 0.01f, 0.32f * drop.opacity);
            Color headColor = new Color(0.74f, 0.04f, 0.04f, 0.88f * drop.opacity);

            DrawFullscreenRect(new Rect(x - width * 0.35f, tailY, width * 0.7f, Mathf.Max(0f, headY - tailY)), trailColor);
            DrawTintedTexture(new Rect(x - width, headY - width * 0.55f, width * 2f, width * 2.2f), dropBlobTexture, headColor);
            DrawTintedTexture(new Rect(x - width * 0.55f, headY + width * 0.8f, width * 1.1f, width * 1.9f), dropBlobTexture, new Color(0.6f, 0.02f, 0.02f, 0.5f * drop.opacity));
        }
    }

    private void EnsureLoseBloodDrops()
    {
        if (loseBloodDropsInitialized)
        {
            return;
        }

        loseBloodDropsInitialized = true;
        loseBloodDrops = new BloodDrop[18];
        System.Random random = new System.Random(4407);

        for (int i = 0; i < loseBloodDrops.Length; i++)
        {
            loseBloodDrops[i] = new BloodDrop
            {
                xNormalized = Mathf.Lerp(0.04f, 0.96f, (float)random.NextDouble()),
                startYNormalized = Mathf.Lerp(-0.34f, -0.04f, (float)random.NextDouble()),
                lengthNormalized = Mathf.Lerp(0.1f, 0.28f, (float)random.NextDouble()),
                widthPixels = Mathf.Lerp(8f, 24f, (float)random.NextDouble()),
                speed = Mathf.Lerp(0.05f, 0.17f, (float)random.NextDouble()),
                wobble = Mathf.Lerp(0.2f, 1.2f, (float)random.NextDouble()),
                phase = Mathf.Lerp(0f, 1f, (float)random.NextDouble()),
                opacity = Mathf.Lerp(0.65f, 1f, (float)random.NextDouble())
            };
        }
    }

    private void DrawFullscreenRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, solidTexture, ScaleMode.StretchToFill, true);
        GUI.color = previousColor;
    }

    private void DrawTintedTexture(Rect rect, Texture texture, Color color)
    {
        if (texture == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previousColor;
    }

    private void DrawGlitchTitle(string title, Rect rect, float pulse)
    {
        Color mainColor = new Color(1f, 0.12f, 0.12f, 0.96f * pulse);
        DrawOffsetLabel(title, rect, horrorTitleStyle, new Vector2(-4f, 1f), new Color(0f, 1f, 0.86f, 0.18f));
        DrawOffsetLabel(title, rect, horrorTitleStyle, new Vector2(4f, -1f), new Color(1f, 0.16f, 0.16f, 0.32f));
        DrawOffsetLabel(title, rect, horrorTitleStyle, Vector2.zero, mainColor);
        DrawTitleBleed(rect, new Color(0.85f, 0.04f, 0.04f, 0.95f));
    }

    private void DrawGlitchSubtitle(string title, Rect rect, float pulse)
    {
        DrawOffsetLabel(title, rect, endStyle, new Vector2(-2f, 0f), new Color(0.75f, 1f, 0.95f, 0.14f));
        DrawOffsetLabel(title, rect, endStyle, Vector2.zero, new Color(0.95f, 0.08f, 0.08f, 0.94f * pulse));
    }

    private void DrawOffsetLabel(string text, Rect rect, GUIStyle style, Vector2 offset, Color color)
    {
        Color previousColor = style.normal.textColor;
        style.normal.textColor = color;
        GUI.Label(new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height), text, style);
        style.normal.textColor = previousColor;
    }

    private void DrawTitleBleed(Rect titleRect, Color color)
    {
        float[] anchors = { 0.16f, 0.31f, 0.49f, 0.67f, 0.82f };
        float[] lengths = { 28f, 18f, 42f, 24f, 35f };
        for (int i = 0; i < anchors.Length; i++)
        {
            float x = titleRect.x + titleRect.width * anchors[i];
            Rect dripRect = new Rect(x, titleRect.yMax - 4f, 4f, lengths[i]);
            DrawFullscreenRect(dripRect, color);
            DrawTintedTexture(new Rect(x - 6f, dripRect.yMax - 10f, 16f, 16f), solidTexture, new Color(color.r, color.g, color.b, 0.92f));
        }
    }

    private void DrawTelemetryPanel(Rect rect, string title, string body, bool alignRight)
    {
        DrawFullscreenRect(rect, new Color(0f, 0f, 0f, 0.5f));
        DrawOutline(rect, new Color(0.8f, 0.12f, 0.12f, 0.72f), 2f);

        Rect titleRect = new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 22f);
        horrorCornerStyle.alignment = alignRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        GUI.Label(titleRect, title, horrorCornerStyle);

        Rect separator = new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 2f);
        DrawFullscreenRect(separator, new Color(0.9f, 0.1f, 0.1f, 0.66f));

        horrorSystemStyle.alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 40f, rect.width - 28f, rect.height - 48f), body, horrorSystemStyle);
    }

    private void DrawNarrativePanel(Rect rect, string body)
    {
        DrawFullscreenRect(rect, new Color(0f, 0f, 0f, 0.58f));
        DrawOutline(rect, new Color(0.95f, 0.1f, 0.1f, 0.55f), 2f);
        GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, rect.width - 48f, rect.height - 36f), body, horrorBodyStyle);
    }

    private void DrawOutline(Rect rect, Color color, float thickness)
    {
        DrawFullscreenRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawFullscreenRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawFullscreenRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawFullscreenRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private Texture2D BuildSolidTexture()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    private Texture2D BuildDropBlobTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.42f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = (x - center.x) / (size * 0.28f);
                float dy = (y - center.y) / (size * 0.34f);
                float dist = dx * dx + dy * dy;
                float alpha = Mathf.Clamp01(1f - dist);

                if (y > size * 0.62f)
                {
                    float tail = Mathf.InverseLerp(size * 0.62f, size * 0.98f, y);
                    float tailWidth = Mathf.Lerp(0.18f, 0.03f, tail) * size;
                    float tailDx = Mathf.Abs(x - center.x) / Mathf.Max(1f, tailWidth);
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(1f - tailDx * tailDx) * (1f - tail * 0.35f));
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private Texture2D BuildBloodEdgeTexture()
    {
        const int width = 1024;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color blood = new Color(0.42f, 0.01f, 0.01f, 1f);
        System.Random random = new System.Random(1481);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int x = 0; x < width; x++)
        {
            float xn = (float)x / (width - 1);
            float topBand = height * (0.1f + Mathf.PerlinNoise(xn * 1.8f, 0.21f) * 0.08f);
            float lumpBand = height * Mathf.PerlinNoise(xn * 7.4f, 1.93f) * 0.08f;
            int fillHeight = Mathf.RoundToInt(topBand + lumpBand);

            for (int y = 0; y < fillHeight; y++)
            {
                float shadeNoise = Mathf.PerlinNoise(xn * 12.5f, y * 0.06f);
                Color pixel = Color.Lerp(blood * 0.72f, blood, shadeNoise * 0.65f);
                pixel.a = Mathf.Lerp(0.82f, 1f, y / Mathf.Max(1f, fillHeight));
                BlendPixel(texture, x, y, pixel);
            }
        }

        for (int i = 0; i < 36; i++)
        {
            float centerX = (float)random.NextDouble() * width;
            float radius = Mathf.Lerp(10f, 34f, (float)random.NextDouble());
            float headY = Mathf.Lerp(8f, 42f, (float)random.NextDouble());
            float dripLength = Mathf.Lerp(32f, 170f, (float)random.NextDouble());
            float wobble = Mathf.Lerp(2f, 12f, (float)random.NextDouble());

            PaintEllipse(texture, centerX, headY, radius * 0.95f, radius * 0.62f, blood, 0.92f);
            PaintDrip(texture, centerX, headY + radius * 0.35f, dripLength, wobble, blood, 0.92f, random);
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private Texture2D BuildBloodSplatterTexture()
    {
        const int width = 320;
        const int height = 512;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color blood = new Color(0.5f, 0.01f, 0.01f, 1f);
        System.Random random = new System.Random(713);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int i = 0; i < 44; i++)
        {
            float centerX = Mathf.Lerp(14f, width * 0.72f, (float)random.NextDouble());
            float centerY = Mathf.Lerp(18f, height * 0.96f, (float)random.NextDouble());
            float radiusX = Mathf.Lerp(10f, 64f, (float)random.NextDouble());
            float radiusY = Mathf.Lerp(12f, 70f, (float)random.NextDouble());
            PaintEllipse(texture, centerX, centerY, radiusX, radiusY, blood, Mathf.Lerp(0.5f, 0.95f, (float)random.NextDouble()));

            int rays = random.Next(2, 6);
            for (int r = 0; r < rays; r++)
            {
                float angle = Mathf.Lerp(-1.4f, 1.4f, (float)random.NextDouble());
                float length = Mathf.Lerp(28f, 120f, (float)random.NextDouble());
                float thickness = Mathf.Lerp(2f, 8f, (float)random.NextDouble());
                PaintStroke(texture, new Vector2(centerX, centerY), new Vector2(centerX + Mathf.Cos(angle) * length, centerY + Mathf.Sin(angle) * length), blood, thickness, 0.8f);
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private Texture2D BuildScanlineTexture()
    {
        const int width = 8;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            float line = y % 4 == 0 ? 0.22f : (y % 2 == 0 ? 0.08f : 0.02f);
            for (int x = 0; x < width; x++)
            {
                float flicker = Mathf.PerlinNoise(x * 0.35f, y * 0.05f) * 0.05f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, line + flicker));
            }
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply();
        return texture;
    }

    private Texture2D BuildVignetteTexture()
    {
        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(Mathf.InverseLerp(0.42f, 1.05f, distance));
                alpha = Mathf.Pow(alpha, 1.65f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private void PaintDrip(Texture2D texture, float x, float y, float length, float wobble, Color color, float alpha, System.Random random)
    {
        float currentX = x;
        float currentY = y;
        float step = 3f;
        int segments = Mathf.CeilToInt(length / step);
        for (int i = 0; i < segments; i++)
        {
            currentY += step;
            currentX += Mathf.Lerp(-wobble, wobble, (float)random.NextDouble()) * 0.22f;
            float radius = Mathf.Lerp(5.5f, 1.8f, i / Mathf.Max(1f, segments - 1f));
            PaintEllipse(texture, currentX, currentY, radius, radius * 1.2f, color, alpha);
        }

        PaintEllipse(texture, currentX, currentY + 8f, 7f, 9f, color, alpha);
    }

    private void PaintStroke(Texture2D texture, Vector2 start, Vector2 end, Color color, float thickness, float alpha)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(start, end));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(start, end, steps == 0 ? 0f : i / (float)steps);
            PaintEllipse(texture, point.x, point.y, thickness, thickness, color, alpha);
        }
    }

    private void PaintEllipse(Texture2D texture, float centerX, float centerY, float radiusX, float radiusY, Color color, float alpha)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(centerX + radiusX));
        int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(centerY + radiusY));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                float dx = (x - centerX) / Mathf.Max(0.0001f, radiusX);
                float dy = (y - centerY) / Mathf.Max(0.0001f, radiusY);
                float dist = dx * dx + dy * dy;
                if (dist > 1f)
                {
                    continue;
                }

                float softness = Mathf.Clamp01(1f - dist);
                Color pixel = Color.Lerp(color * 0.72f, color, softness * 0.8f);
                pixel.a = alpha * softness;
                BlendPixel(texture, x, y, pixel);
            }
        }
    }

    private void BlendPixel(Texture2D texture, int x, int y, Color source)
    {
        Color destination = texture.GetPixel(x, y);
        float outAlpha = source.a + destination.a * (1f - source.a);
        if (outAlpha <= 0.0001f)
        {
            texture.SetPixel(x, y, Color.clear);
            return;
        }

        Color blended =
            (source * source.a + destination * destination.a * (1f - source.a)) / outAlpha;
        blended.a = outAlpha;
        texture.SetPixel(x, y, blended);
    }
}

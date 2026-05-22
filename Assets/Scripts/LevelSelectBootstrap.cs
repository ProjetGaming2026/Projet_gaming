using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectBootstrap : MonoBehaviour
{
    private const string EasySceneName = "demoScene_Facile";
    private const string MediumSceneName = "demoScene_Moyen";
    private const string HardSceneName = "demoScene_Difficile";
    private const string BackSceneName = "MainMenu";

    private const string EasyScenePath = "Assets/Hospital/scenes/demoScene_Facile.unity";
    private const string MediumScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string HardScenePath = "Assets/Hospital/scenes/demoScene_Difficile.unity";
    private const string BackScenePath = "Assets/MainMenu.unity";

    private static LevelSelectBootstrap instance;

    private bool showMenu = true;
    private bool hasChosenLevel;
    private Vector2 windowSize = new Vector2(860f, 720f);

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle hintStyle;
    private GUIStyle backButtonStyle;
    private GUIStyle badgeStyle;
    private static Texture2D pixelTexture;

    public static bool IsMenuOpen => instance != null && instance.showMenu;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        PauseForMenu();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasChosenLevel)
        {
            PauseForMenu();
        }
    }

    private void Update()
    {
        if (!showMenu)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (!showMenu)
        {
            return;
        }

        EnsureStyles();

        DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.45f));
        DrawHorrorVignette(new Rect(0f, 0f, Screen.width, Screen.height));

        float uiScale = Mathf.Clamp(Mathf.Min((Screen.width - 32f) / 700f, (Screen.height - 28f) / 500f), 0.58f, 1.18f);
        Vector2 effectiveWindowSize = new Vector2(
            Mathf.Min(700f * uiScale, Screen.width - 32f),
            Mathf.Min(500f * uiScale, Screen.height - 28f)
        );

        Rect windowRect = new Rect(
            (Screen.width - effectiveWindowSize.x) * 0.5f,
            (Screen.height - effectiveWindowSize.y) * 0.5f,
            effectiveWindowSize.x,
            effectiveWindowSize.y
        );

        DrawPanel(windowRect);

        float titleHeight = 52f * uiScale;
        float buttonWidth = Mathf.Min(520f * uiScale, effectiveWindowSize.x - 56f * uiScale);
        float buttonHeight = 70f * uiScale;
        float gap = 14f * uiScale;
        float stackHeight = titleHeight + 34f * uiScale + buttonHeight * 4f + gap * 3f;
        float startY = windowRect.y + (windowRect.height - stackHeight) * 0.5f;
        float buttonX = windowRect.x + (windowRect.width - buttonWidth) * 0.5f;

        DrawHorrorTitle(new Rect(windowRect.x + 24f * uiScale, startY, windowRect.width - 48f * uiScale, titleHeight), "Choix Du Niveau", uiScale);

        DrawLevelCard(new Rect(buttonX, startY + titleHeight + 34f * uiScale, buttonWidth, buttonHeight), "Facile", EasyScenePath, EasySceneName, new Color(0.25f, 0.88f, 0.56f, 1f), "I");
        DrawLevelCard(new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap), buttonWidth, buttonHeight), "Moyen", MediumScenePath, MediumSceneName, new Color(0.42f, 0.72f, 1f, 1f), "II");
        DrawLevelCard(new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap) * 2f, buttonWidth, buttonHeight), "Difficile", HardScenePath, HardSceneName, new Color(1f, 0.32f, 0.3f, 1f), "III");
        DrawBackButton(new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap) * 3f, buttonWidth, buttonHeight));
    }

    private void DrawLevelCard(Rect cardRect, string label, string scenePath, string sceneName, Color cardColor, string badge)
    {
        bool hovered = cardRect.Contains(Event.current.mousePosition);
        DrawButtonFrame(cardRect, cardColor, hovered, true);

        Rect badgeRect = new Rect(cardRect.x + 14f, cardRect.y + 12f, 48f, cardRect.height - 24f);
        DrawSolidRect(badgeRect, hovered ? new Color(0.62f, 0.02f, 0.02f, 0.95f) : new Color(0.2f, 0.015f, 0.015f, 0.86f));
        DrawSolidRect(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 2f), new Color(cardColor.r, cardColor.g, cardColor.b, 0.75f));
        GUI.Label(badgeRect, badge, badgeStyle);
        GUI.Label(new Rect(cardRect.x + 18f, cardRect.y, cardRect.width - 36f, cardRect.height), label, cardTitleStyle);

        if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
        {
            ChooseLevel(scenePath, sceneName);
        }
    }

    private void DrawBackButton(Rect buttonRect)
    {
        bool hovered = buttonRect.Contains(Event.current.mousePosition);
        DrawButtonFrame(buttonRect, new Color(0.86f, 0.08f, 0.08f, 1f), hovered, false);
        GUI.Label(new Rect(buttonRect.x + 18f, buttonRect.y + 12f, 44f, buttonRect.height - 24f), "<", badgeStyle);

        if (GUI.Button(buttonRect, "RETOUR", backButtonStyle))
        {
            GoBack();
        }
    }

    private void ChooseLevel(string scenePath, string sceneName)
    {
        hasChosenLevel = true;
        showMenu = false;
        ResumeAfterMenu();

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            LoadSceneExact(scenePath, sceneName);
        }
    }

    private void GoBack()
    {
        hasChosenLevel = true;
        showMenu = false;
        ResumeAfterMenu();

        LoadSceneExact(BackScenePath, BackSceneName);
    }

    private void LoadSceneExact(string scenePath, string sceneName)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex >= 0)
        {
            Debug.Log("LevelSelectBootstrap: chargement de " + scenePath);
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            return;
        }

        Debug.LogWarning("LevelSelectBootstrap: scene absente des Build Settings: " + scenePath + ". Fallback par nom: " + sceneName);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void PauseForMenu()
    {
        showMenu = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResumeAfterMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 40;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.96f, 0.94f, 0.9f, 1f);
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label);
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.fontSize = 19;
            subtitleStyle.wordWrap = true;
            subtitleStyle.normal.textColor = Color.white;
        }

        if (cardTitleStyle == null)
        {
            cardTitleStyle = new GUIStyle(GUI.skin.label);
            cardTitleStyle.alignment = TextAnchor.MiddleCenter;
            cardTitleStyle.fontSize = 31;
            cardTitleStyle.fontStyle = FontStyle.Bold;
            cardTitleStyle.normal.textColor = new Color(0.96f, 0.94f, 0.9f, 1f);
        }

        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.fontSize = 16;
            hintStyle.normal.textColor = new Color(0.86f, 0.9f, 0.95f);
        }

        if (backButtonStyle == null)
        {
            backButtonStyle = new GUIStyle(GUI.skin.label);
            backButtonStyle.alignment = TextAnchor.MiddleCenter;
            backButtonStyle.fontSize = 25;
            backButtonStyle.fontStyle = FontStyle.Bold;
            backButtonStyle.normal.textColor = Color.white;
            backButtonStyle.hover.textColor = Color.yellow;
        }

        if (badgeStyle == null)
        {
            badgeStyle = new GUIStyle(GUI.skin.label);
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle.fontSize = 22;
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.normal.textColor = Color.white;
        }
    }

    private static void DrawPanel(Rect rect)
    {
        DrawSolidRect(rect, new Color(0.006f, 0.005f, 0.006f, 0.97f));
        DrawSolidRect(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(0.06f, 0.055f, 0.06f, 0.92f));
        DrawSolidRect(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 2f), new Color(0.8f, 0.03f, 0.025f, 0.62f));
        DrawSolidRect(new Rect(rect.x + 20f, rect.y + rect.height - 20f, rect.width - 40f, 2f), new Color(0.8f, 0.03f, 0.025f, 0.5f));
        DrawSolidRect(new Rect(rect.x, rect.y, 4f, rect.height), new Color(0.8f, 0.03f, 0.025f, 0.55f));
        DrawSolidRect(new Rect(rect.x + rect.width - 4f, rect.y, 4f, rect.height), new Color(0.8f, 0.03f, 0.025f, 0.4f));
        DrawSolidRect(new Rect(rect.x + 24f, rect.y + 24f, 86f, 2f), new Color(0.95f, 0.08f, 0.06f, 0.7f));
        DrawSolidRect(new Rect(rect.x + rect.width - 110f, rect.y + rect.height - 26f, 86f, 2f), new Color(0.95f, 0.08f, 0.06f, 0.55f));
        DrawHorrorScratches(rect);
    }

    private static void DrawButtonFrame(Rect rect, Color accentColor, bool hovered, bool hasBadge)
    {
        Color bodyColor = hovered
            ? new Color(0.16f, 0.045f, 0.048f, 0.98f)
            : new Color(0.035f, 0.028f, 0.032f, 0.93f);

        DrawSolidRect(rect, new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), bodyColor);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.9f, 0.03f, 0.025f, hovered ? 0.9f : 0.44f));
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), new Color(accentColor.r, accentColor.g, accentColor.b, hovered ? 0.55f : 0.24f));
        DrawSolidRect(new Rect(rect.x + rect.width - 6f, rect.y + 8f, 3f, rect.height - 16f), new Color(0.9f, 0.03f, 0.025f, hovered ? 0.84f : 0.34f));
        if (hasBadge)
        {
            DrawSolidRect(new Rect(rect.x + 70f, rect.y + 10f, 1f, rect.height - 20f), new Color(0.9f, 0.03f, 0.025f, 0.42f));
        }

        DrawSolidRect(new Rect(rect.x + 18f, rect.y + rect.height - 9f, rect.width * 0.35f, 1f), new Color(0.9f, 0.03f, 0.025f, hovered ? 0.65f : 0.28f));
        DrawSolidRect(new Rect(rect.x + rect.width * 0.7f, rect.y + 10f, rect.width * 0.2f, 1f), new Color(0.96f, 0.94f, 0.9f, hovered ? 0.22f : 0.08f));
    }

    private static void DrawHorrorTitle(Rect rect, string title, float uiScale)
    {
        float pulse = Mathf.Sin(Time.unscaledTime * 13.7f);
        float glitch = Mathf.Abs(pulse) > 0.82f ? 3f * uiScale : 1f * uiScale;
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(40f * uiScale),
            fontStyle = FontStyle.Bold
        };

        style.normal.textColor = new Color(0.55f, 0f, 0f, 0.68f);
        GUI.Label(new Rect(rect.x - glitch, rect.y + 2f * uiScale, rect.width, rect.height), title, style);
        style.normal.textColor = new Color(0.03f, 0.9f, 0.78f, 0.18f);
        GUI.Label(new Rect(rect.x + glitch, rect.y - 1f * uiScale, rect.width, rect.height), title, style);
        style.normal.textColor = new Color(0.96f, 0.94f, 0.9f, 1f);
        GUI.Label(rect, title, style);
        DrawSolidRect(new Rect(rect.x + rect.width * 0.26f, rect.y + rect.height - 4f * uiScale, rect.width * 0.48f, 2f * uiScale), new Color(0.82f, 0.02f, 0.02f, 0.72f));
    }

    private static void DrawHorrorVignette(Rect rect)
    {
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, rect.height * 0.1f), new Color(0f, 0f, 0f, 0.28f));
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height * 0.9f, rect.width, rect.height * 0.1f), new Color(0f, 0f, 0f, 0.34f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width * 0.08f, rect.height), new Color(0f, 0f, 0f, 0.28f));
        DrawSolidRect(new Rect(rect.x + rect.width * 0.92f, rect.y, rect.width * 0.08f, rect.height), new Color(0f, 0f, 0f, 0.32f));

        for (int i = 0; i < 8; i++)
        {
            float y = rect.y + ((i * 73f + Time.unscaledTime * 18f) % rect.height);
            DrawSolidRect(new Rect(rect.x, y, rect.width, 1f), new Color(0.8f, 0.02f, 0.02f, 0.045f));
        }
    }

    private static void DrawHorrorScratches(Rect rect)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = rect.x + 34f + i * rect.width * 0.13f;
            float y = rect.y + 58f + Mathf.Abs(Mathf.Sin(i * 12.4f)) * (rect.height - 116f);
            float length = 24f + i * 5f;
            DrawSolidRect(new Rect(x, y, 2f, length), new Color(0.9f, 0.03f, 0.025f, 0.09f));
        }
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        if (pixelTexture == null)
        {
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.hideFlags = HideFlags.HideAndDontSave;
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixelTexture);
        GUI.color = previousColor;
    }
}

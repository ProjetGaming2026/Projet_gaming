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
    private GUIStyle cardDescriptionStyle;
    private GUIStyle tagStyle;
    private GUIStyle hintStyle;
    private GUIStyle backButtonStyle;

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

        Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.04f, 0.06f, 0.45f);
        GUI.Box(overlay, GUIContent.none);
        GUI.color = previousColor;

        Vector2 effectiveWindowSize = new Vector2(
            Mathf.Min(windowSize.x, Screen.width - 80f),
            Mathf.Min(windowSize.y, Screen.height - 80f)
        );

        Rect windowRect = new Rect(
            (Screen.width - effectiveWindowSize.x) * 0.5f,
            (Screen.height - effectiveWindowSize.y) * 0.5f,
            effectiveWindowSize.x,
            effectiveWindowSize.y
        );

        GUILayout.BeginArea(windowRect, GUI.skin.window);

        GUILayout.Space(24f);
        GUILayout.Label("Choix Du Niveau", titleStyle);
        GUILayout.Space(12f);
        GUILayout.Label("Selectionne la difficulte avant d'entrer dans l'hopital.", subtitleStyle);
        GUILayout.Space(24f);

        DrawLevelCard("Facile", "Ideal pour decouvrir la map et les objectifs.", "13 min de jeu, plus de marge, monstre plus lent.", EasyScenePath, EasySceneName, new Color(0.18f, 0.36f, 0.22f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(14f);

        DrawLevelCard("Moyen", "La version la plus equilibree du prototype.", "10 min de jeu, bon rythme entre exploration, poursuite et extraction.", MediumScenePath, MediumSceneName, new Color(0.18f, 0.24f, 0.34f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(14f);

        DrawLevelCard("Difficile", "Pour une vraie partie sous pression.", "8 min de jeu, monstre plus agressif, erreurs plus couteuses.", HardScenePath, HardSceneName, new Color(0.36f, 0.16f, 0.16f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(18f);

        GUILayout.Label("Clique sur une carte pour commencer la partie.", hintStyle);
        GUILayout.Space(18f);

        if (GUILayout.Button("BACK", backButtonStyle, GUILayout.Height(64f)))
        {
            GoBack();
        }

        GUILayout.EndArea();
    }

    private void DrawLevelCard(string label, string tagline, string description, string scenePath, string sceneName, Color cardColor, float width)
    {
        Rect cardRect = GUILayoutUtility.GetRect(width, 118f, GUILayout.ExpandWidth(true));
        bool hovered = cardRect.Contains(Event.current.mousePosition);

        Color previousColor = GUI.color;
        GUI.color = hovered
            ? new Color(cardColor.r + 0.08f, cardColor.g + 0.08f, cardColor.b + 0.08f, 0.98f)
            : cardColor;

        GUI.Box(cardRect, GUIContent.none);
        GUI.color = previousColor;

        GUI.Label(new Rect(cardRect.x + 18f, cardRect.y + 14f, cardRect.width - 36f, 30f), label, cardTitleStyle);
        GUI.Label(new Rect(cardRect.x + 18f, cardRect.y + 48f, cardRect.width - 36f, 22f), tagline, tagStyle);
        GUI.Label(new Rect(cardRect.x + 18f, cardRect.y + 74f, cardRect.width - 36f, 34f), description, cardDescriptionStyle);

        if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
        {
            ChooseLevel(scenePath, sceneName);
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
            titleStyle.fontSize = 38;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
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
            cardTitleStyle.fontSize = 28;
            cardTitleStyle.fontStyle = FontStyle.Bold;
            cardTitleStyle.normal.textColor = Color.white;
        }

        if (cardDescriptionStyle == null)
        {
            cardDescriptionStyle = new GUIStyle(GUI.skin.label);
            cardDescriptionStyle.fontSize = 18;
            cardDescriptionStyle.wordWrap = true;
            cardDescriptionStyle.normal.textColor = new Color(0.93f, 0.95f, 0.98f);
        }

        if (tagStyle == null)
        {
            tagStyle = new GUIStyle(GUI.skin.label);
            tagStyle.fontSize = 17;
            tagStyle.fontStyle = FontStyle.Italic;
            tagStyle.normal.textColor = new Color(0.95f, 0.9f, 0.6f);
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
            backButtonStyle = new GUIStyle(GUI.skin.button);
            backButtonStyle.fontSize = 34;
            backButtonStyle.fontStyle = FontStyle.Bold;
            backButtonStyle.normal.textColor = Color.white;
            backButtonStyle.hover.textColor = Color.yellow;
        }
    }
}

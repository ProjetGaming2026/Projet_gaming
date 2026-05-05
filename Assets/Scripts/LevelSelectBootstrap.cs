using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectBootstrap : MonoBehaviour
{
    private const string EasyScene = "demoScene_Facile";
    private const string MediumScene = "demoScene_Moyen";
    private const string HardScene = "demoScene_Difficile";

    private static LevelSelectBootstrap instance;

    private bool showMenu = true;
    private bool hasChosenLevel;
    private Vector2 windowSize = new Vector2(860f, 620f);
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardDescriptionStyle;
    private GUIStyle tagStyle;
    private GUIStyle hintStyle;

    public static bool IsMenuOpen => instance != null && instance.showMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("LevelSelectBootstrap");
        instance = bootstrapObject.AddComponent<LevelSelectBootstrap>();
        DontDestroyOnLoad(bootstrapObject);
    }

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasChosenLevel)
        {
            PauseForMenu();
        }
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
        GUI.color = new Color(0.02f, 0.04f, 0.06f, 0.86f);
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
        GUILayout.Space(26f);
        GUILayout.Label("Choix Du Niveau", titleStyle);
        GUILayout.Space(12f);
        GUILayout.Label("Selectionne la difficulte avant d'entrer dans l'hopital. Le curseur reste libre tant que tu n'as pas choisi.", subtitleStyle);
        GUILayout.Space(24f);

        DrawLevelCard("Facile", "Ideal pour decouvrir la map et les objectifs.", "13 min de jeu, plus de marge, monstre plus lent.", EasyScene, new Color(0.18f, 0.36f, 0.22f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(14f);
        DrawLevelCard("Moyen", "La version la plus equilibree du prototype.", "10 min de jeu, bon rythme entre exploration, poursuite et extraction.", MediumScene, new Color(0.18f, 0.24f, 0.34f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(14f);
        DrawLevelCard("Difficile", "Pour une vraie partie sous pression.", "8 min de jeu, monstre plus agressif, erreurs plus couteuses.", HardScene, new Color(0.36f, 0.16f, 0.16f, 0.92f), effectiveWindowSize.x - 56f);
        GUILayout.Space(18f);
        GUILayout.Label("Clique sur une carte pour commencer la partie.", hintStyle);
        GUILayout.Space(8f);

        GUILayout.EndArea();
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

    private void DrawLevelCard(string label, string tagline, string description, string sceneName, Color cardColor, float width)
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
            ChooseLevel(sceneName);
        }
    }

    private void ChooseLevel(string sceneName)
    {
        hasChosenLevel = true;
        showMenu = false;
        ResumeAfterMenu();

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
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
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 38,
                fontStyle = FontStyle.Bold
            };
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 19,
                wordWrap = true
            };
        }

        if (cardTitleStyle == null)
        {
            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            cardTitleStyle.normal.textColor = Color.white;
        }

        if (cardDescriptionStyle == null)
        {
            cardDescriptionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true
            };
            cardDescriptionStyle.normal.textColor = new Color(0.93f, 0.95f, 0.98f);
        }

        if (tagStyle == null)
        {
            tagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Italic
            };
            tagStyle.normal.textColor = new Color(0.95f, 0.9f, 0.6f);
        }

        if (hintStyle == null)
        {
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            hintStyle.normal.textColor = new Color(0.86f, 0.9f, 0.95f);
        }
    }
}

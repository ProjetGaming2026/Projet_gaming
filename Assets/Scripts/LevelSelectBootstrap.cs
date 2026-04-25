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
    private Vector2 windowSize = new Vector2(720f, 520f);
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;

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
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(overlay, GUIContent.none);
        GUI.color = previousColor;

        Rect windowRect = new Rect(
            (Screen.width - windowSize.x) * 0.5f,
            (Screen.height - windowSize.y) * 0.5f,
            windowSize.x,
            windowSize.y
        );

        GUILayout.BeginArea(windowRect, GUI.skin.window);
        GUILayout.Space(24f);
        GUILayout.Label("Choix Du Niveau", titleStyle);
        GUILayout.Space(14f);
        GUILayout.Label("Selectionne la difficulte avant d'entrer dans l'hopital.", subtitleStyle);
        GUILayout.Space(24f);

        DrawLevelButton("Facile", "Incubation plus longue, moins de taches, monstre plus indulgent.", EasyScene);
        GUILayout.Space(16f);
        DrawLevelButton("Moyen", "Experience equilibree du prototype.", MediumScene);
        GUILayout.Space(16f);
        DrawLevelButton("Difficile", "Moins de temps, plus de taches, monstre plus agressif.", HardScene);
        GUILayout.Space(16f);

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

    private void DrawLevelButton(string label, string description, string sceneName)
    {
        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(62f)))
        {
            ChooseLevel(sceneName);
        }

        GUILayout.Space(6f);
        GUILayout.Label(description, subtitleStyle);
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
                fontSize = 34,
                fontStyle = FontStyle.Bold
            };
        }

        if (subtitleStyle == null)
        {
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

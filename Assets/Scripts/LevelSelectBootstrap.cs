using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectBootstrap : MonoBehaviour
{
    private const string EasyScene = "demoScene_Facile";
    private const string MediumScene = "demoScene_Moyen";
    private const string HardScene = "demoScene_Difficile";
    private const string BackScene = "MainMenu";

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource secondAmbienceSource;

    [Header("Voice Clips")]
    [SerializeField] private AudioClip easyVoice;
    [SerializeField] private AudioClip mediumVoice;
    [SerializeField] private AudioClip hardVoice;

    [Header("Ambience Clips")]
    [SerializeField] private AudioClip easyAmbience;
    [SerializeField] private AudioClip mediumAmbience;
    [SerializeField] private AudioClip hardAmbience;

    [Header("Shared Second Ambience")]
    [SerializeField] private AudioClip secondAmbience;

    [Header("Audio Volumes")]
    [SerializeField] private float mainAmbienceVolume = 0.35f;
    [SerializeField] private float secondAmbienceVolume = 0.15f;
    [SerializeField] private float voiceVolume = 1f;

    [Header("Warning Screen")]
    [SerializeField] private Texture2D warningTexture;
    [SerializeField] private float dotsXPercent = 0.618f;
    [SerializeField] private float dotsYPercent = 0.405f;
    [SerializeField] private int dotsFontSize = 58;

    private static LevelSelectBootstrap instance;

    private bool showMenu = true;
    private bool showWaitingScreen;
    private bool hasChosenLevel;
    private bool isStarting;

    private Vector2 windowSize = new Vector2(860f, 720f);

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardDescriptionStyle;
    private GUIStyle tagStyle;
    private GUIStyle hintStyle;
    private GUIStyle backButtonStyle;
    private GUIStyle dotsStyle;

    public static bool IsMenuOpen => instance != null && instance.showMenu;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        if (ambienceSource == null) ambienceSource = gameObject.AddComponent<AudioSource>();
        if (secondAmbienceSource == null) secondAmbienceSource = gameObject.AddComponent<AudioSource>();

        voiceSource.playOnAwake = false;
        ambienceSource.playOnAwake = false;
        secondAmbienceSource.playOnAwake = false;

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
        if (showWaitingScreen)
        {
            showWaitingScreen = false;
        }

        if (!hasChosenLevel)
        {
            PauseForMenu();
        }
    }

    private void Update()
    {
        if (!showMenu && !showWaitingScreen)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (showWaitingScreen)
        {
            DrawWarningScreen();
            return;
        }

        if (!showMenu)
        {
            return;
        }

        DrawBlackBackground();

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

        DrawLevelCard("Facile", "Ideal pour decouvrir la map et les objectifs.", "13 min de jeu, plus de marge, monstre plus lent.", EasyScene, "Easy", 13, easyVoice, easyAmbience, new Color(0.18f, 0.36f, 0.22f, 0.92f), effectiveWindowSize.x - 56f);

        GUILayout.Space(14f);

        DrawLevelCard("Moyen", "La version la plus equilibree du prototype.", "10 min de jeu, bon rythme entre exploration, poursuite et extraction.", MediumScene, "Medium", 10, mediumVoice, mediumAmbience, new Color(0.18f, 0.24f, 0.34f, 0.92f), effectiveWindowSize.x - 56f);

        GUILayout.Space(14f);

        DrawLevelCard("Difficile", "Pour une vraie partie sous pression.", "7 min de jeu, monstre plus agressif, erreurs plus couteuses.", HardScene, "Hard", 7, hardVoice, hardAmbience, new Color(0.36f, 0.16f, 0.16f, 0.92f), effectiveWindowSize.x - 56f);

        GUILayout.Space(18f);
        GUILayout.Label("Clique sur une carte pour commencer la partie.", hintStyle);
        GUILayout.Space(18f);

        if (GUILayout.Button("BACK", backButtonStyle, GUILayout.Height(64f)))
        {
            GoBack();
        }

        GUILayout.EndArea();
    }

    private void DrawLevelCard(
        string label,
        string tagline,
        string description,
        string sceneName,
        string difficulty,
        int minutes,
        AudioClip voiceClip,
        AudioClip ambienceClip,
        Color cardColor,
        float width
    )
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
            ChooseLevel(sceneName, difficulty, minutes, voiceClip, ambienceClip);
        }
    }

    private void ChooseLevel(string sceneName, string difficulty, int minutes, AudioClip voiceClip, AudioClip ambienceClip)
    {
        if (isStarting)
        {
            return;
        }

        StartCoroutine(PlayIntroThenLoad(sceneName, difficulty, minutes, voiceClip, ambienceClip));
    }

    private IEnumerator PlayIntroThenLoad(string sceneName, string difficulty, int minutes, AudioClip voiceClip, AudioClip ambienceClip)
    {
        isStarting = true;
        hasChosenLevel = true;
        showMenu = false;
        showWaitingScreen = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        PlayerPrefs.SetString("Difficulty", difficulty);
        PlayerPrefs.SetInt("LevelTimeMinutes", minutes);
        PlayerPrefs.Save();

        AsyncOperation loading = null;

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            loading = SceneManager.LoadSceneAsync(sceneName);
            loading.allowSceneActivation = false;
        }

        if (ambienceSource != null && ambienceClip != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.loop = true;
            ambienceSource.volume = mainAmbienceVolume;
            ambienceSource.Play();
        }

        if (secondAmbienceSource != null && secondAmbience != null)
        {
            secondAmbienceSource.clip = secondAmbience;
            secondAmbienceSource.loop = true;
            secondAmbienceSource.volume = secondAmbienceVolume;
            secondAmbienceSource.Play();
        }

        if (voiceSource != null && voiceClip != null)
        {
            voiceSource.clip = voiceClip;
            voiceSource.loop = false;
            voiceSource.volume = voiceVolume;
            voiceSource.Play();

            while (voiceSource.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.5f);
        }

        StopIntroAudio();

        if (loading != null)
        {
            while (loading.progress < 0.9f)
            {
                yield return null;
            }

            loading.allowSceneActivation = true;
        }
    }

    private void StopIntroAudio()
    {
        if (ambienceSource != null && ambienceSource.isPlaying) ambienceSource.Stop();
        if (secondAmbienceSource != null && secondAmbienceSource.isPlaying) secondAmbienceSource.Stop();
        if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
    }

    private void DrawBlackBackground()
    {
        Color previousColor = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawWarningScreen()
    {
        DrawBlackBackground();

        if (warningTexture != null)
        {
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                warningTexture,
                ScaleMode.ScaleAndCrop,
                true
            );
        }

        DrawAnimatedDots();
    }

    private void DrawAnimatedDots()
    {
        int dotCount = ((int)(Time.realtimeSinceStartup * 3f) % 4);
        string dots = new string('.', dotCount);

        Rect dotsRect = new Rect(
            Screen.width * dotsXPercent,
            Screen.height * dotsYPercent,
            220f,
            100f
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0.55f, 0.02f, 0.02f);
        GUI.Label(dotsRect, dots, dotsStyle);
        GUI.color = previousColor;
    }

    private void GoBack()
    {
        hasChosenLevel = true;
        showMenu = false;
        showWaitingScreen = false;
        ResumeAfterMenu();
        StopIntroAudio();

        SceneManager.LoadScene(BackScene);
    }

    private void PauseForMenu()
    {
        showMenu = true;
        showWaitingScreen = false;
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

        if (dotsStyle == null)
        {
            dotsStyle = new GUIStyle(GUI.skin.label);
            dotsStyle.alignment = TextAnchor.MiddleLeft;
            dotsStyle.fontSize = dotsFontSize;
            dotsStyle.fontStyle = FontStyle.Bold;
            dotsStyle.normal.textColor = new Color(0.55f, 0.02f, 0.02f);
        }
    }
}








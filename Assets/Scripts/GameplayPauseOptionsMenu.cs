using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameplayPauseOptionsMenu : MonoBehaviour
{
    private const string MusicVolumeKey = "Options.MusicVolume";
    private const string SfxVolumeKey = "Options.SfxVolume";
    private const string AmbienceVolumeKey = "Options.AmbienceVolume";
    private const string FullscreenKey = "Options.Fullscreen";
    private const string LanguageKey = "Options.Language";

    public static bool IsOpen { get; private set; }

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle hintStyle;
    private float previousTimeScale = 1f;
    private float musicVolume;
    private float sfxVolume;
    private float ambienceVolume;
    private int fullscreenIndex;
    private int languageIndex;

    private void Awake()
    {
        LoadOptions();
        ApplyAudioVolumes();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (IsOpen)
        {
            CloseMenu();
            return;
        }

        if (!CanOpenMenu())
        {
            return;
        }

        OpenMenu();
    }

    private void OnGUI()
    {
        if (!IsOpen)
        {
            return;
        }

        EnsureStyles();

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
        GUI.color = previousColor;

        float panelWidth = Mathf.Min(760f, Screen.width - 80f);
        float panelHeight = Mathf.Min(620f, Screen.height - 80f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        GUILayout.BeginArea(panelRect, panelStyle);
        GUILayout.Space(22f);
        GUILayout.Label(GameLocalization.Text("SETTINGS", "PARAMETRES", "OPCIONES"), titleStyle);
        GUILayout.Space(24f);

        DrawSliderRow(GameLocalization.Text("Music", "Musique", "Musica"), ref musicVolume, SetMusicVolume);
        DrawSliderRow(GameLocalization.Text("Ambience", "Ambiance", "Ambiente"), ref ambienceVolume, SetAmbienceVolume);
        DrawSliderRow(GameLocalization.Text("Sound Effects", "Effets Sonores", "Efectos Sonoros"), ref sfxVolume, SetSfxVolume);

        GUILayout.Space(18f);
        DrawChoiceRow(
            GameLocalization.Text("Language", "Langue", "Idioma"),
            ref languageIndex,
            new[]
            {
                GameLocalization.Text("English", "Anglais", "Ingles"),
                GameLocalization.Text("French", "Francais", "Frances"),
                GameLocalization.Text("Spanish", "Espagnol", "Espanol")
            },
            SetLanguage
        );

        DrawChoiceRow(
            GameLocalization.Text("Fullscreen", "Plein Ecran", "Pantalla Completa"),
            ref fullscreenIndex,
            new[]
            {
                GameLocalization.Text("Yes", "Oui", "Si"),
                GameLocalization.Text("No", "Non", "No")
            },
            SetFullscreen
        );

        GUILayout.FlexibleSpace();
        if (GUILayout.Button(GameLocalization.Text("BACK", "RETOUR", "VOLVER"), buttonStyle, GUILayout.Height(66f)))
        {
            CloseMenu();
        }

        if (GUILayout.Button(GameLocalization.Text("QUIT GAME", "QUITTER LE JEU", "SALIR DEL JUEGO"), buttonStyle, GUILayout.Height(66f)))
        {
            QuitGame();
        }

        GUILayout.Label(GameLocalization.Text("Press Esc to resume", "Appuie sur Esc pour reprendre", "Pulsa Esc para continuar"), hintStyle);
        GUILayout.EndArea();
    }

    public void OpenMenu()
    {
        LoadOptions();
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        PlayerPrefs.Save();
        IsOpen = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
        IsOpen = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool CanOpenMenu()
    {
        if (!IsGameplayScene(SceneManager.GetActiveScene()))
        {
            return false;
        }

        if (LevelSelectBootstrap.IsMenuOpen ||
            CipherTerminal.IsAnyTerminalOpen ||
            PadlockPuzzle.IsAnyPadlockFocused ||
            PadlockController.IsAnyPadlockActive ||
            PhoneAnswerInput.IsAnyAnswerInputOpen ||
            PhoneAnswerInput.WasAnswerInputCancelledThisFrame)
        {
            return false;
        }

        return !Cursor.visible;
    }

    private static bool IsGameplayScene(Scene scene)
    {
        return scene.name == "demoScene_Facile" ||
               scene.name == "demoScene_Moyen" ||
               scene.name == "demoScene_Difficile";
    }

    private void LoadOptions()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        ambienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, 1f);
        fullscreenIndex = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1 ? 0 : 1;
        languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 2);
    }

    private void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        AudioManager.SetMusicVolume(musicVolume);
        ApplyAudioVolumes();
    }

    private void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        AudioManager.SetSFXVolume(sfxVolume);
    }

    private void SetAmbienceVolume(float value)
    {
        ambienceVolume = Mathf.Clamp01(value);
        AudioManager.SetAmbienceVolume(ambienceVolume);
        ApplyAudioVolumes();
    }

    private void SetFullscreen(int index)
    {
        fullscreenIndex = Mathf.Clamp(index, 0, 1);
        bool fullscreen = fullscreenIndex == 0;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        Screen.fullScreen = fullscreen;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);
    }

    private void SetLanguage(int index)
    {
        languageIndex = Mathf.Clamp(index, 0, 2);
        PlayerPrefs.SetInt(LanguageKey, languageIndex);
        PlayerPrefs.Save();
    }

    private void ApplyAudioVolumes()
    {
        AudioManager.ApplyGlobalVolume();
    }

    private void DrawSliderRow(string label, ref float value, System.Action<float> setter)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(56f));
        GUILayout.Label(label, labelStyle, GUILayout.Width(260f));
        float newValue = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Height(34f));
        GUILayout.Label(Mathf.RoundToInt(newValue * 100f) + "%", labelStyle, GUILayout.Width(76f));
        GUILayout.EndHorizontal();

        if (!Mathf.Approximately(newValue, value))
        {
            value = newValue;
            setter(value);
        }
    }

    private void DrawChoiceRow(string label, ref int selectedIndex, string[] choices, System.Action<int> setter)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(58f));
        GUILayout.Label(label, labelStyle, GUILayout.Width(260f));

        for (int i = 0; i < choices.Length; i++)
        {
            GUIStyle style = i == selectedIndex ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(choices[i], style, GUILayout.Height(44f)))
            {
                selectedIndex = i;
                setter(i);
            }
        }

        GUILayout.EndHorizontal();
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(42, 42, 24, 28)
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.red }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(1f, 0.82f, 0.38f) }
        };

        selectedButtonStyle = new GUIStyle(buttonStyle)
        {
            normal = { textColor = new Color(1f, 0.82f, 0.38f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            normal = { textColor = new Color(0.78f, 0.78f, 0.78f, 1f) }
        };
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            IsOpen = false;
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        }
    }
}

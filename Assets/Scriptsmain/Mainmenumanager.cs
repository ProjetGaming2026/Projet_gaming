using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Options.MusicVolume";
    private const string SfxVolumeKey = "Options.SfxVolume";
    private const string AmbienceVolumeKey = "Options.AmbienceVolume";
    private const string FullscreenKey = "Options.Fullscreen";
    private const string LanguageKey = "Options.Language";
    private const string EasySceneName = "demoScene_Facile";
    private const string MediumSceneName = "demoScene_Moyen";
    private const string HardSceneName = "demoScene_Difficile";
    private const string EasyScenePath = "Assets/Hospital/scenes/demoScene_Facile.unity";
    private const string MediumScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string HardScenePath = "Assets/Hospital/scenes/demoScene_Difficile.unity";

    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject titleText;
    public GameObject titleText2;
    public GameObject zombie;

    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider ambienceSlider;
    private TMP_Dropdown fullscreenDropdown;
    private TMP_Dropdown languageDropdown;
    private readonly List<AudioSource> backgroundAudioSources = new List<AudioSource>();
    private readonly List<ButtonAnimator> buttonAnimators = new List<ButtonAnimator>();
    private bool showLevelSelect;
    private GUIStyle levelWindowStyle;
    private GUIStyle levelTitleStyle;
    private GUIStyle levelButtonStyle;
    private GUIStyle levelBackButtonStyle;
    private GUIStyle levelBadgeStyle;
    private static Texture2D levelPixelTexture;
    private AudioSource levelSelectAudioSource;
    private AudioClip levelHoverSound;
    private AudioClip levelClickSound;
    private string hoveredLevelButton;

    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static float AmbienceVolume { get; private set; } = 1f;
    public static string SelectedLanguage { get; private set; } = "English";

    private void Start()
    {
        ResolveSceneReferences();
        WireMenuButtons();
        SyncMenuButtonSounds();
        BindOptionsControls();
        CacheAudioSources();
        LoadSavedOptions();
        ShowMainPanel();
    }

    public void NewGame()
    {
        OpenLevelSelect();
    }

    public void OpenOptions()
    {
        showLevelSelect = false;
        hoveredLevelButton = null;
        SetActiveSafe(mainPanel, false);
        SetActiveSafe(optionsPanel, true);
        if (optionsPanel != null)
        {
            optionsPanel.transform.SetAsLastSibling();
        }

        SetActiveSafe(titleText, false);
        SetActiveSafe(titleText2, false);
        SetActiveSafe(zombie, false);
    }

    public void BackToMenu()
    {
        PlayerPrefs.Save();
        ShowMainPanel();
    }

    public void QuitGame()
    {
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        AudioManager.SetMusicVolume(MusicVolume);
        ApplyAudioVolumes();
    }

    public void SetSFXVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        AudioManager.SetSFXVolume(SfxVolume);
        ApplyAudioVolumes();
    }

    public void SetAmbienceVolume(float value)
    {
        AmbienceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, AmbienceVolume);
        AudioManager.SetAmbienceVolume(AmbienceVolume);
        ApplyAudioVolumes();
    }

    public void SetFullscreen(int optionIndex)
    {
        bool fullscreen = optionIndex == 0;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreenMode = mode;
        Screen.fullScreen = fullscreen;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);
    }

    public void SetLanguage(int optionIndex)
    {
        NormalizeLanguageDropdownOptions();

        if (languageDropdown == null || languageDropdown.options.Count == 0)
        {
            SelectedLanguage = "English";
        }
        else
        {
            int safeIndex = Mathf.Clamp(optionIndex, 0, languageDropdown.options.Count - 1);
            SelectedLanguage = languageDropdown.options[safeIndex].text;
        }

        PlayerPrefs.SetInt(LanguageKey, optionIndex);
        PlayerPrefs.Save();
        ApplyLanguage();
    }

    private void ShowMainPanel()
    {
        showLevelSelect = false;
        hoveredLevelButton = null;
        SetActiveSafe(mainPanel, true);
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(titleText, true);
        SetActiveSafe(titleText2, true);
        SetActiveSafe(zombie, true);
    }

    private void OpenLevelSelect()
    {
        showLevelSelect = true;
        hoveredLevelButton = null;
        PrepareLevelSelectSounds();
        SetActiveSafe(mainPanel, false);
        SetActiveSafe(optionsPanel, false);
        SetActiveSafe(titleText, false);
        SetActiveSafe(titleText2, false);
        SetActiveSafe(zombie, true);
    }

    private void OnGUI()
    {
        if (!showLevelSelect)
        {
            return;
        }

        EnsureLevelStyles();

        DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.44f));
        DrawHorrorVignette(new Rect(0f, 0f, Screen.width, Screen.height));

        float uiScale = Mathf.Clamp(Mathf.Min((Screen.width - 32f) / 700f, (Screen.height - 28f) / 500f), 0.58f, 1.18f);
        float width = Mathf.Min(700f * uiScale, Screen.width - 32f);
        float height = Mathf.Min(500f * uiScale, Screen.height - 28f);
        Rect windowRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        DrawLevelPanel(windowRect);

        float titleHeight = 52f * uiScale;
        float buttonWidth = Mathf.Min(520f * uiScale, width - 56f * uiScale);
        float buttonHeight = 70f * uiScale;
        float gap = 14f * uiScale;
        float stackHeight = titleHeight + 34f * uiScale + buttonHeight * 4f + gap * 3f;
        float startY = windowRect.y + (height - stackHeight) * 0.5f;
        float buttonX = windowRect.x + (width - buttonWidth) * 0.5f;

        DrawHorrorTitle(
            new Rect(windowRect.x + 24f * uiScale, startY, width - 48f * uiScale, titleHeight),
            TextFor(GetCurrentLanguageIndex(), "Choose Level", "Choix Du Niveau", "Elegir Nivel"),
            uiScale
        );

        DrawLevelChoice(
            new Rect(buttonX, startY + titleHeight + 34f * uiScale, buttonWidth, buttonHeight),
            TextFor(GetCurrentLanguageIndex(), "Easy", "Facile", "Facil"),
            EasyScenePath,
            EasySceneName,
            new Color(0.25f, 0.88f, 0.56f, 1f),
            "I",
            DifficultyWarningTransition.VoiceClip.Easy
        );

        DrawLevelChoice(
            new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap), buttonWidth, buttonHeight),
            TextFor(GetCurrentLanguageIndex(), "Medium", "Moyen", "Medio"),
            MediumScenePath,
            MediumSceneName,
            new Color(0.42f, 0.72f, 1f, 1f),
            "II",
            DifficultyWarningTransition.VoiceClip.Medium
        );

        DrawLevelChoice(
            new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap) * 2f, buttonWidth, buttonHeight),
            TextFor(GetCurrentLanguageIndex(), "Hard", "Difficile", "Dificil"),
            HardScenePath,
            HardSceneName,
            new Color(1f, 0.32f, 0.3f, 1f),
            "III",
            DifficultyWarningTransition.VoiceClip.Hard
        );

        DrawLevelBackButton(
            new Rect(buttonX, startY + titleHeight + 34f * uiScale + (buttonHeight + gap) * 3f, buttonWidth, buttonHeight),
            TextFor(GetCurrentLanguageIndex(), "BACK", "RETOUR", "VOLVER")
        );
    }

    private void DrawLevelChoice(Rect buttonRect, string title, string scenePath, string sceneName, Color accentColor, string badge, DifficultyWarningTransition.VoiceClip voiceClip)
    {
        string buttonId = sceneName;
        bool hovered = buttonRect.Contains(Event.current.mousePosition);
        PlayLevelHoverSound(buttonId, buttonRect);
        DrawModernButtonFrame(buttonRect, accentColor, hovered, false, true);

        Rect badgeRect = new Rect(buttonRect.x + 14f, buttonRect.y + 12f, 48f, buttonRect.height - 24f);
        DrawSolidRect(badgeRect, hovered ? new Color(0.62f, 0.02f, 0.02f, 0.95f) : new Color(0.2f, 0.015f, 0.015f, 0.86f));
        DrawSolidRect(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 2f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.75f));
        GUI.Label(badgeRect, badge, levelBadgeStyle);

        if (GUI.Button(buttonRect, title, levelButtonStyle))
        {
            PlayLevelClickSound();
            showLevelSelect = false;
            SetActiveSafe(zombie, false);
            DifficultyWarningTransition.PlayBeforeScene(scenePath, sceneName, voiceClip);
        }
    }

    private void DrawLevelBackButton(Rect buttonRect, string title)
    {
        bool hovered = buttonRect.Contains(Event.current.mousePosition);
        PlayLevelHoverSound("back", buttonRect);
        DrawModernButtonFrame(buttonRect, new Color(0.86f, 0.08f, 0.08f, 1f), hovered, true, false);

        Rect arrowRect = new Rect(buttonRect.x + 18f, buttonRect.y + 12f, 44f, buttonRect.height - 24f);
        GUI.Label(arrowRect, "<", levelBadgeStyle);

        if (GUI.Button(buttonRect, title, levelBackButtonStyle))
        {
            PlayLevelClickSound();
            BackToMenu();
        }
    }

    private void PrepareLevelSelectSounds()
    {
        ButtonAnimator playAnimator = FindButtonAnimator("New Game ");
        if (playAnimator != null)
        {
            levelHoverSound = playAnimator.hoverSound;
            levelClickSound = playAnimator.clickSound;
        }

        if (levelSelectAudioSource == null)
        {
            levelSelectAudioSource = gameObject.GetComponent<AudioSource>();
            if (levelSelectAudioSource == null)
            {
                levelSelectAudioSource = gameObject.AddComponent<AudioSource>();
            }

            levelSelectAudioSource.playOnAwake = false;
        }

        levelSelectAudioSource.volume = SfxVolume;
    }

    private void PlayLevelHoverSound(string buttonId, Rect buttonRect)
    {
        EventType eventType = Event.current.type;
        if (eventType != EventType.Repaint && eventType != EventType.MouseMove && eventType != EventType.MouseDrag)
        {
            return;
        }

        if (!buttonRect.Contains(Event.current.mousePosition))
        {
            if (hoveredLevelButton == buttonId)
            {
                hoveredLevelButton = null;
            }

            return;
        }

        if (hoveredLevelButton == buttonId)
        {
            return;
        }

        hoveredLevelButton = buttonId;
        PlayLevelSound(levelHoverSound);
    }

    private void PlayLevelClickSound()
    {
        PlayLevelSound(levelClickSound);
    }

    private void PlayLevelSound(AudioClip clip)
    {
        if (clip == null || levelSelectAudioSource == null)
        {
            return;
        }

        levelSelectAudioSource.volume = SfxVolume;
        levelSelectAudioSource.PlayOneShot(clip);
    }

    private void LoadSceneExact(string scenePath, string sceneName)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            return;
        }

        Debug.LogWarning("MainMenuManager: scene absente des Build Settings: " + scenePath + ". Fallback par nom: " + sceneName);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void EnsureLevelStyles()
    {
        if (levelWindowStyle != null)
        {
            return;
        }

        levelWindowStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(0, 0, 0, 0),
            normal = { background = null }
        };

        levelTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.96f, 0.94f, 0.9f, 1f) }
        };

        levelButtonStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 31,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.96f, 0.94f, 0.9f, 1f) },
            hover = { textColor = new Color(1f, 0.38f, 0.34f) }
        };

        levelBackButtonStyle = new GUIStyle(levelButtonStyle)
        {
            fontSize = 25
        };

        levelBadgeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
    }

    private static void DrawLevelPanel(Rect rect)
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

    private static void DrawModernButtonFrame(Rect rect, Color accentColor, bool hovered, bool subtle, bool hasBadge)
    {
        Color bodyColor = hovered
            ? new Color(0.16f, 0.045f, 0.048f, 0.98f)
            : new Color(0.035f, 0.028f, 0.032f, subtle ? 0.76f : 0.93f);

        DrawSolidRect(rect, new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), bodyColor);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.9f, 0.03f, 0.025f, hovered ? 0.9f : 0.44f));
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), new Color(accentColor.r, accentColor.g, accentColor.b, hovered ? 0.62f : 0.26f));
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
        if (levelPixelTexture == null)
        {
            levelPixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            levelPixelTexture.hideFlags = HideFlags.HideAndDontSave;
            levelPixelTexture.SetPixel(0, 0, Color.white);
            levelPixelTexture.Apply();
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, levelPixelTexture);
        GUI.color = previousColor;
    }

    private void ResolveSceneReferences()
    {
        if (mainPanel == null)
        {
            mainPanel = FindGameObjectInScene("Buttonspanel");
        }

        if (optionsPanel == null)
        {
            optionsPanel = FindGameObjectInScene("Optionspanel");
        }

        if (titleText == null)
        {
            titleText = FindGameObjectInScene("Text (TMP) (1)");
        }

        if (titleText2 == null)
        {
            titleText2 = FindGameObjectInScene("Text (TMP)");
        }
    }

    private void WireMenuButtons()
    {
        WireButton("New Game ", NewGame);
        WireButton("Options", OpenOptions);
        WireButton("Quit", QuitGame);
        WireButton("Back$", BackToMenu);
    }

    private void SyncMenuButtonSounds()
    {
        ButtonAnimator playAnimator = FindButtonAnimator("New Game ");
        if (playAnimator == null)
        {
            return;
        }

        CopyButtonSounds(playAnimator, "Options");
        CopyButtonSounds(playAnimator, "Quit");
        CopyButtonSounds(playAnimator, "Back$");
    }

    private void CopyButtonSounds(ButtonAnimator source, string targetButtonName)
    {
        ButtonAnimator target = FindButtonAnimator(targetButtonName);
        if (target == null)
        {
            return;
        }

        target.hoverSound = source.hoverSound;
        target.clickSound = source.clickSound;
    }

    private ButtonAnimator FindButtonAnimator(string objectName)
    {
        GameObject buttonObject = FindGameObjectInScene(objectName);
        return buttonObject != null ? buttonObject.GetComponent<ButtonAnimator>() : null;
    }

    private void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = FindGameObjectInScene(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void BindOptionsControls()
    {
        if (optionsPanel == null)
        {
            return;
        }

        musicSlider = FindControlInRow<Slider>("Music");
        sfxSlider = FindControlInRow<Slider>("Soundseffects");
        ambienceSlider = FindControlInRow<Slider>("Ambience");
        fullscreenDropdown = FindControlInRow<TMP_Dropdown>("Fullscreen");
        languageDropdown = FindControlInRow<TMP_Dropdown>("Language");
        StyleDropdown(fullscreenDropdown);
        StyleDropdown(languageDropdown);

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (ambienceSlider != null)
        {
            ambienceSlider.onValueChanged.RemoveListener(SetAmbienceVolume);
            ambienceSlider.onValueChanged.AddListener(SetAmbienceVolume);
        }

        if (fullscreenDropdown != null)
        {
            fullscreenDropdown.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenDropdown.onValueChanged.AddListener(SetFullscreen);
        }

        if (languageDropdown != null)
        {
            NormalizeLanguageDropdownOptions();
            languageDropdown.onValueChanged.RemoveListener(SetLanguage);
            languageDropdown.onValueChanged.AddListener(SetLanguage);
        }
    }

    private void CacheAudioSources()
    {
        backgroundAudioSources.Clear();
        buttonAnimators.Clear();

        buttonAnimators.AddRange(FindObjectsByType<ButtonAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource audioSource = audioSources[i];
            if (audioSource == null)
            {
                continue;
            }

            if (audioSource.GetComponent<ButtonAnimator>() != null ||
                audioSource.GetComponentInParent<ButtonAnimator>() != null)
            {
                continue;
            }

            backgroundAudioSources.Add(audioSource);
        }
    }

    private void LoadSavedOptions()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        AmbienceVolume = PlayerPrefs.GetFloat(AmbienceVolumeKey, 1f);

        AudioManager.SetMusicVolume(MusicVolume);
        AudioManager.SetSFXVolume(SfxVolume);
        AudioManager.SetAmbienceVolume(AmbienceVolume);

        SetSliderValueWithoutNotify(musicSlider, MusicVolume);
        SetSliderValueWithoutNotify(sfxSlider, SfxVolume);
        SetSliderValueWithoutNotify(ambienceSlider, AmbienceVolume);

        int fullscreenIndex = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1 ? 0 : 1;
        SetDropdownValueWithoutNotify(fullscreenDropdown, fullscreenIndex);
        SetFullscreen(fullscreenIndex);

        int languageIndex = PlayerPrefs.GetInt(LanguageKey, 0);
        SetDropdownValueWithoutNotify(languageDropdown, languageIndex);
        SetLanguage(languageIndex);

        ApplyAudioVolumes();
    }

    private void ApplyAudioVolumes()
    {
        AudioListener.volume = 1f;

        float backgroundVolume = Mathf.Clamp01(MusicVolume * AmbienceVolume);
        for (int i = 0; i < backgroundAudioSources.Count; i++)
        {
            if (backgroundAudioSources[i] != null)
            {
                backgroundAudioSources[i].volume = backgroundVolume;
            }
        }

        for (int i = 0; i < buttonAnimators.Count; i++)
        {
            if (buttonAnimators[i] != null)
            {
                buttonAnimators[i].ApplySfxVolume(SfxVolume);
            }
        }
    }

    private void ApplyLanguage()
    {
        int language = GetCurrentLanguageIndex();
        bool rightToLeft = false;

        SetButtonLabel("New Game ", TextFor(language, "PLAY", "JOUER", "JUGAR"));
        SetButtonLabel("Options", TextFor(language, "OPTIONS", "OPTIONS", "OPCIONES"));
        SetButtonLabel("Quit", TextFor(language, "QUIT", "QUITTER", "SALIR"));
        SetButtonLabel("Back$", TextFor(language, "BACK", "RETOUR", "VOLVER"));

        SetFirstTextInObject("Music", TextFor(language, "Music", "Musique", "Musica"), rightToLeft);
        SetFirstTextInObject("Soundseffects", TextFor(language, "Sound Effects", "Effets Sonores", "Efectos Sonoros"), rightToLeft);
        SetFirstTextInObject("Ambience", TextFor(language, "Ambience", "Ambiance", "Ambiente"), rightToLeft);
        SetFirstTextInObject("Fullscreen", TextFor(language, "Fullscreen", "Plein Ecran", "Pantalla Completa"), rightToLeft);
        SetFirstTextInObject("Language", TextFor(language, "Language", "Langue", "Idioma"), rightToLeft);

        SetTextByExactValue("Options Menu", TextFor(language, "Options Menu", "Parametres", "Opciones"), rightToLeft);
        SetTextByExactValue("Title Of The Game", TextFor(language, "Title Of The Game", "Titre Du Jeu", "Titulo Del Juego"), rightToLeft);
        FixOptionsLabelTextOnly();

        if (fullscreenDropdown != null && fullscreenDropdown.options.Count >= 2)
        {
            fullscreenDropdown.options[0].text = TextFor(language, "Yes", "Oui", "Si");
            fullscreenDropdown.options[1].text = TextFor(language, "No", "Non", "No");
            fullscreenDropdown.RefreshShownValue();
        }

        StyleDropdown(fullscreenDropdown);
        StyleDropdown(languageDropdown);
    }

    private void FixOptionsLabelTextOnly()
    {
        FixOptionLabelTextOnly("Music");
        FixOptionLabelTextOnly("Ambience");
        FixOptionLabelTextOnly("Soundseffects");
        FixOptionLabelTextOnly("Language");
        FixOptionLabelTextOnly("Fullscreen");
    }

    private void FixOptionLabelTextOnly(string rowName)
    {
        GameObject row = FindGameObjectInScene(rowName);
        if (row == null)
        {
            return;
        }

        TMP_Text[] labels = row.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || label.GetComponentInParent<TMP_Dropdown>() != null)
            {
                continue;
            }

            label.enableAutoSizing = true;
            label.fontSizeMin = 28f;
            label.fontSizeMax = 40f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableWordWrapping = false;
            label.margin = Vector4.zero;
            label.ForceMeshUpdate();
            return;
        }
    }

    private static void StyleDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
        {
            return;
        }

        ColorBlock colors = dropdown.colors;
        colors.normalColor = new Color(0.06f, 0.06f, 0.065f, 1f);
        colors.highlightedColor = new Color(0.18f, 0.13f, 0.08f, 1f);
        colors.pressedColor = new Color(0.24f, 0.17f, 0.1f, 1f);
        colors.selectedColor = new Color(0.18f, 0.13f, 0.08f, 1f);
        colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.7f);
        colors.colorMultiplier = 1f;
        dropdown.colors = colors;

        if (dropdown.targetGraphic is Image targetImage)
        {
            targetImage.color = new Color(0.045f, 0.045f, 0.05f, 0.96f);
        }

        StyleDropdownText(dropdown.captionText, 27f, Color.white);
        StyleDropdownText(dropdown.itemText, 25f, Color.white);

        if (dropdown.template != null)
        {
            dropdown.template.sizeDelta = new Vector2(
                Mathf.Max(dropdown.template.sizeDelta.x, 610f),
                Mathf.Max(dropdown.template.sizeDelta.y, 190f)
            );

            Image[] templateImages = dropdown.template.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < templateImages.Length; i++)
            {
                Image image = templateImages[i];
                if (image == null)
                {
                    continue;
                }

                string imageName = image.name.Trim();
                if (imageName == "Item Background")
                {
                    image.color = new Color(0.12f, 0.12f, 0.13f, 0.98f);
                }
                else if (imageName == "Item Checkmark")
                {
                    image.color = new Color(1f, 0.78f, 0.28f, 1f);
                }
                else
                {
                    image.color = new Color(0.035f, 0.035f, 0.04f, 0.98f);
                }
            }

            TMP_Text[] labels = dropdown.template.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                StyleDropdownText(labels[i], 25f, Color.white);
            }
        }

        dropdown.RefreshShownValue();
    }

    private static void StyleDropdownText(TMP_Text label, float fontSize, Color color)
    {
        if (label == null)
        {
            return;
        }

        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.overflowMode = TextOverflowModes.Overflow;
        label.ForceMeshUpdate();
    }

    private int GetCurrentLanguageIndex()
    {
        if (languageDropdown == null)
        {
            return PlayerPrefs.GetInt(LanguageKey, 0);
        }

        return Mathf.Clamp(languageDropdown.value, 0, 2);
    }

    private void NormalizeLanguageDropdownOptions()
    {
        if (languageDropdown == null)
        {
            return;
        }

        while (languageDropdown.options.Count < 3)
        {
            languageDropdown.options.Add(new TMP_Dropdown.OptionData());
        }

        languageDropdown.options[0].text = "English";
        languageDropdown.options[1].text = "French";
        languageDropdown.options[2].text = "Spanish";
        languageDropdown.RefreshShownValue();
    }

    private static string TextFor(int language, string english, string french, string spanish)
    {
        if (language == 1)
        {
            return french;
        }

        if (language == 2)
        {
            return spanish;
        }

        return english;
    }

    private void SetButtonLabel(string buttonName, string text)
    {
        GameObject buttonObject = FindGameObjectInScene(buttonName);
        if (buttonObject == null)
        {
            return;
        }

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            ApplyText(label, text, false);
        }
    }

    private void SetFirstTextInObject(string objectName, string text, bool arabic)
    {
        GameObject target = FindGameObjectInScene(objectName);
        if (target == null)
        {
            return;
        }

        TMP_Text[] labels = target.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null)
            {
                continue;
            }

            TMP_Dropdown parentDropdown = labels[i].GetComponentInParent<TMP_Dropdown>();
            if (parentDropdown != null)
            {
                continue;
            }

            ApplyText(labels[i], text, arabic);
            return;
        }
    }

    private void SetTextByExactValue(string originalValue, string text, bool arabic)
    {
        TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < labels.Length; i++)
        {
            string current = labels[i].text.Trim();
            bool isOptionsTitle = originalValue == "Options Menu" &&
                (current == "Options Menu" ||
                 current == "Parametres" ||
                 current == "Opciones" ||
                 current == "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a");
            bool isGameTitle = originalValue == "Title Of The Game" &&
                (current == "Title Of The Game" ||
                 current == "Titre Du Jeu" ||
                 current == "Titulo Del Juego" ||
                 current == "\u0639\u0646\u0648\u0627\u0646 \u0627\u0644\u0644\u0639\u0628\u0629");

            if (isOptionsTitle || isGameTitle)
            {
                ApplyText(labels[i], text, arabic);
            }
        }
    }

    private static void ApplyText(TMP_Text label, string text, bool arabic)
    {
        label.text = text;
        label.isRightToLeftText = arabic;
        label.ForceMeshUpdate();
    }

    private T FindControlInRow<T>(string rowName) where T : Component
    {
        Transform row = FindChildRecursive(optionsPanel.transform, rowName);
        return row != null ? row.GetComponentInChildren<T>(true) : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (string.Equals(root.name.Trim(), childName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindGameObjectInScene(string objectName)
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static void SetActiveSafe(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private static void SetSliderValueWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }

    private static void SetDropdownValueWithoutNotify(TMP_Dropdown dropdown, int value)
    {
        if (dropdown == null || dropdown.options.Count == 0)
        {
            return;
        }

        dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, dropdown.options.Count - 1));
        dropdown.RefreshShownValue();
    }
}

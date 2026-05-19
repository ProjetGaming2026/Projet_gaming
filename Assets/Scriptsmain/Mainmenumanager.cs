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
    private GUIStyle levelDescriptionStyle;
    private GUIStyle levelButtonStyle;
    private GUIStyle levelBackButtonStyle;
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

        Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.36f);
        GUI.Box(overlay, GUIContent.none);
        GUI.color = previousColor;

        float width = Mathf.Min(780f, Screen.width - 80f);
        float height = Mathf.Min(610f, Screen.height - 80f);
        Rect windowRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUILayout.BeginArea(windowRect, levelWindowStyle);
        GUILayout.Space(26f);
        GUILayout.Label(TextFor(GetCurrentLanguageIndex(), "Choose Level", "Choix Du Niveau", "Elegir Nivel"), levelTitleStyle);
        GUILayout.Space(18f);

        DrawLevelChoice(
            TextFor(GetCurrentLanguageIndex(), "Easy", "Facile", "Facil"),
            TextFor(GetCurrentLanguageIndex(), "More time, slower zombie.", "Plus de temps, zombie plus lente.", "Mas tiempo, zombie mas lenta."),
            EasyScenePath,
            EasySceneName
        );

        DrawLevelChoice(
            TextFor(GetCurrentLanguageIndex(), "Medium", "Moyen", "Medio"),
            TextFor(GetCurrentLanguageIndex(), "Balanced exploration and danger.", "Equilibre entre exploration et danger.", "Equilibrio entre exploracion y peligro."),
            MediumScenePath,
            MediumSceneName
        );

        DrawLevelChoice(
            TextFor(GetCurrentLanguageIndex(), "Hard", "Difficile", "Dificil"),
            TextFor(GetCurrentLanguageIndex(), "Less time, more aggressive zombie.", "Moins de temps, zombie plus agressive.", "Menos tiempo, zombie mas agresiva."),
            HardScenePath,
            HardSceneName
        );

        GUILayout.Space(14f);
        DrawLevelBackButton(TextFor(GetCurrentLanguageIndex(), "BACK", "RETOUR", "VOLVER"));

        GUILayout.EndArea();
    }

    private void DrawLevelChoice(string title, string description, string scenePath, string sceneName)
    {
        GUILayout.BeginVertical(GUILayout.Height(112f));
        string buttonId = sceneName;
        Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(title), levelButtonStyle, GUILayout.Height(58f), GUILayout.ExpandWidth(true));
        PlayLevelHoverSound(buttonId, buttonRect);

        if (GUI.Button(buttonRect, title, levelButtonStyle))
        {
            PlayLevelClickSound();
            LoadSceneExact(scenePath, sceneName);
        }

        GUILayout.Label(description, levelDescriptionStyle);
        GUILayout.EndVertical();
        GUILayout.Space(12f);
    }

    private void DrawLevelBackButton(string title)
    {
        Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent(title), levelBackButtonStyle, GUILayout.Height(58f), GUILayout.ExpandWidth(true));
        PlayLevelHoverSound("back", buttonRect);

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
            padding = new RectOffset(32, 32, 22, 24)
        };

        levelTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        levelDescriptionStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            wordWrap = true,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        levelButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(1f, 0.82f, 0.38f) }
        };

        levelBackButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(1f, 0.82f, 0.38f) }
        };
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
            label.textWrappingMode = TextWrappingModes.NoWrap;
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

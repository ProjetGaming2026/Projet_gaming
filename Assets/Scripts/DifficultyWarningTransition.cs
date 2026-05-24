using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficultyWarningTransition : MonoBehaviour
{
    public enum VoiceClip
    {
        Easy,
        Medium,
        Hard
    }

    private const string WarningTexturePath = "WarningIntro/texture_warning";
    private const string EasyVoicePath = "WarningIntro/easy_voice";
    private const string MediumVoicePath = "WarningIntro/medium_voice";
    private const string HardVoicePath = "WarningIntro/hard_voice";
    private const string EasyAmbiencePath = "WarningIntro/easy_ambience";
    private const string HardAmbiencePath = "WarningIntro/hard_ambience";
    private const float MinimumDisplayTime = 2.5f;
    private const float PostAudioDelay = 0.25f;
    private const float AmbienceVolumeMultiplier = 0.45f;

    private static DifficultyWarningTransition activeTransition;

    private Texture2D warningTexture;
    private AudioSource voiceSource;
    private AudioSource ambienceSource;
    private AsyncOperation sceneLoadOperation;
    private GUIStyle loadingStyle;
    private string scenePath;
    private string sceneName;
    private float remainingTime;
    private bool activationRequested;

    public static void PlayBeforeScene(string targetScenePath, string targetSceneName, VoiceClip voiceClip)
    {
        if (activeTransition != null)
        {
            return;
        }

        GameObject transitionObject = new GameObject("Difficulty Warning Transition");
        DontDestroyOnLoad(transitionObject);

        DifficultyWarningTransition transition = transitionObject.AddComponent<DifficultyWarningTransition>();
        transition.Begin(targetScenePath, targetSceneName, voiceClip);
    }

    private void Begin(string targetScenePath, string targetSceneName, VoiceClip voiceClip)
    {
        activeTransition = this;
        scenePath = targetScenePath;
        sceneName = targetSceneName;
        warningTexture = Resources.Load<Texture2D>(WarningTexturePath);

        AudioClip voiceAudioClip = Resources.Load<AudioClip>(GetVoicePath(voiceClip));
        AudioClip ambienceAudioClip = Resources.Load<AudioClip>(GetAmbiencePath(voiceClip));
        voiceSource = CreateAudioSource(Mathf.Clamp01(AudioManager.SfxVolume));
        ambienceSource = CreateAudioSource(Mathf.Clamp01(AudioManager.AmbienceVolume * AmbienceVolumeMultiplier));

        AudioListener.pause = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        remainingTime = voiceAudioClip != null
            ? Mathf.Max(MinimumDisplayTime, voiceAudioClip.length + PostAudioDelay)
            : MinimumDisplayTime;

        PlayClip(voiceSource, voiceAudioClip);
        PlayClip(ambienceSource, ambienceAudioClip);
        PreloadTargetScene();
    }

    private AudioSource CreateAudioSource(float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
        source.volume = volume;
        return source;
    }

    private static void PlayClip(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
        {
            return;
        }

        source.clip = clip;
        source.Play();
    }

    private void Update()
    {
        remainingTime -= Time.unscaledDeltaTime;
        if (remainingTime > 0f)
        {
            return;
        }

        ActivatePreloadedScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (activeTransition == this)
        {
            activeTransition = null;
        }
    }

    private void OnGUI()
    {
        DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), Color.black);

        if (warningTexture == null)
        {
            return;
        }

        Rect targetRect = GetCoverRect(warningTexture.width, warningTexture.height);
        GUI.DrawTexture(targetRect, warningTexture, ScaleMode.StretchToFill, false);
        DrawLoadingDots();
    }

    private void DrawLoadingDots()
    {
        if (loadingStyle == null)
        {
            loadingStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.055f, 28f, 58f))
            };
        }

        int dotCount = 1 + (Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 3);
        string dots = dotCount == 1 ? "." : dotCount == 2 ? ". ." : ". . .";
        float height = Mathf.Clamp(Screen.height * 0.12f, 70f, 130f);
        Rect rect = new Rect(0f, Screen.height - height, Screen.width, height * 0.55f);
        float pulse = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7.5f));
        float jitterX = Mathf.Sin(Time.unscaledTime * 31f) * 2.5f;
        float jitterY = Mathf.Cos(Time.unscaledTime * 23f) * 1.5f;
        float glitchOffset = pulse > 0.72f ? Mathf.Lerp(2f, 7f, pulse) : 1f;

        GUIStyle shadowStyle = new GUIStyle(loadingStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.82f);
        GUI.Label(new Rect(rect.x + 4f + jitterX, rect.y + 4f + jitterY, rect.width, rect.height), dots, shadowStyle);

        GUIStyle redGhostStyle = new GUIStyle(loadingStyle);
        redGhostStyle.normal.textColor = new Color(0.85f, 0.02f, 0.015f, 0.48f);
        GUI.Label(new Rect(rect.x - glitchOffset, rect.y + jitterY, rect.width, rect.height), dots, redGhostStyle);

        GUIStyle coldGhostStyle = new GUIStyle(loadingStyle);
        coldGhostStyle.normal.textColor = new Color(0.18f, 0.88f, 0.78f, 0.14f);
        GUI.Label(new Rect(rect.x + glitchOffset, rect.y - 1f, rect.width, rect.height), dots, coldGhostStyle);

        loadingStyle.normal.textColor = new Color(0.95f, 0.07f, 0.045f, Mathf.Lerp(0.76f, 1f, pulse));
        GUI.Label(new Rect(rect.x + jitterX, rect.y + jitterY, rect.width, rect.height), dots, loadingStyle);
    }

    private static string GetVoicePath(VoiceClip voiceClip)
    {
        switch (voiceClip)
        {
            case VoiceClip.Easy:
                return EasyVoicePath;
            case VoiceClip.Hard:
                return HardVoicePath;
            default:
                return MediumVoicePath;
        }
    }

    private static string GetAmbiencePath(VoiceClip voiceClip)
    {
        return voiceClip == VoiceClip.Hard ? HardAmbiencePath : EasyAmbiencePath;
    }

    private static Rect GetCoverRect(float textureWidth, float textureHeight)
    {
        float screenAspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
        float textureAspect = textureWidth / Mathf.Max(1f, textureHeight);

        if (screenAspect > textureAspect)
        {
            float height = Screen.width / textureAspect;
            return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
        }

        float width = Screen.height * textureAspect;
        return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void PreloadTargetScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex >= 0)
        {
            sceneLoadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("DifficultyWarningTransition: scene absente des Build Settings: " + scenePath + ". Fallback par nom: " + sceneName);
            sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        if (sceneLoadOperation != null)
        {
            sceneLoadOperation.allowSceneActivation = false;
        }
    }

    private void ActivatePreloadedScene()
    {
        if (activationRequested)
        {
            return;
        }

        activationRequested = true;

        if (sceneLoadOperation != null)
        {
            sceneLoadOperation.allowSceneActivation = true;
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        Destroy(gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == sceneName)
        {
            Destroy(gameObject);
        }
    }
}

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
    private string scenePath;
    private string sceneName;
    private float remainingTime;

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

        LoadSceneExact(scenePath, sceneName);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
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

    private static void LoadSceneExact(string targetScenePath, string targetSceneName)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(targetScenePath);
        if (buildIndex >= 0)
        {
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            return;
        }

        Debug.LogWarning("DifficultyWarningTransition: scene absente des Build Settings: " + targetScenePath + ". Fallback par nom: " + targetSceneName);
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}

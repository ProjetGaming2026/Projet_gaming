using UnityEngine;

public static class AudioManager
{
    private const string MusicVolumeKey = "Options.MusicVolume";
    private const string SfxVolumeKey = "Options.SfxVolume";
    private const string AmbienceVolumeKey = "Options.AmbienceVolume";

    public static float MusicVolume { get; private set; } = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    public static float SfxVolume { get; private set; } = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    public static float AmbienceVolume { get; private set; } = PlayerPrefs.GetFloat(AmbienceVolumeKey, 1f);

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyGlobalVolume();
    }

    public static void SetSFXVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        ApplyGlobalVolume();
    }

    public static void SetAmbienceVolume(float value)
    {
        AmbienceVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(AmbienceVolumeKey, AmbienceVolume);
        ApplyGlobalVolume();
    }

    public static void ApplyGlobalVolume()
    {
        AudioListener.volume = 1f;
    }
}

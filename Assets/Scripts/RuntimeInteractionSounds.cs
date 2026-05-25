using UnityEngine;

public static class RuntimeInteractionSounds
{
    private const int SampleRate = 44100;

    private static AudioClip notebookOpen;
    private static AudioClip notebookClose;
    private static AudioClip flashlightOn;
    private static AudioClip flashlightOff;

    public static AudioClip NotebookOpen => notebookOpen != null
        ? notebookOpen
        : notebookOpen = CreatePageSound("Notebook_Open_Fallback", 0.36f, 1217, 0.42f, 0.18f);

    public static AudioClip NotebookClose => notebookClose != null
        ? notebookClose
        : notebookClose = CreatePageSound("Notebook_Close_Fallback", 0.26f, 2311, 0.28f, 0.34f);

    public static AudioClip FlashlightOn => flashlightOn != null
        ? flashlightOn
        : flashlightOn = CreateClickSound("Flashlight_On_Fallback", 0.09f, 940f, 0.52f);

    public static AudioClip FlashlightOff => flashlightOff != null
        ? flashlightOff
        : flashlightOff = CreateClickSound("Flashlight_Off_Fallback", 0.08f, 620f, 0.42f);

    private static AudioClip CreateClickSound(string clipName, float duration, float toneFrequency, float volume)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        float[] data = new float[sampleCount];
        System.Random random = new System.Random(clipName.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float normalized = i / Mathf.Max(1f, sampleCount - 1f);
            float envelope = Mathf.Exp(-normalized * 34f);
            float tone = Mathf.Sin(2f * Mathf.PI * toneFrequency * t) * envelope;
            float noise = ((float)random.NextDouble() * 2f - 1f) * envelope * 0.35f;
            data[i] = Mathf.Clamp((tone * 0.72f + noise) * volume, -1f, 1f);
        }

        return BuildClip(clipName, data);
    }

    private static AudioClip CreatePageSound(string clipName, float duration, int seed, float rustleVolume, float thumpVolume)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * SampleRate));
        float[] data = new float[sampleCount];
        System.Random random = new System.Random(seed);
        float smoothedNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float normalized = i / Mathf.Max(1f, sampleCount - 1f);
            float rustleEnvelope = Mathf.Sin(Mathf.PI * normalized);
            rustleEnvelope *= Mathf.Exp(-normalized * 1.8f);

            float rawNoise = (float)random.NextDouble() * 2f - 1f;
            smoothedNoise = Mathf.Lerp(smoothedNoise, rawNoise, 0.18f);

            float thumpEnvelope = Mathf.Exp(-Mathf.Pow(normalized * 9f, 2f));
            float thump = Mathf.Sin(2f * Mathf.PI * 115f * t) * thumpEnvelope * thumpVolume;
            float rustle = smoothedNoise * rustleEnvelope * rustleVolume;
            data[i] = Mathf.Clamp(thump + rustle, -1f, 1f);
        }

        return BuildClip(clipName, data);
    }

    private static AudioClip BuildClip(string clipName, float[] data)
    {
        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

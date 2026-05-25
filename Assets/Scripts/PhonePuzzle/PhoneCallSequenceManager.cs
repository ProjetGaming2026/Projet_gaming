using System;
using System.Collections;
using UnityEngine;

public class PhoneCallSequenceManager : MonoBehaviour
{
    [SerializeField] private SubtitleUI subtitleUI;
    [SerializeField, Min(0.1f)] private float fallbackLineDuration = 3.5f;
    [SerializeField, Min(0f)] private float extraSubtitleHoldTime = 1.25f;

    private Coroutine activeCall;

    public void PlayCall(PhoneCallData callData, AudioSource voiceSource, AudioClip fallbackVoiceClip, Action onFinished)
    {
        PlayCall(callData, voiceSource, fallbackVoiceClip, onFinished, CreateDefaultPuzzleOneLines());
    }

    public void PlayCall(PhoneCallData callData, AudioSource voiceSource, AudioClip fallbackVoiceClip, Action onFinished, PhoneCallData.SubtitleLine[] fallbackLines)
    {
        if (activeCall != null)
        {
            StopCoroutine(activeCall);
        }

        activeCall = StartCoroutine(PlayCallRoutine(callData, voiceSource, fallbackVoiceClip, onFinished, fallbackLines));
    }

    private IEnumerator PlayCallRoutine(PhoneCallData callData, AudioSource voiceSource, AudioClip fallbackVoiceClip, Action onFinished, PhoneCallData.SubtitleLine[] fallbackLines)
    {
        PhonePuzzleUI ui = PhonePuzzleUI.EnsureExists();
        if (subtitleUI == null)
        {
            subtitleUI = ui.Subtitles;
        }

        AudioClip fullVoiceClip = callData != null && callData.FullVoiceClip != null ? callData.FullVoiceClip : fallbackVoiceClip;
        if (voiceSource != null && fullVoiceClip != null)
        {
            voiceSource.loop = false;
            voiceSource.clip = fullVoiceClip;
            voiceSource.Play();
        }

        PhoneCallData.SubtitleLine[] lines = callData != null && callData.SubtitleLines != null && callData.SubtitleLines.Length > 0
            ? callData.SubtitleLines
            : fallbackLines;

        if (lines == null || lines.Length == 0)
        {
            lines = CreateDefaultPuzzleOneLines();
        }

        for (int i = 0; i < lines.Length; i++)
        {
            PhoneCallData.SubtitleLine line = lines[i];
            if (line == null)
            {
                continue;
            }

            if (voiceSource != null && line.voiceClip != null)
            {
                voiceSource.PlayOneShot(line.voiceClip);
            }

            if (subtitleUI != null)
            {
                subtitleUI.Show(line.text);
            }

            float lineDuration = line.duration > 0f ? line.duration : fallbackLineDuration;
            yield return new WaitForSeconds(Mathf.Max(0.1f, lineDuration + extraSubtitleHoldTime));
        }

        if (subtitleUI != null)
        {
            subtitleUI.Hide();
        }

        activeCall = null;
        onFinished?.Invoke();
    }

    private static PhoneCallData.SubtitleLine[] CreateDefaultPuzzleOneLines()
    {
        return new[]
        {
            new PhoneCallData.SubtitleLine { text = "They waited here, without names, without family, without exit.", duration = 4.5f },
            new PhoneCallData.SubtitleLine { text = "One of them was admitted, declared dead, then placed back into a room.", duration = 5f },
            new PhoneCallData.SubtitleLine { text = "Find the one the hospital tried to erase.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "When you know the answer, return to me.", duration = 4f },
            new PhoneCallData.SubtitleLine { text = "You have four minutes.", duration = 3.2f },
        };
    }
}

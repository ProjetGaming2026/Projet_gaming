using UnityEngine;

[CreateAssetMenu(fileName = "PhoneCallData", menuName = "Phone Puzzle/Phone Call Data")]
public class PhoneCallData : ScriptableObject
{
    [System.Serializable]
    public class SubtitleLine
    {
        [TextArea(2, 4)] public string text;
        [Min(0.1f)] public float duration = 3.5f;
        public AudioClip voiceClip;
    }

    [Header("Call Content")]
    [SerializeField] private AudioClip fullVoiceClip;
    [SerializeField] private SubtitleLine[] subtitleLines;

    public AudioClip FullVoiceClip => fullVoiceClip;
    public SubtitleLine[] SubtitleLines => subtitleLines;
}

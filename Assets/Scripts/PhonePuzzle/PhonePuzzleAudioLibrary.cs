using UnityEngine;

[CreateAssetMenu(fileName = "PhonePuzzleAudioLibrary", menuName = "Phone Puzzle/Audio Library")]
public sealed class PhonePuzzleAudioLibrary : ScriptableObject
{
    private const string ResourceName = "PhonePuzzleAudioLibrary";

    [Header("Phone Calls")]
    [SerializeField] private AudioClip phoneRingingLoop;
    [SerializeField] private AudioClip phoneCall1Voice;
    [SerializeField] private AudioClip phoneCall2Voice;
    [SerializeField] private AudioClip phoneCall3Voice;
    [SerializeField] private AudioClip phoneCall4Voice;
    [SerializeField] private AudioClip phoneCall5Voice;
    [SerializeField] private AudioClip finalPhoneVoice;
    [SerializeField] private AudioClip puzzle1CorrectAnswer;

    [Header("Puzzle 4 Instruments")]
    [SerializeField] private AudioClip clampVoice;
    [SerializeField] private AudioClip scalpelVoice;
    [SerializeField] private AudioClip scissorsVoice;
    [SerializeField] private AudioClip retractorHookVoice;
    [SerializeField] private AudioClip needleHolderVoice;
    [SerializeField] private AudioClip stethoscopeVoice;

    private static PhonePuzzleAudioLibrary instance;

    public AudioClip PhoneRingingLoop => phoneRingingLoop;
    public AudioClip PhoneCall1Voice => phoneCall1Voice;
    public AudioClip PhoneCall2Voice => phoneCall2Voice;
    public AudioClip PhoneCall3Voice => phoneCall3Voice;
    public AudioClip PhoneCall4Voice => phoneCall4Voice;
    public AudioClip PhoneCall5Voice => phoneCall5Voice;
    public AudioClip FinalPhoneVoice => finalPhoneVoice;
    public AudioClip Puzzle1CorrectAnswer => puzzle1CorrectAnswer;
    public AudioClip ClampVoice => clampVoice;
    public AudioClip ScalpelVoice => scalpelVoice;
    public AudioClip ScissorsVoice => scissorsVoice;
    public AudioClip RetractorHookVoice => retractorHookVoice;
    public AudioClip NeedleHolderVoice => needleHolderVoice;
    public AudioClip StethoscopeVoice => stethoscopeVoice;

    public static PhonePuzzleAudioLibrary Load()
    {
        if (instance == null)
        {
            instance = Resources.Load<PhonePuzzleAudioLibrary>(ResourceName);
        }

        return instance;
    }
}

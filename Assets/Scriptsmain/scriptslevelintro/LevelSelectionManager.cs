using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string easySceneName = "EasyScene";
    [SerializeField] private string mediumSceneName = "MediumScene";
    [SerializeField] private string hardSceneName = "HardScene";

    [Header("Audio Source")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Voice Clips")]
    [SerializeField] private AudioClip easyVoice;
    [SerializeField] private AudioClip mediumVoice;
    [SerializeField] private AudioClip hardVoice;

    [Header("Ambience Clips")]
    [SerializeField] private AudioClip easyAmbience;
    [SerializeField] private AudioClip mediumAmbience;
    [SerializeField] private AudioClip hardAmbience;

    [Header("Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button backButton;

    private bool isStarting;

    public void SelectEasy()
    {
        StartLevel("Easy", 13, easyVoice, easyAmbience, easySceneName);
    }

    public void SelectMedium()
    {
        StartLevel("Medium", 10, mediumVoice, mediumAmbience, mediumSceneName);
    }

    public void SelectHard()
    {
        StartLevel("Hard", 7, hardVoice, hardAmbience, hardSceneName);
    }

    private void StartLevel(string difficulty, int minutes, AudioClip voiceClip, AudioClip ambienceClip, string sceneName)
    {
        if (isStarting)
        {
            return;
        }

        StartCoroutine(PlayIntroThenLoad(difficulty, minutes, voiceClip, ambienceClip, sceneName));
    }

    private IEnumerator PlayIntroThenLoad(string difficulty, int minutes, AudioClip voiceClip, AudioClip ambienceClip, string sceneName)
    {
        isStarting = true;
        SetButtonsInteractable(false);

        PlayerPrefs.SetString("Difficulty", difficulty);
        PlayerPrefs.SetInt("LevelTimeMinutes", minutes);
        PlayerPrefs.Save();

        if (ambienceSource != null && ambienceClip != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.35f;
            ambienceSource.Play();
        }

        if (voiceSource != null && voiceClip != null)
        {
            voiceSource.clip = voiceClip;
            voiceSource.loop = false;
            voiceSource.volume = 1f;
            voiceSource.Play();

            yield return new WaitForSeconds(voiceClip.length);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (easyButton != null) easyButton.interactable = value;
        if (mediumButton != null) mediumButton.interactable = value;
        if (hardButton != null) hardButton.interactable = value;
        if (backButton != null) backButton.interactable = value;
    }
}

using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TMP_Text>(true);
        }

        Hide();
    }

    public void Configure(TMP_Text text)
    {
        string currentValue = timerText != null ? timerText.text : string.Empty;
        bool wasVisible = gameObject.activeSelf;
        timerText = text;

        if (timerText != null && !string.IsNullOrEmpty(currentValue))
        {
            timerText.text = currentValue;
        }

        gameObject.SetActive(wasVisible && !string.IsNullOrWhiteSpace(currentValue));
    }

    public void SetTime(float secondsRemaining)
    {
        if (timerText == null)
        {
            Debug.LogWarning("TimerUI has no TMP_Text assigned.");
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (timerText != null)
        {
            timerText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }
}

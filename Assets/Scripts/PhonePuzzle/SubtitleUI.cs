using System.Collections;
using TMPro;
using UnityEngine;

public class SubtitleUI : MonoBehaviour
{
    [SerializeField] private TMP_Text subtitleText;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (subtitleText == null)
        {
            subtitleText = GetComponentInChildren<TMP_Text>(true);
        }

        Hide();
    }

    public void Configure(TMP_Text text)
    {
        string currentMessage = subtitleText != null ? subtitleText.text : string.Empty;
        bool wasVisible = gameObject.activeSelf;
        subtitleText = text;

        if (subtitleText != null && !string.IsNullOrEmpty(currentMessage))
        {
            subtitleText.text = currentMessage;
        }

        gameObject.SetActive(wasVisible && !string.IsNullOrWhiteSpace(currentMessage));
    }

    public void Show(string message)
    {
        if (subtitleText == null)
        {
            Debug.LogWarning("SubtitleUI has no TMP_Text assigned.");
            return;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        subtitleText.text = message;
        gameObject.SetActive(true);
    }

    public void ShowTimed(string message, float seconds)
    {
        Show(message);

        if (seconds > 0f)
        {
            hideRoutine = StartCoroutine(HideAfter(seconds));
        }
    }

    public void Hide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (subtitleText != null)
        {
            subtitleText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        hideRoutine = null;
        Hide();
    }
}

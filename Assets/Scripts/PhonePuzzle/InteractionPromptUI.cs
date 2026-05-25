using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [SerializeField] private TMP_Text promptText;

    private void Awake()
    {
        Instance = this;

        if (promptText == null)
        {
            promptText = GetComponentInChildren<TMP_Text>(true);
        }

        Hide();
    }

    public void Configure(TMP_Text text)
    {
        string currentPrompt = promptText != null ? promptText.text : string.Empty;
        bool wasVisible = gameObject.activeSelf;
        promptText = text;

        if (promptText != null && !string.IsNullOrEmpty(currentPrompt))
        {
            promptText.text = currentPrompt;
        }

        gameObject.SetActive(wasVisible && !string.IsNullOrWhiteSpace(currentPrompt));
    }

    public void Show(string message)
    {
        if (promptText == null)
        {
            Debug.LogWarning("InteractionPromptUI has no TMP_Text assigned.");
            return;
        }

        promptText.text = message;
        gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
    }

    public void Hide()
    {
        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }
}

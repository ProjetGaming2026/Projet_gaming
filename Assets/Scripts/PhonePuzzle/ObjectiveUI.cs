using TMPro;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;

    private void Awake()
    {
        if (objectiveText == null)
        {
            objectiveText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Configure(TMP_Text text)
    {
        objectiveText = text;
    }

    public void SetObjective(string objective)
    {
        if (objectiveText == null)
        {
            Debug.LogWarning("ObjectiveUI has no TMP_Text assigned.");
            return;
        }

        objectiveText.text = objective;
        gameObject.SetActive(!string.IsNullOrWhiteSpace(objective));
    }

    public void Hide()
    {
        if (objectiveText != null)
        {
            objectiveText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }
}

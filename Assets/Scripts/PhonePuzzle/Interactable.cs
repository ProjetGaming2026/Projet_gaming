using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for raycast interactions. Put this on objects that should respond
/// to the player's interaction key, or derive from it for puzzle-specific logic.
/// Assign prompt text and optional events in the Inspector.
/// </summary>
public class Interactable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private bool canInteract = true;
    [SerializeField] private string promptText = "Press E to interact";

    [Header("Events")]
    public UnityEvent OnInteracted = new UnityEvent();

    public virtual bool CanInteract => canInteract;

    public virtual string GetInteractPrompt()
    {
        return promptText;
    }

    public virtual void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        OnInteracted.Invoke();
    }

    public void SetInteractable(bool value)
    {
        canInteract = value;
    }

    public void SetPromptText(string value)
    {
        promptText = value;
    }
}

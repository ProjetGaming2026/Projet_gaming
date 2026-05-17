using System.Collections;
using UnityEngine;

public class PadlockButton : MonoBehaviour, IInteractable
{
    [SerializeField] private float flashDuration = 0.08f;

    private PadlockController controller;
    private string keyValue;
    private Renderer buttonRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashRoutine;
    private bool initialized;

    public void Init(PadlockController controller, string keyValue)
    {
        this.controller = controller;
        this.keyValue = keyValue;

        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            originalColor = buttonRenderer.material.color;
        }

        initialized = true;
        Debug.Log("PadlockButton: initialized " + name + " with key " + keyValue + ".");
    }

    private void OnMouseDown()
    {
        PressPhysicalButton();
    }

    public void Interact()
    {
        if (!initialized || controller == null)
        {
            return;
        }

        if (!controller.IsActive())
        {
            controller.Interact();
            return;
        }

        PressPhysicalButton();
    }

    public bool TryHandlePromptKeyDown()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return false;
        }

        Interact();
        return true;
    }

    public string GetInteractPrompt()
    {
        if (!initialized || controller == null)
        {
            return "Press E to interact";
        }

        if (!controller.IsActive())
        {
            return "Press E to use padlock";
        }

        return "Click or press E: " + keyValue;
    }

    private void PressPhysicalButton()
    {
        if (!initialized || controller == null)
        {
            return;
        }

        if (!controller.IsActive())
        {
            Debug.Log("PadlockButton: ignored " + name + " because the padlock is not active.");
            return;
        }

        controller.OnKeyPressed(keyValue);

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashWhite());
    }

    private IEnumerator FlashWhite()
    {
        if (buttonRenderer == null)
        {
            yield break;
        }

        buttonRenderer.material.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        buttonRenderer.material.color = originalColor;
        flashRoutine = null;
    }
}

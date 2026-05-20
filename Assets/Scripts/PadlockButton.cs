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
            return GameLocalization.Text("Press E to interact", "Appuie sur E pour interagir", "Pulsa E para interactuar");
        }

        if (!controller.IsActive())
        {
            return GameLocalization.Text("Press E to use padlock", "Appuie sur E pour utiliser le cadenas", "Pulsa E para usar el candado");
        }

        return GameLocalization.Text("Click or press E: ", "Clique ou appuie sur E : ", "Haz clic o pulsa E: ") + keyValue;
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

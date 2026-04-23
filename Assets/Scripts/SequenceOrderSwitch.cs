using UnityEngine;
using UnityEngine.Events;

public class SequenceOrderSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] private SequenceOrderPuzzle puzzle;
    [SerializeField] private int switchId;
    [SerializeField] private bool disableInteractionWhenPressed = true;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color defaultColor = Color.red;
    [SerializeField] private Color pressedColor = Color.green;

    [Header("Events")]
    [SerializeField] private UnityEvent onPressed;
    [SerializeField] private UnityEvent onReset;
    [SerializeField] private UnityEvent onSolvedLocked;

    public int SwitchId => switchId;

    private bool isPressed;
    private bool isLocked;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (puzzle == null)
        {
            puzzle = GetComponentInParent<SequenceOrderPuzzle>();
        }

        if (puzzle != null)
        {
            puzzle.RegisterSwitch(this);
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            propertyBlock = new MaterialPropertyBlock();

            UpdateVisual(defaultColor, false);
        }
        else
        {
            Debug.LogWarning("Aucun Renderer trouve sur " + name);
        }
    }

    public void Interact()
    {
        Debug.Log("Interaction avec switch " + switchId);

        if (puzzle == null || isLocked)
        {
            Debug.Log("Switch " + switchId + " ignore: puzzle manquant ou switch verrouille.");
            return;
        }

        if (disableInteractionWhenPressed && isPressed)
        {
            Debug.Log("Switch " + switchId + " ignore: deja presse.");
            return;
        }

        bool accepted = puzzle.TryPressSwitch(this);
        Debug.Log("Resultat interaction switch " + switchId + " = " + accepted);
    }

    public void SetPressedState(bool pressed)
    {
        if (isLocked)
        {
            return;
        }

        bool stateChanged = isPressed != pressed;
        isPressed = pressed;

        if (!stateChanged)
        {
            return;
        }

        if (isPressed)
        {
            Debug.Log("Switch " + switchId + " devient vert.");
            UpdateVisual(pressedColor);
            onPressed.Invoke();
        }
        else
        {
            Debug.Log("Switch " + switchId + " revient a la couleur d'origine.");
            UpdateVisual(defaultColor);
            onReset.Invoke();
        }
    }

    public void LockSolvedState()
    {
        isLocked = true;
        isPressed = true;
        UpdateVisual(pressedColor);
        onSolvedLocked.Invoke();
    }

    private void UpdateVisual(Color color)
    {
        UpdateVisual(color, true);
    }

    private void UpdateVisual(Color color, bool enableEmission)
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(EmissionColorId, enableEmission ? color * 0.5f : Color.black);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}

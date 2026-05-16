using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SequenceOrderPuzzle : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private int[] expectedSequence = { 0, 1, 2, 3, 4 };
    [SerializeField] private bool requireUniqueSwitches = true;

    [Header("Progression")]
    [SerializeField] private bool countAsTask = true;
    [SerializeField] private bool restrictToChasePhase = true;

    [Header("Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;
    [SerializeField] private string successMessage = "Task valid";
    [SerializeField] private string failureMessage = "Sequence invalide";
    [SerializeField] private float statusDuration = 2f;

    [Header("Instruction Text")]
    [SerializeField] private bool showInstructionText = true;
    [SerializeField] private string instructionText = "Press the symbols in the correct order.";
    [SerializeField] private Vector3 instructionLocalPosition = new Vector3(0f, 1.6f, -0.05f);
    [SerializeField] private Vector3 instructionLocalEulerAngles = new Vector3(0f, 90f, 0f);
    [SerializeField] private int instructionFontSize = 48;
    [SerializeField] private float instructionCharacterSize = 0.05f;
    [SerializeField] private Color instructionColor = Color.white;
    [SerializeField] private float instructionMaxVisibleDistance = 12f;
    [SerializeField] private bool instructionVisibleFromFrontOnly = true;
    [SerializeField, Range(-1f, 1f)] private float instructionMinFacingDot = 0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;
    [SerializeField] private UnityEvent onFailed;

    private readonly List<int> enteredSequence = new();
    private readonly List<SequenceOrderSwitch> registeredSwitches = new();

    private PrototypeGameManager gameManager;
    private PrototypeHUD hud;
    private TextMesh runtimeInstructionText;
    private Renderer runtimeInstructionRenderer;
    private Camera mainCamera;
    private bool isSolved;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        hud = FindFirstObjectByType<PrototypeHUD>();
        EnsureRuntimeInstructionText();
    }

    private void LateUpdate()
    {
        UpdateRuntimeInstructionText();
    }

    public void RegisterSwitch(SequenceOrderSwitch sequenceSwitch)
    {
        if (sequenceSwitch == null || registeredSwitches.Contains(sequenceSwitch))
        {
            return;
        }

        registeredSwitches.Add(sequenceSwitch);
    }

    public bool TryPressSwitch(SequenceOrderSwitch sequenceSwitch)
    {
        if (sequenceSwitch == null || isSolved)
        {
            Debug.Log("Switch ignore: puzzle resolu ou switch null.");
            return false;
        }

        if (restrictToChasePhase && gameManager != null && gameManager.CurrentPhase != PrototypeGameManager.GamePhase.Chase)
        {
            Debug.Log("Switch refuse: puzzle disponible seulement en phase CHASE. Phase actuelle = " + gameManager.CurrentPhase);
            return false;
        }

        if (expectedSequence == null || expectedSequence.Length == 0)
        {
            Debug.LogWarning("Aucune sequence attendue configuree.");
            return false;
        }

        if (enteredSequence.Count >= expectedSequence.Length)
        {
            Debug.Log("Switch refuse: sequence deja complete.");
            return false;
        }

        if (requireUniqueSwitches && enteredSequence.Contains(sequenceSwitch.SwitchId))
        {
            Debug.Log("Switch refuse: switch " + sequenceSwitch.SwitchId + " deja selectionne.");
            return false;
        }

        enteredSequence.Add(sequenceSwitch.SwitchId);
        Debug.Log("Switch accepte: " + sequenceSwitch.SwitchId + " | progression " + enteredSequence.Count + "/" + expectedSequence.Length);
        sequenceSwitch.SetPressedState(true);

        if (enteredSequence.Count < expectedSequence.Length)
        {
            return true;
        }

        ValidateSequence();
        return true;
    }

    public void ResetAttempt()
    {
        enteredSequence.Clear();

        foreach (SequenceOrderSwitch sequenceSwitch in registeredSwitches)
        {
            if (sequenceSwitch != null)
            {
                sequenceSwitch.SetPressedState(false);
            }
        }
    }

    private void ValidateSequence()
    {
        bool isValid = true;

        for (int i = 0; i < expectedSequence.Length; i++)
        {
            if (enteredSequence[i] != expectedSequence[i])
            {
                isValid = false;
                break;
            }
        }

        if (isValid)
        {
            CompletePuzzle();
            return;
        }

        PlayClip(failureClip);
        if (hud != null)
        {
            hud.ShowTemporaryStatus(failureMessage, Color.red, statusDuration);
        }

        onFailed.Invoke();
        ResetAttempt();
    }

    private void CompletePuzzle()
    {
        isSolved = true;

        if (countAsTask && gameManager != null)
        {
            gameManager.RegisterTaskSuccess();
        }

        foreach (SequenceOrderSwitch sequenceSwitch in registeredSwitches)
        {
            if (sequenceSwitch != null)
            {
                sequenceSwitch.LockSolvedState();
            }
        }

        PlayClip(successClip);
        if (hud != null)
        {
            hud.ShowTemporaryStatus(successMessage, Color.green, statusDuration);
        }

        onSolved.Invoke();
        Debug.Log("Puzzle sequence resolu.");
    }

    private void EnsureRuntimeInstructionText()
    {
        if (!showInstructionText || GetComponent<WallInstructionText>() != null)
        {
            return;
        }

        Transform existingText = transform.Find("InstructionText");
        if (existingText == null)
        {
            GameObject textObject = new GameObject("InstructionText");
            existingText = textObject.transform;
            existingText.SetParent(transform, false);
        }

        runtimeInstructionText = existingText.GetComponent<TextMesh>();
        if (runtimeInstructionText == null)
        {
            runtimeInstructionText = existingText.gameObject.AddComponent<TextMesh>();
        }

        runtimeInstructionText.transform.localPosition = instructionLocalPosition;
        runtimeInstructionText.transform.localEulerAngles = instructionLocalEulerAngles;
        runtimeInstructionText.transform.localScale = Vector3.one;
        runtimeInstructionText.text = instructionText;
        runtimeInstructionText.anchor = TextAnchor.MiddleCenter;
        runtimeInstructionText.alignment = TextAlignment.Center;
        runtimeInstructionText.fontSize = instructionFontSize;
        runtimeInstructionText.characterSize = instructionCharacterSize;
        runtimeInstructionText.color = instructionColor;
        runtimeInstructionText.fontStyle = FontStyle.Bold;

        runtimeInstructionRenderer = runtimeInstructionText.GetComponent<Renderer>();
        if (runtimeInstructionRenderer != null)
        {
            runtimeInstructionRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            runtimeInstructionRenderer.receiveShadows = false;
            runtimeInstructionRenderer.sortingOrder = 10;
        }
    }

    private void UpdateRuntimeInstructionText()
    {
        if (runtimeInstructionText == null)
        {
            return;
        }

        if (runtimeInstructionRenderer == null)
        {
            runtimeInstructionRenderer = runtimeInstructionText.GetComponent<Renderer>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            if (runtimeInstructionRenderer != null)
            {
                runtimeInstructionRenderer.enabled = true;
            }

            return;
        }

        Transform textTransform = runtimeInstructionText.transform;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 toCamera = cameraPosition - textTransform.position;
        float distance = toCamera.magnitude;
        bool isVisible = distance <= instructionMaxVisibleDistance;

        if (isVisible && instructionVisibleFromFrontOnly && distance > 0.001f)
        {
            Vector3 toCameraDirection = toCamera / distance;
            float facingDot = Vector3.Dot(-textTransform.forward, toCameraDirection);
            isVisible = facingDot >= instructionMinFacingDot;
        }

        if (runtimeInstructionRenderer != null)
        {
            runtimeInstructionRenderer.enabled = isVisible;
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

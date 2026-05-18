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

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;
    [SerializeField] private UnityEvent onFailed;

    private readonly List<int> enteredSequence = new();
    private readonly List<SequenceOrderSwitch> registeredSwitches = new();

    private PrototypeGameManager gameManager;
    private PrototypeHUD hud;
    private BasicMonsterController monsterController;
    private bool isSolved;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
        hud = FindFirstObjectByType<PrototypeHUD>();
        monsterController = FindFirstObjectByType<BasicMonsterController>();
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
        AlertMonsterOnFailure();
        ResetAttempt();
    }

    private void AlertMonsterOnFailure()
    {
        if (monsterController == null)
        {
            monsterController = FindFirstObjectByType<BasicMonsterController>();
        }

        if (monsterController != null)
        {
            monsterController.AlertToPlayerPosition(8f, true);
        }
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

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

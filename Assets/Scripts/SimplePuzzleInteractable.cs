using UnityEngine;
using UnityEngine.Events;

public class SimplePuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("Puzzle")]
    [SerializeField, Range(0f, 1f)] private float successChance = 0.5f;
    [SerializeField] private bool disableAfterSuccess = true;
    [SerializeField] private bool countTowardWinCondition = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFailure;

    private bool isSolved;

    public void Interact()
    {
        if (isSolved)
        {
            return;
        }

        bool isSuccess = Random.value <= successChance;

        if (isSuccess)
        {
            isSolved = true;

            if (countTowardWinCondition && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPuzzleSolved();
            }

            onSuccess.Invoke();

            if (disableAfterSuccess)
            {
                enabled = false;
            }

            return;
        }

        onFailure.Invoke();
    }
}

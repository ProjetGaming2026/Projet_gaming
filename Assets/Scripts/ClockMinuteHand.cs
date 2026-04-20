using UnityEngine;

// À mettre sur l'aiguille des MINUTES
public class ClockMinuteHand : MonoBehaviour, IInteractable
{
    [SerializeField] private int currentMinute = 0;
    [SerializeField] private ClockPuzzle clockPuzzle;

    public int CurrentMinute => currentMinute;

    public void Interact()
    {
        currentMinute += 5;
        if (currentMinute >= 60) currentMinute = 0;

        // Rotation visuelle de l'aiguille
        float angle = -(currentMinute / 60f) * 360f;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        Debug.Log("Minutes : " + currentMinute);

        if (clockPuzzle != null)
            clockPuzzle.CheckSolution();
    }
}

using UnityEngine;

// À mettre sur l'aiguille des HEURES
public class ClockHourHand : MonoBehaviour, IInteractable
{
    [SerializeField] private int currentHour = 12;
    [SerializeField] private ClockPuzzle clockPuzzle;

    public int CurrentHour => currentHour;

    public void Interact()
    {
        currentHour++;
        if (currentHour > 12) currentHour = 1;

        // Rotation visuelle de l'aiguille
        float angle = -(currentHour / 12f) * 360f;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        Debug.Log("Heures : " + currentHour);

        if (clockPuzzle != null)
            clockPuzzle.CheckSolution();
    }
}

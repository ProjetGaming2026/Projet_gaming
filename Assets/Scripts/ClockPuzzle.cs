using UnityEngine;
using UnityEngine.Events;

// À mettre sur l'objet Horloge principal
public class ClockPuzzle : MonoBehaviour
{
    [Header("Aiguilles")]
    [SerializeField] private ClockHourHand hourHand;
    [SerializeField] private ClockMinuteHand minuteHand;

    [Header("Indices sur les murs (assigne tous les murs possibles)")]
    [SerializeField] private ClockHint[] hintObjects;

    [Header("Intégration GameManager")]
    [SerializeField] private bool countAsTask = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onSolved;
    [SerializeField] private UnityEvent onWrongAnswer;

    public int CorrectHour { get; private set; }
    public int CorrectMinute { get; private set; }

    private bool isSolved;

    private void Start()
    {
        GenerateRandomSolution();
    }

    private void GenerateRandomSolution()
    {
        // Heure et minutes aléatoires
        CorrectHour = Random.Range(1, 13);
        CorrectMinute = Random.Range(0, 12) * 5;

        Debug.Log("Solution : " + CorrectHour + "h" + CorrectMinute.ToString("D2"));

        if (hintObjects == null || hintObjects.Length < 2)
        {
            Debug.LogWarning("Il faut au moins 2 murs hint assignés !");
            return;
        }

        // Cache tous les murs
        foreach (ClockHint hint in hintObjects)
            if (hint != null) hint.Hide();

        // Mélange les murs aléatoirement
        ClockHint[] shuffled = (ClockHint[])hintObjects.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            ClockHint temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        // 2 murs aléatoires choisis pour afficher les indices
        shuffled[0].ShowHour(CorrectHour);
        shuffled[1].ShowMinute(CorrectMinute);
    }

    public void CheckSolution()
    {
        if (isSolved) return;
        if (hourHand == null || minuteHand == null) return;

        bool hourCorrect = hourHand.CurrentHour == CorrectHour;
        bool minuteCorrect = minuteHand.CurrentMinute == CorrectMinute;

        if (hourCorrect && minuteCorrect)
        {
            isSolved = true;
            Debug.Log("Puzzle horloge résolu !");

            if (countAsTask)
            {
                PrototypeGameManager gm = FindFirstObjectByType<PrototypeGameManager>();
                if (gm != null) gm.RegisterTaskSuccess();
            }

            onSolved.Invoke();
        }
        else
        {
            onWrongAnswer.Invoke();
        }
    }
}

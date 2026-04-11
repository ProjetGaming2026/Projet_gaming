using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GamePhase
    {
        MainMenu,
        Playing,
        Won,
        Lost
    }

    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GamePhase startingPhase = GamePhase.Playing;
    [SerializeField] private int requiredPuzzleCount = 1;

    [Header("Events")]
    [SerializeField] private UnityEvent onGameStarted;
    [SerializeField] private UnityEvent onGameWon;
    [SerializeField] private UnityEvent onGameLost;

    public GamePhase CurrentPhase { get; private set; }
    public int SolvedPuzzleCount { get; private set; }
    public int RequiredPuzzleCount => requiredPuzzleCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetPhase(startingPhase);
    }

    public void StartGame()
    {
        SolvedPuzzleCount = 0;
        SetPhase(GamePhase.Playing);
        onGameStarted.Invoke();
    }

    public void RegisterPuzzleSolved()
    {
        if (CurrentPhase != GamePhase.Playing)
        {
            return;
        }

        SolvedPuzzleCount++;

        if (SolvedPuzzleCount >= requiredPuzzleCount)
        {
            WinGame();
        }
    }

    public void WinGame()
    {
        if (CurrentPhase == GamePhase.Won || CurrentPhase == GamePhase.Lost)
        {
            return;
        }

        SetPhase(GamePhase.Won);
        onGameWon.Invoke();
    }

    public void LoseGame()
    {
        if (CurrentPhase == GamePhase.Won || CurrentPhase == GamePhase.Lost)
        {
            return;
        }

        SetPhase(GamePhase.Lost);
        onGameLost.Invoke();
    }

    public void SetPhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
    }
}

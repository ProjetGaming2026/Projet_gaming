using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BreakerPanelManager : MonoBehaviour
{
    public static BreakerPanelManager Instance;

    [Header("Task Progression")]
    [SerializeField] private bool countAsTask = true;

    [Header("Les 4 switches dans l'ordre du puzzle")]
    public BreakerSwitch redSwitch;
    public BreakerSwitch blueSwitch;
    public BreakerSwitch yellowSwitch;
    public BreakerSwitch blackSwitch;

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip failSound;
    public AudioClip successSound;

    [Header("Monstre")]
    public GameObject monster;

    [Header("Lumieres de la piece")]
    public Light roomLight;
    public Light roomLight1;

    private BreakerSwitch[] correctOrder;
    private readonly BreakerSwitch.SwitchDirection[] correctDirections =
    {
        BreakerSwitch.SwitchDirection.UP,
        BreakerSwitch.SwitchDirection.DOWN,
        BreakerSwitch.SwitchDirection.DOWN,
        BreakerSwitch.SwitchDirection.DOWN
    };

    private readonly List<int> moveOrder = new List<int>();
    private bool puzzleSolved;
    private bool isResetting;
    private bool isChecking;
    private BreakerSwitch[] allSwitches;
    private PrototypeGameManager gameManager;

    private void Awake()
    {
        Instance = this;
        gameManager = FindFirstObjectByType<PrototypeGameManager>();
    }

    private void Start()
    {
        allSwitches = new[] { redSwitch, blueSwitch, yellowSwitch, blackSwitch };
        correctOrder = new[] { redSwitch, blueSwitch, yellowSwitch, blackSwitch };

        if (redSwitch != null)
        {
            redSwitch.switchIndex = 0;
            redSwitch.switchName = "Rouge";
            redSwitch.nameColor = Color.red;
            redSwitch.correctDirection = BreakerSwitch.SwitchDirection.UP;
        }

        if (blueSwitch != null)
        {
            blueSwitch.switchIndex = 1;
            blueSwitch.switchName = "Bleu";
            blueSwitch.nameColor = Color.cyan;
            blueSwitch.correctDirection = BreakerSwitch.SwitchDirection.DOWN;
        }

        if (yellowSwitch != null)
        {
            yellowSwitch.switchIndex = 2;
            yellowSwitch.switchName = "Jaune";
            yellowSwitch.nameColor = Color.yellow;
            yellowSwitch.correctDirection = BreakerSwitch.SwitchDirection.DOWN;
        }

        if (blackSwitch != null)
        {
            blackSwitch.switchIndex = 3;
            blackSwitch.switchName = "Noir";
            blackSwitch.nameColor = Color.grey;
            blackSwitch.correctDirection = BreakerSwitch.SwitchDirection.DOWN;
        }
    }

    public void OnSwitchChanged(int switchIndex, BreakerSwitch.SwitchState newState)
    {
        if (puzzleSolved || isResetting || isChecking)
        {
            return;
        }

        PlaySound(clickSound);

        if (newState == BreakerSwitch.SwitchState.Idle)
        {
            moveOrder.Remove(switchIndex);
            return;
        }

        if (!moveOrder.Contains(switchIndex))
        {
            moveOrder.Add(switchIndex);
        }

        bool allMoved = true;
        foreach (BreakerSwitch breakerSwitch in allSwitches)
        {
            if (breakerSwitch == null)
            {
                continue;
            }

            if (breakerSwitch.GetState() == BreakerSwitch.SwitchState.Idle)
            {
                allMoved = false;
                break;
            }
        }

        if (allMoved)
        {
            isChecking = true;
            StartCoroutine(CheckPuzzle());
        }
    }

    private IEnumerator CheckPuzzle()
    {
        yield return new WaitForSeconds(0.5f);

        bool correct = true;

        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (i >= moveOrder.Count || moveOrder[i] != correctOrder[i].switchIndex)
            {
                correct = false;
                Debug.Log("Mauvais ordre a l'etape " + (i + 1));
                break;
            }

            BreakerSwitch.SwitchState state = allSwitches[moveOrder[i]].GetState();
            bool directionMatches =
                (state == BreakerSwitch.SwitchState.Up && correctDirections[i] == BreakerSwitch.SwitchDirection.UP) ||
                (state == BreakerSwitch.SwitchState.Down && correctDirections[i] == BreakerSwitch.SwitchDirection.DOWN);

            if (!directionMatches)
            {
                correct = false;
                Debug.Log("Mauvaise direction a l'etape " + (i + 1));
                break;
            }
        }

        isChecking = false;

        if (correct)
        {
            StartCoroutine(SuccessSequence());
        }
        else
        {
            StartCoroutine(FailSequence());
        }
    }

    private IEnumerator FailSequence()
    {
        if (puzzleSolved)
        {
            yield break;
        }

        isResetting = true;
        LockAll(true);
        PlaySound(failSound);

        yield return new WaitForSeconds(0.3f);

        foreach (BreakerSwitch breakerSwitch in allSwitches)
        {
            if (breakerSwitch != null)
            {
                breakerSwitch.ResetSwitch();
            }
        }

        moveOrder.Clear();
        AlertMonster();

        yield return new WaitForSeconds(0.5f);
        LockAll(false);
        isResetting = false;
    }

    private IEnumerator SuccessSequence()
    {
        puzzleSolved = true;
        LockAll(true);
        PlaySound(successSound);

        if (countAsTask)
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<PrototypeGameManager>();
            }

            if (gameManager != null)
            {
                gameManager.RegisterTaskSuccess();
            }
        }

        Debug.Log("Puzzle resolu. Courant retabli.");

        yield return new WaitForSeconds(0.5f);

        if (roomLight != null)
        {
            roomLight.intensity = 3f;
        }

        if (roomLight1 != null)
        {
            roomLight1.intensity = 3f;
        }

        Debug.Log("Lampes allumees.");
    }

    private void AlertMonster()
    {
        PuzzleFailureMonsterAlert.Alert(monster, transform);
    }

    private void LockAll(bool state)
    {
        foreach (BreakerSwitch breakerSwitch in allSwitches)
        {
            if (breakerSwitch != null)
            {
                breakerSwitch.Lock(state);
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

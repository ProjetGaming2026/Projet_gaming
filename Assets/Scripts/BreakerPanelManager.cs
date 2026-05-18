using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BreakerPanelManager : MonoBehaviour
{
    public static BreakerPanelManager Instance;

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

    [Header("Lumières de la pièce")]
    public Light roomLight;
    public Light roomLight1;

    private BreakerSwitch[] correctOrder;
    private BreakerSwitch.SwitchDirection[] correctDirections =
    {
        BreakerSwitch.SwitchDirection.UP,
        BreakerSwitch.SwitchDirection.DOWN,
        BreakerSwitch.SwitchDirection.DOWN,
        BreakerSwitch.SwitchDirection.DOWN
    };

    private List<int> moveOrder = new List<int>();
    private bool puzzleSolved = false;
    private bool isResetting  = false;
    private bool isChecking   = false;
    private BreakerSwitch[] allSwitches;

    void Awake() => Instance = this;

    void Start()
    {
        allSwitches  = new BreakerSwitch[] { redSwitch, blueSwitch, yellowSwitch, blackSwitch };
        correctOrder = new BreakerSwitch[] { redSwitch, blueSwitch, yellowSwitch, blackSwitch };

        if (redSwitch    != null) { redSwitch.switchIndex    = 0; redSwitch.switchName    = "Red";    redSwitch.nameColor    = Color.red;    redSwitch.correctDirection    = BreakerSwitch.SwitchDirection.UP;   }
        if (blueSwitch   != null) { blueSwitch.switchIndex   = 1; blueSwitch.switchName   = "Blue";   blueSwitch.nameColor   = Color.cyan;   blueSwitch.correctDirection   = BreakerSwitch.SwitchDirection.DOWN; }
        if (yellowSwitch != null) { yellowSwitch.switchIndex  = 2; yellowSwitch.switchName = "Yellow"; yellowSwitch.nameColor = Color.yellow; yellowSwitch.correctDirection  = BreakerSwitch.SwitchDirection.DOWN; }
        if (blackSwitch  != null) { blackSwitch.switchIndex   = 3; blackSwitch.switchName  = "Black";  blackSwitch.nameColor  = Color.grey;   blackSwitch.correctDirection   = BreakerSwitch.SwitchDirection.DOWN; }


    }

    public void OnSwitchChanged(int switchIndex, BreakerSwitch.SwitchState newState)
    {
        if (puzzleSolved || isResetting || isChecking) return;

        PlaySound(clickSound);

        if (newState == BreakerSwitch.SwitchState.Idle)
        {
            moveOrder.Remove(switchIndex);
            return;
        }

        if (!moveOrder.Contains(switchIndex))
            moveOrder.Add(switchIndex);

        bool allMoved = true;
        foreach (var sw in allSwitches)
        {
            if (sw == null) continue;
            if (sw.GetState() == BreakerSwitch.SwitchState.Idle)
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

    IEnumerator CheckPuzzle()
    {
        yield return new WaitForSeconds(0.5f);

        bool correct = true;

        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (i >= moveOrder.Count || moveOrder[i] != correctOrder[i].switchIndex)
            {
                correct = false;
                Debug.Log($"❌ Mauvais ordre à l'étape {i + 1}");
                break;
            }

            var state = allSwitches[moveOrder[i]].GetState();
            bool dirOk = (state == BreakerSwitch.SwitchState.Up   && correctDirections[i] == BreakerSwitch.SwitchDirection.UP)
                      || (state == BreakerSwitch.SwitchState.Down  && correctDirections[i] == BreakerSwitch.SwitchDirection.DOWN);

            if (!dirOk)
            {
                correct = false;
                Debug.Log($"❌ Mauvaise direction à l'étape {i + 1}");
                break;
            }
        }

        isChecking = false;

        if (correct)
            StartCoroutine(SuccessSequence());
        else
            StartCoroutine(FailSequence());
    }

    IEnumerator FailSequence()
    {
        // Ne jamais toucher aux lampes si puzzle résolu
        if (puzzleSolved) yield break;

        isResetting = true;
        LockAll(true);
        PlaySound(failSound);

        // Pas de flicker sur les lampes en cas d'échec

        yield return new WaitForSeconds(0.3f);

        foreach (var sw in allSwitches)
            if (sw != null) sw.ResetSwitch();

        moveOrder.Clear();
        AlertMonster();

        yield return new WaitForSeconds(0.5f);
        LockAll(false);
        isResetting = false;
    }

    IEnumerator SuccessSequence()
    {
        puzzleSolved = true;
        LockAll(true);
        PlaySound(successSound);

        Debug.Log("✅ Puzzle résolu ! Courant rétabli.");

        // Attendre que FailSequence finisse son flicker
        yield return new WaitForSeconds(0.5f);

        // Allumer les lampes définitivement
        if (roomLight != null)
        {
            roomLight.intensity = 3f; 
        }
        if (roomLight1 != null)
        {
            roomLight1.intensity = 3f;
        }

        Debug.Log("💡 Lampes allumées !");
    }

    void AlertMonster()
    {
        if (monster == null) { Debug.Log("⚠️ Pas de monstre assigné !"); return; }
        var agent = monster.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            agent.SetDestination(player.position);
            Debug.Log("👹 Monstre alerté !");
        }
    }

    void LockAll(bool state)
    {
        foreach (var sw in allSwitches)
            if (sw != null) sw.Lock(state);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
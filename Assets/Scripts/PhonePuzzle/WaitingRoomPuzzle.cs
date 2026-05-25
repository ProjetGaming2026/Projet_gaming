using System;
using UnityEngine;
using UnityEngine.Events;

public class WaitingRoomPuzzle : MonoBehaviour
{
    public const string AdmissionText =
        "ADMISSION LIST\n" +
        "201 - Admitted\n" +
        "317 - Admitted\n" +
        "402 - Admitted\n" +
        "115 - Admitted";

    public const string DeathRecordsText =
        "DEATH RECORDS\n" +
        "402 - Deceased\n" +
        "666 - Deceased\n" +
        "317 - Deceased\n" +
        "204 - Deceased";

    public const string RoomAssignmentsText =
        "ROOM ASSIGNMENTS\n" +
        "201 - Room A-03\n" +
        "204 - Room B-11\n" +
        "317 - Room B-12\n" +
        "402 - Room C-04";

    [Header("Puzzle 1")]
    [SerializeField] private string correctAnswer = "317";
    [SerializeField, Min(1f)] private float puzzleTimeLimit = 240f;
    [SerializeField] private bool requireAllListsInspectedBeforeAnswer = true;

    [Header("Scene References")]
    [SerializeField] private Transform wallPhoneAnchor;
    [SerializeField] private ReadableWallList admissionList;
    [SerializeField] private ReadableWallList deathRecords;
    [SerializeField] private ReadableWallList roomAssignments;
    [SerializeField] private GameObject patientInputTerminal;

    [Header("Events")]
    public UnityEvent OnPuzzleActivated = new UnityEvent();
    public UnityEvent OnPuzzleCompleted = new UnityEvent();
    public UnityEvent OnTimerExpired = new UnityEvent();

    public event Action TimerExpired;
    public event Action PuzzleCompleted;

    private bool isActive;
    private bool isCompleted;
    private bool timerExpired;
    private float timeRemaining;
    private PhonePuzzleUI ui;

    public string CorrectAnswer => correctAnswer;
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public float TimeRemaining => timeRemaining;
    public bool IsReadyForAnswer => isActive && !isCompleted && (!requireAllListsInspectedBeforeAnswer || AllRequiredListsInspected());

    private void Awake()
    {
        ResolveReferences();
        ConfigureLists();
        SetPuzzleObjectsReadable(false);
    }

    private void Update()
    {
        if (!isActive || isCompleted || timerExpired)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.SetTime(timeRemaining);

        if (timeRemaining <= 0f)
        {
            ExpireTimer();
        }
    }

    public void ActivatePuzzle()
    {
        if (isActive || isCompleted)
        {
            return;
        }

        ui = PhonePuzzleUI.EnsureExists();
        isActive = true;
        timerExpired = false;
        timeRemaining = puzzleTimeLimit;
        SetPuzzleObjectsReadable(true);
        ui.Objectives.SetObjective("Find the erased patient.");
        ui.Timer.SetTime(timeRemaining);
        OnPuzzleActivated.Invoke();
        Debug.Log("Waiting Room Puzzle activated.");
    }

    public void ConfigureSceneReferences(ReadableWallList admission, ReadableWallList deaths, ReadableWallList rooms, GameObject terminal)
    {
        if (admission != null)
        {
            admissionList = admission;
        }

        if (deaths != null)
        {
            deathRecords = deaths;
        }

        if (rooms != null)
        {
            roomAssignments = rooms;
        }

        if (terminal != null)
        {
            patientInputTerminal = terminal;
        }

        ConfigureLists();

        if (!isActive)
        {
            SetPuzzleObjectsReadable(false);
        }
    }

    public void ConfigurePhoneAnchor(Transform phoneAnchor)
    {
        if (phoneAnchor != null)
        {
            wallPhoneAnchor = phoneAnchor;
        }
    }

    public void MarkCompleted()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;
        isActive = false;
        SetPuzzleObjectsReadable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();

        OnPuzzleCompleted.Invoke();
        PuzzleCompleted?.Invoke();
        Debug.Log("Waiting Room Puzzle completed.");
    }

    public void RegisterListInspected(ReadableWallList list)
    {
        if (!isActive || list == null)
        {
            return;
        }

        Debug.Log("Puzzle 1 list inspected: " + list.ListTitle);
    }

    public bool AllRequiredListsInspected()
    {
        return admissionList != null && admissionList.HasBeenInspected
            && deathRecords != null && deathRecords.HasBeenInspected
            && roomAssignments != null && roomAssignments.HasBeenInspected;
    }

    private void ExpireTimer()
    {
        timerExpired = true;
        isActive = false;
        timeRemaining = 0f;
        SetPuzzleObjectsReadable(false);

        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Timer.Hide();

        OnTimerExpired.Invoke();
        TimerExpired?.Invoke();
        Debug.Log("Waiting Room Puzzle timer expired.");
    }

    private void SetPuzzleObjectsReadable(bool value)
    {
        SetListReadable(admissionList, value);
        SetListReadable(deathRecords, value);
        SetListReadable(roomAssignments, value);

        if (patientInputTerminal != null)
        {
            patientInputTerminal.SetActive(value);
        }
    }

    private static void SetListReadable(ReadableWallList list, bool value)
    {
        if (list == null)
        {
            return;
        }

        if (value)
        {
            list.gameObject.SetActive(true);
            list.SetReadable(true);
            return;
        }

        list.SetReadable(false);
        list.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (admissionList == null)
        {
            admissionList = FindListByName("AdmissionList");
        }

        if (deathRecords == null)
        {
            deathRecords = FindListByName("DeathRecords");
        }

        if (roomAssignments == null)
        {
            roomAssignments = FindListByName("RoomAssignments");
        }

        if (patientInputTerminal == null)
        {
            GameObject terminal = GameObject.Find("PatientInputTerminal");
            if (terminal != null)
            {
                patientInputTerminal = terminal;
            }
        }
    }

    private void ConfigureLists()
    {
        admissionList?.Configure("ADMISSION LIST", AdmissionText, this);
        deathRecords?.Configure("DEATH RECORDS", DeathRecordsText, this);
        roomAssignments?.Configure("ROOM ASSIGNMENTS", RoomAssignmentsText, this);
    }

    private static ReadableWallList FindListByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform target = null;
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                target = transforms[i];
                break;
            }
        }

        if (target == null)
        {
            return null;
        }

        ReadableWallList list = target.GetComponent<ReadableWallList>();
        if (list == null)
        {
            list = target.gameObject.AddComponent<ReadableWallList>();
        }

        return list;
    }
}

using UnityEngine;

public class OperationInstrumentSequenceManager : MonoBehaviour
{
    [Header("Puzzle")]
    [SerializeField] private OperationOrderPuzzle puzzle;
    [SerializeField] private bool sequenceEnabled;

    [Header("Correct Order")]
    [SerializeField] private SurgicalInstrumentInteractable[] correctOrder = new SurgicalInstrumentInteractable[6];

    [Header("Registered Instruments")]
    [SerializeField] private SurgicalInstrumentInteractable[] instruments = new SurgicalInstrumentInteractable[6];

    private readonly OperationInstrumentId[] fallbackOrder =
    {
        OperationInstrumentId.Clamp,
        OperationInstrumentId.Scalpel,
        OperationInstrumentId.SurgicalScissors,
        OperationInstrumentId.RetractorHook,
        OperationInstrumentId.NeedleHolder,
        OperationInstrumentId.Stethoscope
    };

    private int currentStep;
    private int wrongAttemptCount;
    private bool sequenceCompleted;

    public bool SequenceEnabled => sequenceEnabled;
    public bool SequenceCompleted => sequenceCompleted;
    public int CurrentStep => currentStep;

    public void Configure(OperationOrderPuzzle owner, SurgicalInstrumentInteractable[] orderedInstruments)
    {
        puzzle = owner;

        if (orderedInstruments != null && orderedInstruments.Length > 0)
        {
            EnsureArraySizes();
            for (int i = 0; i < orderedInstruments.Length && i < correctOrder.Length; i++)
            {
                correctOrder[i] = orderedInstruments[i];
            }

            for (int i = 0; i < orderedInstruments.Length && i < instruments.Length; i++)
            {
                instruments[i] = orderedInstruments[i];
                if (instruments[i] != null)
                {
                    instruments[i].SetSequenceManager(this);
                }
            }
        }
    }

    public void RegisterInstrument(SurgicalInstrumentInteractable instrument)
    {
        if (instrument == null)
        {
            return;
        }

        EnsureArraySizes();
        for (int i = 0; i < instruments.Length; i++)
        {
            if (instruments[i] == instrument)
            {
                instrument.SetSequenceManager(this);
                return;
            }
        }

        for (int i = 0; i < instruments.Length; i++)
        {
            if (instruments[i] == null)
            {
                instruments[i] = instrument;
                instrument.SetSequenceManager(this);
                return;
            }
        }

        Debug.LogWarning("Puzzle 4 could not register instrument " + instrument.name + " because the instrument list is full.");
    }

    public void SetSequenceEnabled(bool value)
    {
        sequenceEnabled = value;
        Debug.Log("Puzzle 4 instrument sequence enabled: " + value + ".");
    }

    public void SetInstrumentsInteractable(bool value)
    {
        EnsureArraySizes();
        for (int i = 0; i < instruments.Length; i++)
        {
            if (instruments[i] != null)
            {
                instruments[i].SetInstrumentInteractable(value);
            }
        }
    }

    public void ResetSequence()
    {
        currentStep = 0;
        sequenceCompleted = false;
    }

    public void ResetWrongAttempts()
    {
        wrongAttemptCount = 0;
    }

    public void TryInspectInstrument(SurgicalInstrumentInteractable instrument)
    {
        if (instrument == null)
        {
            return;
        }

        if (puzzle == null)
        {
            puzzle = GetComponent<OperationOrderPuzzle>() ?? FindFirstObjectByType<OperationOrderPuzzle>(FindObjectsInactive.Include);
        }

        if (puzzle == null)
        {
            Debug.LogWarning("Puzzle 4 instrument " + instrument.name + " has no OperationOrderPuzzle owner.");
            return;
        }

        puzzle.RegisterInstrumentInspected(instrument);

        if (!sequenceEnabled)
        {
            puzzle.HandleInstrumentTouchedBeforeMethodSheet(instrument);
            return;
        }

        if (sequenceCompleted)
        {
            puzzle.HandleInstrumentTouchedAfterSequenceCompleted(instrument);
            return;
        }

        OperationInstrumentId expectedId = GetExpectedInstrumentId(currentStep);
        if (instrument.InstrumentId == expectedId)
        {
            int completedStep = currentStep + 1;
            puzzle.HandleCorrectInstrumentStep(instrument, completedStep);
            currentStep++;

            if (currentStep >= fallbackOrder.Length)
            {
                sequenceCompleted = true;
                puzzle.HandleCorrectSequenceCompleted();
            }

            return;
        }

        wrongAttemptCount++;
        currentStep = 0;
        puzzle.HandleWrongInstrumentStep(instrument, wrongAttemptCount);
    }

    private OperationInstrumentId GetExpectedInstrumentId(int index)
    {
        if (index < 0 || index >= fallbackOrder.Length)
        {
            return OperationInstrumentId.None;
        }

        EnsureArraySizes();
        if (correctOrder[index] != null && correctOrder[index].InstrumentId != OperationInstrumentId.None)
        {
            return correctOrder[index].InstrumentId;
        }

        return fallbackOrder[index];
    }

    private void EnsureArraySizes()
    {
        if (correctOrder == null || correctOrder.Length != fallbackOrder.Length)
        {
            SurgicalInstrumentInteractable[] resized = new SurgicalInstrumentInteractable[fallbackOrder.Length];
            if (correctOrder != null)
            {
                for (int i = 0; i < correctOrder.Length && i < resized.Length; i++)
                {
                    resized[i] = correctOrder[i];
                }
            }

            correctOrder = resized;
        }

        if (instruments == null || instruments.Length != fallbackOrder.Length)
        {
            SurgicalInstrumentInteractable[] resized = new SurgicalInstrumentInteractable[fallbackOrder.Length];
            if (instruments != null)
            {
                for (int i = 0; i < instruments.Length && i < resized.Length; i++)
                {
                    resized[i] = instruments[i];
                }
            }

            instruments = resized;
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class KeypadBoxCode : Interactable
{
    private enum DoorHingeSide
    {
        Left,
        Right
    }

    [Header("Code")]
    [SerializeField] private string correctCode = "21";
    [SerializeField, Min(1)] private int maxInputLength = 2;
    [SerializeField, Min(0.1f)] private float wrongClearDelay = 0.75f;

    [Header("Box Door")]
    [SerializeField] private Transform door;
    [SerializeField] private Transform doorOpenPivot;
    [SerializeField] private bool useDoorOwnPivot = true;
    [SerializeField] private float openDoorLocalY = 90f;
    [SerializeField] private bool autoCreateDoorPivot = true;
    [SerializeField] private DoorHingeSide hingeSide = DoorHingeSide.Left;
    [SerializeField] private string generatedPivotName = "Door_HingePivot";
    [SerializeField] private Vector3 openLocalEuler = new Vector3(0f, 90f, 0f);
    [SerializeField] private bool forceParentYAxisHinge = true;
    [SerializeField, Min(0.05f)] private float doorOpenDuration = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip boxKeypadPressSound;
    [SerializeField] private AudioClip boxUnlockSound;
    [SerializeField] private AudioClip boxOpenSound;
    [SerializeField] private AudioClip boxWrongCodeSound;

    [Header("Events")]
    public UnityEvent OnCorrectBoxCode = new UnityEvent();
    public UnityEvent OnWrongBoxCode = new UnityEvent();
    public UnityEvent OnBoxOpened = new UnityEvent();

    public event Action CorrectCodeEntered;
    public event Action<int> WrongCodeEntered;
    public event Action BoxOpened;

    private HeartRoomPuzzle puzzle;
    private PhoneAnswerInput answerInput;
    private PhonePuzzleUI ui;
    private Vector3 closedDoorLocalEuler;
    private Quaternion closedOpenPivotRotation;
    private Coroutine wrongClearRoutine;
    private Coroutine openDoorRoutine;
    private int wrongAttempts;
    private bool isInputOpen;
    private bool isOpen;
    private bool keypadSoundButtonsBound;

#if UNITY_EDITOR
    private const string DefaultBoxOpenSoundPath = "Assets/audio/opendoor.mp3";
#endif

    private void Awake()
    {
        TryAssignEditorDefaultAudio();
        ResolveReferences();
        SetKeypadInteractable(false);
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultAudio();
    }

    private void Update()
    {
        if (!isInputOpen)
        {
            return;
        }

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i)))
            {
                PlayOneShot(boxKeypadPressSound);
            }
        }
    }

    public void Configure(HeartRoomPuzzle owner, Transform boxDoor, AudioClip keypadPress, AudioClip unlock, AudioClip open, AudioClip wrong)
    {
        puzzle = owner;
        door ??= boxDoor;
        boxKeypadPressSound ??= keypadPress;
        boxUnlockSound ??= unlock;
        boxOpenSound ??= open;
        boxWrongCodeSound ??= wrong;
        TryAssignEditorDefaultAudio();
        ResolveReferences();
    }

    public void SetKeypadInteractable(bool value)
    {
        SetInteractable(value && !isOpen);
        SetPromptText("Press E to enter padlock code");

        if (!value && isInputOpen)
        {
            EndInput();
        }
    }

    public override string GetInteractPrompt()
    {
        return "Press E to enter padlock code";
    }

    public override void Interact()
    {
        if (!CanInteract || isOpen)
        {
            return;
        }

        BeginInput();
    }

    private void BeginInput()
    {
        ResolveReferences();
        ui = PhonePuzzleUI.EnsureExists();

        if (answerInput == null)
        {
            answerInput = GetComponent<PhoneAnswerInput>() ?? gameObject.AddComponent<PhoneAnswerInput>();
        }

        answerInput.BeginAnswerMode(maxInputLength, HandleCodeSubmitted, HandleInputCancelled);
        isInputOpen = true;
        BindKeypadSoundButtons();
        Debug.Log("Puzzle 2 keypad input opened.");
    }

    private void EndInput()
    {
        isInputOpen = false;
        answerInput?.EndAnswerMode();
    }

    private void HandleInputCancelled()
    {
        isInputOpen = false;
        Debug.Log("Puzzle 2 keypad input cancelled.");
    }

    private void HandleCodeSubmitted(string submittedCode)
    {
        if (isOpen)
        {
            return;
        }

        if (submittedCode == correctCode)
        {
            HandleCorrectCode();
            return;
        }

        HandleWrongCode();
    }

    private void HandleCorrectCode()
    {
        EndInput();
        PlayOneShot(boxUnlockSound);
        OnCorrectBoxCode.Invoke();
        CorrectCodeEntered?.Invoke();
        puzzle?.RegisterCorrectBoxCode();
        Debug.Log("Puzzle 2 box keypad correct code entered: " + correctCode);

        if (openDoorRoutine != null)
        {
            StopCoroutine(openDoorRoutine);
        }

        openDoorRoutine = StartCoroutine(OpenDoorRoutine());
    }

    private void HandleWrongCode()
    {
        wrongAttempts++;
        PuzzleFailureMonsterAlert.Alert(transform);
        PlayOneShot(boxWrongCodeSound);
        ui ??= PhonePuzzleUI.EnsureExists();
        ui.Subtitles.ShowTimed("Something heard the noise.", 2.5f);
        OnWrongBoxCode.Invoke();
        WrongCodeEntered?.Invoke(wrongAttempts);
        puzzle?.RegisterWrongBoxCode(wrongAttempts);
        Debug.Log("Puzzle 2 box keypad wrong code. Attempts: " + wrongAttempts);

        if (wrongClearRoutine != null)
        {
            StopCoroutine(wrongClearRoutine);
        }

        wrongClearRoutine = StartCoroutine(ClearWrongInputAfterDelay());
    }

    private IEnumerator ClearWrongInputAfterDelay()
    {
        yield return new WaitForSeconds(wrongClearDelay);
        answerInput?.ClearAnswer();
        wrongClearRoutine = null;
    }

    private IEnumerator OpenDoorRoutine()
    {
        if (door == null)
        {
            Debug.LogWarning("Puzzle 2 cannot open Lbox because Door is missing.");
            MarkBoxOpened();
            yield break;
        }

        PlayOneShot(boxOpenSound);

        if (useDoorOwnPivot)
        {
            yield return OpenDoorOwnPivotRoutine();
            openDoorRoutine = null;
            yield break;
        }

        Transform openTarget = GetDoorOpenTarget();
        if (openTarget == null)
        {
            Debug.LogWarning("Puzzle 2 cannot open Lbox because Door pivot is missing.");
            MarkBoxOpened();
            yield break;
        }

        Quaternion startRotation = openTarget.localRotation;
        Quaternion targetRotation = closedOpenPivotRotation * Quaternion.Euler(openLocalEuler);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            openTarget.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        openTarget.localRotation = targetRotation;
        MarkBoxOpened();
        openDoorRoutine = null;
    }

    private IEnumerator OpenDoorOwnPivotRoutine()
    {
        Quaternion startRotation = door.localRotation;
        Vector3 targetEuler = closedDoorLocalEuler;
        targetEuler.y = openDoorLocalY;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            door.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        door.localRotation = targetRotation;
        MarkBoxOpened();
    }

    private void MarkBoxOpened()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        SetInteractable(false);
        OnBoxOpened.Invoke();
        BoxOpened?.Invoke();
        puzzle?.RegisterBoxOpened();
        Debug.Log("Puzzle 2 Lbox opened.");
    }

    private void ResolveReferences()
    {
        if (door == null)
        {
            Transform parent = transform.parent;
            while (parent != null && door == null)
            {
                if (parent.name == "Door")
                {
                    door = parent;
                    break;
                }

                Transform candidate = parent.Find("Door");
                if (candidate != null)
                {
                    door = candidate;
                    break;
                }

                parent = parent.parent;
            }
        }

        if (door != null)
        {
            closedDoorLocalEuler = door.localEulerAngles;

            if (!useDoorOwnPivot)
            {
                EnsureDoorOpenPivot();
                Transform openTarget = GetDoorOpenTarget();
                if (openTarget != null)
                {
                    closedOpenPivotRotation = openTarget.localRotation;
                }
            }
        }
        else
        {
            Debug.LogWarning("Puzzle 2 keypad could not find Door.");
        }

        EnsureAudioSource();

        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.35f, 0.35f, 0.12f);
            box.center = Vector3.zero;
            Debug.LogWarning("keypad_Lbox had no Collider, so a fallback BoxCollider was added.");
        }
    }

    private Transform GetDoorOpenTarget()
    {
        return doorOpenPivot != null ? doorOpenPivot : door;
    }

    private void EnsureDoorOpenPivot()
    {
        if (door == null || !autoCreateDoorPivot)
        {
            return;
        }

        if (doorOpenPivot != null)
        {
            return;
        }

        if (door.parent != null && door.parent.name == generatedPivotName)
        {
            doorOpenPivot = door.parent;
            AlignPivotToParentYAxis(doorOpenPivot);
            return;
        }

        Transform originalParent = door.parent;
        Transform existingPivot = originalParent != null ? originalParent.Find(generatedPivotName) : null;
        if (existingPivot != null)
        {
            doorOpenPivot = existingPivot;
            AlignPivotToParentYAxis(doorOpenPivot);
            door.SetParent(doorOpenPivot, true);
            return;
        }

        GameObject pivotObject = new GameObject(generatedPivotName);
        doorOpenPivot = pivotObject.transform;
        doorOpenPivot.SetParent(originalParent, true);
        doorOpenPivot.position = ResolveHingeWorldPosition(originalParent);
        doorOpenPivot.rotation = ResolveClosedPivotRotation(originalParent);
        doorOpenPivot.localScale = Vector3.one;
        door.SetParent(doorOpenPivot, true);
        Debug.Log("Puzzle 2 created Door_HingePivot so Lbox opens around the hinge instead of the center.");
    }

    private void AlignPivotToParentYAxis(Transform pivot)
    {
        if (pivot == null || !forceParentYAxisHinge)
        {
            return;
        }

        Transform pivotParent = pivot.parent;
        Vector3 doorWorldPosition = door != null ? door.position : Vector3.zero;
        Quaternion doorWorldRotation = door != null ? door.rotation : Quaternion.identity;
        Vector3 doorWorldScale = door != null ? door.lossyScale : Vector3.one;

        Quaternion targetRotation = ResolveClosedPivotRotation(pivotParent);
        pivot.position = ResolveHingeWorldPosition(pivotParent);
        pivot.rotation = targetRotation;

        if (door != null)
        {
            door.position = doorWorldPosition;
            door.rotation = doorWorldRotation;
            RestoreApproximateWorldScale(door, doorWorldScale);
        }
    }

    private Quaternion ResolveClosedPivotRotation(Transform pivotParent)
    {
        if (!forceParentYAxisHinge)
        {
            return door != null ? door.rotation : Quaternion.identity;
        }

        return pivotParent != null ? pivotParent.rotation : Quaternion.identity;
    }

    private Vector3 ResolveHingeWorldPosition(Transform originalParent)
    {
        if (door == null)
        {
            return transform.position;
        }

        Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return door.position;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        Vector3 sideAxis = originalParent != null ? originalParent.right : door.right;
        Vector3 hingeDirection = hingeSide == DoorHingeSide.Left ? -sideAxis : sideAxis;
        float extent = ProjectBoundsExtent(bounds, hingeDirection);
        return bounds.center + hingeDirection.normalized * extent;
    }

    private static void RestoreApproximateWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null || target.parent == null)
        {
            return;
        }

        Vector3 parentScale = target.parent.lossyScale;
        target.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

    private static float ProjectBoundsExtent(Bounds bounds, Vector3 axis)
    {
        axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)).normalized;
        return bounds.extents.x * axis.x + bounds.extents.y * axis.y + bounds.extents.z * axis.z;
    }

    private void BindKeypadSoundButtons()
    {
        if (keypadSoundButtonsBound || ui == null)
        {
            return;
        }

        Button[] digitButtons = ui.DigitButtons;
        if (digitButtons != null)
        {
            for (int i = 0; i < digitButtons.Length && i < 10; i++)
            {
                if (digitButtons[i] != null)
                {
                    digitButtons[i].onClick.AddListener(() => PlayOneShot(boxKeypadPressSound));
                }
            }
        }

        keypadSoundButtonsBound = true;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.Log("Puzzle 2 keypad audio clip missing; continuing without audio.");
            return;
        }

        AudioSource source = EnsureAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private AudioSource EnsureAudioSource()
    {
        try
        {
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                return audioSource;
            }
        }
        catch (MissingComponentException)
        {
            audioSource = null;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        return audioSource;
    }

    private void TryAssignEditorDefaultAudio()
    {
#if UNITY_EDITOR
        if (boxOpenSound != null)
        {
            return;
        }

        boxOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultBoxOpenSoundPath);
        if (boxOpenSound != null)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }
}

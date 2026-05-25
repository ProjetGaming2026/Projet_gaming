using System.Collections;
using TMPro;
using UnityEngine;

public class TVCodePuzzleController : MonoBehaviour, IInteractable
{
    [Header("Code")]
    [SerializeField] private string correctCode = "1234";

    [Header("UI")]
    [SerializeField] private GameObject pressEText;
    [SerializeField] private GameObject codeCanvas;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private GameObject wrongCodeText;

    [Header("Puzzle")]
    [SerializeField] private TelevisionVideoBootstrap televisionVideo;
    [SerializeField] private GameObject notebookUnderBed;
    [SerializeField] private EntityHuntTrigger entityHuntTrigger;
    [SerializeField] private Transform televisionFocusTarget;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private BasicFirstPersonController playerController;
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour playerLookScript;
    [SerializeField] private bool pausePlayerWhileUIOpen = true;

    [Header("Wrong Code")]
    [SerializeField] private float wrongCodeCloseDelay = 0.5f;

    [Header("Interaction Focus")]
    [SerializeField] private float interactionDistance = 3.25f;
    [SerializeField] private float maxLookAngle = 28f;
    [SerializeField] private float frontDotThreshold = 0.25f;

    private bool codeValidated;
    private bool uiOpen;
    private bool videoFinishedListenerRegistered;
    private Coroutine wrongCodeRoutine;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    public static bool IsAnyCodeUiOpen { get; private set; }

    private void OnEnable()
    {
        RegisterVideoFinishedListener();
    }

    private void OnDisable()
    {
        UnregisterVideoFinishedListener();
        if (uiOpen)
        {
            IsAnyCodeUiOpen = false;
        }
    }

    private void Start()
    {
        ResolveMissingReferences();
        RegisterVideoFinishedListener();

        SetActiveIfAssigned(pressEText, false);
        SetActiveIfAssigned(codeCanvas, false);
        SetActiveIfAssigned(wrongCodeText, false);
        SetActiveIfAssigned(notebookUnderBed, false);
    }

    private void Update()
    {
        SetActiveIfAssigned(pressEText, false);

        if (uiOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                CheckCode();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCodeUI();
            }

            return;
        }

        if (CanInteractWithTelevision() && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (uiOpen || (televisionVideo != null && televisionVideo.IsPlaying))
        {
            return;
        }

        SetActiveIfAssigned(pressEText, false);

        if (codeValidated)
        {
            ReplayVideo();
        }
        else
        {
            OpenCodeUI();
        }
    }

    public string GetInteractPrompt()
    {
        return codeValidated ? "Appuie sur E pour relancer la video" : "Appuie sur E pour entrer le code";
    }

    public void Configure(
        string newCorrectCode,
        GameObject newPressEText,
        GameObject newCodeCanvas,
        TMP_InputField newCodeInput,
        GameObject newWrongCodeText,
        TelevisionVideoBootstrap newTelevisionVideo,
        GameObject newNotebookUnderBed,
        EntityHuntTrigger newEntityHuntTrigger,
        Transform newTelevisionFocusTarget,
        Transform newPlayer,
        BasicFirstPersonController newPlayerController)
    {
        if (!string.IsNullOrWhiteSpace(newCorrectCode))
        {
            correctCode = newCorrectCode;
        }

        pressEText = newPressEText;
        codeCanvas = newCodeCanvas;
        codeInput = newCodeInput;
        wrongCodeText = newWrongCodeText;
        televisionVideo = newTelevisionVideo;
        notebookUnderBed = newNotebookUnderBed;
        entityHuntTrigger = newEntityHuntTrigger;
        televisionFocusTarget = newTelevisionFocusTarget;
        player = newPlayer;
        playerController = newPlayerController;

        RegisterVideoFinishedListener();

        SetActiveIfAssigned(pressEText, false);
        SetActiveIfAssigned(codeCanvas, false);
        SetActiveIfAssigned(wrongCodeText, false);
        SetActiveIfAssigned(notebookUnderBed, false);
    }

    public void CheckCode()
    {
        if (codeInput == null)
        {
            Debug.LogWarning("TVCodePuzzleController: no code input assigned.");
            return;
        }

        if (codeInput.text.Trim() == correctCode)
        {
            HandleCorrectCode();
        }
        else
        {
            HandleWrongCode();
        }
    }

    public void CloseCodeUI()
    {
        uiOpen = false;
        IsAnyCodeUiOpen = false;
        SetActiveIfAssigned(codeCanvas, false);
        SetActiveIfAssigned(wrongCodeText, false);
        RestorePlayerControl();
        SetActiveIfAssigned(pressEText, false);
    }

    private void ResolveMissingReferences()
    {
        if (televisionVideo == null)
        {
            televisionVideo = FindFirstObjectByType<TelevisionVideoBootstrap>();
        }

        if (entityHuntTrigger == null)
        {
            entityHuntTrigger = FindFirstObjectByType<EntityHuntTrigger>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<BasicFirstPersonController>();
        }

        if (televisionFocusTarget == null)
        {
            GameObject screenObject = GameObject.Find("TV_Screen");
            if (screenObject != null)
            {
                televisionFocusTarget = screenObject.transform;
            }
        }
    }

    private void RegisterVideoFinishedListener()
    {
        if (televisionVideo == null || videoFinishedListenerRegistered)
        {
            return;
        }

        televisionVideo.OnVideoFinished.AddListener(ShowNotebookUnderBed);
        videoFinishedListenerRegistered = true;
    }

    private void UnregisterVideoFinishedListener()
    {
        if (televisionVideo == null || !videoFinishedListenerRegistered)
        {
            return;
        }

        televisionVideo.OnVideoFinished.RemoveListener(ShowNotebookUnderBed);
        videoFinishedListenerRegistered = false;
    }

    private bool CanInteractWithTelevision()
    {
        if (uiOpen || televisionFocusTarget == null || (televisionVideo != null && televisionVideo.IsPlaying))
        {
            return false;
        }

        Transform origin = ResolveInteractionOrigin();
        if (origin == null)
        {
            return false;
        }

        Vector3 toTarget = televisionFocusTarget.position - origin.position;
        float distance = toTarget.magnitude;
        if (distance > interactionDistance || distance <= 0.01f)
        {
            return false;
        }

        Vector3 direction = toTarget / distance;
        if (Vector3.Angle(origin.forward, direction) > maxLookAngle)
        {
            return false;
        }

        Vector3 screenNormal = ResolveTelevisionForward();
        Vector3 fromScreenToPlayer = (origin.position - televisionFocusTarget.position).normalized;
        if (Vector3.Dot(screenNormal, fromScreenToPlayer) < frontDotThreshold)
        {
            return false;
        }

        if (Physics.Raycast(origin.position, direction, out RaycastHit hit, distance + 0.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            return hit.collider.GetComponentInParent<TVCodePuzzleController>() == this
                || hit.transform == televisionFocusTarget
                || hit.transform.IsChildOf(televisionFocusTarget)
                || televisionFocusTarget.IsChildOf(hit.transform)
                || hit.transform.root == televisionFocusTarget.root
                || hit.distance >= distance - 0.8f;
        }

        return true;
    }

    private Vector3 ResolveTelevisionForward()
    {
        Renderer renderer = televisionFocusTarget.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = televisionFocusTarget.GetComponentInChildren<Renderer>(true);
        }

        MeshFilter meshFilter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return -televisionFocusTarget.forward;
        }

        Vector3 size = meshFilter.sharedMesh.bounds.size;
        int depthAxis = GetSmallestAxis(size);
        Vector3 normal = televisionFocusTarget.TransformDirection(AxisVector(depthAxis)).normalized;
        Transform origin = ResolveInteractionOrigin();
        if (origin != null && Vector3.Dot(normal, origin.position - televisionFocusTarget.position) < 0f)
        {
            normal = -normal;
        }

        return normal;
    }

    private static int GetSmallestAxis(Vector3 size)
    {
        if (size.x <= size.y && size.x <= size.z)
        {
            return 0;
        }

        return size.y <= size.z ? 1 : 2;
    }

    private static Vector3 AxisVector(int axis)
    {
        if (axis == 0)
        {
            return Vector3.right;
        }

        return axis == 1 ? Vector3.up : Vector3.forward;
    }

    private Transform ResolveInteractionOrigin()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        return player != null ? player : transform;
    }

    private void OpenCodeUI()
    {
        uiOpen = true;
        IsAnyCodeUiOpen = true;
        SetActiveIfAssigned(pressEText, false);
        SetActiveIfAssigned(codeCanvas, true);
        SetActiveIfAssigned(wrongCodeText, false);

        if (codeInput != null)
        {
            codeInput.text = string.Empty;
            codeInput.ActivateInputField();
        }

        ApplyUiPlayerControl();
    }

    private void HandleCorrectCode()
    {
        codeValidated = true;
        SetActiveIfAssigned(pressEText, false);
        CloseCodeUI();
        ReplayVideo();
    }

    private void ReplayVideo()
    {
        if (televisionVideo != null)
        {
            televisionVideo.PlayTVVideo();
        }
        else
        {
            Debug.LogWarning("TVCodePuzzleController: no TelevisionVideoBootstrap assigned.");
        }
    }

    private void HandleWrongCode()
    {
        SetActiveIfAssigned(wrongCodeText, true);

        if (entityHuntTrigger != null)
        {
            Vector3 huntPosition = player != null ? player.position : transform.position;
            entityHuntTrigger.TriggerHunt(huntPosition);
        }

        if (codeInput != null)
        {
            codeInput.text = string.Empty;
        }

        if (wrongCodeRoutine != null)
        {
            StopCoroutine(wrongCodeRoutine);
        }

        wrongCodeRoutine = StartCoroutine(CloseAfterWrongCodeDelay());
    }

    private IEnumerator CloseAfterWrongCodeDelay()
    {
        yield return new WaitForSeconds(wrongCodeCloseDelay);
        CloseCodeUI();
        wrongCodeRoutine = null;
    }

    private void ShowNotebookUnderBed()
    {
        SetActiveIfAssigned(notebookUnderBed, true);
        Debug.Log("Notebook appeared under the bed.");
    }

    private void ApplyUiPlayerControl()
    {
        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!pausePlayerWhileUIOpen)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.SetExternalControlLock(true);
            return;
        }

        SetScriptEnabled(playerMovementScript, false);
        SetScriptEnabled(playerLookScript, false);
    }

    private void RestorePlayerControl()
    {
        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        if (!pausePlayerWhileUIOpen)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.SetExternalControlLock(false);
            return;
        }

        SetScriptEnabled(playerMovementScript, true);
        SetScriptEnabled(playerLookScript, true);
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static void SetScriptEnabled(MonoBehaviour script, bool enabled)
    {
        if (script != null)
        {
            script.enabled = enabled;
        }
    }
}

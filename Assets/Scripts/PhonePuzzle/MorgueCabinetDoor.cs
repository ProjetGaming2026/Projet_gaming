using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MorgueCabinetDoor : Interactable
{
#if UNITY_EDITOR
    private const string DefaultCabinetOpenCloseClipPath = "Assets/audio/cabinet-open-close.mp3";
#endif

    [Header("Cabinet")]
    [SerializeField] private int cabinetNumber;
    [SerializeField] private float openAngleY = -130f;
    [SerializeField, Min(0f)] private float openDuration = 1f;
    [SerializeField, Min(0f)] private float closeDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioSource audioSource;

    [Header("State")]
    [SerializeField] private bool isOpen;
    [SerializeField] private bool isAnimating;
    [SerializeField] private bool interactionEnabled;

    [Header("Events")]
    public UnityEvent OnCabinetOpened = new UnityEvent();
    public UnityEvent OnCabinetClosed = new UnityEvent();

    private Quaternion initialLocalRotation;
    private bool initialRotationCaptured;
    private bool wholeCabinetPrepared;
    private Puzzle5MorgueController puzzleController;
    private Coroutine activeRoutine;

    public int CabinetNumber => cabinetNumber;
    public bool IsOpen => isOpen;
    public bool IsAnimating => isAnimating;
    public bool InteractionEnabled => interactionEnabled;
    public override bool CanInteract => interactionEnabled;

    private void Awake()
    {
        TryAssignEditorDefaultCabinetAudio();
        PrepareWholeCabinetForAnimation();
        EnsureInteractionCollider();
        RefreshPromptText();
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultCabinetAudio();
    }

    private void Start()
    {
        CaptureInitialRotation();
    }

    public void Configure(int number, Puzzle5MorgueController controller, AudioClip defaultOpenSound, AudioClip defaultCloseSound)
    {
        TryAssignEditorDefaultCabinetAudio();
        cabinetNumber = number;
        puzzleController = controller != null ? controller : puzzleController;
        PrepareWholeCabinetForAnimation();

        if (openSound == null)
        {
            openSound = defaultOpenSound;
        }

        if (closeSound == null)
        {
            closeSound = defaultCloseSound;
        }

        EnsureInteractionCollider();
        RefreshPromptText();
    }

    private void PrepareWholeCabinetForAnimation()
    {
        ClearStaticFlagsForAnimatedCabinet();

        if (wholeCabinetPrepared)
        {
            return;
        }

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        int preparedMeshCount = 0;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Mesh dynamicMesh = Instantiate(meshFilter.sharedMesh);
            dynamicMesh.name = meshFilter.sharedMesh.name + "_Cabinet_" + cabinetNumber + "_Dynamic";
            meshFilter.sharedMesh = dynamicMesh;
            preparedMeshCount++;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].staticShadowCaster = false;
            }
        }

        wholeCabinetPrepared = true;
        Debug.Log("Cabinet " + cabinetNumber + " prepared as one animated object with " + preparedMeshCount + " dynamic mesh(es).");
    }

    public void SetPuzzleController(Puzzle5MorgueController controller)
    {
        puzzleController = controller;
    }

    public void SetInteractionEnabled(bool value)
    {
        interactionEnabled = value;
        SetInteractable(value);
        RefreshPromptText();
    }

    public void SetDefaultAudioClips(AudioClip defaultOpenSound, AudioClip defaultCloseSound)
    {
        if (openSound == null)
        {
            openSound = defaultOpenSound;
        }

        if (closeSound == null)
        {
            closeSound = defaultCloseSound;
        }
    }

    public override string GetInteractPrompt()
    {
        if (!interactionEnabled)
        {
            return string.Empty;
        }

        return isOpen
            ? "Press E to close Cabinet " + cabinetNumber
            : "Press E to open Cabinet " + cabinetNumber;
    }

    public override void Interact()
    {
        if (!interactionEnabled)
        {
            return;
        }

        if (isAnimating)
        {
            Debug.Log("Cabinet " + cabinetNumber + " ignored because it is animating.");
            return;
        }

        OnInteracted.Invoke();

        if (isOpen)
        {
            CloseCabinet();
            return;
        }

        OpenCabinet();
    }

    public void OpenCabinet()
    {
        if (!interactionEnabled || isAnimating || isOpen)
        {
            return;
        }

        StartDoorRoutine(true);
    }

    public void CloseCabinet()
    {
        if (!interactionEnabled || isAnimating || !isOpen)
        {
            return;
        }

        StartDoorRoutine(false);
    }

    public void EnsureInteractionCollider()
    {
        Collider[] existingColliders = GetComponentsInChildren<Collider>(true);
        if (existingColliders != null && existingColliders.Length > 0)
        {
            for (int i = 0; i < existingColliders.Length; i++)
            {
                Collider existing = existingColliders[i];
                if (existing == null)
                {
                    continue;
                }

                existing.isTrigger = true;
            }

            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        Bounds visualBounds;
        if (TryGetVisualBounds(out visualBounds))
        {
            Bounds localBounds = BuildLocalBoundsFromWorldBounds(transform, visualBounds);
            box.center = localBounds.center;
            box.size = new Vector3(
                Mathf.Max(0.1f, localBounds.size.x),
                Mathf.Max(0.1f, localBounds.size.y),
                Mathf.Max(0.1f, localBounds.size.z));
        }
        else
        {
            box.center = Vector3.zero;
            box.size = new Vector3(0.75f, 0.75f, 0.75f);
        }

        Debug.Log("Puzzle 5 added BoxCollider to Cabinet " + cabinetNumber + ".");
    }

    private void ClearStaticFlagsForAnimatedCabinet()
    {
        ClearStaticFlags(transform);
    }

    private static void ClearStaticFlags(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                transforms[i].gameObject.isStatic = false;
            }
        }
    }

    private void StartDoorRoutine(bool opening)
    {
        CaptureInitialRotation();

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(RotateDoorRoutine(opening));
    }

    private IEnumerator RotateDoorRoutine(bool opening)
    {
        isAnimating = true;
        PrepareWholeCabinetForAnimation();

        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = opening
            ? initialLocalRotation * Quaternion.Euler(0f, openAngleY, 0f)
            : initialLocalRotation;

        PlayOneShot(opening ? openSound : closeSound);

        float duration = opening ? openDuration : closeDuration;
        if (duration <= 0f)
        {
            transform.localRotation = targetRotation;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.localRotation = targetRotation;
        }

        isOpen = opening;
        isAnimating = false;
        activeRoutine = null;
        RefreshPromptText();

        if (opening)
        {
            NotifyCabinetOpened();
            OnCabinetOpened.Invoke();
            Debug.Log("Cabinet " + cabinetNumber + " opened.");
        }
        else
        {
            OnCabinetClosed.Invoke();
            Debug.Log("Cabinet " + cabinetNumber + " closed.");
        }
    }

    private void NotifyCabinetOpened()
    {
        if (puzzleController == null)
        {
            puzzleController = FindFirstObjectByType<Puzzle5MorgueController>(FindObjectsInactive.Include);
        }

        if (puzzleController != null)
        {
            puzzleController.RegisterCabinetOpened(this);
        }
    }

    private void CaptureInitialRotation()
    {
        if (initialRotationCaptured)
        {
            return;
        }

        initialLocalRotation = transform.localRotation;
        initialRotationCaptured = true;
    }

    private void RefreshPromptText()
    {
        SetPromptText(GetInteractPrompt());
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.PlayOneShot(clip);
    }

    private void TryAssignEditorDefaultCabinetAudio()
    {
#if UNITY_EDITOR
        bool changed = false;
        AudioClip defaultClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultCabinetOpenCloseClipPath);
        if (defaultClip == null)
        {
            return;
        }

        if (openSound == null)
        {
            openSound = defaultClip;
            changed = true;
        }

        if (closeSound == null)
        {
            closeSound = defaultClip;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static Bounds BuildLocalBoundsFromWorldBounds(Transform root, Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        Bounds localBounds = new Bounds(root.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
        {
            localBounds.Encapsulate(root.InverseTransformPoint(corners[i]));
        }

        return localBounds;
    }
}

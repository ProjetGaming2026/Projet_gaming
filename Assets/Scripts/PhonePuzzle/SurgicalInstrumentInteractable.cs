using UnityEngine;

public enum OperationInstrumentId
{
    None,
    Clamp,
    Scalpel,
    SurgicalScissors,
    RetractorHook,
    NeedleHolder,
    Stethoscope
}

public class SurgicalInstrumentInteractable : Interactable
{
    private const string InteractionTriggerName = "Puzzle4_Instrument_Interaction_Trigger";

    [Header("Instrument")]
    [SerializeField] private OperationInstrumentId instrumentId = OperationInstrumentId.None;
    [SerializeField] private string displayName = "Instrument";
    [SerializeField] private int instrumentNumber;
    [SerializeField, TextArea(2, 4)] private string inspectionSubtitle;
    [SerializeField] private Vector3 interactionTriggerPadding = new Vector3(0.18f, 0.18f, 0.18f);
    [SerializeField] private Vector3 minimumInteractionTriggerSize = new Vector3(0.32f, 0.24f, 0.32f);
    [SerializeField, Min(0.35f)] private float maxInspectDistance = 2.15f;

    [Header("Puzzle")]
    [SerializeField] private OperationInstrumentSequenceManager sequenceManager;
    [SerializeField] private InstrumentNumberHint numberHint;

    [Header("Audio")]
    [SerializeField] private AudioClip voiceOrSound;

    public OperationInstrumentId InstrumentId => instrumentId;
    public string DisplayName => displayName;
    public int InstrumentNumber => instrumentNumber;
    public string InspectionSubtitle => inspectionSubtitle;
    public AudioClip VoiceOrSound => voiceOrSound;
    public InstrumentNumberHint NumberHint => numberHint;

    private void Awake()
    {
        EnsureCollider();
        EnsureNumberHint();
        RefreshPromptText();
    }

    public void Configure(
        OperationInstrumentId id,
        string instrumentDisplayName,
        int number,
        string subtitle,
        OperationInstrumentSequenceManager manager,
        AudioClip clip)
    {
        instrumentId = id;
        displayName = string.IsNullOrWhiteSpace(instrumentDisplayName) ? gameObject.name : instrumentDisplayName;
        instrumentNumber = number;
        inspectionSubtitle = subtitle;
        sequenceManager = manager;
        voiceOrSound = clip != null ? clip : voiceOrSound;

        EnsureCollider();
        EnsureNumberHint();

        if (numberHint != null)
        {
            numberHint.Configure(instrumentNumber);
        }

        RefreshPromptText();
    }

    public void SetSequenceManager(OperationInstrumentSequenceManager manager)
    {
        sequenceManager = manager;
    }

    public void SetInstrumentInteractable(bool value)
    {
        SetInteractable(value);
    }

    public override string GetInteractPrompt()
    {
        return BuildInspectPrompt();
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        OnInteracted.Invoke();

        if (sequenceManager == null)
        {
            sequenceManager = FindFirstObjectByType<OperationInstrumentSequenceManager>(FindObjectsInactive.Include);
        }

        if (sequenceManager == null)
        {
            Debug.LogWarning(name + " was inspected, but no OperationInstrumentSequenceManager was found.");
            return;
        }

        sequenceManager.TryInspectInstrument(this);
    }

    public void RevealNumber(float seconds)
    {
        EnsureNumberHint();
        if (numberHint != null)
        {
            numberHint.Configure(instrumentNumber);
            numberHint.Reveal(seconds);
        }
    }

    public bool CanInspectFrom(Vector3 worldPosition)
    {
        Bounds visualBounds;
        if (TryGetVisualBounds(out visualBounds))
        {
            return visualBounds.SqrDistance(worldPosition) <= maxInspectDistance * maxInspectDistance;
        }

        return Vector3.Distance(worldPosition, transform.position) <= maxInspectDistance;
    }

    public float GetInteractionScore(Ray viewRay, RaycastHit hit)
    {
        Bounds visualBounds;
        if (!TryGetVisualBounds(out visualBounds))
        {
            visualBounds = hit.collider != null ? hit.collider.bounds : new Bounds(transform.position, Vector3.one * 0.2f);
        }

        Vector3 closestPointOnRay = ClosestPointOnRay(viewRay, visualBounds.center);
        float rayToVisualBounds = Mathf.Sqrt(visualBounds.SqrDistance(closestPointOnRay));
        return rayToVisualBounds * 100f + hit.distance;
    }

    private void EnsureNumberHint()
    {
        if (numberHint == null)
        {
            numberHint = GetComponentInChildren<InstrumentNumberHint>(true);
        }

        if (numberHint == null)
        {
            GameObject hintObject = new GameObject("InstrumentNumberHint");
            hintObject.transform.SetParent(transform, false);
            hintObject.transform.localPosition = ResolveHintLocalPosition();
            numberHint = hintObject.AddComponent<InstrumentNumberHint>();
        }

        numberHint.Configure(instrumentNumber);
    }

    private void RefreshPromptText()
    {
        SetPromptText(BuildInspectPrompt());
    }

    private string BuildInspectPrompt()
    {
        string targetName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        return "Appuie sur E pour inspecter " + targetName;
    }

    private Vector3 ResolveHintLocalPosition()
    {
        Bounds visualBounds;
        if (!TryGetVisualBounds(out visualBounds))
        {
            return new Vector3(0f, 0.16f, 0f);
        }

        Vector3 worldTop = visualBounds.center + Vector3.up * Mathf.Max(0.08f, visualBounds.extents.y + 0.05f);
        return transform.InverseTransformPoint(worldTop);
    }

    private Renderer FindBestRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Renderer bestRenderer = null;
        float bestVolume = -1f;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null || candidate.GetComponent<InstrumentNumberHint>() != null)
            {
                continue;
            }

            Bounds bounds = candidate.bounds;
            float volume = bounds.size.x * bounds.size.y * bounds.size.z;
            if (volume > bestVolume)
            {
                bestVolume = volume;
                bestRenderer = candidate;
            }
        }

        return bestRenderer;
    }

    private void EnsureCollider()
    {
        Collider[] existingColliders = GetComponentsInChildren<Collider>(true);
        if (existingColliders != null && existingColliders.Length > 0)
        {
            for (int i = 0; i < existingColliders.Length; i++)
            {
                Collider existing = existingColliders[i];
                if (existing == null || existing.name == InteractionTriggerName)
                {
                    continue;
                }

                if (existing is MeshCollider meshCollider && !meshCollider.convex)
                {
                    meshCollider.enabled = false;
                    continue;
                }

                existing.isTrigger = true;
            }
        }

        BoxCollider box = EnsureInteractionTriggerCollider();
        box.isTrigger = true;
        Bounds visualBounds;
        if (TryGetVisualBounds(out visualBounds))
        {
            Bounds localBounds = BuildLocalBoundsFromWorldBounds(transform, visualBounds);
            box.center = localBounds.center;
            box.size = new Vector3(
                Mathf.Max(minimumInteractionTriggerSize.x, localBounds.size.x + interactionTriggerPadding.x),
                Mathf.Max(minimumInteractionTriggerSize.y, localBounds.size.y + interactionTriggerPadding.y),
                Mathf.Max(minimumInteractionTriggerSize.z, localBounds.size.z + interactionTriggerPadding.z));
        }
        else
        {
            box.size = minimumInteractionTriggerSize;
            box.center = Vector3.zero;
        }
    }

    private BoxCollider EnsureInteractionTriggerCollider()
    {
        Transform existing = transform.Find(InteractionTriggerName);
        GameObject triggerObject;
        if (existing != null)
        {
            triggerObject = existing.gameObject;
        }
        else
        {
            triggerObject = new GameObject(InteractionTriggerName);
            triggerObject.transform.SetParent(transform, false);
            triggerObject.transform.localPosition = Vector3.zero;
            triggerObject.transform.localRotation = Quaternion.identity;
            triggerObject.transform.localScale = Vector3.one;
            Debug.Log("Puzzle 4 added a stable interaction trigger to " + name + ".");
        }

        BoxCollider box = triggerObject.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = triggerObject.AddComponent<BoxCollider>();
        }

        return box;
    }

    private bool TryGetVisualBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null ||
                candidate.GetComponentInParent<InstrumentNumberHint>() != null ||
                candidate.transform.name == InteractionTriggerName)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = candidate.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(candidate.bounds);
            }
        }

        return hasBounds;
    }

    private static Vector3 ClosestPointOnRay(Ray ray, Vector3 point)
    {
        float distance = Vector3.Dot(point - ray.origin, ray.direction);
        return ray.origin + ray.direction * Mathf.Max(0f, distance);
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

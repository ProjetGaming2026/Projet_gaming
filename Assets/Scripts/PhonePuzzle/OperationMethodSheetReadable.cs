using UnityEngine;

public class OperationMethodSheetReadable : Interactable
{
    private const string SheetTitle = "OPERATION METHOD";
    private const string InteractionTriggerName = "Puzzle4_MethodSheet_Interaction_Trigger";
    private const string SheetSurfaceName = "OperationMethodSheet_Surface";
    private const string SheetBackSurfaceName = "OperationMethodSheet_BackSurface";
    private const string MissingImageBody =
        "The paper is here, but the image is not assigned.\n\n" +
        "Assign Assets/Phone_Puzzles/ordre_operation.png to the Operation Method Texture field.";

    [Header("Sheet")]
    [SerializeField] private bool hideUntilPuzzleActive = true;
    [SerializeField] private Texture operationMethodTexture;
    [SerializeField] private Sprite operationMethodSprite;
    [SerializeField] private Renderer sheetRenderer;
    [SerializeField] private Renderer backSheetRenderer;
    [SerializeField] private Vector2 sheetSize = new Vector2(0.55f, 0.78f);
    [SerializeField] private Color fallbackPaperColor = new Color(0.72f, 0.66f, 0.48f, 1f);

    [Header("Puzzle")]
    [SerializeField] private OperationOrderPuzzle puzzle;
    [SerializeField] private bool isReadable;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool hasBeenRead;

    public bool HasBeenRead => hasBeenRead;
    public Texture OperationMethodTexture => ResolveTexture();

    private void Awake()
    {
        EnsurePhysicalSheet();
        ApplyTextureToRenderer();
        SetPromptText("Appuie sur E pour lire la feuille");
        SetReadable(isReadable);

        if (hideUntilPuzzleActive)
        {
            SetSheetVisible(false);
        }
    }

    public void Configure(OperationOrderPuzzle owner, Texture texture, Sprite sprite)
    {
        puzzle = owner;
        operationMethodTexture ??= texture;
        operationMethodSprite ??= sprite;
        EnsurePhysicalSheet();
        ApplyTextureToRenderer();
        SetPromptText("Appuie sur E pour lire la feuille");
    }

    public void SetReadable(bool readable)
    {
        isReadable = readable;
        SetInteractable(readable);
    }

    public void SetSheetVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            if (visible && collider is MeshCollider meshCollider && !meshCollider.convex)
            {
                collider.enabled = false;
                continue;
            }

            if (visible && collider.transform.name == SheetBackSurfaceName)
            {
                collider.enabled = false;
                continue;
            }

            collider.enabled = visible;
        }
    }

    public override bool CanInteract => isReadable && base.CanInteract;

    public override string GetInteractPrompt()
    {
        return "Appuie sur E pour lire la feuille";
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        OnInteracted.Invoke();
        hasBeenRead = true;

        Texture texture = ResolveTexture();
        PhonePuzzleUI ui = PhonePuzzleUI.EnsureExists();
        if (texture != null)
        {
            ui.ShowReadableImage(SheetTitle, texture, MissingImageBody);
        }
        else
        {
            ui.ShowReadableText(SheetTitle, MissingImageBody);
            Debug.LogWarning("Puzzle 4 OperationMethodSheet was read, but ordre_operation image is missing.");
        }

        puzzle?.RegisterMethodSheetRead(this);
        Debug.Log("Puzzle 4 operation method sheet read.");
    }

    private void EnsurePhysicalSheet()
    {
        if (sheetRenderer == null)
        {
            sheetRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (sheetRenderer == null)
        {
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = SheetSurfaceName;
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = Vector3.zero;
            surface.transform.localRotation = Quaternion.identity;
            surface.transform.localScale = new Vector3(sheetSize.x, sheetSize.y, 1f);
            sheetRenderer = surface.GetComponent<Renderer>();
            Debug.Log("Puzzle 4 created a physical OperationMethodSheet surface.");
        }

        EnsureBackSurface();

        Collider[] existingColliders = GetComponentsInChildren<Collider>(true);
        bool hasUsableInteractionCollider = false;
        if (existingColliders != null && existingColliders.Length > 0)
        {
            for (int i = 0; i < existingColliders.Length; i++)
            {
                Collider collider = existingColliders[i];
                if (collider == null)
                {
                    continue;
                }

                if (collider.name == InteractionTriggerName)
                {
                    hasUsableInteractionCollider = true;
                    continue;
                }

                if (collider is MeshCollider meshCollider && !meshCollider.convex)
                {
                    meshCollider.enabled = false;
                    continue;
                }

                collider.isTrigger = true;
                hasUsableInteractionCollider = true;
            }
        }

        BoxCollider sheetTrigger = EnsureInteractionTriggerCollider();
        sheetTrigger.isTrigger = true;
        sheetTrigger.size = new Vector3(Mathf.Max(0.75f, sheetSize.x + 0.28f), 0.12f, Mathf.Max(0.95f, sheetSize.y + 0.28f));
        sheetTrigger.center = Vector3.zero;
        sheetTrigger.enabled = true;

        if (!hasUsableInteractionCollider)
        {
            Debug.LogWarning("OperationMethodSheet had no Collider, so Puzzle 4 added a fallback BoxCollider.");
        }

        EnsureAudioSource();
    }

    private void EnsureBackSurface()
    {
        if (sheetRenderer == null)
        {
            return;
        }

        if (backSheetRenderer == null)
        {
            Transform existing = transform.Find(SheetBackSurfaceName);
            if (existing != null)
            {
                backSheetRenderer = existing.GetComponent<Renderer>();
            }
        }

        if (backSheetRenderer == null)
        {
            GameObject backSurface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backSurface.name = SheetBackSurfaceName;
            backSurface.transform.SetParent(transform, false);
            backSurface.transform.localPosition = Vector3.zero;
            backSurface.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backSurface.transform.localScale = new Vector3(sheetSize.x, sheetSize.y, 1f);
            backSheetRenderer = backSurface.GetComponent<Renderer>();

            Collider backCollider = backSurface.GetComponent<Collider>();
            if (backCollider != null)
            {
                backCollider.enabled = false;
            }

            Debug.Log("Puzzle 4 created a back face for OperationMethodSheet visibility.");
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
            Debug.Log("Puzzle 4 added a stable interaction trigger to OperationMethodSheet.");
        }

        BoxCollider box = triggerObject.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = triggerObject.AddComponent<BoxCollider>();
        }

        return box;
    }

    private void ApplyTextureToRenderer()
    {
        if (sheetRenderer == null)
        {
            return;
        }

        Texture texture = ResolveTexture();
        Material material = sheetRenderer.material;
        if (material == null)
        {
            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            material = new Material(shader);
            sheetRenderer.material = material;
        }

        if (texture != null)
        {
            material.mainTexture = texture;
            material.color = Color.white;
        }
        else
        {
            material.color = fallbackPaperColor;
        }

        MakeMaterialDoubleSided(material);

        if (backSheetRenderer != null)
        {
            backSheetRenderer.material = material;
        }
    }

    private static void MakeMaterialDoubleSided(Material material)
    {
        if (material == null)
        {
            return;
        }

        material.doubleSidedGI = true;

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetFloat("_CullMode", 0f);
        }
    }

    private Texture ResolveTexture()
    {
        if (operationMethodTexture != null)
        {
            return operationMethodTexture;
        }

        if (operationMethodSprite != null)
        {
            return operationMethodSprite.texture;
        }

        return null;
    }

    private AudioSource EnsureAudioSource()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            return audioSource;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        return audioSource;
    }
}

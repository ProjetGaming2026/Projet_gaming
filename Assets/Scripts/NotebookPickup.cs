using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotebookPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Appuie sur E pour ramasser le carnet";
    [SerializeField] private KeyCode reopenKey = KeyCode.N;
    [SerializeField] private string pickupHint = "N pour ouvrir / fermer le carnet";
    [SerializeField] private float pickupHintDuration = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private float soundVolume = 0.8f;

    private bool pickedUp;
    private bool reading;
    private GameObject heldNotebookView;
    private BasicFirstPersonController playerController;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    public static bool IsAnyNotebookOpen { get; private set; }

    private void Start()
    {
        EnsurePickupCollider();
    }

    private void Update()
    {
        if (pickedUp && !reading && Input.GetKeyDown(reopenKey))
        {
            OpenHeldNotebookView();
            return;
        }

        if (reading && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(reopenKey)))
        {
            CloseHeldNotebookView();
        }
    }

    public void Interact()
    {
        if (!pickedUp)
        {
            pickedUp = true;
            HideWorldNotebook();
            PrototypeHUD.ShowFeedback(pickupHint, PrototypeHUD.FeedbackTone.Info, pickupHintDuration);
            Debug.Log("NotebookPickup: notebook picked up.");
        }

        OpenHeldNotebookView();
    }

    public string GetInteractPrompt()
    {
        return prompt;
    }

    private void OpenHeldNotebookView()
    {
        Transform cameraTransform = ResolveCameraTransform();
        if (cameraTransform == null)
        {
            Debug.LogWarning("NotebookPickup: no camera found for held notebook view.");
            return;
        }

        EnsureHeldNotebookView(cameraTransform);
        heldNotebookView.SetActive(true);
        reading = true;
        IsAnyNotebookOpen = true;
        PlaySound(openSound, RuntimeInteractionSounds.NotebookOpen);

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (playerController != null)
        {
            playerController.SetExternalControlLock(true);
        }
    }

    private void CloseHeldNotebookView()
    {
        reading = false;
        IsAnyNotebookOpen = false;
        if (heldNotebookView != null)
        {
            heldNotebookView.SetActive(false);
        }

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        if (playerController != null)
        {
            playerController.SetExternalControlLock(false);
        }

        PlaySound(closeSound, RuntimeInteractionSounds.NotebookClose);
    }

    private void HideWorldNotebook()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer notebookRenderer in renderers)
        {
            notebookRenderer.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider notebookCollider in colliders)
        {
            notebookCollider.enabled = false;
        }
    }

    private void EnsureHeldNotebookView(Transform cameraTransform)
    {
        if (heldNotebookView != null)
        {
            heldNotebookView.transform.SetParent(cameraTransform, false);
            PositionHeldNotebook(heldNotebookView.transform);
            return;
        }

        heldNotebookView = new GameObject("Held_Notebook_View", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        heldNotebookView.transform.SetParent(cameraTransform, false);
        PositionHeldNotebook(heldNotebookView.transform);

        Canvas canvas = heldNotebookView.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 220;
        canvas.worldCamera = Camera.main;

        RectTransform canvasRect = heldNotebookView.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(900f, 650f);

        CanvasScaler scaler = heldNotebookView.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2.5f;

        GameObject shadow = new GameObject("Paper_Shadow", typeof(RectTransform), typeof(Image));
        shadow.transform.SetParent(heldNotebookView.transform, false);
        RectTransform shadowRect = shadow.GetComponent<RectTransform>();
        SetRect(shadowRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, -20f), new Vector2(730f, 500f));
        shadow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.34f);

        GameObject page = new GameObject("Notebook_Page", typeof(RectTransform), typeof(Image));
        page.transform.SetParent(heldNotebookView.transform, false);
        RectTransform pageRect = page.GetComponent<RectTransform>();
        SetRect(pageRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 500f));
        page.GetComponent<Image>().color = new Color(0.78f, 0.73f, 0.58f, 1f);

        GameObject fold = new GameObject("Center_Fold", typeof(RectTransform), typeof(Image));
        fold.transform.SetParent(page.transform, false);
        RectTransform foldRect = fold.GetComponent<RectTransform>();
        SetRect(foldRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 0f));
        fold.GetComponent<Image>().color = new Color(0.42f, 0.32f, 0.22f, 0.28f);

        TMP_Text title = CreateText(page.transform, "Title", "CARNET DU PATIENT", 38f, TextAlignmentOptions.Center, new Color(0.11f, 0.07f, 0.04f, 1f));
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(640f, 52f));

        TMP_Text body = CreateText(
            page.transform,
            "Body",
            "Ils ont pris mon nom.\n\nSans mon nom, pas de dossier.\nSans dossier, pas de code.\nSans code, pas de sortie.\n\nMais les murs n’oublient pas......",
            26f,
            TextAlignmentOptions.TopLeft,
            new Color(0.13f, 0.08f, 0.05f, 1f));
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(580f, 330f));

        TMP_Text hint = CreateText(page.transform, "Hint", "E / Esc fermer   |   N revoir", 20f, TextAlignmentOptions.Center, new Color(0.2f, 0.12f, 0.07f, 1f));
        SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(560f, 36f));

        heldNotebookView.SetActive(false);
    }

    private static void PositionHeldNotebook(Transform notebookTransform)
    {
        notebookTransform.localPosition = new Vector3(0.02f, -0.08f, 0.58f);
        notebookTransform.localRotation = Quaternion.Euler(7f, -2.5f, 0f);
        notebookTransform.localScale = Vector3.one * 0.00105f;
    }

    private void PlaySound(AudioClip assignedClip, AudioClip fallbackClip)
    {
        EnsureAudioSource();
        AudioClip clip = assignedClip != null ? assignedClip : fallbackClip;
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private static Transform ResolveCameraTransform()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        Camera camera = FindFirstObjectByType<Camera>();
        return camera != null ? camera.transform : null;
    }

    private void EnsurePickupCollider()
    {
        if (GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        Bounds bounds = ResolveRendererBounds();
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false;

        if (bounds.size != Vector3.zero)
        {
            boxCollider.center = transform.InverseTransformPoint(bounds.center);
            boxCollider.size = new Vector3(
                Mathf.Max(bounds.size.x, 0.35f),
                Mathf.Max(bounds.size.y, 0.08f),
                Mathf.Max(bounds.size.z, 0.25f));
        }
        else
        {
            boxCollider.size = new Vector3(0.45f, 0.08f, 0.3f);
        }
    }

    private Bounds ResolveRendererBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = color;
        return textComponent;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

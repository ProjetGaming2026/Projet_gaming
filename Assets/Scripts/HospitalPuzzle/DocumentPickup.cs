using UnityEngine;

public class DocumentPickup : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public float interactDistance = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    public float lookAngle = 65f;

    [Header("Document Text")]
    [TextArea(10, 20)]
    public string documentText =
        "PATIENT REPORT - Room 304\n" +
        "Dr. Elara Voss — Deceased, October 14, 1987\n\n" +
        "Body found incomplete.\n\n" +
        "Missing organs, in order of removal :\n\n" +
        "   1. The Heart — taken first, while still beating.\n" +
        "   2. The Hand — her right hand, the one that wrote.\n" +
        "   3. The Liver — removed last, in the dark.\n\n" +
        "Find my organs so I can rest in peace.";

    [Header("Blood Texture")]
    public Texture2D bloodTexture; // glisse ton image de sang ici dans l'Inspector
    public Texture2D paperTexture;

    [Header("Prompt")]
    public string promptText = "Press E to read";

    [Header("First Read Sound")]
    public AudioClip helpMeSound;
    [Range(0f, 1f)] public float helpMeVolume = 1f;

    private bool playerNearby = false;
    private bool isOpen = false;
    private Transform playerTransform;
    private Transform playerCamera;
    private Texture2D resolvedBloodTexture;
    private Texture2D resolvedPaperTexture;
    private int openedFrame = -1;
    private AudioSource audioSource;
    private bool helpMePlayed;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        Camera camera = Camera.main;
        if (camera != null)
            playerCamera = camera.transform;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        resolvedBloodTexture = bloodTexture != null ? bloodTexture : FindTextureByName("blood", "sang", "stain", "tache");
        resolvedPaperTexture = paperTexture != null ? paperTexture : FindTextureByName("paper", "papier", "vintage");
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        playerNearby = distance <= interactDistance;

        if (isOpen && Time.frameCount != openedFrame && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseDocument();
            return;
        }

        if (!isOpen && Input.GetKeyDown(interactKey) && CanOpenFromProximity())
        {
            OpenDocument();
        }
    }

    public void Interact()
    {
        if (OrganPickup.CurrentlyHeld != null)
            return;

        if (isOpen)
            CloseDocument();
        else
            OpenDocument();
    }

    void OpenDocument()
    {
        isOpen = true;
        openedFrame = Time.frameCount;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
        PlayFirstReadSound();
    }

    public void CloseDocument()
    {
        isOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    void OnGUI()
    {
        // Afficher le prompt "Press E" quand le joueur est proche
        if (!isOpen && CanOpenFromProximity())
        {
            GUIStyle promptStyle = new GUIStyle(GUI.skin.label);
            promptStyle.fontSize = 18;
            promptStyle.normal.textColor = Color.white;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            float pw = 200;
            float ph = 30;
            GUI.Label(new Rect(Screen.width / 2 - pw / 2, Screen.height * 0.72f, pw, ph), promptText, promptStyle);
        }

        if (!isOpen) return;

        // Fond noir semi-transparent
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        // Fond papier beige
        float panelW = 620;
        float panelH = 500;
        float panelX = (Screen.width - panelW) / 2;
        float panelY = (Screen.height - panelH) / 2;

        Rect panelRect = new Rect(panelX, panelY, panelW, panelH);
        GUI.color = new Color(0.96f, 0.90f, 0.78f, 1f);
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);

        if (resolvedPaperTexture != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(panelRect, resolvedPaperTexture, ScaleMode.StretchToFill, true);
        }

        // Tache de sang en haut à droite du document
        if (resolvedBloodTexture != null)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.78f);
            float bloodW = panelW * 0.96f;
            float bloodH = panelH * 0.88f;
            GUI.DrawTexture(
                new Rect(panelX + panelW * 0.02f, panelY + panelH * 0.04f, bloodW, bloodH),
                resolvedBloodTexture,
                ScaleMode.StretchToFill,
                true
            );
        }

        // Texte du document par dessus
        GUI.color = Color.white;
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 17;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = new Color(0.12f, 0.08f, 0.04f);
        textStyle.padding = new RectOffset(40, 40, 30, 30);
        GUI.Label(new Rect(panelX, panelY, panelW, panelH - 60), documentText, textStyle);

        // Bouton fermer
        GUI.color = Color.white;
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 15;
        float btnW = 120;
        float btnH = 36;
        if (GUI.Button(new Rect(panelX + panelW / 2 - btnW / 2, panelY + panelH - 50, btnW, btnH), "Close", btnStyle))
        {
            CloseDocument();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }

    public string GetInteractPrompt() => isOpen ? "Appuie sur E pour fermer la feuille" : promptText;

    private void PlayFirstReadSound()
    {
        if (helpMePlayed || helpMeSound == null || audioSource == null)
            return;

        helpMePlayed = true;
        audioSource.PlayOneShot(helpMeSound, helpMeVolume);
    }

    private bool CanOpenFromProximity()
    {
        return playerNearby
            && OrganPickup.CurrentlyHeld == null
            && OrganPickup.LastInteractionFrame != Time.frameCount
            && IsPlayerLookingAtDocument();
    }

    private bool IsPlayerLookingAtDocument()
    {
        if (playerCamera == null)
        {
            Camera camera = Camera.main;
            if (camera != null)
                playerCamera = camera.transform;
        }

        if (playerCamera == null)
            return true;

        Vector3 toDocument = transform.position - playerCamera.position;
        if (toDocument.sqrMagnitude <= 0.001f)
            return true;

        float angle = Vector3.Angle(playerCamera.forward, toDocument);
        return angle <= lookAngle;
    }

    private Texture2D FindTextureByName(params string[] nameParts)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null || material.mainTexture == null)
                    continue;

                string textureName = material.mainTexture.name.ToLowerInvariant();
                for (int k = 0; k < nameParts.Length; k++)
                {
                    if (textureName.Contains(nameParts[k]))
                        return material.mainTexture as Texture2D;
                }
            }
        }

        return null;
    }
}

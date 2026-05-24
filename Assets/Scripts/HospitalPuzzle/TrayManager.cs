using UnityEngine;
using System.Collections;

public class TrayManager : MonoBehaviour
{
    [Header("Puzzle Setup")]
    public int totalSlots = 3;
    public bool countAsTask = true;

    [Header("Ending UI")]
    public GameObject endingPanel;
    public float messageDelay = 1.5f;
    public float finalOrganViewDelay = 2f;
    public float typingSpeed = 0.025f;

    [Header("Placement Feedback")]
    public float placementFeedbackDuration = 2.2f;

    [Header("Tray Visuals")]
    public bool applyMedicalTrayLook = true;
    public Color trayMetalColor = new Color(0.95f, 0.98f, 1f, 1f);
    [Range(0f, 1f)] public float trayMetallic = 0.15f;
    [Range(0f, 1f)] public float traySmoothness = 0.98f;
    public float trayHighlightIntensity = 1.35f;
    public float trayHighlightRange = 2.2f;

    [Header("Ending Sound & Light")]
    public AudioClip endingAmbientSound;
    public AudioClip whisperSound;
    public Light[] sceneLights;
    public float dimDuration = 3f;

    [Header("Optional: Door to unlock")]
    public GameObject doorToOpen;
    public float doorOpenAngle = 90f;

    [Header("Ending Message")]
    [TextArea(5, 15)]
    public string finalMessage =
        "Thank you...\n\n" +
        "For finding what was lost.\n" +
        "For giving me back what was taken.\n\n" +
        "My heart... my hands... my soul.\n\n" +
        "I can finally rest.\n\n" +
        "- Dr. Elara Voss\n" +
        "  Room 304, 1987";

    private int filledSlotsCount = 0;
    private bool puzzleComplete = false;
    private AudioSource audioSource;
    private string displayedText = "";
    private bool showEnding = false;
    private Coroutine endingRoutine;
    private string placementFeedbackText = "";
    private float placementFeedbackUntil;
    private bool taskRegistered;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (applyMedicalTrayLook)
            ApplyMedicalTrayLook();
    }

    void Update()
    {
        if (!showEnding)
            return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
            CloseEndingPanel();
    }

    public void OnSlotFilled(int slotNumber)
    {
        filledSlotsCount++;
        Debug.Log("[TrayManager] Slots filled: " + filledSlotsCount + "/" + totalSlots);

        if (filledSlotsCount >= totalSlots && !puzzleComplete)
        {
            puzzleComplete = true;
            RegisterTaskProgress();
            endingRoutine = StartCoroutine(TriggerEnding());
        }
    }

    public bool CanFillSlot(int slotNumber)
    {
        return slotNumber == GetNextExpectedSlotNumber();
    }

    public int GetNextExpectedSlotNumber()
    {
        return Mathf.Clamp(filledSlotsCount + 1, 1, totalSlots);
    }

    public void ShowPlacementFeedback(string message)
    {
        placementFeedbackText = message;
        placementFeedbackUntil = Time.time + placementFeedbackDuration;
    }

    IEnumerator TriggerEnding()
    {
        yield return new WaitForSeconds(Mathf.Max(messageDelay, finalOrganViewDelay));

        if (sceneLights != null && sceneLights.Length > 0)
            StartCoroutine(DimLights());

        if (endingAmbientSound != null)
            audioSource.PlayOneShot(endingAmbientSound);

        yield return new WaitForSeconds(1.5f);

        if (whisperSound != null)
            audioSource.PlayOneShot(whisperSound);

        if (endingPanel != null)
            endingPanel.SetActive(true);

        showEnding = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        displayedText = "";
        foreach (string token in SplitMessageIntoWordTokens(finalMessage))
        {
            if (!showEnding)
                yield break;

            displayedText += token;
            if (!string.IsNullOrWhiteSpace(token))
                yield return new WaitForSecondsRealtime(Mathf.Min(typingSpeed, 0.03f));
        }

        if (doorToOpen != null)
            StartCoroutine(OpenDoor());
    }

    void OnGUI()
    {
        DrawPlacementFeedback();

        if (!showEnding) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
        {
            CloseEndingPanel();
            return;
        }

        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        GUI.Label(
            new Rect(Screen.width * 0.2f, Screen.height * 0.2f, Screen.width * 0.6f, Screen.height * 0.6f),
            displayedText,
            style
        );

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = 16;
        if (GUI.Button(new Rect(Screen.width / 2 - 60, Screen.height * 0.82f, 120, 40), "Continue", btnStyle))
        {
            CloseEndingPanel();
        }
    }

    private void DrawPlacementFeedback()
    {
        if (string.IsNullOrEmpty(placementFeedbackText) || Time.time > placementFeedbackUntil)
            return;

        GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label);
        feedbackStyle.fontSize = 20;
        feedbackStyle.fontStyle = FontStyle.Bold;
        feedbackStyle.alignment = TextAnchor.MiddleCenter;
        feedbackStyle.normal.textColor = new Color(1f, 0.25f, 0.2f);

        GUI.Label(
            new Rect(Screen.width / 2f - 300f, Screen.height * 0.58f, 600f, 36f),
            placementFeedbackText,
            feedbackStyle
        );
    }

    IEnumerator DimLights()
    {
        float elapsed = 0f;
        float[] startIntensities = new float[sceneLights.Length];

        for (int i = 0; i < sceneLights.Length; i++)
            if (sceneLights[i] != null)
                startIntensities[i] = sceneLights[i].intensity;

        while (elapsed < dimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dimDuration;
            for (int i = 0; i < sceneLights.Length; i++)
                if (sceneLights[i] != null)
                    sceneLights[i].intensity = Mathf.Lerp(startIntensities[i], 0.05f, t);
            yield return null;
        }
    }

    IEnumerator OpenDoor()
    {
        Quaternion startRot = doorToOpen.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, doorOpenAngle, 0);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 0.5f;
            doorToOpen.transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }
    }

    public void CloseEndingPanel()
    {
        showEnding = false;
        displayedText = "";
        if (endingRoutine != null)
        {
            StopCoroutine(endingRoutine);
            endingRoutine = null;
        }

        if (endingPanel != null)
            endingPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    private void RegisterTaskProgress()
    {
        if (!countAsTask || taskRegistered)
            return;

        PrototypeGameManager gameManager = FindFirstObjectByType<PrototypeGameManager>();
        if (gameManager != null)
        {
            gameManager.RegisterTaskSuccess();
            taskRegistered = true;
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPuzzleSolved();
            taskRegistered = true;
        }
    }

    private void ApplyMedicalTrayLook()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer trayRenderer = renderers[i];
            if (trayRenderer == null || !trayRenderer.name.ToLowerInvariant().Contains("tray"))
                continue;

            Material material = trayRenderer.material;
            if (material == null)
                continue;

            material.color = trayMetalColor;
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", trayMetalColor);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", trayMetallic);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", traySmoothness);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", traySmoothness);

            if (material.HasProperty("_GlossyReflections"))
                material.SetFloat("_GlossyReflections", 1f);

            AddTrayHighlight(trayRenderer);
        }
    }

    private void AddTrayHighlight(Renderer trayRenderer)
    {
        GameObject lightObject = new GameObject(trayRenderer.name + "_MedicalHighlight");
        lightObject.transform.SetParent(trayRenderer.transform, false);
        lightObject.transform.localPosition = Vector3.up * 0.35f;

        Light highlight = lightObject.AddComponent<Light>();
        highlight.type = LightType.Point;
        highlight.color = new Color(0.78f, 0.9f, 1f);
        highlight.intensity = trayHighlightIntensity;
        highlight.range = trayHighlightRange;
        highlight.shadows = LightShadows.None;
    }

    private static System.Collections.Generic.IEnumerable<string> SplitMessageIntoWordTokens(string message)
    {
        if (string.IsNullOrEmpty(message))
            yield break;

        int index = 0;
        while (index < message.Length)
        {
            int start = index;
            bool whitespace = char.IsWhiteSpace(message[index]);
            while (index < message.Length && char.IsWhiteSpace(message[index]) == whitespace)
                index++;

            yield return message.Substring(start, index - start);
        }
    }
}

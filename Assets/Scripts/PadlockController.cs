using System.Collections;
using TMPro;
using UnityEngine;

public class PadlockController : MonoBehaviour, IInteractable
{
    [Header("Code")]
    [SerializeField] private string secretCode = "1234";
    [SerializeField] private int codeLength = 4;

    [Header("References")]
    [SerializeField] private GameObject screenObject;
    [SerializeField] private DoorController linkedDoor;

    [Header("Screen Text")]
    [SerializeField] private string screenChildName = "Red_Screen";
    [SerializeField] private bool useRedScreenTransform = true;
    [SerializeField] private Vector3 displayLocalPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 displayLocalEulerOffset = Vector3.zero;
    [SerializeField] private Vector2 displayWorldSize = new Vector2(0.46f, 0.18f);
    [SerializeField] private float displayDepth = 0.018f;
    [SerializeField] private float textForwardOffset = 0.012f;
    [SerializeField] private bool hideOriginalScreenMesh = true;
    [SerializeField] private Color displayBackgroundColor = new Color(0.02f, 0.65f, 0.18f, 1f);
    [SerializeField] private Color idleTextColor = new Color(0.02f, 0.04f, 0.025f, 1f);
    [SerializeField] private Color grantedTextColor = new Color(0.28f, 1f, 0.42f, 1f);
    [SerializeField] private Color deniedTextColor = new Color(1f, 0.05f, 0.03f, 1f);
    [SerializeField] private float statusFontSize = 1.45f;
    [SerializeField] private float codeFontSize = 2.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip keyPressSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip successSound;

    private const string ClearKey = "CLEAR";
    private const string BackKey = "BACK";
    private const string EnterKey = "ENTER";

    private string currentInput = string.Empty;
    private bool isActive;
    private bool isSolved;
    private bool isResetting;
    private AudioSource audioSource;
    private Coroutine resetRoutine;
    private Renderer screenRenderer;
    private TMP_Text statusText;
    private TMP_Text codeText;
    private Transform displayRoot;
    private Transform displayPlate;

    public bool IsActive()
    {
        return isActive;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupScreen();
        SetupButtons();
    }

    public void Interact()
    {
        if (isSolved)
        {
            Debug.Log("PadlockController: padlock is already solved.");
            return;
        }

        isActive = !isActive;
        Debug.Log(isActive
            ? "PadlockController: padlock activated. Click the physical buttons."
            : "PadlockController: padlock deactivated.");

        if (isActive)
        {
            UpdateDisplay(Color.white);
        }
    }

    public string GetInteractPrompt()
    {
        if (isSolved)
        {
            return "Unlocked";
        }

        return isActive ? "Click the keypad buttons" : "Press E to use padlock";
    }

    public void OnKeyPressed(string keyValue)
    {
        if (!isActive || isSolved || isResetting)
        {
            return;
        }

        PlaySound(keyPressSound);

        switch (keyValue)
        {
            case BackKey:
                RemoveLastDigit();
                break;

            case ClearKey:
                ClearInput();
                break;

            case EnterKey:
                ValidateCode();
                return;

            default:
                AddDigit(keyValue);
                break;
        }

        UpdateDisplay(Color.white);
    }

    public bool TryHandleKeyboardInput()
    {
        if (!isActive || isSolved || isResetting)
        {
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            OnKeyPressed(BackKey);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            OnKeyPressed(ClearKey);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnKeyPressed(EnterKey);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isActive = false;
            Debug.Log("PadlockController: padlock deactivated with Escape.");
            return true;
        }

        return false;
    }

    private void SetupScreen()
    {
        if (screenObject == null)
        {
            Transform screenTransform = transform.Find(screenChildName);
            if (screenTransform != null)
            {
                screenObject = screenTransform.gameObject;
            }
        }

        if (screenObject == null)
        {
            Debug.LogError("PadlockController: Red_Screen child was not found. Assign screenObject in the Inspector.");
            return;
        }

        screenRenderer = screenObject.GetComponent<Renderer>();
        if (screenRenderer == null)
        {
            screenRenderer = screenObject.GetComponentInChildren<Renderer>();
        }

        CreateOrRefreshPhysicalDisplay();

        if (hideOriginalScreenMesh && screenRenderer != null)
        {
            screenRenderer.enabled = false;
        }

        UpdateDisplay(idleTextColor);
        Debug.Log("PadlockController: Red_Screen replaced with integrated LCD display on " + screenObject.name + ".");
    }

    private void CreateOrRefreshPhysicalDisplay()
    {
        Transform existingDisplay = screenObject.transform.Find("PadlockDigitalDisplay");
        if (existingDisplay != null)
        {
            Destroy(existingDisplay.gameObject);
        }

        GameObject rootObject = GameObject.Find("PadlockDigitalDisplay_Runtime");
        if (rootObject == null)
        {
            rootObject = new GameObject("PadlockDigitalDisplay_Runtime");
        }

        rootObject.transform.SetParent(transform, false);
        if (useRedScreenTransform && screenObject != null)
        {
            rootObject.transform.localPosition = screenObject.transform.localPosition + displayLocalPositionOffset;
            rootObject.transform.localRotation = screenObject.transform.localRotation * Quaternion.Euler(displayLocalEulerOffset);
        }
        else
        {
            rootObject.transform.localPosition = displayLocalPositionOffset;
            rootObject.transform.localRotation = Quaternion.Euler(displayLocalEulerOffset);
        }
        rootObject.transform.localScale = Vector3.one;
        displayRoot = rootObject.transform;

        GameObject plateObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plateObject.name = "GreenBacklitDisplay";
        plateObject.transform.SetParent(displayRoot, false);
        plateObject.transform.localPosition = Vector3.zero;
        plateObject.transform.localRotation = Quaternion.identity;
        plateObject.transform.localScale = new Vector3(displayWorldSize.x, displayWorldSize.y, displayDepth);
        displayPlate = plateObject.transform;

        Renderer plateRenderer = plateObject.GetComponent<Renderer>();
        Material plateMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (plateMaterial.shader == null)
        {
            plateMaterial = new Material(Shader.Find("Standard"));
        }

        plateMaterial.color = displayBackgroundColor;
        if (plateMaterial.HasProperty("_EmissionColor"))
        {
            plateMaterial.EnableKeyword("_EMISSION");
            plateMaterial.SetColor("_EmissionColor", displayBackgroundColor * 0.45f);
        }

        plateRenderer.material = plateMaterial;

        statusText = CreateWorldText("StatusText", new Vector3(0f, 0.018f, -0.012f), statusFontSize);
        codeText = CreateWorldText("CodeText", new Vector3(0f, -0.038f, -0.012f), codeFontSize);

        Debug.Log("PadlockController: created physical green display on Main_Padlocker. Adjust Display Local Position/Euler if needed.");
    }

    private TMP_Text CreateWorldText(string objectName, Vector3 localPosition, float fontSize)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(displayRoot, false);
        textObject.transform.localPosition = localPosition + Vector3.back * textForwardOffset;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one * 0.035f;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(12f, 2f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = idleTextColor;
        text.alpha = 1f;
        text.enableCulling = false;

        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingOrder = 60;
        }

        return text;
    }

    private void SetupButtons()
    {
        int buttonCount = 0;

        foreach (Transform child in transform)
        {
            string keyValue = ResolveKeyValue(child.name);
            if (string.IsNullOrEmpty(keyValue))
            {
                continue;
            }

            Collider buttonCollider = child.GetComponent<Collider>();
            if (buttonCollider == null)
            {
                buttonCollider = child.gameObject.AddComponent<BoxCollider>();
                Debug.Log("PadlockController: added BoxCollider to button " + child.name + ".");
            }

            PadlockButton button = child.GetComponent<PadlockButton>();
            if (button == null)
            {
                button = child.gameObject.AddComponent<PadlockButton>();
            }

            button.Init(this, keyValue);
            buttonCount++;
            Debug.Log("PadlockController: configured button " + child.name + " as " + keyValue + ".");
        }

        Debug.Log("PadlockController: setup complete with " + buttonCount + " keypad buttons.");
    }

    private string ResolveKeyValue(string childName)
    {
        if (childName.StartsWith("Number "))
        {
            string digit = childName.Substring("Number ".Length).Trim();
            if (digit.Length == 1 && digit[0] >= '0' && digit[0] <= '9')
            {
                return digit;
            }
        }

        if (childName == "#")
        {
            return ClearKey;
        }

        if (childName == "Back")
        {
            return BackKey;
        }

        if (childName == "Enter")
        {
            return EnterKey;
        }

        return null;
    }

    private void AddDigit(string keyValue)
    {
        if (currentInput.Length >= codeLength)
        {
            return;
        }

        if (keyValue.Length == 1 && keyValue[0] >= '0' && keyValue[0] <= '9')
        {
            currentInput += keyValue;
        }
    }

    private void RemoveLastDigit()
    {
        if (currentInput.Length == 0)
        {
            return;
        }

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
    }

    private void ClearInput()
    {
        currentInput = string.Empty;
    }

    private void ValidateCode()
    {
        if (currentInput == secretCode)
        {
            isSolved = true;
            isActive = false;
            UpdateScreenText("ACCESS GRANTED", BuildEnteredCodeDisplay(false), grantedTextColor);
            PlaySound(successSound);
            Debug.Log("PadlockController: correct code entered. Opening linked door.");

            if (linkedDoor != null)
            {
                linkedDoor.OpenDoor();
            }
            else
            {
                Debug.LogWarning("PadlockController: no linkedDoor assigned.");
            }

            return;
        }

        UpdateScreenText("ACCESS DENIED", "INVALID CODE", deniedTextColor);
        PlaySound(errorSound);
        Debug.Log("PadlockController: wrong code entered.");

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
        }

        resetRoutine = StartCoroutine(ResetAfterWrongCode());
    }

    private IEnumerator ResetAfterWrongCode()
    {
        isResetting = true;
        yield return new WaitForSeconds(1.25f);
        currentInput = string.Empty;
        isResetting = false;
        UpdateDisplay(Color.white);
        resetRoutine = null;
    }

    private void UpdateDisplay(Color color)
    {
        UpdateScreenText(
            isActive ? "ENTER ACCESS CODE" : "SYSTEM LOCKED",
            BuildEnteredCodeDisplay(true),
            color == Color.white ? idleTextColor : color
        );
    }

    private void UpdateScreenText(string status, string code, Color color)
    {
        if (statusText == null || codeText == null)
        {
            return;
        }

        statusText.text = status;
        codeText.text = code;
        statusText.color = color;
        codeText.color = color;
        statusText.ForceMeshUpdate();
        codeText.ForceMeshUpdate();
    }

    private string BuildEnteredCodeDisplay(bool maskDigits)
    {
        string display = string.Empty;

        for (int i = 0; i < codeLength; i++)
        {
            if (i < currentInput.Length)
            {
                display += maskDigits ? "*" : currentInput[i].ToString();
            }
            else
            {
                display += "_";
            }

            if (i < codeLength - 1)
            {
                display += " ";
            }
        }

        return display;
    }

    private Vector3 GetCompensatedLocalScale(Transform parent, float targetWorldScale)
    {
        if (parent == null)
        {
            return Vector3.one * targetWorldScale;
        }

        Vector3 scale = parent.lossyScale;
        return new Vector3(
            SafeDivide(targetWorldScale, scale.x),
            SafeDivide(targetWorldScale, scale.y),
            SafeDivide(targetWorldScale, scale.z)
        );
    }

    private float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}

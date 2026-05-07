using UnityEngine;

public class CipherTerminal : MonoBehaviour, IInteractable
{
    public static bool IsAnyTerminalOpen { get; private set; }

    [Header("Display (3D TextMesh on computer screen)")]
    [SerializeField] private TextMesh screenText;

    [Header("Fallback Placement")]
    [SerializeField] private Vector3 screenLocalPosition = new Vector3(-0.01f, 0.41f, -0.31f);
    [SerializeField] private Vector3 screenLocalEulerAngles = new Vector3(90f, 0f, 0f);
    [SerializeField] private int screenFontSize = 64;
    [SerializeField] private float screenCharacterSize = 0.0032f;
    [SerializeField] private Color screenColor = Color.green;
    [SerializeField] private float maxScreenWidth = 0.22f;
    [SerializeField] private int maxVisibleCharactersPerLine = 18;

    [Header("Puzzle Reference")]
    [SerializeField] private CipherPuzzle cipherPuzzle;

    [Header("Messages")]
    [SerializeField] private string promptMessage = "ENTREZ LA PHRASE CHIFFREE:";
    [SerializeField] private string successMessage = "ACCES ACCORDE...";
    [SerializeField] private string wrongMessage = "CODE INCORRECT.\nREESSAYEZ.";

    private string currentInput = "";
    private bool isOpen;
    private bool isSolved;

    private void Awake()
    {
        EnsureScreenText();
        EnsurePuzzleReference();

        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.25f, 0f);
            collider.size = new Vector3(0.8f, 0.8f, 0.8f);
        }
    }

    private void Start()
    {
        if (screenText != null)
        {
            screenText.text = "[ APPUYEZ SUR E ]";
        }
    }

    public void Interact()
    {
        EnsurePuzzleReference();

        if (isSolved)
        {
            return;
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            currentInput = "";
            IsAnyTerminalOpen = true;
            UpdateDisplay();
        }
        else if (screenText != null)
        {
            IsAnyTerminalOpen = false;
            screenText.text = "[ APPUYEZ SUR E ]";
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Interact();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            CheckAnswer();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            }

            UpdateDisplay();
            return;
        }

        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c) || c == ' ')
            {
                currentInput += char.ToUpperInvariant(c);
                UpdateDisplay();
            }
        }
    }

    private void CheckAnswer()
    {
        EnsurePuzzleReference();

        if (cipherPuzzle == null)
        {
            Debug.LogWarning("CipherTerminal: aucun CipherPuzzle assigne.");
            return;
        }

        if (currentInput.Trim() == cipherPuzzle.ExpectedAnswer.ToUpperInvariant())
        {
            isSolved = true;
            isOpen = false;
            IsAnyTerminalOpen = false;

            if (screenText != null)
            {
                screenText.text = successMessage;
            }

            cipherPuzzle.OnPuzzleSolved();
        }
        else
        {
            if (screenText != null)
            {
                screenText.text = wrongMessage;
            }

            currentInput = "";
            Invoke(nameof(ResetDisplay), 1.5f);
        }
    }

    private void UpdateDisplay()
    {
        if (screenText != null)
        {
            screenText.text = FormatForScreen(promptMessage) + "\n> " + FormatForScreen(currentInput + "_");
        }
    }

    private void ResetDisplay()
    {
        UpdateDisplay();
    }

    private void Reset()
    {
        EnsureScreenText();
    }

    private void OnValidate()
    {
        EnsureScreenText();
    }

    private void EnsureScreenText()
    {
        if (screenText == null)
        {
            Transform child = transform.Find("ScreenText");
            if (child == null)
            {
                GameObject screenObject = new GameObject("ScreenText");
                child = screenObject.transform;
                child.SetParent(transform, false);
            }

            screenText = child.GetComponent<TextMesh>();
            if (screenText == null)
            {
                screenText = child.gameObject.AddComponent<TextMesh>();
            }
        }

        Transform screenTransform = screenText.transform;
        screenTransform.SetParent(transform, false);
        screenTransform.localScale = Vector3.one;

        screenTransform.localPosition = screenLocalPosition;
        screenTransform.localRotation = Quaternion.Euler(screenLocalEulerAngles);
        screenText.characterSize = screenCharacterSize;

        screenText.anchor = TextAnchor.MiddleCenter;
        screenText.alignment = TextAlignment.Center;
        screenText.fontSize = screenFontSize;
        screenText.color = screenColor;
        screenText.offsetZ = 0f;

        WorldTextOcclusion occlusion = screenText.GetComponent<WorldTextOcclusion>();
        if (occlusion == null)
        {
            occlusion = screenText.gameObject.AddComponent<WorldTextOcclusion>();
        }

        if (string.IsNullOrWhiteSpace(screenText.text))
        {
            screenText.text = "[ APPUYEZ SUR E ]";
        }

        ClampScreenTextWidth();
    }

    public void SetCipherPuzzle(CipherPuzzle puzzleReference)
    {
        cipherPuzzle = puzzleReference;
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            IsAnyTerminalOpen = false;
        }
    }

    private void EnsurePuzzleReference()
    {
        if (cipherPuzzle == null)
        {
            cipherPuzzle = FindFirstObjectByType<CipherPuzzle>();
        }
    }

    private string FormatForScreen(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        string[] words = value.Split(' ');
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int currentLineLength = 0;

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            int extraLength = currentLineLength == 0 ? word.Length : word.Length + 1;

            if (currentLineLength > 0 && currentLineLength + extraLength > maxVisibleCharactersPerLine)
            {
                builder.Append('\n');
                builder.Append(word);
                currentLineLength = word.Length;
            }
            else
            {
                if (currentLineLength > 0)
                {
                    builder.Append(' ');
                    currentLineLength++;
                }

                builder.Append(word);
                currentLineLength += word.Length;
            }
        }

        return builder.ToString();
    }

    private void ClampScreenTextWidth()
    {
        if (screenText == null)
        {
            return;
        }

        Renderer renderer = screenText.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Bounds bounds = renderer.bounds;
        if (bounds.size.x <= maxScreenWidth)
        {
            return;
        }

        float shrinkRatio = maxScreenWidth / bounds.size.x;
        screenText.characterSize *= Mathf.Clamp(shrinkRatio, 0.5f, 1f);
    }
}

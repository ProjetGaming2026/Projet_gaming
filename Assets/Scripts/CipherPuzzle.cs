using UnityEngine;

public class CipherPuzzle : MonoBehaviour
{
    [Header("Phrases possibles (en clair)")]
    [SerializeField] private string[] possiblePhrases =
    {
        "RUN AWAY",
        "IT IS BEHIND YOU",
        "DONT LOOK BACK",
        "HIDE NOW",
        "KEEP RUNNING",
        "FIND THE EXIT",
        "IT HEARS YOU",
        "STAY QUIET"
    };

    [Header("References")]
    [SerializeField] private CipherWall cipherWall;
    [SerializeField] private CipherTerminal terminal;
    [SerializeField] private bool autoCreateWallAnchor = true;
    [SerializeField] private Vector3 autoWallOffsetFromTerminal = new Vector3(0f, 1.15f, 1.6f);

    [Header("Answer Mode")]
    [SerializeField] private bool wallShowsClearPhrase = true;

    [Header("GameManager")]
    [SerializeField] private bool countAsTask = true;

    public string CorrectPhrase { get; private set; }
    public int ShiftValue { get; private set; }
    public string ExpectedAnswer { get; private set; }

    private bool isSolved;

    private void Awake()
    {
        if (terminal == null)
        {
            terminal = FindFirstObjectByType<CipherTerminal>();
        }

        if (cipherWall == null)
        {
            cipherWall = FindPlacedCipherWall();
        }

        if (cipherWall == null && autoCreateWallAnchor)
        {
            cipherWall = CreateAutoWallAnchor();
        }

        if (cipherWall == null)
        {
            cipherWall = FindFirstObjectByType<CipherWall>();
        }

        if (cipherWall == null)
        {
            cipherWall = CreateAutoWallAnchor();
            Debug.Log("CipherPuzzle: aucun mur configure, creation automatique de AutoCipherWall.");
        }
    }

    private void Start()
    {
        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        CorrectPhrase = possiblePhrases[Random.Range(0, possiblePhrases.Length)];
        ShiftValue = Random.Range(2, 6);

        string encryptedPhrase = Encrypt(CorrectPhrase, ShiftValue);
        ExpectedAnswer = wallShowsClearPhrase ? encryptedPhrase : CorrectPhrase;

        if (cipherWall != null)
        {
            cipherWall.DisplayText(wallShowsClearPhrase ? CorrectPhrase : encryptedPhrase, ShiftValue);
        }
        else
        {
            Debug.LogWarning("CipherPuzzle: aucun CipherWall trouve dans la scene.");
        }
    }

    public static string Encrypt(string text, int shift)
    {
        string result = "";

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
                result += (char)(((c - baseChar + shift) % 26) + baseChar);
            }
            else
            {
                result += c;
            }
        }

        return result;
    }

    public void OnPuzzleSolved()
    {
        if (isSolved)
        {
            return;
        }

        isSolved = true;

        if (countAsTask)
        {
            PrototypeGameManager gm = FindFirstObjectByType<PrototypeGameManager>();
            if (gm != null)
            {
                gm.RegisterTaskSuccess();
            }
        }
    }

    private CipherWall CreateAutoWallAnchor()
    {
        CipherWall existingAutoWall = FindExistingAutoWall();
        if (existingAutoWall != null)
        {
            return existingAutoWall;
        }

        if (terminal == null)
        {
            terminal = FindFirstObjectByType<CipherTerminal>();
        }

        GameObject anchor = new GameObject("AutoCipherWall");

        if (terminal != null)
        {
            Transform terminalTransform = terminal.transform;
            anchor.transform.position = terminalTransform.TransformPoint(autoWallOffsetFromTerminal);
            anchor.transform.rotation = terminalTransform.rotation * Quaternion.Euler(0f, -90f, 0f);
        }

        return anchor.AddComponent<CipherWall>();
    }

    private CipherWall FindExistingAutoWall()
    {
        CipherWall[] walls = FindObjectsByType<CipherWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CipherWall wall in walls)
        {
            if (wall != null && wall.name == "AutoCipherWall")
            {
                return wall;
            }
        }

        return null;
    }

    private CipherWall FindPlacedCipherWall()
    {
        CipherWall[] walls = FindObjectsByType<CipherWall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CipherWall wall in walls)
        {
            if (wall == null)
            {
                continue;
            }

            if (wall.name == "AutoCipherWall")
            {
                continue;
            }

            return wall;
        }

        return null;
    }
}

using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ChemistryRecipe
{
    public string recipeName = "Nouvelle recette";
    public int redDose;
    public int blueDose;
    public int greenDose;
    public int whiteDose;
    public int blackDose;
    public FlaskColor[] colorOrder;
    public Material bookMaterial;
    public Color resultColor = Color.white;
}

public class ChemistryPuzzleManager : MonoBehaviour, IInteractable
{
    public Transform player;
    public Transform puzzleCenter;
    public float interactionDistance = 3f;

    public GameObject mainCamera;
    public GameObject puzzleCamera;
    public bool lockPlayerCameraOnPuzzle = true;
    public bool useFixedPuzzleView = true;
    public Vector3 fixedPuzzlePlayerPosition = new Vector3(-6.716f, 1f, 1.895f);
    public float fixedPuzzleYaw = 180f;
    public float fixedPuzzlePitch = 0f;
    public bool useExactCameraPose = true;
    public Vector3 fixedPuzzleCameraPosition = new Vector3(-6.716f, 1f, 1.895f);
    public Vector3 fixedPuzzleCameraEuler = new Vector3(0f, 180f, 0f);
    public bool useLocalFixedCameraPose = false;
    public bool lookAtPuzzleFromFixedCameraPose = false;
    public Vector3 fixedPuzzleCameraLookOffset = new Vector3(-0.1f, 0.35f, 0.75f);
    public Vector3 puzzleCameraLookOffset = new Vector3(0f, 0.65f, -0.15f);

    public Renderer testTubeRenderer;
    public GameObject hiddenKey;
    public int requiredRecipeCount = 3;

    public Renderer bookRenderer;
    public Material bookVioletMaterial;
    public Material bookPinkMaterial;
    public ChemistryRecipe[] recipes;

    public GameObject recipePoint1;
    public GameObject recipePoint2;
    public GameObject recipePoint3;
    public GameObject recipePoint4;

    public Material pointRedMaterial;
    public Material pointBlueMaterial;
    public Material pointGreenMaterial;
    public Material pointWhiteMaterial;
    public Material pointBlackMaterial;

    public TMP_Text controlsText;
    public float controlsFontSize = 18f;
    public AudioClip wrongAttemptSound;
    public float wrongAttemptVolume = 1f;
    public AudioClip correctAttemptSound;
    public float correctAttemptVolume = 1f;
    public AudioClip colorInputSound;
    public float colorInputVolume = 1f;
    public float colorInputPitch = 1.25f;

    private int redDose = 0;
    private int blueDose = 0;
    private int greenDose = 0;
    private int whiteDose = 0;
    private int blackDose = 0;

    private bool puzzleActive = false;
    private bool puzzleSolved = false;
    private int solvedRecipeCount = 0;

    private int targetRed;
    private int targetBlue;
    private int targetGreen;
    private int targetWhite;
    private int targetBlack;
    private FlaskColor[] targetSequence = new FlaskColor[0];
    private readonly List<FlaskColor> enteredSequence = new List<FlaskColor>();
    private ChemistryRecipe activeRecipe;
    private int lastStartFrame = -1;
    private int lastDoseFrame = -1;
    private int lastValidateFrame = -1;
    private BasicFirstPersonController playerController;
    private AudioSource audioSource;

    public bool IsPuzzleActive
    {
        get { return puzzleActive; }
    }

    public bool IsPuzzleSolved
    {
        get { return puzzleSolved; }
    }

    private void Start()
    {
        FindPlayerController();
        EnsureAudioSource();
        KeyControl.hasLaboratoryKey = false;
        ResetMixture();

        if (hiddenKey != null)
            hiddenKey.SetActive(false);

        if (controlsText != null)
        {
            controlsText.fontSize = controlsFontSize;
            controlsText.enableWordWrapping = false;
            controlsText.alignment = TextAlignmentOptions.Center;
            controlsText.gameObject.SetActive(false);
        }

        if (puzzleCamera != null)
            puzzleCamera.SetActive(false);

        if (mainCamera != null)
            mainCamera.SetActive(true);

        ChooseRandomRecipe();
    }

    private void Update()
    {
        if (puzzleSolved)
        {
            if (KeyControl.hasLaboratoryKey)
                HideText();

            return;
        }

        if (player == null || puzzleCenter == null) return;

        float distance = Vector3.Distance(player.position, puzzleCenter.position);
        bool playerIsNear = distance <= interactionDistance;

        if (!playerIsNear && !puzzleActive)
        {
            HideText();
            return;
        }

        if (!puzzleActive)
        {
            ShowText("E : commencer");

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartPuzzleInteraction();
            }

            return;
        }

        ShowText("E : finir");

        if (Input.GetKeyDown(KeyCode.R))
            AddRed();

        if (Input.GetKeyDown(KeyCode.U))
            AddBlue();

        if (Input.GetKeyDown(KeyCode.G))
            AddGreen();

        if (Input.GetKeyDown(KeyCode.B))
            AddWhite();

        if (Input.GetKeyDown(KeyCode.N))
            AddBlack();

        if (Input.GetKeyDown(KeyCode.X))
            ResetMixture();

        if (Input.GetKeyDown(KeyCode.E) && Time.frameCount != lastStartFrame && Time.frameCount != lastDoseFrame)
            ValidateMixture();
    }

    private void StartPuzzleInteraction()
    {
        if (puzzleSolved || puzzleActive)
        {
            return;
        }

        puzzleActive = true;
        lastStartFrame = Time.frameCount;
        LockPlayerOnPuzzle();

        if (mainCamera != null)
            mainCamera.SetActive(true);

        if (puzzleCamera != null)
            puzzleCamera.SetActive(false);

        Debug.Log("Interaction avec le puzzle commencée.");
    }

    private void ValidateMixture()
    {
        if (Time.frameCount == lastValidateFrame)
        {
            return;
        }

        lastValidateFrame = Time.frameCount;

        if (!puzzleActive || puzzleSolved)
        {
            return;
        }

        if (redDose == targetRed &&
            blueDose == targetBlue &&
            greenDose == targetGreen &&
            whiteDose == targetWhite &&
            blackDose == targetBlack &&
            IsEnteredSequenceComplete())
        {
            PlayCorrectAttemptSound();
            CompleteCurrentRecipe();
        }
        else
        {
            PlayWrongAttemptSound();
            ResetMixture();
            ShowText("Rate. Recommence");
            Debug.Log("Mélange incorrect.");
        }
    }

    private void CompleteCurrentRecipe()
    {
        solvedRecipeCount++;
        ResetMixture();

        if (solvedRecipeCount >= Mathf.Max(1, requiredRecipeCount))
        {
            SolvePuzzle();
            return;
        }

        ChooseRandomRecipe();
        ShowText("Recette " + solvedRecipeCount + "/" + Mathf.Max(1, requiredRecipeCount));
    }

    private void SolvePuzzle()
    {
        puzzleSolved = true;
        puzzleActive = false;

        if (hiddenKey != null)
            hiddenKey.SetActive(true);

        if (mainCamera != null)
            mainCamera.SetActive(true);

        if (puzzleCamera != null)
            puzzleCamera.SetActive(false);

        UnlockPlayer();

        ShowText("Cle apparue");

        Debug.Log("Bon mélange ! La clé apparaît.");
    }

    private void ChooseRandomRecipe()
    {
        if (recipes != null && recipes.Length > 0)
        {
            ChemistryRecipe recipe = PickValidCustomRecipe();
            if (recipe != null)
            {
                activeRecipe = recipe;
                ApplyRecipe(activeRecipe);
                return;
            }
        }

        ChooseFallbackRecipe();
    }

    private ChemistryRecipe PickValidCustomRecipe()
    {
        int validRecipeCount = 0;

        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && !IsCanceledRecipe(recipes[i]))
            {
                validRecipeCount++;
            }
        }

        if (validRecipeCount == 0)
        {
            return null;
        }

        int pickedIndex = Random.Range(0, validRecipeCount);
        for (int i = 0; i < recipes.Length; i++)
        {
            ChemistryRecipe recipe = recipes[i];
            if (recipe == null || IsCanceledRecipe(recipe))
            {
                continue;
            }

            if (pickedIndex == 0)
            {
                return recipe;
            }

            pickedIndex--;
        }

        return null;
    }

    private bool IsCanceledRecipe(ChemistryRecipe recipe)
    {
        return recipe != null &&
            Mathf.Max(0, recipe.redDose) == 2 &&
            Mathf.Max(0, recipe.blueDose) == 2 &&
            Mathf.Max(0, recipe.greenDose) == 0 &&
            Mathf.Max(0, recipe.whiteDose) == 0 &&
            Mathf.Max(0, recipe.blackDose) == 0;
    }

    private void ApplyRecipe(ChemistryRecipe recipe)
    {
        if (recipe == null)
        {
            ChooseFallbackRecipe();
            return;
        }

        targetRed = Mathf.Max(0, recipe.redDose);
        targetBlue = Mathf.Max(0, recipe.blueDose);
        targetGreen = Mathf.Max(0, recipe.greenDose);
        targetWhite = Mathf.Max(0, recipe.whiteDose);
        targetBlack = Mathf.Max(0, recipe.blackDose);
        targetSequence = BuildRecipeSequence(recipe);

        if (bookRenderer != null && recipe.bookMaterial != null)
            bookRenderer.material = recipe.bookMaterial;

        ApplyBookColor(recipe.resultColor);
        ShowRecipePoints(targetSequence);
    }

    private void ChooseFallbackRecipe()
    {
        int recipeIndex = solvedRecipeCount % 3;

        if (recipeIndex == 0)
        {
            targetRed = 1;
            targetBlue = 1;
            targetGreen = 1;
            targetWhite = 1;
            targetBlack = 0;

            activeRecipe = CreateRuntimeRecipe("Serum A", targetRed, targetBlue, targetGreen, targetWhite, targetBlack, bookPinkMaterial, new Color(0.7f, 0.55f, 0.75f), FlaskColor.Red, FlaskColor.White, FlaskColor.Green, FlaskColor.Blue);

            if (bookRenderer != null && bookPinkMaterial != null)
                bookRenderer.material = bookPinkMaterial;

            ApplyBookColor(activeRecipe.resultColor);
            targetSequence = BuildRecipeSequence(activeRecipe);
            ShowRecipePoints(targetSequence);
        }
        else if (recipeIndex == 1)
        {
            targetRed = 1;
            targetBlue = 0;
            targetGreen = 1;
            targetWhite = 1;
            targetBlack = 1;

            activeRecipe = CreateRuntimeRecipe("Serum B", targetRed, targetBlue, targetGreen, targetWhite, targetBlack, bookVioletMaterial, new Color(0.45f, 0.55f, 0.35f), FlaskColor.Black, FlaskColor.Green, FlaskColor.White, FlaskColor.Red);

            if (bookRenderer != null && bookVioletMaterial != null)
                bookRenderer.material = bookVioletMaterial;

            ApplyBookColor(activeRecipe.resultColor);
            targetSequence = BuildRecipeSequence(activeRecipe);
            ShowRecipePoints(targetSequence);
        }
        else
        {
            targetRed = 0;
            targetBlue = 1;
            targetGreen = 1;
            targetWhite = 1;
            targetBlack = 1;

            activeRecipe = CreateRuntimeRecipe("Serum C", targetRed, targetBlue, targetGreen, targetWhite, targetBlack, null, new Color(0.25f, 0.65f, 0.55f), FlaskColor.Green, FlaskColor.Black, FlaskColor.White, FlaskColor.Blue);

            ApplyBookColor(activeRecipe.resultColor);
            targetSequence = BuildRecipeSequence(activeRecipe);
            ShowRecipePoints(targetSequence);
        }
    }

    private ChemistryRecipe CreateRuntimeRecipe(string recipeName, int red, int blue, int green, int white, int black, Material bookMaterial, Color resultColor, params FlaskColor[] colorOrder)
    {
        ChemistryRecipe recipe = new ChemistryRecipe();
        recipe.recipeName = recipeName;
        recipe.redDose = red;
        recipe.blueDose = blue;
        recipe.greenDose = green;
        recipe.whiteDose = white;
        recipe.blackDose = black;
        recipe.bookMaterial = bookMaterial;
        recipe.resultColor = resultColor;
        recipe.colorOrder = colorOrder;
        return recipe;
    }

    private void ApplyBookColor(Color color)
    {
        if (bookRenderer == null)
        {
            return;
        }

        bookRenderer.material.color = color;
    }

    private FlaskColor[] BuildRecipeSequence(ChemistryRecipe recipe)
    {
        if (recipe != null && recipe.colorOrder != null && recipe.colorOrder.Length > 0)
        {
            return recipe.colorOrder;
        }

        List<FlaskColor> sequence = new List<FlaskColor>();
        AddColorToSequence(sequence, FlaskColor.Red, targetRed);
        AddColorToSequence(sequence, FlaskColor.Blue, targetBlue);
        AddColorToSequence(sequence, FlaskColor.Green, targetGreen);
        AddColorToSequence(sequence, FlaskColor.White, targetWhite);
        AddColorToSequence(sequence, FlaskColor.Black, targetBlack);
        ShuffleSequence(sequence);
        return sequence.ToArray();
    }

    private void AddColorToSequence(List<FlaskColor> sequence, FlaskColor color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            sequence.Add(color);
        }
    }

    private void ShuffleSequence(List<FlaskColor> sequence)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            int randomIndex = Random.Range(i, sequence.Count);
            FlaskColor current = sequence[i];
            sequence[i] = sequence[randomIndex];
            sequence[randomIndex] = current;
        }
    }

    private void ShowRecipePoints(FlaskColor[] sequence)
    {
        GameObject[] points = { recipePoint1, recipePoint2, recipePoint3, recipePoint4 };

        for (int i = 0; i < points.Length; i++)
        {
            if (sequence != null && i < sequence.Length)
            {
                ShowPoint(points[i], GetPointMaterial(sequence[i]), GetPointFallbackColor(sequence[i]));
            }
            else
            {
                HidePoint(points[i]);
            }
        }
    }

    private Material GetPointMaterial(FlaskColor color)
    {
        switch (color)
        {
            case FlaskColor.Red:
                return pointRedMaterial;
            case FlaskColor.Blue:
                return pointBlueMaterial;
            case FlaskColor.Green:
                return pointGreenMaterial;
            case FlaskColor.White:
                return pointWhiteMaterial;
            case FlaskColor.Black:
                return pointBlackMaterial;
            default:
                return null;
        }
    }

    private Color GetPointFallbackColor(FlaskColor color)
    {
        switch (color)
        {
            case FlaskColor.Red:
                return Color.red;
            case FlaskColor.Blue:
                return Color.blue;
            case FlaskColor.Green:
                return Color.green;
            case FlaskColor.White:
                return Color.white;
            case FlaskColor.Black:
                return Color.black;
            default:
                return Color.white;
        }
    }

    private void ShowPoint(GameObject point, Material material, Color fallbackColor)
    {
        if (point == null) return;

        point.SetActive(true);

        Renderer pointRenderer = point.GetComponent<Renderer>();
        if (pointRenderer == null)
            pointRenderer = point.GetComponentInChildren<Renderer>();

        if (pointRenderer == null)
            return;

        if (material != null)
            pointRenderer.material = material;
        else
            pointRenderer.material.color = fallbackColor;
    }

    private void HidePoint(GameObject point)
    {
        if (point != null)
            point.SetActive(false);
    }

    public void AddRed()
    {
        if (!TryAddDose(FlaskColor.Red)) return;

        redDose++;
        UpdateMixtureColor();
    }

    public void AddBlue()
    {
        if (!TryAddDose(FlaskColor.Blue)) return;

        blueDose++;
        UpdateMixtureColor();
    }

    public void AddGreen()
    {
        if (!TryAddDose(FlaskColor.Green)) return;

        greenDose++;
        UpdateMixtureColor();
    }

    public void AddWhite()
    {
        if (!TryAddDose(FlaskColor.White)) return;

        whiteDose++;
        UpdateMixtureColor();
    }

    public void AddBlack()
    {
        if (!TryAddDose(FlaskColor.Black)) return;

        blackDose++;
        UpdateMixtureColor();
    }

    public void ResetMixture()
    {
        redDose = 0;
        blueDose = 0;
        greenDose = 0;
        whiteDose = 0;
        blackDose = 0;
        enteredSequence.Clear();

        if (testTubeRenderer != null)
            testTubeRenderer.material.color = Color.white;
    }

    public void Interact()
    {
        if (puzzleSolved)
        {
            return;
        }

        if (!puzzleActive)
        {
            StartPuzzleInteraction();
            return;
        }

        if (Time.frameCount != lastStartFrame)
        {
            ValidateMixture();
        }
    }

    public string GetInteractPrompt()
    {
        if (puzzleSolved)
        {
            return KeyControl.hasLaboratoryKey ? "" : "Cle apparue";
        }

        return puzzleActive
            ? "E : finir"
            : "E : commencer";
    }

    private bool CanAddDose()
    {
        return puzzleActive && !puzzleSolved;
    }

    private bool TryAddDose(FlaskColor color)
    {
        if (!CanAddDose()) return false;

        lastDoseFrame = Time.frameCount;

        if (!IsNextColorCorrect(color))
        {
            PlayWrongAttemptSound();
            ResetMixture();
            ShowText("Ordre incorrect");
            Debug.Log("Ordre de couleur incorrect.");
            return false;
        }

        enteredSequence.Add(color);
        PlayColorInputSound();
        return true;
    }

    private bool IsNextColorCorrect(FlaskColor color)
    {
        if (targetSequence == null || targetSequence.Length == 0)
        {
            return true;
        }

        int sequenceIndex = enteredSequence.Count;
        return sequenceIndex < targetSequence.Length && targetSequence[sequenceIndex] == color;
    }

    private bool IsEnteredSequenceComplete()
    {
        return targetSequence == null ||
            targetSequence.Length == 0 ||
            enteredSequence.Count == targetSequence.Length;
    }

    private void FindPlayerController()
    {
        if (player != null)
        {
            playerController = player.GetComponent<BasicFirstPersonController>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<BasicFirstPersonController>();
        }

        if (mainCamera == null && Camera.main != null)
        {
            mainCamera = Camera.main.gameObject;
        }
    }

    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void PlayWrongAttemptSound()
    {
        if (wrongAttemptSound == null)
        {
            return;
        }

        if (audioSource == null)
        {
            EnsureAudioSource();
        }

        audioSource.PlayOneShot(wrongAttemptSound, wrongAttemptVolume);
    }

    private void PlayCorrectAttemptSound()
    {
        if (correctAttemptSound == null)
        {
            return;
        }

        if (audioSource == null)
        {
            EnsureAudioSource();
        }

        audioSource.PlayOneShot(correctAttemptSound, correctAttemptVolume);
    }

    private void PlayColorInputSound()
    {
        if (colorInputSound == null)
        {
            return;
        }

        if (audioSource == null)
        {
            EnsureAudioSource();
        }

        float previousPitch = audioSource.pitch;
        audioSource.pitch = colorInputPitch;
        audioSource.PlayOneShot(colorInputSound, colorInputVolume);
        audioSource.pitch = previousPitch;
    }

    private void LockPlayerOnPuzzle()
    {
        if (!lockPlayerCameraOnPuzzle)
        {
            return;
        }

        FindPlayerController();

        if (playerController == null)
        {
            return;
        }

        playerController.SetExternalControlLock(true);

        if (useFixedPuzzleView)
        {
            playerController.ForceViewPose(fixedPuzzlePlayerPosition, fixedPuzzleYaw, fixedPuzzlePitch);

            if (useExactCameraPose)
            {
                if (useLocalFixedCameraPose)
                {
                    playerController.ForceCameraLocalPose(fixedPuzzleCameraPosition, fixedPuzzleCameraEuler);
                }
                else if (lookAtPuzzleFromFixedCameraPose && puzzleCenter != null)
                {
                    playerController.ForceCameraLookAtWorldPose(
                        fixedPuzzleCameraPosition,
                        puzzleCenter.position + fixedPuzzleCameraLookOffset
                    );
                }
                else
                {
                    playerController.ForceCameraWorldPose(fixedPuzzleCameraPosition, fixedPuzzleCameraEuler);
                }
            }

            return;
        }

        if (puzzleCenter != null)
        {
            playerController.ForceLookAt(puzzleCenter.position + puzzleCameraLookOffset);
        }
    }

    private void UnlockPlayer()
    {
        if (playerController != null)
        {
            if (useFixedPuzzleView && useExactCameraPose)
            {
                playerController.RestoreDefaultCameraLocalPose();
            }

            playerController.SetExternalControlLock(false);
        }
    }

    private void UpdateMixtureColor()
    {
        if (testTubeRenderer == null) return;

        int totalDoses = redDose + blueDose + greenDose + whiteDose + blackDose;

        if (totalDoses == 0)
        {
            testTubeRenderer.material.color = Color.white;
            return;
        }

        ChemistryRecipe matchingRecipe = FindMatchingRecipe(redDose, blueDose, greenDose, whiteDose, blackDose);
        if (matchingRecipe != null)
        {
            testTubeRenderer.material.color = matchingRecipe.resultColor;
            return;
        }

        if (activeRecipe != null && MatchesRecipe(activeRecipe, redDose, blueDose, greenDose, whiteDose, blackDose))
        {
            testTubeRenderer.material.color = activeRecipe.resultColor;
            return;
        }

        if (redDose == 1 && whiteDose == 1 && blueDose == 0 && greenDose == 0 && blackDose == 0)
        {
            testTubeRenderer.material.color = new Color(1f, 0.35f, 0.7f);
            return;
        }

        if (redDose == 2 && blueDose == 1 && blackDose == 1 && greenDose == 0 && whiteDose == 0)
        {
            testTubeRenderer.material.color = new Color(0.35f, 0f, 0.55f);
            return;
        }

        Color mixedColor = Color.clear;
        mixedColor += Color.red * redDose;
        mixedColor += Color.blue * blueDose;
        mixedColor += Color.green * greenDose;
        mixedColor += Color.white * whiteDose;
        mixedColor += Color.black * blackDose;
        mixedColor /= totalDoses;

        testTubeRenderer.material.color = mixedColor;
    }

    private ChemistryRecipe FindMatchingRecipe(int red, int blue, int green, int white, int black)
    {
        if (recipes == null)
        {
            return null;
        }

        for (int i = 0; i < recipes.Length; i++)
        {
            ChemistryRecipe recipe = recipes[i];
            if (recipe == null)
            {
                continue;
            }

            if (IsCanceledRecipe(recipe))
            {
                continue;
            }

            if (red == Mathf.Max(0, recipe.redDose) &&
                blue == Mathf.Max(0, recipe.blueDose) &&
                green == Mathf.Max(0, recipe.greenDose) &&
                white == Mathf.Max(0, recipe.whiteDose) &&
                black == Mathf.Max(0, recipe.blackDose))
            {
                return recipe;
            }
        }

        return null;
    }

    private bool MatchesRecipe(ChemistryRecipe recipe, int red, int blue, int green, int white, int black)
    {
        if (recipe == null)
        {
            return false;
        }

        return red == Mathf.Max(0, recipe.redDose) &&
            blue == Mathf.Max(0, recipe.blueDose) &&
            green == Mathf.Max(0, recipe.greenDose) &&
            white == Mathf.Max(0, recipe.whiteDose) &&
            black == Mathf.Max(0, recipe.blackDose);
    }

    private void ShowText(string message)
    {
        if (controlsText == null) return;

        controlsText.gameObject.SetActive(true);
        controlsText.fontSize = controlsFontSize;
        controlsText.enableWordWrapping = false;
        controlsText.alignment = TextAlignmentOptions.Center;
        controlsText.text = message;
    }

    private void HideText()
    {
        if (controlsText == null) return;

        controlsText.text = "";
        controlsText.gameObject.SetActive(false);
    }
}


using UnityEngine;
using TMPro;

public class FlaskClick : MonoBehaviour, IInteractable
{
    public ChemistryPuzzleManager puzzleManager;
    public FlaskColor flaskColor;
    public bool showKeyLabel = false;
    public bool placeLabelOnWall = false;
    public Transform keyLabelAnchor;
    public Vector3 keyLabelOffset = new Vector3(0f, 0.05f, 0f);
    public Vector3 keyLabelEulerAngles = new Vector3(0f, 0f, 0f);
    public float wallSearchDistance = 2.5f;
    public float wallSurfaceOffset = 0.015f;
    public float keyLabelWorldScale = 0.035f;
    public float keyLabelFontSize = 1.35f;
    public float tableLabelHeight = 0.56f;
    public float tableLabelLineZ = -0.62f;
    public Color keyLabelColor = Color.white;

    private TMP_Text keyLabel;
    private Renderer flaskRenderer;

    private void Start()
    {
        flaskRenderer = GetComponentInChildren<Renderer>();
        CreateKeyLabel();
    }

    private void LateUpdate()
    {
        if (keyLabel == null)
        {
            return;
        }

        bool shouldShowLabel = showKeyLabel && puzzleManager != null && puzzleManager.IsPuzzleActive;
        keyLabel.gameObject.SetActive(shouldShowLabel);

        if (!shouldShowLabel)
        {
            return;
        }

        if (keyLabelAnchor != null)
        {
            keyLabel.transform.position = keyLabelAnchor.position;
            keyLabel.transform.rotation = keyLabelAnchor.rotation;
        }
        else if (placeLabelOnWall && TryFindWallLabelPose(out Vector3 wallPosition, out Quaternion wallRotation))
        {
            keyLabel.transform.position = wallPosition;
            keyLabel.transform.rotation = wallRotation;
        }
        else
        {
            keyLabel.transform.position = GetTableLabelPosition();
            FaceCamera();
        }

        keyLabel.transform.localScale = Vector3.one * keyLabelWorldScale;
    }

    private void OnMouseDown()
    {
        PourFlask();
    }

    public void Interact()
    {
        if (puzzleManager != null && puzzleManager.IsPuzzleActive)
        {
            return;
        }

        PourFlask();
    }

    public string GetInteractPrompt()
    {
        string colorName = GetColorName();

        if (puzzleManager == null)
        {
            return "Flacon " + colorName + " non connecte";
        }

        if (puzzleManager.IsPuzzleSolved)
        {
            return "Reaction terminee";
        }

        if (!puzzleManager.IsPuzzleActive)
        {
            return "Commence la reaction avant de toucher le flacon " + colorName;
        }

        return "";
    }

    private void CreateKeyLabel()
    {
        if (!showKeyLabel || keyLabel != null)
        {
            return;
        }

        GameObject labelObject = new GameObject("Touche " + GetKeyText());
        labelObject.transform.SetParent(transform, true);

        keyLabel = labelObject.AddComponent<TextMeshPro>();
        keyLabel.text = GetDisplayText();
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.fontSize = keyLabelFontSize;
        keyLabel.color = Color.white;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.enableWordWrapping = false;
        keyLabel.lineSpacing = -10f;
        keyLabel.rectTransform.sizeDelta = new Vector2(0.55f, 0.35f);
        keyLabel.transform.localScale = Vector3.one * keyLabelWorldScale;
        keyLabel.gameObject.SetActive(false);
    }

    private Vector3 GetFlaskCenter()
    {
        if (flaskRenderer != null)
        {
            return flaskRenderer.bounds.center;
        }

        return transform.position;
    }

    private Vector3 GetTableLabelPosition()
    {
        Vector3 flaskCenter = GetFlaskCenter();

        return new Vector3(flaskCenter.x, tableLabelHeight, tableLabelLineZ) +
            keyLabelOffset +
            GetReadableTableOffset();
    }

    private void FaceCamera()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            keyLabel.transform.rotation = Quaternion.Euler(keyLabelEulerAngles);
            return;
        }

        Vector3 directionToCamera = keyLabel.transform.position - activeCamera.transform.position;
        if (directionToCamera.sqrMagnitude <= 0.0001f)
        {
            keyLabel.transform.rotation = Quaternion.Euler(keyLabelEulerAngles);
            return;
        }

        keyLabel.transform.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
    }

    private bool TryFindWallLabelPose(out Vector3 position, out Quaternion rotation)
    {
        Vector3 origin = GetFlaskCenter() + keyLabelOffset + GetColorWallSpacingOffset();
        Vector3[] directions =
        {
            Vector3.back,
            Vector3.forward,
            Vector3.left,
            Vector3.right
        };

        RaycastHit bestHit = new RaycastHit();
        bool foundHit = false;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(origin, directions[i], out RaycastHit hit, wallSearchDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.GetComponentInParent<FlaskClick>() != null)
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                    foundHit = true;
                }
            }
        }

        if (!foundHit)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = bestHit.point + bestHit.normal * wallSurfaceOffset;
        rotation = Quaternion.LookRotation(-bestHit.normal, Vector3.up);
        return true;
    }

    private void PourFlask()
    {
        if (puzzleManager == null)
        {
            Debug.LogWarning("Puzzle Manager manquant sur " + gameObject.name);
            return;
        }

        if (flaskColor == FlaskColor.Red)
            puzzleManager.AddRed();

        if (flaskColor == FlaskColor.Blue)
            puzzleManager.AddBlue();

        if (flaskColor == FlaskColor.Green)
            puzzleManager.AddGreen();

        if (flaskColor == FlaskColor.White)
            puzzleManager.AddWhite();

        if (flaskColor == FlaskColor.Black)
            puzzleManager.AddBlack();
    }

    private string GetColorName()
    {
        switch (flaskColor)
        {
            case FlaskColor.Red:
                return "rouge";
            case FlaskColor.Blue:
                return "bleu";
            case FlaskColor.Green:
                return "vert";
            case FlaskColor.White:
                return "blanc";
            case FlaskColor.Black:
                return "noir";
            default:
                return "inconnu";
        }
    }

    private string GetKeyText()
    {
        switch (flaskColor)
        {
            case FlaskColor.Red:
                return "R";
            case FlaskColor.Blue:
                return "U";
            case FlaskColor.Green:
                return "G";
            case FlaskColor.White:
                return "B";
            case FlaskColor.Black:
                return "N";
            default:
                return "?";
        }
    }

    private string GetDisplayText()
    {
        return GetKeyText();
    }

    private Vector3 GetReadableTableOffset()
    {
        switch (flaskColor)
        {
            case FlaskColor.Blue:
                return new Vector3(-0.03f, 0f, 0f);
            case FlaskColor.Green:
                return new Vector3(0.03f, 0f, 0f);
            case FlaskColor.Black:
                return Vector3.zero;
            default:
                return Vector3.zero;
        }
    }

    private Vector3 GetColorWallSpacingOffset()
    {
        switch (flaskColor)
        {
            case FlaskColor.Blue:
                return new Vector3(-1.25f, 0.38f, 0f);
            case FlaskColor.White:
                return new Vector3(1.25f, 0f, 0f);
            case FlaskColor.Black:
                return new Vector3(0f, 0f, 0f);
            case FlaskColor.Red:
                return new Vector3(-1.25f, 0f, 0f);
            case FlaskColor.Green:
                return new Vector3(1.25f, 0.38f, 0f);
            default:
                return Vector3.zero;
        }
    }

    private string GetColorNameCapitalized()
    {
        switch (flaskColor)
        {
            case FlaskColor.Red:
                return "Rouge";
            case FlaskColor.Blue:
                return "Bleu";
            case FlaskColor.Green:
                return "Vert";
            case FlaskColor.White:
                return "Blanc";
            case FlaskColor.Black:
                return "Noir";
            default:
                return "Couleur";
        }
    }
}

public enum FlaskColor
{
    Red,
    Blue,
    Green,
    White,
    Black
}

using TMPro;
using UnityEngine;

public class GeneratedMedicalFile : MonoBehaviour
{
    public enum FileStyle
    {
        RedDeceased,
        BlueMemory,
        BlackObservation
    }

    private const string FolderObjectName = "Generated_File_Folder";
    private const string PaperObjectName = "Generated_File_Paper";
    private const string LabelObjectName = "Generated_File_Label";
    private const string AccentObjectName = "Generated_File_Accent";

    [Header("Generated File")]
    [SerializeField] private FileStyle style;
    [SerializeField] private string displayTitle = "MEDICAL FILE";
    [SerializeField, TextArea(4, 12)] private string visibleSummary;
    [SerializeField] private bool showWorldLabel = true;

    [Header("Colors")]
    [SerializeField] private Color folderColor = new Color(0.35f, 0.02f, 0.018f, 1f);
    [SerializeField] private Color paperColor = new Color(0.68f, 0.62f, 0.48f, 1f);
    [SerializeField] private Color inkColor = new Color(0.06f, 0.04f, 0.03f, 1f);
    [SerializeField] private Color accentColor = new Color(0.62f, 0.02f, 0.018f, 1f);

    [Header("World Label")]
    [SerializeField, Min(0.1f)] private float labelFontSize = 3.6f;
    [SerializeField] private Vector2 labelRectSize = new Vector2(8f, 5.4f);

    private TextMeshPro labelText;

    public FileStyle Style => style;
    public string DisplayTitle => displayTitle;

    public void Configure(FileStyle fileStyle, string title, string summary)
    {
        style = fileStyle;
        displayTitle = title;
        visibleSummary = summary;
        ApplyStyleColors();
        EnsureVisuals();
    }

    public void EnsureVisuals()
    {
        ApplyStyleColors();

        GameObject folder = FindOrCreatePrimitive(FolderObjectName, PrimitiveType.Cube);
        folder.transform.localPosition = Vector3.zero;
        folder.transform.localRotation = Quaternion.identity;
        folder.transform.localScale = new Vector3(1f, 0.42f, 1f);
        SetRendererMaterial(folder, CreateMaterial("Runtime_" + name + "_Folder", folderColor, 0.01f, 0.22f));

        GameObject paper = FindOrCreatePrimitive(PaperObjectName, PrimitiveType.Cube);
        paper.transform.localPosition = new Vector3(0f, 0.34f, 0.02f);
        paper.transform.localRotation = Quaternion.identity;
        paper.transform.localScale = new Vector3(0.83f, 0.18f, 0.72f);
        SetRendererMaterial(paper, CreateMaterial("Runtime_" + name + "_Paper", paperColor, 0f, 0.12f));

        GameObject accent = FindOrCreatePrimitive(AccentObjectName, PrimitiveType.Cube);
        accent.transform.localPosition = new Vector3(-0.36f, 0.46f, 0.37f);
        accent.transform.localRotation = Quaternion.identity;
        accent.transform.localScale = new Vector3(0.18f, 0.08f, 0.17f);
        SetRendererMaterial(accent, CreateMaterial("Runtime_" + name + "_Accent", accentColor, 0.02f, 0.28f));

        EnsureLabel();
        EnsureRootCollider();
    }

    private void ApplyStyleColors()
    {
        switch (style)
        {
            case FileStyle.RedDeceased:
                folderColor = new Color(0.30f, 0.018f, 0.016f, 1f);
                accentColor = new Color(0.62f, 0.02f, 0.018f, 1f);
                inkColor = new Color(0.11f, 0.012f, 0.01f, 1f);
                break;
            case FileStyle.BlueMemory:
                folderColor = new Color(0.04f, 0.12f, 0.28f, 1f);
                accentColor = new Color(0.12f, 0.32f, 0.62f, 1f);
                inkColor = new Color(0.025f, 0.045f, 0.085f, 1f);
                break;
            case FileStyle.BlackObservation:
                folderColor = new Color(0.018f, 0.017f, 0.016f, 1f);
                accentColor = new Color(0.34f, 0.31f, 0.25f, 1f);
                inkColor = new Color(0.035f, 0.028f, 0.022f, 1f);
                break;
        }
    }

    private void EnsureLabel()
    {
        Transform existing = transform.Find(LabelObjectName);
        if (labelText == null && existing != null)
        {
            labelText = existing.GetComponent<TextMeshPro>();
        }

        if (labelText == null)
        {
            GameObject labelObject = new GameObject(LabelObjectName, typeof(TextMeshPro));
            labelObject.transform.SetParent(transform, false);
            labelText = labelObject.GetComponent<TextMeshPro>();
        }

        labelText.gameObject.SetActive(showWorldLabel);
        labelText.transform.localPosition = new Vector3(0f, 0.56f, 0.025f);
        labelText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelText.transform.localScale = Vector3.one * 0.075f;
        labelText.rectTransform.sizeDelta = labelRectSize;
        labelText.text = BuildVisibleLabelText();
        labelText.fontSize = labelFontSize;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 1.6f;
        labelText.fontSizeMax = labelFontSize;
        labelText.alignment = TextAlignmentOptions.TopLeft;
        labelText.color = inkColor;
        labelText.fontStyle = FontStyles.Bold;
        labelText.textWrappingMode = TextWrappingModes.Normal;
        labelText.overflowMode = TextOverflowModes.Truncate;
        labelText.enabled = showWorldLabel;
        labelText.ForceMeshUpdate();
    }

    private string BuildVisibleLabelText()
    {
        if (string.IsNullOrWhiteSpace(visibleSummary))
        {
            return displayTitle;
        }

        return displayTitle + "\n\n" + visibleSummary;
    }

    private void EnsureRootCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            box = gameObject.AddComponent<BoxCollider>();
        }

        box.center = new Vector3(0f, 0.18f, 0f);
        box.size = new Vector3(1.06f, 0.66f, 1.06f);
    }

    private GameObject FindOrCreatePrimitive(string objectName, PrimitiveType primitiveType)
    {
        Transform existing = transform.Find(objectName);
        GameObject target = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitiveType);
        target.name = objectName;
        target.transform.SetParent(transform, false);

        Collider primitiveCollider = target.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            DestroyImmediateSafe(primitiveCollider);
        }

        return target;
    }

    private static void SetRendererMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.enabled = true;
        }
    }

    private static Material CreateMaterial(string materialName, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        material.name = materialName;
        material.color = color;

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        return material;
    }

    private static void DestroyImmediateSafe(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}

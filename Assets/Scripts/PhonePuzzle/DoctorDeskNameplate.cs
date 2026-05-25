using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class DoctorDeskNameplate : MonoBehaviour
{
    private const string RootName = "Doctor_H_Marek_Nameplate";
    private const string PlateBaseName = "Nameplate_Base_Dark_Brass";
    private const string PlateFaceName = "Nameplate_Face_Aged_Ivory";
    private const string NameTextName = "Nameplate_Text_Doctor_Marek";
    private const string BloodRootName = "Nameplate_Blood_Details";
    private const string DirtRootName = "Nameplate_Dirt_And_Age";

    [Header("Nameplate")]
    [SerializeField] private string doctorName = "Doctor H. MAREK";
    [SerializeField] private Vector3 plateSize = new Vector3(0.76f, 0.045f, 0.19f);
    [SerializeField] private float frontPlacement = 0.34f;
    [SerializeField] private float surfaceOffset = 0.035f;
    [SerializeField] private Vector3 manualWorldOffset = Vector3.zero;

    [Header("Typography")]
    [SerializeField, Min(0.1f)] private float nameFontSize = 3.7f;
    [SerializeField] private Color textColor = new Color(0.035f, 0.027f, 0.018f, 1f);

    [Header("Blood")]
    [SerializeField] private bool showBlood = true;
    [SerializeField] private Color bloodDark = new Color(0.18f, 0.006f, 0.003f, 1f);
    [SerializeField] private Color bloodWet = new Color(0.44f, 0.01f, 0.005f, 1f);

    [Header("Ageing")]
    [SerializeField] private bool showDirtAndWear = true;
    [SerializeField] private Color grimeColor = new Color(0.055f, 0.043f, 0.025f, 1f);
    [SerializeField] private Color scratchColor = new Color(0.82f, 0.76f, 0.61f, 1f);

    private void Awake()
    {
        EnsureNameplate();
    }

    private void OnEnable()
    {
        EnsureNameplate();
    }

    private void OnValidate()
    {
        EnsureNameplate();
    }

    public void EnsureNameplate()
    {
        if (!gameObject.activeInHierarchy && Application.isPlaying)
        {
            return;
        }

        if (!TryResolveDeskBounds(out Bounds bounds))
        {
            return;
        }

        Vector3 forward = ResolveHorizontalForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float forwardExtent = ProjectBoundsExtent(bounds, forward);

        Transform root = FindOrCreateChild(transform, RootName);
        root.gameObject.SetActive(true);
        root.SetPositionAndRotation(
            bounds.center - forward * (forwardExtent * frontPlacement) + Vector3.up * (bounds.extents.y + surfaceOffset) + manualWorldOffset,
            Quaternion.LookRotation(forward, Vector3.up));
        root.localScale = Vector3.one;

        Material brass = CreateMaterial("Runtime_DoctorNameplate_Oxidized_Brass", new Color(0.22f, 0.17f, 0.09f, 1f), 0.42f, 0.28f);
        Material face = CreateMaterial("Runtime_DoctorNameplate_Stained_Ivory", new Color(0.50f, 0.45f, 0.34f, 1f), 0.01f, 0.18f);
        Material bloodOne = CreateMaterial("Runtime_DoctorNameplate_Dried_Blood", bloodDark, 0.02f, 0.36f);
        Material bloodTwo = CreateMaterial("Runtime_DoctorNameplate_Wet_Blood", bloodWet, 0.01f, 0.86f);
        Material grime = CreateMaterial("Runtime_DoctorNameplate_Dark_Grime", grimeColor, 0f, 0.12f);
        Material scratch = CreateMaterial("Runtime_DoctorNameplate_Exposed_Scratches", scratchColor, 0.15f, 0.22f);

        GameObject basePlate = FindOrCreatePrimitive(root, PlateBaseName, PrimitiveType.Cube);
        basePlate.transform.localPosition = Vector3.zero;
        basePlate.transform.localRotation = Quaternion.identity;
        basePlate.transform.localScale = plateSize;
        SetRendererMaterial(basePlate, brass);

        GameObject facePlate = FindOrCreatePrimitive(root, PlateFaceName, PrimitiveType.Cube);
        facePlate.transform.localPosition = new Vector3(0f, plateSize.y * 0.56f, 0f);
        facePlate.transform.localRotation = Quaternion.identity;
        facePlate.transform.localScale = new Vector3(plateSize.x * 0.88f, 0.007f, plateSize.z * 0.72f);
        SetRendererMaterial(facePlate, face);

        TextMeshPro nameText = FindOrCreateText(root, NameTextName);
        nameText.transform.localPosition = new Vector3(0f, plateSize.y * 0.66f, -0.004f);
        nameText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        nameText.transform.localScale = Vector3.one * 0.035f;
        nameText.text = doctorName;
        nameText.fontSize = nameFontSize;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = textColor;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;
        nameText.rectTransform.sizeDelta = new Vector2(8.2f, 1.15f);
        nameText.gameObject.SetActive(true);
        nameText.enabled = true;
        nameText.ForceMeshUpdate();

        Transform dirtRoot = FindOrCreateChild(root, DirtRootName);
        dirtRoot.gameObject.SetActive(showDirtAndWear);
        if (showDirtAndWear)
        {
            CreateDirtAndWear(dirtRoot, grime, scratch);
        }

        Transform bloodRoot = FindOrCreateChild(root, BloodRootName);
        bloodRoot.gameObject.SetActive(showBlood);
        if (showBlood)
        {
            CreateBloodDetails(bloodRoot, bloodOne, bloodTwo, right);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void CreateBloodDetails(Transform bloodRoot, Material darkBlood, Material wetBlood, Vector3 right)
    {
        CreateBloodDrop(bloodRoot, "Blood_Smear_Left", new Vector3(-0.19f, plateSize.y * 0.77f, 0.052f), new Vector3(0.06f, 0.0025f, 0.026f), 18f, wetBlood);
        CreateBloodDrop(bloodRoot, "Blood_Smear_Right", new Vector3(0.21f, plateSize.y * 0.775f, -0.048f), new Vector3(0.085f, 0.0025f, 0.018f), -12f, darkBlood);
        CreateBloodDrop(bloodRoot, "Blood_Fingerprint_1", new Vector3(-0.285f, plateSize.y * 0.78f, -0.03f), new Vector3(0.025f, 0.0022f, 0.011f), -24f, darkBlood);
        CreateBloodDrop(bloodRoot, "Blood_Fingerprint_2", new Vector3(-0.245f, plateSize.y * 0.781f, -0.052f), new Vector3(0.022f, 0.0022f, 0.009f), -18f, darkBlood);
        CreateBloodDrop(bloodRoot, "Blood_Drop_Center", new Vector3(0.04f, plateSize.y * 0.782f, 0.067f), new Vector3(0.023f, 0.0022f, 0.023f), 0f, wetBlood);
        CreateBloodStreak(bloodRoot, "Blood_Drag_Under_Text", new Vector3(0.095f, plateSize.y * 0.785f, 0.002f), new Vector3(0.22f, 0.002f, 0.011f), -4f, darkBlood);
        CreateBloodStreak(bloodRoot, "Blood_Thin_Drip_Front", new Vector3(0.295f, plateSize.y * 0.786f, 0.078f), new Vector3(0.018f, 0.002f, 0.09f), 2f, wetBlood);
        CreateBloodDrop(bloodRoot, "Blood_Old_Clot_Left_Edge", new Vector3(-0.335f, plateSize.y * 0.789f, 0.078f), new Vector3(0.033f, 0.0024f, 0.03f), 7f, darkBlood);
        CreateBloodDrop(bloodRoot, "Blood_Old_Clot_Right_Edge", new Vector3(0.327f, plateSize.y * 0.789f, -0.071f), new Vector3(0.028f, 0.0024f, 0.026f), -9f, darkBlood);
        CreateBloodStreak(bloodRoot, "Blood_Dried_Rim_Back", new Vector3(-0.045f, plateSize.y * 0.788f, -0.078f), new Vector3(0.34f, 0.002f, 0.008f), 1.5f, darkBlood);
        CreateBloodStreak(bloodRoot, "Blood_Wiped_Front_Right", new Vector3(0.205f, plateSize.y * 0.79f, 0.084f), new Vector3(0.17f, 0.002f, 0.013f), -18f, wetBlood);
    }

    private void CreateDirtAndWear(Transform dirtRoot, Material grime, Material scratch)
    {
        CreateWearPatch(dirtRoot, "Dust_Stain_Top_Left", new Vector3(-0.21f, plateSize.y * 0.772f, -0.053f), new Vector3(0.16f, 0.0018f, 0.028f), -8f, grime);
        CreateWearPatch(dirtRoot, "Dust_Stain_Bottom_Right", new Vector3(0.235f, plateSize.y * 0.773f, 0.055f), new Vector3(0.14f, 0.0018f, 0.022f), 11f, grime);
        CreateWearPatch(dirtRoot, "Old_Water_Ring", new Vector3(-0.025f, plateSize.y * 0.774f, 0.061f), new Vector3(0.055f, 0.0018f, 0.012f), 0f, grime);
        CreateWearPatch(dirtRoot, "Blackened_Left_Corner", new Vector3(-0.333f, plateSize.y * 0.775f, -0.075f), new Vector3(0.064f, 0.0018f, 0.024f), -16f, grime);
        CreateWearPatch(dirtRoot, "Blackened_Right_Corner", new Vector3(0.334f, plateSize.y * 0.775f, 0.075f), new Vector3(0.061f, 0.0018f, 0.023f), 17f, grime);

        CreateWearPatch(dirtRoot, "Scratch_Long_A", new Vector3(-0.12f, plateSize.y * 0.79f, -0.018f), new Vector3(0.23f, 0.0016f, 0.004f), 6f, scratch);
        CreateWearPatch(dirtRoot, "Scratch_Long_B", new Vector3(0.17f, plateSize.y * 0.791f, 0.032f), new Vector3(0.18f, 0.0016f, 0.0035f), -11f, scratch);
        CreateWearPatch(dirtRoot, "Scratch_Short_A", new Vector3(-0.305f, plateSize.y * 0.792f, 0.01f), new Vector3(0.075f, 0.0016f, 0.003f), 21f, scratch);
        CreateWearPatch(dirtRoot, "Scratch_Short_B", new Vector3(0.012f, plateSize.y * 0.793f, -0.066f), new Vector3(0.11f, 0.0016f, 0.003f), -23f, scratch);
        CreateWearPatch(dirtRoot, "Scratch_Nick_Center", new Vector3(0.004f, plateSize.y * 0.794f, 0.009f), new Vector3(0.038f, 0.0016f, 0.005f), 12f, scratch);
    }

    private void CreateWearPatch(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, float yRotation, Material material)
    {
        GameObject patch = FindOrCreatePrimitive(parent, objectName, PrimitiveType.Cube);
        patch.transform.localPosition = localPosition;
        patch.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        patch.transform.localScale = localScale;
        SetRendererMaterial(patch, material);
    }

    private void CreateBloodDrop(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, float yRotation, Material material)
    {
        GameObject drop = FindOrCreatePrimitive(parent, objectName, PrimitiveType.Cylinder);
        drop.transform.localPosition = localPosition;
        drop.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        drop.transform.localScale = localScale;
        SetRendererMaterial(drop, material);
    }

    private void CreateBloodStreak(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, float yRotation, Material material)
    {
        GameObject streak = FindOrCreatePrimitive(parent, objectName, PrimitiveType.Cube);
        streak.transform.localPosition = localPosition;
        streak.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        streak.transform.localScale = localScale;
        SetRendererMaterial(streak, material);
    }

    private bool TryResolveDeskBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Transform existingNameplateRoot = transform.Find(RootName);
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null || (existingNameplateRoot != null && target.transform.IsChildOf(existingNameplateRoot)))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = target.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(target.bounds);
            }
        }

        return hasBounds;
    }

    private Vector3 ResolveHorizontalForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        return forward.normalized;
    }

    private static float ProjectBoundsExtent(Bounds bounds, Vector3 axis)
    {
        axis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)).normalized;
        return bounds.extents.x * axis.x + bounds.extents.y * axis.y + bounds.extents.z * axis.z;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject created = new GameObject(childName);
        created.transform.SetParent(parent, true);
        return created.transform;
    }

    private static GameObject FindOrCreatePrimitive(Transform parent, string objectName, PrimitiveType primitiveType)
    {
        Transform existing = parent.Find(objectName);
        GameObject target = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitiveType);
        target.name = objectName;
        target.transform.SetParent(parent, false);
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediateSafe(collider);
        }

        return target;
    }

    private static TextMeshPro FindOrCreateText(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            TextMeshPro existingText = existing.GetComponent<TextMeshPro>();
            if (existingText != null)
            {
                return existingText;
            }
        }

        GameObject textObject = new GameObject(objectName, typeof(TextMeshPro));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshPro>();
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

public static class DoctorDeskNameplateBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureNameplate();
    }

    public static void EnsureNameplate()
    {
        Transform desk = FindDesk();
        if (desk == null)
        {
            return;
        }

        DoctorDeskNameplate nameplate = desk.GetComponent<DoctorDeskNameplate>();
        if (nameplate == null)
        {
            nameplate = desk.gameObject.AddComponent<DoctorDeskNameplate>();
        }

        nameplate.EnsureNameplate();
    }

    private static Transform FindDesk()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null)
            {
                continue;
            }

            string normalized = target.name.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
            if (normalized == "doctordesk" || normalized.Contains("officedesk"))
            {
                return target;
            }
        }

        return null;
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public static class DoctorDeskNameplateEditorInstaller
{
    static DoctorDeskNameplateEditorInstaller()
    {
        EditorApplication.delayCall += Install;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += Install;
    }

    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        DoctorDeskNameplateBootstrap.EnsureNameplate();
    }
}
#endif

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class XRayPuzzleSceneSetupTool
{
    private const string TargetScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string RootName = "XRayPuzzle";
    private const string ManagerName = "XRayPuzzleManager";

    private static readonly XRayData[] XRayDefinitions =
    {
        new XRayData(1, "XRay_01_Cervical", "Colonne cervicale", "Assets/Hospital/_Textures/XRayPuzzle/scientific-surgery-diagnosis-clinical-spine.jpg"),
        new XRayData(2, "XRay_02_Thorax", "Thorax / Poumons", "Assets/Hospital/_Textures/XRayPuzzle/female-chest-x-ray.jpg"),
        new XRayData(3, "XRay_03_Pied", "Pied / Cheville", "Assets/Hospital/_Textures/XRayPuzzle/body-man-surgery-patient-diagnosis.jpg"),
        new XRayData(4, "XRay_04_Lombaire", "Colonne lombaire", "Assets/Hospital/_Textures/XRayPuzzle/spine-x-ray.jpg"),
    };

    [MenuItem("Tools/Gameplay/Setup XRay Puzzle In DemoScene Moyen")]
    public static void SetupXRayPuzzleInDemoSceneMoyen()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[XRayPuzzleSceneSetupTool] Unable to open demoScene_Moyen.");
            return;
        }

        GameObject root = FindRoot(scene, RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        root.SetActive(true);

        GameObject keypad = FindRoot(scene, "SM_Pin_Pad");
        if (keypad == null)
        {
            Debug.LogWarning("[XRayPuzzleSceneSetupTool] SM_Pin_Pad not found. Place the keypad first.");
            return;
        }

        SetupXRayManager(scene);
        CreateXRays(root.transform, keypad.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[XRayPuzzleSceneSetupTool] XRay puzzle objects created in demoScene_Moyen.");
    }

    private static void SetupXRayManager(Scene scene)
    {
        GameObject managerObject = FindRoot(scene, ManagerName);
        if (managerObject == null)
        {
            managerObject = new GameObject(ManagerName);
            SceneManager.MoveGameObjectToScene(managerObject, scene);
        }

        XRayPuzzleManager manager = managerObject.GetComponent<XRayPuzzleManager>();
        if (manager == null)
        {
            manager = managerObject.AddComponent<XRayPuzzleManager>();
        }
    }

    // Places the radiographies on the inner wall of the padded room behind the keypad door.
    private static void CreateXRays(Transform parent, Transform keypadTransform)
    {
        // Visible on the right-side wall once the player enters the padded room.
        Vector3 basePosition = keypadTransform.position + new Vector3(-1.30f, 0.22f, -1.18f);
        Vector3 horizontalStep = new Vector3(0f, 0f, 0.52f);
        Vector3 verticalStep = new Vector3(0f, 0.58f, 0f);
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

        for (int i = 0; i < XRayDefinitions.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;
            Vector3 position = basePosition + horizontalStep * col - verticalStep * row;

            GameObject xrayObject = FindChild(parent, XRayDefinitions[i].ObjectName);
            if (xrayObject == null)
            {
                xrayObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                xrayObject.name = XRayDefinitions[i].ObjectName;
                xrayObject.transform.SetParent(parent, true);
            }

            xrayObject.transform.position = position;
            xrayObject.transform.rotation = rotation;
            xrayObject.transform.localScale = new Vector3(0.24f, 0.34f, 1f);

            EnsureCollider(xrayObject);
            ApplyTextureToRenderer(xrayObject, XRayDefinitions[i].TexturePath);
            ConfigureXRayComponent(xrayObject, XRayDefinitions[i]);
        }
    }

    private static void ConfigureXRayComponent(GameObject xrayObject, XRayData data)
    {
        XRayItem xrayItem = xrayObject.GetComponent<XRayItem>();
        if (xrayItem == null)
        {
            xrayItem = xrayObject.AddComponent<XRayItem>();
        }

        xrayItem.xrayID = data.XRayId;
        xrayItem.bodyPartName = data.BodyPartName;
        xrayItem.correctPatientID = data.XRayId;
        xrayItem.isSolved = false;
        xrayItem.xraySprite = LoadSprite(data.TexturePath);
        xrayItem.ResetVisual();
    }

    private static void EnsureCollider(GameObject target)
    {
        Collider itemCollider = target.GetComponent<Collider>();
        if (itemCollider == null)
        {
            itemCollider = target.AddComponent<BoxCollider>();
        }

        itemCollider.isTrigger = false;
    }

    private static void ApplyTextureToRenderer(GameObject target, string texturePath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning($"[XRayPuzzleSceneSetupTool] Missing texture at {texturePath}.");
            return;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            material = new Material(shader);
            renderer.sharedMaterial = material;
        }

        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", new Color(0.95f, 0.98f, 1f, 1f));
        }

        if (material.HasProperty("_Color"))
        {
            material.color = new Color(0.95f, 0.98f, 1f, 1f);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 0.2f, 0.24f, 1f));
        }
    }

    private static Sprite LoadSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private static GameObject FindRoot(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
            {
                return root;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private readonly struct XRayData
    {
        public readonly int XRayId;
        public readonly string ObjectName;
        public readonly string BodyPartName;
        public readonly string TexturePath;

        public XRayData(int xrayId, string objectName, string bodyPartName, string texturePath)
        {
            XRayId = xrayId;
            ObjectName = objectName;
            BodyPartName = bodyPartName;
            TexturePath = texturePath;
        }
    }
}

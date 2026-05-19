using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PatientFolderSceneSetup
{
    private const string TargetScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string PaperMaterialPath = "Assets/Hospital/_Materials/PatientFolderPaper.mat";
    private const string UiRootName = "PatientUIRoot";
    private const string UiPanelName = "PatientPanel";
    private const string UiTextName = "PatientInfoText";

    [MenuItem("Tools/Gameplay/Setup Patient Folders In DemoScene Moyen")]
    public static void SetupPatientFoldersInDemoSceneMoyen()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] Unable to open demoScene_Moyen.");
            return;
        }

        Material paperMaterial = GetOrCreatePaperMaterial();
        if (paperMaterial == null)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] Unable to create paper material.");
            return;
        }

        EnsurePatientUiManager(scene);

        GameObject root = FindOrCreateRoot(scene, "PatientFolders");
        GameObject keypad = FindRoot(scene, "SM_Pin_Pad");
        if (keypad != null)
        {
            EnsurePatientTable(root.transform, keypad.transform);
        }

        PatientData[] patients =
        {
            new PatientData(1, "Sarah M.", 67, "Amnesie traumatique severe", "Patiente retrouvee inconsciente\ndans le couloir du sous-sol.\nAucun souvenir des 3 derniers mois."),
            new PatientData(2, "Adam K.", 45, "Contusion pulmonaire grave", "Retrouve dans la chambre 12.\nRespiration difficile.\nCause du traumatisme inconnue."),
            new PatientData(3, "Elena R.", 30, "Fracture complexe du pied", "Patiente admise en urgence.\nImpossibilite de marcher.\nRefuse de parler de l'incident."),
            new PatientData(4, "Marcus T.", 55, "Hernie discale L4-L5", "Douleurs lombaires extremes.\nPatient agite et violent.\nTransfert demande."),
        };

        PlacementData[] placements = ResolvePlacements(keypad != null ? keypad.transform : null);

        for (int i = 0; i < patients.Length; i++)
        {
            CreateOrUpdateFolder(root.transform, patients[i], placements[i], paperMaterial);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PatientFolderSceneSetup] Patient folders created in demoScene_Moyen.");
    }

    private static void CreateOrUpdateFolder(Transform parent, PatientData data, PlacementData placement, Material paperMaterial)
    {
        string objectName = $"Dossier_Patient{data.PatientId:00}";
        GameObject folder = FindChildByName(parent, objectName);

        if (folder == null)
        {
            folder = GameObject.CreatePrimitive(PrimitiveType.Quad);
            folder.name = objectName;
            folder.transform.SetParent(parent, true);
        }

        folder.transform.position = placement.Position;
        folder.transform.rotation = placement.Rotation;
        folder.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);

        MeshRenderer renderer = folder.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = paperMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        EnsureFolderCollider(folder);

        PatientFolder patientFolder = folder.GetComponent<PatientFolder>();
        if (patientFolder == null)
        {
            patientFolder = folder.AddComponent<PatientFolder>();
        }

        patientFolder.patientID = data.PatientId;
        patientFolder.patientName = data.PatientName;
        patientFolder.patientAge = data.PatientAge;
        patientFolder.disease = data.Disease;
        patientFolder.medicalNotes = data.Notes;
        patientFolder.isRead = false;

        EnsureFolderLabel(folder.transform, data);

        Debug.Log($"[PatientFolderSceneSetup] {objectName} => {data.PatientName}, pos {placement.Position}");
    }

    private static void EnsurePatientTable(Transform parent, Transform keypadTransform)
    {
        GameObject tableRoot = FindChildByName(parent, "PatientTable");
        if (tableRoot == null)
        {
            tableRoot = new GameObject("PatientTable");
            tableRoot.transform.SetParent(parent, true);
        }

        // Keep the patient table clearly inside the padded room, just after the keypad door.
        Vector3 tableCenter = keypadTransform.position + new Vector3(-1.90f, -0.96f, -0.02f);

        tableRoot.transform.position = tableCenter;
        tableRoot.transform.rotation = Quaternion.identity;
        tableRoot.transform.localScale = Vector3.one;

        CreateOrUpdateTablePart(tableRoot.transform, "TableTop", new Vector3(0f, 0.62f, 0f), new Vector3(0.58f, 0.05f, 0.42f));
        CreateOrUpdateTablePart(tableRoot.transform, "Leg_A", new Vector3(-0.22f, 0.31f, -0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(tableRoot.transform, "Leg_B", new Vector3(0.22f, 0.31f, -0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(tableRoot.transform, "Leg_C", new Vector3(-0.22f, 0.31f, 0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(tableRoot.transform, "Leg_D", new Vector3(0.22f, 0.31f, 0.15f), new Vector3(0.05f, 0.62f, 0.05f));
    }

    private static void CreateOrUpdateTablePart(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = FindChildByName(parent, objectName);
        if (part == null)
        {
            part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = objectName;
            part.transform.SetParent(parent, false);
        }

        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
        {
            renderer.sharedMaterial.color = new Color(0.35f, 0.28f, 0.22f, 1f);
        }
    }

    private static void EnsurePatientUiManager(Scene scene)
    {
        PatientUIManager existingManager = Object.FindObjectOfType<PatientUIManager>(true);
        if (existingManager != null)
        {
            ConfigureExistingUi(existingManager);
            return;
        }

        GameObject root = new GameObject(UiRootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject(UiPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 240f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.03f, 0.03f, 0.82f);

        GameObject textObject = new GameObject(UiTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 20f);
        textRect.offsetMax = new Vector2(-22f, -20f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 20f;
        text.color = new Color(0.96f, 0.92f, 0.83f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.text = "DOSSIER MEDICAL";

        PatientUIManager manager = root.AddComponent<PatientUIManager>();
        SetPrivateField(manager, "rootCanvas", canvas);
        SetPrivateField(manager, "patientPanel", panel);
        SetPrivateField(manager, "patientInfoText", text);

        panel.SetActive(false);
        Debug.Log("[PatientFolderSceneSetup] Patient UI created.");
    }

    private static void ConfigureExistingUi(PatientUIManager manager)
    {
        Canvas canvas = manager.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
        }

        Transform panelTransform = manager.transform.Find(UiPanelName);
        GameObject panel = panelTransform != null ? panelTransform.gameObject : FindNamedChild(manager.gameObject, UiPanelName);
        if (panel == null)
        {
            panel = CreatePanelUnderRoot(manager.gameObject);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(460f, 240f);
            panelRect.anchoredPosition = Vector2.zero;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panel.AddComponent<Image>();
        }

        panelImage.color = new Color(0.03f, 0.03f, 0.03f, 0.82f);

        TMP_Text text = panel.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            text = CreateTextUnderPanel(panel);
        }

        if (text != null)
        {
            text.fontSize = 20f;
            text.color = new Color(0.96f, 0.92f, 0.83f, 1f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform textRect = text.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(22f, 20f);
                textRect.offsetMax = new Vector2(-22f, -20f);
            }
        }

        SetPrivateField(manager, "rootCanvas", canvas);
        SetPrivateField(manager, "patientPanel", panel);
        SetPrivateField(manager, "patientInfoText", text);
        panel.SetActive(false);
    }

    // Places the patient files directly on the padded-room floor behind the keypad door.
    private static PlacementData[] ResolvePlacements(Transform keypadTransform)
    {
        if (keypadTransform != null)
        {
            // Put the folders on the table inside the room, not in the corridor.
            Vector3 basePosition = keypadTransform.position + new Vector3(-1.90f, -0.36f, -0.02f);
            return new[]
            {
                new PlacementData(basePosition + new Vector3(-0.10f, 0f, -0.08f), Quaternion.Euler(90f, -8f, 0f)),
                new PlacementData(basePosition + new Vector3(0.10f, 0f, -0.06f), Quaternion.Euler(90f, 9f, 0f)),
                new PlacementData(basePosition + new Vector3(-0.08f, 0f, 0.08f), Quaternion.Euler(90f, -4f, 0f)),
                new PlacementData(basePosition + new Vector3(0.10f, 0f, 0.08f), Quaternion.Euler(90f, 6f, 0f)),
            };
        }

        return new[]
        {
            new PlacementData(new Vector3(0.34f, -6.26f, -3.60f), Quaternion.Euler(90f, 0f, 0f)),
            new PlacementData(new Vector3(0.78f, -6.26f, -3.60f), Quaternion.Euler(90f, 14f, 0f)),
            new PlacementData(new Vector3(0.42f, -6.26f, -3.18f), Quaternion.Euler(90f, -12f, 0f)),
            new PlacementData(new Vector3(0.84f, -6.26f, -3.22f), Quaternion.Euler(90f, 7f, 0f)),
        };
    }

    private static Material GetOrCreatePaperMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(PaperMaterialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        material = new Material(shader);
        material.name = "PatientFolderPaper";
        material.color = new Color(0.84f, 0.78f, 0.62f, 1f);

        AssetDatabase.CreateAsset(material, PaperMaterialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return material;
    }

    private static GameObject FindOrCreateRoot(Scene scene, string rootName)
    {
        GameObject existing = FindRoot(scene, rootName);
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(rootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
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

    private static GameObject FindChildByName(Transform parent, string childName)
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

    private static void EnsureFolderCollider(GameObject folder)
    {
        Collider[] colliders = folder.GetComponents<Collider>();
        foreach (Collider existingCollider in colliders)
        {
            if (!(existingCollider is BoxCollider))
            {
                Object.DestroyImmediate(existingCollider);
            }
        }

        BoxCollider boxCollider = folder.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = folder.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = false;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(0.78f, 0.08f, 0.78f);
    }

    private static GameObject FindNamedChild(GameObject root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static GameObject CreatePanelUnderRoot(GameObject root)
    {
        GameObject panel = new GameObject(UiPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        return panel;
    }

    private static TMP_Text CreateTextUnderPanel(GameObject panel)
    {
        GameObject textObject = new GameObject(UiTextName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private static void EnsureFolderLabel(Transform folderTransform, PatientData data)
    {
        Transform labelTransform = folderTransform.Find("FolderLabel");
        GameObject labelObject = labelTransform != null ? labelTransform.gameObject : new GameObject("FolderLabel");
        labelObject.transform.SetParent(folderTransform, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.18f, -0.015f);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.03f;

        TextMesh label = labelObject.GetComponent<TextMesh>();
        if (label == null)
        {
            label = labelObject.AddComponent<TextMesh>();
        }

        label.text = $"Dossier {data.PatientId:00}";
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.18f;
        label.fontSize = 28;
        label.color = new Color(0.32f, 0.12f, 0.05f, 1f);
    }

    private static void SetPrivateField(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning($"[PatientFolderSceneSetup] Missing serialized field '{fieldName}'.");
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct PatientData
    {
        public readonly int PatientId;
        public readonly string PatientName;
        public readonly int PatientAge;
        public readonly string Disease;
        public readonly string Notes;

        public PatientData(int patientId, string patientName, int patientAge, string disease, string notes)
        {
            PatientId = patientId;
            PatientName = patientName;
            PatientAge = patientAge;
            Disease = disease;
            Notes = notes;
        }
    }

    private readonly struct PlacementData
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public PlacementData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}

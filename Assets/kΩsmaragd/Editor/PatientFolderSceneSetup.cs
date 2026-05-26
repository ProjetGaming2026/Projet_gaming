using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PatientFolderSceneSetup
{
    private const string TargetScenePath = "Assets/Hospital/scenes/demoScene_Moyen.unity";
    private const string PaperMaterialPath = "Assets/Hospital/_Materials/PatientFolderPaper.mat";
    private const string TablePrefabPath = "Assets/Hospital/_Prefabs/furniture/table.prefab";
    // Tuned to place the table clearly inside the padded room on the left side after the
    // keypad door opens, so the player sees it as soon as they enter the chamber.
    private static readonly Vector3 TableRoomOffset = new Vector3(-1.25f, 0f, 0.18f);
    private const float RoomFloorOffsetFromKeypad = 1.02f;

    [MenuItem("Tools/Gameplay/Setup Patient Folders In DemoScene Moyen")]
    public static void SetupPatientFoldersInDemoSceneMoyen()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] Unable to open demoScene_Moyen.");
            return;
        }

        GameObject keypad = FindRoot(scene, "SM_Pin_Pad");
        if (keypad == null)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] SM_Pin_Pad not found in scene.");
            return;
        }

        Material paperMaterial = GetOrCreatePaperMaterial();
        if (paperMaterial == null)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] Unable to create paper material.");
            return;
        }

        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
        if (tablePrefab == null)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] table.prefab not found.");
            return;
        }

        foreach (GameObject existingTable in FindAllGameObjectsInScene(scene, "PatientFilesTable"))
        {
            Object.DestroyImmediate(existingTable);
        }

        DeleteOrphanFolders(scene);
        DeleteLoosePaperQuads(scene);

        GameObject tableInstance = CreateSceneTable(scene, keypad.transform.position, tablePrefab);
        if (tableInstance == null)
        {
            Debug.LogWarning("[PatientFolderSceneSetup] Unable to instantiate table prefab.");
            return;
        }

        ConfigureTableInteraction(tableInstance);
        BuildFoldersOnTable(scene, tableInstance, paperMaterial);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PatientFolderSceneSetup] PatientFilesTable and folders placed in demoScene_Moyen.");
    }

    private static void ConfigureTableInteraction(GameObject tableInstance)
    {
        Bounds bounds = CalculateBounds(tableInstance);
        BoxCollider collider = tableInstance.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = tableInstance.AddComponent<BoxCollider>();
        }

        collider.center = tableInstance.transform.InverseTransformPoint(bounds.center);
        collider.size = new Vector3(bounds.size.x + 0.10f, bounds.size.y + 0.12f, bounds.size.z + 0.10f);
        collider.isTrigger = false;

        PatientFilesTableInteraction interaction = tableInstance.GetComponent<PatientFilesTableInteraction>();
        if (interaction == null)
        {
            tableInstance.AddComponent<PatientFilesTableInteraction>();
        }
    }

    private static GameObject CreateSceneTable(Scene scene, Vector3 keypadPosition, GameObject tablePrefab)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(tablePrefab, scene) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = "PatientFilesTable";
        instance.transform.position = keypadPosition + TableRoomOffset;
        instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        instance.transform.localScale = Vector3.one;

        Bounds bounds = CalculateBounds(instance);
        float desiredFloorY = keypadPosition.y - RoomFloorOffsetFromKeypad;
        instance.transform.position += Vector3.up * (desiredFloorY - bounds.min.y);
        return instance;
    }

    private static void BuildFoldersOnTable(Scene scene, GameObject tableInstance, Material paperMaterial)
    {
        foreach (GameObject existingRoot in FindAllGameObjectsInScene(scene, "PatientFolders"))
        {
            Object.DestroyImmediate(existingRoot);
        }

        GameObject root = new GameObject("PatientFolders");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.SetParent(tableInstance.transform, false);

        PatientData[] patients =
        {
            new PatientData(1, "Patient A-17", 0, "respiration difficile", "Zone : cage thoracique\nOrdre : 1"),
            new PatientData(2, "Patient B-09", 0, "incapacite de marcher", "Zone : pied\nOrdre : 2"),
            new PatientData(3, "Patient C-31", 0, "perte de memoire", "Zone : crane\nOrdre : 3"),
            new PatientData(4, "Patient D-44", 0, "dos brise", "Zone : colonne vertebrale\nOrdre : 4"),
        };

        Bounds tableBounds = CalculateBounds(tableInstance);
        Vector3 localTopCenter = tableInstance.transform.InverseTransformPoint(new Vector3(tableBounds.center.x, tableBounds.max.y + 0.008f, tableBounds.center.z));
        Vector3[] localOffsets =
        {
            new Vector3(-0.16f, 0f, -0.11f),
            new Vector3(0.12f, 0f, -0.05f),
            new Vector3(-0.04f, 0f, 0.10f),
            new Vector3(0.18f, 0f, 0.14f),
        };

        float[] yRotations = { -14f, 9f, -6f, 12f };
        string[] folderNames =
        {
            "Dossier_Patient01",
            "Dossier_Patient02",
            "Dossier_Patient03",
            "Dossier_Patient04",
        };

        for (int i = 0; i < patients.Length; i++)
        {
            GameObject folder = GameObject.CreatePrimitive(PrimitiveType.Quad);
            folder.name = folderNames[i];
            folder.transform.SetParent(root.transform, false);
            folder.transform.localPosition = localTopCenter + localOffsets[i];
            folder.transform.localRotation = Quaternion.Euler(90f, yRotations[i], 0f);
            folder.transform.localScale = new Vector3(0.28f, 0.38f, 0.28f);

            MeshRenderer renderer = folder.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = paperMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Collider folderCollider = folder.GetComponent<Collider>();
            if (folderCollider != null)
            {
                Object.DestroyImmediate(folderCollider);
            }

            PatientFolder patientFolder = folder.AddComponent<PatientFolder>();
            patientFolder.patientID = patients[i].PatientId;
            patientFolder.patientName = patients[i].PatientName;
            patientFolder.patientAge = patients[i].PatientAge;
            patientFolder.disease = patients[i].Disease;
            patientFolder.medicalNotes = patients[i].Notes;
            patientFolder.isRead = false;
        }
    }

    private static Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
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

    private static void DeleteOrphanFolders(Scene scene)
    {
        string[] names =
        {
            "PatientFolders",
            "Dossier_Patient01",
            "Dossier_Patient02",
            "Dossier_Patient03",
            "Dossier_Patient04",
        };

        foreach (string objectName in names)
        {
            foreach (GameObject target in FindAllGameObjectsInScene(scene, objectName))
            {
                Object.DestroyImmediate(target);
            }
        }
    }

    private static void DeleteLoosePaperQuads(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            DeleteLoosePaperQuadsRecursive(root.transform);
        }
    }

    private static void DeleteLoosePaperQuadsRecursive(Transform current)
    {
        for (int i = current.childCount - 1; i >= 0; i--)
        {
            DeleteLoosePaperQuadsRecursive(current.GetChild(i));
        }

        if (current.name == "PatientFilesTable" || current.IsChildOf(FindTransformByName(current.root, "PatientFilesTable")))
        {
            return;
        }

        MeshRenderer renderer = current.GetComponent<MeshRenderer>();
        MeshFilter filter = current.GetComponent<MeshFilter>();
        if (renderer == null || filter == null || filter.sharedMesh == null)
        {
            return;
        }

        if (!filter.sharedMesh.name.ToLowerInvariant().Contains("quad"))
        {
            return;
        }

        Material sharedMaterial = renderer.sharedMaterial;
        if (sharedMaterial == null)
        {
            return;
        }

        if (sharedMaterial.name.Contains("PatientFolderPaper"))
        {
            Object.DestroyImmediate(current.gameObject);
        }
    }

    private static Transform FindTransformByName(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindTransformByName(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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

    private static GameObject FindGameObjectInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindRecursive(root.transform, objectName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static System.Collections.Generic.List<GameObject> FindAllGameObjectsInScene(Scene scene, string objectName)
    {
        var matches = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            FindAllRecursive(root.transform, objectName, matches);
        }

        return matches;
    }

    private static Transform FindRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindRecursive(current.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void FindAllRecursive(Transform current, string objectName, System.Collections.Generic.List<GameObject> matches)
    {
        if (current.name == objectName)
        {
            matches.Add(current.gameObject);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            FindAllRecursive(current.GetChild(i), objectName, matches);
        }
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
}

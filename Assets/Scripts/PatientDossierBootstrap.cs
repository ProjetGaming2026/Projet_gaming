using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PatientDossierBootstrap
{
    private const string MediumSceneName = "demoScene_Moyen";
    private const string ArchiveRootName = "Patient_Dossier_Archive_V4";
    private const string LegacyArchiveRootName = "Patient_Dossier_Archive";
    private const float FolderWidth = 0.46f;
    private const float FolderDepth = 0.32f;

    private static readonly Dictionary<string, PlacementPose> FixedPlacementsByPatient = new Dictionary<string, PlacementPose>
    {
        { "ELIAS MORVAN", new PlacementPose(new Vector3(17.63f, -6.91f, -10.30f), Quaternion.Euler(0f, 6f, 0f)) },
        { "SARAH BIDAULT", new PlacementPose(new Vector3(-5.51f, 0.09f, 16.91f), Quaternion.Euler(0f, -18f, 0f)) },
        { "MARA KESSEL", new PlacementPose(new Vector3(20.58f, 0.10f, 14.22f), Quaternion.Euler(0f, 12f, 0f)) },
        { "ADRIEN VOSS", new PlacementPose(new Vector3(-1.57f, 1.07f, 0.99f), Quaternion.Euler(0f, -8f, 0f)) },
        { "NIKOLAS VARGA", new PlacementPose(new Vector3(8.54f, -6.89f, 1.02f), Quaternion.Euler(0f, 25f, 0f)) },
        { "LINA ORTEGA", new PlacementPose(new Vector3(-12.45f, 0.80f, 1.44f), Quaternion.Euler(0f, -30f, 0f)) },
        { "HECTOR SALIM", new PlacementPose(new Vector3(22.00f, 0.14f, 8.93f), Quaternion.Euler(0f, 18f, 0f)) },
        { "IRIS DUMONT", new PlacementPose(new Vector3(-1.50f, 1.50f, 14.29f), Quaternion.Euler(0f, -6f, 0f)) },
        { "NOAH KLINE", new PlacementPose(new Vector3(-7.11f, 1.08f, 9.39f), Quaternion.Euler(0f, -22f, 0f)) }
    };

    private static readonly Vector3[] FallbackPositions =
    {
        new Vector3(-0.9f, -6.06f, 3.35f),
        new Vector3(0.05f, -6.08f, 2.35f),
        new Vector3(1.2f, -6.05f, 3.15f),
        new Vector3(5.9f, -1.0f, -3.0f),
        new Vector3(4.4f, -1.0f, -1.7f),
        new Vector3(2.8f, -1.0f, -4.2f),
        new Vector3(-2.5f, -6.05f, 5.2f),
        new Vector3(2.9f, -6.05f, 6.2f),
        new Vector3(-3.4f, -6.05f, 1.2f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ConfigureScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (!IsTelevisionPuzzleScene(scene))
        {
            RemoveLegacyArchives(scene);
            return;
        }

        if (FindSceneObjectByName(scene, "room5") == null && FindSceneObjectByName(scene, "TV_Screen") == null)
        {
            return;
        }

        RemoveLegacyArchives(scene);
        if (FindSceneObjectByName(scene, ArchiveRootName) != null)
        {
            return;
        }

        PatientRecord[] records = CreateRecords();
        PlacementPose[] placements = ResolveArchivePlacements(scene, records);
        CreatePatientArchive(records, placements);
    }

    private static void CreatePatientArchive(PatientRecord[] records, PlacementPose[] placements)
    {
        GameObject root = new GameObject(ArchiveRootName);

        Material paperMaterial = CreateDirtyMaterial("Runtime_Dossier_Paper_Dirty", new Color(0.63f, 0.57f, 0.43f, 1f), new Color(0.22f, 0.15f, 0.08f, 1f), 441, 0.36f);
        Material darkMarkMaterial = CreateDirtyMaterial("Runtime_Dossier_Dark_Ink", new Color(0.08f, 0.045f, 0.025f, 1f), new Color(0.015f, 0.01f, 0.006f, 1f), 909, 0.5f);

        for (int i = 0; i < records.Length; i++)
        {
            PatientRecord record = records[i];
            PlacementPose placement = placements[Mathf.Min(i, placements.Length - 1)];
            Material folderMaterial = CreateDirtyMaterial(
                "Runtime_Dossier_Folder_" + i,
                record.IsCritical ? new Color(0.34f, 0.16f, 0.1f, 1f) : new Color(0.48f, 0.38f, 0.22f, 1f),
                new Color(0.15f, 0.09f, 0.045f, 1f),
                1200 + i * 37,
                record.IsCritical ? 0.52f : 0.42f);
            Material tabMaterial = CreateDirtyMaterial(
                "Runtime_Dossier_Tab_" + i,
                record.IsCritical ? new Color(0.45f, 0.055f, 0.035f, 1f) : new Color(0.58f, 0.45f, 0.25f, 1f),
                new Color(0.16f, 0.08f, 0.04f, 1f),
                2200 + i * 41,
                0.45f);

            GameObject folder = CreateFolderObject(record, folderMaterial, paperMaterial, tabMaterial, darkMarkMaterial, i);
            folder.transform.SetParent(root.transform, true);
            folder.transform.SetPositionAndRotation(placement.Position, placement.Rotation);

            Debug.Log(
                "PatientDossierBootstrap: placed " + folder.name +
                " at " + FormatVector(placement.Position) +
                " rotY " + placement.Rotation.eulerAngles.y.ToString("F1"));
        }

        Debug.Log("PatientDossierBootstrap: created " + records.Length + " dirty patient dossiers at fixed player-chosen placements. Nikolas ID/code is 739412.");
    }

    private static GameObject CreateFolderObject(
        PatientRecord record,
        Material folderMaterial,
        Material paperMaterial,
        Material tabMaterial,
        Material darkMarkMaterial,
        int index)
    {
        GameObject folder = new GameObject("Patient_Dossier_" + SanitizeName(record.PatientName));

        GameObject cover = CreatePart("Folder_Cover", folder.transform, new Vector3(0f, 0.014f, 0f), new Vector3(FolderWidth, 0.018f, FolderDepth), folderMaterial);
        GameObject backCover = CreatePart("Folder_Back_Cover", folder.transform, new Vector3(0.01f, 0.002f, 0.012f), new Vector3(FolderWidth * 0.96f, 0.012f, FolderDepth * 0.92f), folderMaterial);
        GameObject paperA = CreatePart("Paper_Edge_A", folder.transform, new Vector3(0.018f, 0.031f, -0.012f), new Vector3(FolderWidth * 0.86f, 0.008f, FolderDepth * 0.82f), paperMaterial);
        GameObject paperB = CreatePart("Paper_Edge_B", folder.transform, new Vector3(-0.014f, 0.041f, 0.006f), new Vector3(FolderWidth * 0.78f, 0.007f, FolderDepth * 0.72f), paperMaterial);
        GameObject frontLip = CreatePart("Folder_Front_Lip", folder.transform, new Vector3(0f, 0.052f, -FolderDepth * 0.43f), new Vector3(FolderWidth, 0.026f, 0.037f), folderMaterial);
        GameObject tab = CreatePart("Folder_Tab", folder.transform, new Vector3(-FolderWidth * 0.24f, 0.059f, FolderDepth * 0.39f), new Vector3(FolderWidth * 0.36f, 0.025f, FolderDepth * 0.17f), tabMaterial);
        GameObject mark = CreatePart("Archive_Number_Mark", folder.transform, new Vector3(FolderWidth * 0.23f, 0.067f, -FolderDepth * 0.25f), new Vector3(FolderWidth * 0.16f, 0.006f, FolderDepth * 0.035f), darkMarkMaterial);

        cover.transform.localRotation = Quaternion.Euler(0f, index % 2 == 0 ? 1.2f : -1.5f, 0f);
        backCover.transform.localRotation = Quaternion.Euler(0f, index % 3 == 0 ? -0.7f : 0.8f, 0f);
        paperA.transform.localRotation = Quaternion.Euler(0f, 0.7f + index * 0.2f, 0f);
        paperB.transform.localRotation = Quaternion.Euler(0f, -0.9f - index * 0.15f, 0f);
        tab.transform.localRotation = Quaternion.Euler(0f, index % 2 == 0 ? -1.8f : 1.4f, 0f);
        mark.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);

        CreateWorldLabel(folder.transform, record);
        AddDirtDetails(folder.transform, darkMarkMaterial, index);
        DisableChildColliders(folder);

        BoxCollider interactCollider = folder.AddComponent<BoxCollider>();
        interactCollider.isTrigger = false;
        interactCollider.center = new Vector3(0f, 0.08f, 0f);
        interactCollider.size = new Vector3(0.66f, 0.24f, 0.5f);

        PatientDossier dossier = folder.AddComponent<PatientDossier>();
        dossier.Configure(
            record.PatientName,
            record.PatientId,
            record.Diagnosis,
            record.RiskLevel,
            record.Observations,
            record.Treatment,
            record.AdministrativeNote,
            record.IsCritical);

        return folder;
    }

    private static GameObject CreatePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static void AddDirtDetails(Transform parent, Material darkMaterial, int seed)
    {
        for (int i = 0; i < 5; i++)
        {
            float x = Mathf.Lerp(-FolderWidth * 0.36f, FolderWidth * 0.36f, PseudoRandom(seed, i * 2));
            float z = Mathf.Lerp(-FolderDepth * 0.35f, FolderDepth * 0.35f, PseudoRandom(seed, i * 2 + 1));
            float width = Mathf.Lerp(0.035f, 0.12f, PseudoRandom(seed + 19, i));
            float depth = Mathf.Lerp(0.006f, 0.018f, PseudoRandom(seed + 29, i));

            GameObject stain = CreatePart("Dirt_Smear_" + i, parent, new Vector3(x, 0.073f + i * 0.001f, z), new Vector3(width, 0.004f, depth), darkMaterial);
            stain.transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-35f, 35f, PseudoRandom(seed + 43, i)), 0f);
        }
    }

    private static void CreateWorldLabel(Transform parent, PatientRecord record)
    {
        GameObject labelObject = new GameObject("Folder_Label", typeof(TextMeshPro));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0.015f, 0.081f, 0.012f);
        labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.052f;

        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = record.PatientName + "\n#" + record.PatientId;
        label.fontSize = 2.25f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = record.IsCritical ? new Color(0.35f, 0.01f, 0.005f, 1f) : new Color(0.08f, 0.055f, 0.035f, 1f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(5.4f, 1.4f);
    }

    private static void DisableChildColliders(GameObject folder)
    {
        Collider[] colliders = folder.GetComponentsInChildren<Collider>(true);
        foreach (Collider childCollider in colliders)
        {
            childCollider.enabled = false;
        }
    }

    private static PatientRecord[] CreateRecords()
    {
        return new[]
        {
            new PatientRecord(
                "ELIAS MORVAN",
                "284913",
                "Trouble paranoide avec idees de surveillance persistantes.",
                "Modere",
                "Refuse les entretiens apres extinction des lumieres. Reagit fortement aux sons metalliques dans le couloir.",
                "Stabilisation, isolement court, evaluation nocturne.",
                "Archive transferee vers l'aile B. Aucun code de sortie associe.",
                false),
            new PatientRecord(
                "SARAH BIDAULT",
                "581047",
                "Episode depressif majeur avec dissociation post-traumatique.",
                "Surveillance",
                "Retrouvee plusieurs fois pres des lits vides. Repete qu'un nom est cache sur les murs.",
                "Sedation legere, therapie d'ancrage, suivi du sommeil.",
                "Dossier incomplet. Voir carnet patient pour contexte.",
                false),
            new PatientRecord(
                "MARA KESSEL",
                "920614",
                "Catatonie intermittente et mutisme selectif.",
                "Faible",
                "Ne communique que par dessins de portes fermees. Les dessins indiquent toujours six marques.",
                "Observation sans contrainte, environnement calme.",
                "Aucun lien avec le verrou de sortie.",
                false),
            new PatientRecord(
                "ADRIEN VOSS",
                "463208",
                "Psychose breve reactive, hallucinations auditives episodiques.",
                "Eleve",
                "Declare entendre une television allumee meme lorsque l'appareil est eteint.",
                "Antipsychotique faible dose, retrait des stimuli sonores.",
                "Controle administratif negatif.",
                false),
            new PatientRecord(
                "NIKOLAS VARGA",
                "739412",
                "Trouble dissociatif avec delires de persecution et episodes amnesiques.",
                "Critique",
                "Le patient reagit a une lumiere dirigee sur les murs. Il a grave son prenom dans la chambre avant son transfert.",
                "Contention levee sous surveillance. Acces aux archives bloque apres incident du secteur M.",
                "IDENTIFIANT ADMINISTRATIF : 739412. Ce numero autorise l'ouverture du controle de sortie.",
                true),
            new PatientRecord(
                "LINA ORTEGA",
                "157839",
                "Trouble panique severe avec agoraphobie hospitaliere.",
                "Faible",
                "Se calme uniquement lorsque toutes les portes restent visibles.",
                "Respiration guidee, exposition progressive.",
                "ID archive seulement. Ne pas utiliser pour la sortie.",
                false),
            new PatientRecord(
                "HECTOR SALIM",
                "806341",
                "Amnesie dissociative recurrente.",
                "Modere",
                "Oublie son propre nom mais recite des suites de chiffres sans signification clinique.",
                "Reorientation, carnet de memoire, repos.",
                "Numeros recites non valides pour le systeme de sortie.",
                false),
            new PatientRecord(
                "IRIS DUMONT",
                "392750",
                "Stress post-traumatique complexe avec hypervigilance.",
                "Surveillance",
                "Signale une presence derriere les meubles. Evite les zones sombres.",
                "Suivi anxiolytique, chambre eclairee.",
                "Archive consultee par le service de nuit.",
                false),
            new PatientRecord(
                "NOAH KLINE",
                "648205",
                "Trouble obsessionnel-compulsif centre sur les serrures et combinaisons.",
                "Modere",
                "Verifie chaque code trois fois. Cache parfois les cles dans des dossiers voisins.",
                "Rituels limites, therapie comportementale.",
                "Aucun code de sortie valide dans ce dossier.",
                false)
        };
    }

    private static PlacementPose[] ResolveArchivePlacements(Scene scene, PatientRecord[] records)
    {
        PlacementPose[] placements = new PlacementPose[records.Length];
        List<PlacementPose> fallbackPlacements = null;

        for (int i = 0; i < records.Length; i++)
        {
            if (FixedPlacementsByPatient.TryGetValue(records[i].PatientName, out PlacementPose fixedPlacement))
            {
                placements[i] = fixedPlacement;
                continue;
            }

            if (fallbackPlacements == null)
            {
                List<PlacementPose> candidates = CollectPlacementSurfaces(scene);
                fallbackPlacements = SelectSpacedPlacements(candidates, records.Length);
            }

            if (i < fallbackPlacements.Count)
            {
                placements[i] = fallbackPlacements[i];
                continue;
            }

            Vector3 position = FallbackPositions[i % FallbackPositions.Length] + Vector3.up * 0.08f;
            Quaternion rotation = Quaternion.Euler(0f, 20f + i * 37f, 0f);
            placements[i] = new PlacementPose(position, rotation);
        }

        return placements;
    }

    private static List<PlacementPose> CollectPlacementSurfaces(Scene scene)
    {
        List<PlacementPose> placements = new List<PlacementPose>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectPlacementSurfaces(root.transform, placements);
        }

        return placements;
    }

    private static void CollectPlacementSurfaces(Transform current, List<PlacementPose> placements)
    {
        if (IsUsableSurfaceName(current.name) && TryGetSurfacePose(current.gameObject, placements.Count, out PlacementPose placement))
        {
            if (IsDistinctPlacement(placements, placement.Position))
            {
                placements.Add(placement);
            }
        }

        foreach (Transform child in current)
        {
            CollectPlacementSurfaces(child, placements);
        }
    }

    private static bool IsUsableSurfaceName(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();
        if (lowerName.Contains("drawer") ||
            lowerName.Contains("door") ||
            lowerName.Contains("floor") ||
            lowerName.Contains("wall") ||
            lowerName.Contains("chair") ||
            lowerName.Contains("pipe") ||
            lowerName.Contains("flashlight"))
        {
            return false;
        }

        return lowerName.Contains("table") ||
            lowerName.Contains("cabinet") ||
            lowerName.Contains("bedtable") ||
            lowerName.Contains("desk") ||
            lowerName.Contains("shelf") ||
            lowerName.Contains("sink");
    }

    private static bool TryGetSurfacePose(GameObject surfaceObject, int seed, out PlacementPose placement)
    {
        Renderer[] renderers = surfaceObject.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            placement = default;
            return false;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        if (bounds.size.x < 0.35f || bounds.size.z < 0.22f || bounds.size.y > 2.4f)
        {
            placement = default;
            return false;
        }

        Vector3 position = bounds.center + Vector3.up * (bounds.extents.y + 0.07f);
        position += surfaceObject.transform.right * Mathf.Lerp(-0.16f, 0.16f, PseudoRandom(seed, 3));
        position += surfaceObject.transform.forward * Mathf.Lerp(-0.1f, 0.1f, PseudoRandom(seed, 5));

        float yaw = surfaceObject.transform.eulerAngles.y + Mathf.Lerp(-26f, 26f, PseudoRandom(seed, 7));
        placement = new PlacementPose(position, Quaternion.Euler(0f, yaw, 0f));
        return true;
    }

    private static bool IsDistinctPlacement(List<PlacementPose> placements, Vector3 candidate)
    {
        foreach (PlacementPose placement in placements)
        {
            Vector2 a = new Vector2(placement.Position.x, placement.Position.z);
            Vector2 b = new Vector2(candidate.x, candidate.z);
            if (Vector2.Distance(a, b) < 1.15f)
            {
                return false;
            }
        }

        return true;
    }

    private static List<PlacementPose> SelectSpacedPlacements(List<PlacementPose> candidates, int count)
    {
        List<PlacementPose> selected = new List<PlacementPose>();
        if (candidates.Count == 0)
        {
            return selected;
        }

        selected.Add(candidates[0]);
        candidates.RemoveAt(0);

        while (selected.Count < count && candidates.Count > 0)
        {
            int bestIndex = 0;
            float bestScore = float.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                float score = MinDistanceToSelected(candidates[i], selected);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            selected.Add(candidates[bestIndex]);
            candidates.RemoveAt(bestIndex);
        }

        return selected;
    }

    private static float MinDistanceToSelected(PlacementPose candidate, List<PlacementPose> selected)
    {
        float minDistance = float.MaxValue;
        Vector2 candidateXZ = new Vector2(candidate.Position.x, candidate.Position.z);

        foreach (PlacementPose placement in selected)
        {
            Vector2 selectedXZ = new Vector2(placement.Position.x, placement.Position.z);
            minDistance = Mathf.Min(minDistance, Vector2.Distance(candidateXZ, selectedXZ));
        }

        return minDistance;
    }

    private static Material CreateDirtyMaterial(string name, Color baseColor, Color dirtColor, int seed, float dirtAmount)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
        }

        Material material = new Material(shader);
        material.name = name;
        material.color = Color.white;

        Texture2D texture = CreateDirtyTexture(name + "_Texture", baseColor, dirtColor, seed, dirtAmount);
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.05f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.05f);
        }

        return material;
    }

    private static Texture2D CreateDirtyTexture(string textureName, Color baseColor, Color dirtColor, int seed, float dirtAmount)
    {
        const int size = 256;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float fiber = Mathf.PerlinNoise(u * 18f + seed * 0.013f, v * 18f + seed * 0.021f);
                float largeStain = Mathf.PerlinNoise(u * 5f + seed * 0.031f, v * 5f + seed * 0.047f);
                float edgeWear = Mathf.Clamp01((Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) - 0.04f) / 0.16f);
                float dirt = Mathf.Clamp01((1f - edgeWear) * 0.55f + Mathf.Max(0f, largeStain - 0.53f) * dirtAmount + fiber * 0.12f);

                Color color = Color.Lerp(baseColor * Mathf.Lerp(0.82f, 1.16f, fiber), dirtColor, dirt);
                color.a = 1f;
                pixels[y * size + x] = color;
            }
        }

        for (int i = 0; i < 34; i++)
        {
            Vector2 center = new Vector2(PseudoRandom(seed + 100, i) * size, PseudoRandom(seed + 200, i) * size);
            float radius = Mathf.Lerp(4f, 24f, PseudoRandom(seed + 300, i));
            Color stain = Color.Lerp(dirtColor, new Color(0.05f, 0.025f, 0.012f, 1f), PseudoRandom(seed + 400, i));
            DrawTextureBlob(pixels, size, center, radius, stain, Mathf.Lerp(0.08f, 0.28f, PseudoRandom(seed + 500, i)));
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = textureName;
        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply(false, false);
        return texture;
    }

    private static void DrawTextureBlob(Color[] pixels, int size, Vector2 center, float radius, Color color, float alpha)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance > radius)
                {
                    continue;
                }

                float blend = Mathf.Clamp01(1f - distance / radius) * alpha;
                int index = y * size + x;
                pixels[index] = Color.Lerp(pixels[index], color, blend);
            }
        }
    }

    private static void RemoveLegacyArchives(Scene scene)
    {
        List<GameObject> rootsToRemove = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == LegacyArchiveRootName || (root.name.StartsWith("Patient_Dossier_Archive") && root.name != ArchiveRootName))
            {
                rootsToRemove.Add(root);
            }
        }

        foreach (GameObject root in rootsToRemove)
        {
            Object.Destroy(root);
        }
    }

    private static float PseudoRandom(int seed, int index)
    {
        float value = Mathf.Sin(seed * 12.9898f + index * 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    private static string SanitizeName(string value)
    {
        return value.Replace(" ", "_").Replace(".", string.Empty).Replace("#", string.Empty);
    }

    private static string FormatVector(Vector3 value)
    {
        return "(" +
            value.x.ToString("F3") + ", " +
            value.y.ToString("F3") + ", " +
            value.z.ToString("F3") + ")";
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildByName(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildByName(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsTelevisionPuzzleScene(Scene scene)
    {
        return scene.name == MediumSceneName;
    }

    private readonly struct PlacementPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public PlacementPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }

    private readonly struct PatientRecord
    {
        public readonly string PatientName;
        public readonly string PatientId;
        public readonly string Diagnosis;
        public readonly string RiskLevel;
        public readonly string Observations;
        public readonly string Treatment;
        public readonly string AdministrativeNote;
        public readonly bool IsCritical;

        public PatientRecord(
            string patientName,
            string patientId,
            string diagnosis,
            string riskLevel,
            string observations,
            string treatment,
            string administrativeNote,
            bool isCritical)
        {
            PatientName = patientName;
            PatientId = patientId;
            Diagnosis = diagnosis;
            RiskLevel = riskLevel;
            Observations = observations;
            Treatment = treatment;
            AdministrativeNote = administrativeNote;
            IsCritical = isCritical;
        }
    }
}

using System;
using UnityEngine;

public class DoctorDeskFileGenerator : MonoBehaviour
{
    private enum FilePlacementSurface
    {
        DeskTop,
        FloorNearDesk
    }

    private const string RedFileName = "MedicalFile_Red";
    private const string BlueFileName = "MedicalFile_Blue";
    private const string BlackFileName = "MedicalFile_Black";

    private const string RedContent =
        "PATIENT 317\n" +
        "STATUS: DECEASED\n" +
        "TIME: 03:19\n" +
        "STAMP: DEAD\n" +
        "NOTE:\n" +
        "The body was declared silent before midnight.";

    private const string BlueContent =
        "PATIENT 317\n" +
        "MEMORY TEST\n" +
        "TIME: 05:12\n" +
        "NOTE:\n" +
        "Patient remembers events that never happened.\n" +
        "Patient claims the walls answered him.";

    private const string BlackContent =
        "PATIENT 317\n" +
        "OBSERVATION REPORT\n" +
        "TIME: 02:46\n" +
        "NOTE:\n" +
        "Patient screamed every night at exactly 02:46.\n" +
        "He repeated:\n" +
        "\"I was awake when they declared me dead.\"";

    [Header("Desk")]
    [SerializeField] private Transform doctorDesk;
    [SerializeField] private bool placeOnDetectedDeskTop = true;
    [SerializeField, Min(0f)] private float deskSurfacePadding = 0.025f;

    [Header("Document Placement")]
    [SerializeField] private bool preserveExistingFileTransforms = true;
    [SerializeField] private bool useExactFileLocalPositions = true;
    [SerializeField] private FilePlacementSurface redFileSurface = FilePlacementSurface.FloorNearDesk;
    [SerializeField] private FilePlacementSurface blueFileSurface = FilePlacementSurface.DeskTop;
    [SerializeField] private FilePlacementSurface blackFileSurface = FilePlacementSurface.DeskTop;
    [SerializeField] private Vector3 redFileLocalOffset = new Vector3(-0.720000267f, -0.00791931152f, 0.810997009f);
    [SerializeField] private Vector3 blueFileLocalOffset = new Vector3(1.65100002f, -0.853143692f, 0.586000025f);
    [SerializeField] private Vector3 blackFileLocalOffset = new Vector3(0.359999657f, 0.627162933f, 0.00899999961f);
    [SerializeField] private Vector3 redFileRotation = new Vector3(67.9978561f, 90.7773438f, 90.8383865f);
    [SerializeField] private Vector3 blueFileRotation = new Vector3(84.9972382f, 271.913849f, 271.921143f);
    [SerializeField] private Vector3 blackFileRotation = new Vector3(84.9990768f, 268.850739f, 268.846313f);
    [SerializeField] private Vector3 fileRotation = new Vector3(0f, -5f, 0f);
    [SerializeField] private Vector3 floorFileRotation = new Vector3(0f, 22f, 0f);
    [SerializeField] private Vector3 fileScale = new Vector3(0.36f, 0.018f, 0.50f);

    [Header("Floor Placement")]
    [SerializeField] private bool usePhysicsFloorDetection = true;
    [SerializeField] private LayerMask floorRaycastMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.1f)] private float floorRaycastHeight = 2.5f;
    [SerializeField, Min(0.1f)] private float floorRaycastDistance = 6f;
    [SerializeField, Min(0f)] private float floorSurfacePadding = 0.018f;

    private GeneratedMedicalFile[] generatedFiles;

    public Transform DoctorDesk => doctorDesk != null ? doctorDesk : transform;

    private void Awake()
    {
        if (doctorDesk == null)
        {
            doctorDesk = transform;
        }
    }

    public void ConfigureDesk(Transform desk)
    {
        if (desk != null)
        {
            doctorDesk = desk;
        }
        else if (doctorDesk == null)
        {
            doctorDesk = transform;
        }
    }

    public GeneratedMedicalFile[] GenerateFilesIfNeeded(DoctorReportsPuzzle owner, AudioClip fileOpenSound, AudioClip paperRustleSound)
    {
        if (doctorDesk == null)
        {
            doctorDesk = transform;
        }

        float deskTopLocalY = ResolveDeskTopLocalY();
        float floorLocalY = ResolveFloorFallbackLocalY();

        generatedFiles = new[]
        {
            EnsureFile(
                RedFileName,
                "DECEASED REPORT",
                "PATIENT 317\nSTATUS: DECEASED\nTIME: 03:19\nSTAMP: DEAD",
                RedContent,
                GeneratedMedicalFile.FileStyle.RedDeceased,
                redFileSurface,
                redFileLocalOffset,
                deskTopLocalY,
                floorLocalY,
                owner,
                fileOpenSound,
                paperRustleSound),
            EnsureFile(
                BlueFileName,
                "MEMORY TEST",
                "PATIENT 317\nMEMORY TEST\nTIME: 05:12",
                BlueContent,
                GeneratedMedicalFile.FileStyle.BlueMemory,
                blueFileSurface,
                blueFileLocalOffset,
                deskTopLocalY,
                floorLocalY,
                owner,
                fileOpenSound,
                paperRustleSound),
            EnsureFile(
                BlackFileName,
                "OBSERVATION REPORT",
                "PATIENT 317\nOBSERVATION REPORT\nTIME: 02:46",
                BlackContent,
                GeneratedMedicalFile.FileStyle.BlackObservation,
                blackFileSurface,
                blackFileLocalOffset,
                deskTopLocalY,
                floorLocalY,
                owner,
                fileOpenSound,
                paperRustleSound),
        };

        Debug.Log("DoctorDeskFileGenerator ensured Doctor Marek's three reports exist.");
        return generatedFiles;
    }

    public void SetFilesReadable(bool readable)
    {
        if (generatedFiles == null || generatedFiles.Length == 0)
        {
            generatedFiles = GetComponentsInChildren<GeneratedMedicalFile>(true);
        }

        for (int i = 0; i < generatedFiles.Length; i++)
        {
            if (generatedFiles[i] == null)
            {
                continue;
            }

            MedicalFileReadable readableFile = generatedFiles[i].GetComponent<MedicalFileReadable>();
            if (readableFile != null)
            {
                readableFile.SetReadable(readable);
            }
        }
    }

    private GeneratedMedicalFile EnsureFile(
        string objectName,
        string title,
        string visibleSummary,
        string fullBody,
        GeneratedMedicalFile.FileStyle style,
        FilePlacementSurface surface,
        Vector3 localOffset,
        float deskTopLocalY,
        float floorLocalY,
        DoctorReportsPuzzle owner,
        AudioClip fileOpenSound,
        AudioClip paperRustleSound)
    {
        Transform existing = FindChildRecursive(doctorDesk, objectName);
        GameObject fileObject;
        bool created = false;

        if (existing == null)
        {
            fileObject = new GameObject(objectName);
            fileObject.transform.SetParent(doctorDesk, false);
            created = true;
        }
        else
        {
            fileObject = existing.gameObject;
        }

        if (created || !preserveExistingFileTransforms)
        {
            fileObject.transform.localPosition = ResolveFilePosition(surface, localOffset, deskTopLocalY, floorLocalY);
            fileObject.transform.localRotation = Quaternion.Euler(ResolveFileRotation(objectName, surface));
            fileObject.transform.localScale = SanitizeScale(fileScale);
        }
        else
        {
            Debug.Log("DoctorDeskFileGenerator preserved manual transform for existing report: " + objectName);
        }

        GeneratedMedicalFile generatedFile = fileObject.GetComponent<GeneratedMedicalFile>();
        if (generatedFile == null)
        {
            generatedFile = fileObject.AddComponent<GeneratedMedicalFile>();
        }

        generatedFile.Configure(style, title, visibleSummary);

        MedicalFileReadable readable = fileObject.GetComponent<MedicalFileReadable>();
        if (readable == null)
        {
            readable = fileObject.AddComponent<MedicalFileReadable>();
        }

        readable.Configure(owner, title, fullBody, fileOpenSound, paperRustleSound);
        readable.SetReadable(false);

        if (created)
        {
            Debug.Log("Created generated medical report: " + objectName);
        }

        return generatedFile;
    }

    private Vector3 ResolveFileRotation(string objectName, FilePlacementSurface surface)
    {
        switch (objectName)
        {
            case RedFileName:
                return redFileRotation;
            case BlueFileName:
                return blueFileRotation;
            case BlackFileName:
                return blackFileRotation;
            default:
                return surface == FilePlacementSurface.FloorNearDesk ? floorFileRotation : fileRotation;
        }
    }

    private Vector3 ResolveFilePosition(FilePlacementSurface surface, Vector3 localOffset, float deskTopLocalY, float floorLocalY)
    {
        if (useExactFileLocalPositions)
        {
            return localOffset;
        }

        if (surface == FilePlacementSurface.FloorNearDesk)
        {
            float resolvedFloorY = floorLocalY;
            if (usePhysicsFloorDetection && TryResolvePhysicsFloorLocalY(localOffset, out float physicsFloorY))
            {
                resolvedFloorY = physicsFloorY;
            }

            return new Vector3(localOffset.x, resolvedFloorY + floorSurfacePadding + localOffset.y, localOffset.z);
        }

        if (!placeOnDetectedDeskTop)
        {
            return localOffset;
        }

        return new Vector3(localOffset.x, deskTopLocalY + deskSurfacePadding + localOffset.y, localOffset.z);
    }

    private float ResolveDeskTopLocalY()
    {
        return ResolveDeskLocalYExtreme(true, "top");
    }

    private float ResolveFloorFallbackLocalY()
    {
        return ResolveDeskLocalYExtreme(false, "floor fallback");
    }

    private float ResolveDeskLocalYExtreme(bool highest, string label)
    {
        if (doctorDesk == null)
        {
            return 0f;
        }

        Renderer[] renderers = doctorDesk.GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        float resolvedY = 0f;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer target = renderers[i];
            if (target == null || ShouldIgnoreRenderer(target))
            {
                continue;
            }

            Bounds bounds = target.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        float localY = doctorDesk.InverseTransformPoint(worldCorner).y;
                        if (!foundRenderer || (highest ? localY > resolvedY : localY < resolvedY))
                        {
                            resolvedY = localY;
                            foundRenderer = true;
                        }
                    }
                }
            }
        }

        if (!foundRenderer)
        {
            Debug.LogWarning("DoctorDeskFileGenerator could not detect doctor_desk " + label + " height. Using file local offsets as-is.");
            return 0f;
        }

        return resolvedY;
    }

    private bool TryResolvePhysicsFloorLocalY(Vector3 localOffset, out float floorLocalY)
    {
        floorLocalY = 0f;
        if (doctorDesk == null)
        {
            return false;
        }

        Vector3 localRayPoint = new Vector3(localOffset.x, 0f, localOffset.z);
        Vector3 worldPoint = doctorDesk.TransformPoint(localRayPoint);
        Vector3 rayOrigin = worldPoint + Vector3.up * floorRaycastHeight;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            floorRaycastHeight + floorRaycastDistance,
            floorRaycastMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(doctorDesk))
            {
                continue;
            }

            floorLocalY = doctorDesk.InverseTransformPoint(hits[i].point).y;
            return true;
        }

        return false;
    }

    private bool ShouldIgnoreRenderer(Renderer target)
    {
        if (target.GetComponentInParent<GeneratedMedicalFile>() != null ||
            target.GetComponentInParent<MedicalFileReadable>() != null)
        {
            return true;
        }

        Transform current = target.transform;
        while (current != null && current != doctorDesk)
        {
            if (current.name.StartsWith("MedicalFile_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current.name.IndexOf("Nameplate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                current.name.Equals("Doctor_H_Marek_Nameplate", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Vector3 SanitizeScale(Vector3 scale)
    {
        return new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            Mathf.Max(0.002f, Mathf.Abs(scale.y)),
            Mathf.Max(0.01f, Mathf.Abs(scale.z)));
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

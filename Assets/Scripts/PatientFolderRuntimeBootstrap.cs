using UnityEngine;

public static class PatientFolderRuntimeBootstrap
{
    private static readonly PatientSeed[] Patients =
    {
        new PatientSeed(
            1,
            "Sarah M.",
            67,
            "Amnesie traumatique severe",
            "Patiente retrouvee inconsciente\ndans le couloir du sous-sol.\nAucun souvenir des 3 derniers mois."),
        new PatientSeed(
            2,
            "Adam K.",
            45,
            "Contusion pulmonaire grave",
            "Retrouve dans la chambre 12.\nRespiration difficile.\nCause du traumatisme inconnue."),
        new PatientSeed(
            3,
            "Elena R.",
            30,
            "Fracture complexe du pied",
            "Patiente admise en urgence.\nImpossibilite de marcher.\nRefuse de parler de l'incident."),
        new PatientSeed(
            4,
            "Marcus T.",
            55,
            "Hernie discale L4-L5",
            "Douleurs lombaires extremes.\nPatient agite et violent.\nTransfert demande."),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SeedExistingFolders()
    {
        Transform keypadTransform = GameObject.Find("SM_Pin_Pad")?.transform;
        Transform tableTransform = EnsureRuntimeTable(keypadTransform);

        for (int i = 0; i < Patients.Length; i++)
        {
            GameObject folderObject = GameObject.Find($"Dossier_Patient{Patients[i].PatientId:00}");
            if (folderObject == null)
            {
                continue;
            }

            PatientFolder folder = folderObject.GetComponent<PatientFolder>();
            if (folder == null)
            {
                folder = folderObject.AddComponent<PatientFolder>();
            }

            folder.patientID = Patients[i].PatientId;
            folder.patientName = Patients[i].PatientName;
            folder.patientAge = Patients[i].PatientAge;
            folder.disease = Patients[i].Disease;
            folder.medicalNotes = Patients[i].Notes;
            folder.isRead = false;

            if (tableTransform != null)
            {
                PlaceFolderOnTable(folderObject.transform, tableTransform, i);
            }
        }
    }

    private static Transform EnsureRuntimeTable(Transform keypadTransform)
    {
        if (keypadTransform == null)
        {
            return null;
        }

        GameObject table = GameObject.Find("PatientTable");
        if (table == null)
        {
            table = new GameObject("PatientTable");
        }

        // Force the table inside the padded room, visible as soon as the player enters.
        Vector3 tableCenter = keypadTransform.position + new Vector3(0.10f, -1.28f, -1.20f);
        table.transform.position = tableCenter;
        table.transform.rotation = Quaternion.identity;

        CreateOrUpdateTablePart(table.transform, "TableTop", new Vector3(0f, 0.62f, 0f), new Vector3(0.58f, 0.05f, 0.42f));
        CreateOrUpdateTablePart(table.transform, "Leg_A", new Vector3(-0.22f, 0.31f, -0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(table.transform, "Leg_B", new Vector3(0.22f, 0.31f, -0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(table.transform, "Leg_C", new Vector3(-0.22f, 0.31f, 0.15f), new Vector3(0.05f, 0.62f, 0.05f));
        CreateOrUpdateTablePart(table.transform, "Leg_D", new Vector3(0.22f, 0.31f, 0.15f), new Vector3(0.05f, 0.62f, 0.05f));

        return table.transform;
    }

    private static void PlaceFolderOnTable(Transform folderTransform, Transform tableTransform, int index)
    {
        Vector3[] offsets =
        {
            new Vector3(-0.10f, 0.66f, -0.08f),
            new Vector3(0.10f, 0.66f, -0.06f),
            new Vector3(-0.08f, 0.66f, 0.08f),
            new Vector3(0.10f, 0.66f, 0.08f),
        };

        Quaternion[] rotations =
        {
            Quaternion.Euler(90f, -8f, 0f),
            Quaternion.Euler(90f, 9f, 0f),
            Quaternion.Euler(90f, -4f, 0f),
            Quaternion.Euler(90f, 6f, 0f),
        };

        folderTransform.position = tableTransform.position + offsets[Mathf.Clamp(index, 0, offsets.Length - 1)];
        folderTransform.rotation = rotations[Mathf.Clamp(index, 0, rotations.Length - 1)];
        folderTransform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
    }

    private static void CreateOrUpdateTablePart(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale)
    {
        Transform existing = parent.Find(objectName);
        GameObject part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
    }

    private readonly struct PatientSeed
    {
        public readonly int PatientId;
        public readonly string PatientName;
        public readonly int PatientAge;
        public readonly string Disease;
        public readonly string Notes;

        public PatientSeed(int patientId, string patientName, int patientAge, string disease, string notes)
        {
            PatientId = patientId;
            PatientName = patientName;
            PatientAge = patientAge;
            Disease = disease;
            Notes = notes;
        }
    }
}

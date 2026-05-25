using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhonePuzzleRuntimeBootstrap
{
    private const string HardSceneName = "demoScene_Difficile";

    private static bool subscribedToSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        if (!subscribedToSceneLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            subscribedToSceneLoaded = true;
        }

        EnsureSceneSetup(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSceneSetup(scene);
    }

    public static void EnsureSceneSetup()
    {
        EnsureSceneSetup(SceneManager.GetActiveScene());
    }

    private static void EnsureSceneSetup(Scene scene)
    {
        if (!IsPhonePuzzleScene(scene))
        {
            return;
        }

        Transform phone = FindBestObject("WallPhone_Main", "wallphone", "wall phone", "telephone", "phone");
        WaitingRoomPuzzle puzzle = EnsureWaitingRoomPuzzle();
        HeartRoomPuzzle puzzle2 = EnsureHeartRoomPuzzle();
        DoctorReportsPuzzle puzzle3 = EnsureDoctorReportsPuzzle();
        OperationOrderPuzzle puzzle4 = EnsureOperationOrderPuzzle();
        Puzzle5MorgueController puzzle5 = EnsurePuzzle5MorgueController();
        OperationRoomPassageFix.EnsureForScene();
        LightFlickerController flicker = UnityEngine.Object.FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        MonsterActivationController monster = EnsureMonsterActivationController();
        FinalExitDoubleDoorController finalExitDoors = EnsureFinalExitDoubleDoorController();
        FinalDoorKeypad finalDoorKeypad = EnsureFinalDoorKeypad(finalExitDoors, monster, flicker);

        if (phone == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find WallPhone_Main. Rename the phone object to WallPhone_Main or add WallPhoneController manually.");
            return;
        }

        WallPhoneController controller = phone.GetComponent<WallPhoneController>();
        if (controller == null)
        {
            controller = phone.gameObject.AddComponent<WallPhoneController>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added WallPhoneController to " + phone.name + ".");
        }

        controller.ConfigureSceneReferences(puzzle, flicker, monster);
        controller.ConfigurePuzzle2Reference(puzzle2);
        controller.ConfigurePuzzle3Reference(puzzle3);
        controller.ConfigurePuzzle4Reference(puzzle4);
        controller.ConfigurePuzzle5Reference(puzzle5);

        PhoneCallSequenceManager callSequenceManager = phone.GetComponent<PhoneCallSequenceManager>() ?? phone.gameObject.AddComponent<PhoneCallSequenceManager>();
        AudioSource phoneAudioSource = phone.GetComponent<AudioSource>();
        EnsureFinalEscapeFlow(phone, puzzle5, finalDoorKeypad, callSequenceManager, phoneAudioSource);
    }

    private static bool IsPhonePuzzleScene(Scene scene)
    {
        return scene.IsValid() && scene.isLoaded && scene.name == HardSceneName;
    }

    private static WaitingRoomPuzzle EnsureWaitingRoomPuzzle()
    {
        Transform waitingRoomWall = FindBestObject("WaitingRoomWall", "waitingroomwall", "waiting room wall");
        if (waitingRoomWall == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find WaitingRoomWall. The phone will ring, but Puzzle 1 lists need that object.");
            return null;
        }

        WaitingRoomPuzzle puzzle = waitingRoomWall.GetComponent<WaitingRoomPuzzle>();
        if (puzzle == null)
        {
            puzzle = waitingRoomWall.gameObject.AddComponent<WaitingRoomPuzzle>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added WaitingRoomPuzzle to WaitingRoomWall.");
        }

        ReadableWallList admission = EnsureReadableList("AdmissionList", waitingRoomWall);
        ReadableWallList deaths = EnsureReadableList("DeathRecords", waitingRoomWall);
        ReadableWallList rooms = EnsureReadableList("RoomAssignments", waitingRoomWall);
        GameObject terminal = FindBestObject("PatientInputTerminal", "patientinputterminal", "patient input terminal")?.gameObject;

        puzzle.ConfigureSceneReferences(admission, deaths, rooms, terminal);
        return puzzle;
    }

    private static ReadableWallList EnsureReadableList(string objectName, Transform preferredParent)
    {
        Transform target = FindExactObject(objectName);

        if (target == null && preferredParent != null)
        {
            target = FindChildRecursive(preferredParent, objectName);
        }

        if (target == null)
        {
            GameObject created = new GameObject(objectName);
            created.transform.SetParent(preferredParent, false);
            created.transform.localPosition = Vector3.zero;
            created.transform.localRotation = Quaternion.identity;
            target = created.transform;
            Debug.Log("PhonePuzzleRuntimeBootstrap created missing " + objectName + " under WaitingRoomWall.");
        }

        ReadableWallList list = target.GetComponent<ReadableWallList>();
        if (list == null)
        {
            list = target.gameObject.AddComponent<ReadableWallList>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added ReadableWallList to " + objectName + ".");
        }

        return list;
    }

    private static MonsterActivationController EnsureMonsterActivationController()
    {
        MonsterActivationController existing = UnityEngine.Object.FindFirstObjectByType<MonsterActivationController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        Transform monster = FindBestObject("Monster_Entity", "monster_entity", "monster", "entity");
        if (monster == null)
        {
            return null;
        }

        MonsterActivationController controller = monster.GetComponent<MonsterActivationController>();
        if (controller == null)
        {
            controller = monster.gameObject.AddComponent<MonsterActivationController>();
        }

        return controller;
    }

    private static HeartRoomPuzzle EnsureHeartRoomPuzzle()
    {
        Transform puzzleRoom = FindExactObject("Puzzle2_room");
        if (puzzleRoom == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find Puzzle2_room. Puzzle 2 will wait for a manual HeartRoomPuzzle reference.");
            return null;
        }

        HeartRoomPuzzle puzzle = puzzleRoom.GetComponent<HeartRoomPuzzle>();
        if (puzzle == null)
        {
            puzzle = puzzleRoom.gameObject.AddComponent<HeartRoomPuzzle>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added HeartRoomPuzzle to Puzzle2_room.");
        }

        return puzzle;
    }

    private static DoctorReportsPuzzle EnsureDoctorReportsPuzzle()
    {
        Transform puzzleRoom = FindExactObject("Puzzle3_room");
        if (puzzleRoom == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find Puzzle3_room. Puzzle 3 will wait for a manual DoctorReportsPuzzle reference.");
            return null;
        }

        DoctorReportsPuzzle puzzle = puzzleRoom.GetComponent<DoctorReportsPuzzle>();
        if (puzzle == null)
        {
            puzzle = puzzleRoom.gameObject.AddComponent<DoctorReportsPuzzle>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added DoctorReportsPuzzle to Puzzle3_room.");
        }

        Transform desk = FindChildRecursive(puzzleRoom, "doctor_desk");
        if (desk == null)
        {
            desk = FindExactObject("doctor_desk");
        }

        if (desk == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find doctor_desk under Puzzle3_room. Reports will not generate until the desk is assigned.");
        }
        else if (desk.GetComponent<DoctorDeskFileGenerator>() == null)
        {
            desk.gameObject.AddComponent<DoctorDeskFileGenerator>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added DoctorDeskFileGenerator to doctor_desk.");
        }

        MonsterActivationController monster = EnsureMonsterActivationController();
        LightFlickerController flicker = UnityEngine.Object.FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        puzzle.ConfigureSceneReferences(desk, monster, flicker);
        return puzzle;
    }

    private static OperationOrderPuzzle EnsureOperationOrderPuzzle()
    {
        Transform puzzleRoot = FindExactObject("Puzzle4_OperationOrder");
        Transform allInstruments = FindExactObject("All_Instruments");

        if (puzzleRoot == null)
        {
            puzzleRoot = allInstruments;
        }

        if (puzzleRoot == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find Puzzle4_OperationOrder or All_Instruments. Puzzle 4 will wait for a manual OperationOrderPuzzle reference.");
            return null;
        }

        OperationOrderPuzzle puzzle = puzzleRoot.GetComponent<OperationOrderPuzzle>();
        if (puzzle == null)
        {
            puzzle = puzzleRoot.gameObject.AddComponent<OperationOrderPuzzle>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added OperationOrderPuzzle to " + puzzleRoot.name + ".");
        }

        OperationInstrumentSequenceManager sequenceManager = puzzleRoot.GetComponent<OperationInstrumentSequenceManager>();
        if (sequenceManager == null)
        {
            sequenceManager = puzzleRoot.gameObject.AddComponent<OperationInstrumentSequenceManager>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added OperationInstrumentSequenceManager to " + puzzleRoot.name + ".");
        }

        MonsterActivationController monster = EnsureMonsterActivationController();
        LightFlickerController flicker = UnityEngine.Object.FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        puzzle.ConfigureSceneReferences(allInstruments, monster, flicker);
        return puzzle;
    }

    private static Puzzle5MorgueController EnsurePuzzle5MorgueController()
    {
        Transform morgue = FindExactObject("morgue");
        Transform slots = morgue != null ? FindChildRecursive(morgue, "slots") : FindExactObject("slots");
        Transform puzzleRoot = FindExactObject("Puzzle5_MorgueCabinets");

        if (slots == null)
        {
            slots = FindExactObject("slots");
        }

        if (puzzleRoot == null)
        {
            puzzleRoot = morgue != null ? morgue : slots;
        }

        if (puzzleRoot == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find morgue/slots. Puzzle 5 will wait for a manual Puzzle5MorgueController reference.");
            return null;
        }

        Puzzle5MorgueController puzzle = puzzleRoot.GetComponent<Puzzle5MorgueController>();
        if (puzzle == null)
        {
            puzzle = puzzleRoot.gameObject.AddComponent<Puzzle5MorgueController>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added Puzzle5MorgueController to " + puzzleRoot.name + ".");
        }

        MorgueCabinetSetup setup = puzzleRoot.GetComponent<MorgueCabinetSetup>();
        if (setup == null)
        {
            setup = puzzleRoot.gameObject.AddComponent<MorgueCabinetSetup>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added MorgueCabinetSetup to " + puzzleRoot.name + ".");
        }

        MonsterActivationController monster = EnsureMonsterActivationController();
        LightFlickerController flicker = UnityEngine.Object.FindFirstObjectByType<LightFlickerController>(FindObjectsInactive.Include);
        puzzle.ConfigureSceneReferences(slots, monster, flicker);
        setup.ConfigureSceneReferences(slots, puzzle);
        return puzzle;
    }

    private static FinalExitDoubleDoorController EnsureFinalExitDoubleDoorController()
    {
        Transform controllerTransform = FindExactObject("FinalExitDoubleDoorController");
        GameObject controllerObject;
        if (controllerTransform != null)
        {
            controllerObject = controllerTransform.gameObject;
        }
        else
        {
            controllerObject = new GameObject("FinalExitDoubleDoorController");
        }

        FinalExitDoubleDoorController controller = controllerObject.GetComponent<FinalExitDoubleDoorController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<FinalExitDoubleDoorController>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added FinalExitDoubleDoorController.");
        }

        Transform leftDoor = FindExactObject("portegauchesortie");
        Transform rightDoor = FindExactObject("portedroitesortie");
        if (leftDoor == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find final exit door: portegauchesortie.");
        }

        if (rightDoor == null)
        {
            Debug.LogWarning("PhonePuzzleRuntimeBootstrap could not find final exit door: portedroitesortie.");
        }

        controller.Configure(leftDoor, rightDoor, null);
        return controller;
    }

    private static FinalDoorKeypad EnsureFinalDoorKeypad(FinalExitDoubleDoorController doors, MonsterActivationController monster, LightFlickerController flicker)
    {
        Transform keypadTransform = FindExactObject("FinalDoorKeypad");
        GameObject keypadObject;
        if (keypadTransform != null)
        {
            keypadObject = keypadTransform.gameObject;
        }
        else
        {
            keypadObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keypadObject.name = "FinalDoorKeypad";
            keypadObject.transform.position = ResolveFinalKeypadPosition(doors);
            keypadObject.transform.rotation = ResolveFinalKeypadRotation(doors);
            keypadObject.transform.localScale = new Vector3(0.35f, 0.55f, 0.08f);

            Renderer renderer = keypadObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.035f, 0.038f, 0.042f, 1f);
            }

            Debug.Log("PhonePuzzleRuntimeBootstrap created FinalDoorKeypad near the final exit.");
        }

        Collider collider = keypadObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        FinalDoorKeypad keypad = keypadObject.GetComponent<FinalDoorKeypad>();
        if (keypad == null)
        {
            keypad = keypadObject.AddComponent<FinalDoorKeypad>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added FinalDoorKeypad.");
        }

        keypad.Configure(doors, monster, flicker);
        keypad.SetKeypadEnabled(false);
        return keypad;
    }

    private static FinalEscapeFlow EnsureFinalEscapeFlow(
        Transform phone,
        Puzzle5MorgueController puzzle5,
        FinalDoorKeypad keypad,
        PhoneCallSequenceManager callSequenceManager,
        AudioSource phoneAudioSource)
    {
        if (phone == null)
        {
            return null;
        }

        FinalEscapeFlow flow = phone.GetComponent<FinalEscapeFlow>();
        if (flow == null)
        {
            flow = phone.gameObject.AddComponent<FinalEscapeFlow>();
            Debug.Log("PhonePuzzleRuntimeBootstrap added FinalEscapeFlow to " + phone.name + ".");
        }

        flow.Configure(puzzle5, keypad, callSequenceManager, phoneAudioSource);
        return flow;
    }

    private static Vector3 ResolveFinalKeypadPosition(FinalExitDoubleDoorController doors)
    {
        Transform leftDoor = doors != null ? doors.LeftDoor : FindExactObject("portegauchesortie");
        Transform rightDoor = doors != null ? doors.RightDoor : FindExactObject("portedroitesortie");

        if (leftDoor != null && rightDoor != null)
        {
            Vector3 midpoint = (leftDoor.position + rightDoor.position) * 0.5f;
            Vector3 forward = leftDoor.forward.sqrMagnitude > 0.0001f ? leftDoor.forward : Vector3.forward;
            return midpoint + Vector3.up * 1.25f - forward.normalized * 0.35f;
        }

        if (leftDoor != null)
        {
            return leftDoor.position + Vector3.up * 1.25f - leftDoor.forward.normalized * 0.35f;
        }

        if (rightDoor != null)
        {
            return rightDoor.position + Vector3.up * 1.25f - rightDoor.forward.normalized * 0.35f;
        }

        Debug.LogWarning("PhonePuzzleRuntimeBootstrap created FinalDoorKeypad at origin because final exit doors were not found.");
        return new Vector3(0f, 1.35f, 0f);
    }

    private static Quaternion ResolveFinalKeypadRotation(FinalExitDoubleDoorController doors)
    {
        Transform leftDoor = doors != null ? doors.LeftDoor : FindExactObject("portegauchesortie");
        Transform rightDoor = doors != null ? doors.RightDoor : FindExactObject("portedroitesortie");
        Transform referenceDoor = leftDoor != null ? leftDoor : rightDoor;
        if (referenceDoor == null)
        {
            return Quaternion.identity;
        }

        Vector3 forward = -referenceDoor.forward;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private static Transform FindBestObject(string exactName, params string[] nameHints)
    {
        Transform exact = FindExactObject(exactName);
        if (exact != null)
        {
            return exact;
        }

        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform best = null;
        int bestScore = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name.StartsWith("UI_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string normalized = NormalizeName(candidate.name);
            int score = ScoreName(normalized, nameHints);
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static Transform FindExactObject(string objectName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static int ScoreName(string normalizedName, string[] hints)
    {
        int score = 0;
        for (int i = 0; i < hints.Length; i++)
        {
            string hint = NormalizeName(hints[i]);
            if (normalizedName == hint)
            {
                score = Mathf.Max(score, 100 - i);
            }
            else if (normalizedName.Contains(hint))
            {
                score = Mathf.Max(score, 70 - i);
            }
        }

        return score;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }
}

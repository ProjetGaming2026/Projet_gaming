using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemoSceneMapExpansionTool
{
    private const string DemoScenePath = "Assets/Hospital/scenes/demoScene.unity";
    private const string ExpansionRootName = "MapExtension_WingA";
    private const float WallThickness = 0.2f;
    private const float FloorThickness = 0.15f;
    private const float DoorWidth = 1.6f;
    private const float DoorHeight = 2.45f;
    private const float DefaultHeight = 3.3f;

    private static readonly string Room2Path = "Assets/Hospital/_Prefabs/rooms/room2.prefab";
    private static readonly string BedPath = "Assets/Hospital/_Prefabs/bed/_premade/bed2.prefab";
    private static readonly string CabinetPath = "Assets/Hospital/_Prefabs/furniture/cabinet2.prefab";
    private static readonly string LockerPath = "Assets/Hospital/_Prefabs/furniture/locker.prefab";
    private static readonly string TablePath = "Assets/Hospital/_Prefabs/furniture/table2.prefab";
    private static readonly string ChairPath = "Assets/Hospital/_Prefabs/furniture/chair1.prefab";
    private static readonly string BoxGroupPath = "Assets/Hospital/_Prefabs/props/boxgroup1.prefab";
    private static readonly string LampPath = "Assets/Hospital/_Prefabs/props/lamp1.prefab";
    private static readonly string DoorPath = "Assets/Hospital/_Prefabs/corridor2/door1.prefab";
    private static readonly string MapPicturePath = "Assets/Hospital/_Prefabs/pictures/map1.prefab";
    private static readonly string SplatterPath = "Assets/Hospital/_Prefabs/decals/splatter3.prefab";

    private static readonly string CorridorWallMaterialPath = "Assets/Hospital/_Materials/corridor2.mat";
    private static readonly string RoomWallMaterialPath = "Assets/Hospital/_Materials/room5.mat";
    private static readonly string CorridorFloorMaterialPath = "Assets/Hospital/_Materials/floor2.mat";
    private static readonly string RoomFloorMaterialPath = "Assets/Hospital/_Materials/floor4.mat";
    private static readonly string CeilingMaterialPath = "Assets/Hospital/_Materials/black.mat";
    private static readonly string OpenDoorClipPath = "Assets/audio/opendoor.mp3";
    private static readonly string CloseDoorClipPath = "Assets/audio/closedoor.mp3";

    [Flags]
    private enum DoorSides
    {
        None = 0,
        West = 1,
        East = 2,
        North = 4,
        South = 8,
    }

    [MenuItem("Tools/Gameplay/Expand Demo Scene Map")]
    public static void ExpandDemoSceneMap()
    {
        Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);

        try
        {
            GameObject room2 = FindRoot(demoScene, "room2");
            if (room2 == null)
            {
                throw new InvalidOperationException("room2 anchor was not found in demoScene.");
            }

            RemoveRootIfExists(demoScene, ExpansionRootName);

            var materials = new MaterialSet
            {
                CorridorWall = LoadAsset<Material>(CorridorWallMaterialPath),
                RoomWall = LoadAsset<Material>(RoomWallMaterialPath),
                CorridorFloor = LoadAsset<Material>(CorridorFloorMaterialPath),
                RoomFloor = LoadAsset<Material>(RoomFloorMaterialPath),
                Ceiling = LoadAsset<Material>(CeilingMaterialPath),
            };

            GameObject expansionRoot = new GameObject(ExpansionRootName);
            SceneManager.MoveGameObjectToScene(expansionRoot, demoScene);

            Vector3 anchorDoor = room2.transform.TransformPoint(new Vector3(0f, 0f, -2.45f));
            expansionRoot.transform.position = new Vector3(anchorDoor.x, room2.transform.position.y, anchorDoor.z);

            var bridgeDoors = BuildShell(
                expansionRoot.transform,
                "WingBridge",
                new Vector3(2f, 0f, 0f),
                new Vector2(4f, 3f),
                DefaultHeight,
                DoorSides.West | DoorSides.East,
                materials.CorridorWall,
                materials.CorridorFloor,
                materials.Ceiling
            );

            var corridorDoors = BuildShell(
                expansionRoot.transform,
                "WingCorridor",
                new Vector3(9f, 0f, 0f),
                new Vector2(10f, 3f),
                DefaultHeight,
                DoorSides.West | DoorSides.East | DoorSides.North | DoorSides.South,
                materials.CorridorWall,
                materials.CorridorFloor,
                materials.Ceiling
            );

            var observationDoors = BuildShell(
                expansionRoot.transform,
                "ObservationRoom",
                new Vector3(9f, 0f, 6f),
                new Vector2(6f, 6f),
                DefaultHeight,
                DoorSides.South,
                materials.RoomWall,
                materials.RoomFloor,
                materials.Ceiling
            );

            var storageDoors = BuildShell(
                expansionRoot.transform,
                "StorageRoom",
                new Vector3(9f, 0f, -6f),
                new Vector2(6f, 6f),
                DefaultHeight,
                DoorSides.North,
                materials.RoomWall,
                materials.RoomFloor,
                materials.Ceiling
            );

            var recordsDoors = BuildShell(
                expansionRoot.transform,
                "RecordsRoom",
                new Vector3(17f, 0f, 0f),
                new Vector2(6f, 6f),
                DefaultHeight,
                DoorSides.West,
                materials.RoomWall,
                materials.RoomFloor,
                materials.Ceiling
            );

            CreateDoor(expansionRoot.transform, "Door_Room2_To_Wing", bridgeDoors[DoorSides.West], Quaternion.identity);
            CreateDoor(expansionRoot.transform, "Door_Bridge_To_Corridor", bridgeDoors[DoorSides.East], Quaternion.identity);
            CreateDoor(expansionRoot.transform, "Door_Corridor_To_Observation", corridorDoors[DoorSides.North], Quaternion.Euler(0f, 90f, 0f));
            CreateDoor(expansionRoot.transform, "Door_Corridor_To_Storage", corridorDoors[DoorSides.South], Quaternion.Euler(0f, 90f, 0f));
            CreateDoor(expansionRoot.transform, "Door_Corridor_To_Records", corridorDoors[DoorSides.East], Quaternion.identity);

            DecorateObservationRoom(expansionRoot.transform);
            DecorateStorageRoom(expansionRoot.transform);
            DecorateRecordsRoom(expansionRoot.transform);
            DecorateCorridor(expansionRoot.transform);

            CreateLampLight(expansionRoot.transform, "BridgeLight", new Vector3(2f, 2.7f, 0f), new Color(0.82f, 0.88f, 1f), 2.6f, 7.5f);
            CreateLampLight(expansionRoot.transform, "CorridorLight_A", new Vector3(6.5f, 2.7f, 0f), new Color(0.78f, 0.86f, 1f), 2.9f, 8f);
            CreateLampLight(expansionRoot.transform, "CorridorLight_B", new Vector3(11.5f, 2.7f, 0f), new Color(0.76f, 0.84f, 1f), 2.8f, 8f);
            CreateLampLight(expansionRoot.transform, "ObservationLight", new Vector3(9f, 2.65f, 6f), new Color(1f, 0.95f, 0.82f), 2.2f, 7f);
            CreateLampLight(expansionRoot.transform, "StorageLight", new Vector3(9f, 2.55f, -6f), new Color(0.88f, 0.92f, 1f), 1.9f, 6.5f);
            CreateLampLight(expansionRoot.transform, "RecordsLight", new Vector3(17f, 2.65f, 0f), new Color(1f, 0.93f, 0.84f), 2.4f, 7f);

            EditorSceneManager.MarkSceneDirty(demoScene);
            EditorSceneManager.SaveScene(demoScene);

            Debug.Log("Demo scene map expansion applied successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to expand demo scene map: " + ex);
            throw;
        }
    }

    private static void DecorateObservationRoom(Transform parent)
    {
        CreatePrefabInstance(BedPath, parent, "ObservationBed", new Vector3(7.55f, 0f, 7.4f), Quaternion.Euler(0f, 90f, 0f), 1f);
        CreatePrefabInstance(CabinetPath, parent, "ObservationCabinet", new Vector3(10.95f, 0f, 7.95f), Quaternion.Euler(0f, 180f, 0f), 1f);
        CreatePrefabInstance(MapPicturePath, parent, "ObservationMap", new Vector3(11.75f, 1.65f, 6f), Quaternion.Euler(0f, -90f, 0f), 1f);
        CreatePrefabInstance(SplatterPath, parent, "ObservationSplatter", new Vector3(6.25f, 0.02f, 8.2f), Quaternion.Euler(90f, 0f, 0f), 1.2f);
    }

    private static void DecorateStorageRoom(Transform parent)
    {
        CreatePrefabInstance(LockerPath, parent, "StorageLocker_A", new Vector3(7.1f, 0f, -8.2f), Quaternion.identity, 1f);
        CreatePrefabInstance(LockerPath, parent, "StorageLocker_B", new Vector3(8.35f, 0f, -8.2f), Quaternion.identity, 1f);
        CreatePrefabInstance(BoxGroupPath, parent, "StorageBoxes", new Vector3(10.75f, 0f, -7.7f), Quaternion.Euler(0f, 35f, 0f), 1f);
        CreatePrefabInstance(CabinetPath, parent, "StorageCabinet", new Vector3(11.6f, 0f, -4.45f), Quaternion.Euler(0f, -90f, 0f), 0.95f);
    }

    private static void DecorateRecordsRoom(Transform parent)
    {
        CreatePrefabInstance(TablePath, parent, "RecordsTable", new Vector3(17.2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 1f);
        CreatePrefabInstance(ChairPath, parent, "RecordsChair_A", new Vector3(16.35f, 0f, 1.05f), Quaternion.Euler(0f, 35f, 0f), 1f);
        CreatePrefabInstance(ChairPath, parent, "RecordsChair_B", new Vector3(18.1f, 0f, -1.05f), Quaternion.Euler(0f, 215f, 0f), 1f);
        CreatePrefabInstance(CabinetPath, parent, "RecordsCabinet", new Vector3(19.8f, 0f, 2.15f), Quaternion.Euler(0f, 180f, 0f), 1f);
        CreatePrefabInstance(MapPicturePath, parent, "RecordsMap", new Vector3(14.2f, 1.55f, 0f), Quaternion.Euler(0f, 90f, 0f), 1f);
    }

    private static void DecorateCorridor(Transform parent)
    {
        CreatePrefabInstance(LampPath, parent, "CorridorLamp_A", new Vector3(6.5f, 2.92f, 0f), Quaternion.identity, 1f);
        CreatePrefabInstance(LampPath, parent, "CorridorLamp_B", new Vector3(11.5f, 2.92f, 0f), Quaternion.identity, 1f);
        CreatePrefabInstance(BoxGroupPath, parent, "CorridorBoxes", new Vector3(13.4f, 0f, -0.9f), Quaternion.Euler(0f, 20f, 0f), 0.8f);
    }

    private static void CreateLampLight(
        Transform parent,
        string name,
        Vector3 localPosition,
        Color color,
        float intensity,
        float range
    )
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.Soft;
    }

    private static Dictionary<DoorSides, Vector3> BuildShell(
        Transform parent,
        string name,
        Vector3 center,
        Vector2 size,
        float height,
        DoorSides doorSides,
        Material wallMaterial,
        Material floorMaterial,
        Material ceilingMaterial
    )
    {
        GameObject shellRoot = new GameObject(name);
        shellRoot.transform.SetParent(parent, false);
        shellRoot.transform.localPosition = center;

        float halfWidth = size.x * 0.5f;
        float halfDepth = size.y * 0.5f;
        float wallHeight = height * 0.5f;

        CreateBox("Floor", shellRoot.transform, new Vector3(0f, -FloorThickness * 0.5f, 0f), new Vector3(size.x, FloorThickness, size.y), floorMaterial);
        CreateBox("Ceiling", shellRoot.transform, new Vector3(0f, height + FloorThickness * 0.5f, 0f), new Vector3(size.x, FloorThickness, size.y), ceilingMaterial);

        BuildHorizontalWall(shellRoot.transform, "NorthWall", new Vector3(0f, wallHeight, halfDepth), size.x, doorSides.HasFlag(DoorSides.North), wallMaterial);
        BuildHorizontalWall(shellRoot.transform, "SouthWall", new Vector3(0f, wallHeight, -halfDepth), size.x, doorSides.HasFlag(DoorSides.South), wallMaterial);
        BuildVerticalWall(shellRoot.transform, "EastWall", new Vector3(halfWidth, wallHeight, 0f), size.y, doorSides.HasFlag(DoorSides.East), wallMaterial);
        BuildVerticalWall(shellRoot.transform, "WestWall", new Vector3(-halfWidth, wallHeight, 0f), size.y, doorSides.HasFlag(DoorSides.West), wallMaterial);

        return new Dictionary<DoorSides, Vector3>
        {
            [DoorSides.North] = shellRoot.transform.TransformPoint(new Vector3(0f, 0f, halfDepth)),
            [DoorSides.South] = shellRoot.transform.TransformPoint(new Vector3(0f, 0f, -halfDepth)),
            [DoorSides.East] = shellRoot.transform.TransformPoint(new Vector3(halfWidth, 0f, 0f)),
            [DoorSides.West] = shellRoot.transform.TransformPoint(new Vector3(-halfWidth, 0f, 0f)),
        };
    }

    private static void BuildHorizontalWall(
        Transform parent,
        string name,
        Vector3 center,
        float width,
        bool hasDoor,
        Material material
    )
    {
        if (!hasDoor)
        {
            CreateBox(name, parent, center, new Vector3(width, DefaultHeight, WallThickness), material);
            return;
        }

        float sideWidth = (width - DoorWidth) * 0.5f;
        float headerHeight = Mathf.Max(0.35f, DefaultHeight - DoorHeight);
        float sideOffset = (DoorWidth + sideWidth) * 0.5f;
        float headerCenterY = DoorHeight + headerHeight * 0.5f;

        CreateBox(name + "_Left", parent, center + new Vector3(-sideOffset, 0f, 0f), new Vector3(sideWidth, DefaultHeight, WallThickness), material);
        CreateBox(name + "_Right", parent, center + new Vector3(sideOffset, 0f, 0f), new Vector3(sideWidth, DefaultHeight, WallThickness), material);
        CreateBox(name + "_Header", parent, new Vector3(center.x, headerCenterY, center.z), new Vector3(DoorWidth, headerHeight, WallThickness), material);
    }

    private static void BuildVerticalWall(
        Transform parent,
        string name,
        Vector3 center,
        float depth,
        bool hasDoor,
        Material material
    )
    {
        if (!hasDoor)
        {
            CreateBox(name, parent, center, new Vector3(WallThickness, DefaultHeight, depth), material);
            return;
        }

        float sideDepth = (depth - DoorWidth) * 0.5f;
        float headerHeight = Mathf.Max(0.35f, DefaultHeight - DoorHeight);
        float sideOffset = (DoorWidth + sideDepth) * 0.5f;
        float headerCenterY = DoorHeight + headerHeight * 0.5f;

        CreateBox(name + "_Top", parent, center + new Vector3(0f, 0f, sideOffset), new Vector3(WallThickness, DefaultHeight, sideDepth), material);
        CreateBox(name + "_Bottom", parent, center + new Vector3(0f, 0f, -sideOffset), new Vector3(WallThickness, DefaultHeight, sideDepth), material);
        CreateBox(name + "_Header", parent, new Vector3(center.x, headerCenterY, center.z), new Vector3(WallThickness, headerHeight, DoorWidth), material);
    }

    private static GameObject CreateBox(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material
    )
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = localScale;

        MeshRenderer renderer = box.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return box;
    }

    private static void CreateDoor(Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        GameObject instance = CreatePrefabInstance(DoorPath, parent, name, parent.InverseTransformPoint(position), rotation, 1f);
        if (instance == null)
        {
            return;
        }

        Transform leaf = instance
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(candidate => candidate.name.IndexOf("wing", StringComparison.OrdinalIgnoreCase) >= 0);

        if (leaf == null)
        {
            leaf = instance.transform;
        }

        EnsureInteractiveDoor(leaf.gameObject);
    }

    private static void EnsureInteractiveDoor(GameObject doorLeaf)
    {
        AudioClip openClip = LoadAsset<AudioClip>(OpenDoorClipPath);
        AudioClip closeClip = LoadAsset<AudioClip>(CloseDoorClipPath);

        var audioSource = doorLeaf.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = doorLeaf.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.2f;
            audioSource.maxDistance = 11f;
        }

        if (doorLeaf.GetComponent<Collider>() == null)
        {
            doorLeaf.AddComponent<BoxCollider>();
        }

        var doorInteractable = doorLeaf.GetComponent<DoorInteractable>();
        if (doorInteractable == null)
        {
            doorInteractable = doorLeaf.AddComponent<DoorInteractable>();
        }

        var serializedDoor = new SerializedObject(doorInteractable);
        serializedDoor.FindProperty("openSound").objectReferenceValue = openClip;
        serializedDoor.FindProperty("closeSound").objectReferenceValue = closeClip;
        serializedDoor.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedDoor.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreatePrefabInstance(
        string assetPath,
        Transform parent,
        string instanceName,
        Vector3 localPosition,
        Quaternion localRotation,
        float scale
    )
    {
        GameObject prefab = LoadAsset<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing prefab at path: " + assetPath);
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = instanceName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static void RemoveRootIfExists(Scene scene, string rootName)
    {
        GameObject root = FindRoot(scene, rootName);
        if (root != null)
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private sealed class MaterialSet
    {
        public Material CorridorWall { get; set; }
        public Material RoomWall { get; set; }
        public Material CorridorFloor { get; set; }
        public Material RoomFloor { get; set; }
        public Material Ceiling { get; set; }
    }
}

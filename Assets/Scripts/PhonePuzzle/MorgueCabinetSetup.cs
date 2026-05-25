using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MorgueCabinetSetup : MonoBehaviour
{
    private const string Puzzle5SceneName = "demoScene_Difficile";
#if UNITY_EDITOR
    private const string DefaultCabinetOpenCloseClipPath = "Assets/audio/cabinet-open-close.mp3";
#endif

    [Header("Scene References")]
    [SerializeField] private Transform slotsParent;
    [SerializeField] private Puzzle5MorgueController puzzleController;

    [Header("Default Audio")]
    [SerializeField] private AudioClip cabinetOpenSound;
    [SerializeField] private AudioClip cabinetCloseSound;

    [Header("Setup")]
    [SerializeField] private bool runSetupOnAwake = true;
    [SerializeField] private List<MorgueCabinetDoor> cabinets = new List<MorgueCabinetDoor>();

    public IReadOnlyList<MorgueCabinetDoor> Cabinets => cabinets;

    private void Awake()
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (runSetupOnAwake)
        {
            SetupCabinets();
        }
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultCabinetAudio();
    }

    public void ConfigureSceneReferences(Transform parent, Puzzle5MorgueController controller)
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            return;
        }

        if (parent != null)
        {
            slotsParent = parent;
        }

        if (controller != null)
        {
            puzzleController = controller;
        }

        SetupCabinets();
    }

    public List<MorgueCabinetDoor> SetupCabinets()
    {
        TryAssignEditorDefaultCabinetAudio();

        if (!IsPuzzle5Scene())
        {
            return cabinets;
        }

        ResolveReferences();
        cabinets.Clear();

        for (int i = 0; i <= 9; i++)
        {
            Transform cabinetTransform = FindCabinetTransform(i);
            if (cabinetTransform == null)
            {
                Debug.LogWarning("Puzzle 5 missing cabinet warning: Cabinet " + i + " was not found.");
                continue;
            }

            MorgueCabinetDoor door = cabinetTransform.GetComponent<MorgueCabinetDoor>();
            if (door == null)
            {
                door = cabinetTransform.gameObject.AddComponent<MorgueCabinetDoor>();
                Debug.Log("Puzzle 5 added MorgueCabinetDoor to Cabinet " + i + ".");
            }

            door.Configure(i, puzzleController, cabinetOpenSound, cabinetCloseSound);
            door.EnsureInteractionCollider();
            door.SetInteractionEnabled(false);
            cabinets.Add(door);
        }

        if (puzzleController != null)
        {
            puzzleController.RegisterCabinets(cabinets);
        }

        Debug.Log("MorgueCabinetSetup configured " + cabinets.Count + "/10 morgue cabinets.");
        return cabinets;
    }

    private void ResolveReferences()
    {
        if (slotsParent == null)
        {
            slotsParent = ResolveSlotsParent();
        }

        if (puzzleController == null)
        {
            puzzleController = GetComponent<Puzzle5MorgueController>();
        }

        if (puzzleController == null)
        {
            puzzleController = GetComponentInParent<Puzzle5MorgueController>();
        }

        if (puzzleController == null)
        {
            puzzleController = FindFirstObjectByType<Puzzle5MorgueController>(FindObjectsInactive.Include);
        }
    }

    private Transform ResolveSlotsParent()
    {
        if (name == "slots")
        {
            return transform;
        }

        Transform childSlots = FindChildRecursiveExact(transform, "slots");
        if (childSlots != null)
        {
            return childSlots;
        }

        Transform exactSlots = FindExactTransform("slots");
        if (exactSlots != null)
        {
            return exactSlots;
        }

        Transform morgue = FindExactTransform("morgue");
        if (morgue != null)
        {
            return FindChildRecursiveExact(morgue, "slots");
        }

        return null;
    }

    private Transform FindCabinetTransform(int number)
    {
        string cabinetName = "Cabinet " + number;

        if (slotsParent != null)
        {
            Transform child = FindChildRecursiveExact(slotsParent, cabinetName);
            if (child != null)
            {
                return child;
            }
        }

        return FindExactTransform(cabinetName);
    }

    private static Transform FindExactTransform(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindChildRecursiveExact(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursiveExact(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void TryAssignEditorDefaultCabinetAudio()
    {
#if UNITY_EDITOR
        bool changed = false;
        AudioClip defaultClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultCabinetOpenCloseClipPath);
        if (defaultClip == null)
        {
            return;
        }

        if (cabinetOpenSound == null)
        {
            cabinetOpenSound = defaultClip;
            changed = true;
        }

        if (cabinetCloseSound == null)
        {
            cabinetCloseSound = defaultClip;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private static bool IsPuzzle5Scene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == Puzzle5SceneName;
    }
}

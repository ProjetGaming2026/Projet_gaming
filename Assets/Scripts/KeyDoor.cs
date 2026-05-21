using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KeyDoor : MonoBehaviour, IInteractable
{
    [Header("Lower Floor Room Key")]
    [SerializeField] private bool consumeKeyOnUnlock = true;
    [SerializeField] private string lockedPrompt = "Appuie sur E pour deverrouiller avec la cle de chambre";
    [SerializeField] private string unlockedOpenPrompt = "Appuie sur E pour ouvrir";
    [SerializeField] private string unlockedClosePrompt = "Appuie sur E pour fermer";

    [Header("Door Link")]
    [SerializeField] private DoorInteractable linkedDoor;
    [SerializeField] private DoorController linkedDoorController;

    [Header("Fallback Motion")]
    [SerializeField] private float fallbackOpenAngle = 95f;
    [SerializeField] private float fallbackOpenDuration = 0.85f;
    [SerializeField] private Transform fallbackMotionTarget;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip lockedSound;

    private static bool lowerFloorRoomDoorUnlocked;

    private Quaternion fallbackClosedRotation;
    private Quaternion fallbackOpenRotation;
    private Coroutine fallbackMotionRoutine;
    private bool fallbackOpen;
    private bool referencesResolved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        lowerFloorRoomDoorUnlocked = false;
        RoomKeyDoorHud.Reset();
    }

    private void Awake()
    {
        ResolveReferences();
        RefreshFallbackRotations();
    }

    public void SetFallbackMotionTarget(Transform motionTarget)
    {
        fallbackMotionTarget = motionTarget != null ? motionTarget : transform;
        fallbackOpen = false;
        RefreshFallbackRotations();
    }

    public void Interact()
    {
        ResolveReferences();

        if (!lowerFloorRoomDoorUnlocked)
        {
            InventorySystem inventory = InventorySystem.Instance;
            if (inventory == null || !inventory.HasLowerFloorRoomKey())
            {
                PlayOneShot(lockedSound);
                RoomKeyDoorHud.Show("CLE DE CHAMBRE REQUISE", "Cette porte mene a la chambre de l'etage inferieur.", true);
                Debug.Log("KeyDoor: door3b locked. Lower floor room key is missing.");
                return;
            }

            lowerFloorRoomDoorUnlocked = true;
            if (consumeKeyOnUnlock)
            {
                inventory.UseLowerFloorRoomKey();
            }

            PlayOneShot(unlockSound);
            RoomKeyDoorHud.Show("PORTE DEVERROUILLEE", "Cle de chambre utilisee.", false);
            Debug.Log("KeyDoor: door3b unlocked with the lower floor room key.");
        }

        ToggleDoor();
    }

    public string GetInteractPrompt()
    {
        ResolveReferences();

        if (!lowerFloorRoomDoorUnlocked)
        {
            return lockedPrompt;
        }

        if (linkedDoor != null)
        {
            return linkedDoor.IsOpen ? unlockedClosePrompt : unlockedOpenPrompt;
        }

        return fallbackOpen ? unlockedClosePrompt : unlockedOpenPrompt;
    }

    private void ToggleDoor()
    {
        if (linkedDoor != null)
        {
            linkedDoor.Interact();
            return;
        }

        if (linkedDoorController != null && !fallbackOpen)
        {
            linkedDoorController.OpenDoor();
            fallbackOpen = true;
            return;
        }

        Transform motionTarget = GetFallbackMotionTarget();
        if (motionTarget == null)
        {
            return;
        }

        Quaternion targetRotation = fallbackOpen ? fallbackClosedRotation : fallbackOpenRotation;
        fallbackOpen = !fallbackOpen;

        if (fallbackMotionRoutine != null)
        {
            StopCoroutine(fallbackMotionRoutine);
        }

        fallbackMotionRoutine = StartCoroutine(RotateFallbackDoor(motionTarget, targetRotation));
    }

    private IEnumerator RotateFallbackDoor(Transform motionTarget, Quaternion targetRotation)
    {
        if (motionTarget == null)
        {
            yield break;
        }

        Quaternion startRotation = motionTarget.localRotation;
        float elapsed = 0f;

        while (elapsed < fallbackOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fallbackOpenDuration));
            t = t * t * (3f - 2f * t);
            motionTarget.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        motionTarget.localRotation = targetRotation;
        fallbackMotionRoutine = null;
    }

    private Transform GetFallbackMotionTarget()
    {
        return fallbackMotionTarget != null ? fallbackMotionTarget : transform;
    }

    private void RefreshFallbackRotations()
    {
        Transform motionTarget = GetFallbackMotionTarget();
        if (motionTarget == null)
        {
            return;
        }

        fallbackClosedRotation = motionTarget.localRotation;
        fallbackOpenRotation = fallbackClosedRotation * Quaternion.Euler(0f, fallbackOpenAngle, 0f);
    }

    private void ResolveReferences()
    {
        if (referencesResolved && (linkedDoor != null || linkedDoorController != null))
        {
            return;
        }

        if (linkedDoor == null)
        {
            linkedDoor = GetComponent<DoorInteractable>();
        }

        if (linkedDoor == null)
        {
            linkedDoor = GetComponentInChildren<DoorInteractable>(true);
        }

        if (linkedDoor == null && transform.parent != null)
        {
            linkedDoor = GetComponentInParent<DoorInteractable>();
        }

        if (linkedDoorController == null)
        {
            linkedDoorController = GetComponent<DoorController>();
        }

        if (linkedDoorController == null)
        {
            linkedDoorController = GetComponentInChildren<DoorController>(true);
        }

        if (linkedDoorController == null && transform.parent != null)
        {
            linkedDoorController = GetComponentInParent<DoorController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>(true);
        }

        referencesResolved = true;
        Debug.Log("KeyDoor: configured on " + name + ". Linked DoorInteractable: " + (linkedDoor != null) + ", DoorController: " + (linkedDoorController != null) + ".");
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}

public static class LowerFloorRoomDoorBootstrap
{
    private const string TargetSceneName = "demoScene_Facile";
    private const string DoorName = "door3b";
    private const string DoorFrameName = "door3_frame";
    private const string RotationMarkerName = "point rotation";
    private static bool registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!registered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            registered = true;
        }

        ConfigureScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        Transform targetDoorRoot = FindTargetDoorRoot(scene);
        if (targetDoorRoot == null)
        {
            Debug.LogWarning("LowerFloorRoomDoorBootstrap: target door3b with point rotation marker not found. No room key door configured.");
            return;
        }

        Transform movingDoorPanel = FindMovingDoorPanel(targetDoorRoot);
        int removedCount = RemoveKeyDoorFromOtherDoor3B(scene, targetDoorRoot);
        bool wasConfigured = false;
        KeyDoor keyDoor = targetDoorRoot.GetComponent<KeyDoor>();
        if (keyDoor == null)
        {
            keyDoor = targetDoorRoot.gameObject.AddComponent<KeyDoor>();
            wasConfigured = true;
        }

        keyDoor.SetFallbackMotionTarget(movingDoorPanel != null ? movingDoorPanel : targetDoorRoot);

        if (wasConfigured || removedCount > 0)
        {
            string motionTargetName = movingDoorPanel != null ? movingDoorPanel.name : targetDoorRoot.name;
            Debug.Log("LowerFloorRoomDoorBootstrap: KeyDoor is limited to the door3b that contains point rotation. Motion target: " + motionTargetName + ". Removed from other door3b objects: " + removedCount + ".");
        }
    }

    private static Transform FindTargetDoorRoot(Scene scene)
    {
        if (!string.Equals(scene.name, TargetSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (IsTargetDoorRoot(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static int RemoveKeyDoorFromOtherDoor3B(Scene scene, Transform targetDoor)
    {
        int removedCount = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == targetDoor || !IsNamedDoor3B(candidate))
                {
                    continue;
                }

                KeyDoor keyDoor = candidate.GetComponent<KeyDoor>();
                if (keyDoor == null)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(keyDoor);
                removedCount++;
            }
        }

        return removedCount;
    }

    private static bool IsTargetDoorRoot(Transform candidate)
    {
        if (!IsNamedDoor3B(candidate))
        {
            return false;
        }

        return HasDirectChild(candidate, DoorFrameName) &&
               HasDirectChild(candidate, RotationMarkerName) &&
               FindMovingDoorPanel(candidate) != null;
    }

    private static bool IsNamedDoor3B(Transform candidate)
    {
        return candidate != null && string.Equals(candidate.name, DoorName, StringComparison.OrdinalIgnoreCase);
    }

    private static Transform FindMovingDoorPanel(Transform doorRoot)
    {
        if (doorRoot == null)
        {
            return null;
        }

        for (int i = 0; i < doorRoot.childCount; i++)
        {
            Transform child = doorRoot.GetChild(i);
            if (!IsNamedDoor3B(child))
            {
                continue;
            }

            if (child.GetComponent<MeshFilter>() != null && child.GetComponent<Collider>() != null)
            {
                return child;
            }
        }

        return FindDirectChild(doorRoot, DoorName);
    }

    private static bool HasDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (string.Equals(parent.GetChild(i).name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDescendant(Transform parent, string descendantName)
    {
        Transform[] descendants = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] == parent)
            {
                continue;
            }

            if (string.Equals(descendants[i].name, descendantName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

internal static class RoomKeyDoorHud
{
    private static Canvas canvas;
    private static TMP_Text titleText;
    private static TMP_Text bodyText;
    private static Coroutine hideRoutine;
    private static RoomKeyDoorHudRunner runner;

    public static void Reset()
    {
        canvas = null;
        titleText = null;
        bodyText = null;
        hideRoutine = null;
        runner = null;
    }

    public static void Show(string title, string body, bool isWarning)
    {
        EnsureHud();

        titleText.text = title;
        bodyText.text = body;
        titleText.color = isWarning ? new Color(1f, 0.28f, 0.18f, 1f) : new Color(1f, 0.74f, 0.2f, 1f);
        canvas.gameObject.SetActive(true);

        if (hideRoutine != null)
        {
            runner.StopCoroutine(hideRoutine);
        }

        hideRoutine = runner.StartCoroutine(HideAfterDelay());
    }

    private static IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(2.4f);

        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }

        hideRoutine = null;
    }

    private static void EnsureHud()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("RoomKeyDoorHud");
        UnityEngine.Object.DontDestroyOnLoad(hudObject);
        runner = hudObject.AddComponent<RoomKeyDoorHudRunner>();

        canvas = hudObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        CanvasScaler scaler = hudObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        hudObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = CreateUiObject("MessagePanel", hudObject.transform);
        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0.02f, 0.018f, 0.015f, 0.9f);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 128f);
        panelRect.sizeDelta = new Vector2(650f, 104f);

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.68f, 0.22f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        titleText = CreateText("Title", panelObject.transform, 25f, FontStyles.Bold);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(26f, -6f);
        titleRect.offsetMax = new Vector2(-26f, -12f);

        bodyText = CreateText("Body", panelObject.transform, 19f, FontStyles.Normal);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 0.5f);
        bodyRect.offsetMin = new Vector2(26f, 16f);
        bodyRect.offsetMax = new Vector2(-26f, 4f);
        bodyText.color = new Color(0.9f, 0.9f, 0.86f, 1f);

        canvas.gameObject.SetActive(false);
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float size, FontStyles style)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}

internal sealed class RoomKeyDoorHudRunner : MonoBehaviour
{
}

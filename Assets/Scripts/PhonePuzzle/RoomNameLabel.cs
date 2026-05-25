using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class RoomNameLabel : MonoBehaviour
{
    private const string TextChildName = "RoomName_Text_ROOM_B12";

    [SerializeField] private string labelText = "ROOM B12";
    [SerializeField] private Vector3 labelLocalPosition = new Vector3(0f, 0.03f, 0f);
    [SerializeField] private Vector3 labelLocalEuler = new Vector3(90f, 0f, 0f);
    [SerializeField, Min(0.001f)] private float labelLocalScale = 0.075f;
    [SerializeField, Min(0.1f)] private float fontSize = 5.8f;
    [SerializeField] private Color textColor = new Color(0.025f, 0.014f, 0.008f, 1f);
    [SerializeField] private TextMeshPro textMesh;

    private void Awake()
    {
        EnsureLabel();
    }

    private void OnEnable()
    {
        EnsureLabel();
    }

    private void OnValidate()
    {
        EnsureLabel();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying)
        {
            RoomNameLabelPlayModeSettings.Capture(this);
        }
    }
#endif

    public void EnsureLabel()
    {
        bool createdTextMesh = false;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (textMesh == null)
        {
            Transform existing = transform.Find(TextChildName);
            if (existing != null)
            {
                textMesh = existing.GetComponent<TextMeshPro>();
            }
        }

        if (textMesh == null)
        {
            GameObject textObject = new GameObject(TextChildName, typeof(TextMeshPro));
            textObject.transform.SetParent(transform, false);
            textMesh = textObject.GetComponent<TextMeshPro>();
            createdTextMesh = true;
        }

        textMesh.gameObject.SetActive(true);
        textMesh.enabled = true;

        Renderer textRenderer = textMesh.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.enabled = true;
        }

        Transform textTransform = textMesh.transform;
        if (createdTextMesh)
        {
            textTransform.localPosition = labelLocalPosition;
            textTransform.localRotation = Quaternion.Euler(labelLocalEuler);
            textTransform.localScale = Vector3.one * labelLocalScale;
        }

        textMesh.text = labelText;
        if (createdTextMesh)
        {
            textMesh.fontSize = fontSize;
        }

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = textColor;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.enableCulling = false;
        if (createdTextMesh)
        {
            textMesh.rectTransform.sizeDelta = new Vector2(6.5f, 1.6f);
        }

        textMesh.ForceMeshUpdate();
    }

#if UNITY_EDITOR
    public TextMeshPro EditorTextMesh
    {
        get
        {
            EnsureLabel();
            return textMesh;
        }
    }

    public void ApplyEditorSettings(Vector3 localPosition, Vector3 localEuler, Vector3 localScale, float capturedFontSize, Vector2 rectSize)
    {
        EnsureLabel();
        if (textMesh == null)
        {
            return;
        }

        textMesh.transform.localPosition = localPosition;
        textMesh.transform.localRotation = Quaternion.Euler(localEuler);
        textMesh.transform.localScale = localScale;
        textMesh.fontSize = capturedFontSize;
        textMesh.rectTransform.sizeDelta = rectSize;
        textMesh.text = labelText;
        textMesh.gameObject.SetActive(true);
        textMesh.enabled = true;
        textMesh.ForceMeshUpdate();

        labelLocalPosition = localPosition;
        labelLocalEuler = localEuler;
        labelLocalScale = Mathf.Max(0.001f, (localScale.x + localScale.y + localScale.z) / 3f);
        fontSize = capturedFontSize;

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(textMesh);
    }
#endif
}

public static class RoomNameLabelBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        Transform roomName = FindExactObject("RoomName");
        if (roomName == null)
        {
            return;
        }

        RoomNameLabel label = roomName.GetComponent<RoomNameLabel>();
        if (label == null)
        {
            label = roomName.gameObject.AddComponent<RoomNameLabel>();
        }

        label.EnsureLabel();
    }

    private static Transform FindExactObject(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public static class RoomNameLabelEditorInstaller
{
    static RoomNameLabelEditorInstaller()
    {
        EditorApplication.delayCall += InstallInOpenScene;
        EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += InstallInOpenScene;
    }

    private static void InstallInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        Transform roomName = FindExactObject("RoomName");
        if (roomName == null)
        {
            return;
        }

        RoomNameLabel label = roomName.GetComponent<RoomNameLabel>();
        if (label == null)
        {
            label = Undo.AddComponent<RoomNameLabel>(roomName.gameObject);
        }

        label.EnsureLabel();
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static Transform FindExactObject(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }
}

public static class RoomNameLabelPlayModeSettings
{
    private const string HasSettingsKey = "ProjetGaming.RoomNameLabel.HasSettings";
    private const string PositionKey = "ProjetGaming.RoomNameLabel.Position";
    private const string EulerKey = "ProjetGaming.RoomNameLabel.Euler";
    private const string ScaleKey = "ProjetGaming.RoomNameLabel.Scale";
    private const string FontSizeKey = "ProjetGaming.RoomNameLabel.FontSize";
    private const string RectSizeKey = "ProjetGaming.RoomNameLabel.RectSize";

    static RoomNameLabelPlayModeSettings()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static void Capture(RoomNameLabel label)
    {
        if (label == null)
        {
            return;
        }

        TextMeshPro text = label.EditorTextMesh;
        if (text == null)
        {
            return;
        }

        SaveVector3(PositionKey, text.transform.localPosition);
        SaveVector3(EulerKey, text.transform.localEulerAngles);
        SaveVector3(ScaleKey, text.transform.localScale);
        SaveFloat(FontSizeKey, text.fontSize);
        SaveVector2(RectSizeKey, text.rectTransform.sizeDelta);
        SessionState.SetBool(HasSettingsKey, true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            RoomNameLabel label = Object.FindFirstObjectByType<RoomNameLabel>(FindObjectsInactive.Include);
            Capture(label);
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += ApplyCapturedSettings;
        }
    }

    private static void ApplyCapturedSettings()
    {
        if (!SessionState.GetBool(HasSettingsKey, false))
        {
            return;
        }

        RoomNameLabel label = Object.FindFirstObjectByType<RoomNameLabel>(FindObjectsInactive.Include);
        if (label == null)
        {
            return;
        }

        label.ApplyEditorSettings(
            LoadVector3(PositionKey, Vector3.zero),
            LoadVector3(EulerKey, new Vector3(90f, 0f, 0f)),
            LoadVector3(ScaleKey, Vector3.one * 0.075f),
            LoadFloat(FontSizeKey, 5.8f),
            LoadVector2(RectSizeKey, new Vector2(6.5f, 1.6f)));

        EditorSceneManager.MarkSceneDirty(label.gameObject.scene);
        Debug.Log("RoomNameLabel copied Play Mode ROOM B12 text settings into Edit Mode. Save the scene with Ctrl+S.");
    }

    private static void SaveFloat(string key, float value)
    {
        SessionState.SetFloat(key, value);
    }

    private static float LoadFloat(string key, float fallback)
    {
        return SessionState.GetFloat(key, fallback);
    }

    private static void SaveVector2(string key, Vector2 value)
    {
        SessionState.SetFloat(key + ".x", value.x);
        SessionState.SetFloat(key + ".y", value.y);
    }

    private static Vector2 LoadVector2(string key, Vector2 fallback)
    {
        return new Vector2(
            SessionState.GetFloat(key + ".x", fallback.x),
            SessionState.GetFloat(key + ".y", fallback.y));
    }

    private static void SaveVector3(string key, Vector3 value)
    {
        SessionState.SetFloat(key + ".x", value.x);
        SessionState.SetFloat(key + ".y", value.y);
        SessionState.SetFloat(key + ".z", value.z);
    }

    private static Vector3 LoadVector3(string key, Vector3 fallback)
    {
        return new Vector3(
            SessionState.GetFloat(key + ".x", fallback.x),
            SessionState.GetFloat(key + ".y", fallback.y),
            SessionState.GetFloat(key + ".z", fallback.z));
    }
}
#endif

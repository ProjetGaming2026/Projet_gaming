using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class TVPuzzleSceneBootstrap
{
    private const string MediumSceneName = "demoScene_Moyen";
    private const string ScreenObjectName = "TV_Screen";
    private const string TriggerName = "TV_Interaction_Trigger";
    private const string CanvasName = "TV_Code_Canvas";
    private const string NotebookName = "Notebook_Under_Bed";
    private const string CorrectCode = "1234";

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
            CleanupRuntimeTvPuzzleObjects(scene);
            return;
        }

        TelevisionVideoBootstrap televisionVideo = Object.FindFirstObjectByType<TelevisionVideoBootstrap>();
        GameObject screenObject = FindSceneObjectByName(scene, ScreenObjectName);
        if (televisionVideo == null || screenObject == null)
        {
            return;
        }

        RuntimeUi runtimeUi = FindOrCreateUi(screenObject);
        Transform player = FindPlayer();
        BasicFirstPersonController playerController = player != null ? player.GetComponent<BasicFirstPersonController>() : null;
        EntityHuntTrigger huntTrigger = FindOrCreateHuntTrigger();
        GameObject notebook = FindOrCreateNotebook(scene, screenObject);
        TVCodePuzzleController controller = FindOrCreateInteractionTrigger(screenObject, player);

        controller.Configure(
            CorrectCode,
            runtimeUi.PressEText,
            runtimeUi.CodeCanvas,
            runtimeUi.CodeInput,
            runtimeUi.WrongCodeText,
            televisionVideo,
            notebook,
            huntTrigger,
            screenObject.transform,
            player,
            playerController);

        runtimeUi.SubmitButton.onClick.RemoveAllListeners();
        runtimeUi.SubmitButton.onClick.AddListener(controller.CheckCode);
        runtimeUi.CloseButton.onClick.RemoveAllListeners();
        runtimeUi.CloseButton.onClick.AddListener(controller.CloseCodeUI);

        Debug.Log("TVPuzzleSceneBootstrap: TV puzzle connected.");
    }

    private static bool IsTelevisionPuzzleScene(Scene scene)
    {
        return scene.name == MediumSceneName;
    }

    private static void CleanupRuntimeTvPuzzleObjects(Scene scene)
    {
        DestroySceneObjectIfFound(scene, TriggerName);
        DestroySceneObjectIfFound(scene, CanvasName);
        DestroySceneObjectIfFound(scene, "TV_Prompt_Canvas");
        DestroySceneObjectIfFound(scene, "TV_Entity_Hunt_Trigger");
        DestroySceneObjectIfFound(scene, NotebookName);
    }

    private static void DestroySceneObjectIfFound(Scene scene, string objectName)
    {
        GameObject target = FindSceneObjectByName(scene, objectName);
        if (target != null)
        {
            Object.Destroy(target);
        }
    }

    private static TVCodePuzzleController FindOrCreateInteractionTrigger(GameObject screenObject, Transform player)
    {
        GameObject triggerObject = GameObject.Find(TriggerName);
        if (triggerObject == null)
        {
            triggerObject = new GameObject(TriggerName);
        }

        Renderer screenRenderer = screenObject.GetComponent<Renderer>();
        if (screenRenderer == null)
        {
            screenRenderer = screenObject.GetComponentInChildren<Renderer>(true);
        }

        Bounds screenBounds = screenRenderer != null
            ? screenRenderer.bounds
            : new Bounds(screenObject.transform.position, new Vector3(1.8f, 1f, 0.35f));

        triggerObject.transform.position = screenBounds.center;
        triggerObject.transform.rotation = Quaternion.identity;

        BoxCollider triggerCollider = triggerObject.GetComponent<BoxCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = triggerObject.AddComponent<BoxCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(
            Mathf.Max(screenBounds.size.x + 0.45f, 1.4f),
            Mathf.Max(screenBounds.size.y + 0.45f, 0.9f),
            Mathf.Max(screenBounds.size.z + 0.45f, 0.55f));
        triggerCollider.center = Vector3.zero;

        Rigidbody triggerBody = triggerObject.GetComponent<Rigidbody>();
        if (triggerBody == null)
        {
            triggerBody = triggerObject.AddComponent<Rigidbody>();
        }

        triggerBody.isKinematic = true;
        triggerBody.useGravity = false;

        TVCodePuzzleController controller = triggerObject.GetComponent<TVCodePuzzleController>();
        if (controller == null)
        {
            controller = triggerObject.AddComponent<TVCodePuzzleController>();
        }

        return controller;
    }

    private static EntityHuntTrigger FindOrCreateHuntTrigger()
    {
        EntityHuntTrigger huntTrigger = Object.FindFirstObjectByType<EntityHuntTrigger>();
        if (huntTrigger == null)
        {
            GameObject triggerObject = new GameObject("TV_Entity_Hunt_Trigger");
            huntTrigger = triggerObject.AddComponent<EntityHuntTrigger>();
        }

        BasicMonsterController monsterController = Object.FindFirstObjectByType<BasicMonsterController>();
        GameObject entity = monsterController != null ? monsterController.gameObject : GameObject.Find("Entity");
        huntTrigger.Configure(entity, monsterController);
        return huntTrigger;
    }

    private static RuntimeUi FindOrCreateUi(GameObject screenObject)
    {
        EnsureEventSystem();

        GameObject promptCanvasObject = GameObject.Find("TV_Prompt_Canvas");
        if (promptCanvasObject == null)
        {
            promptCanvasObject = new GameObject("TV_Prompt_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas promptCanvas = promptCanvasObject.GetComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 100;

        CanvasScaler scaler = promptCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject pressEObject = FindChild(promptCanvasObject.transform, "Press_E_Text");
        if (pressEObject == null)
        {
            pressEObject = CreateText(promptCanvasObject.transform, "Press_E_Text", "Press E to enter code", 34f, TextAlignmentOptions.Center, Color.white).gameObject;
            RectTransform pressRect = pressEObject.GetComponent<RectTransform>();
            pressRect.anchorMin = new Vector2(0.5f, 0f);
            pressRect.anchorMax = new Vector2(0.5f, 0f);
            pressRect.pivot = new Vector2(0.5f, 0f);
            pressRect.anchoredPosition = new Vector2(0f, 150f);
            pressRect.sizeDelta = new Vector2(520f, 70f);
        }

        GameObject canvasObject = GameObject.Find(CanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CanvasScaler codeScaler = canvasObject.GetComponent<CanvasScaler>();
        codeScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        GameObject panel = FindChild(canvasObject.transform, "Panel");
        if (panel == null)
        {
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(320f, 180f);
            panel.GetComponent<Image>().color = new Color(0.015f, 0.018f, 0.018f, 0.96f);
        }

        TVScreenOverlayFollower follower = panel.GetComponent<TVScreenOverlayFollower>();
        if (follower == null)
        {
            follower = panel.AddComponent<TVScreenOverlayFollower>();
        }

        follower.Configure(screenObject.transform);

        TMP_Text title = EnsureText(panel.transform, "Title", "PATIENT ARCHIVE", 20f, TextAlignmentOptions.Center, new Color(0.78f, 1f, 0.92f, 1f));
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(280f, 28f));

        TMP_Text subtitle = EnsureText(panel.transform, "Subtitle", "ENTER CODE", 12f, TextAlignmentOptions.Center, new Color(0.48f, 0.75f, 0.68f, 1f));
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(260f, 18f));

        TMP_InputField input = EnsureInput(panel.transform);
        SetRect(input.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(190f, 32f));

        TMP_Text wrongCodeText = EnsureText(panel.transform, "Wrong_Code_Text", "ACCESS DENIED", 13f, TextAlignmentOptions.Center, new Color(1f, 0.18f, 0.12f, 1f));
        SetRect(wrongCodeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(190f, 20f));

        Button submitButton = EnsureButton(panel.transform, "Submit_Button", "CONFIRM");
        SetRect(submitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-54f, 22f), new Vector2(96f, 30f));

        Button closeButton = EnsureButton(panel.transform, "Close_Button", "CLOSE");
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(54f, 22f), new Vector2(96f, 30f));

        canvasObject.SetActive(true);
        panel.SetActive(false);
        pressEObject.SetActive(false);
        wrongCodeText.gameObject.SetActive(false);

        return new RuntimeUi
        {
            CodeCanvas = panel,
            PressEText = pressEObject,
            CodeInput = input,
            WrongCodeText = wrongCodeText.gameObject,
            SubmitButton = submitButton,
            CloseButton = closeButton
        };
    }

    private static void PositionCodeCanvasOnTelevision(Transform canvasTransform, GameObject screenObject)
    {
        Renderer screenRenderer = screenObject.GetComponent<Renderer>();
        if (screenRenderer == null)
        {
            screenRenderer = screenObject.GetComponentInChildren<Renderer>(true);
        }

        Bounds screenBounds = screenRenderer != null
            ? screenRenderer.bounds
            : new Bounds(screenObject.transform.position, new Vector3(1.6f, 0.9f, 0.1f));

        Vector3 normal = ResolveScreenNormal(screenObject, screenRenderer);
        Vector3 up = ResolveScreenUp(screenObject, screenRenderer, normal);
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform != null && Vector3.Dot(normal, cameraTransform.position - screenBounds.center) < 0f)
        {
            normal = -normal;
        }

        canvasTransform.position = screenBounds.center + normal * 0.018f;
        canvasTransform.rotation = Quaternion.LookRotation(normal, up);

        float screenWidth = Mathf.Max(screenBounds.size.x, screenBounds.size.z, 1.2f);
        float screenHeight = Mathf.Max(screenBounds.size.y, 0.65f);
        float worldScale = Mathf.Min(screenWidth / 1280f, screenHeight / 720f) * 0.86f;
        canvasTransform.localScale = Vector3.one * worldScale;
    }

    private static Vector3 ResolveScreenNormal(GameObject screenObject, Renderer screenRenderer)
    {
        MeshFilter meshFilter = screenRenderer != null ? screenRenderer.GetComponent<MeshFilter>() : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return -screenObject.transform.forward;
        }

        Vector3 size = meshFilter.sharedMesh.bounds.size;
        int depthAxis = GetSmallestAxis(size);
        return screenObject.transform.TransformDirection(AxisVector(depthAxis)).normalized;
    }

    private static Vector3 ResolveScreenUp(GameObject screenObject, Renderer screenRenderer, Vector3 normal)
    {
        MeshFilter meshFilter = screenRenderer != null ? screenRenderer.GetComponent<MeshFilter>() : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return Vector3.up;
        }

        Vector3 size = meshFilter.sharedMesh.bounds.size;
        int depthAxis = GetSmallestAxis(size);
        int horizontalAxis = GetLargestAxis(size, depthAxis);
        int verticalAxis = GetRemainingAxis(depthAxis, horizontalAxis);
        Vector3 up = screenObject.transform.TransformDirection(AxisVector(verticalAxis)).normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f)
        {
            up = -up;
        }

        if (Mathf.Abs(Vector3.Dot(up, normal)) > 0.92f)
        {
            return Vector3.up;
        }

        return up;
    }

    private static int GetSmallestAxis(Vector3 size)
    {
        if (size.x <= size.y && size.x <= size.z)
        {
            return 0;
        }

        return size.y <= size.z ? 1 : 2;
    }

    private static int GetLargestAxis(Vector3 size, int excludedAxis)
    {
        int largestAxis = -1;
        float largestSize = float.MinValue;
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis == excludedAxis)
            {
                continue;
            }

            float axisSize = AxisValue(size, axis);
            if (axisSize > largestSize)
            {
                largestSize = axisSize;
                largestAxis = axis;
            }
        }

        return largestAxis;
    }

    private static int GetRemainingAxis(int firstAxis, int secondAxis)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            if (axis != firstAxis && axis != secondAxis)
            {
                return axis;
            }
        }

        return 1;
    }

    private static Vector3 AxisVector(int axis)
    {
        if (axis == 0)
        {
            return Vector3.right;
        }

        return axis == 1 ? Vector3.up : Vector3.forward;
    }

    private static float AxisValue(Vector3 value, int axis)
    {
        if (axis == 0)
        {
            return value.x;
        }

        return axis == 1 ? value.y : value.z;
    }

    private static TMP_InputField EnsureInput(Transform parent)
    {
        GameObject inputObject = FindChild(parent, "Code_Input");
        if (inputObject == null)
        {
            inputObject = new GameObject("Code_Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);
            inputObject.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.88f, 1f);
        }

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        if (input == null)
        {
            input = inputObject.AddComponent<TMP_InputField>();
        }

        GameObject textArea = FindChild(inputObject.transform, "Text Area");
        if (textArea == null)
        {
            textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObject.transform, false);
        }

        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        SetRect(textAreaRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(-36f, -18f));
        textAreaRect.offsetMin = new Vector2(18f, 9f);
        textAreaRect.offsetMax = new Vector2(-18f, -9f);

        TMP_Text inputText = EnsureText(textArea.transform, "Text", string.Empty, 20f, TextAlignmentOptions.Center, Color.black);
        SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        inputText.rectTransform.offsetMin = Vector2.zero;
        inputText.rectTransform.offsetMax = Vector2.zero;

        TMP_Text placeholder = EnsureText(textArea.transform, "Placeholder", "----", 20f, TextAlignmentOptions.Center, new Color(0f, 0f, 0f, 0.35f));
        SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;

        input.textViewport = textAreaRect;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 8;
        return input;
    }

    private static Button EnsureButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = FindChild(parent, name);
        if (buttonObject == null)
        {
            buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.85f, 0.82f, 0.75f, 1f);
        }

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text labelText = EnsureText(buttonObject.transform, "Text", label, 12f, TextAlignmentOptions.Center, Color.black);
        SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = FindChild(parent, name);
        if (textObject == null)
        {
            return CreateText(parent, name, text, size, alignment, color);
        }

        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        if (textComponent == null)
        {
            textComponent = textObject.AddComponent<TextMeshProUGUI>();
        }

        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.alignment = alignment;
        textComponent.color = color;
        return textComponent;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text textComponent = textObject.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.fontSize = size;
        textComponent.alignment = alignment;
        textComponent.color = color;
        return textComponent;
    }

    private static GameObject FindOrCreateNotebook(Scene scene, GameObject screenObject)
    {
        GameObject notebook = FindSceneObjectByName(scene, NotebookName);
        if (notebook != null)
        {
            EnsureNotebookPickup(notebook);
            return notebook;
        }

        notebook = FindSceneObjectContainingName(scene, "BidaultSarah_Notebook");
        if (notebook != null)
        {
            notebook.name = NotebookName;
            EnsureNotebookPickup(notebook);
            return notebook;
        }

#if UNITY_EDITOR
        GameObject notebookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Television_Puzzle/old-notebook/source/BidaultSarah_Notebook_low.fbx");
        if (notebookPrefab != null)
        {
            notebook = Object.Instantiate(notebookPrefab);
            notebook.name = NotebookName;
            notebook.transform.position = ResolveNotebookPosition(scene, screenObject);
            notebook.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            notebook.transform.localScale = Vector3.one;
            EnsureNotebookPickup(notebook);
            return notebook;
        }
#endif

        notebook = GameObject.CreatePrimitive(PrimitiveType.Cube);
        notebook.name = NotebookName;
        notebook.transform.position = ResolveNotebookPosition(scene, screenObject);
        notebook.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        notebook.transform.localScale = new Vector3(0.42f, 0.035f, 0.28f);

        Renderer renderer = notebook.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.18f, 0.12f, 0.08f, 1f);
        }

        EnsureNotebookPickup(notebook);
        return notebook;
    }

    private static void EnsureNotebookPickup(GameObject notebook)
    {
        if (notebook.GetComponent<NotebookPickup>() == null)
        {
            notebook.AddComponent<NotebookPickup>();
        }
    }

    private static Vector3 ResolveNotebookPosition(Scene scene, GameObject screenObject)
    {
        GameObject mattress = FindSceneObjectByName(scene, "matrass");
        Renderer mattressRenderer = mattress != null ? mattress.GetComponentInChildren<Renderer>() : null;
        if (mattressRenderer != null)
        {
            Bounds bounds = mattressRenderer.bounds;
            return bounds.center + new Vector3(0f, -bounds.extents.y - 0.04f, 0.2f);
        }

        return screenObject.transform.position + new Vector3(0f, -0.8f, -1.2f);
    }

    private static Transform FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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

    private static GameObject FindSceneObjectContainingName(Scene scene, string objectNamePart)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildContainingName(root.transform, objectNamePart);
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

    private static Transform FindChildContainingName(Transform current, string objectNamePart)
    {
        if (current.name.Contains(objectNamePart))
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildContainingName(child, objectNamePart);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform parent, string childName)
    {
        Transform found = parent.Find(childName);
        return found != null ? found.gameObject : null;
    }

    private struct RuntimeUi
    {
        public GameObject CodeCanvas;
        public GameObject PressEText;
        public TMP_InputField CodeInput;
        public GameObject WrongCodeText;
        public Button SubmitButton;
        public Button CloseButton;
    }
}

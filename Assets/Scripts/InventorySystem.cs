using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Inventory")]
    [SerializeField] private bool hasLowerFloorRoomKey;

    [Header("UI")]
    [SerializeField] private Color panelColor = new Color(0.025f, 0.027f, 0.03f, 0.86f);
    [SerializeField] private Color slotEmptyColor = new Color(0.08f, 0.085f, 0.09f, 0.9f);
    [SerializeField] private Color slotFilledColor = new Color(0.15f, 0.12f, 0.055f, 0.95f);
    [SerializeField] private Color accentColor = new Color(1f, 0.72f, 0.22f, 1f);

    private Canvas inventoryCanvas;
    private Image keySlotImage;
    private TMP_Text keyIconText;
    private TMP_Text keyLabelText;
    private TMP_Text notificationText;
    private Coroutine notificationRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildInventoryUi();
        RefreshUi();
    }

    public void PickupKey()
    {
        PickupLowerFloorRoomKey();
    }

    public void PickupLowerFloorRoomKey()
    {
        hasLowerFloorRoomKey = true;
        RefreshUi();
        ShowNotification("CLE DE CHAMBRE - ETAGE INFERIEUR AJOUTEE");
        Debug.Log("InventorySystem: lower floor room key picked up.");
    }

    public bool HasKey()
    {
        return hasLowerFloorRoomKey;
    }

    public bool HasLowerFloorRoomKey()
    {
        return hasLowerFloorRoomKey;
    }

    public void UseKey()
    {
        UseLowerFloorRoomKey();
    }

    public void UseLowerFloorRoomKey()
    {
        hasLowerFloorRoomKey = false;
        RefreshUi();
        Debug.Log("InventorySystem: lower floor room key used.");
    }

    private void BuildInventoryUi()
    {
        GameObject canvasObject = new GameObject("InventoryCanvas");
        canvasObject.transform.SetParent(transform, false);

        inventoryCanvas = canvasObject.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = CreateUiObject("InventoryPanel", canvasObject.transform);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-34f, 32f);
        panelRect.sizeDelta = new Vector2(240f, 108f);

        GameObject slotObject = CreateUiObject("KeySlot", panelObject.transform);
        keySlotImage = slotObject.AddComponent<Image>();
        keySlotImage.color = slotEmptyColor;
        RectTransform slotRect = keySlotImage.rectTransform;
        slotRect.anchorMin = new Vector2(0f, 0.5f);
        slotRect.anchorMax = new Vector2(0f, 0.5f);
        slotRect.pivot = new Vector2(0f, 0.5f);
        slotRect.anchoredPosition = new Vector2(18f, 0f);
        slotRect.sizeDelta = new Vector2(74f, 74f);
        AddOutline(slotObject, accentColor, new Vector2(1.5f, -1.5f));

        keyIconText = CreateText("KeyIcon", slotObject.transform, "-", 34f, accentColor);
        Stretch(keyIconText.rectTransform);
        keyIconText.alignment = TextAlignmentOptions.Center;

        keyLabelText = CreateText("KeyLabel", panelObject.transform, "CLE\nABSENTE", 20f, Color.white);
        RectTransform labelRect = keyLabelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(108f, 18f);
        labelRect.offsetMax = new Vector2(-14f, -18f);
        keyLabelText.alignment = TextAlignmentOptions.MidlineLeft;

        notificationText = CreateText("InventoryNotification", canvasObject.transform, "", 24f, accentColor);
        RectTransform notificationRect = notificationText.rectTransform;
        notificationRect.anchorMin = new Vector2(0.5f, 0f);
        notificationRect.anchorMax = new Vector2(0.5f, 0f);
        notificationRect.pivot = new Vector2(0.5f, 0f);
        notificationRect.anchoredPosition = new Vector2(0f, 158f);
        notificationRect.sizeDelta = new Vector2(760f, 54f);
        notificationText.alignment = TextAlignmentOptions.Center;
        notificationText.gameObject.SetActive(false);
    }

    private void RefreshUi()
    {
        if (keySlotImage == null || keyIconText == null || keyLabelText == null)
        {
            return;
        }

        keySlotImage.color = hasLowerFloorRoomKey ? slotFilledColor : slotEmptyColor;
        keyIconText.text = hasLowerFloorRoomKey ? "ROOM" : "-";
        keyLabelText.text = hasLowerFloorRoomKey ? "CLE CHAMBRE\nETAGE BAS" : "CLE\nABSENTE";
        keyLabelText.color = hasLowerFloorRoomKey ? accentColor : Color.white;
    }

    private void ShowNotification(string message)
    {
        if (notificationText == null)
        {
            return;
        }

        if (notificationRoutine != null)
        {
            StopCoroutine(notificationRoutine);
        }

        notificationRoutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.2f);
        notificationText.gameObject.SetActive(false);
        notificationRoutine = null;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        uiObject.AddComponent<RectTransform>();
        return uiObject;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TMP_Text label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }
}

public static class InventorySystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInventorySystem()
    {
        if (InventorySystem.Instance != null)
        {
            return;
        }

        GameObject inventoryObject = new GameObject("InventorySystem_Runtime");
        inventoryObject.AddComponent<InventorySystem>();
        Debug.Log("InventorySystemBootstrap: created runtime inventory system.");
    }
}

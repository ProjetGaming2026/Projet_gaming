using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RadioInteraction : MonoBehaviour, IInteractable
{
    [Header("Audio Clips")]
    public AudioClip operaClip;          // creepy background opera/static
    public AudioClip morseClip;          // morse code beeping

    [Header("Audio Sources")]
    public AudioSource radioAudioSource; // main audio source

    [Header("Settings")]
    public float audioPitch = 0.5f;      // morse code speed
    public float operaVolume = 0.4f;     // opera background volume
    public float morseVolume = 1f;       // morse volume

    [Header("UI")]
    public GameObject hintCanvas;
    [FormerlySerializedAs("hintText")]
    public Component hintTextComponent;
    public string listenMessage = "Listen carefully...";
    public float hintDuration = 5f;

    private bool isPlayingMorse;

    private void Start()
    {
        ApplyEasySceneMessaging();
        ResolveHintReferences();
        HideHint();
        StartOpera();
    }

    public void Interact()
    {
        if (isPlayingMorse)
        {
            Debug.Log("RadioInteraction: morse is already playing, interaction ignored.");
            return;
        }

        StartMorse();
    }

    private void StartMorse()
    {
        if (radioAudioSource == null)
        {
            Debug.LogWarning("RadioInteraction: missing AudioSource.");
            return;
        }

        if (morseClip == null)
        {
            Debug.LogWarning("RadioInteraction: missing morse clip.");
            return;
        }

        CancelInvoke(nameof(ResumeOpera));
        CancelInvoke(nameof(HideHint));

        isPlayingMorse = true;
        ShowHint();

        radioAudioSource.Stop();
        radioAudioSource.loop = false;
        radioAudioSource.volume = morseVolume;
        radioAudioSource.pitch = audioPitch;
        radioAudioSource.clip = morseClip;
        radioAudioSource.Play();

        float safePitch = Mathf.Max(0.01f, Mathf.Abs(audioPitch));
        float morseLength = morseClip.length / safePitch;
        Invoke(nameof(HideHint), Mathf.Max(0.1f, hintDuration));
        Invoke(nameof(ResumeOpera), morseLength);

        Debug.Log("RadioInteraction: morse started. Replay will be available after it finishes.");
    }

    private void ResumeOpera()
    {
        isPlayingMorse = false;
        StartOpera();
        Debug.Log("RadioInteraction: morse finished. Radio can be replayed with E.");
    }

    private void StartOpera()
    {
        if (operaClip == null || radioAudioSource == null)
        {
            return;
        }

        radioAudioSource.Stop();
        radioAudioSource.loop = true;
        radioAudioSource.volume = operaVolume;
        radioAudioSource.pitch = 1f;
        radioAudioSource.clip = operaClip;
        radioAudioSource.Play();
        Debug.Log("RadioInteraction: opera/static loop started.");
    }

    private void ShowHint()
    {
        ResolveHintReferences();
        SetHintText(listenMessage);

        if (hintCanvas != null)
        {
            hintCanvas.SetActive(true);
        }
    }

    private void HideHint()
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(false);
        }
    }

    private void ResolveHintReferences()
    {
        if (hintCanvas == null)
        {
            GameObject existingHint = GameObject.Find("HintCanvas");
            if (existingHint != null)
            {
                hintCanvas = existingHint;
            }
        }

        if (hintCanvas != null && hintTextComponent == null)
        {
            TMP_Text tmpText = hintCanvas.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                hintTextComponent = tmpText;
            }
            else
            {
                Text legacyText = hintCanvas.GetComponentInChildren<Text>(true);
                if (legacyText != null)
                {
                    hintTextComponent = legacyText;
                }
            }
        }

        if (hintCanvas == null || hintTextComponent == null)
        {
            CreateRuntimeHintCanvas();
        }
    }

    private void CreateRuntimeHintCanvas()
    {
        GameObject canvasObject = new GameObject("HintCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("HintText");
        textObject.transform.SetParent(canvasObject.transform, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = listenMessage;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 170f);
        rect.sizeDelta = new Vector2(620f, 72f);

        hintCanvas = canvasObject;
        hintTextComponent = text;
        Debug.Log("RadioInteraction: created runtime HintCanvas.");
    }

    private void SetHintText(string textValue)
    {
        if (hintTextComponent == null) return;

        // Works with either TMPro or uGUI text components without hard dependency on a scene setup.
        var type = hintTextComponent.GetType();
        PropertyInfo textProperty = type.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty != null && textProperty.CanWrite && textProperty.PropertyType == typeof(string))
        {
            textProperty.SetValue(hintTextComponent, textValue);
            return;
        }

        FieldInfo textField = type.GetField("text", BindingFlags.Instance | BindingFlags.Public);
        if (textField != null && textField.FieldType == typeof(string))
        {
            textField.SetValue(hintTextComponent, textValue);
        }
    }

    private void ApplyEasySceneMessaging()
    {
        if (SceneManager.GetActiveScene().name != "demoScene_Facile")
        {
            return;
        }

        listenMessage = "Ecoute le morse pour trouver le code de sortie.";
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PhoneAnswerInput : MonoBehaviour
{
    public static bool IsAnyAnswerInputOpen { get; private set; }
    public static bool WasAnswerInputCancelledThisFrame => answerInputCancelledFrame == Time.frameCount;

    [Header("Answer")]
    [SerializeField, Min(1)] private int maxAnswerLength = 3;
    [SerializeField] private TMP_Text answerText;

    [Header("Digit Audio")]
    [SerializeField] private AudioSource digitAudioSource;
    [SerializeField] private AudioClip digit0Voice;
    [SerializeField] private AudioClip digit1Voice;
    [SerializeField] private AudioClip digit2Voice;
    [SerializeField] private AudioClip digit3Voice;
    [SerializeField] private AudioClip digit4Voice;
    [SerializeField] private AudioClip digit5Voice;
    [SerializeField] private AudioClip digit6Voice;
    [SerializeField] private AudioClip digit7Voice;
    [SerializeField] private AudioClip digit8Voice;
    [SerializeField] private AudioClip digit9Voice;

    private PhonePuzzleUI ui;
    private bool buttonsBound;
    private bool isActive;
    private string currentAnswer = string.Empty;
    private Action<string> submitCallback;
    private Action cancelCallback;
    private static int answerInputCancelledFrame = -1;

    public string CurrentAnswer => currentAnswer;

    private void Awake()
    {
        TryAssignEditorDefaultDigitAudio();

        if (digitAudioSource == null)
        {
            digitAudioSource = GetComponent<AudioSource>();
        }

        ConfigureDigitAudioSource();
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultDigitAudio();
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAnswer();
            return;
        }

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i)))
            {
                AddDigit(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteLastDigit();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Submit();
        }
    }

    public void BeginAnswerMode(int maximumLength, Action<string> onSubmit, Action onCancel = null)
    {
        ui = PhonePuzzleUI.EnsureExists();
        maxAnswerLength = Mathf.Max(1, maximumLength);
        submitCallback = onSubmit;
        cancelCallback = onCancel;
        currentAnswer = string.Empty;
        isActive = true;
        IsAnyAnswerInputOpen = true;

        if (answerText == null)
        {
            answerText = ui.AnswerInputText;
        }

        BindButtons(ui);
        ui.SetAnswerInputVisible(true);
        UpdateAnswerText();
    }

    public void EndAnswerMode()
    {
        isActive = false;
        IsAnyAnswerInputOpen = false;
        submitCallback = null;
        cancelCallback = null;

        if (ui != null)
        {
            ui.SetAnswerInputVisible(false);
        }
    }

    public void CancelAnswer()
    {
        if (!isActive)
        {
            return;
        }

        Action callback = cancelCallback;
        answerInputCancelledFrame = Time.frameCount;
        EndAnswerMode();
        callback?.Invoke();
        Debug.Log("Phone answer input cancelled with Escape.");
    }

    public void ClearAnswer()
    {
        currentAnswer = string.Empty;
        UpdateAnswerText();
    }

    public void AddDigit(int digit)
    {
        if (!isActive || digit < 0 || digit > 9 || currentAnswer.Length >= maxAnswerLength)
        {
            return;
        }

        currentAnswer += digit.ToString();
        UpdateAnswerText();
        PlayDigitVoice(digit);
    }

    public void DeleteLastDigit()
    {
        if (!isActive || string.IsNullOrEmpty(currentAnswer))
        {
            return;
        }

        currentAnswer = currentAnswer.Substring(0, currentAnswer.Length - 1);
        UpdateAnswerText();
    }

    public void Submit()
    {
        if (!isActive)
        {
            return;
        }

        submitCallback?.Invoke(currentAnswer);
    }

    private void BindButtons(PhonePuzzleUI targetUi)
    {
        if (buttonsBound || targetUi == null)
        {
            return;
        }

        Button[] buttons = targetUi.DigitButtons;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length && i < 10; i++)
            {
                int digit = i;
                if (buttons[i] != null)
                {
                    buttons[i].onClick.AddListener(() => AddDigit(digit));
                }
            }
        }

        if (targetUi.DeleteButton != null)
        {
            targetUi.DeleteButton.onClick.AddListener(DeleteLastDigit);
        }

        if (targetUi.SubmitButton != null)
        {
            targetUi.SubmitButton.onClick.AddListener(Submit);
        }

        buttonsBound = true;
    }

    private void UpdateAnswerText()
    {
        if (answerText != null)
        {
            answerText.text = string.IsNullOrEmpty(currentAnswer) ? "___" : currentAnswer;
        }

        if (ui != null)
        {
            ui.SetAnswerText(currentAnswer);
        }
    }

    private void PlayDigitVoice(int digit)
    {
        AudioClip clip = GetDigitClip(digit);
        if (clip == null)
        {
            return;
        }

        if (digitAudioSource == null)
        {
            digitAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureDigitAudioSource();
        digitAudioSource.PlayOneShot(clip);
    }

    private void ConfigureDigitAudioSource()
    {
        if (digitAudioSource == null)
        {
            return;
        }

        digitAudioSource.playOnAwake = false;
        digitAudioSource.loop = false;
    }

    private AudioClip GetDigitClip(int digit)
    {
        switch (digit)
        {
            case 0: return digit0Voice;
            case 1: return digit1Voice;
            case 2: return digit2Voice;
            case 3: return digit3Voice;
            case 4: return digit4Voice;
            case 5: return digit5Voice;
            case 6: return digit6Voice;
            case 7: return digit7Voice;
            case 8: return digit8Voice;
            case 9: return digit9Voice;
            default: return null;
        }
    }

    private void TryAssignEditorDefaultDigitAudio()
    {
#if UNITY_EDITOR
        bool changed = false;

        changed |= TryAssignDigitClip(0, ref digit0Voice);
        changed |= TryAssignDigitClip(1, ref digit1Voice);
        changed |= TryAssignDigitClip(2, ref digit2Voice);
        changed |= TryAssignDigitClip(3, ref digit3Voice);
        changed |= TryAssignDigitClip(4, ref digit4Voice);
        changed |= TryAssignDigitClip(5, ref digit5Voice);
        changed |= TryAssignDigitClip(6, ref digit6Voice);
        changed |= TryAssignDigitClip(7, ref digit7Voice);
        changed |= TryAssignDigitClip(8, ref digit8Voice);
        changed |= TryAssignDigitClip(9, ref digit9Voice);

        if (changed)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

#if UNITY_EDITOR
    private static bool TryAssignDigitClip(int digit, ref AudioClip targetClip)
    {
        if (targetClip != null)
        {
            return false;
        }

        targetClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/audio/" + digit + ".mp3");
        return targetClip != null;
    }
#endif
}

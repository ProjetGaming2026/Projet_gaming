using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimatedHeartSequence : Interactable
{
    [Header("Heart Animation")]
    [SerializeField] private Animator heartAnimator;
    [SerializeField] private RuntimeAnimatorController heartBeatController;
    [SerializeField] private string animationStateName = "Scene_heart";
    [SerializeField, Min(0.01f)] private float fallbackPulseScale = 1.16f;
    [SerializeField, Min(0.05f)] private float fallbackPulseDuration = 0.22f;
    [SerializeField, Min(0.05f)] private float fallbackRestDuration = 0.42f;

    [Header("Heart Sequence Timing")]
    [SerializeField, Min(0.05f)] private float beatGap = 0.32f;
    [SerializeField, Min(0.1f)] private float longPauseDuration = 2.25f;
    [SerializeField, Min(0.1f)] private float shortPauseDuration = 1.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource heartAudioSource;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private AudioClip heartStaticClip;
    [SerializeField] private AudioClip scaryWhisperSound;

    [Header("Events")]
    public UnityEvent OnHeartSequenceStarted = new UnityEvent();
    public UnityEvent OnHeartBeat = new UnityEvent();
    public UnityEvent OnHeartSequenceFinished = new UnityEvent();

    private HeartRoomPuzzle puzzle;
    private PhonePuzzleUI ui;
    private Coroutine sequenceRoutine;
    private Coroutine fallbackPulseRoutine;
    private Vector3 originalScale;
    private bool useFallbackPulse;
    private bool configured;

#if UNITY_EDITOR
    private const string DefaultHeartbeatClipPath = "Assets/audio/heartBeat.mp3";
#endif

    private void Awake()
    {
        TryAssignEditorDefaultAudio();
        originalScale = transform.localScale;
        ResolveReferences();
        SetHeartInteractable(false);
    }

    private void OnValidate()
    {
        TryAssignEditorDefaultAudio();
    }

    private void OnEnable()
    {
        if (configured)
        {
            PrepareAnimation();
        }
    }

    private void Update()
    {
        KeepAnimatorLooping();
    }

    public void Configure(HeartRoomPuzzle owner, AudioClip beatClip, AudioClip staticClip, AudioClip whisperClip)
    {
        puzzle = owner;
        heartbeatClip ??= beatClip;
        heartStaticClip ??= staticClip;
        scaryWhisperSound ??= whisperClip;
        configured = true;
        ResolveReferences();
        PrepareAnimation();
    }

    public void SetHeartInteractable(bool value)
    {
        SetInteractable(value);
        SetPromptText("Press E to listen");

        if (value)
        {
            PrepareAnimation();
        }
        else
        {
            StopFallbackPulse();
        }
    }

    public override string GetInteractPrompt()
    {
        return "Press E to listen";
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        PlayHeartSequence();
    }

    public void PlayHeartSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(HeartSequenceRoutine());
    }

    private IEnumerator HeartSequenceRoutine()
    {
        ui = PhonePuzzleUI.EnsureExists();
        OnHeartSequenceStarted.Invoke();
        puzzle?.RegisterHeartSequenceStarted();
        PlayOneShot(heartStaticClip);
        ui.Subtitles.ShowTimed("beat beat ... long silence ... beat ... short silence ... beat beat beat", 4.25f);
        Debug.Log("Heart clue: beat beat ... long silence ... beat ... short silence ... beat beat beat");

        yield return Beat();
        yield return new WaitForSeconds(beatGap);
        yield return Beat();
        yield return new WaitForSeconds(longPauseDuration);
        yield return Beat();
        yield return new WaitForSeconds(shortPauseDuration);
        yield return Beat();
        yield return new WaitForSeconds(beatGap);
        yield return Beat();
        yield return new WaitForSeconds(beatGap);
        yield return Beat();

        ui.Subtitles.ShowTimed("The pauses are the answer.", 3f);
        OnHeartSequenceFinished.Invoke();
        puzzle?.RegisterHeartSequenceFinished();
        Debug.Log("Heart clue complete. The pauses are the answer.");
        sequenceRoutine = null;
    }

    private IEnumerator Beat()
    {
        OnHeartBeat.Invoke();
        puzzle?.RegisterHeartBeat();
        PlayOneShot(heartbeatClip);

        if (useFallbackPulse)
        {
            yield return PulseOnce();
        }
        else
        {
            yield return new WaitForSeconds(0.18f);
        }
    }

    private IEnumerator PulseOnce()
    {
        Vector3 pulseScale = originalScale * fallbackPulseScale;
        float halfDuration = fallbackPulseDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(originalScale, pulseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.localScale = Vector3.Lerp(pulseScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator FallbackPulseLoop()
    {
        while (true)
        {
            yield return PulseOnce();
            yield return new WaitForSeconds(fallbackRestDuration);
        }
    }

    private void PrepareAnimation()
    {
        ResolveReferences();

        if (heartAnimator != null)
        {
            if (heartAnimator.runtimeAnimatorController == null && heartBeatController != null)
            {
                heartAnimator.runtimeAnimatorController = heartBeatController;
            }

            if (heartAnimator.runtimeAnimatorController != null)
            {
                useFallbackPulse = false;
                StopFallbackPulse();
                heartAnimator.enabled = true;
                heartAnimator.speed = 1f;
                heartAnimator.Play(animationStateName, 0, 0f);
                Debug.Log("Animated_heart Animator playing " + animationStateName + ".");
                return;
            }
        }

        useFallbackPulse = true;
        if (fallbackPulseRoutine == null && isActiveAndEnabled && CanInteract)
        {
            fallbackPulseRoutine = StartCoroutine(FallbackPulseLoop());
        }

        Debug.LogWarning("Animated_heart has no usable Animator Controller. Using fallback scale pulse.");
    }

    private void KeepAnimatorLooping()
    {
        if (useFallbackPulse || heartAnimator == null || !heartAnimator.enabled || heartAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = heartAnimator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.loop && stateInfo.normalizedTime >= 1f && !heartAnimator.IsInTransition(0))
        {
            heartAnimator.Play(animationStateName, 0, 0f);
        }
    }

    private void StopFallbackPulse()
    {
        if (fallbackPulseRoutine != null)
        {
            StopCoroutine(fallbackPulseRoutine);
            fallbackPulseRoutine = null;
        }

        transform.localScale = originalScale;
    }

    private void ResolveReferences()
    {
        if (heartAnimator == null)
        {
            heartAnimator = GetComponent<Animator>();
        }

        if (heartAnimator == null && heartBeatController != null)
        {
            heartAnimator = gameObject.AddComponent<Animator>();
            heartAnimator.runtimeAnimatorController = heartBeatController;
            Debug.Log("Animated_heart had no Animator, so one was added.");
        }

        EnsureHeartAudioSource();

        if (GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = Vector3.one * 0.35f;
            box.center = Vector3.zero;
            Debug.LogWarning("Animated_heart had no Collider, so a fallback BoxCollider was added.");
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.Log("Animated_heart audio clip missing; continuing without audio.");
            return;
        }

        AudioSource source = EnsureHeartAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private AudioSource EnsureHeartAudioSource()
    {
        try
        {
            if (heartAudioSource != null)
            {
                heartAudioSource.playOnAwake = false;
                return heartAudioSource;
            }
        }
        catch (MissingComponentException)
        {
            heartAudioSource = null;
        }

        heartAudioSource = GetComponent<AudioSource>();
        if (heartAudioSource == null)
        {
            heartAudioSource = gameObject.AddComponent<AudioSource>();
        }

        heartAudioSource.playOnAwake = false;
        return heartAudioSource;
    }

    private void TryAssignEditorDefaultAudio()
    {
#if UNITY_EDITOR
        if (heartbeatClip != null)
        {
            return;
        }

        heartbeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultHeartbeatClipPath);
        if (heartbeatClip != null)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }
}

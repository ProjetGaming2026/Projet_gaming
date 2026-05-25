using UnityEngine;
using UnityEngine.Events;

public class MedicalFileReadable : Interactable
{
    [Header("Readable Medical File")]
    [SerializeField] private string fileTitle = "MEDICAL FILE";
    [SerializeField, TextArea(6, 16)] private string fileBody;
    [SerializeField] private bool isReadable;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fileOpenSound;
    [SerializeField] private AudioClip paperRustleSound;

    [Header("Events")]
    public UnityEvent OnFileRead = new UnityEvent();

    private DoctorReportsPuzzle puzzle;
    private bool hasBeenRead;

    public bool HasBeenRead => hasBeenRead;
    public string FileTitle => fileTitle;
    public string FileBody => fileBody;

    private void Awake()
    {
        EnsureCollider();
        EnsureAudioSource();
        SetPromptText("Press E to read report");
        SetInteractable(isReadable);
    }

    public void Configure(DoctorReportsPuzzle owner, string title, string body, AudioClip openSound, AudioClip rustleSound)
    {
        puzzle = owner;
        fileTitle = title;
        fileBody = body;
        fileOpenSound ??= openSound;
        paperRustleSound ??= rustleSound;
        EnsureCollider();
        EnsureAudioSource();
        SetPromptText("Press E to read report");
    }

    public void SetReadable(bool value)
    {
        isReadable = value;
        SetInteractable(value);
    }

    public override bool CanInteract => isReadable && base.CanInteract;

    public override string GetInteractPrompt()
    {
        return "Press E to read report";
    }

    public override void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        hasBeenRead = true;
        PlayOneShot(paperRustleSound);
        PlayOneShot(fileOpenSound);
        PhonePuzzleUI.EnsureExists().ShowReadableText(fileTitle, fileBody);
        OnFileRead.Invoke();
        puzzle?.RegisterMedicalFileRead(this);
        Debug.Log("Medical report read: " + fileTitle);
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
        {
            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(1f, 0.18f, 1f);
        box.center = Vector3.zero;
        Debug.LogWarning(name + " had no Collider, so a fallback BoxCollider was added.");
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = EnsureAudioSource();
        if (source != null)
        {
            source.PlayOneShot(clip);
        }
    }

    private AudioSource EnsureAudioSource()
    {
        try
        {
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                return audioSource;
            }
        }
        catch (MissingComponentException)
        {
            audioSource = null;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        return audioSource;
    }
}

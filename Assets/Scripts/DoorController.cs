using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum OpenType
    {
        Rotate,
        Slide
    }

    [Header("Open Mode")]
    [SerializeField] private OpenType openType = OpenType.Rotate;
    [SerializeField] private float openDuration = 1.25f;

    [Header("Rotate")]
    [SerializeField] private Vector3 openRotation = new Vector3(0f, 90f, 0f);

    [Header("Slide")]
    [SerializeField] private Vector3 slideDirection = new Vector3(0f, 3f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private float audioVolume = 1f;

    private Vector3 closedLocalPosition;
    private Quaternion closedLocalRotation;
    private Vector3 openLocalPosition;
    private Quaternion openLocalRotation;
    private AudioSource audioSource;
    private Coroutine openRoutine;
    private bool isOpen;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        closedLocalRotation = transform.localRotation;
        openLocalPosition = closedLocalPosition + slideDirection;
        openLocalRotation = closedLocalRotation * Quaternion.Euler(openRotation);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && doorOpenSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        Debug.Log("DoorController: setup complete on " + name + " using " + openType + " mode.");
    }

    public void OpenDoor()
    {
        if (isOpen)
        {
            Debug.Log("DoorController: " + name + " is already open.");
            return;
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        PlayOpenSound();
        openRoutine = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        isOpen = true;
        float duration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;

        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (openType == OpenType.Rotate)
            {
                transform.localRotation = Quaternion.Lerp(startRotation, openLocalRotation, easedT);
            }
            else
            {
                transform.localPosition = Vector3.Lerp(startPosition, openLocalPosition, easedT);
            }

            yield return null;
        }

        if (openType == OpenType.Rotate)
        {
            transform.localRotation = openLocalRotation;
        }
        else
        {
            transform.localPosition = openLocalPosition;
        }

        openRoutine = null;
        Debug.Log("DoorController: " + name + " opened.");
    }

    private void PlayOpenSound()
    {
        if (doorOpenSound == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.PlayOneShot(doorOpenSound, audioVolume);
    }
}

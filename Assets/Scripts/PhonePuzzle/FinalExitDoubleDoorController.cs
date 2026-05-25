using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FinalExitDoubleDoorController : MonoBehaviour
{
    private const string LeftDoorName = "portegauchesortie";
    private const string RightDoorName = "portedroitesortie";

    [Header("Exit Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float leftDoorOpenAngleY = -90f;
    [SerializeField] private float rightDoorOpenAngleY = 90f;
    [SerializeField, Min(0.05f)] private float openDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorOpenSound;

    [Header("Events")]
    public UnityEvent OnExitDoorsOpened = new UnityEvent();

    private Quaternion leftClosedLocalRotation;
    private Quaternion rightClosedLocalRotation;
    private bool closedRotationsCaptured;
    private bool isOpen;
    private bool isOpening;
    private Coroutine openRoutine;

    public Transform LeftDoor => leftDoor;
    public Transform RightDoor => rightDoor;
    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveDoors();
        CaptureClosedRotations();
        EnsureAudioSource();
        ClearStaticFlags(leftDoor);
        ClearStaticFlags(rightDoor);
    }

    public void Configure(Transform left, Transform right, AudioClip openSound)
    {
        if (left != null)
        {
            leftDoor = left;
        }

        if (right != null)
        {
            rightDoor = right;
        }

        if (doorOpenSound == null)
        {
            doorOpenSound = openSound;
        }

        ResolveDoors();
        CaptureClosedRotations();
        EnsureAudioSource();
        ClearStaticFlags(leftDoor);
        ClearStaticFlags(rightDoor);
    }

    public void OpenDoors()
    {
        ResolveDoors();
        CaptureClosedRotations();

        if (isOpen || isOpening)
        {
            return;
        }

        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogWarning("Final exit cannot open because portegauchesortie or portedroitesortie is missing.");
            return;
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        openRoutine = StartCoroutine(OpenDoorsRoutine());
    }

    private IEnumerator OpenDoorsRoutine()
    {
        isOpening = true;
        PlayDoorOpenSound();

        Quaternion leftStartRotation = leftDoor.localRotation;
        Quaternion rightStartRotation = rightDoor.localRotation;
        Quaternion leftTargetRotation = leftClosedLocalRotation * Quaternion.Euler(0f, leftDoorOpenAngleY, 0f);
        Quaternion rightTargetRotation = rightClosedLocalRotation * Quaternion.Euler(0f, rightDoorOpenAngleY, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localRotation = Quaternion.Slerp(leftStartRotation, leftTargetRotation, t);
            rightDoor.localRotation = Quaternion.Slerp(rightStartRotation, rightTargetRotation, t);
            yield return null;
        }

        leftDoor.localRotation = leftTargetRotation;
        rightDoor.localRotation = rightTargetRotation;

        isOpen = true;
        isOpening = false;
        openRoutine = null;
        OnExitDoorsOpened.Invoke();
        Debug.Log("Final exit doors opened: portegauchesortie and portedroitesortie.");
    }

    private void ResolveDoors()
    {
        if (leftDoor == null)
        {
            Transform foundLeftDoor = FindExactTransform(LeftDoorName);
            if (foundLeftDoor != null)
            {
                leftDoor = foundLeftDoor;
            }
            else
            {
                Debug.LogWarning("Final exit left door missing: expected object named " + LeftDoorName + ".");
            }
        }

        if (rightDoor == null)
        {
            Transform foundRightDoor = FindExactTransform(RightDoorName);
            if (foundRightDoor != null)
            {
                rightDoor = foundRightDoor;
            }
            else
            {
                Debug.LogWarning("Final exit right door missing: expected object named " + RightDoorName + ".");
            }
        }
    }

    private void CaptureClosedRotations()
    {
        if (closedRotationsCaptured)
        {
            return;
        }

        if (leftDoor != null)
        {
            leftClosedLocalRotation = leftDoor.localRotation;
        }

        if (rightDoor != null)
        {
            rightClosedLocalRotation = rightDoor.localRotation;
        }

        closedRotationsCaptured = leftDoor != null && rightDoor != null;
    }

    private AudioSource EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null && doorOpenSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }

        return audioSource;
    }

    private void PlayDoorOpenSound()
    {
        if (doorOpenSound == null)
        {
            return;
        }

        AudioSource source = EnsureAudioSource();
        if (source != null)
        {
            source.PlayOneShot(doorOpenSound);
        }
    }

    private static void ClearStaticFlags(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                transforms[i].gameObject.isStatic = false;
            }
        }
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
}

using System.Collections;
using System.Linq;
using UnityEngine;

public class XRayPuzzleManager : MonoBehaviour
{
    public static XRayPuzzleManager Instance { get; private set; }

    [Header("References")]
    public PatientFolder[] allFolders;
    public XRayItem[] allXRays;
    public Animator exitDoorAnimator;
    public Light roomLight;

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip horrorSound;

    [Header("Horror")]
    public GameObject monsterPrefab;
    public Transform monsterSpawnPoint;

    public int solvedCount = 0;
    public bool horrorActive = false;
    public bool puzzleSolved = false;

    private AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("XRayPuzzleManager");
        root.AddComponent<XRayPuzzleManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        RefreshReferences();
        RepositionXRaysForRoom();
    }

    public void RefreshReferences()
    {
        if (allFolders == null || allFolders.Length == 0)
        {
            allFolders = FindObjectsByType<PatientFolder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(folder => folder.patientID)
                .ToArray();
        }

        if (allXRays == null || allXRays.Length == 0)
        {
            allXRays = FindObjectsByType<XRayItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderBy(xray => xray.xrayID)
                .ToArray();
        }
    }

    private void RepositionXRaysForRoom()
    {
        GameObject keypad = GameObject.Find("SM_Pin_Pad");
        if (keypad == null)
        {
            return;
        }

        RefreshReferences();
        if (allXRays == null || allXRays.Length == 0)
        {
            return;
        }

        // Force the X-rays onto the back wall inside the padded room so the next task
        // becomes visible immediately after the keypad door opens.
        Vector3 basePosition = keypad.transform.position + new Vector3(-0.70f, 0.18f, -2.32f);
        Vector3 horizontalStep = new Vector3(0.46f, 0f, 0f);
        Vector3 verticalStep = new Vector3(0f, 0.58f, 0f);
        Quaternion wallRotation = Quaternion.Euler(0f, 180f, 0f);

        for (int i = 0; i < allXRays.Length; i++)
        {
            if (allXRays[i] == null)
            {
                continue;
            }

            int row = i / 2;
            int col = i % 2;
            Transform xrayTransform = allXRays[i].transform;
            xrayTransform.position = basePosition + horizontalStep * col - verticalStep * row;
            xrayTransform.rotation = wallRotation;
            xrayTransform.localScale = new Vector3(0.24f, 0.34f, 1f);
        }
    }

    public bool HasAnyFolderBeenRead()
    {
        RefreshReferences();
        return allFolders != null && allFolders.Any(folder => folder != null && folder.isRead);
    }

    public void OnCorrect()
    {
        if (puzzleSolved)
        {
            return;
        }

        RefreshReferences();
        solvedCount = allXRays.Count(xray => xray != null && xray.isSolved);
        PlayOneShot(successSound);
        XRayUIManager.Instance?.ShowToast($"{solvedCount}/4 correctes", new Color(0.35f, 1f, 0.4f, 1f), 1.6f);

        if (solvedCount >= 4)
        {
            StartCoroutine(SuccessSequence());
        }
    }

    public void TriggerHorror()
    {
        if (horrorActive || puzzleSolved)
        {
            return;
        }

        StartCoroutine(HorrorSequence());
    }

    public IEnumerator SuccessSequence()
    {
        puzzleSolved = true;
        PlayOneShot(successSound);
        XRayUIManager.Instance?.ShowToast("PUZZLE RESOLU !", new Color(0.35f, 1f, 0.4f, 1f), 2.5f);

        if (roomLight != null)
        {
            float elapsed = 0f;
            float startIntensity = Mathf.Max(0.2f, roomLight.intensity);
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                roomLight.intensity = Mathf.Lerp(startIntensity, 2f, elapsed / 2f);
                yield return null;
            }

            roomLight.intensity = 2f;
        }

        yield return new WaitForSeconds(1f);

        if (exitDoorAnimator != null)
        {
            exitDoorAnimator.SetTrigger("Open");
        }
    }

    public IEnumerator HorrorSequence()
    {
        horrorActive = true;
        PlayOneShot(horrorSound);
        XRayUIManager.Instance?.ShowToast("MAUVAISE ASSOCIATION", new Color(1f, 0.25f, 0.25f, 1f), 2f);

        if (roomLight != null)
        {
            for (int i = 0; i < 6; i++)
            {
                roomLight.enabled = false;
                yield return new WaitForSeconds(0.1f);
                roomLight.enabled = true;
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return StartCoroutine(ShakeCamera(0.5f, 0.1f));
        yield return new WaitForSeconds(0.3f);

        if (monsterPrefab != null && monsterSpawnPoint != null)
        {
            Instantiate(monsterPrefab, monsterSpawnPoint.position, monsterSpawnPoint.rotation);
        }

        yield return new WaitForSeconds(10f);
        horrorActive = false;
    }

    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (Camera.main == null)
        {
            yield break;
        }

        Transform target = Camera.main.transform;
        Vector3 originalLocalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * magnitude;
            target.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        target.localPosition = originalLocalPosition;
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

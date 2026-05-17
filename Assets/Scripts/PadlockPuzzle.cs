using UnityEngine;

public class PadlockPuzzle : MonoBehaviour, IInteractable
{
    [Header("Code Settings")]
    public int[] correctCode = { 1, 2, 1, 0 };

    [Header("References")]
    public DialInteraction[] dials;
    public Animator padlockAnimator;
    public ChestPuzzle chest;

    [Header("Camera Zoom")]
    public Transform playerCamera;
    public Transform zoomTarget;
    public float zoomSpeed = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dialClickSound;
    public AudioClip unlockSound;

    private bool isInFocus;
    private bool isSolved;
    private int selectedDial;
    private bool isZooming;
    private bool isReturning; // camera qui revient

    // Sauvegarde LOCAL (par rapport au parent = Player)
    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Interact()
    {
        if (isSolved) return;

        ResolveReferences();

        if (playerCamera == null || zoomTarget == null)
        {
            Debug.LogWarning("PadlockPuzzle: playerCamera or zoomTarget is missing.");
            return;
        }

        if (!isInFocus)
        {
            isInFocus = true;
            isZooming = true;
            isReturning = false;

            // Sauvegarder position LOCALE par rapport au Player
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("PadlockPuzzle: focus enabled.");
        }
    }

    private void Update()
    {
        if (isSolved && isReturning)
        {
            // Camera revient à sa position originale
            playerCamera.localPosition = Vector3.Lerp(
                playerCamera.localPosition,
                originalCamLocalPos,
                Time.deltaTime * zoomSpeed
            );

            playerCamera.localRotation = Quaternion.Lerp(
                playerCamera.localRotation,
                originalCamLocalRot,
                Time.deltaTime * zoomSpeed
            );

            // Stop quand assez proche
            if (Vector3.Distance(playerCamera.localPosition, originalCamLocalPos) < 0.01f)
            {
                playerCamera.localPosition = originalCamLocalPos;
                playerCamera.localRotation = originalCamLocalRot;
                isReturning = false;
                Debug.Log("Camera revenue à sa position!");
            }
            return;
        }

        if (!isInFocus || isSolved) return;

        // Zoom vers le padlock
        if (isZooming)
        {
            playerCamera.position = Vector3.Lerp(
                playerCamera.position,
                zoomTarget.position,
                Time.deltaTime * zoomSpeed
            );

            playerCamera.rotation = Quaternion.Lerp(
                playerCamera.rotation,
                zoomTarget.rotation,
                Time.deltaTime * zoomSpeed
            );
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitFocus();
            return;
        }

        // Q = roulette gauche, E = roulette droite
        if (Input.GetKeyDown(KeyCode.Q))
        {
            selectedDial = Mathf.Max(0, selectedDial - 1);
            Debug.Log("Roulette sélectionnée: " + selectedDial);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            selectedDial = Mathf.Min(dials.Length - 1, selectedDial + 1);
            Debug.Log("Roulette sélectionnée: " + selectedDial);
        }

        // Molette souris
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            if (HasSelectedDial())
            {
                dials[selectedDial].ScrollUp();
                PlayDialSound();
            }
        }

        if (scroll < 0f)
        {
            if (HasSelectedDial())
            {
                dials[selectedDial].ScrollDown();
                PlayDialSound();
            }
        }
    }

    public void CheckCode()
    {
        if (dials == null || dials.Length < correctCode.Length)
        {
            Debug.LogWarning("PadlockPuzzle: dials are not configured.");
            return;
        }

        string entered = "";
        string expected = "";

        for (int i = 0; i < correctCode.Length; i++)
        {
            entered += dials[i].GetNumber();
            expected += correctCode[i];
        }

        Debug.Log("PadlockPuzzle: checking " + entered + " expected " + expected);

        for (int i = 0; i < correctCode.Length; i++)
        {
            if (dials[i].GetNumber() != correctCode[i])
                return;
        }

        Unlock();
    }

    private void Unlock()
    {
        isSolved = true;
        isInFocus = false;
        isZooming = false;
        isReturning = true; // ← camera revient!

        if (audioSource != null && unlockSound != null)
            audioSource.PlayOneShot(unlockSound);

        if (padlockAnimator != null)
            padlockAnimator.SetTrigger("Open");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Invoke(nameof(HidePadlock), 1f);
        Invoke(nameof(OpenChest), 1.2f);

        Debug.Log("PadlockPuzzle: unlocked! Camera returning...");
    }

    private void HidePadlock()
    {
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            mr.enabled = false;
    }

    private void OpenChest()
    {
        if (chest != null)
            chest.OpenFromPadlock();
        else
            Debug.LogWarning("PadlockPuzzle: chest is missing.");
    }

    private void ExitFocus()
    {
        isInFocus = false;
        isZooming = false;
        isReturning = true; // ← camera revient aussi quand Escape

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("PadlockPuzzle: focus disabled. Camera returning...");
    }

    private void ResolveReferences()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (dials == null || dials.Length == 0)
            dials = GetComponentsInChildren<DialInteraction>(true);

        if (dials != null)
        {
            for (int i = 0; i < dials.Length; i++)
            {
                if (dials[i] != null)
                    dials[i].padlockPuzzle = this;
            }
        }

        if (chest == null)
            chest = GetComponentInParent<ChestPuzzle>();
    }

    private bool HasSelectedDial()
    {
        return dials != null &&
               dials.Length > 0 &&
               selectedDial >= 0 &&
               selectedDial < dials.Length &&
               dials[selectedDial] != null;
    }

    private void PlayDialSound()
    {
        if (audioSource != null && dialClickSound != null)
            audioSource.PlayOneShot(dialClickSound);
    }
}
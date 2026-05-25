using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClueItem : MonoBehaviour, IInteractable
{
    [Header("Clue")]
    public string clueText;
    public string clueDescription;
    public bool isFound = false;

    [Header("Feedback")]
    [SerializeField] private AudioClip discoverySound;
    [SerializeField] private float disableDelay = 2f;
    [SerializeField] private float interactionDistance = 2f;

    private AudioSource audioSource;

    private void Awake()
    {
        Collider clueCollider = GetComponent<Collider>();
        if (clueCollider != null && clueCollider.isTrigger)
        {
            clueCollider.isTrigger = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        if (isFound || KeypadUIManager.IsUiOpen)
        {
            return;
        }

        Transform viewer = Camera.main != null ? Camera.main.transform : null;
        if (viewer == null)
        {
            return;
        }

        float distance = Vector3.Distance(viewer.position, transform.position);
        if (distance <= interactionDistance)
        {
            KeypadUIManager.Instance?.ShowInteractionPrompt("Appuyer E pour interagir", this);
        }
        else
        {
            KeypadUIManager.Instance?.HideInteractionPrompt(this);
        }
    }

    public void Interact()
    {
        if (isFound)
        {
            return;
        }

        if (CodePuzzleManager.Instance == null)
        {
            Debug.LogWarning($"[ClueItem] No CodePuzzleManager found for {name}.", this);
            return;
        }

        isFound = true;
        CodePuzzleManager.Instance.AddClue(clueText, clueDescription);
        KeypadUIManager.Instance?.ShowToast($"Indice decouvert ! {clueDescription}", 2f);
        PlayDiscoverySound();
        Debug.Log($"[ClueItem] Clue found: {clueDescription}", this);
        StartCoroutine(DisableLater());
    }

    private IEnumerator DisableLater()
    {
        yield return new WaitForSeconds(disableDelay);
        gameObject.SetActive(false);
    }

    private void PlayDiscoverySound()
    {
        if (discoverySound == null || audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = discoverySound;
        audioSource.Play();
    }
}

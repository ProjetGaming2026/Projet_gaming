using UnityEngine;

public class ChestPuzzle : MonoBehaviour, IInteractable
{
    [Header("References")]
    public Animator chestAnimator;
    public GameObject padlock;
    public GameObject keyItem;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;

    private bool isSolved = false;

    void Start()
    {
        if (keyItem != null)
            keyItem.SetActive(false);
    }

    public void Interact()
    {
        if (isSolved) return;
        Debug.Log("Chest is locked!");
    }

    public void OpenFromPadlock()
    {
        Debug.Log("OpenFromPadlock called!");
        if (isSolved) return;
        isSolved = true;

        if (chestAnimator != null)
            chestAnimator.SetTrigger("Open");
        else
            Debug.Log("❌ chestAnimator is NULL!");

        if (audioSource != null && correctSound != null)
            audioSource.PlayOneShot(correctSound);

        Invoke(nameof(RevealItem), 2f);
    }

    void RevealItem()
    {
        if (keyItem != null)
            keyItem.SetActive(true);
        Debug.Log("✅ Item revealed!");
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class ButtonAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    private TextMeshProUGUI label;

    [Header("Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    private AudioSource audioSource;

    [Header("Couleurs")]
    public Color normalColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color hoverColor = Color.white;
    public Color selectedColor = new Color(0.91f, 0.84f, 0.63f, 1f);

    [Header("Spacing")]
    public float normalSpacing = 8f;
    public float hoverSpacing = 14f;
    public float animSpeed = 8f;

    private float targetSpacing;
    private Color targetColor;

    void Awake()
    {
        label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.color = normalColor;
            label.characterSpacing = normalSpacing;
        }
        targetSpacing = normalSpacing;
        targetColor = normalColor;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        ApplySfxVolume(AudioManager.SfxVolume);
    }

    void Update()
    {
        if (label == null)
        {
            return;
        }

        label.characterSpacing = Mathf.Lerp(
            label.characterSpacing, targetSpacing, Time.deltaTime * animSpeed);
        label.color = Color.Lerp(
            label.color, targetColor, Time.deltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        targetSpacing = hoverSpacing;
        targetColor = hoverColor;
        if (hoverSound != null)
        {
            ApplySfxVolume(AudioManager.SfxVolume);
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        targetSpacing = normalSpacing;
        targetColor = normalColor;
    }

    public void OnPointerClick(PointerEventData e)
    {
        targetColor = selectedColor;
        if (clickSound != null)
        {
            ApplySfxVolume(AudioManager.SfxVolume);
            audioSource.PlayOneShot(clickSound);
        }
        if (gameObject.activeInHierarchy)
            StartCoroutine(ResetColor());
    }

    public void ApplySfxVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.15f);
        targetColor = normalColor;
    }
}

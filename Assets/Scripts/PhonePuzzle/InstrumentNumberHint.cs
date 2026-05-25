using System.Collections;
using TMPro;
using UnityEngine;

public class InstrumentNumberHint : MonoBehaviour
{
    [Header("Number")]
    [SerializeField] private int instrumentNumber;
    [SerializeField] private bool visibleAtStart;
    [SerializeField, Min(0.1f)] private float defaultRevealSeconds = 1.8f;

    [Header("Display")]
    [SerializeField] private TextMeshPro textMeshPro;
    [SerializeField] private TextMesh legacyText;
    [SerializeField] private Color numberColor = new Color(0.86f, 0.02f, 0.015f, 1f);
    [SerializeField] private Color numberOutlineColor = Color.black;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.16f, 0f);
    [SerializeField, Min(0.001f)] private float textScale = 0.055f;

    private Coroutine revealRoutine;
    private bool isVisible;

    private void Awake()
    {
        EnsureText();
        SetVisible(visibleAtStart);
    }

    private void LateUpdate()
    {
        if (!isVisible)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
        }
    }

    public void Configure(int number)
    {
        instrumentNumber = number;
        EnsureText();
        ApplyText();
    }

    public void Reveal(float seconds)
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
        }

        revealRoutine = StartCoroutine(RevealRoutine(seconds > 0f ? seconds : defaultRevealSeconds));
    }

    public void SetVisible(bool visible)
    {
        EnsureText();
        isVisible = visible;

        if (textMeshPro != null)
        {
            textMeshPro.enabled = visible;
        }

        if (legacyText != null)
        {
            MeshRenderer renderer = legacyText.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    private IEnumerator RevealRoutine(float seconds)
    {
        EnsureText();
        ApplyText();
        SetVisible(true);

        Vector3 baseScale = Vector3.one * textScale;
        transform.localScale = baseScale * 0.78f;

        float growDuration = 0.18f;
        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            transform.localScale = Vector3.Lerp(baseScale * 0.78f, baseScale, t);
            yield return null;
        }

        transform.localScale = baseScale;
        yield return new WaitForSeconds(Mathf.Max(0.1f, seconds));
        SetVisible(visibleAtStart);
        revealRoutine = null;
    }

    private void EnsureText()
    {
        transform.localPosition = localOffset;
        transform.localScale = Vector3.one * textScale;

        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TextMeshPro>();
        }

        if (textMeshPro == null && legacyText == null)
        {
            textMeshPro = gameObject.AddComponent<TextMeshPro>();
        }

        if (textMeshPro != null)
        {
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.fontSize = 4.5f;
            textMeshPro.color = numberColor;
            textMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
            textMeshPro.raycastTarget = false;
            textMeshPro.outlineWidth = 0.22f;
            textMeshPro.outlineColor = numberOutlineColor;
        }
        else
        {
            legacyText = GetComponent<TextMesh>() ?? gameObject.AddComponent<TextMesh>();
            legacyText.alignment = TextAlignment.Center;
            legacyText.anchor = TextAnchor.MiddleCenter;
            legacyText.fontSize = 64;
            legacyText.color = numberColor;
        }

        ApplyText();
    }

    private void ApplyText()
    {
        string text = instrumentNumber.ToString();
        if (textMeshPro != null)
        {
            textMeshPro.text = text;
        }

        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
}

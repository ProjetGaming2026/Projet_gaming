using TMPro;
using UnityEngine;

public class HiddenBloodWallClue : MonoBehaviour
{
    [Header("Reveal")]
    [SerializeField] private Transform revealPoint;
    [SerializeField] private float revealDistance = 12f;
    [SerializeField] private float revealAngle = 24f;
    [SerializeField] private float revealSpeed = 4.5f;
    [SerializeField] private float hideSpeed = 7f;
    [SerializeField] private LayerMask occlusionLayers = ~0;

    [Header("Visuals")]
    [SerializeField] private TMP_Text[] bloodTexts;
    [SerializeField] private SpriteRenderer[] bloodSprites;
    [SerializeField] private Color bloodColor = new Color(0.46f, 0.01f, 0.004f, 1f);
    [SerializeField] private float maxAlpha = 0.95f;

    private PlayerHeldFlashlight flashlight;
    private Camera playerCamera;
    private Color[] originalTextColors;
    private Color[] originalSpriteColors;
    private float currentVisibility;

    public void Configure(TMP_Text[] texts, Transform point)
    {
        bloodTexts = texts;
        revealPoint = point != null ? point : transform;
        CacheOriginalTextColors();
        ApplyVisibility(0f, true);
    }

    public void Configure(SpriteRenderer[] sprites, Transform point)
    {
        bloodSprites = sprites;
        revealPoint = point != null ? point : transform;
        CacheOriginalSpriteColors();
        ApplyVisibility(0f, true);
    }

    private void Awake()
    {
        if (revealPoint == null)
        {
            revealPoint = transform;
        }

        if (bloodTexts == null || bloodTexts.Length == 0)
        {
            bloodTexts = GetComponentsInChildren<TMP_Text>(true);
        }

        if (bloodSprites == null || bloodSprites.Length == 0)
        {
            bloodSprites = GetComponentsInChildren<SpriteRenderer>(true);
        }

        CacheOriginalTextColors();
        CacheOriginalSpriteColors();
        ApplyVisibility(0f, true);
    }

    private void Update()
    {
        ResolveReferences();

        float targetVisibility = ShouldReveal() ? 1f : 0f;
        float speed = targetVisibility > currentVisibility ? revealSpeed : hideSpeed;
        currentVisibility = Mathf.MoveTowards(currentVisibility, targetVisibility, speed * Time.deltaTime);
        ApplyVisibility(currentVisibility, false);
    }

    private void ResolveReferences()
    {
        if (flashlight == null)
        {
            flashlight = FindFirstObjectByType<PlayerHeldFlashlight>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private bool ShouldReveal()
    {
        if (flashlight == null || !flashlight.HasFlashlight || !flashlight.IsOn)
        {
            return false;
        }

        Transform beam = flashlight.BeamTransform != null
            ? flashlight.BeamTransform
            : playerCamera != null ? playerCamera.transform : null;

        if (beam == null || revealPoint == null)
        {
            return false;
        }

        Vector3 targetPoint = GetBestRevealPoint(beam);
        Vector3 toClue = targetPoint - beam.position;
        float distance = toClue.magnitude;
        if (distance <= 0.01f || distance > revealDistance)
        {
            return false;
        }

        Vector3 direction = toClue / distance;
        if (Vector3.Angle(beam.forward, direction) > revealAngle)
        {
            return false;
        }

        if (Physics.Raycast(beam.position, direction, out RaycastHit hit, distance + 0.12f, occlusionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != null && hit.transform.IsChildOf(transform))
            {
                return true;
            }

            return hit.distance >= distance - 0.08f;
        }

        return true;
    }

    private Vector3 GetBestRevealPoint(Transform beam)
    {
        Vector3 fallbackPoint = revealPoint != null ? revealPoint.position : transform.position;
        if (beam == null || bloodSprites == null || bloodSprites.Length == 0)
        {
            return fallbackPoint;
        }

        Vector3 bestPoint = fallbackPoint;
        float bestAngle = float.MaxValue;

        foreach (SpriteRenderer bloodSprite in bloodSprites)
        {
            if (bloodSprite == null || bloodSprite.sprite == null)
            {
                continue;
            }

            Bounds bounds = bloodSprite.bounds;
            float forwardDistance = Vector3.Dot(bounds.center - beam.position, beam.forward);
            if (forwardDistance <= 0.01f)
            {
                continue;
            }

            Vector3 projectedBeamPoint = beam.position + beam.forward * forwardDistance;
            Vector3 candidatePoint = bounds.ClosestPoint(projectedBeamPoint);
            Vector3 toCandidate = candidatePoint - beam.position;
            if (toCandidate.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            float angle = Vector3.Angle(beam.forward, toCandidate.normalized);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestPoint = candidatePoint;
            }
        }

        return bestPoint;
    }

    private void ApplyVisibility(float visibility, bool force)
    {
        float alpha = Mathf.Clamp01(visibility) * maxAlpha;
        bool visible = force || alpha > 0.01f;

        if (bloodTexts == null)
        {
            bloodTexts = new TMP_Text[0];
        }

        foreach (TMP_Text bloodText in bloodTexts)
        {
            if (bloodText == null)
            {
                continue;
            }

            bloodText.enabled = visible;
            Renderer textRenderer = bloodText.GetComponent<Renderer>();
            if (textRenderer != null)
            {
                textRenderer.enabled = visible;
            }

            Color color = GetOriginalTextColor(bloodText);
            color.a = alpha;
            bloodText.color = color;
        }

        if (bloodSprites == null)
        {
            return;
        }

        foreach (SpriteRenderer bloodSprite in bloodSprites)
        {
            if (bloodSprite == null)
            {
                continue;
            }

            bloodSprite.enabled = true;
            Color color = GetOriginalSpriteColor(bloodSprite);
            color.a = alpha;
            bloodSprite.color = color;
        }
    }

    private void CacheOriginalTextColors()
    {
        if (bloodTexts == null)
        {
            originalTextColors = null;
            return;
        }

        originalTextColors = new Color[bloodTexts.Length];
        for (int i = 0; i < bloodTexts.Length; i++)
        {
            originalTextColors[i] = bloodTexts[i] != null ? bloodTexts[i].color : bloodColor;
            originalTextColors[i].a = 1f;
        }
    }

    private Color GetOriginalTextColor(TMP_Text bloodText)
    {
        if (bloodTexts == null || originalTextColors == null)
        {
            return bloodColor;
        }

        for (int i = 0; i < bloodTexts.Length && i < originalTextColors.Length; i++)
        {
            if (bloodTexts[i] == bloodText)
            {
                return originalTextColors[i];
            }
        }

        return bloodColor;
    }

    private void CacheOriginalSpriteColors()
    {
        if (bloodSprites == null)
        {
            originalSpriteColors = null;
            return;
        }

        originalSpriteColors = new Color[bloodSprites.Length];
        for (int i = 0; i < bloodSprites.Length; i++)
        {
            originalSpriteColors[i] = bloodSprites[i] != null ? bloodSprites[i].color : bloodColor;
            originalSpriteColors[i].a = 1f;
        }
    }

    private Color GetOriginalSpriteColor(SpriteRenderer bloodSprite)
    {
        if (bloodSprites == null || originalSpriteColors == null)
        {
            return bloodColor;
        }

        for (int i = 0; i < bloodSprites.Length && i < originalSpriteColors.Length; i++)
        {
            if (bloodSprites[i] == bloodSprite)
            {
                return originalSpriteColors[i];
            }
        }

        return bloodColor;
    }
}

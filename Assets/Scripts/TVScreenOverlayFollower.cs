using UnityEngine;

public class TVScreenOverlayFollower : MonoBehaviour
{
    [SerializeField] private Transform screenTarget;
    [SerializeField] private Vector2 minSize = new Vector2(260f, 145f);
    [SerializeField] private Vector2 maxSize = new Vector2(620f, 350f);
    [SerializeField] private float screenPadding = 0.72f;

    private RectTransform rectTransform;
    private Renderer targetRenderer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        UpdateOverlayRect();
    }

    public void Configure(Transform newScreenTarget)
    {
        screenTarget = newScreenTarget;
        targetRenderer = screenTarget != null ? screenTarget.GetComponent<Renderer>() : null;
        if (targetRenderer == null && screenTarget != null)
        {
            targetRenderer = screenTarget.GetComponentInChildren<Renderer>(true);
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        UpdateOverlayRect();
    }

    private void UpdateOverlayRect()
    {
        if (rectTransform == null || screenTarget == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Bounds bounds = targetRenderer != null
            ? targetRenderer.bounds
            : new Bounds(screenTarget.position, Vector3.one);

        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
        };

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        bool hasVisibleCorner = false;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(corners[i]);
            if (screenPoint.z <= 0f)
            {
                continue;
            }

            hasVisibleCorner = true;
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        if (!hasVisibleCorner)
        {
            return;
        }

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = (max - min) * screenPadding;
        size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
        size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.position = center;
        rectTransform.sizeDelta = size;
    }
}

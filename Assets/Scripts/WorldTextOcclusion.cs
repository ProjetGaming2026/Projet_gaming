using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WorldTextOcclusion : MonoBehaviour
{
    [SerializeField] private Transform visibilityTarget;
    [SerializeField] private LayerMask occlusionLayers = ~0;
    [SerializeField] private float surfaceBias = 0.02f;

    private Renderer cachedRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();

        if (visibilityTarget == null)
        {
            visibilityTarget = transform;
        }
    }

    private void LateUpdate()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 targetPosition = visibilityTarget.position;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            cachedRenderer.enabled = true;
            return;
        }

        direction /= distance;
        Vector3 rayOrigin = cameraPosition + direction * surfaceBias;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, distance, occlusionLayers, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.collider.transform;
            bool visible = hitTransform == transform ||
                           hitTransform == visibilityTarget ||
                           hitTransform.IsChildOf(transform) ||
                           transform.IsChildOf(hitTransform) ||
                           (visibilityTarget != null && (hitTransform.IsChildOf(visibilityTarget) || visibilityTarget.IsChildOf(hitTransform)));

            cachedRenderer.enabled = visible;
            return;
        }

        cachedRenderer.enabled = true;
    }
}

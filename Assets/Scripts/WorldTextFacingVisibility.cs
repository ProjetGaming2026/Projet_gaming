using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WorldTextFacingVisibility : MonoBehaviour
{
    [SerializeField] private Transform facingReference;
    [SerializeField, Range(-1f, 1f)] private float minFacingDot = 0.05f;
    [SerializeField] private bool invertFacing;

    private Renderer cachedRenderer;
    private Camera mainCamera;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();

        if (facingReference == null)
        {
            facingReference = transform;
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

        if (mainCamera == null || facingReference == null)
        {
            return;
        }

        Vector3 toCamera = (mainCamera.transform.position - facingReference.position).normalized;
        Vector3 facingDirection = invertFacing ? -facingReference.forward : facingReference.forward;
        float facingDot = Vector3.Dot(facingDirection, toCamera);
        cachedRenderer.enabled = facingDot >= minFacingDot;
    }

    public void Configure(Transform reference, bool invert)
    {
        facingReference = reference;
        invertFacing = invert;
    }
}

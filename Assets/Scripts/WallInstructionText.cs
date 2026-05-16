using UnityEngine;

public class WallInstructionText : MonoBehaviour
{
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private string instruction = "Press the symbols in the correct order.";
    [SerializeField] private Vector3 localPosition = new Vector3(0f, 1.25f, -0.05f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private int fontSize = 48;
    [SerializeField] private float characterSize = 0.05f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private bool hideThroughWalls = true;
    [SerializeField] private bool visibleFromFrontOnly = true;
    [SerializeField] private LayerMask occlusionLayers = ~0;
    [SerializeField] private float occlusionBias = 0.02f;

    private Renderer cachedRenderer;
    private Camera mainCamera;
    private WorldTextOcclusion childOcclusion;

    private void Awake()
    {
        EnsureTextMesh();
        ApplyVisuals();

        if (textMesh != null)
        {
            cachedRenderer = textMesh.GetComponent<Renderer>();
            childOcclusion = textMesh.GetComponent<WorldTextOcclusion>();
            if (childOcclusion != null)
            {
                childOcclusion.enabled = false;
            }
        }
    }

    private void Reset()
    {
        EnsureTextMesh();
        ApplyVisuals();
    }

    private void OnValidate()
    {
        if (!IsSceneInstance())
        {
            return;
        }

        EnsureTextMesh();
        ApplyVisuals();
    }

    private void EnsureTextMesh()
    {
        if (textMesh == null)
        {
            Transform child = transform.Find("InstructionText");
            if (child == null)
            {
                if (!IsSceneInstance())
                {
                    return;
                }

                GameObject textObject = new GameObject("InstructionText");
                child = textObject.transform;
                child.SetParent(transform, false);
            }

            textMesh = child.GetComponent<TextMesh>();
            if (textMesh == null)
            {
                textMesh = child.gameObject.AddComponent<TextMesh>();
            }
        }
    }

    private void ApplyVisuals()
    {
        if (textMesh == null)
        {
            return;
        }

        Transform textTransform = textMesh.transform;
        if (textTransform.parent != transform && IsSceneInstance())
        {
            textTransform.SetParent(transform, false);
        }

        textTransform.localPosition = localPosition;
        textTransform.localEulerAngles = localEulerAngles;
        textTransform.localScale = Vector3.one;

        textMesh.text = instruction;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = textColor;
        textMesh.fontStyle = FontStyle.Bold;
        cachedRenderer = textMesh.GetComponent<Renderer>();

        if (cachedRenderer != null)
        {
            cachedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cachedRenderer.receiveShadows = false;
            cachedRenderer.sortingOrder = 10;
        }
    }

    private void LateUpdate()
    {
        if (textMesh == null)
        {
            return;
        }

        if (cachedRenderer == null)
        {
            cachedRenderer = textMesh.GetComponent<Renderer>();
        }

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

        Transform textTransform = textMesh.transform;

        if (visibleFromFrontOnly)
        {
            Vector3 toCamera = (mainCamera.transform.position - textTransform.position).normalized;
            float facingDot = Vector3.Dot(-textTransform.forward, toCamera);
            if (facingDot < 0.05f)
            {
                cachedRenderer.enabled = false;
                return;
            }
        }

        if (!hideThroughWalls)
        {
            cachedRenderer.enabled = true;
            return;
        }

        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 targetPosition = textTransform.position;
        Vector3 direction = targetPosition - cameraPosition;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            cachedRenderer.enabled = true;
            return;
        }

        direction /= distance;
        Vector3 rayOrigin = cameraPosition + direction * occlusionBias;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, distance, occlusionLayers, QueryTriggerInteraction.Ignore))
        {
            Transform hitTransform = hit.collider.transform;
            bool visible =
                hitTransform == textTransform ||
                hitTransform == transform ||
                hitTransform.IsChildOf(textTransform) ||
                textTransform.IsChildOf(hitTransform) ||
                hitTransform.IsChildOf(transform);

            cachedRenderer.enabled = visible;
            return;
        }

        cachedRenderer.enabled = true;
    }

    private bool IsSceneInstance()
    {
        return gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }
}

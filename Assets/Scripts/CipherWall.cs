using UnityEngine;

public class CipherWall : MonoBehaviour
{
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private Vector3 textLocalPosition = new Vector3(0f, 1.55f, 0.03f);
    [SerializeField] private Vector3 textLocalEulerAngles = new Vector3(0f, 0f, -10f);
    [SerializeField] private int fontSize = 64;
    [SerializeField] private float characterSize = 0.09f;
    [SerializeField] private Color textColor = Color.red;
    [SerializeField] private string shiftSeparator = "        ";
    [SerializeField] private bool snapToSurface = true;
    [SerializeField] private float surfaceCastDistance = 7f;
    [SerializeField] private float surfaceOffset = 0.03f;
    [SerializeField] private LayerMask surfaceLayers = ~0;

    private void Awake()
    {
        EnsureTextMesh();
    }

    private void Reset()
    {
        if (!IsSceneInstance())
        {
            return;
        }

        EnsureTextMesh();
    }

    private void OnValidate()
    {
        if (!IsSceneInstance())
        {
            return;
        }

        EnsureTextMesh();
    }

    public void DisplayText(string encryptedText, int shift)
    {
        EnsureTextMesh();

        if (textMesh != null)
        {
            textMesh.text = encryptedText + shiftSeparator + "+" + shift;
        }
    }

    private void EnsureTextMesh()
    {
        bool createdTextMesh = false;

        if (textMesh == null)
        {
            Transform child = transform.Find("CipherWallText");
            if (child == null)
            {
                if (!IsSceneInstance())
                {
                    return;
                }

                GameObject textObject = new GameObject("CipherWallText");
                child = textObject.transform;
                child.SetParent(transform, false);
                createdTextMesh = true;
            }

            textMesh = child.GetComponent<TextMesh>();
            if (textMesh == null)
            {
                textMesh = child.gameObject.AddComponent<TextMesh>();
                createdTextMesh = true;
            }
        }

        Transform textTransform = textMesh.transform;
        if (textTransform.parent != transform && IsSceneInstance())
        {
            textTransform.SetParent(transform, false);
        }
        textTransform.localScale = Vector3.one;

        if (createdTextMesh)
        {
            if (!snapToSurface || !TrySnapToSurface(textTransform))
            {
                textTransform.localPosition = textLocalPosition;
                textTransform.localEulerAngles = textLocalEulerAngles;
            }
        }
        else
        {
            textLocalPosition = textTransform.localPosition;
            textLocalEulerAngles = textTransform.localEulerAngles;
        }

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.color = textColor;
        textMesh.fontStyle = FontStyle.Bold;

        WorldTextOcclusion occlusion = textMesh.GetComponent<WorldTextOcclusion>();
        if (occlusion == null)
        {
            occlusion = textMesh.gameObject.AddComponent<WorldTextOcclusion>();
        }

        occlusion.Configure(textTransform, true, textTransform, true);

        WorldTextFacingVisibility facingVisibility = textMesh.GetComponent<WorldTextFacingVisibility>();
        if (facingVisibility != null)
        {
            facingVisibility.enabled = false;
        }

        textMesh.text = string.IsNullOrWhiteSpace(textMesh.text) ? "...   +0" : textMesh.text;
    }

    private bool TrySnapToSurface(Transform textTransform)
    {
        Vector3 castOrigin = transform.position + Vector3.up * 0.2f;
        Vector3[] directions =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        bool foundHit = false;
        RaycastHit bestHit = default;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < directions.Length; i++)
        {
            if (!Physics.Raycast(castOrigin, directions[i], out RaycastHit hit, surfaceCastDistance, surfaceLayers, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (Mathf.Abs(hit.normal.y) > 0.35f)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                foundHit = true;
            }
        }

        if (!foundHit)
        {
            return false;
        }

        textTransform.position = bestHit.point + bestHit.normal * surfaceOffset + Vector3.up * 1.05f;
        textTransform.rotation = Quaternion.LookRotation(-bestHit.normal, Vector3.up) * Quaternion.Euler(textLocalEulerAngles);
        return true;
    }

    private bool IsSceneInstance()
    {
        return gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }
}

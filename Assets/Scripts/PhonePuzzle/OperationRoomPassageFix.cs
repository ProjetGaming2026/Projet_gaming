using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OperationRoomPassageFix : MonoBehaviour
{
    private const string GeneratedColliderName = "Puzzle4_Table_Physical_Blocker";
    private const string LegacySlimColliderName = "Puzzle4_Table_Slim_Blocker";

    [Header("Target")]
    [SerializeField] private Transform surgicalTableRoot;

    [Header("Collision")]
    [SerializeField] private bool preserveOriginalBlockingColliders = true;
    [SerializeField] private bool addFallbackPhysicalColliders = true;
    [SerializeField] private Vector3 physicalColliderScale = new Vector3(0.92f, 0.9f, 0.92f);
    [SerializeField] private Vector3 minimumColliderSize = new Vector3(0.35f, 0.25f, 0.45f);
    [SerializeField] private Vector3 maximumColliderSize = new Vector3(6f, 2f, 6f);

    private readonly List<BoxCollider> generatedColliders = new List<BoxCollider>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterSceneLoad()
    {
        EnsureForScene();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene();
    }

    private void Awake()
    {
        Apply();
    }

    private void Start()
    {
        Apply();
    }

    public void Configure(Transform tableRoot)
    {
        if (tableRoot != null)
        {
            surgicalTableRoot = tableRoot;
        }

        Apply();
    }

    public void Apply()
    {
        Transform root = surgicalTableRoot != null ? surgicalTableRoot : transform;
        if (root == null)
        {
            Debug.LogWarning("OperationRoomPassageFix has no surgical table root.");
            return;
        }

        surgicalTableRoot = root;

        DisableNamedGeneratedColliders(root, LegacySlimColliderName);
        DisableNamedGeneratedColliders(root, GeneratedColliderName);

        if (preserveOriginalBlockingColliders)
        {
            EnsureOriginalBlockingCollidersEnabled(root);
        }

        EnsurePhysicalColliders(root);
        Debug.Log("Puzzle 4 table collision adjusted without changing table or instrument transforms.");
    }

    public static OperationRoomPassageFix EnsureForScene(Transform preferredTableRoot = null)
    {
        Transform tableRoot = preferredTableRoot;
        if (tableRoot == null)
        {
            tableRoot = FindExactTransform("surgical__instrument_table");
        }

        if (tableRoot == null)
        {
            tableRoot = FindExactTransform("Surgical_Table_Single");
        }

        if (tableRoot == null)
        {
            tableRoot = FindBestTransformByHints("surgicalinstrumenttable", "surgicaltable", "operationtable", "operatingtable");
        }

        if (tableRoot == null)
        {
            Debug.LogWarning("Puzzle 4 could not find the surgical table collection for collision fixing.");
            return null;
        }

        OperationRoomPassageFix fix = tableRoot.GetComponent<OperationRoomPassageFix>();
        if (fix == null)
        {
            fix = tableRoot.gameObject.AddComponent<OperationRoomPassageFix>();
        }

        fix.Configure(tableRoot);
        return fix;
    }

    private void EnsurePhysicalColliders(Transform root)
    {
        generatedColliders.Clear();
        if (!addFallbackPhysicalColliders)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsBlockingRenderer(renderer))
            {
                continue;
            }

            BoxCollider collider = EnsureRendererCollider(renderer);
            if (collider == null)
            {
                continue;
            }

            Bounds localBounds = BuildLocalBoundsFromWorldBounds(renderer.transform, renderer.bounds);
            collider.center = localBounds.center;
            collider.size = ClampVector3(Vector3.Scale(localBounds.size, physicalColliderScale), minimumColliderSize, maximumColliderSize);
            collider.isTrigger = false;
            collider.enabled = true;
            generatedColliders.Add(collider);
        }

        if (generatedColliders.Count > 0)
        {
            return;
        }

        BoxCollider fallbackCollider = EnsureRootFallbackCollider(root);
        if (fallbackCollider == null)
        {
            return;
        }

        Bounds localRootBounds;
        if (!TryGetLocalRenderBounds(root, out localRootBounds))
        {
            fallbackCollider.center = new Vector3(0f, 0.45f, 0f);
            fallbackCollider.size = new Vector3(1.25f, 0.9f, 1.8f);
        }
        else
        {
            fallbackCollider.center = localRootBounds.center;
            fallbackCollider.size = ClampVector3(Vector3.Scale(localRootBounds.size, physicalColliderScale), minimumColliderSize, maximumColliderSize);
        }

        fallbackCollider.isTrigger = false;
        fallbackCollider.enabled = true;
        generatedColliders.Add(fallbackCollider);
    }

    private static bool IsBlockingRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Transform rendererTransform = renderer.transform;
        if (rendererTransform == null ||
            rendererTransform.name == GeneratedColliderName ||
            rendererTransform.name == LegacySlimColliderName)
        {
            return false;
        }

        if (renderer.GetComponentInParent<OperationMethodSheetReadable>() != null ||
            renderer.GetComponentInParent<SurgicalInstrumentInteractable>() != null ||
            renderer.GetComponentInParent<InstrumentNumberHint>() != null)
        {
            return false;
        }

        Bounds bounds = renderer.bounds;
        return bounds.size.x > 0.1f && bounds.size.y > 0.1f && bounds.size.z > 0.1f;
    }

    private BoxCollider EnsureRendererCollider(Renderer renderer)
    {
        Transform existing = renderer.transform.Find(GeneratedColliderName);
        GameObject colliderObject;
        if (existing != null)
        {
            colliderObject = existing.gameObject;
        }
        else
        {
            colliderObject = new GameObject(GeneratedColliderName);
            colliderObject.transform.SetParent(renderer.transform, false);
            colliderObject.transform.localPosition = Vector3.zero;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;
        }

        BoxCollider collider = colliderObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = colliderObject.AddComponent<BoxCollider>();
        }

        return collider;
    }

    private BoxCollider EnsureRootFallbackCollider(Transform root)
    {
        Transform existing = root.Find(GeneratedColliderName);
        GameObject colliderObject;
        if (existing != null)
        {
            colliderObject = existing.gameObject;
        }
        else
        {
            colliderObject = new GameObject(GeneratedColliderName);
            colliderObject.transform.SetParent(root, false);
            colliderObject.transform.localPosition = Vector3.zero;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;
        }

        BoxCollider collider = colliderObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = colliderObject.AddComponent<BoxCollider>();
        }

        return collider;
    }

    private void DisableNamedGeneratedColliders(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != objectName)
            {
                continue;
            }

            Collider[] colliders = candidate.GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                if (colliders[colliderIndex] != null)
                {
                    colliders[colliderIndex].enabled = false;
                }
            }
        }
    }

    private void EnsureOriginalBlockingCollidersEnabled(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider target = colliders[i];
            if (target == null || target.isTrigger)
            {
                continue;
            }

            if (target.transform.name == GeneratedColliderName || target.transform.name == LegacySlimColliderName)
            {
                continue;
            }

            target.enabled = true;
        }
    }

    private static bool TryGetLocalRenderBounds(Transform root, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (!IsBlockingRenderer(renderer))
            {
                continue;
            }

            Bounds localRendererBounds = BuildLocalBoundsFromWorldBounds(root, renderer.bounds);
            if (!hasBounds)
            {
                bounds = localRendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localRendererBounds);
            }
        }

        return hasBounds;
    }

    private static Vector3 ClampVector3(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z));
    }

    private static Bounds BuildLocalBoundsFromWorldBounds(Transform root, Bounds worldBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        Bounds localBounds = new Bounds(root.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
        {
            localBounds.Encapsulate(root.InverseTransformPoint(corners[i]));
        }

        return localBounds;
    }

    private static Transform FindExactTransform(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static Transform FindBestTransformByHints(params string[] hints)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform best = null;
        int bestScore = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            string normalizedName = NormalizeName(candidate.name);
            for (int hintIndex = 0; hintIndex < hints.Length; hintIndex++)
            {
                string normalizedHint = NormalizeName(hints[hintIndex]);
                int score = 0;
                if (normalizedName == normalizedHint)
                {
                    score = 100 - hintIndex;
                }
                else if (normalizedName.Contains(normalizedHint))
                {
                    score = 70 - hintIndex;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty).ToLowerInvariant();
    }
}

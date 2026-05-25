using UnityEngine;
using UnityEngine.SceneManagement;

public static class HiddenBloodNikolasBootstrap
{
    private const string MediumSceneName = "demoScene_Moyen";
    private const string ClueName = "Hidden_Blood_Nikolas";
    private const string DecalName = "Blood_Decal_Texture_V5";
    private const string TargetWallName = "mur_nom";
    private const string FlashlightName = "old_flashlight";
    private const int TextureWidth = 2048;
    private const int TextureHeight = 768;
    private const float SpritePixelsPerUnit = 650f;
    private const float WallSurfaceOffset = 0.055f;
    private const float WallHighPlacement = 0.12f;

    private static readonly Vector3 FallbackPosition = new Vector3(1.32f, -5.05f, 7.35f);
    private static readonly Quaternion FallbackRotation = Quaternion.Euler(0f, 180f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ConfigureScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureScene(scene);
    }

    private static void ConfigureScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (!IsTelevisionPuzzleScene(scene))
        {
            GameObject nonMediumClue = FindSceneObjectByName(scene, ClueName);
            if (nonMediumClue != null)
            {
                Object.Destroy(nonMediumClue);
            }

            return;
        }

        GameObject existingClue = FindSceneObjectByName(scene, ClueName);
        if (existingClue != null && IsCurrentBloodDecal(existingClue))
        {
            return;
        }

        if (existingClue != null)
        {
            UnityEngine.Object.Destroy(existingClue);
        }

        GameObject targetWall = FindSceneObjectByName(scene, TargetWallName);
        GameObject flashlight = FindSceneObjectByName(scene, FlashlightName);

        ResolveWallPose(targetWall, flashlight, out Vector3 position, out Quaternion rotation);
        CreateNikolasClue(position, rotation);
    }

    private static bool IsCurrentBloodDecal(GameObject clue)
    {
        if (clue == null)
        {
            return false;
        }

        Transform decal = clue.transform.Find(DecalName);
        return decal != null && decal.GetComponent<SpriteRenderer>() != null;
    }

    private static void CreateNikolasClue(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject(ClueName);
        root.transform.SetPositionAndRotation(position, rotation);

        GameObject decalObject = new GameObject(DecalName);
        decalObject.transform.SetParent(root.transform, false);
        decalObject.transform.localPosition = Vector3.zero;
        decalObject.transform.localRotation = Quaternion.identity;

        Texture2D texture = CreateBloodTexture();
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            SpritePixelsPerUnit,
            0,
            SpriteMeshType.FullRect);

        SpriteRenderer spriteRenderer = decalObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.flipX = true;
        spriteRenderer.sortingOrder = 40;

        HiddenBloodWallClue clue = root.AddComponent<HiddenBloodWallClue>();
        clue.Configure(new[] { spriteRenderer }, root.transform);

        Debug.Log("HiddenBloodNikolasBootstrap: large blood texture clue Nikolas created.");
    }

    private static Texture2D CreateBloodTexture()
    {
        BloodCanvas canvas = new BloodCanvas(TextureWidth, TextureHeight, 1873);
        Color dark = new Color(0.16f, 0.002f, 0.001f, 0.94f);
        Color deep = new Color(0.32f, 0.005f, 0.002f, 0.96f);
        Color wet = new Color(0.68f, 0.015f, 0.006f, 0.9f);
        Color highlight = new Color(1f, 0.08f, 0.035f, 0.34f);

        DrawNikolas(canvas, dark, 54f, 0f, 0f);
        DrawNikolas(canvas, deep, 42f, 0f, 0f);
        DrawNikolas(canvas, wet, 24f, 0f, 0f);
        DrawNikolas(canvas, highlight, 8f, -7f, 14f);

        DrawLongDrips(canvas, dark, deep, wet);
        DrawSplatter(canvas, dark, wet);
        DrawHandSmears(canvas, dark, wet);

        Texture2D texture = canvas.ToTexture();
        texture.name = "Runtime_Nikolas_Blood_Decal";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    private static void DrawNikolas(BloodCanvas canvas, Color color, float width, float offsetX, float offsetY)
    {
        float y = offsetY;
        float x = offsetX;

        // N
        DrawStroke(canvas, color, width, new Vector2(110f + x, 150f + y), new Vector2(130f + x, 620f + y));
        DrawStroke(canvas, color, width, new Vector2(130f + x, 620f + y), new Vector2(365f + x, 155f + y));
        DrawStroke(canvas, color, width, new Vector2(365f + x, 155f + y), new Vector2(395f + x, 620f + y));
        DrawStroke(canvas, color, width * 0.58f, new Vector2(120f + x, 150f + y), new Vector2(45f + x, 90f + y));
        DrawStroke(canvas, color, width * 0.55f, new Vector2(145f + x, 618f + y), new Vector2(232f + x, 688f + y));

        // I
        DrawStroke(canvas, color, width * 0.82f, new Vector2(470f + x, 600f + y), new Vector2(680f + x, 600f + y));
        DrawStroke(canvas, color, width, new Vector2(580f + x, 165f + y), new Vector2(575f + x, 610f + y));
        DrawStroke(canvas, color, width * 0.82f, new Vector2(470f + x, 165f + y), new Vector2(680f + x, 160f + y));

        // K
        DrawStroke(canvas, color, width, new Vector2(770f + x, 155f + y), new Vector2(785f + x, 620f + y));
        DrawStroke(canvas, color, width, new Vector2(792f + x, 385f + y), new Vector2(1022f + x, 615f + y));
        DrawStroke(canvas, color, width, new Vector2(792f + x, 365f + y), new Vector2(1042f + x, 150f + y));
        DrawStroke(canvas, color, width * 0.55f, new Vector2(995f + x, 617f + y), new Vector2(1088f + x, 682f + y));

        // O
        DrawLoop(canvas, color, width, new Vector2(1190f + x, 385f + y), 145f, 214f, 0f, 360f);
        DrawStroke(canvas, color, width * 0.48f, new Vector2(1082f + x, 245f + y), new Vector2(1018f + x, 180f + y));

        // L
        DrawStroke(canvas, color, width, new Vector2(1395f + x, 165f + y), new Vector2(1405f + x, 630f + y));
        DrawStroke(canvas, color, width * 0.92f, new Vector2(1390f + x, 165f + y), new Vector2(1612f + x, 175f + y));

        // A
        DrawStroke(canvas, color, width, new Vector2(1660f + x, 155f + y), new Vector2(1780f + x, 625f + y));
        DrawStroke(canvas, color, width, new Vector2(1780f + x, 625f + y), new Vector2(1915f + x, 155f + y));
        DrawStroke(canvas, color, width * 0.72f, new Vector2(1712f + x, 345f + y), new Vector2(1868f + x, 355f + y));
        DrawStroke(canvas, color, width * 0.48f, new Vector2(1782f + x, 620f + y), new Vector2(1838f + x, 688f + y));

        // S
        DrawStroke(canvas, color, width, new Vector2(2000f + x, 595f + y), new Vector2(1908f + x, 628f + y));
        DrawStroke(canvas, color, width, new Vector2(1908f + x, 628f + y), new Vector2(1835f + x, 515f + y));
        DrawStroke(canvas, color, width, new Vector2(1835f + x, 515f + y), new Vector2(1990f + x, 390f + y));
        DrawStroke(canvas, color, width, new Vector2(1990f + x, 390f + y), new Vector2(1912f + x, 170f + y));
        DrawStroke(canvas, color, width, new Vector2(1912f + x, 170f + y), new Vector2(1762f + x, 228f + y));
    }

    private static void DrawLongDrips(BloodCanvas canvas, Color dark, Color deep, Color wet)
    {
        DrawDrip(canvas, new Vector2(125f, 300f), 210f, 18f, deep);
        DrawDrip(canvas, new Vector2(362f, 275f), 150f, 14f, dark);
        DrawDrip(canvas, new Vector2(575f, 260f), 190f, 13f, wet);
        DrawDrip(canvas, new Vector2(790f, 285f), 165f, 15f, deep);
        DrawDrip(canvas, new Vector2(1015f, 250f), 118f, 12f, dark);
        DrawDrip(canvas, new Vector2(1190f, 238f), 198f, 17f, wet);
        DrawDrip(canvas, new Vector2(1400f, 252f), 215f, 14f, deep);
        DrawDrip(canvas, new Vector2(1600f, 235f), 160f, 18f, dark);
        DrawDrip(canvas, new Vector2(1782f, 265f), 195f, 14f, deep);
        DrawDrip(canvas, new Vector2(1915f, 240f), 220f, 16f, wet);
    }

    private static void DrawDrip(BloodCanvas canvas, Vector2 start, float length, float width, Color color)
    {
        int samples = Mathf.Max(6, Mathf.RoundToInt(length / 8f));
        Vector2 previous = start;

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 next = new Vector2(
                start.x + Mathf.Sin(t * Mathf.PI * 2.7f + start.x * 0.01f) * width * 0.45f,
                start.y - length * t);
            DrawStroke(canvas, color, Mathf.Lerp(width, width * 0.45f, t), previous, next);
            previous = next;
        }

        canvas.DrawBlob(previous, width * 1.45f, color);
    }

    private static void DrawSplatter(BloodCanvas canvas, Color dark, Color wet)
    {
        for (int i = 0; i < 115; i++)
        {
            float x = canvas.RandomRange(75f, 1950f);
            float y = canvas.RandomRange(150f, 650f);
            float distanceFromCenter = Mathf.Abs(y - 385f) / 260f;
            if (canvas.RandomValue() < distanceFromCenter * 0.35f)
            {
                continue;
            }

            Color color = canvas.RandomValue() > 0.45f ? dark : wet;
            color.a *= canvas.RandomRange(0.35f, 0.85f);
            canvas.DrawBlob(new Vector2(x, y), canvas.RandomRange(4f, 17f), color);
        }
    }

    private static void DrawHandSmears(BloodCanvas canvas, Color dark, Color wet)
    {
        DrawStroke(canvas, dark, 18f, new Vector2(315f, 620f), new Vector2(435f, 660f));
        DrawStroke(canvas, wet, 14f, new Vector2(720f, 152f), new Vector2(935f, 178f));
        DrawStroke(canvas, dark, 16f, new Vector2(1345f, 575f), new Vector2(1540f, 620f));
        DrawStroke(canvas, wet, 12f, new Vector2(1580f, 142f), new Vector2(1800f, 195f));
    }

    private static void DrawLoop(BloodCanvas canvas, Color color, float width, Vector2 center, float radiusX, float radiusY, float startAngle, float endAngle)
    {
        const int segments = 44;
        Vector2 previous = default;
        bool hasPrevious = false;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            Vector2 point = new Vector2(
                center.x + Mathf.Cos(angle) * radiusX,
                center.y + Mathf.Sin(angle) * radiusY);

            if (hasPrevious)
            {
                DrawStroke(canvas, color, width, previous, point);
            }

            previous = point;
            hasPrevious = true;
        }
    }

    private static void DrawStroke(BloodCanvas canvas, Color color, float width, Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(2, Mathf.CeilToInt(distance / Mathf.Max(width * 0.22f, 2f)));

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(from, to, t);
            point += new Vector2(canvas.RandomRange(-width, width), canvas.RandomRange(-width, width)) * 0.075f;
            float radius = width * canvas.RandomRange(0.43f, 0.62f);
            canvas.DrawBlob(point, radius, color);
        }
    }

    private static void ResolveWallPose(GameObject targetWall, GameObject flashlight, out Vector3 position, out Quaternion rotation)
    {
        Vector3 referencePoint = flashlight != null
            ? flashlight.transform.position
            : FallbackPosition + Vector3.forward;

        if (targetWall != null && TryResolveNamedWallPose(targetWall, referencePoint, out position, out rotation))
        {
            return;
        }

        if (flashlight != null && TryFindNearbyWallPose(flashlight.transform.position + Vector3.up * 1.15f, out position, out rotation))
        {
            return;
        }

        position = FallbackPosition;
        rotation = FallbackRotation;
    }

    private static bool TryResolveNamedWallPose(GameObject wallObject, Vector3 referencePoint, out Vector3 position, out Quaternion rotation)
    {
        Renderer wallRenderer = wallObject.GetComponentInChildren<Renderer>();
        Collider wallCollider = wallObject.GetComponentInChildren<Collider>();
        Bounds bounds;

        if (wallRenderer != null)
        {
            bounds = wallRenderer.bounds;
        }
        else if (wallCollider != null)
        {
            bounds = wallCollider.bounds;
        }
        else
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        Transform wallTransform = wallObject.transform;
        Vector3 normal = GetThinAxisNormal(wallTransform);
        if (Vector3.Dot(referencePoint - bounds.center, normal) < 0f)
        {
            normal = -normal;
        }

        position = bounds.center + Vector3.up * (bounds.extents.y * WallHighPlacement) + normal * WallSurfaceOffset;
        rotation = Quaternion.LookRotation(normal, Vector3.up);
        return true;
    }

    private static Vector3 GetThinAxisNormal(Transform wallTransform)
    {
        Vector3 lossyScale = wallTransform.lossyScale;
        Vector3 normal = wallTransform.right;
        float thinnest = Mathf.Abs(lossyScale.x);

        if (Mathf.Abs(lossyScale.y) < thinnest)
        {
            normal = wallTransform.up;
            thinnest = Mathf.Abs(lossyScale.y);
        }

        if (Mathf.Abs(lossyScale.z) < thinnest)
        {
            normal = wallTransform.forward;
        }

        return normal.normalized;
    }

    private static bool TryFindNearbyWallPose(Vector3 origin, out Vector3 position, out Quaternion rotation)
    {
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            new Vector3(1f, 0f, 1f).normalized,
            new Vector3(-1f, 0f, 1f).normalized,
            new Vector3(1f, 0f, -1f).normalized,
            new Vector3(-1f, 0f, -1f).normalized
        };

        bool found = false;
        RaycastHit bestHit = default;
        float bestDistance = float.MaxValue;

        foreach (Vector3 direction in directions)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, 7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (hit.distance < 0.75f || Mathf.Abs(hit.normal.y) > 0.35f || IsLikelyFurnitureOrProp(hit.transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                found = true;
                bestHit = hit;
                bestDistance = hit.distance;
            }
        }

        if (!found)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = bestHit.point + bestHit.normal * 0.035f + Vector3.up * 0.28f;
        rotation = Quaternion.LookRotation(-bestHit.normal, Vector3.up);
        return true;
    }

    private static bool IsLikelyFurnitureOrProp(Transform hitTransform)
    {
        Transform current = hitTransform;
        while (current != null)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("table") ||
                lowerName.Contains("bed") ||
                lowerName.Contains("chair") ||
                lowerName.Contains("toilet") ||
                lowerName.Contains("sink") ||
                lowerName.Contains("flashlight") ||
                lowerName.Contains("lamp") ||
                lowerName.Contains("pipe") ||
                lowerName.Contains("drawer") ||
                lowerName.Contains("cabinet"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildByName(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildByName(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsTelevisionPuzzleScene(Scene scene)
    {
        return scene.name == MediumSceneName;
    }

    private sealed class BloodCanvas
    {
        private readonly int width;
        private readonly int height;
        private readonly Color[] pixels;
        private readonly System.Random random;

        public BloodCanvas(int width, int height, int seed)
        {
            this.width = width;
            this.height = height;
            pixels = new Color[width * height];
            random = new System.Random(seed);
        }

        public float RandomValue()
        {
            return (float)random.NextDouble();
        }

        public float RandomRange(float min, float max)
        {
            return Mathf.Lerp(min, max, RandomValue());
        }

        public void DrawBlob(Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 2f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 2f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius + 2f));

            float radiusSquared = radius * radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 delta = new Vector2(x - center.x, y - center.y);
                    float distanceSquared = delta.sqrMagnitude;
                    if (distanceSquared > radiusSquared)
                    {
                        continue;
                    }

                    float edge = 1f - Mathf.Sqrt(distanceSquared) / radius;
                    float alpha = Mathf.Clamp01(edge * 2.8f) * color.a;
                    Color source = color;
                    source.a = alpha;
                    BlendPixel(x, y, source);
                }
            }
        }

        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void BlendPixel(int x, int y, Color source)
        {
            int index = y * width + x;
            Color destination = pixels[index];
            float outAlpha = source.a + destination.a * (1f - source.a);
            if (outAlpha <= 0.0001f)
            {
                pixels[index] = Color.clear;
                return;
            }

            Color output = new Color
            {
                r = (source.r * source.a + destination.r * destination.a * (1f - source.a)) / outAlpha,
                g = (source.g * source.a + destination.g * destination.a * (1f - source.a)) / outAlpha,
                b = (source.b * source.a + destination.b * destination.a * (1f - source.a)) / outAlpha,
                a = outAlpha
            };

            pixels[index] = output;
        }
    }
}

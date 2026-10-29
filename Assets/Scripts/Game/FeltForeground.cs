using System.Collections.Generic;
using UnityEngine;

/// Batch 2 camera match (Imagine 285.jpg): a near felt layer between the lens and the lane,
/// a big sage rosette, moss balls and a roast bean on the burlap close to camera, like the
/// 285 foreground. Runtime only, decoration only (no colliders), never over a gap, and kept
/// under the camera-to-feet sightline (≤ 0.35 m tall at z ≈ −1.1) so it never hides the path. The HUD is top-of-screen.
/// Built once for the title yard at load, then rebuilt on every LevelRunner.OnLevelLoaded.
public static class FeltForeground
{
    public const float ZNear = -1.35f, ZFar = -1.00f;
    public const float MaxHeight = 0.35f;
    public const float SucculentScale = 3.0f; // b4: 285's rosette is ~25% of frame width (audit2 #4)
    static GameObject _root;
    public static int Count { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        LevelRunner.OnLevelLoaded -= OnLevel;
        LevelRunner.OnLevelLoaded += OnLevel;
        Build(-4f, 9f, 285); // title / opening yard before any level is loaded
    }

    static void OnLevel(LevelCatalog.LevelDef def) => Build(-3f, def.goalZ + 4f, def.seed);

    public static void Build(float x0, float x1, int seed)
    {
        if (_root != null) Object.Destroy(_root);
        _root = new GameObject("Felt Foreground");
        Count = 0;
        var rng = new System.Random(seed ^ 0x0f0e);
        int kind = 0;
        for (float x = x0 + (float)rng.NextDouble(); x < x1; x += 2.4f + (float)rng.NextDouble() * 1.4f)
        {
            float z = Mathf.Lerp(ZNear, ZFar, (float)rng.NextDouble());
            if (!Ground(x, z, out float y)) continue;
            switch (kind++ % 3)
            {
                case 0: // the big 285 rosette
                    Add(FeltSetProps.Succulent(new Vector3(x, y, z), SucculentScale * (0.95f + (float)rng.NextDouble() * 0.2f)));
                    break;
                case 1: // moss pair, like 285's right foreground
                    Add(FeltSetProps.MossBall(new Vector3(x, y, z), 0.26f, rng.Next(1, 90)));
                    if (Ground(x + 0.34f, z + 0.12f, out float y2))
                        Add(FeltSetProps.MossBall(new Vector3(x + 0.34f, y2, z + 0.12f), 0.20f, rng.Next(1, 90)));
                    break;
                default: // moss ball plus a dark roast bean
                    Add(FeltSetProps.MossBall(new Vector3(x, y, z), 0.22f, rng.Next(1, 90)));
                    if (Ground(x - 0.38f, z - 0.05f, out float y3))
                        Add(FeltSetProps.RoastBean(new Vector3(x - 0.38f, y3, z - 0.05f), 0.12f, (float)rng.NextDouble() * 180f));
                    break;
            }
        }
    }

    // flat burlap/kraft under the spot, and no gap within ±0.55 m
    static bool Ground(float x, float z, out float y)
    {
        y = 0f;
        for (float s = x - 0.55f; s <= x + 0.55f; s += 0.11f)
            if (LevelRunner.IsOverGap(s)) return false;
        if (!Physics.Raycast(new Vector3(x, 3f, z), Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return false;
        if (hit.collider.attachedRigidbody != null || hit.normal.y < 0.97f || hit.point.y < -0.05f) return false;
        y = hit.point.y;
        return true;
    }

    static void Add(GameObject go)
    {
        if (go == null) return;
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        go.transform.SetParent(_root.transform, true);
        Count++;
    }
}

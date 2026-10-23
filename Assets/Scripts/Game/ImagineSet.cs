using System.Collections.Generic;
using UnityEngine;

/// Imagine set layout (set-01..06, gen-2d7dcad9, detail-bridge-closer).
/// One kraft arch on the run lane. The grinder stands to its left with a clear gap.
/// The flower tree stands to its right. The deck is CraftMesh.ArchBridge plus a smooth box walk surface.
public static class ImagineSet
{
    public static float SpanL => LevelRunner.BridgeX0 - LevelRunner.BridgePlankRun * 0.5f;
    public static float SpanR => LevelRunner.BridgeX0 + LevelRunner.BridgeLen + LevelRunner.BridgePlankRun * 0.5f;
    public static float MidX => (SpanL + SpanR) * 0.5f;

    public static Vector3 GrinderAt => new Vector3(SpanL - 1.12f, 0f, 1.28f);
    public static Vector3 FlowerAt => new Vector3(7.42f, 0f, 1.32f);
    public static Vector3 TreeBehind => new Vector3(SpanL - 1.28f, 0f, 1.98f);
    public static Vector3 TreeRight => new Vector3(7.58f, 0f, 1.98f);

    /// Scene-root props only (spawned level props live under LevelSetSpawner). Safe to call again.
    public static void Align()
    {
        Transform farPine = null;
        foreach (var pine in Roots("Pine"))
        {
            if (farPine == null || pine.transform.position.x > farPine.position.x)
                farPine = pine.transform;
        }
        if (farPine != null && farPine.position.x > 2.2f)
            farPine.position = new Vector3(2.05f, 0f, 1.05f);

        foreach (var g in Roots("Cardboard Grinder"))
            Place(g, GrinderAt, 0.85f);

        var dots = Roots("Dotted Tree");
        dots.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (dots.Count >= 1) Place(dots[0], TreeBehind, 0.78f);
        if (dots.Count >= 2) Place(dots[dots.Count - 1], TreeRight, 0.62f);

        var flowers = Roots("Flower Tree");
        flowers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (flowers.Count >= 1) Place(flowers[0], FlowerAt, 0.92f);

        float length = LevelRunner.BridgeLen + LevelRunner.BridgePlankRun;
        foreach (var bridge in Roots("Kraft Bridge"))
        {
            bridge.transform.SetPositionAndRotation(new Vector3(MidX, 0f, 0f), Quaternion.identity);
            bridge.transform.localScale = Vector3.one;
            FillBridge(bridge, length, LevelRunner.BridgeArch);
        }
    }

    /// Corrugated arch, chunky posts, two rails, grass at the feet. Root sits at the deck centre, y = 0, lane z = 0.
    public static void FillBridge(GameObject root, float length, float rise)
    {
        var host = root.transform;
        for (int i = host.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(host.GetChild(i).gameObject);

        length = Mathf.Max(0.8f, length);
        rise = Mathf.Max(0.05f, rise);
        float width = LevelRunner.BridgeWidth;
        float foot = LevelRunner.BridgeFootTop;
        const float thick = 0.11f;
        float half = length * 0.5f;
        float Deck(float x)
        {
            float t = Mathf.Clamp01((x + half) / length);
            return foot + Mathf.Sin(t * Mathf.PI) * rise;
        }

        var deck = new GameObject("Bridge Plank");
        deck.transform.SetParent(host, false);
        deck.transform.localPosition = new Vector3(0f, foot - thick * 1.12f, 0f);
        deck.AddComponent<MeshFilter>().sharedMesh = CraftMesh.ArchBridge(length, width, rise, thick, 28, 16);
        var rend = deck.AddComponent<MeshRenderer>();
        rend.sharedMaterial = KraftMat(new Vector2(5.5f, 2.4f), new Color(0.98f, 0.95f, 0.90f));
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        const int boxes = 14;
        for (int i = 0; i < boxes; i++)
        {
            float t = (i + 0.5f) / boxes;
            float x = Mathf.Lerp(-half, half, t);
            float slope = rise * Mathf.PI / length * Mathf.Cos(t * Mathf.PI);
            WalkBox(host, "Bridge Deck", new Vector3(x, Deck(x) - 0.055f, 0f),
                new Vector3(length / boxes * 1.22f, 0.11f, width * 0.94f),
                Mathf.Atan(slope) * Mathf.Rad2Deg);
        }
        Lip(host, -half - 0.22f, -half, 0.012f, foot, width);
        Lip(host, half, half + 0.22f, foot, 0.012f, width);

        float hz = width * 0.5f + 0.04f;
        const float postH = 0.34f;
        const int posts = 5;
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 prevHi = Vector3.zero, prevLo = Vector3.zero;
            for (int i = 0; i < posts; i++)
            {
                float t = i / (float)(posts - 1);
                float x = Mathf.Lerp(-half, half, t);
                float y = Deck(x);
                float z = side * hz;
                Post(host, new Vector3(x, y + postH * 0.5f, z), postH);
                var hi = new Vector3(x, y + postH * 0.78f, z);
                var lo = new Vector3(x, y + 0.10f, z);
                if (i > 0)
                {
                    Rail(host, prevHi, hi, 0.022f);
                    Rail(host, prevLo, lo, 0.016f);
                    var mid = Vector3.Lerp(prevLo, lo, 0.5f);
                    float topY = Mathf.Lerp(prevHi.y, hi.y, 0.5f);
                    Rail(host, mid, new Vector3(mid.x, topY, mid.z), 0.012f);
                }
                prevHi = hi;
                prevLo = lo;
            }
        }

        Tuft(host, new Vector3(-half - 0.06f, 0f, 0.22f), 0.16f);
        Tuft(host, new Vector3(-half - 0.02f, 0f, -0.28f), 0.13f);
        Tuft(host, new Vector3(half + 0.05f, 0f, 0.26f), 0.15f);
        Tuft(host, new Vector3(half + 0.02f, 0f, -0.22f), 0.12f);
    }

    static void Place(GameObject go, Vector3 pos, float scale)
    {
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
    }

    static List<GameObject> Roots(string name)
    {
        var list = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>())
        {
            if (t.name == name && t.parent == null)
                list.Add(t.gameObject);
        }
        return list;
    }

    static void WalkBox(Transform host, string name, Vector3 localPos, Vector3 scale, float angZ)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angZ);
        go.transform.localScale = scale;
        go.isStatic = true;
        go.GetComponent<Renderer>().enabled = false;
        var col = go.GetComponent<Collider>();
        if (col != null) col.material = Grip();
    }

    static void Lip(Transform host, float x0, float x1, float y0, float y1, float width)
    {
        float dx = x1 - x0, dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        var rot = Quaternion.Euler(0f, 0f, ang);
        var mid = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f);
        WalkBox(host, "Bridge Lip", mid - rot * Vector3.up * 0.04f, new Vector3(len, 0.08f, width * 0.94f), ang);
    }

    static void Post(Transform host, Vector3 localPos, float height)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Bridge Post";
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(0.09f, height * 0.5f, 0.09f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = KraftMat(new Vector2(1.2f, 3f), new Color(0.90f, 0.84f, 0.74f));
        var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "Bridge Post Cap";
        cap.transform.SetParent(host, false);
        cap.transform.localPosition = localPos + Vector3.up * (height * 0.5f + 0.012f);
        cap.transform.localScale = new Vector3(0.12f, 0.016f, 0.12f);
        Object.DestroyImmediate(cap.GetComponent<Collider>());
        cap.GetComponent<Renderer>().sharedMaterial = KraftMat(Vector2.one, new Color(0.86f, 0.78f, 0.66f));
    }

    static void Rail(Transform host, Vector3 a, Vector3 b, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Bridge Rail";
        go.transform.SetParent(host, false);
        go.transform.localPosition = (a + b) * 0.5f;
        float len = Mathf.Max(0.001f, Vector3.Distance(a, b));
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        go.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = KraftMat(new Vector2(2f, 0.4f), new Color(0.88f, 0.80f, 0.68f));
    }

    static void Tuft(Transform host, Vector3 at, float s)
    {
        for (int i = 0; i < 3; i++)
        {
            float a = (i - 1) * 0.45f;
            var blade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blade.name = "Bridge Grass";
            blade.transform.SetParent(host, false);
            blade.transform.localPosition = at + new Vector3(Mathf.Sin(a) * s * 0.35f, s * 0.55f, i * 0.02f);
            blade.transform.localScale = new Vector3(s * 0.28f, s * 0.85f, s * 0.22f);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg);
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            blade.GetComponent<Renderer>().sharedMaterial = GrassMat();
        }
    }

    static Material KraftMat(Vector2 tile, Color tint)
    {
        var tex = Resources.Load<Texture2D>("Look/tex-kraft-corrugated");
        string key = CraftMesh.Key("imagine-kraft", tint, tile, 0.85f, 0.05f, 0.55f, 0.92f, 0f);
        return CraftMesh.Shared(key, () => CraftMesh.Felt(tex, tint, tile, 0.85f, 0.05f, 0.55f, 0.92f));
    }

    static Material GrassMat()
    {
        var tex = Resources.Load<Texture2D>("Look/tex-yarn-green");
        var tint = new Color(0.90f, 1.00f, 0.82f);
        var tile = new Vector2(2f, 2f);
        return CraftMesh.Shared(CraftMesh.Key("imagine-grass", tint, tile, 1.1f, 0.08f, 0.66f, 0.94f, 0f),
            () => CraftMesh.Felt(tex, tint, tile, 1.1f, 0.08f, 0.66f, 0.94f));
    }

    static PhysicsMaterial _grip;
    static PhysicsMaterial Grip()
    {
        if (_grip == null)
            _grip = new PhysicsMaterial("ArchGrip")
            {
                dynamicFriction = 0.85f,
                staticFriction = 1.0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        return _grip;
    }
}

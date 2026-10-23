using System.Collections.Generic;
using UnityEngine;

/// Arched rope walkway from Assets/Look/refs/library/rotate-walkway.mp4.
/// Packed yarn logs run across the lane, rise over a double corrugated arch, and step down at each end.
/// Three wood posts a side, a handrail and a lower rail, sage tufts at the four feet.
/// One call is one span along +X. The ground stays the collider. This prop has none.
public static class FeltWalkway
{
    /// Metres across the lane (z), posts included. Wide enough for the bean, whose capsule radius is 0.38.
    public const float Width = 1.4f;

    const float Foot = 0.15f;       // end post, where the ramp meets the arch
    const float RiseRate = 0.30f;   // crown height / span, from the side of the clip
    const float Clear = 0.003f;     // keeps the rope bottoms off the ground face
    const string Yarn = "Look/tex-yarn-fiber";
    const string KraftTex = "Look/tex-kraft-corrugated";
    const string Green = "Look/tex-yarn-green";

    static readonly Color Rope = new Color(1.02f, 0.96f, 0.88f);
    static readonly Color Wood = new Color(0.56f, 0.42f, 0.30f);
    static readonly Color RailWood = new Color(0.70f, 0.54f, 0.38f);
    static readonly Color Seam = new Color(0.22f, 0.14f, 0.09f);
    static readonly Color Kraft = new Color(0.48f, 0.34f, 0.22f);
    static readonly Color KraftDeep = new Color(0.30f, 0.20f, 0.13f);
    static readonly Color Moss = new Color(0.78f, 0.88f, 0.64f);

    static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

    /// <summary>
    /// One walkway from <paramref name="x0"/> to <paramref name="x1"/> along +X, centred on z = 0.
    /// <paramref name="surfaceY"/> is the existing ground top. The root is named Bean Walkway.
    /// Returns null when the span is shorter than 5 cm.
    /// </summary>
    public static GameObject Build(float x0, float x1, float surfaceY = 0f, Transform parent = null)
    {
        if (x1 < x0)
        {
            float swap = x0;
            x0 = x1;
            x1 = swap;
        }
        float length = x1 - x0;
        if (length < 0.05f) return null;

        float half = length * 0.5f;
        float rise = length * RiseRate;
        float postR = Width * 0.046f;
        float postZ = Width * 0.5f - postR;
        float plankHalf = postZ - postR * 0.42f;
        float faceZ = plankHalf - 0.016f;
        float postH = rise * 0.52f;

        var root = new GameObject("Bean Walkway");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.transform.position = new Vector3((x0 + x1) * 0.5f, surfaceY, 0f);
        root.isStatic = true;
        var host = root.transform;

        var rope = Mat(Yarn, Rope, 1.15f, 0.04f, 0.56f, 0.93f, 0.11f);
        var wood = Mat(Yarn, Wood, 1.40f, 0.05f, 0.46f, 0.90f, 0.05f);
        var railMat = Mat(Yarn, RailWood, 1.25f, 0.05f, 0.48f, 0.91f, 0.05f);
        var seamMat = Mat(Yarn, Seam, 0.55f, 0.02f, 0.30f, 0.97f, 0.02f);
        var kraft = Mat(KraftTex, Kraft, 1.20f, 0.03f, 0.42f, 0.96f, 0.02f);
        var kraftDeep = Mat(KraftTex, KraftDeep, 1.35f, 0.02f, 0.38f, 0.97f, 0.02f);
        var moss = Mat(Green, Moss, 1.80f, 0.28f, 0.72f, 0.94f, 0.12f, new Vector2(1.5f, 1.5f));

        const int samples = 96;
        var pts = new Vector3[samples + 1];
        var acc = new float[samples + 1];
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            pts[i] = new Vector3(Mathf.Lerp(-half, half, t), Bottom(t, rise), 0f);
            if (i > 0) acc[i] = acc[i - 1] + Vector3.Distance(pts[i], pts[i - 1]);
        }
        float total = acc[samples];
        float spacing = Mathf.Clamp(Width * 0.078f, total / 52f, total / 16f);
        int planks = Mathf.Max(1, Mathf.RoundToInt(total / spacing));
        float step = total / planks;
        float plankR = step * 0.40f;

        Vector3 OnArc(float arc)
        {
            arc = Mathf.Clamp(arc, 0f, total);
            int lo = 1;
            int hi = samples;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (acc[mid] < arc) lo = mid + 1;
                else hi = mid;
            }
            float span = acc[lo] - acc[lo - 1];
            float u = span < 1e-5f ? 0f : (arc - acc[lo - 1]) / span;
            return Vector3.Lerp(pts[lo - 1], pts[lo], u);
        }

        for (int i = 0; i < planks; i++)
        {
            Vector3 p = OnArc((i + 0.5f) * step);
            float y = p.y + plankR;
            Bar(host, "Walkway Plank", new Vector3(p.x, y, -plankHalf), new Vector3(p.x, y, plankHalf),
                plankR, 12, 1.35f, 3.2f, rope);
        }
        float seamR = step * 0.11f;
        for (int i = 1; i < planks; i++)
        {
            Vector3 p = OnArc(i * step);
            float y = p.y + plankR * 0.72f;
            Bar(host, "Walkway Seam", new Vector3(p.x, y, -plankHalf * 0.94f), new Vector3(p.x, y, plankHalf * 0.94f),
                seamR, 8, 1f, 2f, seamMat);
        }

        float[] postT = { Foot, 0.5f, 1f - Foot };
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * postZ;
            for (int i = 0; i < postT.Length; i++)
            {
                float t = postT[i];
                float top = Bottom(t, rise) + plankR * 2f + postH;
                float foot = i == 1 ? Bottom(t, rise) + plankR * 0.15f : 0f;
                Bar(host, "Walkway Post", new Vector3(X(t, half), foot, z), new Vector3(X(t, half), top, z),
                    postR, 12, 1.15f, 4.2f, wood);
            }
            float plankTop = plankR * 2f;
            Rail(host, half, rise, z, plankTop + postH * 0.22f, postR * 0.20f, postR, "Walkway Rail", railMat, 8, 3.6f);
            Rail(host, half, rise, z, plankTop + postH * 0.80f, postR * 0.42f, postR, "Walkway Handrail", railMat, 10, 3.2f);
        }

        float upperThick = rise * 0.17f;
        float upperDrop = 0.004f;
        float lowerDrop = upperDrop + upperThick + rise * 0.05f;
        float lowerThick = rise * 0.12f;
        for (int side = -1; side <= 1; side += 2)
        {
            Arch(host, half, rise, faceZ, side, 0, upperDrop, upperThick, 0.030f, 26, kraft);
            Arch(host, half, rise, faceZ - 0.006f, side, 1, lowerDrop, lowerThick, 0.022f, 20, kraftDeep);
        }
        Soffit(host, half, rise, faceZ - 0.034f, kraft);

        Tuft(host, new Vector3(X(0.04f, half), 0.008f, postZ + postR * 0.25f), moss, 2);
        Tuft(host, new Vector3(X(0.04f, half), 0.008f, -postZ - postR * 0.25f), moss, 5);
        Tuft(host, new Vector3(X(0.96f, half), 0.008f, postZ + postR * 0.2f), moss, 9);
        Tuft(host, new Vector3(X(0.96f, half), 0.008f, -postZ - postR * 0.2f), moss, 13);
        return root;
    }

    /// Rope-bottom height. Linear ramp outside the end posts, sine arch between them.
    static float DeckLift(float t, float rise)
    {
        t = Mathf.Clamp01(t);
        float edge = rise * 0.18f;
        if (t < Foot) return Mathf.Lerp(0f, edge, t / Foot);
        if (t > 1f - Foot) return Mathf.Lerp(0f, edge, (1f - t) / Foot);
        float u = (t - Foot) / (1f - 2f * Foot);
        return edge + Mathf.Sin(u * Mathf.PI) * (rise - edge);
    }

    static float Bottom(float t, float rise) => Clear + DeckLift(t, rise);

    static float X(float t, float half) => Mathf.Lerp(-half, half, t);

    static Vector3 Butt(Vector3 end, Vector3 other, float inset)
    {
        Vector3 flat = other - end;
        flat.y = 0f;
        float mag = flat.magnitude;
        if (mag < 0.001f) return end;
        return end + flat * (inset / mag);
    }

    static void Rail(Transform host, float half, float rise, float z, float lift, float radius, float postR, string name, Material mat, int sides, float perMeter)
    {
        float[] bay = { Foot, 0.5f, 1f - Foot };
        const int steps = 8;
        for (int b = 0; b < 2; b++)
        {
            var path = new List<Vector3>(steps + 1);
            for (int s = 0; s <= steps; s++)
            {
                float t = Mathf.Lerp(bay[b], bay[b + 1], s / (float)steps);
                path.Add(new Vector3(X(t, half), Bottom(t, rise) + lift, z));
            }
            if (Mathf.Abs(path[path.Count - 1].x - path[0].x) < postR * 2.5f) continue;
            path[0] = Butt(path[0], path[1], postR);
            int last = path.Count - 1;
            path[last] = Butt(path[last], path[last - 1], postR);
            var mesh = Sweep(path, radius, sides, perMeter, half, rise, z, lift, b);
            Shade(Piece(host, mesh, name, Vector3.zero, Quaternion.identity), mat);
        }
    }

    static void Bar(Transform host, string name, Vector3 a, Vector3 b, float radius, int sides, float uRepeat, float perMeter, Material mat)
    {
        Vector3 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.0008f || radius <= 0f) return;
        var mesh = Tube(radius, len, sides, uRepeat, len * perMeter);
        var go = Piece(host, mesh, name, (a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, delta / len));
        Shade(go, mat);
    }

    /// Straight yarn or wood bar. Mesh axis is Y. UV repeats are baked so one material keeps the grain density.
    static Mesh Tube(float radius, float length, int sides, float uRepeat, float vRepeat)
    {
        radius = Mathf.Max(0.0008f, radius);
        length = Mathf.Max(0.001f, length);
        sides = Mathf.Max(6, sides);
        string key = string.Format("walkway-tube-{0:0.####}-{1:0.####}-{2}-{3:0.##}-{4:0.##}", radius, length, sides, uRepeat, vRepeat);
        return Cached(key, () =>
        {
            int cols = sides + 1;
            float half = length * 0.5f;
            var v = new List<Vector3>(cols * 2 + 2);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(sides * 12);
            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? -half : half;
                for (int i = 0; i <= sides; i++)
                {
                    float u = i / (float)sides;
                    float a = u * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                    uv.Add(new Vector2(u * uRepeat, ring * vRepeat));
                }
            }
            for (int i = 0; i < sides; i++)
            {
                float a = (i + 0.5f) / sides * Mathf.PI * 2f;
                Quad(v, tris, i, i + 1, i + 1 + cols, i + cols, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
            }
            int bot = v.Count;
            v.Add(new Vector3(0f, -half, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            int top = v.Count;
            v.Add(new Vector3(0f, half, 0f));
            uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                Tri(v, tris, bot, i + 1, i, Vector3.down);
                Tri(v, tris, top, cols + i, cols + i + 1, Vector3.up);
            }
            return Finish(v, uv, tris, key);
        });
    }

    static Mesh Sweep(List<Vector3> path, float radius, int sides, float perMeter, float half, float rise, float z, float lift, int bay)
    {
        sides = Mathf.Max(6, sides);
        string key = string.Format("walkway-rail-{0:0.###}-{1:0.###}-{2:0.###}-{3:0.###}-{4:0.###}-{5}-{6}-{7:0.##}",
            half, rise, z, lift, radius, sides, bay, perMeter);
        return Cached(key, () =>
        {
            int n = path.Count;
            int cols = sides + 1;
            var v = new List<Vector3>(n * cols + 2);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>((n - 1) * sides * 6 + sides * 6);
            float dist = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = i == 0 ? path[1] - path[0] : i == n - 1 ? path[i] - path[i - 1] : path[i + 1] - path[i - 1];
                if (dir.sqrMagnitude < 1e-8f) dir = Vector3.right;
                dir.Normalize();
                Vector3 side = Vector3.Cross(dir, Vector3.up);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(dir, Vector3.forward);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, dir);
                if (i > 0) dist += Vector3.Distance(path[i], path[i - 1]);
                for (int s = 0; s <= sides; s++)
                {
                    float u = s / (float)sides;
                    float a = u * Mathf.PI * 2f;
                    v.Add(path[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius);
                    uv.Add(new Vector2(u * 1.15f, dist * perMeter));
                }
                if (i == 0) continue;
                int row = i * cols;
                int prev = (i - 1) * cols;
                for (int s = 0; s < sides; s++)
                    Quad(v, tris, prev + s, prev + s + 1, row + s + 1, row + s, v[prev + s] - path[i - 1]);
            }
            Cap(v, uv, tris, path[0], 0, sides, (path[0] - path[1]).normalized);
            Cap(v, uv, tris, path[n - 1], (n - 1) * cols, sides, (path[n - 1] - path[n - 2]).normalized);
            return Finish(v, uv, tris, key);
        });
    }

    static void Cap(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector3 center, int ring, int sides, Vector3 outward)
    {
        int c = v.Count;
        v.Add(center);
        uv.Add(new Vector2(0.5f, 0.5f));
        for (int s = 0; s < sides; s++)
            Tri(v, tris, c, ring + s, ring + s + 1, outward);
    }

    /// Corrugated cheek under the ropes. Two layers a side, from the clip's double arch.
    static void Arch(Transform host, float half, float rise, float faceZ, int zSign, int layer, float drop, float thick, float depth, int ribs, Material mat)
    {
        var mesh = Fascia(half, rise, faceZ, zSign, layer, drop, thick, depth, ribs);
        if (mesh == null) return;
        Shade(Piece(host, mesh, "Walkway Arch", Vector3.zero, Quaternion.identity), mat);
    }

    static Mesh Fascia(float half, float rise, float faceZ, int zSign, int layer, float drop, float thick, float depth, int ribs)
    {
        float t0 = Foot;
        float t1 = 1f - Foot;
        while (t0 < 0.42f && !BandFits(t0, rise, drop, thick)) t0 += 0.01f;
        while (t1 > 0.58f && !BandFits(t1, rise, drop, thick)) t1 -= 0.01f;
        if (t1 - t0 < 0.12f) return null;

        string key = string.Format("walkway-arch-{0:0.###}-{1:0.###}-{2:0.###}-{3}-{4}-{5:0.###}-{6:0.###}",
            half, rise, faceZ, zSign, layer, drop, thick);
        return Cached(key, () =>
        {
            const int n = 36;
            var v = new List<Vector3>((n + 1) * 4);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(n * 24);
            for (int i = 0; i <= n; i++)
            {
                float f = i / (float)n;
                float t = Mathf.Lerp(t0, t1, f);
                float x = X(t, half);
                float yTop = Bottom(t, rise) - drop;
                float yBot = Mathf.Max(0.008f, yTop - thick);
                float wave = Mathf.Sin(f * ribs * Mathf.PI * 2f) * 0.004f;
                float zOut = zSign * (faceZ + wave);
                float zIn = zSign * (faceZ - depth);
                v.Add(new Vector3(x, yTop, zOut));
                v.Add(new Vector3(x, yBot, zOut));
                v.Add(new Vector3(x, yBot, zIn));
                v.Add(new Vector3(x, yTop, zIn));
                uv.Add(new Vector2(f * ribs, 1f));
                uv.Add(new Vector2(f * ribs, 0f));
                uv.Add(new Vector2(f * ribs, 0f));
                uv.Add(new Vector2(f * ribs, 1f));
            }
            Vector3 outZ = zSign * Vector3.forward;
            for (int i = 0; i < n; i++)
            {
                int a = i * 4;
                int b = a + 4;
                Quad(v, tris, a, b, b + 1, a + 1, outZ);
                Quad(v, tris, a + 1, b + 1, b + 2, a + 2, Vector3.down);
                Quad(v, tris, a + 2, b + 2, b + 3, a + 3, -outZ);
                Quad(v, tris, a + 3, b + 3, b, a, Vector3.up);
            }
            Quad(v, tris, 0, 3, 2, 1, Vector3.left);
            int e = n * 4;
            Quad(v, tris, e, e + 1, e + 2, e + 3, Vector3.right);
            return Finish(v, uv, tris, key);
        });
    }

    static bool BandFits(float t, float rise, float drop, float thick)
    {
        float yTop = Bottom(t, rise) - drop;
        return yTop - thick >= 0.012f;
    }

    /// Cardboard underside between the cheeks, so the opening reads as a vault rather than a hole.
    static void Soffit(Transform host, float half, float rise, float zHalf, Material mat)
    {
        if (zHalf < 0.05f) return;
        float t0 = Foot + 0.03f;
        float t1 = 1f - Foot - 0.03f;
        string key = string.Format("walkway-soffit-{0:0.###}-{1:0.###}-{2:0.###}", half, rise, zHalf);
        var mesh = Cached(key, () =>
        {
            const int nx = 28;
            const int nz = 6;
            int cols = nx + 1;
            var v = new List<Vector3>(cols * (nz + 1) * 2);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(nx * nz * 12);
            int Top(int ix, int iz) => iz * cols + ix;
            for (int iz = 0; iz <= nz; iz++)
            {
                float z = Mathf.Lerp(-zHalf, zHalf, iz / (float)nz);
                for (int ix = 0; ix <= nx; ix++)
                {
                    float f = ix / (float)nx;
                    float t = Mathf.Lerp(t0, t1, f);
                    float y = Bottom(t, rise) - 0.006f + Mathf.Sin(iz / (float)nz * Mathf.PI) * 0.002f;
                    v.Add(new Vector3(X(t, half), y, z));
                    uv.Add(new Vector2(f * 12f, iz / (float)nz * 4f));
                }
            }
            int baseBot = v.Count;
            for (int i = 0; i < baseBot; i++)
            {
                var p = v[i];
                p.y -= 0.008f;
                v.Add(p);
                uv.Add(uv[i]);
            }
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int a = Top(ix, iz);
                    Quad(v, tris, a, a + 1, a + 1 + cols, a + cols, Vector3.up);
                    int c = baseBot + a;
                    Quad(v, tris, c, c + cols, c + 1 + cols, c + 1, Vector3.down);
                }
            return Finish(v, uv, tris, key);
        });
        Shade(Piece(host, mesh, "Walkway Arch", Vector3.zero, Quaternion.identity), mat);
    }

    static void Tuft(Transform host, Vector3 at, Material mat, int seed)
    {
        var puff = Piece(host, CraftMesh.Pompom(0.022f, 0.16f, 10, 8, seed), "Walkway Tuft", at + Vector3.up * 0.016f, Quaternion.identity);
        Shade(puff, mat);
        float yaw0 = Mathf.Atan2(at.z, at.x);
        for (int i = 0; i < 5; i++)
        {
            float yaw = yaw0 + (i - 2f) * 0.48f;
            const float lean = 1.02f;
            var aim = new Vector3(Mathf.Sin(yaw) * Mathf.Sin(lean), Mathf.Cos(lean), Mathf.Cos(yaw) * Mathf.Sin(lean));
            float len = 0.05f + Hash(seed, i) * 0.03f;
            float ry = len * 0.5f;
            var leaf = Piece(host, CraftMesh.Egg(0.011f, ry, 0.0065f, 0.55f, 10, 8), "Walkway Tuft",
                at + aim * ry + Vector3.up * 0.006f, AimUp(aim));
            Shade(leaf, mat);
        }
    }

    static Quaternion AimUp(Vector3 aim)
    {
        if (aim.sqrMagnitude < 1e-6f) aim = Vector3.up;
        aim.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, aim);
        if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
        side.Normalize();
        return Quaternion.LookRotation(Vector3.Cross(aim, side), aim);
    }

    static float Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)b * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 1023) / 1023f;
        }
    }

    static Material Mat(string resource, Color tint, float bump, float sheen, float wrap, float rough, float fiber)
    {
        return Mat(resource, tint, bump, sheen, wrap, rough, fiber, Vector2.one);
    }

    static Material Mat(string resource, Color tint, float bump, float sheen, float wrap, float rough, float fiber, Vector2 tile)
    {
        string key = "walkway|" + CraftMesh.Key(resource, tint, tile, bump, sheen, wrap, rough, 0f) + "|f" + fiber.ToString("0.###");
        return CraftMesh.Shared(key, () =>
        {
            var tex = Resources.Load<Texture2D>(resource);
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough);
            if (mat != null && mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
            return mat;
        });
    }

    static GameObject Piece(Transform parent, Mesh mesh, string name, Vector3 localPos, Quaternion localRot)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        go.isStatic = true;
        return go;
    }

    static void Shade(GameObject go, Material mat)
    {
        if (go == null) return;
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        rend.sharedMaterial = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        rend.receiveShadows = true;
    }

    static void Quad(List<Vector3> v, List<int> tris, int a, int b, int c, int d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }
        else
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(a); tris.Add(d); tris.Add(c);
        }
    }

    static void Tri(List<Vector3> v, List<int> tris, int a, int b, int c, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
        }
        else
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
        }
    }

    static Mesh Finish(List<Vector3> v, List<Vector2> uv, List<int> tris, string name)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Mesh Cached(string key, System.Func<Mesh> build)
    {
        if (Meshes.TryGetValue(key, out var found) && found != null) return found;
        var mesh = build();
        Meshes[key] = mesh;
        return mesh;
    }
}

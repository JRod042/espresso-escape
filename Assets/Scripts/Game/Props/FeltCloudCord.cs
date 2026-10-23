using System.Collections.Generic;
using UnityEngine;

/// rotate-cloud.mp4 and rotate-cord.mp4. The stills lose.
/// The cloud is one stuffed felt pillow, wider than tall (about 1.8), with a blanket-stitch
/// lip around the scalloped edge. It is not a cluster of loose wool balls.
/// Left tip sits low, left shoulder is the high plateau, center dome is tallest,
/// right shoulder and right tip step down. Bottom is three scallops. The center one is
/// largest and lowest. The upper face leans toward local −Z so the strand can pass behind it.
/// Side thickness is about 0.45 of the height. Scale 1 center lobe is 0.40 across.
///
/// The cord is the loose strand in rotate-cord.mp4: three-ply jute, one turn about every
/// 5.5 diameters, short flyaway hairs. The later frames are that same strand wound on a spool.
/// Cord() is the strand from a to b. Sag stays 0.11 of the span so the backdrop hang still meets it.
/// Cloud builds one cloud. It does not hang the three.
public static class FeltCloudCord
{
    // Outer radius of the strand. Diameter 0.04 = one-tenth of the scale-1 center lobe. Do not raise this.
    const float CordRadius = 0.02f;

    // x, y, radius. Nine discs. The union is the front silhouette of rotate-cloud.mp4 frame 0.
    static readonly float[] Lobes =
    {
        -0.42f, -0.08f, 0.100f, // left tip, low
        -0.26f,  0.04f, 0.150f, // left shoulder
        -0.10f,  0.07f, 0.145f, // inner left, rises into the dome
         0.01f,  0.10f, 0.188f, // center dome, tallest
         0.21f,  0.015f, 0.135f, // right shoulder, lower than the left
         0.40f, -0.08f, 0.100f, // right tip, lower still
        -0.20f, -0.05f, 0.145f, // lower left scallop
         0.02f, -0.08f, 0.200f, // lower center, largest. Diameter 0.40.
         0.26f, -0.09f, 0.142f  // lower right
    };

    // tex-felt-cream mean is 244.7, 233.1, 223.0.
    // rotate-cloud.mp4 body is p50 244,235,226 and p75 250,243,236. Albedo is the lit face.
    static readonly Color IvoryMul = new Color(248f / 244.7f, 240f / 233.1f, 232f / 223f);
    static readonly Color IvoryFlat = new Color(248f / 255f, 240f / 255f, 232f / 255f);

    // tex-yarn-fiber mean is 147.9, 115.9, 82.0.
    // rotate-cord.mp4 strand is p75 192,152,116 and p90 206,168,135. Pale tan, less red than 196,158,119.
    static readonly Color JuteMul = new Color(200f / 147.9f, 162f / 115.9f, 124f / 82f);
    static readonly Color JuteFlat = new Color(200f / 255f, 162f / 255f, 124f / 255f);

    static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

    /// One cloud under parent. localPosition and scale are parent-local.
    /// Seed moves the nap and the lobes a little. It does not copy a second cloud.
    public static GameObject Cloud(Transform parent, Vector3 localPosition, float scale, int seed = 1)
    {
        var root = new GameObject("Felt Cloud");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(
            (Hash01(seed, 1) - 0.5f) * 3f,
            (Hash01(seed, 2) - 0.5f) * 8f,
            (Hash01(seed, 3) - 0.5f) * 3f);
        root.transform.localScale = Vector3.one * Mathf.Max(0.02f, scale);

        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = CottonMesh(seed);
        var rend = root.AddComponent<MeshRenderer>();
        rend.sharedMaterial = Ivory();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return root;
    }

    /// One strand from a to b in parent-local space (world space when parent is null).
    /// The middle sags. Both ends stay on a and b and sit higher than the middle.
    /// Radius stays 0.02. No collider and no rigidbody.
    public static GameObject Cord(Transform parent, Vector3 a, Vector3 b, int seed = 1)
    {
        var root = new GameObject("Jute Cord");
        if (parent != null) root.transform.SetParent(parent, false);

        Vector3 span = b - a;
        float len = span.magnitude;
        if (len < 0.001f) return root;

        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = CordMesh(a, b, len, seed);
        var rend = root.AddComponent<MeshRenderer>();
        rend.sharedMaterial = Jute();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return root;
    }

    static Mesh CottonMesh(int seed)
    {
        string key = "FeltPillow_" + seed;
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int su = 96;
        const int sv = 32;
        const float halfDepth = 0.115f;
        int cols = su + 1;
        int rings = sv - 1;
        var v = new List<Vector3>((rings + 2) * cols + su * 8);
        var uv = new List<Vector2>(v.Capacity);
        var t = new List<int>(su * sv * 6 + su * 36);
        float ou = (Hash01(seed, 4) - 0.5f) * 0.04f;
        float ov = (Hash01(seed, 5) - 0.5f) * 0.04f;

        int Add(Vector3 p)
        {
            v.Add(p);
            // One patch of tex-felt-cream. A sphere wrap turns the nap into a stripe.
            uv.Add(new Vector2(0.5f + ou + p.x * 0.46f, 0.5f + ov + p.y * 0.62f));
            return v.Count - 1;
        }

        int front = Add(new Vector3(0f, 0f, -halfDepth));
        int ring0 = v.Count;
        for (int iv = 1; iv < sv; iv++)
        {
            float theta = iv / (float)sv * Mathf.PI;
            float s = Mathf.Sin(theta);
            float face = Mathf.Pow(Mathf.Max(s, 1e-4f), 0.66f);
            float seam = Mathf.Exp(-14f * (theta - Mathf.PI * 0.5f) * (theta - Mathf.PI * 0.5f));
            for (int iu = 0; iu <= su; iu++)
            {
                float phi = (iu % su) / (float)su * Mathf.PI * 2f;
                float dx = Mathf.Cos(phi);
                float dy = Mathf.Sin(phi);
                float sil = Silhouette(phi, seed);
                float rad = sil * face * (1f + Nap(phi, theta, seed)) * (1f + 0.03f * seam);
                float z = -Mathf.Cos(theta) * halfDepth;
                // Upper face toward the camera. The rim stays on z = 0 so the stitch can sit on it.
                z -= 0.05f * Mathf.Clamp01(dy) * Mathf.Clamp01(Mathf.Cos(theta));
                Add(new Vector3(dx * rad, dy * rad, z));
            }
        }
        int back = Add(new Vector3(0f, 0f, halfDepth));

        for (int iu = 0; iu < su; iu++)
            Tri(v, t, front, ring0 + iu, ring0 + iu + 1, Vector3.back);
        for (int iv = 0; iv < rings - 1; iv++)
        {
            int rowA = ring0 + iv * cols;
            int rowB = rowA + cols;
            for (int iu = 0; iu < su; iu++)
                Quad(v, t, rowA + iu, rowA + iu + 1, rowB + iu + 1, rowB + iu, v[rowA + iu]);
        }
        int last = ring0 + (rings - 1) * cols;
        for (int iu = 0; iu < su; iu++)
            Tri(v, t, back, last + iu + 1, last + iu, Vector3.forward);

        // Blanket stitch. rotate-cloud.mp4: short dashes across the scalloped lip, one every ~0.02.
        int eq = sv / 2;
        int eqRow = ring0 + (eq - 1) * cols;
        for (int iu = 0; iu < su; iu++)
        {
            Vector3 p = v[eqRow + iu];
            Vector3 tangent = v[eqRow + iu + 1] - p;
            Vector3 outward = new Vector3(p.x, p.y, 0f);
            float scale = 0.86f + Hash01(seed, 200 + iu) * 0.28f;
            AddStitch(v, uv, t, p, outward, tangent, scale);
        }

        var mesh = new Mesh { name = "Felt Cloud" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        var nrm = mesh.normals;
        for (int iv = 0; iv < rings; iv++)
        {
            int a = ring0 + iv * cols;
            int b = a + su;
            Vector3 avg = (nrm[a] + nrm[b]).normalized;
            nrm[a] = avg;
            nrm[b] = avg;
        }
        mesh.normals = nrm;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        Meshes[key] = mesh;
        return mesh;
    }

    /// Smooth union of the scallop discs in the cloud plane.
    static float Silhouette(float phi, int seed)
    {
        float dx = Mathf.Cos(phi);
        float dy = Mathf.Sin(phi);
        float r = -1f;
        int n = Lobes.Length / 3;
        const float k = 0.02f;
        for (int i = 0; i < n; i++)
        {
            Vector2 c = LobeCenter(i, seed);
            float hit = FarCircle(dx, dy, c, LobeRadius(i, seed));
            if (hit <= 0f) continue;
            r = r < 0f ? hit : SmoothMax(r, hit, k);
        }
        if (r < 0.05f) r = 0.2f;
        return r;
    }

    static float FarCircle(float dx, float dy, Vector2 c, float radius)
    {
        float along = dx * c.x + dy * c.y;
        float disc = along * along - c.sqrMagnitude + radius * radius;
        if (disc <= 0f) return -1f;
        return along + Mathf.Sqrt(disc);
    }

    static float SmoothMax(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(a, b, h) + k * h * (1f - h);
    }

    static Vector2 LobeCenter(int i, int seed)
    {
        int o = i * 3;
        const float j = 0.01f;
        return new Vector2(
            Lobes[o] + (Hash01(seed, 10 + i) - 0.5f) * j,
            Lobes[o + 1] + (Hash01(seed, 30 + i) - 0.5f) * j * 0.35f);
    }

    static float LobeRadius(int i, int seed)
    {
        return Lobes[i * 3 + 2] * (0.985f + Hash01(seed, 70 + i) * 0.03f);
    }

    static float Nap(float phi, float theta, int seed)
    {
        float p = seed * 0.17f;
        float a = Mathf.PerlinNoise(phi * 1.6f + p, theta * 0.8f + 0.4f);
        float b = Mathf.PerlinNoise(phi * 5.2f + 1.7f, theta * 3.4f + p);
        return (a - 0.5f) * 0.028f + (b - 0.5f) * 0.012f;
    }

    /// One raised dash across the lip. Long axis points out of the scallop.
    static void AddStitch(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 rim, Vector3 outward, Vector3 tangent, float scale)
    {
        Vector3 o = outward.sqrMagnitude > 1e-8f ? outward.normalized : Vector3.right;
        Vector3 g = tangent - o * Vector3.Dot(tangent, o);
        if (g.sqrMagnitude < 1e-8f) g = Vector3.up;
        g.Normalize();
        Vector3 z = Vector3.Cross(o, g);
        if (z.sqrMagnitude < 1e-8f) z = Vector3.forward;
        else z.Normalize();

        float len = 0.015f * scale;
        float hw = 0.0031f * scale;
        float hz = 0.0046f * scale;
        Vector3 c = rim + o * (0.0025f + len * 0.5f);
        Vector3 ax = o * (len * 0.5f);
        Vector3 ay = g * hw;
        Vector3 az = z * hz;

        int i = v.Count;
        v.Add(c - ax - ay - az);
        v.Add(c + ax - ay - az);
        v.Add(c + ax + ay - az);
        v.Add(c - ax + ay - az);
        v.Add(c - ax - ay + az);
        v.Add(c + ax - ay + az);
        v.Add(c + ax + ay + az);
        v.Add(c - ax + ay + az);
        for (int k = 0; k < 8; k++) uv.Add(new Vector2(0.52f, 0.58f));

        Quad(v, t, i + 0, i + 1, i + 2, i + 3, -z);
        Quad(v, t, i + 4, i + 5, i + 6, i + 7, z);
        Quad(v, t, i + 0, i + 1, i + 5, i + 4, -g);
        Quad(v, t, i + 3, i + 2, i + 6, i + 7, g);
        Quad(v, t, i + 0, i + 3, i + 7, i + 4, -o);
        Quad(v, t, i + 1, i + 2, i + 6, i + 5, o);
    }

    static Mesh CordMesh(Vector3 a, Vector3 b, float len, int seed)
    {
        Vector3 dir = (b - a) / len;
        Vector3 sagDir = Vector3.down - dir * Vector3.Dot(Vector3.down, dir);
        if (sagDir.sqrMagnitude < 1e-6f)
            sagDir = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right);
        sagDir.Normalize();

        // FeltWall.CordHang uses this 0.11. The visible belly in the cropped frame is shallower
        // because the ends leave the picture still sloping.
        float sag = len * (0.11f + (Hash01(seed, 4) - 0.5f) * 0.01f);
        int along = Mathf.Clamp(Mathf.RoundToInt(len / 0.035f), 10, 220);
        const int around = 18;
        int cols = around + 1;
        // rotate-cord.mp4: highlight repeat ~1.8 diameters, three plies, so one full turn ~5.5 diameters.
        const float turnsPerUnit = 4.6f;
        float phase = Hash01(seed, 6) * Mathf.PI * 2f;

        var centers = new Vector3[along + 1];
        var tangents = new Vector3[along + 1];
        for (int i = 0; i <= along; i++)
        {
            float u = i / (float)along;
            float belly = 4f * u * (1f - u);
            centers[i] = Vector3.Lerp(a, b, u) + sagDir * (sag * belly);
        }
        for (int i = 0; i <= along; i++)
        {
            Vector3 tng = i == 0 ? centers[1] - centers[0]
                : i == along ? centers[along] - centers[along - 1]
                : centers[i + 1] - centers[i - 1];
            tangents[i] = tng.sqrMagnitude > 1e-8f ? tng.normalized : dir;
        }

        var sides = new Vector3[along + 1];
        var bins = new Vector3[along + 1];
        Vector3 carry = Vector3.Cross(tangents[0], sagDir);
        if (carry.sqrMagnitude < 1e-8f) carry = Vector3.Cross(tangents[0], Vector3.right);
        carry.Normalize();
        for (int i = 0; i <= along; i++)
        {
            Vector3 tng = tangents[i];
            Vector3 bin = Vector3.Cross(tng, carry);
            if (bin.sqrMagnitude < 1e-8f) bin = Vector3.Cross(tng, Vector3.up);
            bin.Normalize();
            carry = Vector3.Cross(bin, tng).normalized;
            sides[i] = carry;
            bins[i] = bin;
            if (i < along)
            {
                carry = carry - tangents[i + 1] * Vector3.Dot(carry, tangents[i + 1]);
                if (carry.sqrMagnitude < 1e-8f) carry = Vector3.Cross(tangents[i + 1], sagDir);
                else carry.Normalize();
            }
        }

        var v = new List<Vector3>((along + 1) * cols + 64);
        var uv = new List<Vector2>(v.Capacity);
        var t = new List<int>(along * around * 6 + 128);

        for (int i = 0; i <= along; i++)
        {
            float u = i / (float)along;
            // S-lay. On a left-to-right strand the ridges fall from upper left to lower right.
            float twist = phase - u * turnsPerUnit * Mathf.PI * 2f;
            for (int j = 0; j <= around; j++)
            {
                float ang = j / (float)around * Mathf.PI * 2f;
                float a0 = ang - twist;
                float m = Mathf.Cos(a0);
                float m1 = Mathf.Cos(a0 - 2.09439516f);
                float m2 = Mathf.Cos(a0 + 2.09439516f);
                if (m1 > m) m = m1;
                if (m2 > m) m = m2;
                float lobe = Mathf.Clamp01((m - 0.5f) * 2f);
                lobe = Mathf.Pow(lobe, 0.6f);
                float n = Mathf.PerlinNoise(u * len * 6.5f + seed * 0.13f, ang * 0.15f + 0.2f);
                // Crown stays under CordRadius. Grooves are deep enough to read as three strands.
                float rad = CordRadius * 0.96f * Mathf.Lerp(0.62f, 1f, lobe);
                rad *= 1f + (n - 0.5f) * 0.05f;
                v.Add(centers[i] + (sides[i] * Mathf.Cos(ang) + bins[i] * Mathf.Sin(ang)) * rad);
                // Fibers run along the strand. The ply shape is the mesh, not a second braid in the map.
                uv.Add(new Vector2(j / (float)around * 2f, u * len * 14f));
            }
        }

        for (int i = 0; i < along; i++)
        {
            for (int j = 0; j < around; j++)
            {
                int s = i * cols + j;
                Quad(v, t, s, s + 1, s + cols + 1, s + cols, v[s] - centers[i]);
            }
        }

        Cap(v, uv, t, centers[0], 0, around, -tangents[0]);
        Cap(v, uv, t, centers[along], along * cols, around, tangents[along]);

        var mesh = new Mesh { name = "Jute Cord" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals();
        var nrm = mesh.normals;
        for (int i = 0; i <= along; i++)
        {
            int s = i * cols;
            int e = s + around;
            Vector3 avg = (nrm[s] + nrm[e]).normalized;
            nrm[s] = avg;
            nrm[e] = avg;
        }
        mesh.normals = nrm;

        var verts = new List<Vector3>(mesh.vertices);
        var norms = new List<Vector3>(mesh.normals);
        var uvs = new List<Vector2>(mesh.uv);
        var tris = new List<int>(mesh.triangles);
        int hairs = Mathf.Clamp(Mathf.RoundToInt(len / 0.05f), 8, 280);
        for (int h = 0; h < hairs; h++)
        {
            float u = (h + 0.35f) / hairs;
            int i = Mathf.Clamp(Mathf.RoundToInt(u * along), 1, along - 1);
            float ang = Hash01(seed, 20 + h) * Mathf.PI * 2f;
            float twist = phase - u * turnsPerUnit * Mathf.PI * 2f;
            float a0 = ang - twist;
            float m = Mathf.Cos(a0);
            float m1 = Mathf.Cos(a0 - 2.09439516f);
            float m2 = Mathf.Cos(a0 + 2.09439516f);
            if (m1 > m) m = m1;
            if (m2 > m) m = m2;
            float lobe = Mathf.Pow(Mathf.Clamp01((m - 0.5f) * 2f), 0.6f);
            float rad = CordRadius * 0.96f * Mathf.Lerp(0.62f, 1f, lobe);
            Vector3 outward = sides[i] * Mathf.Cos(ang) + bins[i] * Mathf.Sin(ang);
            Vector3 root = centers[i] + outward * rad;
            float trail = Hash01(seed, 40 + h) > 0.5f ? 1f : -1f;
            Vector3 fly = outward * 0.82f + tangents[i] * trail * 0.4f + sagDir * (Hash01(seed, 80 + h) - 0.5f) * 0.35f;
            if (fly.sqrMagnitude < 1e-8f) continue;
            fly.Normalize();
            float hairLen = 0.006f + Hash01(seed, 60 + h) * 0.014f;
            AddHair(verts, norms, uvs, tris, root, fly, hairLen, 0.0008f);
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static void Cap(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 center, int ring, int around, Vector3 outward)
    {
        int c = v.Count;
        v.Add(center);
        uv.Add(new Vector2(0.5f, 0.5f));
        for (int j = 0; j < around; j++)
            Tri(v, t, c, ring + j, ring + j + 1, outward);
    }

    /// Two crossed slivers. Much thinner than the strand, so they read as flyaway hairs.
    static void AddHair(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 root, Vector3 dir, float len, float halfWidth)
    {
        Vector3 tip = root + dir * len;
        Vector3 side = Vector3.Cross(dir, Vector3.up);
        if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(dir, Vector3.right);
        side.Normalize();
        Vector3 bin = Vector3.Cross(dir, side).normalized;
        Ribbon(v, n, uv, t, root, tip, side * halfWidth);
        Ribbon(v, n, uv, t, root, tip, bin * halfWidth);
    }

    static void Ribbon(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t, Vector3 root, Vector3 tip, Vector3 side)
    {
        Vector3 normal = Vector3.Cross(tip - root, side);
        if (normal.sqrMagnitude < 1e-10f) normal = Vector3.up;
        else normal.Normalize();
        int i = v.Count;
        v.Add(root - side);
        v.Add(root + side);
        v.Add(tip + side * 0.25f);
        v.Add(tip - side * 0.25f);
        for (int k = 0; k < 4; k++) n.Add(normal);
        uv.Add(new Vector2(0f, 0f));
        uv.Add(new Vector2(1f, 0f));
        uv.Add(new Vector2(1f, 1f));
        uv.Add(new Vector2(0f, 1f));
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);

        int j = v.Count;
        v.Add(root - side);
        v.Add(root + side);
        v.Add(tip + side * 0.25f);
        v.Add(tip - side * 0.25f);
        for (int k = 0; k < 4; k++) n.Add(-normal);
        uv.Add(new Vector2(0f, 0f));
        uv.Add(new Vector2(1f, 0f));
        uv.Add(new Vector2(1f, 1f));
        uv.Add(new Vector2(0f, 1f));
        t.Add(j); t.Add(j + 2); t.Add(j + 1);
        t.Add(j); t.Add(j + 3); t.Add(j + 2);
    }

    /// Escape/FeltFiber over Resources Look/tex-felt-cream. Matte stuffed felt. Stitches are the mesh.
    static Material Ivory()
    {
        var tile = Vector2.one;
        const float bump = 0.95f, sheen = 0.18f, wrap = 0.92f, rough = 0.98f;
        return CraftMesh.Shared(CraftMesh.Key("Look/tex-felt-cream", IvoryMul, tile, bump, sheen, wrap, rough, 0f), () =>
        {
            var tex = Resources.Load<Texture2D>("Look/tex-felt-cream");
            var tint = tex != null ? IvoryMul : IvoryFlat;
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough);
            if (mat != null && mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", 0.05f);
            return mat;
        });
    }

    /// One strand of Resources Look/tex-yarn-fiber. The three plies are the mesh.
    static Material Jute()
    {
        var tile = Vector2.one;
        const float bump = 0.7f, sheen = 0.06f, wrap = 0.58f, rough = 0.98f;
        return CraftMesh.Shared(CraftMesh.Key("Look/tex-yarn-fiber", JuteMul, tile, bump, sheen, wrap, rough, 0f), () =>
        {
            var tex = Resources.Load<Texture2D>("Look/tex-yarn-fiber");
            var tint = tex != null ? JuteMul : JuteFlat;
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough);
            if (mat != null && mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", 0.14f);
            return mat;
        });
    }

    static void Quad(List<Vector3> v, List<int> tris, int a, int b, int c, int d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }
        else
        {
            tris.Add(a); tris.Add(d); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(b);
        }
    }

    static void Tri(List<Vector3> v, List<int> tris, int a, int b, int c, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f) { tris.Add(a); tris.Add(b); tris.Add(c); }
        else { tris.Add(a); tris.Add(c); tris.Add(b); }
    }

    static float Hash01(int a, int b)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)b * 668265263u + 0x9E3779B9u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }
}

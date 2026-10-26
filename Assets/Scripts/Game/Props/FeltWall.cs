using System.Collections.Generic;
using UnityEngine;

/// rotate-wall.mp4. One kraft sheet: vertical flutes, a thick cut edge, one jute cord, three cotton clouds.
/// n014 y160: crest 188, 147, 107 and groove 101, 64, 29, period 30.7 px, about five flutes across the center cloud.
/// The cord sits in the upper third. The center cloud is the low point of a short sag. The sheet top is above the cord.
public static class FeltWall
{
    // tex-kraft-flute is 233, 183, 139 at the crest and 114, 72, 32 in the groove.
    // 0.86, 0.84, 0.80 brings that crest to about 200, 154, 111 so the key can land near the plate.
    static readonly Color KraftMul = new Color(0.86f, 0.84f, 0.80f);
    static readonly Color KraftFlat = new Color(0.737f, 0.576f, 0.420f);
    static readonly Color GrooveMul = new Color(0.90f, 0.87f, 0.76f);
    static readonly Color GrooveFlat = new Color(0.392f, 0.302f, 0.224f);

    // tex-yarn-fiber mean is about 148, 116, 82. Pale jute, still fine, not a light rope.
    static readonly Color JuteMul = new Color(1.35f, 1.48f, 1.58f);
    static readonly Color JuteFlat = new Color(0.784f, 0.675f, 0.510f);

    static readonly Color CottonMul = new Color(1f, 0.985f, 0.96f);
    static readonly Color CottonFlat = new Color(0.96f, 0.91f, 0.86f);

    const float Width = 22f;
    const float Height = 6.6f;
    // n014: 14 crests from x27 to x426, mean gap 30.7 px. Center cloud is about five of those gaps wide.
    // Pitch 0.22 m is five flutes across a scale-1.15 cloud (lobe span 0.955).
    const int Ribs = 100;
    const float FluteR = 0.098f;
    const float FluteDeg = 78f;
    const float CrownZ = -0.050f;    // crest, toward the camera
    const float Amp = 0.050f;        // valley sits this far behind the crest; round rib, not a trench
    const float Sheet = 0.085f;      // back liner, constant thickness behind the front wave
    const float GrooveDrop = 0.010f;
    const float KraftU = 0.012f;
    // Upper-wall jute garland and cotton pillow clouds (285.jpg, set-01.jpg, rotate-wall.mp4).
    const float CordX0 = -3.4f;
    const float CordX1 = 1.4f;
    const float CordX2 = 6.2f;
    const float CordY = 3.12f;
    const float CordSag = 0.32f;
    const float CordRadius = 0.015f;
    const float CloudZ = -0.14f;
    const float CordZ = -0.16f;

    // Overlapping puffs. Centres sit inside each other's radii so they melt into cotton, not a row of balls.
    static readonly Vector3[] LobeAt =
    {
        new Vector3(-0.22f, -0.03f, 0.02f),
        new Vector3( 0.20f, -0.01f, 0.00f),
        new Vector3( 0.00f,  0.16f, 0.01f),
        new Vector3(-0.06f,  0.02f, 0.10f),
        new Vector3( 0.08f, -0.05f,-0.08f),
        new Vector3(-0.38f,  0.08f, 0.02f)
    };
    static readonly Vector3[] LobeRad =
    {
        new Vector3(0.42f, 0.30f, 0.24f),
        new Vector3(0.38f, 0.28f, 0.22f),
        new Vector3(0.32f, 0.24f, 0.20f),
        new Vector3(0.28f, 0.20f, 0.18f),
        new Vector3(0.26f, 0.18f, 0.16f),
        new Vector3(0.24f, 0.18f, 0.16f)
    };

    static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

    /// Root "Kraft Backdrop". Position is the floor point on the wall.
    /// The sheet rises from local y = 0 and faces local −Z, toward the camera.
    public static GameObject Build(Vector3 position, float width = 22f)
    {
        var root = new GameObject("Kraft Backdrop");
        root.transform.position = position;

        var ribs = Blank(root.transform, "Kraft Ribs");
        ribs.GetComponent<MeshFilter>().sharedMesh = RibMesh();
        // One gradient cycle per flute. Tile 1 keeps the texture's dark-light-dark from wrapping a second rib.
        PaintKraft(ribs);
        KraftEdge(root.transform, -Width * 0.5f);
        KraftEdge(root.transform, Width * 0.5f);
        KraftLip(root.transform);

        // Garland across the upper wall: small stuffed pillow clouds on the jute cord.
        // Span 1 (-3.4 .. 1.4) and Span 2 (1.4 .. 6.2) meet under the center-right cloud so there is no exposed kink
        // and the title sign below (-0.35, 1.92) sits on clean corrugated kraft flutes.
        FeltCloudCord.Cord(root.transform, new Vector3(CordX0, CordY, CordZ), new Vector3(CordX1, CordY, CordZ), 3);
        FeltCloudCord.Cord(root.transform, new Vector3(CordX1, CordY, CordZ), new Vector3(CordX2, CordY, CordZ), 7);

        const float c1X = -2.20f;
        const float c2X = -0.50f;
        const float c3X = 1.40f;
        const float c4X = 3.25f;
        const float c5X = 4.95f;
        FeltCloudCord.Cloud(root.transform, new Vector3(c1X, SpanHang(CordX0, CordX1, c1X) - 0.10f, CloudZ), 0.76f, 2);
        FeltCloudCord.Cloud(root.transform, new Vector3(c2X, SpanHang(CordX0, CordX1, c2X) - 0.06f, CloudZ), 0.70f, 5);
        FeltCloudCord.Cloud(root.transform, new Vector3(c3X, CordY - 0.10f, CloudZ), 0.78f, 8);
        FeltCloudCord.Cloud(root.transform, new Vector3(c4X, SpanHang(CordX1, CordX2, c4X) - 0.10f, CloudZ), 0.76f, 11);
        FeltCloudCord.Cloud(root.transform, new Vector3(c5X, SpanHang(CordX1, CordX2, c5X) - 0.10f, CloudZ), 0.72f, 14);
        return root;
    }

    static void Cotton(Transform root, float x, float scale, int seed)
    {
        var go = Blank(root, "Cotton Cloud");
        go.transform.localPosition = new Vector3(x, CordHeight(x) - 0.32f * scale, CloudZ);
        go.transform.localRotation = Quaternion.Euler(
            (Hash01(seed, 1) - 0.5f) * 8f,
            (Hash01(seed, 2) - 0.5f) * 14f,
            (Hash01(seed, 3) - 0.5f) * 6f);
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshFilter>().sharedMesh = CottonMesh(seed);
        Paint(go, "Look/tex-felt-cream", CottonMul, CottonFlat, Vector2.one, 1.55f, 0.36f, 0.86f, 0.98f, 0.04f);
    }

    static float CordHeight(float x)
    {
        float u = Mathf.InverseLerp(CordX0, CordX1, x);
        return CordY - CordSag * 4f * u * (1f - u);
    }

    /// Same belly as FeltCloudCord.CordMesh: sag = span * 0.11, deepest at mid-span.
    static float SpanHang(float x0, float x1, float x)
    {
        float len = x1 - x0;
        float u = Mathf.InverseLerp(x0, x1, x);
        return CordY - len * 0.11f * 4f * u * (1f - u);
    }

    /// Front wave and a parallel back liner. Valley at each join, crest in the middle, both facing out.
    static Mesh RibMesh()
    {
        string key = $"KraftSheet_{Width:0.##}_{Ribs}_{Amp:0.###}_{Sheet:0.###}";
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int seg = 12;
        float pitch = Width / Ribs;
        var front = new List<Vector4>(seg + 1);
        var back = new List<Vector4>(seg + 1);
        for (int s = 0; s <= seg; s++)
        {
            float u = s / (float)seg;
            float x = (u - 0.5f) * pitch;
            float ang = u * Mathf.PI * 2f;
            float sn = Mathf.Sin(ang);
            float zf = CrownZ + Amp * 0.5f * (1f + Mathf.Cos(ang));
            // dz/du = -Amp * π * sin. Front faces the camera (−Z). Back is the opposite side of the same wave.
            front.Add(Unit(x, zf, -Amp * Mathf.PI * sn, -pitch));
            back.Add(Unit(x, zf + Sheet, Amp * Mathf.PI * sn, pitch));
        }

        var mesh = new Mesh { name = "Kraft Ribs" };
        var v = new List<Vector3>();
        var nrm = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        AddStrip(v, nrm, uv, tris, front, Ribs);
        AddStrip(v, nrm, uv, tris, back, Ribs);
        Stamp(mesh, v, nrm, uv, tris);
        Meshes[key] = mesh;
        return mesh;
    }

    /// Steep walls from each crown edge down to a crease. This is the dark groove, not a second stripe.
    static Mesh GrooveMesh()
    {
        string key = $"KraftGrooves_{Width:0.##}_{Ribs}_{FluteR:0.###}_{FluteDeg:0}_{GrooveDrop:0.###}";
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int seg = 4;
        float pitch = Width / Ribs;
        Flute(FluteDeg, out float edgeX, out float edgeZ, out float edgeNx, out float edgeNz);
        float creaseX = pitch * 0.5f;
        float creaseZ = edgeZ + GrooveDrop;
        var right = new List<Vector4>(seg + 2) { new Vector4(edgeX, edgeZ, edgeNx, edgeNz) };
        var left = new List<Vector4>(seg + 2);
        float dx = creaseX - edgeX;
        float dz = creaseZ - edgeZ;
        for (int s = 1; s <= seg; s++)
        {
            float t = s / (float)seg;
            float x = Mathf.Lerp(edgeX, creaseX, t);
            float z = Mathf.Lerp(edgeZ, creaseZ, t);
            // Face the camera and into the groove. Honest to the wall slope, not a painted dark line.
            right.Add(Unit(x, z, dz, -dx));
        }
        for (int i = right.Count - 1; i >= 0; i--)
        {
            Vector4 q = right[i];
            left.Add(new Vector4(-q.x, q.y, -q.z, q.w));
        }

        var mesh = new Mesh { name = "Kraft Grooves" };
        // Two strips. One profile would bridge the crown and cover the flute.
        var v = new List<Vector3>();
        var nrm = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        AddStrip(v, nrm, uv, tris, left, Ribs);
        AddStrip(v, nrm, uv, tris, right, Ribs);
        Stamp(mesh, v, nrm, uv, tris);
        Meshes[key] = mesh;
        return mesh;
    }

    static void Flute(float deg, out float x, out float z, out float nx, out float nz)
    {
        float a = deg * Mathf.Deg2Rad;
        float s = Mathf.Sin(a);
        float c = Mathf.Cos(a);
        x = s * FluteR;
        z = CrownZ + FluteR * (1f - c);
        nx = s;
        nz = -c;
    }

    static Vector4 Unit(float x, float z, float nx, float nz)
    {
        float m = Mathf.Sqrt(nx * nx + nz * nz);
        if (m < 1e-6f) return new Vector4(x, z, 0f, -1f);
        return new Vector4(x, z, nx / m, nz / m);
    }

    /// Profile x is rib-local. One span per rib, centred on the backdrop, running vertically.
    static void AddStrip(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> t, List<Vector4> profile, int spans)
    {
        const int ny = 14;
        int pts = profile.Count;
        int cols = spans * pts;
        int start = v.Count;
        float pitch = Width / spans;
        float origin = -Width * 0.5f + pitch * 0.5f;

        for (int iy = 0; iy <= ny; iy++)
        {
            float fy = iy / (float)ny;
            for (int span = 0; span < spans; span++)
            {
                float cx = origin + span * pitch;
                for (int ip = 0; ip < pts; ip++)
                {
                    Vector4 p = profile[ip];
                    float x = cx + p.x;
                    v.Add(new Vector3(x, fy * Height, p.y));
                    nrm.Add(new Vector3(p.z, 0f, p.w));
                    // tex-kraft-flute is dark at 0 and 1, light at 0.5. One cycle is one flute.
                    float shade = Mathf.Clamp01(p.x / (pitch * 0.5f) * 0.5f + 0.5f);
                    uv.Add(new Vector2(shade, 0.42f + fy * 0.16f));
                }
            }
        }

        for (int iy = 0; iy < ny; iy++)
        {
            for (int span = 0; span < spans; span++)
            {
                int row = start + iy * cols + span * pts;
                int next = row + cols;
                for (int ip = 0; ip < pts - 1; ip++)
                    Quad(v, t, row + ip, row + ip + 1, next + ip + 1, next + ip, nrm[row + ip]);
            }
        }
    }

    static void Stamp(Mesh mesh, List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> t)
    {
        mesh.SetVertices(v);
        mesh.SetNormals(nrm);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
    }

    /// One plain round tube on a single sag. No helix, so it stays a fine cord.
    static Mesh CordMesh()
    {
        string key = $"JuteCord_{CordX0:0.##}_{CordX1:0.##}_{CordY:0.##}_{CordSag:0.##}_{CordRadius:0.###}";
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int along = 120;
        const int around = 8;
        int cols = around + 1;
        float span = CordX1 - CordX0;
        var v = new List<Vector3>((along + 1) * cols);
        var uv = new List<Vector2>(v.Capacity);
        var t = new List<int>(along * around * 6);

        for (int i = 0; i <= along; i++)
        {
            float u = i / (float)along;
            float x = Mathf.Lerp(CordX0, CordX1, u);
            var c = new Vector3(x, CordHeight(x), CordZ);
            float dydx = -CordSag * 4f * (1f - 2f * u) / span;
            Vector3 tangent = new Vector3(1f, dydx, 0f).normalized;
            Vector3 bin = Vector3.Cross(tangent, Vector3.back);
            if (bin.sqrMagnitude < 1e-8f) bin = Vector3.up;
            bin.Normalize();
            Vector3 cam = Vector3.Cross(bin, tangent).normalized;
            for (int j = 0; j <= around; j++)
            {
                float a = j / (float)around * Mathf.PI * 2f;
                v.Add(c + (cam * Mathf.Cos(a) + bin * Mathf.Sin(a)) * CordRadius);
                uv.Add(new Vector2(j / (float)around, u * span * 6f));
            }
        }

        for (int i = 0; i < along; i++)
        {
            for (int j = 0; j < around; j++)
            {
                int a = i * cols + j;
                Quad(v, t, a, a + 1, a + cols + 1, a + cols, v[a] - new Vector3(Mathf.Lerp(CordX0, CordX1, i / (float)along), CordHeight(Mathf.Lerp(CordX0, CordX1, i / (float)along)), CordZ));
            }
        }

        var mesh = new Mesh { name = "Jute Cord" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        Finish(mesh);
        var nrm = mesh.normals;
        for (int i = 0; i <= along; i++)
        {
            int a = i * cols;
            int b = a + around;
            Vector3 avg = (nrm[a] + nrm[b]).normalized;
            nrm[a] = avg;
            nrm[b] = avg;
        }
        mesh.normals = nrm;
        mesh.RecalculateTangents();
        Meshes[key] = mesh;
        return mesh;
    }

    /// One iso-surface of overlapping puffs, then a soft radial wobble. Not separate spheres.
    static Mesh CottonMesh(int seed)
    {
        string key = "Cotton_" + seed;
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int su = 24;
        const int sv = 12;
        int cols = su + 1;
        int rings = sv - 1;
        var v = new List<Vector3>((rings + 2) * cols);
        var uv = new List<Vector2>(v.Capacity);
        var t = new List<int>(su * sv * 6);
        float ou = (Hash01(seed, 7) - 0.5f) * 0.12f;
        float ov = (Hash01(seed, 8) - 0.5f) * 0.12f;

        int Add(Vector3 dir)
        {
            dir.Normalize();
            Vector3 p = dir * Radius(dir, seed);
            v.Add(p);
            uv.Add(new Vector2(0.5f + ou + p.x * 0.26f, 0.5f + ov + p.y * 0.30f));
            return v.Count - 1;
        }

        int top = Add(Vector3.up);
        int ring0 = v.Count;
        for (int iv = 1; iv < sv; iv++)
        {
            float theta = iv / (float)sv * Mathf.PI;
            float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
            for (int iu = 0; iu <= su; iu++)
            {
                float phi = (iu % su) / (float)su * Mathf.PI * 2f;
                Add(new Vector3(Mathf.Cos(phi) * st, ct, Mathf.Sin(phi) * st));
            }
        }
        int bot = Add(Vector3.down);

        for (int iu = 0; iu < su; iu++)
            Tri(v, t, top, ring0 + iu, ring0 + iu + 1, Vector3.up);
        for (int iv = 0; iv < rings - 1; iv++)
        {
            int rowA = ring0 + iv * cols;
            int rowB = rowA + cols;
            for (int iu = 0; iu < su; iu++)
                Quad(v, t, rowA + iu, rowA + iu + 1, rowB + iu + 1, rowB + iu, v[rowA + iu]);
        }
        int last = ring0 + (rings - 1) * cols;
        for (int iu = 0; iu < su; iu++)
            Tri(v, t, bot, last + iu + 1, last + iu, Vector3.down);

        var mesh = new Mesh { name = "Cotton Cloud" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        Finish(mesh);
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
        mesh.RecalculateTangents();
        Meshes[key] = mesh;
        return mesh;
    }

    static float Radius(Vector3 dir, int seed)
    {
        const float threshold = 0.40f;
        float lo = 0.04f, hi = 1.25f;
        if (Field(dir * lo, seed) < threshold) return 0.28f;
        for (int i = 0; i < 14; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (Field(dir * mid, seed) > threshold) lo = mid;
            else hi = mid;
        }
        float n1 = Mathf.PerlinNoise(dir.x * 2.1f + seed * 0.17f, dir.y * 2.1f + 0.4f);
        float n2 = Mathf.PerlinNoise(dir.z * 3.4f + seed * 0.11f, dir.x * 1.6f + 1.7f);
        float wobble = (n1 - 0.5f) * 0.20f + (n2 - 0.5f) * 0.08f;
        return lo * (1f + wobble);
    }

    static float Field(Vector3 p, int seed)
    {
        float f = 0f;
        for (int i = 0; i < LobeAt.Length; i++)
        {
            Vector3 q = p - LobeCenter(i, seed);
            Vector3 rad = LobeRad[i];
            q = new Vector3(q.x / rad.x, q.y / rad.y, q.z / rad.z);
            f += Mathf.Exp(-Vector3.Dot(q, q) * 1.15f);
        }
        return f;
    }

    static Vector3 LobeCenter(int i, int seed)
    {
        Vector3 c = LobeAt[i];
        const float j = 0.05f;
        return new Vector3(
            c.x + (Hash01(seed, 10 + i) - 0.5f) * j,
            c.y + (Hash01(seed, 30 + i) - 0.5f) * j * 0.6f,
            c.z + (Hash01(seed, 50 + i) - 0.5f) * j * 0.5f);
    }

    static void Finish(Mesh mesh)
    {
        mesh.RecalculateNormals();
        var nrm = mesh.normals;
        var verts = mesh.vertices;
        for (int i = 0; i < nrm.Length; i++)
        {
            if (nrm[i].sqrMagnitude > 1e-8f) continue;
            nrm[i] = verts[i].sqrMagnitude > 1e-8f ? verts[i].normalized : Vector3.back;
        }
        mesh.normals = nrm;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
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

    static void PaintKraft(GameObject go)
    {
        // Paper, not wool. The flute texture carries the crest-to-groove ramp.
        Paint(go, "Look/tex-kraft-flute", KraftMul, KraftFlat, Vector2.one, 0.18f, 0.02f, 0.62f, 0.96f, 0.06f);
    }

    static void KraftEdge(Transform root, float x)
    {
        var go = Blank(root, "Kraft Edge");
        // The side join is a valley. The cut runs from that valley back through the liner.
        go.transform.localPosition = new Vector3(x, 0f, CrownZ + Amp);
        if (x < 0f) go.transform.localScale = new Vector3(-1f, 1f, 1f);
        go.GetComponent<MeshFilter>().sharedMesh = EdgeMesh();
        PaintKraft(go);
    }

    static void KraftLip(Transform root)
    {
        var go = Blank(root, "Kraft Lip");
        go.GetComponent<MeshFilter>().sharedMesh = LipMesh();
        PaintKraft(go);
    }

    /// Cut end of the sheet. Two light liners, a darker core, and a wave at the flute pitch.
    /// Local +X is outward before the left edge is mirrored.
    static Mesh EdgeMesh()
    {
        string key = $"KraftEdge_{Height:0.##}_{Sheet:0.###}_{Ribs}";
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;
        const int rows = 180;
        const int cols = 5;
        const float waveAmp = 0.016f;
        float pitch = Width / Ribs;
        float waves = Height / pitch;
        var v = new List<Vector3>((rows + 1) * (cols + 1));
        var nrm = new List<Vector3>(v.Capacity);
        var uv = new List<Vector2>(v.Capacity);
        var tris = new List<int>(rows * cols * 6);
        for (int y = 0; y <= rows; y++)
        {
            float fy = y / (float)rows;
            float phase = fy * waves * Mathf.PI * 2f;
            float wave = Mathf.Sin(phase);
            float slope = Mathf.Cos(phase) * waveAmp * waves * Mathf.PI * 2f / Height;
            var n = new Vector3(1f, -slope, 0f).normalized;
            for (int x = 0; x <= cols; x++)
            {
                float fx = x / (float)cols;
                float core = Mathf.Sin(fx * Mathf.PI);
                v.Add(new Vector3(wave * waveAmp - core * 0.006f, fy * Height, fx * Sheet));
                nrm.Add(n);
                // 0.5 is the light crest of tex-kraft-flute, 0 is the groove. Liners light, core dark.
                uv.Add(new Vector2(Mathf.Abs(fx - 0.5f), fy * 6f));
            }
        }
        int stride = cols + 1;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i0 = y * stride + x, i1 = i0 + 1, i2 = i0 + stride, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }
        var mesh = new Mesh { name = "Kraft Edge" };
        Stamp(mesh, v, nrm, uv, tris);
        Meshes[key] = mesh;
        return mesh;
    }

    /// Top and bottom cuts. The lip follows the front wave so the sheet top is scalloped.
    static Mesh LipMesh()
    {
        string key = $"KraftLip_{Width:0.##}_{Ribs}_{Amp:0.###}_{Sheet:0.###}";
        if (Meshes.TryGetValue(key, out var cached) && cached != null) return cached;

        const int seg = 12;
        float pitch = Width / Ribs;
        float origin = -Width * 0.5f + pitch * 0.5f;
        var v = new List<Vector3>();
        var nrm = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();

        void Band(float y, Vector3 outward)
        {
            int start = v.Count;
            for (int span = 0; span < Ribs; span++)
            {
                float cx = origin + span * pitch;
                for (int s = 0; s <= seg; s++)
                {
                    float u = s / (float)seg;
                    float x = cx + (u - 0.5f) * pitch;
                    float zf = CrownZ + Amp * 0.5f * (1f + Mathf.Cos(u * Mathf.PI * 2f));
                    v.Add(new Vector3(x, y, zf));
                    v.Add(new Vector3(x, y, zf + Sheet));
                    nrm.Add(outward);
                    nrm.Add(outward);
                    uv.Add(new Vector2(u, 0.08f));
                    uv.Add(new Vector2(u, 0.92f));
                }
            }
            int pair = seg + 1;
            for (int span = 0; span < Ribs; span++)
            {
                int row = start + span * pair * 2;
                for (int s = 0; s < seg; s++)
                {
                    int a = row + s * 2;
                    Quad(v, tris, a, a + 2, a + 3, a + 1, outward);
                }
            }
        }

        Band(Height, Vector3.up);
        Band(0f, Vector3.down);

        var mesh = new Mesh { name = "Kraft Lip" };
        Stamp(mesh, v, nrm, uv, tris);
        Meshes[key] = mesh;
        return mesh;
    }

    static GameObject Blank(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>();
        var rend = go.AddComponent<MeshRenderer>();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    /// Escape/FeltFiber. Resource is a Resources path such as Look/tex-kraft-corrugated.
    static void Paint(GameObject go, string resource, Color mul, Color flat, Vector2 tile, float bump, float sheen, float wrap, float rough, float fiber)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        string key = "FeltWall|" + CraftMesh.Key(resource, mul, tile, bump, sheen, wrap, rough, 0f) + "|f" + fiber.ToString("0.###");
        rend.sharedMaterial = CraftMesh.Shared(key, () =>
        {
            var tex = Resources.Load<Texture2D>(resource);
            var tint = tex != null ? mul : flat;
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough);
            if (mat != null && mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
            return mat;
        });
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

using System.Collections.Generic;
using UnityEngine;

/// rotate-red-tree.mp4: a thick knitted brown trunk, flared at the foot, forking into
/// short limbs. The canopy is three bunches of large bright-red yarn balls that touch:
/// a crown on the leader, and a cluster to each side. Balls stay round. The yarn
/// texture carries the knit; the trunk ribs are only a shallow tooth.
/// Scale 1 is about 1.54 tall, foot on y = 0. Camera looks along +Z, so -Z is the camera face.
public static class FeltRedTree
{
    // x, y, z, radius. Packed so neighbours overlap by a few millimetres and still read as balls.
    // Crown, then the left cluster, the right cluster, and one ball on the camera side of the fork.
    static readonly Vector4[] Balls =
    {
        new Vector4(0.00f, 1.40f, 0.00f, 0.135f),
        new Vector4(-0.18f, 1.26f, 0.02f, 0.128f),
        new Vector4(0.16f, 1.25f, 0.05f, 0.126f),
        new Vector4(0.00f, 1.23f, 0.19f, 0.120f),
        new Vector4(-0.02f, 1.24f, -0.18f, 0.126f),
        new Vector4(-0.26f, 1.08f, 0.06f, 0.124f),
        new Vector4(0.26f, 1.08f, -0.04f, 0.122f),
        new Vector4(0.08f, 1.06f, 0.20f, 0.118f),
        new Vector4(-0.10f, 1.04f, -0.20f, 0.120f),
        new Vector4(-0.28f, 1.12f, -0.12f, 0.112f),
        new Vector4(0.00f, 1.18f, 0.00f, 0.108f),

        new Vector4(-0.30f, 0.78f, -0.03f, 0.118f),
        new Vector4(-0.45f, 0.89f, -0.09f, 0.110f),
        new Vector4(-0.44f, 0.66f, 0.04f, 0.108f),
        new Vector4(-0.34f, 0.92f, 0.06f, 0.108f),
        new Vector4(-0.16f, 0.66f, -0.08f, 0.106f),
        new Vector4(-0.38f, 0.70f, -0.16f, 0.100f),
        new Vector4(-0.14f, 0.92f, 0.02f, 0.106f),

        new Vector4(0.28f, 0.86f, 0.05f, 0.120f),
        new Vector4(0.44f, 0.96f, 0.08f, 0.112f),
        new Vector4(0.42f, 0.74f, 0.00f, 0.110f),
        new Vector4(0.12f, 0.98f, 0.00f, 0.100f),
        new Vector4(0.18f, 0.72f, 0.16f, 0.104f),
        new Vector4(0.42f, 0.80f, 0.16f, 0.102f),
        new Vector4(0.18f, 0.78f, -0.10f, 0.104f),

        new Vector4(0.04f, 0.92f, -0.12f, 0.100f)
    };

    static Material _trunk, _red, _redDeep;
    static Mesh _column, _armL, _armR, _armBack;

    /// Root is named "Red Tree". Position is world space. Scale 1 is about 1.54 tall, foot on y = 0.
    public static GameObject Build(Vector3 position, float scale = 1f)
    {
        var root = new GameObject("Red Tree");
        root.transform.position = position;
        root.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        root.isStatic = true;

        Transform host = root.transform;
        EnsureLimbs();
        YarnLimb(host, "Red Trunk", _column);
        YarnLimb(host, "Red Limb", _armL);
        YarnLimb(host, "Red Limb", _armR);
        YarnLimb(host, "Red Limb", _armBack);
        Canopy(host);
        return root;
    }

    static void Canopy(Transform host)
    {
        for (int i = 0; i < Balls.Length; i++)
        {
            Vector4 b = Balls[i];
            Pom(host, new Vector3(b.x, b.y, b.z), b.w, i);
        }
    }

    static void Pom(Transform host, Vector3 localPos, float radius, int index)
    {
        var go = new GameObject("Red Pompom");
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        int seed = 4 + (Mathf.Abs(index) % 7);
        go.transform.localRotation = Quaternion.Euler(seed * 19f, index * 47f, seed * 11f);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = CraftMesh.Pompom(radius, 0.10f, 16, 12, seed);
        var renderer = go.AddComponent<MeshRenderer>();
        Material mat = (index % 4 == 3) ? RedDeep() : RedWool();
        if (mat != null) renderer.sharedMaterial = mat;
    }

    static void YarnLimb(Transform host, string name, Mesh mesh)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        if (TrunkYarn() != null) renderer.sharedMaterial = TrunkYarn();
    }

    /// Thick bole into the crown, one limb into each side cluster, one short limb into the back of the crown.
    static void EnsureLimbs()
    {
        if (_column != null) return;
        _column = RibbedCurve("Red Trunk Ribs",
            new Vector3(0f, 0f, 0f), new Vector3(0.012f, 0.46f, 0.004f), new Vector3(0.02f, 1.08f, 0f),
            0.168f, 0.038f, 24, true);
        _armL = RibbedCurve("Red Limb Left",
            new Vector3(-0.05f, 0.58f, 0.00f), new Vector3(-0.16f, 0.68f, -0.02f), new Vector3(-0.28f, 0.78f, -0.03f),
            0.072f, 0.040f, 9, false);
        _armR = RibbedCurve("Red Limb Right",
            new Vector3(0.05f, 0.64f, 0.01f), new Vector3(0.16f, 0.76f, 0.03f), new Vector3(0.26f, 0.86f, 0.05f),
            0.070f, 0.038f, 9, false);
        _armBack = RibbedCurve("Red Limb Back",
            new Vector3(0.02f, 0.70f, 0.04f), new Vector3(0.05f, 0.88f, 0.12f), new Vector3(0.07f, 1.02f, 0.17f),
            0.050f, 0.028f, 8, false);
    }

    static Vector3 Curve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    static Vector3 CurveTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u0 = Mathf.Max(0f, t - 0.012f);
        float u1 = Mathf.Min(1f, t + 0.012f);
        Vector3 d = Curve(p0, p1, p2, u1) - Curve(p0, p1, p2, u0);
        if (d.sqrMagnitude < 1e-8f) d = Vector3.up;
        return d.normalized;
    }

    static float Fade(float a, float b, float u)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, u));
    }

    /// Foot radius, a narrower neck, a thicker fork, then a thin leader inside the crown.
    static float TrunkRadius(float u, float footR, float tipR)
    {
        float neck = footR * 0.60f;
        float fork = footR * 0.67f;
        float foot = Mathf.Lerp(footR, neck, Fade(0f, 0.22f, u));
        float body = Mathf.Lerp(foot, fork, Fade(0.28f, 0.64f, u));
        return Mathf.Lerp(body, tipR, Fade(0.74f, 1f, u));
    }

    /// Yarn column along a curve. Grooves are shallow so the knit texture stays the stitch, not a flute.
    static Mesh RibbedCurve(string meshName, Vector3 p0, Vector3 p1, Vector3 p2, float radius0, float radius1, int rows, bool flared)
    {
        const int around = 40;
        const int cords = 8;
        float amp = flared ? 0.055f : 0.06f;
        float uRepeat = flared ? 2.3f : 1.7f;
        float vRepeat = flared ? 4.6f : 1.5f;
        int cols = around + 1;
        var centers = new Vector3[rows];
        var side = new Vector3[rows];
        var bin = new Vector3[rows];
        Vector3 n = Vector3.Cross(CurveTangent(p0, p1, p2, 0f), Vector3.forward);
        if (n.sqrMagnitude < 1e-6f) n = Vector3.Cross(CurveTangent(p0, p1, p2, 0f), Vector3.right);
        n.Normalize();
        for (int row = 0; row < rows; row++)
        {
            float u = row / (float)(rows - 1);
            Vector3 t = CurveTangent(p0, p1, p2, u);
            n = n - t * Vector3.Dot(n, t);
            if (n.sqrMagnitude < 1e-6f) n = Vector3.Cross(t, Vector3.forward);
            n.Normalize();
            centers[row] = Curve(p0, p1, p2, u);
            side[row] = n;
            bin[row] = Vector3.Cross(t, n);
        }

        Vector3 On(int row, float phi)
        {
            float u = row / (float)(rows - 1);
            float radius = flared ? TrunkRadius(u, radius0, radius1) : Mathf.Lerp(radius0, radius1, u);
            radius *= 1f + amp * Mathf.Cos(phi * cords);
            return centers[row] + (side[row] * Mathf.Cos(phi) + bin[row] * Mathf.Sin(phi)) * radius;
        }

        var v = new List<Vector3>(rows * cols + 2);
        var nrm = new List<Vector3>(rows * cols + 2);
        var uv = new List<Vector2>(rows * cols + 2);
        var tris = new List<int>((rows - 1) * around * 6 + around * 6);
        for (int row = 0; row < rows; row++)
        {
            float u = row / (float)(rows - 1);
            for (int iu = 0; iu <= around; iu++)
            {
                float phi = iu / (float)around * Mathf.PI * 2f;
                Vector3 p = On(row, phi);
                Vector3 dp = On(row, phi + 0.04f) - p;
                Vector3 du = row < rows - 1 ? On(row + 1, phi) - p : p - On(row - 1, phi);
                Vector3 normal = Vector3.Cross(dp, du);
                if (normal.sqrMagnitude < 1e-8f) normal = (p - centers[row]);
                if (normal.sqrMagnitude < 1e-8f) normal = Vector3.up;
                v.Add(p);
                nrm.Add(normal.normalized);
                uv.Add(new Vector2(iu / (float)around * uRepeat, u * vRepeat));
            }
        }
        for (int row = 0; row < rows; row++)
        {
            int a = row * cols;
            int b = a + cols - 1;
            Vector3 avg = nrm[a] + nrm[b];
            if (avg.sqrMagnitude < 1e-8f) avg = nrm[a];
            avg.Normalize();
            nrm[a] = avg;
            nrm[b] = avg;
        }
        for (int row = 0; row < rows - 1; row++)
        {
            int rowA = row * cols;
            int rowB = rowA + cols;
            for (int iu = 0; iu < around; iu++)
            {
                int i0 = rowA + iu, i1 = i0 + 1, i2 = rowB + iu, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }

        int bottom = v.Count;
        v.Add(centers[0]);
        nrm.Add(-CurveTangent(p0, p1, p2, 0f));
        uv.Add(new Vector2(0.5f, 0f));
        int top = v.Count;
        v.Add(centers[rows - 1]);
        nrm.Add(CurveTangent(p0, p1, p2, 1f));
        uv.Add(new Vector2(0.5f, vRepeat));
        Fan(v, tris, bottom, 0, around, -CurveTangent(p0, p1, p2, 0f));
        Fan(v, tris, top, (rows - 1) * cols, around, CurveTangent(p0, p1, p2, 1f));

        var mesh = new Mesh { name = meshName };
        mesh.SetVertices(v);
        mesh.SetNormals(nrm);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static void Fan(List<Vector3> v, List<int> tris, int center, int ring, int seg, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[ring] - v[center], v[ring + 1] - v[center]);
        bool wind = Vector3.Dot(n, outward) >= 0f;
        for (int i = 0; i < seg; i++)
        {
            int a = ring + i, b = a + 1;
            if (wind) { tris.Add(center); tris.Add(a); tris.Add(b); }
            else { tris.Add(center); tris.Add(b); tris.Add(a); }
        }
    }

    /// tex-yarn-fiber mean is about (147, 115, 81). This multiplier lands near (109, 58, 29), the lit knit.
    static Material TrunkYarn()
    {
        if (_trunk != null) return _trunk;
        _trunk = Felt("Red Trunk Yarn", "Look/tex-yarn-fiber", new Color(0.74f, 0.50f, 0.36f), new Color(0.42f, 0.24f, 0.14f),
            Vector2.one, 1.6f, 0.08f, 0.70f, 0.04f, 0.18f);
        return _trunk;
    }

    /// tex-felt-red mean is about (116, 20, 26). This multiplier lands near (211, 45, 37), a warm bright red.
    static Material RedWool()
    {
        if (_red != null) return _red;
        _red = Felt("Red Bloom", "Look/tex-felt-red", new Color(1.82f, 2.25f, 1.42f), new Color(0.82f, 0.18f, 0.14f),
            new Vector2(2.0f, 2.0f), 2.3f, 0.16f, 0.84f, 0.035f, 0.22f);
        return _red;
    }

    /// Same plate, a step deeper, still red. Used on every fourth ball so the bunch is not one flat color.
    static Material RedDeep()
    {
        if (_redDeep != null) return _redDeep;
        _redDeep = Felt("Red Bloom Deep", "Look/tex-felt-red", new Color(1.55f, 1.80f, 1.12f), new Color(0.70f, 0.14f, 0.11f),
            new Vector2(2.0f, 2.0f), 2.1f, 0.14f, 0.82f, 0.035f, 0.20f);
        return _redDeep;
    }

    /// Escape/FeltFiber when the shader and the Look plate both load. Otherwise a flat fallback color.
    static Material Felt(string matName, string resource, Color tint, Color fallback, Vector2 tile, float bump, float sheen, float wrap, float smooth, float fiber)
    {
        var shader = Shader.Find("Escape/FeltFiber");
        var tex = Resources.Load<Texture2D>(resource);
        if (shader == null || tex == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            var plain = new Material(shader) { name = matName };
            plain.enableInstancing = true;
            if (plain.HasProperty("_BaseColor")) plain.SetColor("_BaseColor", fallback);
            if (plain.HasProperty("_Color")) plain.color = fallback;
            if (plain.HasProperty("_Smoothness")) plain.SetFloat("_Smoothness", 0.08f);
            if (plain.HasProperty("_Glossiness")) plain.SetFloat("_Glossiness", 0.08f);
            return plain;
        }

        var mat = new Material(shader) { name = matName };
        mat.enableInstancing = true;
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tile);
        }
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", tex);
            mat.SetTextureScale("_MainTex", tile);
        }
        var fuzz = Resources.Load<Texture2D>("Look/tex-felt-normal");
        if (fuzz != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", fuzz);
            mat.SetTextureScale("_BumpMap", tile);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.color = tint;
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bump);
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", sheen);
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", wrap);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
        if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", 0f);
        return mat;
    }
}

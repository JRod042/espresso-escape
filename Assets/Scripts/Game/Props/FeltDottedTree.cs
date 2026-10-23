using System.Collections.Generic;
using UnityEngine;

/// rotate-dotted-tree.mp4: a fuzzy yellow-green felt ball on a thick stem.
/// The stem is narrow where it meets the dome and wider at the foot.
/// The dome is covered in small cream yarn coils, spirals facing outward.
public static class FeltDottedTree
{
    static readonly Color Green = new Color(1.26f, 1.38f, 1.18f);
    static readonly Color Cream = new Color(1.06f, 1.02f, 0.94f);

    const float DomeR = 0.42f;
    static readonly Vector3 DomeScale = new Vector3(1.03f, 0.99f, 1.03f);
    const float FootR = 0.235f;
    const float NeckR = 0.158f;
    const float StemTop = 0.60f;
    const float NeckU = 0.74f;

    const int Coils = 96;
    const float CoilRadius = 0.050f;

    static Material _green, _cream;
    static Mesh _stem, _coil;
    static float _coilSeat;

    /// Scale 1 is about 1.25 tall, foot on y = 0. Root is named Dotted Tree.
    public static GameObject Build(Vector3 position, float scale = 1f)
    {
        var tree = new GameObject("Dotted Tree");
        tree.transform.position = position;
        tree.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        tree.isStatic = true;
        Transform host = tree.transform;

        float rx = DomeR * DomeScale.x;
        float ry = DomeR * DomeScale.y;
        float rz = DomeR * DomeScale.z;
        float neckY = StemTop * NeckU;
        float drop = ry * Mathf.Sqrt(Mathf.Max(0.04f, 1f - (NeckR * NeckR) / (rx * rx)));
        float centerY = neckY + drop;
        var center = new Vector3(0f, centerY, 0f);

        var stem = Body(host, "Dotted Trunk", Vector3.zero, Quaternion.identity, Vector3.one, StemMesh(), GreenMat());
        NoShadow(stem);

        var dome = Body(host, "Dotted Canopy", center, Quaternion.identity, DomeScale,
            CraftMesh.Pompom(DomeR, 0.02f, 32, 22, 4), GreenMat());
        NoShadow(dome);

        Mesh coil = CoilMesh();
        float golden = 2.399963f;
        for (int i = 0; i < Coils; i++)
        {
            float t = (i + 0.5f) / Coils;
            float y = Mathf.Lerp(0.97f, -0.46f, t);
            float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float phi = i * golden;
            var dir = new Vector3(Mathf.Cos(phi) * ring, y, Mathf.Sin(phi) * ring).normalized;
            var surface = new Vector3(dir.x * rx, dir.y * ry, dir.z * rz);
            var normal = new Vector3(dir.x / rx, dir.y / ry, dir.z / rz).normalized;
            float vary = 0.94f + (i % 4) * 0.035f;
            float radius = CoilRadius * vary;
            var pos = center + surface + normal * (_coilSeat * radius - 0.01f);
            var dot = Body(host, "Canopy Coil", pos, FaceOut(normal, i * 53f), Vector3.one * radius, coil, CreamMat());
            NoShadow(dot);
        }

        return tree;
    }

    static Quaternion FaceOut(Vector3 normal, float twist)
    {
        Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, n);
        if (up.sqrMagnitude < 1e-6f) up = Vector3.ProjectOnPlane(Vector3.forward, n);
        return Quaternion.AngleAxis(twist, n) * Quaternion.LookRotation(n, up.normalized);
    }

    static float StemRadius(float u)
    {
        float r;
        if (u <= NeckU)
        {
            float t = Mathf.SmoothStep(0f, 1f, u / NeckU);
            r = Mathf.Lerp(FootR, NeckR, t);
            float fade = Mathf.Clamp01((NeckU - u) / 0.45f);
            r *= 1f - 0.02f * fade * (0.5f + 0.5f * Mathf.Sin(u * 16f));
        }
        else
        {
            float t = Mathf.SmoothStep(0f, 1f, (u - NeckU) / (1f - NeckU));
            r = Mathf.Lerp(NeckR, NeckR * 1.9f, t);
        }
        return r;
    }

    static Mesh StemMesh()
    {
        if (_stem != null) return _stem;
        const int seg = 22;
        var us = new List<float>(14);
        for (int k = 0; k <= 8; k++) us.Add(NeckU * k / 8f);
        for (int k = 1; k <= 4; k++) us.Add(Mathf.Lerp(NeckU, 1f, k / 4f));
        int samples = us.Count;
        int cols = seg + 1;
        var v = new List<Vector3>(samples * cols + 1);
        var uv = new List<Vector2>(v.Capacity);
        var tris = new List<int>(seg * (samples - 1) * 6 + seg * 3);
        for (int k = 0; k < samples; k++)
        {
            float u = us[k];
            float y = u * StemTop;
            float r = StemRadius(u);
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                uv.Add(new Vector2(i / (float)seg * 1.2f, u * 1.1f));
            }
        }
        for (int k = 0; k < samples - 1; k++)
            for (int i = 0; i < seg; i++)
            {
                int i0 = k * cols + i, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        int hub = v.Count;
        v.Add(Vector3.zero);
        uv.Add(new Vector2(0.5f, 0f));
        for (int i = 0; i < seg; i++)
        {
            tris.Add(hub);
            tris.Add(i + 1);
            tris.Add(i);
        }
        _stem = Finish("Dotted Stem", v, uv, tris);
        return _stem;
    }

    /// Yarn spiral in the XY plane, face along +Z. The disc is about radius 1.
    static Mesh CoilMesh()
    {
        if (_coil != null) return _coil;
        const int steps = 68;
        const int seg = 8;
        const float turns = 2.35f;
        const float strandOut = 0.27f;
        const float strandIn = 0.145f;
        const float r0 = 0.095f;
        const float r1 = 0.80f;
        float unit = r1 + strandIn;
        int cols = seg + 1;
        var center = new Vector3[steps];
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            float ang = t * turns * Mathf.PI * 2f;
            float rad = Mathf.Lerp(r0, r1, t);
            float dome = 0.20f * (1f - rad * rad);
            center[i] = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad, dome) / unit;
        }
        var v = new List<Vector3>(steps * cols + 1);
        var uv = new List<Vector2>(v.Capacity);
        var tris = new List<int>((steps - 1) * seg * 6 + seg * 3);
        for (int i = 0; i < steps; i++)
        {
            Vector3 prev = center[Mathf.Max(0, i - 1)];
            Vector3 next = center[Mathf.Min(steps - 1, i + 1)];
            Vector3 tangent = next - prev;
            if (tangent.sqrMagnitude < 1e-10f) tangent = Vector3.right;
            tangent.Normalize();
            Vector3 side = Vector3.Cross(tangent, Vector3.forward);
            if (side.sqrMagnitude < 1e-8f) side = Vector3.Cross(tangent, Vector3.up);
            side.Normalize();
            Vector3 outward = Vector3.Cross(side, tangent).normalized;
            float t = i / (float)(steps - 1);
            float taper = t > 0.9f ? Mathf.Lerp(1f, 0.2f, (t - 0.9f) / 0.1f) : 1f;
            float oR = strandOut * taper / unit;
            float iR = strandIn * taper / unit;
            for (int s = 0; s <= seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                v.Add(center[i] + outward * (Mathf.Cos(a) * oR) + side * (Mathf.Sin(a) * iR));
                uv.Add(new Vector2(t * turns * 2f, s / (float)seg));
            }
        }
        for (int i = 0; i < steps - 1; i++)
            for (int s = 0; s < seg; s++)
            {
                int a = i * cols + s;
                int b = a + 1;
                int c = a + cols;
                int d = c + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
        int hub = v.Count;
        v.Add(center[0]);
        uv.Add(Vector2.zero);
        for (int s = 0; s < seg; s++)
        {
            tris.Add(hub);
            tris.Add(s);
            tris.Add(s + 1);
        }
        _coil = Finish("Yarn Coil", v, uv, tris);
        _coilSeat = -_coil.bounds.min.z;
        return _coil;
    }

    static Mesh Finish(string name, List<Vector3> v, List<Vector2> uv, List<int> tris)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void NoShadow(Transform part)
    {
        var renderer = part.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Transform Body(Transform host, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        return go.transform;
    }

    static Material GreenMat()
    {
        if (_green != null) return _green;
        _green = Felt("Dotted Green", Green, "Look/tex-yarn-green", new Vector2(3.2f, 3.2f), 2.1f, 0.30f);
        return _green;
    }

    static Material CreamMat()
    {
        if (_cream != null) return _cream;
        _cream = Felt("Dotted Cream", Cream, "Look/tex-felt-cream", new Vector2(1.4f, 1.4f), 1.35f, 0.22f);
        return _cream;
    }

    static Material Felt(string name, Color color, string resource, Vector2 tile, float bump, float fiber)
    {
        var shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) return null;
        var mat = new Material(shader) { name = name };
        if (!string.IsNullOrEmpty(resource))
        {
            var tex = Resources.Load<Texture2D>(resource);
            if (tex != null && mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tile);
            }
        }
        var fuzz = Resources.Load<Texture2D>("Look/tex-felt-normal");
        if (fuzz != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", fuzz);
            mat.SetTextureScale("_BumpMap", tile);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bump);
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", 0.07f);
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", 0.84f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.025f);
        if (mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
        if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", 0f);
        return mat;
    }
}

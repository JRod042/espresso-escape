using System.Collections.Generic;
using UnityEngine;

/// rotate-dotted-tree.mp4 & crop-dotted-tree.jpg: a tall moss-green needle-felted dome
/// studded with plump 3D cream/tan/olive wound-yarn knots on a ribbed tan-brown knitted trunk.
public static class FeltDottedTree
{
    static readonly Color Green = new Color(0.76f, 0.84f, 0.56f);
    static readonly Color TrunkTan = new Color(0.86f, 0.75f, 0.60f);
    static readonly Color DotCream = new Color(0.96f, 0.89f, 0.76f);
    static readonly Color DotTan = new Color(0.84f, 0.73f, 0.58f);
    static readonly Color DotSage = new Color(0.88f, 0.92f, 0.66f);

    const float DomeR = 0.42f;
    static readonly Vector3 DomeScale = new Vector3(0.96f, 1.12f, 0.96f);
    const float FootR = 0.172f;
    const float NeckR = 0.122f;
    const float StemTop = 0.56f;
    const float NeckU = 0.76f;

    const int Coils = 42;
    const float CoilRadius = 0.068f;

    static Material _green, _trunk, _dotCream, _dotTan, _dotSage;
    static Mesh _stem;
    static readonly Mesh[] DotMeshes = new Mesh[4];

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
        float drop = ry * Mathf.Sqrt(Mathf.Max(0.04f, 1f - (NeckR * NeckR) / (rx * rx))) * 0.78f;
        float centerY = neckY + drop;
        var center = new Vector3(0f, centerY, 0f);

        var stem = Body(host, "Dotted Trunk", Vector3.zero, Quaternion.identity, Vector3.one, StemMesh(), TrunkMat());
        NoShadow(stem);

        var dome = Body(host, "Dotted Canopy", center, Quaternion.identity, DomeScale,
            CraftMesh.Pompom(DomeR, 0.05f, 32, 22, 4), GreenMat());
        NoShadow(dome);

        float golden = 2.399963f;
        for (int i = 0; i < Coils; i++)
        {
            float t = (i + 0.5f) / Coils;
            float y = Mathf.Lerp(0.96f, -0.44f, t);
            float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float phi = i * golden;
            var dir = new Vector3(Mathf.Cos(phi) * ring, y, Mathf.Sin(phi) * ring).normalized;
            var surface = new Vector3(dir.x * rx, dir.y * ry, dir.z * rz);
            var normal = new Vector3(dir.x / rx, dir.y / ry, dir.z / rz).normalized;
            float vary = 0.92f + (i % 4) * 0.055f;
            float radius = CoilRadius * vary;
            var pos = center + surface + normal * (radius * 0.34f);
            var dotScale = new Vector3(radius * 1.06f, radius * 1.06f, radius * 0.82f);
            Material dotMat = (i % 5 == 0) ? DotSageMat() : ((i % 3 == 0) ? DotTanMat() : DotCreamMat());
            var dot = Body(host, "Canopy Coil", pos, FaceOut(normal, i * 53f), dotScale, DotMesh(i % 4), dotMat);
            NoShadow(dot);
        }

        return tree;
    }

    static Mesh DotMesh(int variant)
    {
        int idx = Mathf.Clamp(variant, 0, DotMeshes.Length - 1);
        if (DotMeshes[idx] == null)
            DotMeshes[idx] = CraftMesh.Pompom(1f, 0.14f, 12, 10, 11 + idx * 3);
        return DotMeshes[idx];
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
            r = Mathf.Lerp(NeckR, NeckR * 1.45f, t);
        }
        return r;
    }

    static Mesh StemMesh()
    {
        if (_stem != null) return _stem;
        const int seg = 24;
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
                float rib = 1f + 0.045f * Mathf.Sin(a * 12f);
                v.Add(new Vector3(Mathf.Cos(a) * r * rib, y, Mathf.Sin(a) * r * rib));
                uv.Add(new Vector2(i / (float)seg * 1.6f, u * 1.4f));
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
        _green = Felt("Dotted Green", Green, "Look/tex-yarn-green", new Vector2(3.2f, 3.2f), 2.1f, 0.28f);
        return _green;
    }

    static Material TrunkMat()
    {
        if (_trunk != null) return _trunk;
        _trunk = Felt("Dotted Trunk", TrunkTan, "Look/tex-yarn-fiber", new Vector2(2.2f, 1.6f), 1.6f, 0.14f);
        return _trunk;
    }

    static Material DotCreamMat()
    {
        if (_dotCream != null) return _dotCream;
        _dotCream = Felt("Dotted Knot Cream", DotCream, "Look/tex-yarn-fiber", new Vector2(2.0f, 2.0f), 1.45f, 0.18f);
        return _dotCream;
    }

    static Material DotTanMat()
    {
        if (_dotTan != null) return _dotTan;
        _dotTan = Felt("Dotted Knot Tan", DotTan, "Look/tex-yarn-fiber", new Vector2(2.0f, 2.0f), 1.45f, 0.18f);
        return _dotTan;
    }

    static Material DotSageMat()
    {
        if (_dotSage != null) return _dotSage;
        _dotSage = Felt("Dotted Knot Sage", DotSage, "Look/tex-yarn-fiber", new Vector2(2.0f, 2.0f), 1.45f, 0.18f);
        return _dotSage;
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

using System.Collections.Generic;
using UnityEngine;

/// rotate-flower-tree.mp4: a tight bouquet of orange and red yarn daisies
/// with small beige centers, red yarn balls in the gaps, on a short ribbed trunk.
public static class FeltFlowerTree
{
    // tex-yarn-fiber mean is about (148, 116, 82). These tints land on the clip's wool:
    // orange (210, 97, 41), crimson (193, 21, 16), deep red (141, 14, 11),
    // beige centers (181, 135, 100), trunk (119, 70, 39).
    static readonly Color OrangeTint = new Color(1.42f, 0.84f, 0.50f);
    static readonly Color OrangeFlat = new Color(0.90f, 0.38f, 0.14f);
    static readonly Color RedTint = new Color(1.30f, 0.18f, 0.20f);
    static readonly Color RedFlat = new Color(0.75f, 0.08f, 0.06f);
    static readonly Color DeepTint = new Color(0.95f, 0.12f, 0.14f);
    static readonly Color DeepFlat = new Color(0.55f, 0.05f, 0.045f);
    static readonly Color BeigeTint = new Color(1.22f, 1.16f, 1.22f);
    static readonly Color BeigeFlat = new Color(0.71f, 0.53f, 0.39f);
    static readonly Color TrunkTint = new Color(0.80f, 0.60f, 0.48f);
    static readonly Color TrunkFlat = new Color(0.46f, 0.27f, 0.15f);

    static Material _orange, _red, _deep, _beige, _trunk;
    static Mesh _petal, _bole;

    /// Scale 1 is about 1.50 tall, foot on y = 0. Root is named Flower Tree.
    public static GameObject Build(Vector3 position, float scale = 1f)
    {
        var tree = new GameObject("Flower Tree");
        tree.transform.position = position;
        tree.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        tree.isStatic = true;
        Transform host = tree.transform;

        var trunk = Body(host, "Flower Trunk", Vector3.zero, Quaternion.identity, TrunkMesh(), TrunkMat());
        NoShadow(trunk);

        Vector3 heart = new Vector3(0f, 0.80f, 0f);
        const float shell = 0.50f;

        for (int i = 0; i < 9; i++)
        {
            Vector3 dir = SphereDir(i, 9);
            if (dir.y < 0f) dir.y = -dir.y * 0.35f;
            dir.Normalize();
            Fill(host, heart + dir * (shell * 0.36f), 0.13f + (i % 3) * 0.012f,
                i % 4 == 0 ? OrangeMat() : DeepMat(), i);
        }

        const int slots = 34;
        for (int i = 0; i < slots; i++)
        {
            Vector3 dir = SphereDir(i, slots);
            if (dir.y < -0.58f) continue;
            Vector3 pos = OnBouquet(dir, heart, shell);
            if ((i % 5) == 3)
            {
                Fill(host, pos, 0.095f + (i % 3) * 0.012f, (i % 2) == 0 ? DeepMat() : RedMat(), 20 + i);
                continue;
            }
            float size = 0.94f + ((i * 3) % 5) * 0.04f;
            if (dir.y < -0.2f) size *= 0.88f;
            Daisy(host, pos, dir, (i % 2) == 0 ? OrangeMat() : RedMat(), i, size);
        }

        for (int i = 0; i < 16; i++)
        {
            Vector3 dir = SphereDir(i * 2 + 1, 32);
            if (dir.y < -0.30f) continue;
            Vector3 pos = OnBouquet(dir, heart, shell * (0.78f + (i % 3) * 0.04f));
            Material mat = (i % 7) == 0 ? OrangeMat() : ((i % 2) == 0 ? DeepMat() : RedMat());
            Fill(host, pos, 0.046f + (i % 4) * 0.008f, mat, 50 + (i % 6));
        }

        return tree;
    }

    /// Horizontal pinch below the equator so the ball sits on the trunk instead of hanging past it.
    static Vector3 OnBouquet(Vector3 dir, Vector3 heart, float shell)
    {
        float tuck = 1f;
        if (dir.y < -0.38f)
        {
            float t = Mathf.Clamp01((-0.38f - dir.y) / 0.28f);
            tuck = Mathf.Lerp(1f, 0.62f, t * t);
        }
        return heart + new Vector3(dir.x * shell * tuck, dir.y * shell, dir.z * shell * tuck);
    }

    static Vector3 SphereDir(int i, int count)
    {
        float y = 1f - (i / (float)(count - 1)) * 2f;
        float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float phi = i * 2.399963f;
        return new Vector3(Mathf.Cos(phi) * ring, y, Mathf.Sin(phi) * ring).normalized;
    }

    static void Daisy(Transform host, Vector3 pos, Vector3 outDir, Material petal, int seed, float size)
    {
        var flower = new GameObject("Daisy");
        flower.transform.SetParent(host, false);
        flower.transform.localPosition = pos;
        Vector3 hint = Mathf.Abs(outDir.y) > 0.92f ? Vector3.forward : Vector3.up;
        flower.transform.localRotation = Quaternion.LookRotation(outDir, hint) * Quaternion.Euler(0f, 0f, (seed * 37) % 360);
        flower.transform.localScale = Vector3.one * size;
        flower.isStatic = true;

        const int n = 8;
        for (int p = 0; p < n; p++)
        {
            float a = p * Mathf.PI * 2f / n;
            var radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            var spin = Quaternion.FromToRotation(Vector3.up, radial) * Quaternion.Euler(16f, 0f, 0f);
            NoShadow(Body(flower.transform, "Daisy Petal", radial * 0.016f, spin, PetalMesh(), petal));
        }

        NoShadow(Body(flower.transform, "Daisy Center", new Vector3(0f, 0f, 0.02f), Quaternion.identity,
            CraftMesh.Pompom(0.020f, 0.35f, 8, 6, 4), BeigeMat()));
        for (int c = 0; c < 6; c++)
        {
            float a = c * Mathf.PI * 2f / 6f + 0.4f;
            var nub = new Vector3(Mathf.Cos(a) * 0.026f, Mathf.Sin(a) * 0.026f, 0.026f);
            NoShadow(Body(flower.transform, "Daisy Center", nub, Quaternion.identity,
                CraftMesh.Pompom(0.011f, 0.42f, 8, 6, 5 + (c % 3)), BeigeMat()));
        }
    }

    static void Fill(Transform host, Vector3 pos, float radius, Material mat, int seed)
    {
        NoShadow(Body(host, "Bouquet Fill", pos, Quaternion.identity,
            CraftMesh.Pompom(radius, 0.20f, 10, 8, seed), mat));
    }

    /// Chubby yarn petal. Base at the origin, tip toward +y, cupped a little toward +z.
    static Mesh PetalMesh()
    {
        if (_petal != null) return _petal;
        const int along = 8;
        const int around = 12;
        const float length = 0.145f;
        int cols = around + 1;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        for (int k = 0; k <= along; k++)
        {
            float u = k / (float)along;
            float belly = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.62f));
            float width = Mathf.Lerp(0.44f, 1f, belly);
            width *= Mathf.Lerp(1f, 0.30f, Mathf.SmoothStep(0.72f, 1f, u));
            float y = u * length;
            float cup = 0.016f * u * u;
            for (int i = 0; i <= around; i++)
            {
                float a = i / (float)around * Mathf.PI * 2f;
                float wob = 1f + 0.04f * Mathf.Sin(a * 3f + u * 11f);
                v.Add(new Vector3(Mathf.Cos(a) * 0.050f * width * wob, y, Mathf.Sin(a) * 0.020f * width * wob + cup));
                uv.Add(new Vector2(i / (float)around, u));
            }
        }
        for (int k = 0; k < along; k++)
            for (int i = 0; i < around; i++)
            {
                int i0 = k * cols + i, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        Cap(v, uv, tris, 0, around, false);
        Cap(v, uv, tris, along * cols, around, true);
        var mesh = new Mesh { name = "Daisy Petal Yarn" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        _petal = mesh;
        return _petal;
    }

    static void Cap(List<Vector3> v, List<Vector2> uv, List<int> tris, int ring, int around, bool tip)
    {
        Vector3 c = Vector3.zero;
        for (int i = 0; i < around; i++) c += v[ring + i];
        c /= around;
        int center = v.Count;
        v.Add(c);
        uv.Add(new Vector2(0.5f, tip ? 1f : 0f));
        Vector3 n = Vector3.Cross(v[ring] - c, v[ring + 1] - c);
        Vector3 outward = tip ? Vector3.up : Vector3.down;
        bool wind = Vector3.Dot(n, outward) >= 0f;
        for (int i = 0; i < around; i++)
        {
            int a = ring + i, b = a + 1;
            if (wind) { tris.Add(center); tris.Add(a); tris.Add(b); }
            else { tris.Add(center); tris.Add(b); tris.Add(a); }
        }
    }

    static Mesh TrunkMesh()
    {
        if (_bole != null) return _bole;
        const int samples = 14;
        const int seg = 20;
        int cols = seg + 1;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var tris = new List<int>();
        for (int k = 0; k < samples; k++)
        {
            float u = k / (float)(samples - 1);
            float y = u * 0.62f;
            float neck = Mathf.Lerp(0.158f, 0.122f, Mathf.SmoothStep(0f, 0.58f, u));
            float flare = Mathf.SmoothStep(0.52f, 1f, u);
            float r = Mathf.Lerp(neck, 0.28f, flare * flare);
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float rib = 1f + 0.055f * Mathf.Sin(a * 10f) + 0.02f * Mathf.Sin(a * 3f + u * 15f);
                v.Add(new Vector3(Mathf.Cos(a) * r * rib, y, Mathf.Sin(a) * r * rib));
                uv.Add(new Vector2(i / (float)seg, u));
            }
        }
        for (int k = 0; k < samples - 1; k++)
            for (int i = 0; i < seg; i++)
            {
                int i0 = k * cols + i, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        Cap(v, uv, tris, 0, seg, false);
        Cap(v, uv, tris, (samples - 1) * cols, seg, true);
        var mesh = new Mesh { name = "Flower Trunk Yarn" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        _bole = mesh;
        return _bole;
    }

    static Transform Body(Transform host, string name, Vector3 localPos, Quaternion localRot, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        return go.transform;
    }

    static void NoShadow(Transform part)
    {
        var renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Material OrangeMat()
    {
        return _orange != null ? _orange : (_orange = Yarn("Daisy Orange", OrangeTint, OrangeFlat, new Vector2(0.55f, 2.2f), 1.35f, 0.10f, 0.08f));
    }

    static Material RedMat()
    {
        return _red != null ? _red : (_red = Yarn("Daisy Red", RedTint, RedFlat, new Vector2(0.55f, 2.2f), 1.35f, 0.10f, 0.08f));
    }

    static Material DeepMat()
    {
        return _deep != null ? _deep : (_deep = Yarn("Daisy Deep", DeepTint, DeepFlat, new Vector2(1.8f, 1.8f), 1.45f, 0.08f, 0.10f));
    }

    static Material BeigeMat()
    {
        return _beige != null ? _beige : (_beige = Yarn("Daisy Center", BeigeTint, BeigeFlat, new Vector2(2.4f, 2.4f), 1.2f, 0.04f, 0.12f));
    }

    static Material TrunkMat()
    {
        return _trunk != null ? _trunk : (_trunk = Yarn("Flower Trunk Felt", TrunkTint, TrunkFlat, new Vector2(2.2f, 1.5f), 1.6f, 0.06f, 0.10f));
    }

    static Material Yarn(string name, Color tint, Color flat, Vector2 tile, float bump, float sheen, float fiber)
    {
        var shader = Shader.Find("Escape/FeltFiber");
        var tex = Resources.Load<Texture2D>("Look/tex-yarn-fiber");
        if (shader == null || tex == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            var plain = new Material(shader) { name = name };
            if (plain.HasProperty("_BaseColor")) plain.SetColor("_BaseColor", flat);
            if (plain.HasProperty("_Color")) plain.color = flat;
            if (plain.HasProperty("_Smoothness")) plain.SetFloat("_Smoothness", 0.08f);
            return plain;
        }

        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", tile);
        }
        var fuzz = Resources.Load<Texture2D>("Look/tex-felt-normal");
        if (fuzz != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", fuzz);
            mat.SetTextureScale("_BumpMap", tile);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bump);
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", sheen);
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", 0.74f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);
        if (mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
        if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", 0f);
        return mat;
    }
}

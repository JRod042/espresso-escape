using System.Collections.Generic;
using UnityEngine;

/// One opening pine. rotate-pine.mp4: four wide scalloped felt shelves,
/// a brown cone on each lobe, a pointed cap, and a short trunk.
public static class FeltPine
{
    // Sage felt. The yarn texture is already green, so the tint pulls it yellow-olive.
    static readonly Color Olive = new Color(0.80f, 0.74f, 0.68f);
    static readonly Color TrunkBrown = new Color(0.72f, 0.62f, 0.50f);
    static readonly Color ConeBrown = new Color(0.52f, 0.40f, 0.32f);

    const float TrunkHeight = 0.42f;
    const float FootR = 0.17f;
    const float NeckR = 0.11f;

    const int Scallops = 7;
    const float ScallopDepth = 0.30f;
    const float LipDroop = 0.20f;

    // baseY, hem radius, height, top radius. Lip of the lobe tips sits at baseY.
    // Lower shelves stay wide; only the cap draws in to the point. Tip is at 1.38.
    static readonly Vector4[] Shelves =
    {
        new Vector4(0.15f, 0.50f, 0.46f, 0.30f),
        new Vector4(0.46f, 0.45f, 0.40f, 0.24f),
        new Vector4(0.72f, 0.37f, 0.38f, 0.15f),
        new Vector4(1.00f, 0.27f, 0.38f, 0.02f)
    };

    static Material _yarn, _trunk, _coneMat;
    static Mesh _bole;

    /// Builds one pine under a root named Pine. Position is world space.
    /// Scale 1 is about 1.38 tall with the trunk foot on y = 0.
    public static GameObject Build(Vector3 position, float scale = 1f, Material green = null, Material brown = null)
    {
        var pine = new GameObject("Pine");
        pine.transform.position = position;
        pine.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        pine.isStatic = true;
        Transform host = pine.transform;

        Material yarn = Yarn() ?? green;
        Material bole = TrunkYarn() ?? brown;
        Material cones = ConeYarn() ?? brown;

        MeshBody(host, "Pine Trunk", Vector3.zero, 0f, TrunkMesh(), bole);
        for (int i = 0; i < Shelves.Length; i++)
        {
            Vector4 s = Shelves[i];
            float yaw = (i & 1) == 0 ? 0f : 180f / Scallops;
            float hem = Mathf.Lerp(0.016f, 0.038f, s.y / 0.50f);
            Mesh shelfMesh = CraftMesh.PineFeltTier(s.y, s.w, s.z, Scallops, ScallopDepth, LipDroop, hem, s.y * 0.028f, 72, 12, 3 + i);
            Transform shelf = MeshBody(host, "Pine Shelf", new Vector3(0f, s.x, 0f), yaw, shelfMesh, yarn);
            var shelfRenderer = shelf.GetComponent<MeshRenderer>();
            if (shelfRenderer != null)
            {
                shelfRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shelfRenderer.receiveShadows = false;
            }
            for (int k = 0; k < Scallops; k++)
            {
                float phi = k * Mathf.PI * 2f / Scallops;
                float reach = s.y * 0.88f;
                float y = s.z * 0.14f;
                float diam = Mathf.Lerp(0.090f, 0.055f, i / (float)(Shelves.Length - 1));
                var p = new Vector3(Mathf.Cos(phi) * reach, y, Mathf.Sin(phi) * reach);
                Cone(shelf, cones, p, diam, i * 10 + k);
            }
        }

        return pine;
    }

    static float TrunkR(float y01)
    {
        return Mathf.Lerp(FootR, NeckR, Mathf.SmoothStep(0f, 1f, y01));
    }

    static Mesh TrunkMesh()
    {
        if (_bole != null) return _bole;
        const int samples = 8;
        const int seg = 24;
        int cols = seg + 1;
        var v = new List<Vector3>(samples * cols + 2);
        var uv = new List<Vector2>(samples * cols + 2);
        var tris = new List<int>(samples * seg * 6);
        for (int k = 0; k < samples; k++)
        {
            float y01 = 1f - k / (float)(samples - 1);
            for (int iu = 0; iu <= seg; iu++)
            {
                float a = iu / (float)seg;
                float phi = a * Mathf.PI * 2f;
                float r = TrunkR(y01) * (1f + 0.02f * Mathf.Sin(phi * 3f + y01 * 6f));
                v.Add(new Vector3(Mathf.Cos(phi) * r, TrunkHeight * y01, Mathf.Sin(phi) * r));
                uv.Add(new Vector2(a, y01));
            }
        }
        for (int k = 0; k < samples - 1; k++)
            Strip(tris, k * cols, (k + 1) * cols, cols);
        int top = v.Count;
        v.Add(new Vector3(0f, TrunkHeight, 0f));
        uv.Add(new Vector2(0.5f, 1f));
        int foot = v.Count;
        v.Add(Vector3.zero);
        uv.Add(new Vector2(0.5f, 0f));
        Fan(v, tris, top, 0, seg, Vector3.up);
        Fan(v, tris, foot, (samples - 1) * cols, seg, Vector3.down);
        var mesh = new Mesh { name = "Pine Trunk" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        _bole = mesh;
        return _bole;
    }

    static void Strip(List<int> t, int rowA, int rowB, int cols)
    {
        for (int iu = 0; iu < cols - 1; iu++)
        {
            int i0 = rowA + iu, i1 = i0 + 1, i2 = rowB + iu, i3 = i2 + 1;
            t.Add(i0); t.Add(i1); t.Add(i2);
            t.Add(i1); t.Add(i3); t.Add(i2);
        }
    }

    static void Fan(List<Vector3> v, List<int> t, int center, int ring, int seg, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[ring] - v[center], v[ring + 1] - v[center]);
        bool wind = Vector3.Dot(n, outward) >= 0f;
        for (int i = 0; i < seg; i++)
        {
            int a = ring + i, b = a + 1;
            if (wind) { t.Add(center); t.Add(a); t.Add(b); }
            else { t.Add(center); t.Add(b); t.Add(a); }
        }
    }

    static Transform MeshBody(Transform host, string name, Vector3 localPos, float yaw, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(host, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        return go.transform;
    }

    static void Cone(Transform host, Material mat, Vector3 localPos, float diameter, int seed)
    {
        var go = new GameObject("Pine Cone");
        go.transform.SetParent(host, false);
        Vector3 flat = new Vector3(localPos.x, 0f, localPos.z);
        Vector3 outDir = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
        float length = diameter * 1.32f;
        Vector3 aim = (Vector3.up + outDir * 0.16f).normalized;
        go.transform.localPosition = localPos + aim * (length * 0.30f);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, aim);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = ConeMesh(diameter * 0.5f, length, seed + 1);
        var renderer = go.AddComponent<MeshRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static readonly Dictionary<string, Mesh> ConeMeshes = new Dictionary<string, Mesh>();

    /// Soft felt egg with a few scale bumps, so the ornament reads as a pinecone at yard size.
    static Mesh ConeMesh(float radius, float length, int seed)
    {
        string key = radius.ToString("0.###") + "_" + length.ToString("0.###") + "_" + seed;
        if (ConeMeshes.TryGetValue(key, out var cached) && cached != null) return cached;
        int su = 12, sv = 10, cols = su + 1;
        const int scaleRows = 4;
        const int scaleAround = 5;
        var v = new List<Vector3>(cols * (sv + 1));
        var uv = new List<Vector2>(cols * (sv + 1));
        var tris = new List<int>(su * sv * 6);
        for (int iv = 0; iv <= sv; iv++)
        {
            float fv = iv / (float)sv;
            float taper = fv < 0.66f
                ? Mathf.SmoothStep(0f, 1f, fv / 0.66f)
                : Mathf.Lerp(1f, 0.72f, (fv - 0.66f) / 0.34f);
            float y = Mathf.Lerp(length * 0.56f, -length * 0.44f, fv);
            for (int iu = 0; iu <= su; iu++)
            {
                float phi = iu == su ? 0f : iu / (float)su * Mathf.PI * 2f;
                int rowi = Mathf.Min(scaleRows - 1, Mathf.FloorToInt(fv * scaleRows));
                float rowFrac = fv * scaleRows - rowi;
                float around = phi / (Mathf.PI * 2f) * scaleAround + ((rowi & 1) == 0 ? 0f : 0.5f);
                float aroundFrac = around - Mathf.Floor(around);
                float cell = Mathf.Sin(Mathf.Clamp01(rowFrac) * Mathf.PI) * Mathf.Sin(Mathf.Clamp01(aroundFrac) * Mathf.PI);
                float band = Mathf.Clamp01((fv - 0.1f) / 0.16f) * Mathf.Clamp01((0.96f - fv) / 0.16f);
                float bump = radius * 0.28f * Mathf.Pow(Mathf.Clamp01(cell), 0.40f) * band;
                float r = radius * taper + bump;
                v.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
                uv.Add(new Vector2(iu / (float)su, 1f - fv));
            }
        }
        for (int iv = 0; iv < sv; iv++)
        {
            for (int iu = 0; iu < su; iu++)
            {
                int i0 = iv * cols + iu, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        }
        var mesh = new Mesh { name = "Felt Pinecone " + key };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ConeMeshes[key] = mesh;
        return mesh;
    }

    static Material Yarn()
    {
        if (_yarn != null) return _yarn;
        _yarn = Felt("Escape/FeltFiber", "Pine Yarn", "Look/tex-yarn-green", Olive, new Vector2(2.6f, 2.6f), 1.15f, 0.04f, 0.86f, 0.02f, 0.16f);
        return _yarn;
    }

    static Material TrunkYarn()
    {
        if (_trunk != null) return _trunk;
        _trunk = Felt("Escape/FeltFiber", "Pine Trunk Felt", "Look/tex-felt-brown", TrunkBrown, new Vector2(1.3f, 1.7f), 1.0f, 0.03f, 0.78f, 0.03f, 0.10f);
        return _trunk;
    }

    static Material ConeYarn()
    {
        if (_coneMat != null) return _coneMat;
        _coneMat = Felt("Escape/FeltFiber", "Pine Cone Felt", "Look/tex-felt-brown", ConeBrown, new Vector2(2.4f, 3.2f), 1.6f, 0.05f, 0.62f, 0.05f, 0.12f);
        return _coneMat;
    }

    static Material Felt(string shaderName, string matName, string resource, Color tint, Vector2 tile, float bump, float sheen, float wrap, float smooth, float fiber)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) return null;
        var tex = Resources.Load<Texture2D>(resource);
        if (tex == null) return null;
        var mat = new Material(shader) { name = matName };
        mat.enableInstancing = true;
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
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", wrap);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
        if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", 0f);
        return mat;
    }
}
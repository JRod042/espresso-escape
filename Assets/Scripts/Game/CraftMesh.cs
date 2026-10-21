using System.Collections.Generic;
using UnityEngine;

/// Procedural handmade-craft meshes + shared material cache.
/// Used by BuildEscapeScene (editor) and LevelSetSpawner (runtime) so props batch.
public static class CraftMesh
{
    static Mesh _lowSphere;
    static readonly Dictionary<string, Mesh> _eggs = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

    /// Felt egg: UV sphere with a gentle taper toward the top (coffee bean / plush body).
    public static Mesh Egg(float rx = 1f, float ry = 1f, float rz = 1f, float taper = 0.12f, int segU = 36, int segV = 26)
    {
        string key = EggName(rx, ry, rz, taper) + $"_{segU}x{segV}";
        if (_eggs.TryGetValue(key, out var cached) && cached != null) return cached;
        var m = BuildEgg(rx, ry, rz, taper, segU, segV);
        m.name = key;
        _eggs[key] = m;
        return m;
    }

    static string EggName(float rx, float ry, float rz, float taper) => $"FeltEgg_{rx:0.###}_{ry:0.###}_{rz:0.###}_{taper:0.###}";

    static Mesh BuildEgg(float rx, float ry, float rz, float taper, int segU, int segV)
    {
        segU = Mathf.Max(8, segU); segV = Mathf.Max(6, segV);
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int iv = 0; iv <= segV; iv++)
        {
            float v = iv / (float)segV;
            float theta = v * Mathf.PI;          // 0 = top
            float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
            float yN = ct;                        // -1..1
            float shrink = 1f - taper * Mathf.Clamp01(yN) * Mathf.Clamp01(yN); // narrower top
            for (int iu = 0; iu <= segU; iu++)
            {
                float u = iu / (float)segU;
                float phi = u * Mathf.PI * 2f;
                float x = Mathf.Cos(phi) * st * shrink * rx;
                float z = Mathf.Sin(phi) * st * shrink * rz;
                float y = yN * ry;
                verts.Add(new Vector3(x, y, z));
                uvs.Add(new Vector2(u, 1f - v));
                norms.Add(Vector3.zero);
            }
        }
        int cols = segU + 1;
        for (int iv = 0; iv < segV; iv++)
            for (int iu = 0; iu < segU; iu++)
            {
                int i0 = iv * cols + iu, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        var mesh = new Mesh { name = EggName(rx, ry, rz, taper) };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        // smooth the seam column normals so the egg has no visible UV seam
        var n = mesh.normals;
        for (int iv = 0; iv <= segV; iv++)
        {
            int a = iv * cols, b = iv * cols + segU;
            var avg = (n[a] + n[b]).normalized;
            n[a] = avg; n[b] = avg;
        }
        mesh.normals = n;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    /// Point on the egg surface for a given latitude (deg, +up) and longitude (deg, 0 = +z front).
    public static Vector3 EggPoint(float rx, float ry, float rz, float taper, float latDeg, float lonDeg)
    {
        float theta = (90f - latDeg) * Mathf.Deg2Rad;
        float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
        float shrink = 1f - taper * Mathf.Clamp01(ct) * Mathf.Clamp01(ct);
        float phi = (90f - lonDeg) * Mathf.Deg2Rad; // lon 0 → +z
        return new Vector3(Mathf.Cos(phi) * st * shrink * rx, ct * ry, Mathf.Sin(phi) * st * shrink * rz);
    }

    /// 12x8 sphere (~120 verts) for small dressing props instead of the 515-vert primitive.
    public static Mesh LowSphere()
    {
        if (_lowSphere != null) return _lowSphere;
        _lowSphere = BuildEgg(1f, 1f, 1f, 0f, 12, 8);
        _lowSphere.name = "LowSphere12x8";
        return _lowSphere;
    }

    /// Swap a primitive sphere's mesh for the low-poly one (count-neutral, vertex budget /4).
    public static void UseLowSphere(GameObject go)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = LowSphere();
    }

    // ——— shared materials ———

    public static Material Shared(string key, System.Func<Material> make)
    {
        if (_mats.TryGetValue(key, out var m) && m != null) return m;
        m = make();
        if (m != null) m.enableInstancing = true;
        _mats[key] = m;
        return m;
    }

    public static void ClearSharedCache() => _mats.Clear();

    public static string Key(string tex, Color tint, Vector2 tile, float bump, float sheen, float wrap, float rough, float seam)
    {
        return $"{tex}|{tint.r:0.###},{tint.g:0.###},{tint.b:0.###}|{tile.x:0.##},{tile.y:0.##}|{bump:0.##}|{sheen:0.##}|{wrap:0.##}|{rough:0.##}|{seam:0.##}";
    }

    /// Normal map for a felt material: burlap gets its own weave normal, everything else the wool fuzz normal.
    static Texture2D _feltNormal, _burlapNormal;
    public static Texture2D NormalFor(Texture2D albedo)
    {
        bool burlap = albedo != null && albedo.name.Contains("burlap");
        if (burlap)
        {
            if (_burlapNormal == null) _burlapNormal = Resources.Load<Texture2D>("Look/tex-burlap-normal");
            return _burlapNormal;
        }
        if (_feltNormal == null) _feltNormal = Resources.Load<Texture2D>("Look/tex-felt-normal");
        return _feltNormal;
    }

    static Shader FeltShader()
    {
        var s = Shader.Find("Escape/FeltFiber");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    /// Configure an Escape/FeltFiber material. Falls back to URP/Lit when the shader is missing.
    public static Material Felt(Texture2D tex, Color tint, Vector2 tile, float bump, float sheen, float wrap, float rough, float seam = 0f)
    {
        var mat = new Material(FeltShader());
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); mat.SetTextureScale("_BaseMap", tile); }
            if (mat.HasProperty("_MainTex")) { mat.SetTexture("_MainTex", tex); mat.SetTextureScale("_MainTex", tile); }
        }
        var nrm = NormalFor(tex);
        if (nrm != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", nrm);
            mat.SetTextureScale("_BumpMap", tile);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.color = tint;
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bump);
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", sheen);
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", wrap);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - rough);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - rough);
        if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", seam);
        return mat;
    }

    public static Material Bead(Color color, float gloss = 0.9f)
    {
        var shader = Shader.Find("Escape/Bead");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Gloss")) mat.SetFloat("_Gloss", gloss);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", gloss);
        return mat;
    }
}

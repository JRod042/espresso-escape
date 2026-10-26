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

    // ——— needle-felt diorama builders ———
    // Conventions: +y up, lathe seam at +x, u = angle / 2π. Row strips use the Egg winding, so a lathe
    // profile is walked top → bottom along outer walls, centre → rim across top caps and rim → centre
    // across bottom caps to get outward normals. Every builder is cached by its parameter key.

    static readonly Dictionary<string, Mesh> _props = new Dictionary<string, Mesh>();

    static Mesh Cached(string key, System.Func<Mesh> build)
    {
        if (_props.TryGetValue(key, out var m) && m != null) return m;
        m = build();
        m.name = key;
        _props[key] = m;
        return m;
    }

    /// Deterministic 0..1 hash of two ints (no Random state, same result in editor and player).
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

    /// Smooth, seamless ±1 noise over a unit direction: a few hashed sine plane waves.
    static float FuzzNoise(Vector3 d, int seed)
    {
        float n = 0f;
        for (int k = 0; k < 4; k++)
        {
            var f = new Vector3(Hash01(seed, k * 3) - 0.5f, Hash01(seed, k * 3 + 1) - 0.5f, Hash01(seed, k * 3 + 2) - 0.5f).normalized * (3f + k * 2.5f);
            float ph = Hash01(seed + 97, k) * 6.2831853f;
            n += Mathf.Sin(Vector3.Dot(d, f) + ph) / (1f + k * 0.6f);
        }
        return n / 2.44f;
    }

    /// Two triangles joining vertex row A to row B (both `cols` long), Egg winding.
    static void Strip(List<int> t, int rowA, int rowB, int cols)
    {
        for (int iu = 0; iu < cols - 1; iu++)
        {
            int i0 = rowA + iu, i1 = i0 + 1, i2 = rowB + iu, i3 = i2 + 1;
            t.Add(i0); t.Add(i1); t.Add(i2);
            t.Add(i1); t.Add(i3); t.Add(i2);
        }
    }

    /// Quad a-b-c-d with the winding chosen so its face normal points along `outward`.
    static void OrientQuad(List<Vector3> v, List<int> t, int a, int b, int c, int d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]) + Vector3.Cross(v[c] - v[a], v[d] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f) { t.Add(a); t.Add(b); t.Add(c); t.Add(a); t.Add(c); t.Add(d); }
        else { t.Add(a); t.Add(c); t.Add(b); t.Add(a); t.Add(d); t.Add(c); }
    }

    /// Upload lists, recalc normals (optionally averaging the u=0 / u=1 seam columns), bounds, tangents.
    static Mesh Finish(List<Vector3> v, List<Vector2> uv, List<int> t, string name, int seamCols = 0)
    {
        var m = new Mesh { name = name };
        if (v.Count > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        if (seamCols > 1 && v.Count % seamCols == 0)
        {
            var n = m.normals;
            int rows = v.Count / seamCols;
            for (int r = 0; r < rows; r++)
            {
                int a = r * seamCols, b = a + seamCols - 1;
                var avg = (n[a] + n[b]).normalized;
                n[a] = avg; n[b] = avg;
            }
            m.normals = n;
        }
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }

    struct Prof
    {
        public float r, y;
        public bool crease; // true: the next profile point duplicates this one → hard edge, no strip between them
        public Prof(float r, float y, bool crease) { this.r = r; this.y = y; this.crease = crease; }
    }

    static void Pt(List<Prof> p, float r, float y) => p.Add(new Prof(r, y, false));
    static void Crease(List<Prof> p, float r, float y) { p.Add(new Prof(r, y, true)); p.Add(new Prof(r, y, false)); }

    /// Revolve an (r, y) profile around +y (see the winding note above). UV v = arc length along the profile.
    static Mesh Lathe(List<Prof> prof, int segU, string name)
    {
        int su = Mathf.Max(6, segU), cols = su + 1, rows = prof.Count;
        var v = new List<Vector3>(rows * cols);
        var uv = new List<Vector2>(rows * cols);
        var t = new List<int>(rows * su * 6);
        var arc = new float[rows];
        float total = 0f;
        for (int k = 1; k < rows; k++)
        {
            total += Vector2.Distance(new Vector2(prof[k].r, prof[k].y), new Vector2(prof[k - 1].r, prof[k - 1].y));
            arc[k] = total;
        }
        for (int k = 0; k < rows; k++)
        {
            float vt = total > 0f ? arc[k] / total : 0f;
            for (int iu = 0; iu <= su; iu++)
            {
                float u = iu / (float)su, phi = u * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(phi) * prof[k].r, prof[k].y, Mathf.Sin(phi) * prof[k].r));
                uv.Add(new Vector2(u, vt));
            }
        }
        for (int k = 0; k < rows - 1; k++)
            if (!prof[k].crease) Strip(t, k * cols, (k + 1) * cols, cols);
        return Finish(v, uv, t, name, cols);
    }

    /// Bake several meshes (each with its own transform) into one single-sub-mesh mesh.
    /// Unity transforms normals/tangents by the matrix, so keep part transforms to rotation,
    /// translation and uniform scale for exact shading (build ellipsoids with Egg instead of scaling).
    public static Mesh Combine(IList<(Mesh mesh, Matrix4x4 xf)> parts, string name)
    {
        var ci = new CombineInstance[parts.Count];
        int total = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            ci[i].mesh = parts[i].mesh;
            ci[i].transform = parts[i].xf;
            ci[i].subMeshIndex = 0;
            total += parts[i].mesh.vertexCount;
        }
        var m = new Mesh { name = name };
        if (total > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.CombineMeshes(ci, true, true, false);
        m.RecalculateBounds();
        return m;
    }

    // 1. bean body ------------------------------------------------------------------------------

    const float BeanBelly = 0.08f; // shifts the fullest ring just below mid-height

    /// Normalised bean lathe profile. yN ∈ [-1, 1] (1 = crown) → radial factor 0..1 and y factor -1..1.
    static void BeanProfile(float yN, float topTaper, float bottomFlat, out float rN, out float yOut)
    {
        float s = Mathf.Clamp(yN + BeanBelly * (1f - yN * yN), -1f, 1f);
        float r = Mathf.Sqrt(Mathf.Max(0f, 1f - s * s));
        float up = Mathf.Clamp01(yN);
        r *= 1f - topTaper * up * up;                 // narrower crown
        float bf = Mathf.Clamp(bottomFlat, 0f, 0.5f);
        float y = yN;
        if (yN < 0f) { float d = -yN; y = yN + bf * d * d * d * d; } // gently squash the base so it sits
        rN = r;
        yOut = (2f * y - bf) / (2f - bf);             // renormalise the span to exactly -1..1
    }

    /// Plush felt bean body: lathe centred at the origin, +y up, lon 0 = +z front. Spans
    /// y ∈ [-height/2, height/2]; width = height·widthRatio (x), depth = height·depthRatio (z).
    /// Slightly narrower crown (topTaper), fullest just below mid-height, gently flattened base (bottomFlat).
    /// ≈ (segU+1)(segV+1) verts: 1073 at defaults.
    public static Mesh BeanBody(float height = 1f, float widthRatio = 0.76f, float depthRatio = 0.68f, float topTaper = 0.10f, float bottomFlat = 0.06f, int segU = 36, int segV = 28)
    {
        string key = $"BeanBody_{height:0.###}_{widthRatio:0.###}_{depthRatio:0.###}_{topTaper:0.###}_{bottomFlat:0.###}_{segU}x{segV}";
        return Cached(key, () =>
        {
            int su = Mathf.Max(8, segU), sv = Mathf.Max(6, segV), cols = su + 1;
            float rx = height * widthRatio * 0.5f, rz = height * depthRatio * 0.5f, hy = height * 0.5f;
            var v = new List<Vector3>(cols * (sv + 1));
            var uv = new List<Vector2>(cols * (sv + 1));
            var t = new List<int>(su * sv * 6);
            for (int iv = 0; iv <= sv; iv++)
            {
                float fv = iv / (float)sv;
                BeanProfile(Mathf.Cos(fv * Mathf.PI), topTaper, bottomFlat, out float rN, out float yP);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su, phi = u * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(phi) * rN * rx, yP * hy, Mathf.Sin(phi) * rN * rz));
                    uv.Add(new Vector2(u, 1f - fv));
                }
            }
            for (int iv = 0; iv < sv; iv++) Strip(t, iv * cols, (iv + 1) * cols, cols);
            return Finish(v, uv, t, key, cols);
        });
    }

    /// Point on the BeanBody surface (same parameters as BeanBody) for a latitude (deg, +up) and
    /// longitude (deg, 0 = +z front, +lon toward -x like EggPoint). Use it to seat eyes, stitches, limbs.
    public static Vector3 BeanBodyPoint(float height, float widthRatio, float depthRatio, float topTaper, float bottomFlat, float latDeg, float lonDeg)
    {
        float theta = (90f - latDeg) * Mathf.Deg2Rad;
        BeanProfile(Mathf.Cos(theta), topTaper, bottomFlat, out float rN, out float yP);
        float phi = (90f - lonDeg) * Mathf.Deg2Rad; // lon 0 → +z
        float rx = height * widthRatio * 0.5f, rz = height * depthRatio * 0.5f;
        return new Vector3(Mathf.Cos(phi) * rN * rx, yP * height * 0.5f, Mathf.Sin(phi) * rN * rz);
    }

    // 2. mitt / foot nub -------------------------------------------------------------------------

    /// Rounded mitt or foot nub: soft capsule along +y with its root pole at the origin and its tip at
    /// y = length + 2·radius (`length` is the straight middle; 0 gives a soft ball). z is multiplied by
    /// `squash` (<1 flattens it front-to-back). Superellipse caps (p = 2.5) keep the cylinder join
    /// curvature-continuous, so there is no hard shading edge. ≈ (segU+1)(2·⌊segV/2⌋+2) verts: 180 at defaults.
    public static Mesh Nub(float radius = 1f, float length = 1.4f, float squash = 0.9f, int segU = 14, int segV = 10)
    {
        string key = $"Nub_{radius:0.###}_{length:0.###}_{squash:0.###}_{segU}x{segV}";
        return Cached(key, () =>
        {
            int su = Mathf.Max(8, segU), cols = su + 1, nCap = Mathf.Max(3, segV / 2);
            float len = Mathf.Max(0f, length), total = len + 2f * radius;
            const float e = 0.8f; // 2/p for superellipse exponent p = 2.5
            int rows = 2 * nCap + 2;
            var v = new List<Vector3>(rows * cols);
            var uv = new List<Vector2>(rows * cols);
            var t = new List<int>(rows * su * 6);
            void Ring(float r, float y)
            {
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su, phi = u * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r * squash));
                    uv.Add(new Vector2(u, 1f - y / total));
                }
            }
            for (int k = 0; k <= nCap; k++) // tip cap: pole → upper join
            {
                float th = k / (float)nCap * Mathf.PI * 0.5f;
                Ring(radius * Mathf.Pow(Mathf.Sin(th), e), radius + len + radius * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(th)), e));
            }
            for (int k = 0; k <= nCap; k++) // root cap: lower join → root pole
            {
                float th = Mathf.PI * 0.5f + k / (float)nCap * Mathf.PI * 0.5f;
                Ring(radius * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(th)), e), radius - radius * Mathf.Pow(Mathf.Max(0f, -Mathf.Cos(th)), e));
            }
            for (int r = 0; r < rows - 1; r++)
            {
                if (r == nCap && len < 1e-5f) continue; // joins coincide → skip the zero-height strip
                Strip(t, r * cols, (r + 1) * cols, cols);
            }
            return Finish(v, uv, t, key, cols);
        });
    }

    // 3. + 4. felt pine --------------------------------------------------------------------------

    /// One felt pine tier: cone skirt from a small flat top (radiusTop at y = height) to a scalloped rim
    /// (radius modulated by cos(scallops·φ), depth = scallopDepth of radiusBottom). Scallop points droop
    /// to y = 0 while the valleys lift by lipDroop·height; closed underside. +y up, base at y = 0.
    /// ≈ (segU+1)(rings+5) verts: 627 at defaults.
    public static Mesh PineTier(float radiusBottom = 1f, float radiusTop = 0.18f, float height = 0.9f, int scallops = 7, float scallopDepth = 0.14f, float lipDroop = 0.10f, int segU = 56, int rings = 6)
    {
        string key = $"PineTier_{radiusBottom:0.###}_{radiusTop:0.###}_{height:0.###}_{scallops}_{scallopDepth:0.###}_{lipDroop:0.###}_{segU}x{rings}";
        return Cached(key, () =>
        {
            int su = Mathf.Max(8, segU), cols = su + 1, nr = Mathf.Max(2, rings), ns = Mathf.Max(0, scallops);
            int rows = nr + 5; // top centre, top edge, skirt (nr+1), rim duplicate, bottom centre
            var v = new List<Vector3>(rows * cols);
            var uv = new List<Vector2>(rows * cols);
            var t = new List<int>(rows * su * 6);
            int row = 0;
            void Ring(System.Func<float, Vector3> f)
            {
                float vt = row / (float)(rows - 1);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su;
                    v.Add(f(u * Mathf.PI * 2f));
                    uv.Add(new Vector2(u, vt));
                }
                row++;
            }
            float Sc(float phi) => ns == 0 ? 0f : 0.5f - 0.5f * Mathf.Cos(ns * phi);
            Vector3 Skirt(float phi, float f)
            {
                float sc = Sc(phi);
                float r = Mathf.Lerp(radiusTop, radiusBottom, Mathf.Pow(f, 1.15f)) * (1f - scallopDepth * sc * f * f);
                float y = height * (1f - f) + lipDroop * height * sc * f * f * f;
                return new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
            }
            Ring(phi => new Vector3(0f, height, 0f));                                                 // top centre
            Ring(phi => new Vector3(Mathf.Cos(phi) * radiusTop, height, Mathf.Sin(phi) * radiusTop)); // top edge (cap side)
            for (int k = 0; k <= nr; k++)                                                             // skirt, top → rim
            {
                float f = k / (float)nr;
                Ring(phi => Skirt(phi, f));
            }
            Ring(phi => Skirt(phi, 1f));                                                              // rim duplicate (underside)
            Ring(phi => new Vector3(0f, lipDroop * height * 0.5f, 0f));                               // bottom centre
            int skirtFirst = 2, rimRow = skirtFirst + nr, rimDup = rimRow + 1, bottom = rimDup + 1;
            Strip(t, 0, cols, cols);
            for (int k = skirtFirst; k < rimRow; k++) Strip(t, k * cols, (k + 1) * cols, cols);
            Strip(t, rimDup * cols, bottom * cols, cols);
            return Finish(v, uv, t, key, cols);
        });
    }

    /// Soft felt pine shelf. Same stack as PineTier, plus a rolled hem and a little nap so the
    /// scallops read as thick felt instead of a hard cone. hemRoll and fuzz are metres.
    public static Mesh PineFeltTier(float radiusBottom, float radiusTop, float height, int scallops, float scallopDepth, float lipDroop, float hemRoll, float fuzz, int segU = 64, int rings = 10, int fuzzSeed = 3)
    {
        string key = $"PineFelt_{radiusBottom:0.###}_{radiusTop:0.###}_{height:0.###}_{scallops}_{scallopDepth:0.###}_{lipDroop:0.###}_{hemRoll:0.###}_{fuzz:0.###}_{segU}x{rings}_{fuzzSeed}";
        return Cached(key, () =>
        {
            int su = Mathf.Max(16, segU), cols = su + 1, nr = Mathf.Max(4, rings), ns = Mathf.Max(0, scallops);
            const int rollN = 5;
            int skirt0 = 2;
            int bottom = skirt0 + nr + rollN + 1;
            int rows = bottom + 1;
            var v = new List<Vector3>(rows * cols);
            var uv = new List<Vector2>(rows * cols);
            var t = new List<int>(rows * su * 6);
            int row = 0;
            void Ring(System.Func<float, Vector3> f)
            {
                float vt = row / (float)(rows - 1);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su;
                    v.Add(f(u * Mathf.PI * 2f));
                    uv.Add(new Vector2(u, vt));
                }
                row++;
            }
            float Sc(float phi) => ns == 0 ? 0f : Mathf.SmoothStep(0f, 1f, 0.5f - 0.5f * Mathf.Cos(ns * phi));
            Vector3 Nap(Vector3 p, float along)
            {
                if (fuzz <= 0f) return p;
                Vector3 axis = new Vector3(p.x, 0f, p.z);
                Vector3 n = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.forward;
                n = (n + Vector3.up * 0.25f).normalized;
                float g = FuzzNoise(n + Vector3.up * along, fuzzSeed);
                return p + n * (g * fuzz * Mathf.Lerp(0.4f, 1f, along));
            }
            Vector3 Skirt(float phi, float f)
            {
                float sc = Sc(phi);
                float flare = Mathf.SmoothStep(0f, 1f, f);
                float r = Mathf.Lerp(radiusTop, radiusBottom, flare) * (1f - scallopDepth * sc * Mathf.Lerp(0.2f, 1f, flare));
                float y = height * (1f - f) + lipDroop * height * sc * f * f;
                return Nap(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r), f);
            }
            Ring(phi => new Vector3(0f, height, 0f));
            Ring(phi => Nap(new Vector3(Mathf.Cos(phi) * radiusTop, height, Mathf.Sin(phi) * radiusTop), 0f));
            for (int k = 0; k <= nr; k++)
                Ring(phi => Skirt(phi, k / (float)nr));
            float roll = Mathf.Max(0.004f, hemRoll);
            for (int a = 1; a <= rollN; a++)
            {
                float ang = a / (float)rollN * Mathf.PI * 0.82f;
                Ring(phi =>
                {
                    Vector3 rim = Skirt(phi, 1f);
                    float rRim = new Vector2(rim.x, rim.z).magnitude;
                    float r = Mathf.Max(0.004f, rRim - roll * (1f - Mathf.Cos(ang)));
                    float y = rim.y - roll * Mathf.Sin(ang);
                    return new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
                });
            }
            Ring(phi => new Vector3(0f, lipDroop * height * 0.35f - roll * 0.65f, 0f));
            Strip(t, 0, cols, cols);
            for (int k = 1; k < bottom; k++) Strip(t, k * cols, (k + 1) * cols, cols);
            return Finish(v, uv, t, key, cols);
        });
    }

    /// Brown felt pinecone. Egg with staggered scale bumps. +y is the tip, centred on the origin.
    public static Mesh Pinecone(float radius = 0.04f, float length = 0.065f, int seed = 1)
    {
        string key = $"Pinecone_{radius:0.###}_{length:0.###}_{seed}";
        return Cached(key, () =>
        {
            int su = 16, sv = 12, cols = su + 1;
            const int scaleRows = 5;
            const int scaleAround = 6;
            var v = new List<Vector3>(cols * (sv + 1));
            var uv = new List<Vector2>(cols * (sv + 1));
            var t = new List<int>(su * sv * 6);
            for (int iv = 0; iv <= sv; iv++)
            {
                float fv = iv / (float)sv;
                float taper = fv < 0.70f
                    ? Mathf.SmoothStep(0f, 1f, fv / 0.70f)
                    : Mathf.Lerp(1f, 0.62f, (fv - 0.70f) / 0.30f);
                float r0 = radius * taper;
                float y = Mathf.Lerp(length * 0.62f, -length * 0.38f, fv);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su;
                    float phi = iu == su ? 0f : u * Mathf.PI * 2f;
                    int rowi = Mathf.Min(scaleRows - 1, Mathf.FloorToInt(fv * scaleRows));
                    float rowFrac = fv * scaleRows - rowi;
                    float shift = (rowi & 1) == 0 ? 0f : 0.5f;
                    float around = phi / (Mathf.PI * 2f) * scaleAround + shift;
                    float aroundFrac = around - Mathf.Floor(around);
                    float cell = Mathf.Sin(Mathf.Clamp01(rowFrac) * Mathf.PI) * Mathf.Sin(Mathf.Clamp01(aroundFrac) * Mathf.PI);
                    float band = Mathf.Clamp01((fv - 0.18f) / 0.2f) * Mathf.Clamp01((0.92f - fv) / 0.2f);
                    float bump = radius * 0.26f * Mathf.Pow(Mathf.Clamp01(cell), 0.45f) * band;
                    float r = r0 + bump;
                    v.Add(new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r));
                    uv.Add(new Vector2(u * 2f, 1f - fv));
                }
            }
            for (int iv = 0; iv < sv; iv++) Strip(t, iv * cols, (iv + 1) * cols, cols);
            return Finish(v, uv, t, key, cols);
        });
    }

    /// Whole felt pine: `tiers` PineTier skirts stacked into one mesh (bottom tier radius = baseRadius,
    /// each higher tier ×0.78, odd tiers rotated half a scallop), overlapping by `overlap` of the lower
    /// tier's height. +y up, base at y = 0, top flat at y = height. ≈ 1.5k verts at defaults (4 tiers).
    public static Mesh PineTree(int tiers = 4, float baseRadius = 1f, float height = 2.6f, float overlap = 0.35f, int scallops = 7)
    {
        string key = $"PineTree_{tiers}_{baseRadius:0.###}_{height:0.###}_{overlap:0.###}_{scallops}";
        return Cached(key, () =>
        {
            int n = Mathf.Max(1, tiers);
            float ov = Mathf.Clamp(overlap, 0f, 0.9f);
            var unit = new float[n];
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                unit[i] = Mathf.Pow(0.78f, i * 0.6f);
                sum += i < n - 1 ? unit[i] * (1f - ov) : unit[i];
            }
            float scale = height / sum, y = 0f;
            var parts = new List<(Mesh mesh, Matrix4x4 xf)>(n);
            for (int i = 0; i < n; i++)
            {
                float r = baseRadius * Mathf.Pow(0.78f, i), th = unit[i] * scale;
                int su = Mathf.Clamp(Mathf.RoundToInt(48f * Mathf.Pow(r / baseRadius, 0.7f)), 16, 56);
                var tier = PineTier(r, r * 0.18f, th, scallops, 0.14f, 0.10f, su, 4);
                float yaw = (i & 1) == 1 ? 180f / Mathf.Max(1, scallops) : 0f;
                parts.Add((tier, Matrix4x4.TRS(Vector3.up * y, Quaternion.AngleAxis(yaw, Vector3.up), Vector3.one)));
                y += th * (1f - ov);
            }
            return Combine(parts, key);
        });
    }

    // 5. pompom / moss ball ----------------------------------------------------------------------

    /// Slightly irregular felt ball: UV sphere whose radius is modulated by ±fuzz of seamless hashed noise
    /// (`seed` picks the lump pattern). Centred at the origin. ≈ (segU+1)(segV+1) verts: 475 at defaults.
    public static Mesh Pompom(float radius = 1f, float fuzz = 0.06f, int segU = 24, int segV = 18, int seed = 1)
    {
        string key = $"Pompom_{radius:0.###}_{fuzz:0.###}_{segU}x{segV}_{seed}";
        return Cached(key, () =>
        {
            int su = Mathf.Max(8, segU), sv = Mathf.Max(6, segV), cols = su + 1;
            var v = new List<Vector3>(cols * (sv + 1));
            var uv = new List<Vector2>(cols * (sv + 1));
            var t = new List<int>(su * sv * 6);
            for (int iv = 0; iv <= sv; iv++)
            {
                float fv = iv / (float)sv, theta = fv * Mathf.PI;
                float st = Mathf.Sin(theta), ct = Mathf.Cos(theta);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su;
                    float phi = iu == su ? 0f : u * Mathf.PI * 2f; // identical seam direction → identical noise
                    var dir = new Vector3(Mathf.Cos(phi) * st, ct, Mathf.Sin(phi) * st);
                    v.Add(dir * (radius * (1f + fuzz * FuzzNoise(dir, seed))));
                    uv.Add(new Vector2(u, 1f - fv));
                }
            }
            for (int iv = 0; iv < sv; iv++) Strip(t, iv * cols, (iv + 1) * cols, cols);
            return Finish(v, uv, t, key, cols);
        });
    }

    // 6. grinder hopper bowl ---------------------------------------------------------------------

    /// Hopper bowl: lathe with a convex belly (bulge · radiusTop at mid-height), open top with a rounded
    /// lip of thickness `wall`, inner wall descending to a flat inner floor at ≈30 % height, closed flat
    /// bottom of radiusBottom. +y up, base at y = 0, rim at y = height. ≈ (segU+1)(rings+rings/2+7) verts: 627 at defaults.
    public static Mesh Bowl(float radiusTop = 1f, float radiusBottom = 0.55f, float height = 0.6f, float wall = 0.08f, float bulge = 0.12f, int segU = 32, int rings = 8)
    {
        string key = $"Bowl_{radiusTop:0.###}_{radiusBottom:0.###}_{height:0.###}_{wall:0.###}_{bulge:0.###}_{segU}x{rings}";
        return Cached(key, () =>
        {
            int nr = Mathf.Max(2, rings), ni = Mathf.Max(2, nr / 2);
            float w = Mathf.Clamp(wall, 0.001f, radiusBottom * 0.8f);
            float Outer(float f) => Mathf.Lerp(radiusBottom, radiusTop, Mathf.Pow(f, 0.7f)) + bulge * radiusTop * Mathf.Sin(f * Mathf.PI);
            float floorY = Mathf.Clamp(Mathf.Max(w, height * 0.3f), 0f, Mathf.Max(0f, height - w));
            float fFloor = height > 0f ? floorY / height : 0f;
            var p = new List<Prof>(nr + ni + 7);
            Pt(p, 0f, floorY);                                           // inner floor centre
            Crease(p, Outer(fFloor) - w, floorY);                        // floor edge
            for (int k = 1; k <= ni; k++)                                // inner wall, up
            {
                float f = Mathf.Lerp(fFloor, 1f, k / (float)ni);
                Pt(p, Outer(f) - w, f * height);
            }
            Pt(p, radiusTop - w * 0.5f, height + w * 0.35f);             // rounded lip
            for (int k = 0; k < nr; k++)                                 // outer wall, down
            {
                float f = 1f - k / (float)nr;
                Pt(p, Outer(f), f * height);
            }
            Crease(p, Outer(0f), 0f);                                    // bottom edge
            Pt(p, 0f, 0f);                                               // bottom centre
            return Lathe(p, segU, key);
        });
    }

    // 7. kraft arch bridge -----------------------------------------------------------------------

    /// Cardboard arch bridge deck. Spans x ∈ [-length/2, length/2], width along z (centred), +y up.
    /// Underside y = rise·sin(πt) (so both ends sit at y = 0), slab `thickness` thick, closed sides and ends.
    /// The deck is `planks` flat plank segments raised 0.12·thickness above the deck line with a narrow
    /// groove between neighbours. Side rails/posts are left to the caller. ≈ 600 verts at defaults.
    public static Mesh ArchBridge(float length = 3f, float width = 1f, float rise = 0.6f, float thickness = 0.18f, int segs = 24, int planks = 11)
    {
        string key = $"ArchBridge_{length:0.###}_{width:0.###}_{rise:0.###}_{thickness:0.###}_{segs}_{planks}";
        return Cached(key, () =>
        {
            int n = Mathf.Max(4, segs), np = Mathf.Max(1, planks);
            float hl = length * 0.5f, hw = width * 0.5f;
            float T(float x) => Mathf.Clamp01((x + hl) / length);
            float YBot(float x) => rise * Mathf.Sin(T(x) * Mathf.PI);
            float YTop(float x) => YBot(x) + thickness;
            var v = new List<Vector3>(640);
            var uv = new List<Vector2>(640);
            var t = new List<int>(1200);
            int Add(Vector3 p, Vector2 c) { v.Add(p); uv.Add(c); return v.Count - 1; }
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward)
            {
                int ia = Add(a, ua), ib = Add(b, ub), ic = Add(c, uc), id = Add(d, ud);
                OrientQuad(v, t, ia, ib, ic, id, outward);
            }
            // sides
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * hw;
                int start = v.Count;
                for (int i = 0; i <= n; i++)
                {
                    float x = -hl + length * i / n, fu = i / (float)n;
                    Add(new Vector3(x, YBot(x), z), new Vector2(fu, 0f));
                    Add(new Vector3(x, YTop(x), z), new Vector2(fu, 1f));
                }
                for (int i = 0; i < n; i++)
                {
                    int a = start + i * 2;
                    OrientQuad(v, t, a, a + 1, a + 3, a + 2, new Vector3(0f, 0f, side));
                }
            }
            // underside
            {
                int start = v.Count;
                for (int i = 0; i <= n; i++)
                {
                    float x = -hl + length * i / n, fu = i / (float)n;
                    Add(new Vector3(x, YBot(x), -hw), new Vector2(fu, 0f));
                    Add(new Vector3(x, YBot(x), hw), new Vector2(fu, 1f));
                }
                for (int i = 0; i < n; i++)
                {
                    int a = start + i * 2;
                    OrientQuad(v, t, a, a + 1, a + 3, a + 2, Vector3.down);
                }
            }
            // ends
            Quad(new Vector3(-hl, 0f, -hw), new Vector3(-hl, 0f, hw), new Vector3(-hl, thickness, hw), new Vector3(-hl, thickness, -hw),
                 new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector3.left);
            Quad(new Vector3(hl, 0f, -hw), new Vector3(hl, 0f, hw), new Vector3(hl, thickness, hw), new Vector3(hl, thickness, -hw),
                 new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector3.right);
            // deck: groove floors on the deck line, planks lifted by `step`
            float plankLen = length / np, g = plankLen * 0.10f, step = thickness * 0.12f;
            int m = Mathf.Max(1, Mathf.RoundToInt(n / (float)np));
            void DeckQuad(float xs, float xe, float lift)
            {
                Quad(new Vector3(xs, YTop(xs) + lift, -hw), new Vector3(xe, YTop(xe) + lift, -hw), new Vector3(xe, YTop(xe) + lift, hw), new Vector3(xs, YTop(xs) + lift, hw),
                     new Vector2(T(xs), 0f), new Vector2(T(xe), 0f), new Vector2(T(xe), 1f), new Vector2(T(xs), 1f), Vector3.up);
            }
            void EndWall(float x, float dir)
            {
                float y0 = YTop(x), y1 = y0 + step;
                Quad(new Vector3(x, y0, -hw), new Vector3(x, y1, -hw), new Vector3(x, y1, hw), new Vector3(x, y0, hw),
                     new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector3(dir, 0f, 0f));
            }
            void SideLip(float xs, float xe, float side)
            {
                float z = side * hw;
                Quad(new Vector3(xs, YTop(xs), z), new Vector3(xe, YTop(xe), z), new Vector3(xe, YTop(xe) + step, z), new Vector3(xs, YTop(xs) + step, z),
                     new Vector2(T(xs), 0f), new Vector2(T(xe), 0f), new Vector2(T(xe), 1f), new Vector2(T(xs), 1f), new Vector3(0f, 0f, side));
            }
            for (int p = 0; p < np; p++)
            {
                float x0 = -hl + p * plankLen, x1 = x0 + plankLen, xa = x0 + g, xb = x1 - g;
                DeckQuad(x0, xa, 0f);
                DeckQuad(xb, x1, 0f);
                EndWall(xa, -1f);
                EndWall(xb, 1f);
                for (int k = 0; k < m; k++)
                {
                    float xs = Mathf.Lerp(xa, xb, k / (float)m), xe = Mathf.Lerp(xa, xb, (k + 1) / (float)m);
                    DeckQuad(xs, xe, step);
                    SideLip(xs, xe, -1f);
                    SideLip(xs, xe, 1f);
                }
            }
            return Finish(v, uv, t, key);
        });
    }

    // 8. felt cloud ------------------------------------------------------------------------------

    /// Felt cloud: `lobes` low-poly (12×8) ellipsoids in a row along x, bigger in the middle, bottoms aligned
    /// at y = -height/2 (flat-ish underside), overall extent ≈ width × height × depth. Centred at the origin;
    /// `seed` jitters radii/positions. ≈ 117·lobes verts: 468 at defaults.
    public static Mesh Cloud(float width = 1f, float height = 0.55f, float depth = 0.6f, int lobes = 4, int seed = 1)
    {
        string key = $"Cloud_{width:0.###}_{height:0.###}_{depth:0.###}_{lobes}_{seed}";
        return Cached(key, () =>
        {
            int n = Mathf.Max(1, lobes);
            var s = new float[n];
            float maxS = 0f;
            for (int k = 0; k < n; k++)
            {
                float tk = (k + 0.5f) / n;
                s[k] = (0.72f + 0.28f * Mathf.Sin(tk * Mathf.PI)) * (1f + (Hash01(seed, k) - 0.5f) * 0.16f);
                maxS = Mathf.Max(maxS, s[k]);
            }
            const float ySquash = 0.82f;
            float baseR = height / (2f * ySquash * maxS);
            float spread = n > 1 ? Mathf.Max(0f, width - (s[0] + s[n - 1]) * baseR) / (1f - 1f / n) : 0f;
            var parts = new List<(Mesh mesh, Matrix4x4 xf)>(n);
            for (int k = 0; k < n; k++)
            {
                float tk = (k + 0.5f) / n;
                float rx = s[k] * baseR, ry = rx * ySquash, rz = depth * 0.5f * (0.8f + 0.2f * s[k] / maxS);
                var pos = new Vector3((tk - 0.5f) * spread,
                                      -height * 0.5f + ry + (Hash01(seed + 11, k) - 0.5f) * 0.04f * height,
                                      (Hash01(seed + 23, k) - 0.5f) * 0.10f * depth);
                parts.Add((Egg(rx, ry, rz, 0f, 12, 8), Matrix4x4.Translate(pos)));
            }
            return Combine(parts, key);
        });
    }

    // 9. + 10. succulent -------------------------------------------------------------------------

    /// Thin double-sided leaf: lanceolate outline (widest ≈ 1/3 up), base at the origin, grows along +y,
    /// cups toward +z and curls back toward -z by `curl` (fraction of a quarter turn reached at the tip).
    /// 2·3·(segs+1) verts: 54 at defaults.
    public static Mesh Leaf(float length = 1f, float width = 0.4f, float curl = 0.25f, int segs = 8)
    {
        string key = $"Leaf_{length:0.###}_{width:0.###}_{curl:0.###}_{segs}";
        return Cached(key, () =>
        {
            int n = Mathf.Max(2, segs);
            var centre = new Vector3[n + 1];
            var front = new Vector3[n + 1];
            float stepLen = length / n, bend = curl * Mathf.PI * 0.5f;
            Vector3 pos = Vector3.zero;
            centre[0] = pos;
            front[0] = Vector3.forward;
            for (int i = 1; i <= n; i++)
            {
                float am = bend * Mathf.Pow((i - 0.5f) / n, 1.6f);
                pos += new Vector3(0f, Mathf.Cos(am), -Mathf.Sin(am)) * stepLen;
                centre[i] = pos;
                float a = bend * Mathf.Pow(i / (float)n, 1.6f);
                front[i] = new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a));
            }
            var v = new List<Vector3>(6 * (n + 1));
            var uv = new List<Vector2>(6 * (n + 1));
            var t = new List<int>(24 * n);
            for (int side = 0; side < 2; side++) // 0 = front (+z cup side), 1 = back
            {
                int start = v.Count;
                for (int i = 0; i <= n; i++)
                {
                    float f = i / (float)n;
                    float hw = width * 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Pow(f, 0.75f))), 0.85f) + width * 0.06f * (1f - f);
                    for (int s = -1; s <= 1; s++)
                    {
                        v.Add(centre[i] + Vector3.right * (s * hw) + front[i] * (0.3f * hw * s * s));
                        uv.Add(new Vector2((s + 1) * 0.5f, f));
                    }
                }
                float sign = side == 0 ? 1f : -1f;
                for (int i = 0; i < n; i++)
                    for (int s = 0; s < 2; s++)
                    {
                        int a = start + i * 3 + s;
                        OrientQuad(v, t, a, a + 1, a + 4, a + 3, front[i] * sign);
                    }
            }
            return Finish(v, uv, t, key);
        });
    }

    /// Succulent rosette: `leaves` Leaf meshes in `whorls` rings around +y (inner whorl shorter and more
    /// upright, outer whorl longer, leaning out ~58° with drooping tips), merged into one mesh.
    /// Base at y = 0, footprint ≈ 2·radius. ≈ 54·leaves verts: 486 at defaults.
    public static Mesh Succulent(int leaves = 9, float radius = 1f, int whorls = 2)
    {
        string key = $"Succulent_{leaves}_{radius:0.###}_{whorls}";
        return Cached(key, () =>
        {
            int nw = Mathf.Clamp(whorls, 1, 6), nl = Mathf.Max(nw, leaves);
            int totalW = nw * (nw + 1) / 2, assigned = 0;
            var counts = new int[nw];
            for (int w = 0; w < nw; w++) { counts[w] = Mathf.Max(1, nl * (w + 1) / totalW); assigned += counts[w]; }
            counts[nw - 1] += Mathf.Max(0, nl - assigned);
            var parts = new List<(Mesh mesh, Matrix4x4 xf)>(nl);
            for (int w = 0; w < nw; w++)
            {
                float f = nw == 1 ? 1f : w / (float)(nw - 1); // 0 = innermost, 1 = outermost
                float len = radius * Mathf.Lerp(0.55f, 0.95f, f);
                var leaf = Leaf(len, len * 0.78f, Mathf.Lerp(0.06f, 0.18f, f), 8);
                float lean = Mathf.Lerp(24f, 46f, f), rOff = radius * Mathf.Lerp(0.04f, 0.16f, f), y0 = radius * 0.06f * (1f - f);
                for (int i = 0; i < counts[w]; i++)
                {
                    float yaw = 360f * i / counts[w] + w * (180f / counts[w]) + (Hash01(7, w * 31 + i) - 0.5f) * 10f;
                    var yawQ = Quaternion.AngleAxis(yaw, Vector3.up);
                    Vector3 outDir = yawQ * Vector3.back;                       // leaf back (-z) faces outward
                    var rot = yawQ * Quaternion.Euler(-lean, 0f, 0f);           // tilt +y toward the leaf's -z
                    parts.Add((leaf, Matrix4x4.TRS(outDir * rOff + Vector3.up * y0, rot, Vector3.one)));
                }
            }
            return Combine(parts, key);
        });
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
        if (mat.HasProperty("_FiberAmt") && (bump <= 0.001f || (tex != null && tex.name.Contains("burlap"))))
            mat.SetFloat("_FiberAmt", 0f);
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

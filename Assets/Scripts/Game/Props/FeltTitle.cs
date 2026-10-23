using System.Collections.Generic;
using UnityEngine;

/// Title plates: DriveLook/grok/title-letters-front.png, library-title-letters-1.jpg,
/// library-title-sign-1.jpg, detail-title-closer.png.
/// Pale cream stuffed felt. Flat fuzzy face, soft shoulder, dashed yarn stitch on the face.
/// ESPRESSO over ESCAPE, Casa Rústico in the same yarn with the acute accent.
/// The builder places the root. This file only builds the letters.
public static class FeltTitle
{
    const string YarnPlate = "Look/tex-yarn-fiber";
    const string CreamPlate = "Look/tex-felt-cream";

    // Metres at scale 1. Cap ink spans 2 * HalfH * Unit. Origin sits between ESPRESSO and ESCAPE.
    const float Unit = 0.48f;
    const float HalfH = 0.64f;
    const float Pitch = 1.46f;
    const float SubScale = 0.48f;
    const float DepthN = 0.22f;
    const float BevelN = 0.05f;
    const float StitchInN = 0.082f;
    const float ThreadN = 0.026f;
    const float DashN = 0.11f;
    const float GapN = 0.052f;
    const int BevelSteps = 4;

    struct Poly
    {
        public Vector2[] outer;
        public Vector2[] hole;
    }

    struct LetterMesh
    {
        public Mesh body;
        public Mesh stitch;
    }

    static readonly Dictionary<char, LetterMesh> Cache = new Dictionary<char, LetterMesh>();

    /// Root "Yarn Title". <paramref name="position"/> is world space.
    public static GameObject Build(Vector3 position, float scale = 1f)
    {
        return Build(null, position, scale);
    }

    /// Root "Yarn Title" under <paramref name="parent"/> (world space when parent is null).
    /// The face points -Z, toward the camera. The builder owns the root pose.
    public static GameObject Build(Transform parent, Vector3 localPosition, float scale = 1f)
    {
        var root = new GameObject("Yarn Title");
        if (parent != null) root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        root.isStatic = true;

        var cream = CreamYarn();
        var stitch = FuzzyStitch();
        Word(root.transform, "ESPRESSO", Pitch * 0.5f, 1f, 0, cream, stitch);
        Word(root.transform, "ESCAPE", -Pitch * 0.5f, 1f, 1, cream, stitch);
        // Same stuffed yarn, smaller, under ESCAPE. The acute sits on the u.
        float subY = -Pitch * 0.5f - HalfH - 0.30f - HalfH * SubScale;
        Word(root.transform, "Casa R\u00fastico", subY, SubScale, 2, cream, stitch);
        return root;
    }

    static void Word(Transform root, string word, float yNorm, float scale, int line, Material cream, Material stitch)
    {
        float y = yNorm * Unit;
        float x = -Width(word, scale) * 0.5f;
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            float adv = Cell(c) * scale * Unit;
            if (c != ' ')
                Place(root, c, new Vector3(x + adv * 0.5f, y, 0f), scale, line * 32 + i + 1, cream, stitch);
            x += adv;
        }
    }

    static float Width(string word, float scale)
    {
        float w = 0f;
        for (int i = 0; i < word.Length; i++) w += Cell(word[i]);
        return w * scale * Unit;
    }

    static float Cell(char c)
    {
        switch (c)
        {
            case ' ': return 0.40f;
            case 'i': return 0.42f;
            case 't': return 0.70f;
            case 'a':
            case 'c':
            case 'o':
            case 's':
            case 'u':
            case '\u00fa': return 0.88f;
            default: return 1.16f;
        }
    }

    static void Place(Transform root, char c, Vector3 local, float scale, int seed, Material cream, Material stitchMat)
    {
        if (!Meshes(c, out var body, out var stitch)) return;

        var letter = new GameObject("Yarn " + c);
        letter.transform.SetParent(root, false);
        letter.transform.localPosition = local + new Vector3(
            (Hash01(seed, 1) - 0.5f) * 0.008f * scale,
            (Hash01(seed, 2) - 0.5f) * 0.006f * scale,
            0f);
        letter.transform.localRotation = Quaternion.Euler(0f, 0f, (Hash01(seed, 3) - 0.5f) * 1.1f);
        letter.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        letter.isStatic = true;
        letter.AddComponent<MeshFilter>().sharedMesh = body;
        letter.AddComponent<MeshRenderer>().sharedMaterial = cream;

        var seam = new GameObject("Yarn Stitch");
        seam.transform.SetParent(letter.transform, false);
        seam.transform.localPosition = Vector3.zero;
        seam.transform.localRotation = Quaternion.identity;
        seam.transform.localScale = Vector3.one;
        seam.isStatic = true;
        seam.AddComponent<MeshFilter>().sharedMesh = stitch;
        seam.AddComponent<MeshRenderer>().sharedMaterial = stitchMat;
    }

    /// tex-felt-cream is the pale nap. Tint stays near the cloth so the letters do not blow out white.
    static Material CreamYarn()
    {
        var felt = Resources.Load<Texture2D>(CreamPlate);
        var yarn = Resources.Load<Texture2D>(YarnPlate);
        var tex = felt != null ? felt : yarn;
        var tint = felt != null
            ? new Color(1.00f, 0.985f, 0.95f)
            : new Color(1.62f, 1.90f, 2.35f);
        return Plate(tex, "Yarn Title Wool", tint, 1.10f, 0.04f, 0.84f, 0.97f, 0.05f, new Vector2(1.6f, 1.45f));
    }

    /// Same pale yarn, a hair lighter so the running stitch reads by relief.
    static Material FuzzyStitch()
    {
        var felt = Resources.Load<Texture2D>(CreamPlate);
        var yarn = Resources.Load<Texture2D>(YarnPlate);
        var tex = felt != null ? felt : yarn;
        var tint = felt != null
            ? new Color(1.03f, 1.00f, 0.96f)
            : new Color(1.70f, 1.98f, 2.40f);
        return Plate(tex, "Yarn Title Stitch", tint, 0.75f, 0.08f, 0.76f, 0.93f, 0.035f, new Vector2(2.2f, 1.8f));
    }

    static Material Plate(Texture2D tex, string matName, Color tint, float bump, float sheen, float wrap, float rough, float fiber, Vector2 tile)
    {
        string plate = tex != null ? "Look/" + tex.name : "flat";
        return CraftMesh.Shared(CraftMesh.Key(plate + "|" + matName, tint, tile, bump, sheen, wrap, rough, 0f), () =>
        {
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough, 0f);
            mat.name = matName;
            if (mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
            if (mat.HasProperty("_SeamAmt")) mat.SetFloat("_SeamAmt", 0f);
            return mat;
        });
    }

    static bool Meshes(char c, out Mesh body, out Mesh stitch)
    {
        body = null;
        stitch = null;
        if (Cache.TryGetValue(c, out var cached) && cached.body != null && cached.stitch != null)
        {
            body = cached.body;
            stitch = cached.stitch;
            return true;
        }

        var polys = Glyphs(c);
        if (polys == null || polys.Length == 0) return false;

        var bv = new List<Vector3>(2048);
        var bn = new List<Vector3>(2048);
        var bu = new List<Vector2>(2048);
        var bt = new List<int>(4096);
        var sv = new List<Vector3>(2048);
        var sn = new List<Vector3>(2048);
        var su = new List<Vector2>(2048);
        var st = new List<int>(4096);

        for (int i = 0; i < polys.Length; i++)
            AppendPatch(bv, bn, bu, bt, sv, sn, su, st, polys[i], c * 17 + i);

        if (bt.Count < 3 || st.Count < 3) return false;
        FaceOut(bv, bn, bt, 0);
        FaceOut(sv, sn, st, 0);
        body = Finish("Yarn " + c, bv, bn, bu, bt);
        stitch = Finish("Yarn Stitch " + c, sv, sn, su, st);
        Cache[c] = new LetterMesh { body = body, stitch = stitch };
        return true;
    }

    static void AppendPatch(List<Vector3> bv, List<Vector3> bn, List<Vector2> bu, List<int> bt, List<Vector3> sv, List<Vector3> sn, List<Vector2> su, List<int> st, Poly poly, int seed)
    {
        if (poly.outer == null || poly.outer.Length < 3) return;
        var outer = Nap(Clean(Ccw(new List<Vector2>(poly.outer))), seed);
        List<Vector2> hole = null;
        if (poly.hole != null && poly.hole.Length >= 3)
            hole = Nap(Clean(Ccw(new List<Vector2>(poly.hole))), seed + 5);
        if (outer.Count < 3) return;

        var faceO = Offset(outer, -BevelN);
        List<Vector2> faceH = hole == null ? null : Offset(hole, BevelN);
        AppendFace(bv, bn, bu, bt, faceO, faceH);
        AppendBevel(bv, bn, bu, bt, faceO, outer);
        if (hole != null && faceH != null) AppendBevel(bv, bn, bu, bt, faceH, hole);

        var stitchO = Offset(outer, -StitchInN);
        Dashes(sv, sn, su, st, stitchO, seed);
        if (hole != null)
        {
            var stitchH = Offset(hole, StitchInN);
            Dashes(sv, sn, su, st, stitchH, seed + 11);
        }
    }

    static void AppendFace(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> tris, List<Vector2> outer, List<Vector2> hole)
    {
        List<Vector2> ring = hole == null ? outer : Bridge(outer, hole);
        if (ring == null || ring.Count < 3) return;
        ring = Ccw(Clean(ring));
        var map = Ear(ring);
        if (map == null) return;
        int b = v.Count;
        float z = -DepthN * Unit;
        for (int i = 0; i < ring.Count; i++)
        {
            v.Add(new Vector3(ring[i].x * Unit, ring[i].y * Unit, z));
            nrm.Add(Vector3.back);
            uv.Add(new Vector2(ring[i].x * 0.85f + 0.5f, ring[i].y * 0.7f + 0.5f));
        }
        for (int i = 0; i < map.Count; i++) tris.Add(b + map[i]);
    }

    /// Quarter-round shoulder from the flat face out to the silhouette. Soft edge, not a tube.
    static void AppendBevel(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> tris, List<Vector2> face, List<Vector2> sil)
    {
        int n = face == null || sil == null ? 0 : Mathf.Min(face.Count, sil.Count);
        if (n < 3) return;
        int cols = n + 1;
        int rows = BevelSteps + 1;
        int b = v.Count;
        var outward = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 d = sil[i] - face[i];
            outward[i] = d.sqrMagnitude > 1e-8f ? d.normalized : Vector2.right;
        }
        for (int s = 0; s < rows; s++)
        {
            float ang = s / (float)BevelSteps * Mathf.PI * 0.5f;
            float side = Mathf.Sin(ang);
            float front = Mathf.Cos(ang);
            float z = -DepthN * Unit * front;
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                Vector2 p = Vector2.Lerp(face[k], sil[k], side);
                v.Add(new Vector3(p.x * Unit, p.y * Unit, z));
                Vector3 normal = new Vector3(outward[k].x * side, outward[k].y * side, -front);
                nrm.Add(normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.back);
                uv.Add(new Vector2(p.x * 0.85f + 0.5f, p.y * 0.7f + 0.5f));
            }
        }
        for (int s = 0; s < rows - 1; s++)
        {
            for (int i = 0; i < n; i++)
            {
                int i0 = b + s * cols + i;
                int i1 = i0 + 1;
                int i2 = i0 + cols;
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }
    }

    static void Dashes(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> tris, List<Vector2> loop, int seed)
    {
        int n = loop == null ? 0 : loop.Count;
        if (n < 3) return;
        var p = new List<Vector3>(n);
        var acc = new float[n + 1];
        float len = 0f;
        for (int i = 0; i < n; i++)
        {
            p.Add(new Vector3(loop[i].x * Unit, loop[i].y * Unit, 0f));
            acc[i] = len;
            len += Vector2.Distance(loop[i], loop[(i + 1) % n]) * Unit;
        }
        acc[n] = len;
        float dash = DashN * Unit;
        float gap = GapN * Unit;
        float step = dash + gap;
        if (len < step * 2f || dash < 1e-4f) return;
        int count = Mathf.Max(3, Mathf.FloorToInt(len / step));
        float use = len / count;
        float dLen = use * (dash / step);
        float rad = ThreadN * Unit;
        float z = -DepthN * Unit - rad * 0.42f;
        int phase = Mathf.Abs(seed) % count;
        for (int i = 0; i < count; i++)
        {
            float d0 = ((i + phase) % count) * use;
            float d1 = d0 + dLen;
            if (d1 <= len)
            {
                Pill(v, nrm, uv, tris, At(p, acc, len, d0, z), At(p, acc, len, d1, z), rad);
            }
            else
            {
                Pill(v, nrm, uv, tris, At(p, acc, len, d0, z), At(p, acc, len, len - 0.0001f, z), rad);
                Pill(v, nrm, uv, tris, At(p, acc, len, 0f, z), At(p, acc, len, d1 - len, z), rad);
            }
        }
    }

    static Vector3 At(List<Vector3> p, float[] acc, float len, float d, float z)
    {
        int n = p.Count;
        if (d <= 0f) return new Vector3(p[0].x, p[0].y, z);
        if (d >= len) return new Vector3(p[0].x, p[0].y, z);
        int i = 0;
        while (i < n - 1 && acc[i + 1] < d) i++;
        float span = Mathf.Max(1e-6f, acc[i + 1] - acc[i]);
        float t = (d - acc[i]) / span;
        Vector3 q = Vector3.Lerp(p[i], p[(i + 1) % n], t);
        q.z = z;
        return q;
    }

    static void Pill(List<Vector3> v, List<Vector3> nrm, List<Vector2> uv, List<int> tris, Vector3 a, Vector3 b, float rad)
    {
        Vector3 axis = b - a;
        float span = axis.magnitude;
        if (span < 1e-5f || rad < 1e-5f) return;
        axis /= span;
        Vector3 up = Vector3.back;
        Vector3 side = Vector3.Cross(up, axis);
        if (side.sqrMagnitude < 1e-8f)
        {
            up = Vector3.up;
            side = Vector3.Cross(up, axis);
        }
        side.Normalize();
        up = Vector3.Cross(axis, side).normalized;
        if (Vector3.Dot(up, Vector3.back) < 0f)
        {
            side = -side;
            up = -up;
        }

        const int sides = 6;
        const int rings = 3;
        int cols = sides + 1;
        int bse = v.Count;
        for (int r = 0; r < rings; r++)
        {
            Vector3 c = Vector3.Lerp(a, b, r / (float)(rings - 1));
            for (int k = 0; k <= sides; k++)
            {
                float ang = k / (float)sides * Mathf.PI * 2f;
                Vector3 n = side * Mathf.Cos(ang) + up * Mathf.Sin(ang);
                v.Add(c + n * rad);
                nrm.Add(n);
                uv.Add(new Vector2(k / (float)sides, r / (float)(rings - 1)));
            }
        }
        for (int r = 0; r < rings - 1; r++)
        {
            for (int k = 0; k < sides; k++)
            {
                int i0 = bse + r * cols + k;
                int i1 = i0 + 1;
                int i2 = i0 + cols;
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i1); tris.Add(i3); tris.Add(i2);
            }
        }
        int poleA = v.Count;
        v.Add(a - axis * rad);
        nrm.Add(-axis);
        uv.Add(new Vector2(0.5f, 0f));
        for (int k = 0; k < sides; k++)
        {
            tris.Add(poleA);
            tris.Add(bse + k + 1);
            tris.Add(bse + k);
        }
        int poleB = v.Count;
        int end = bse + (rings - 1) * cols;
        v.Add(b + axis * rad);
        nrm.Add(axis);
        uv.Add(new Vector2(0.5f, 1f));
        for (int k = 0; k < sides; k++)
        {
            tris.Add(poleB);
            tris.Add(end + k);
            tris.Add(end + k + 1);
        }
    }

    static Poly[] Glyphs(char c)
    {
        switch (c)
        {
            case 'E': return new[] { Block(EOutline(), null, 0.15f) };
            case 'S': return new[] { StrokePoly(SCenter(-0.50f, 0.52f), 0.14f) };
            case 'P': return BowlLetter(false);
            case 'R': return BowlLetter(true);
            case 'O': return new[] { Ring(0f, 0f, 0.48f, 0.62f, 0.17f, 0.30f) };
            case 'A': return new[] { Block(AOutline(), AHole(), 0.10f) };
            case 'C': return new[] { StrokePoly(Arc(0.02f, 0f, 0.32f, 0.48f, 48f, 312f, 16), 0.15f) };
            case 'a': return new[] { ALower() };
            case 's': return new[] { StrokePoly(SCenter(-0.62f, 0.02f), 0.125f) };
            case 'c': return new[] { StrokePoly(Arc(0.02f, -0.30f, 0.24f, 0.28f, 50f, 310f, 14), 0.12f) };
            case 'o': return new[] { Ring(0f, -0.30f, 0.32f, 0.32f, 0.13f, 0.14f) };
            case 'u': return new[] { UBowl() };
            case '\u00fa': return new[] { UBowl(), Accent() };
            case 't': return new[] { Block(TOutline(), null, 0.07f) };
            case 'i': return new[] { Block(IStem(), null, 0.08f), Disk(0f, 0.40f, 0.105f, 0.09f) };
            default: return null;
        }
    }

    static Poly Block(Vector2[] corners, Vector2[] hole, float fillet)
    {
        var outer = corners == null ? null : Fillet(corners, fillet, true);
        var h = hole == null ? null : Fillet(hole, Mathf.Min(0.06f, fillet), true);
        return new Poly { outer = outer, hole = h };
    }

    static Poly StrokePoly(Vector2[] center, float half)
    {
        var line = Resample(center, false, 0.07f);
        return new Poly { outer = Stroke(line, half).ToArray(), hole = null };
    }

    static Poly Ring(float cx, float cy, float rx, float ry, float hx, float hy)
    {
        return new Poly
        {
            outer = Ellipse(cx, cy, rx, ry, 28),
            hole = Ellipse(cx, cy, hx, hy, 18)
        };
    }

    static Poly Disk(float cx, float cy, float rx, float ry)
    {
        return new Poly { outer = Ellipse(cx, cy, rx, ry, 14), hole = null };
    }

    static Vector2[] EOutline()
    {
        float L = -0.50f, R = 0.50f, B = -HalfH, T = HalfH;
        return new[]
        {
            P(L, B), P(R, B), P(R, -0.34f),
            P(-0.18f, -0.34f), P(-0.18f, -0.15f),
            P(0.20f, -0.15f), P(0.20f, 0.15f),
            P(-0.18f, 0.15f), P(-0.18f, 0.34f),
            P(R, 0.34f), P(R, T),
            P(L, T)
        };
    }

    static Vector2[] AOutline()
    {
        float B = -HalfH, T = HalfH;
        return new[]
        {
            P(-0.50f, B), P(-0.18f, B),
            P(-0.14f, -0.20f), P(0.14f, -0.20f),
            P(0.18f, B), P(0.50f, B),
            P(0.15f, T), P(-0.15f, T)
        };
    }

    static Vector2[] AHole()
    {
        return new[] { P(-0.12f, 0.06f), P(0.12f, 0.06f), P(0f, 0.46f) };
    }

    static Poly[] BowlLetter(bool leg)
    {
        float L = -0.50f, SR = -0.18f, B = -HalfH, T = HalfH;
        float cx = 0.06f, cy = 0.18f, rad = 0.44f;
        float dx = SR - cx;
        float rise = Mathf.Sqrt(Mathf.Max(0.02f, rad * rad - dx * dx));
        float yUp = cy + rise;
        float yDn = cy - rise;
        float aUp = Mathf.Atan2(yUp - cy, dx) * Mathf.Rad2Deg;
        float aDn = Mathf.Atan2(yDn - cy, dx) * Mathf.Rad2Deg;
        var pts = new List<Vector2>();
        pts.Add(P(L, B));
        pts.Add(P(SR, B));
        if (leg)
        {
            // Stem up to the bowl, along the bowl bottom, then the diagonal leg, then the outer bowl.
            pts.Add(P(SR, yDn));
            AddArc(pts, cx, cy, rad, aDn, -50f, 8);
            pts.Add(P(0.14f, B));
            pts.Add(P(0.50f, B));
            AddArc(pts, cx, cy, rad, -14f, aUp, 12);
        }
        else
        {
            AddArc(pts, cx, cy, rad, aDn, aUp, 16);
        }
        if (T - yUp > 0.03f) pts.Add(P(SR, T));
        pts.Add(P(L, T));
        var outer = Fillet(pts.ToArray(), 0.09f, true);
        var hole = Ellipse(0.12f, 0.24f, leg ? 0.13f : 0.15f, leg ? 0.14f : 0.16f, 16);
        return new[] { new Poly { outer = outer, hole = hole } };
    }

    static Poly ALower()
    {
        float cx = -0.08f, cy = -0.32f, rad = 0.26f;
        float x0 = 0.04f, x1 = 0.34f, y0 = -HalfH, y1 = 0.12f;
        float dx = x0 - cx;
        float rise = Mathf.Sqrt(Mathf.Max(0.01f, rad * rad - dx * dx));
        float yHi = cy + rise;
        float yLo = cy - rise;
        float aHi = Mathf.Atan2(yHi - cy, dx) * Mathf.Rad2Deg;
        float aLo = Mathf.Atan2(yLo - cy, dx) * Mathf.Rad2Deg;
        if (aLo < 0f) aLo += 360f;
        var pts = new List<Vector2>();
        pts.Add(P(x0, y0));
        pts.Add(P(x1, y0));
        pts.Add(P(x1, y1));
        pts.Add(P(x0, y1));
        pts.Add(P(x0, yHi));
        AddArc(pts, cx, cy, rad, aHi, aLo, 16);
        pts.Add(P(x0, y0));
        var outer = Fillet(Clean(pts).ToArray(), 0.07f, true);
        var hole = Ellipse(cx - 0.02f, cy, 0.09f, 0.10f, 12);
        return new Poly { outer = outer, hole = hole };
    }

    static Poly UBowl()
    {
        var corners = Fillet(new[]
        {
            P(-0.24f, 0.04f), P(-0.24f, -0.50f), P(0.24f, -0.50f), P(0.24f, 0.04f)
        }, 0.20f, false);
        var line = Resample(corners, false, 0.06f);
        return new Poly { outer = Stroke(line, 0.13f).ToArray(), hole = null };
    }

    static Poly Accent()
    {
        var line = new List<Vector2> { P(-0.02f, 0.26f), P(0.20f, 0.46f) };
        return new Poly { outer = Stroke(line, 0.075f).ToArray(), hole = null };
    }

    static Vector2[] TOutline()
    {
        return new[]
        {
            P(-0.12f, -HalfH), P(0.12f, -HalfH), P(0.12f, -0.02f),
            P(0.30f, -0.02f), P(0.30f, 0.16f), P(0.12f, 0.16f),
            P(0.12f, 0.30f), P(-0.12f, 0.30f), P(-0.12f, 0.16f),
            P(-0.30f, 0.16f), P(-0.30f, -0.02f), P(-0.12f, -0.02f)
        };
    }

    static Vector2[] IStem()
    {
        return new[]
        {
            P(-0.11f, -HalfH), P(0.11f, -HalfH), P(0.11f, 0.06f), P(-0.11f, 0.06f)
        };
    }

    static Vector2[] SCenter(float y0, float y1)
    {
        float dy = Mathf.Max(0.2f, y1 - y0);
        float Y(float t) { return y0 + dy * t; }
        return new[]
        {
            P(0.12f, Y(0.74f)),
            P(0.28f, Y(0.90f)),
            P(0.02f, Y(1f)),
            P(-0.24f, Y(0.86f)),
            P(-0.28f, Y(0.64f)),
            P(-0.06f, Y(0.50f)),
            P(0.12f, Y(0.46f)),
            P(0.28f, Y(0.30f)),
            P(0.16f, Y(0.10f)),
            P(-0.04f, Y(0f)),
            P(-0.28f, Y(0.12f)),
            P(-0.12f, Y(0.28f))
        };
    }

    static void AddArc(List<Vector2> dst, float cx, float cy, float rad, float a0, float a1, int n)
    {
        var arc = Arc(cx, cy, rad, rad, a0, a1, n);
        for (int i = 0; i < arc.Length; i++) Add(dst, arc[i]);
    }

    static List<Vector2> Nap(List<Vector2> src, int seed)
    {
        int n = src.Count;
        var dst = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = src[(i + n - 1) % n];
            Vector2 next = src[(i + 1) % n];
            Vector2 t = next - prev;
            if (t.sqrMagnitude < 1e-8f)
            {
                dst.Add(src[i]);
                continue;
            }
            t.Normalize();
            Vector2 outward = new Vector2(t.y, -t.x);
            float w = Mathf.Sin(i * 0.65f + (seed % 11) * 0.4f);
            w += 0.45f * Mathf.Sin(i * 0.27f + 1.3f);
            dst.Add(src[i] + outward * (w * 0.0045f));
        }
        return dst;
    }

    static List<Vector2> Offset(List<Vector2> src, float dist)
    {
        int n = src.Count;
        var dst = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = src[(i + n - 1) % n];
            Vector2 c = src[i];
            Vector2 next = src[(i + 1) % n];
            Vector2 e0 = c - prev;
            Vector2 e1 = next - c;
            float l0 = e0.magnitude;
            float l1 = e1.magnitude;
            if (l0 < 1e-5f || l1 < 1e-5f)
            {
                dst.Add(c);
                continue;
            }
            e0 /= l0;
            e1 /= l1;
            Vector2 n0 = new Vector2(e0.y, -e0.x);
            Vector2 n1 = new Vector2(e1.y, -e1.x);
            Vector2 m = n0 + n1;
            if (m.sqrMagnitude < 1e-8f)
            {
                dst.Add(c + n0 * dist);
                continue;
            }
            m.Normalize();
            float denom = Vector2.Dot(m, n0);
            float scale = Mathf.Abs(denom) < 0.3f ? dist / (denom < 0f ? -0.3f : 0.3f) : dist / denom;
            float lim = Mathf.Abs(dist) * 2.4f;
            scale = Mathf.Clamp(scale, -lim, lim);
            dst.Add(c + m * scale);
        }
        return dst;
    }

    static List<Vector2> Stroke(List<Vector2> center, float half)
    {
        int n = center.Count;
        var dst = new List<Vector2>();
        if (n < 2 || half <= 0f) return dst;
        var left = new Vector2[n];
        var right = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 tan = Tangent2(center, i);
            Vector2 side = new Vector2(-tan.y, tan.x);
            left[i] = center[i] + side * half;
            right[i] = center[i] - side * half;
        }
        for (int i = 0; i < n; i++) Add(dst, left[i]);
        Cap(dst, center[n - 1], Tangent2(center, n - 1), half);
        for (int i = n - 1; i >= 0; i--) Add(dst, right[i]);
        Vector2 back = Tangent2(center, 0) * -1f;
        Cap(dst, center[0], back, half);
        return dst;
    }

    static void Cap(List<Vector2> dst, Vector2 c, Vector2 dir, float half)
    {
        if (dir.sqrMagnitude < 1e-8f) return;
        dir.Normalize();
        Vector2 left = new Vector2(-dir.y, dir.x);
        for (int k = 1; k < 6; k++)
        {
            float a = k / 6f * Mathf.PI;
            Add(dst, c + left * (Mathf.Cos(a) * half) + dir * (Mathf.Sin(a) * half));
        }
    }

    static Vector2 Tangent2(List<Vector2> p, int i)
    {
        int n = p.Count;
        int i0 = Mathf.Max(0, i - 1);
        int i1 = Mathf.Min(n - 1, i + 1);
        Vector2 d = p[i1] - p[i0];
        if (d.sqrMagnitude < 1e-8f) return Vector2.up;
        return d.normalized;
    }

    static List<Vector2> Bridge(List<Vector2> outer, List<Vector2> hole)
    {
        if (outer == null || hole == null || outer.Count < 3 || hole.Count < 3) return null;
        outer = Ccw(new List<Vector2>(outer));
        hole = Ccw(new List<Vector2>(hole));
        hole.Reverse();
        int hi = 0;
        for (int i = 1; i < hole.Count; i++)
            if (hole[i].x > hole[hi].x) hi = i;
        Vector2 h = hole[hi];
        int best = -1;
        float bestD = 1e9f;
        for (int i = 0; i < outer.Count; i++)
        {
            if (outer[i].x + 1e-4f < h.x) continue;
            if (!BridgeOk(outer, hole, h, outer[i])) continue;
            float d = (outer[i] - h).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = i;
            }
        }
        if (best < 0)
        {
            for (int i = 0; i < outer.Count; i++)
            {
                float d = (outer[i] - h).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
        }
        if (best < 0) return null;
        var ring = new List<Vector2>();
        for (int i = 0; i <= best; i++) ring.Add(outer[i]);
        for (int k = 0; k <= hole.Count; k++) ring.Add(hole[(hi + k) % hole.Count]);
        ring.Add(outer[best]);
        for (int i = best + 1; i < outer.Count; i++) ring.Add(outer[i]);
        return ring;
    }

    static bool BridgeOk(List<Vector2> outer, List<Vector2> hole, Vector2 a, Vector2 b)
    {
        for (int s = 1; s <= 3; s++)
        {
            Vector2 q = Vector2.Lerp(a, b, s / 4f);
            if (!Inside(outer, q) || Inside(hole, q)) return false;
        }
        return true;
    }

    static bool Inside(List<Vector2> p, Vector2 q)
    {
        bool c = false;
        int n = p.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((p[i].y > q.y) != (p[j].y > q.y) &&
                q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / ((p[j].y - p[i].y) + 1e-12f) + p[i].x)
                c = !c;
        }
        return c;
    }

    static List<int> Ear(List<Vector2> pts)
    {
        int n0 = pts.Count;
        if (n0 < 3) return null;
        if (Mathf.Abs(Area(pts)) < 1e-5f) return null;
        var id = new List<int>(n0);
        for (int i = 0; i < n0; i++) id.Add(i);
        var tris = new List<int>((n0 - 2) * 3);
        int guard = 0;
        while (id.Count > 3 && guard++ < n0 * n0)
        {
            int n = id.Count;
            bool cut = false;
            for (int i = 0; i < n; i++)
            {
                int i0 = (i - 1 + n) % n;
                int i2 = (i + 1) % n;
                Vector2 a = pts[id[i0]];
                Vector2 b = pts[id[i]];
                Vector2 c = pts[id[i2]];
                if (Cross(b - a, c - b) <= 1e-7f) continue;
                bool ear = true;
                for (int k = 0; k < n; k++)
                {
                    if (k == i0 || k == i || k == i2) continue;
                    if (InTri(pts[id[k]], a, b, c))
                    {
                        ear = false;
                        break;
                    }
                }
                if (!ear) continue;
                tris.Add(id[i0]);
                tris.Add(id[i]);
                tris.Add(id[i2]);
                id.RemoveAt(i);
                cut = true;
                break;
            }
            if (!cut) return null;
        }
        if (id.Count == 3)
        {
            tris.Add(id[0]);
            tris.Add(id[1]);
            tris.Add(id[2]);
        }
        return tris.Count >= 3 ? tris : null;
    }

    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);
        const float e = 1e-6f;
        return (c1 > e && c2 > e && c3 > e) || (c1 < -e && c2 < -e && c3 < -e);
    }

    static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static float Area(List<Vector2> p)
    {
        float a = 0f;
        int n = p.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 q = p[(i + 1) % n];
            a += p[i].x * q.y - q.x * p[i].y;
        }
        return a * 0.5f;
    }

    static List<Vector2> Ccw(List<Vector2> p)
    {
        if (Area(p) < 0f) p.Reverse();
        return p;
    }

    static List<Vector2> Clean(List<Vector2> src)
    {
        var dst = new List<Vector2>(src.Count);
        for (int i = 0; i < src.Count; i++) Add(dst, src[i]);
        if (dst.Count > 2 && (dst[0] - dst[dst.Count - 1]).sqrMagnitude < 1e-8f)
            dst.RemoveAt(dst.Count - 1);
        return dst;
    }

    static Mesh Finish(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(v);
        mesh.SetNormals(n);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Vector2 P(float x, float y)
    {
        return new Vector2(x, y);
    }

    static Vector2[] Ellipse(float cx, float cy, float rx, float ry, int n)
    {
        n = Mathf.Max(8, n);
        var p = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            p[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        return p;
    }

    static Vector2[] Arc(float cx, float cy, float rx, float ry, float a0, float a1, int n)
    {
        n = Mathf.Max(2, n);
        var p = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)(n - 1)) * Mathf.Deg2Rad;
            p[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
        }
        return p;
    }

    static Vector2[] Fillet(Vector2[] corners, float radius, bool closed)
    {
        int n = corners == null ? 0 : corners.Length;
        if (n < 3 || radius <= 0.0001f) return corners;
        var dst = new List<Vector2>(n * 6);
        for (int i = 0; i < n; i++)
        {
            bool end = !closed && (i == 0 || i == n - 1);
            if (end)
            {
                Add(dst, corners[i]);
                continue;
            }
            int ip = (i - 1 + n) % n;
            int inx = (i + 1) % n;
            AddFillet(dst, corners[ip], corners[i], corners[inx], radius);
        }
        return dst.Count >= 2 ? dst.ToArray() : corners;
    }

    static void AddFillet(List<Vector2> dst, Vector2 prev, Vector2 c, Vector2 next, float radius)
    {
        Vector2 inV = c - prev;
        Vector2 outV = next - c;
        float len0 = inV.magnitude;
        float len1 = outV.magnitude;
        if (len0 < 1e-5f || len1 < 1e-5f)
        {
            Add(dst, c);
            return;
        }
        Vector2 inDir = inV / len0;
        Vector2 outDir = outV / len1;
        float cross = inDir.x * outDir.y - inDir.y * outDir.x;
        float dot = Mathf.Clamp(Vector2.Dot(inDir, outDir), -1f, 1f);
        float phi = Mathf.Atan2(cross, dot);
        float absPhi = Mathf.Abs(phi);
        if (absPhi < 0.08f || absPhi > 2.95f)
        {
            Add(dst, c);
            return;
        }
        float half = absPhi * 0.5f;
        float tanHalf = Mathf.Tan(half);
        if (tanHalf < 1e-4f)
        {
            Add(dst, c);
            return;
        }
        float d = radius * tanHalf;
        float dMax = Mathf.Min(len0, len1) * 0.46f;
        if (d > dMax) d = dMax;
        float r = d / tanHalf;
        Vector2 t0 = c - inDir * d;
        Vector2 t1 = c + outDir * d;
        Vector2 left = new Vector2(-inDir.y, inDir.x);
        float side = phi >= 0f ? 1f : -1f;
        Vector2 center = t0 + left * side * r;
        float a0 = Mathf.Atan2(t0.y - center.y, t0.x - center.x);
        float a1 = Mathf.Atan2(t1.y - center.y, t1.x - center.x);
        float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        if (Mathf.Abs(Mathf.Abs(sweep) - absPhi) > 0.45f)
        {
            Add(dst, c);
            return;
        }
        int steps = Mathf.Clamp(Mathf.CeilToInt(absPhi / (16f * Mathf.Deg2Rad)), 2, 8);
        for (int k = 0; k <= steps; k++)
        {
            float a = a0 + sweep * (k / (float)steps);
            Add(dst, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
    }

    static void Add(List<Vector2> dst, Vector2 p)
    {
        if (dst.Count > 0 && (dst[dst.Count - 1] - p).sqrMagnitude < 1e-8f) return;
        dst.Add(p);
    }

    static List<Vector2> Resample(Vector2[] src, bool closed, float step)
    {
        var clean = new List<Vector2>();
        if (src != null)
        {
            for (int i = 0; i < src.Length; i++)
            {
                if (clean.Count == 0 || (clean[clean.Count - 1] - src[i]).sqrMagnitude > 1e-8f)
                    clean.Add(src[i]);
            }
        }
        if (closed && clean.Count > 2 && (clean[0] - clean[clean.Count - 1]).sqrMagnitude < 1e-8f)
            clean.RemoveAt(clean.Count - 1);

        var dst = new List<Vector2>();
        int n = clean.Count;
        if (n == 0) return dst;
        if (n == 1)
        {
            dst.Add(clean[0]);
            return dst;
        }

        int seg = closed ? n : n - 1;
        var acc = new float[seg + 1];
        float len = 0f;
        for (int i = 0; i < seg; i++)
        {
            acc[i] = len;
            len += Vector2.Distance(clean[i], clean[(i + 1) % n]);
        }
        acc[seg] = len;
        if (len < 1e-5f)
        {
            dst.Add(clean[0]);
            return dst;
        }

        float stride = Mathf.Max(0.02f, step);
        int count = Mathf.Max(closed ? 8 : 2, Mathf.RoundToInt(len / stride));
        int samples = closed ? count : count + 1;
        for (int k = 0; k < samples; k++)
        {
            float d = (k / (float)count) * len;
            dst.Add(Along(clean, acc, seg, d));
        }
        return dst;
    }

    static Vector2 Along(List<Vector2> p, float[] acc, int seg, float d)
    {
        int n = p.Count;
        if (d <= 0f) return p[0];
        if (d >= acc[seg]) return seg == n ? p[0] : p[n - 1];
        int i = 0;
        while (i < seg - 1 && acc[i + 1] < d) i++;
        float span = Mathf.Max(1e-6f, acc[i + 1] - acc[i]);
        float t = (d - acc[i]) / span;
        return Vector2.Lerp(p[i], p[(i + 1) % n], t);
    }

    static void FaceOut(List<Vector3> v, List<Vector3> nrm, List<int> tris, int start)
    {
        for (int i = start; i + 2 < tris.Count; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];
            if (a >= v.Count || b >= v.Count || c >= v.Count) continue;
            Vector3 face = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            Vector3 want = nrm[a] + nrm[b] + nrm[c];
            if (Vector3.Dot(face, want) < 0f)
            {
                tris[i + 1] = c;
                tris[i + 2] = b;
            }
        }
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

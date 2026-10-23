using System.Collections.Generic;
using UnityEngine;

/// Wool read of the existing Imagine Bean.
/// Call FeltBeanSurface.Apply on that root.
/// rotate-bean.mp4, the turn (side ~11s, back ~8s, front it returns to ~14s):
/// one cream two-ply yarn over the crown, round black bead eyes, a thin smile, short wool.
/// The opening seconds still show the portrait's dashed stitch. The turn replaces it.
/// The body mesh stays on its MeshFilter. The capsule, mass, and BeanImpulse stay as authored.
public static class FeltBeanSurface
{
    const string ShellName = "Fuzz Shell";

    // Metres. Short even nap: 0.055 read as thorns, and the clip's halo is only a soft edge.
    const float BodyOutline = 0.026f;
    const float BodyFace = 0.011f;
    const float LimbOutline = 0.010f;
    // play139 beads are about a sixth of the body width, with a small catchlight.
    const float EyeDiameter = 0.090f;

    // Mid brown felt. White (1,1,1) bleached the wool whenever the source tint was missing.
    static readonly Color WoolBrown = new Color(0.62f, 0.40f, 0.26f);
    static readonly Color SeamTan = new Color(0.93f, 0.84f, 0.66f);
    static readonly Color EyeBlack = new Color(0.015f, 0.012f, 0.010f);
    static readonly Color Catchlight = new Color(0.98f, 0.97f, 0.94f);

    const float SeamH = 0.78f, SeamWR = 0.80f, SeamDR = 0.72f, SeamTop = 0.11f, SeamFlat = 0.07f;

    /// <summary>
    /// Short wool, a two-ply cream cord over the crown, and round black bead eyes.
    /// </summary>
    public static void Apply(GameObject root)
    {
        if (root == null) return;
        Transform body = EnsureBody(root.transform);
        QuietFillers(root.transform);
        DressWool(root.transform, body);

        var filter = body != null ? body.GetComponent<MeshFilter>() : null;
        Mesh source = filter != null ? filter.sharedMesh : null;
        if (body == null || source == null) return;

        var verts = source.vertices;
        var normals = source.normals;
        if (normals == null || normals.Length != verts.Length)
            normals = RadialNormals(verts);

        RaiseSeam(root.transform, body);
        EnlargeEyes(root.transform, body, verts, normals);
        ClosedSmile(root.transform, body, verts, normals);
    }

    static void DressWool(Transform root, Transform body)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.gameObject.name == ShellName) continue;
            string name = renderer.gameObject.name;
            bool isBody = body != null && renderer.transform == body;
            if (!isBody && name != "Arm" && name != "Foot") continue;

            TuneWool(renderer);
            AddShell(renderer.gameObject, isBody ? BodyOutline : LimbOutline, isBody ? BodyFace : LimbOutline, isBody);
        }
    }

    static void TuneWool(MeshRenderer renderer)
    {
        var material = renderer.sharedMaterial;
        if (material == null || (!material.HasProperty("_BumpScale") && !material.HasProperty("_BaseColor")))
        {
            renderer.sharedMaterial = WoolMaterial(material);
            return;
        }

        // rotate-bean.mp4 wool is matte. The rim stays soft and the cord stays the light yarn.
        SetFloat(material, "_BumpScale", 2.2f);
        SetFloat(material, "_Sheen", 0.12f);
        SetFloat(material, "_Wrap", 0.90f);
        SetFloat(material, "_Smoothness", 0.02f);
        SetFloat(material, "_Glossiness", 0.02f);
        SetFloat(material, "_FiberAmt", 0.20f);
        SetFloat(material, "_SeamAmt", 0f);
    }

    /// The turn's seam is one cream cord on the front and the back. The portrait dashes stay off.
    static void RaiseSeam(Transform root, Transform body)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform dash = root.GetChild(i);
            if (dash.name != "Dash Stitch") continue;
            var dashRenderer = dash.GetComponent<MeshRenderer>();
            if (dashRenderer != null) dashRenderer.enabled = false;
        }

        Transform stripe = body.Find("Yarn Seam");
        GameObject go;
        if (stripe == null)
        {
            go = new GameObject("Yarn Seam");
            go.transform.SetParent(body, false);
            stripe = go.transform;
        }
        else go = stripe.gameObject;

        stripe.localPosition = Vector3.zero;
        stripe.localRotation = Quaternion.identity;
        stripe.localScale = Vector3.one;

        var filter = go.GetComponent<MeshFilter>();
        if (filter == null) filter = go.AddComponent<MeshFilter>();
        Mesh previous = filter.sharedMesh;
        filter.sharedMesh = YarnCord();
        if (previous != null && previous.name == "Yarn Cord")
        {
            if (Application.isPlaying) Object.Destroy(previous);
            else Object.DestroyImmediate(previous);
        }

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SeamMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Vector3 SurfacePoint(float lat, float lon, float lift)
    {
        Vector3 p = CraftMesh.BeanBodyPoint(SeamH, SeamWR, SeamDR, SeamTop, SeamFlat, lat, lon);
        Vector3 a = CraftMesh.BeanBodyPoint(SeamH, SeamWR, SeamDR, SeamTop, SeamFlat, lat + 3f, lon);
        Vector3 b = CraftMesh.BeanBodyPoint(SeamH, SeamWR, SeamDR, SeamTop, SeamFlat, lat, lon + 4f);
        Vector3 n = Vector3.Cross(a - p, b - p);
        if (n.sqrMagnitude < 1e-8f) n = p.sqrMagnitude > 1e-8f ? p : Vector3.up;
        n.Normalize();
        if (Vector3.Dot(n, p) < 0f) n = -n;
        return p + n * lift;
    }

    static void PushCenter(List<Vector3> pts, Vector3 p)
    {
        if (pts.Count > 0 && (pts[pts.Count - 1] - p).sqrMagnitude < 0.00012f) return;
        pts.Add(p);
    }

    /// Front, over the crown, and down the back. One centre line for both plies.
    static List<Vector3> CordCenters()
    {
        var pts = new List<Vector3>(80);
        for (int i = 0; i <= 34; i++)
        {
            float lat = Mathf.Lerp(-46f, 88f, i / 34f);
            PushCenter(pts, SurfacePoint(lat, 0f, 0.014f));
        }
        for (int i = 0; i <= 36; i++)
        {
            float lat = Mathf.Lerp(88f, -50f, i / 36f);
            float lift = Mathf.Lerp(0.016f, 0.030f, i / 36f);
            PushCenter(pts, SurfacePoint(lat, 180f, lift));
        }
        return pts;
    }

    static List<Vector3> Ply(List<Vector3> centers, float phase)
    {
        var pts = new List<Vector3>(centers.Count);
        const float orbit = 0.0062f;
        for (int i = 0; i < centers.Count; i++)
        {
            Vector3 prev = centers[Mathf.Max(0, i - 1)];
            Vector3 next = centers[Mathf.Min(centers.Count - 1, i + 1)];
            Vector3 tan = next - prev;
            if (tan.sqrMagnitude < 1e-8f) tan = Vector3.up;
            tan.Normalize();
            Vector3 n = centers[i].sqrMagnitude > 1e-8f ? centers[i].normalized : Vector3.up;
            Vector3 side = Vector3.Cross(tan, n);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tan, Vector3.right);
            side.Normalize();
            Vector3 bin = Vector3.Cross(side, tan).normalized;
            float a = i * 1.15f + phase;
            pts.Add(centers[i] + (side * Mathf.Cos(a) + bin * Mathf.Sin(a)) * orbit);
        }
        return pts;
    }

    static Mesh YarnCord()
    {
        var centers = CordCenters();
        var v = new List<Vector3>(centers.Count * 16);
        var uv = new List<Vector2>(centers.Count * 16);
        var tris = new List<int>(centers.Count * 36);
        AppendTube(v, uv, tris, Ply(centers, 0f), 0.0068f);
        AppendTube(v, uv, tris, Ply(centers, Mathf.PI), 0.0068f);
        var mesh = new Mesh { name = "Yarn Cord" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AppendTube(List<Vector3> v, List<Vector2> uv, List<int> tris, List<Vector3> pts, float radius)
    {
        const int sides = 6;
        int count = pts.Count;
        if (count < 2) return;
        int cols = sides + 1;
        int b = v.Count;
        for (int i = 0; i < count; i++)
        {
            Vector3 prev = pts[Mathf.Max(0, i - 1)];
            Vector3 next = pts[Mathf.Min(count - 1, i + 1)];
            Vector3 tan = next - prev;
            if (tan.sqrMagnitude < 1e-8f) tan = Vector3.up;
            tan.Normalize();
            Vector3 side = Vector3.Cross(tan, Vector3.forward);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tan, Vector3.up);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, tan).normalized;
            float rad = radius * (0.9f + 0.1f * Mathf.Sin(i * 1.7f));
            for (int s = 0; s <= sides; s++)
            {
                float a = s / (float)sides * Mathf.PI * 2f;
                v.Add(pts[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rad);
                uv.Add(new Vector2(s / (float)sides, i / (float)(count - 1)));
            }
        }
        for (int i = 0; i < count - 1; i++)
        {
            for (int s = 0; s < sides; s++)
            {
                int i0 = b + i * cols + s, i1 = i0 + 1, i2 = i0 + cols, i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }
    }

    static void EnlargeEyes(Transform root, Transform body, Vector3[] verts, Vector3[] normals)
    {
        var eyes = new List<Transform>();
        var lights = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == "Eye") eyes.Add(child);
            else if (child.name == "Eye Highlight") lights.Add(child);
        }

        float radius = EyeDiameter * 0.5f;
        float lift = radius * 0.28f;
        for (int i = 0; i < eyes.Count; i++)
        {
            Transform eye = eyes[i];
            eye.localScale = Vector3.one * EyeDiameter;
            var eyeRenderer = eye.GetComponent<MeshRenderer>();
            if (eyeRenderer != null)
            {
                eyeRenderer.sharedMaterial = EyeBead();
                eyeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            bool seated = OnSurface(body, verts, normals, eye.position, lift, out Vector3 pos, out Vector3 normal);
            if (seated) eye.position = pos;

            if (i >= lights.Count) continue;
            Transform hl = lights[i];
            hl.localScale = Vector3.one * (EyeDiameter * 0.18f);
            var hlRenderer = hl.GetComponent<MeshRenderer>();
            if (hlRenderer != null)
            {
                hlRenderer.sharedMaterial = EyeHighlight();
                hlRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (!seated) continue;
            // Upper right of each eye from the camera, not a mirrored pair.
            Vector3 view = normal + Vector3.up * 0.50f + Vector3.right * 0.38f;
            if (view.sqrMagnitude < 1e-8f) view = normal;
            view.Normalize();
            hl.position = pos + view * (radius * 0.98f);
        }
    }

    /// Thin shallow curve just under the eyes, in front of the yarn.
    static void ClosedSmile(Transform root, Transform body, Vector3[] verts, Vector3[] normals)
    {
        var eyes = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == "Eye") eyes.Add(child);
        }

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(root.forward, up);
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
        right.Normalize();

        Vector3 mid = root.position + up * 0.28f + root.forward * 0.15f;
        if (eyes.Count >= 2) mid = (eyes[0].position + eyes[1].position) * 0.5f;
        else if (eyes.Count == 1) mid = eyes[0].position;

        const float half = 0.052f;
        const float sag = 0.014f;
        const float radius = 0.0048f;
        Vector3 center = mid - up * (EyeDiameter * 0.55f + 0.016f);
        var points = new List<Vector3>();
        const int count = 9;
        for (int i = 0; i < count; i++)
        {
            float u = i / (float)(count - 1) * 2f - 1f;
            Vector3 probe = center + right * (u * half) - up * (sag * (1f - u * u));
            if (OnSurface(body, verts, normals, probe, 0.036f, out Vector3 pos, out _))
                points.Add(pos);
        }
        if (points.Count < 3) return;

        Transform smile = root.Find("Smile");
        GameObject go;
        if (smile == null)
        {
            go = new GameObject("Smile");
            go.transform.SetParent(root, false);
            smile = go.transform;
        }
        else go = smile.gameObject;

        smile.localPosition = Vector3.zero;
        smile.localRotation = Quaternion.identity;
        smile.localScale = Vector3.one;

        var filter = go.GetComponent<MeshFilter>();
        if (filter == null) filter = go.AddComponent<MeshFilter>();
        Mesh previous = filter.sharedMesh;
        filter.sharedMesh = SmileArc(points, root, radius);
        if (previous != null && previous.name == "Closed Smile")
        {
            if (Application.isPlaying) Object.Destroy(previous);
            else Object.DestroyImmediate(previous);
        }

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SmileMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        HideExtraSmiles(root, smile);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
    }

    static Mesh SmileArc(List<Vector3> world, Transform space, float radius)
    {
        const int sides = 6;
        int count = world.Count;
        int cols = sides + 1;
        var v = new List<Vector3>(count * cols);
        var uv = new List<Vector2>(count * cols);
        var tris = new List<int>((count - 1) * sides * 6);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = space.InverseTransformPoint(world[i]);
            Vector3 prev = space.InverseTransformPoint(world[Mathf.Max(0, i - 1)]);
            Vector3 next = space.InverseTransformPoint(world[Mathf.Min(count - 1, i + 1)]);
            Vector3 tan = next - prev;
            if (tan.sqrMagnitude < 1e-8f) tan = Vector3.up;
            tan.Normalize();
            Vector3 side = Vector3.Cross(tan, Vector3.forward);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tan, Vector3.up);
            side.Normalize();
            Vector3 bin = Vector3.Cross(side, tan).normalized;
            for (int k = 0; k <= sides; k++)
            {
                float a = k / (float)sides * Mathf.PI * 2f;
                v.Add(p + (side * Mathf.Cos(a) + bin * Mathf.Sin(a)) * radius);
                uv.Add(new Vector2(k / (float)sides, i / (float)Mathf.Max(1, count - 1)));
            }
        }
        for (int i = 0; i < count - 1; i++)
        {
            for (int k = 0; k < sides; k++)
            {
                int i0 = i * cols + k;
                int i1 = i0 + 1;
                int i2 = i0 + cols;
                int i3 = i2 + 1;
                tris.Add(i0); tris.Add(i2); tris.Add(i1);
                tris.Add(i1); tris.Add(i2); tris.Add(i3);
            }
        }
        var mesh = new Mesh { name = "Closed Smile" };
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// Puts a point back on the bean along its current direction, then lifts it on the outward normal.
    /// Reads the body mesh. Does not write it.
    static bool OnSurface(Transform body, Vector3[] verts, Vector3[] normals, Vector3 worldPoint, float lift, out Vector3 worldPos, out Vector3 worldNormal)
    {
        worldPos = worldPoint;
        worldNormal = Vector3.forward;
        Vector3 dir = body.InverseTransformPoint(worldPoint);
        if (dir.sqrMagnitude < 1e-8f) return false;
        dir.Normalize();

        int best = -1;
        float bestDot = -2f;
        for (int i = 0; i < verts.Length; i++)
        {
            if (verts[i].sqrMagnitude < 1e-8f) continue;
            float dot = Vector3.Dot(verts[i].normalized, dir);
            if (dot <= bestDot) continue;
            bestDot = dot;
            best = i;
        }
        if (best < 0 || bestDot < 0.2f) return false;

        float along = Vector3.Dot(verts[best], dir);
        if (along < 0.02f) along = verts[best].magnitude;
        Vector3 n = best < normals.Length && normals[best].sqrMagnitude > 1e-8f
            ? normals[best].normalized
            : verts[best].normalized;

        worldPos = body.TransformPoint(dir * along + n * lift);
        worldNormal = body.TransformDirection(n).normalized;
        return true;
    }

    static Vector3[] RadialNormals(Vector3[] verts)
    {
        var normals = new Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
            normals[i] = verts[i].sqrMagnitude > 1e-8f ? verts[i].normalized : Vector3.forward;
        return normals;
    }

    static Material EyeBead()
    {
        return CraftMesh.Shared("bean-eye-bead", () => CraftMesh.Bead(EyeBlack, 0.98f));
    }

    static Material EyeHighlight()
    {
        return CraftMesh.Shared("bean-eye-highlight", () => CraftMesh.Bead(Catchlight, 0.45f));
    }

    static void AddShell(GameObject host, float outline, float faceHold, bool face)
    {
        var hostFilter = host.GetComponent<MeshFilter>();
        var hostRenderer = host.GetComponent<MeshRenderer>();
        if (hostFilter == null || hostFilter.sharedMesh == null || hostRenderer == null) return;

        // The body mesh is 40 x 28. A copy of it stays a smooth egg. The halo
        // uses a denser bean of the same profile so the fibers can break the edge.
        Mesh source = face
            ? CraftMesh.BeanBody(SeamH, SeamWR, SeamDR, SeamTop, SeamFlat, 96, 64)
            : hostFilter.sharedMesh;
        Transform child = host.transform.Find(ShellName);
        GameObject shell;
        if (child == null)
        {
            shell = new GameObject(ShellName);
            shell.transform.SetParent(host.transform, false);
            shell.layer = host.layer;
            shell.AddComponent<MeshFilter>();
            var created = shell.AddComponent<MeshRenderer>();
            created.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            created.receiveShadows = true;
        }
        else shell = child.gameObject;

        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one;

        var filter = shell.GetComponent<MeshFilter>();
        if (filter == null) filter = shell.AddComponent<MeshFilter>();
        Mesh previous = filter.sharedMesh;
        Mesh nap = CopyNap(source, outline, faceHold, face);
        filter.sharedMesh = nap;
        if (previous != null && previous != source && previous != nap)
        {
            if (Application.isPlaying) Object.Destroy(previous);
            else Object.DestroyImmediate(previous);
        }

        var renderer = shell.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = shell.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = WoolMaterial(hostRenderer.sharedMaterial);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var collider = shell.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }
    }

    /// Displaced copy. The source mesh is only read.
    static Mesh CopyNap(Mesh source, float outline, float faceHold, bool face)
    {
        var mesh = new Mesh { name = ShellName, indexFormat = source.indexFormat };
        mesh.vertices = source.vertices;
        mesh.normals = source.normals;
        mesh.uv = source.uv;
        mesh.tangents = source.tangents;
        mesh.triangles = source.triangles;

        var original = mesh.vertices;
        var verts = (Vector3[])original.Clone();
        var normals = mesh.normals;
        if (normals == null || normals.Length != verts.Length)
        {
            mesh.RecalculateNormals();
            normals = mesh.normals;
        }

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = normals[i];
            if (n.sqrMagnitude < 1e-8f)
                n = verts[i].sqrMagnitude > 1e-8f ? verts[i].normalized : Vector3.up;
            else
                n.Normalize();
            verts[i] += n * Nap(original[i], n, outline, faceHold, face);
        }

        mesh.vertices = verts;
        Weld(mesh, original, true);
        mesh.RecalculateNormals();
        Weld(mesh, original, false);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static float Nap(Vector3 position, Vector3 normal, float outline, float faceHold, bool face)
    {
        float cover = 0f;
        if (face)
        {
            float t = Mathf.Clamp01(normal.z / 0.35f);
            cover = t * t * (3f - 2f * t);
        }
        float n = WoolClump(position);
        float fiber = Mathf.Clamp01(n * 0.5f + 0.5f);
        float nap = Mathf.Lerp(outline * 0.8f, outline, fiber * fiber);
        return Mathf.Lerp(nap, faceHold, cover);
    }

    static float WoolClump(Vector3 position)
    {
        Vector3 d = position.sqrMagnitude > 1e-8f ? position.normalized : Vector3.up;
        float n = Mathf.Sin(Vector3.Dot(d, new Vector3(14f, 20f, 11f)));
        n += Mathf.Sin(Vector3.Dot(d, new Vector3(-18f, 12f, 22f)) + 1.3f) * 0.55f;
        n += Mathf.Sin(Vector3.Dot(d, new Vector3(22f, -9f, 16f)) + 0.4f) * 0.35f;
        return n / 2.05f;
    }

    static void Weld(Mesh mesh, Vector3[] original, bool positions)
    {
        var verts = mesh.vertices;
        var normals = mesh.normals;
        int countVerts = verts.Length;
        var seen = new bool[countVerts];
        var group = new int[countVerts];
        for (int i = 0; i < countVerts; i++)
        {
            if (seen[i]) continue;
            int count = 0;
            group[count++] = i;
            seen[i] = true;
            for (int j = i + 1; j < countVerts; j++)
            {
                if (seen[j]) continue;
                if ((original[i] - original[j]).sqrMagnitude > 1e-10f) continue;
                seen[j] = true;
                group[count++] = j;
            }
            if (count == 1) continue;

            if (positions)
            {
                Vector3 sum = Vector3.zero;
                for (int k = 0; k < count; k++) sum += verts[group[k]];
                Vector3 avg = sum / count;
                for (int k = 0; k < count; k++) verts[group[k]] = avg;
            }
            else
            {
                Vector3 sum = Vector3.zero;
                for (int k = 0; k < count; k++) sum += normals[group[k]];
                Vector3 avg = sum.sqrMagnitude > 1e-8f ? sum.normalized : Vector3.up;
                for (int k = 0; k < count; k++) normals[group[k]] = avg;
            }
        }
        if (positions) mesh.vertices = verts;
        else mesh.normals = normals;
    }

    static Material WoolMaterial(Material source)
    {
        Color tint = EvenBrown(source);
        Texture2D tex = null;
        if (source != null)
        {
            if (source.HasProperty("_BaseMap")) tex = source.GetTexture("_BaseMap") as Texture2D;
            if (tex == null && source.HasProperty("_MainTex")) tex = source.GetTexture("_MainTex") as Texture2D;
        }
        if (tex == null) tex = Resources.Load<Texture2D>("Look/tex-felt-brown");

        Texture2D captured = tex;
        Color capturedTint = tint;
        Color halo = new Color(
            Mathf.Clamp(capturedTint.r * 1.06f, 0f, 1.8f),
            Mathf.Clamp(capturedTint.g * 1.04f, 0f, 1.8f),
            Mathf.Clamp(capturedTint.b * 1.02f, 0f, 1.8f),
            1f);
        return CraftMesh.Shared("bean-wool-halo", () =>
        {
            var material = CraftMesh.Felt(captured, halo, new Vector2(1.8f, 1.8f), 2.8f, 0.40f, 0.92f, 0.98f, 0f);
            var soft = Shader.Find("Escape/PineWool");
            if (soft != null) material.shader = soft;
            SetFloat(material, "_FiberAmt", 0.16f);
            SetFloat(material, "_SeamAmt", 0f);
            return material;
        });
    }

    static Material SmileMaterial()
    {
        return CraftMesh.Shared("bean-closed-smile", () =>
        {
            var material = CraftMesh.Felt(null, new Color(0.03f, 0.02f, 0.015f), Vector2.one, 0.4f, 0.04f, 0.55f, 0.96f, 0f);
            SetFloat(material, "_FiberAmt", 0.02f);
            SetFloat(material, "_SeamAmt", 0f);
            return material;
        });
    }

    static Material SeamMaterial()
    {
        return CraftMesh.Shared("bean-raised-seam", () =>
        {
            var material = CraftMesh.Felt(null, SeamTan, Vector2.one, 0.9f, 0.12f, 0.55f, 0.88f, 0f);
            SetFloat(material, "_FiberAmt", 0.12f);
            SetFloat(material, "_SeamAmt", 0f);
            return material;
        });
    }

    static Color EvenBrown(Material source)
    {
        Color color = WoolBrown;
        if (source != null && source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
        else if (source != null && source.HasProperty("_Color")) color = source.color;

        color.a = 1f;
        float peak = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        if (peak < 0.2f || peak > 0.92f) return WoolBrown;
        if (color.r > color.g + 0.14f && color.b > color.g + 0.02f) return WoolBrown;
        color.r = Mathf.Clamp(color.r, 0f, 1f);
        color.g = Mathf.Clamp(color.g, 0f, 1f);
        color.b = Mathf.Clamp(color.b, 0f, 1f);
        return color;
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property)) material.SetFloat(property, value);
    }

    static Transform EnsureBody(Transform root)
    {
        Transform body = root.Find("Bean Body");
        if (body != null)
        {
            var ready = body.GetComponent<MeshFilter>();
            if (ready != null && ready.sharedMesh != null) return body;
        }

        if (body == null)
        {
            var go = new GameObject("Bean Body");
            go.transform.SetParent(root, false);
            go.layer = root.gameObject.layer;
            body = go.transform;
        }

        var filter = body.GetComponent<MeshFilter>();
        if (filter == null) filter = body.gameObject.AddComponent<MeshFilter>();
        if (filter.sharedMesh == null)
            filter.sharedMesh = CraftMesh.BeanBody(SeamH, SeamWR, SeamDR, SeamTop, SeamFlat, 40, 28);

        var renderer = body.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = body.gameObject.AddComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            Material donor = DonorMaterial(root);
            renderer.sharedMaterial = donor != null ? new Material(donor) : WoolMaterial(null);
        }
        renderer.enabled = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        return body;
    }

    static Material DonorMaterial(Transform root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null) continue;
            string name = renderer.gameObject.name;
            if (name == "Bean Body" || name == ShellName || name == "Yarn Seam") continue;
            if (name == "Eye" || name == "Eye Highlight" || name == "Dash Stitch" || name == "Smile") continue;
            if (name.StartsWith("Arm") || name.StartsWith("Foot") || name.StartsWith("Leg")) continue;
            return renderer.sharedMaterial;
        }
        return null;
    }

    /// The clip is one oval. The old cleft lobes, trench, and flecks stay in the scene and stop drawing.
    static void QuietFillers(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!IsFiller(child.name)) continue;
            var renderers = child.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
                if (renderers[r] != null) renderers[r].enabled = false;
        }
    }

    static bool IsFiller(string name)
    {
        if (name == "Bean Body" || name == "Smile" || name == ShellName || name == "Yarn Seam") return false;
        if (name == "Eye" || name == "Eye Highlight" || name == "Dash Stitch") return false;
        if (name.StartsWith("Arm") || name.StartsWith("Foot") || name.StartsWith("Leg")) return false;
        if (name.StartsWith("Bean ") || name.StartsWith("Felt ") || name.StartsWith("Face ")) return true;
        if (name.StartsWith("Pad ") || name.StartsWith("Cheek ") || name.StartsWith("Crown ")) return true;
        if (name.StartsWith("Seam ") || name == "Crease" || name == "Valley Recess") return true;
        return false;
    }

    static void HideExtraSmiles(Transform root, Transform keep)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name != "Smile" || child == keep) continue;
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }
}

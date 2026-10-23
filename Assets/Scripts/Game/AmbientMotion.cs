using System.Collections.Generic;
using UnityEngine;

/// Set-dressing motion read off the Drive clips. gen-2d7dcad9.mp4: felt leaves drift off the red / flower
/// trees (≈0.5 m/s, ±0.2 m sway at 0.5 Hz, slow spin) and settle on the burlap; the hanging yarn balls swing a
/// degree or two; the clouds barely drift on the twine. Purely cosmetic: no colliders, no Rigidbodies, nothing
/// on the Imagine Bean. Attached once to the Backdrop root by BuildEscapeScene.
public class AmbientMotion : MonoBehaviour
{
    [Header("Leaves (gen-2d7dcad9 2.7–5.5 s)")]
    public float leafFallSpeed = 0.5f;
    public float leafSway = 0.2f;
    public float leafSwayHz = 0.5f;
    public float leafSpinDegPerSec = 70f;
    public float leafEverySeconds = 1.4f;
    public int maxLeaves = 12;
    public float leafRestSeconds = 9f;
    public float sourceRange = 7f;        // only shed from trees near the camera

    [Header("Hanging yarn")]
    public float yarnSwingDeg = 2.2f;
    public float yarnSwingHz = 0.22f;

    [Header("Clouds on the twine")]
    public float cloudDrift = 0.05f;
    public float cloudDriftHz = 0.06f;
    public float cloudRockDeg = 1.2f;

    class Leaf
    {
        public Transform t;
        public float x0, z0, phase, spin, floorY, restAt;
        public bool resting;
        public Quaternion restRot;
    }

    readonly List<Leaf> _leaves = new List<Leaf>();
    readonly List<Transform> _sources = new List<Transform>();
    readonly List<Transform> _cloudHits = new List<Transform>(8);
    readonly List<Transform> _hangHits = new List<Transform>(8);
    readonly List<(Transform t, Vector3 p, float ph)> _clouds = new List<(Transform, Vector3, float)>();
    readonly List<(Transform t, Quaternion r, float ph)> _hangs = new List<(Transform, Quaternion, float)>();
    Material[] _leafMats;
    Camera _cam;
    float _nextLeaf, _nextScan;

    void Start()
    {
        _cam = Camera.main;
        var tile = Vector2.one;
        _leafMats = new[]
        {
            LeafMat(new Color(1.00f, 0.98f, 0.96f), tile),
            LeafMat(new Color(1.08f, 0.82f, 0.55f), tile), // orange
            LeafMat(new Color(0.92f, 0.86f, 0.60f), tile)  // ochre
        };
        Scan();
        _nextLeaf = Time.time + 2.0f;
    }

    /// Shared with the rest of the felt kit so a scene reload does not leak three extra materials.
    static Material LeafMat(Color tint, Vector2 tile)
    {
        const string tex = "Look/tex-felt-red";
        string key = CraftMesh.Key(tex, tint, tile, 0.8f, 0.10f, 0.6f, 0.95f, 0f);
        return CraftMesh.Shared(key, () =>
        {
            var red = Resources.Load<Texture2D>(tex);
            return CraftMesh.Felt(red, tint, tile, 0.8f, 0.10f, 0.6f, 0.95f);
        });
    }

    void Scan()
    {
        _sources.Clear();
        _clouds.Clear();
        _hangs.Clear();
        _cloudHits.Clear();
        _hangHits.Clear();
        SetNameQuery.Collect("Red Pompom", _sources, "Flower Petal", _sources, "Cloud", _cloudHits, "Hang Pivot", _hangHits);
        for (int i = 0; i < _cloudHits.Count; i++)
        {
            var t = _cloudHits[i];
            _clouds.Add((t, t.localPosition, Random.value * 6.2832f));
        }
        for (int i = 0; i < _hangHits.Count; i++)
        {
            var t = _hangHits[i];
            _hangs.Add((t, t.localRotation, Random.value * 6.2832f));
        }
        _nextScan = Time.time + 6f;
    }

    void Update()
    {
        float t = Time.time, dt = Time.deltaTime;
        if (t >= _nextScan) Scan();

        // clouds: a few centimetres of drift and a slow rock, never in step with each other
        for (int i = 0; i < _clouds.Count; i++)
        {
            var c = _clouds[i];
            if (c.t == null) continue;
            float w = t * cloudDriftHz * 6.2832f + c.ph;
            c.t.localPosition = c.p + new Vector3(Mathf.Sin(w) * cloudDrift, Mathf.Sin(w * 1.7f) * cloudDrift * 0.3f, 0f);
            c.t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(w * 0.8f) * cloudRockDeg);
        }

        // hanging yarn: pendulum about the twine
        for (int i = 0; i < _hangs.Count; i++)
        {
            var h = _hangs[i];
            if (h.t == null) continue;
            float w = t * yarnSwingHz * 6.2832f + h.ph;
            h.t.localRotation = h.r * Quaternion.Euler(Mathf.Sin(w * 0.6f) * yarnSwingDeg * 0.4f, 0f, Mathf.Sin(w) * yarnSwingDeg);
        }

        // leaves
        if (t >= _nextLeaf && _cam != null)
        {
            _nextLeaf = t + leafEverySeconds * Random.Range(0.7f, 1.4f);
            Shed();
        }
        for (int i = _leaves.Count - 1; i >= 0; i--)
        {
            var l = _leaves[i];
            if (l.t == null) { _leaves.RemoveAt(i); continue; }
            if (l.resting)
            {
                if (t >= l.restAt + leafRestSeconds) { Destroy(l.t.gameObject); _leaves.RemoveAt(i); }
                continue;
            }
            var p = l.t.position;
            p.y -= leafFallSpeed * dt;
            float w = t * leafSwayHz * 6.2832f + l.phase;
            p.x = l.x0 + Mathf.Sin(w) * leafSway;
            p.z = l.z0 + Mathf.Sin(w * 0.6f + 1.3f) * leafSway * 0.25f;
            l.t.position = p;
            // a felt disc rocks as it falls, then lies flat
            l.t.rotation = Quaternion.Euler(Mathf.Cos(w) * 28f, l.spin * t, Mathf.Sin(w) * 22f);
            if (p.y <= l.floorY)
            {
                if (l.floorY < -1f) { Destroy(l.t.gameObject); _leaves.RemoveAt(i); continue; } // fell into a gap
                p.y = l.floorY;
                l.t.position = p;
                l.t.rotation = Quaternion.Euler(Random.Range(-6f, 6f), l.spin * t, Random.Range(-6f, 6f));
                l.resting = true;
                l.restAt = t;
            }
        }
    }

    void Shed()
    {
        if (_leaves.Count >= maxLeaves || _sources.Count == 0) return;
        // pick a source near the camera's x so the fall is on screen
        float cx = _cam.transform.position.x;
        Transform src = null;
        for (int tries = 0; tries < 6 && src == null; tries++)
        {
            var cand = _sources[Random.Range(0, _sources.Count)];
            if (cand != null && Mathf.Abs(cand.position.x - cx) < sourceRange) src = cand;
        }
        if (src == null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Felt Leaf";
        Destroy(go.GetComponent<Collider>());
        CraftMesh.UseLowSphere(go);
        float s = Random.Range(0.055f, 0.085f);
        go.transform.localScale = new Vector3(s, s * 0.22f, s * 0.72f);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = _leafMats[Random.Range(0, _leafMats.Length)];
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var p = src.position + new Vector3(Random.Range(-0.12f, 0.12f), -0.08f, Random.Range(-0.08f, 0.08f));
        go.transform.position = p; // stays at scene root: the Backdrop this script lives on follows the camera

        bool overGap = LevelRunner.IsOverGap(p.x);
        _leaves.Add(new Leaf
        {
            t = go.transform, x0 = p.x, z0 = p.z, phase = Random.value * 6.2832f,
            spin = leafSpinDegPerSec * (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.6f, 1.2f),
            floorY = overGap ? -6f : 0.012f + s * 0.11f
        });
    }
}

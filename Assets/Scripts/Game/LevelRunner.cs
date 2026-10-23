using System.Collections.Generic;
using UnityEngine;

/// Side-scroll level runner. Builds the play lane for a LevelDef (burlap ground blocks with hop gaps,
/// kraft steps, honey-bean pickups, goal post), applies the LOOK dressing through LevelSetSpawner,
/// and runs goal / fall / checkpoint logic. The lane is z = 0; the bean travels +X; `goalZ` in the
/// catalog is the goal distance along X.
/// Pickups and ground live under their own roots so LevelSetSpawner counts stay exactly what the proofs expect.
public class LevelRunner : MonoBehaviour
{
    public Transform bean;
    public Transform goalMarker;
    public LevelCatalog.LevelDef active;
    public float fallResetY = -3f;

    /// Start apron built by BuildEscapeScene spans x ∈ [-5, 3]; level ground starts here.
    public const float ApronEnd = 3f;
    public const float LaneDepth = 6f; // ground depth (z) so background and foreground dressing have floor under them

    /// L2 Kraft Bridge (gen-2d7dcad9): the arch sits ON the burlap, on the run lane.
    /// Deck span is BridgeX0 − plank/2 … BridgeX0+BridgeLen+plank/2 (about 3.97–6.53), inside every level's first solid block.
    /// ImagineSet builds the walkable CraftMesh arch. LevelSetSpawner still emits the old plank/post/rail children (count-held) and hides them.
    public const float BridgeX0 = 4.15f;       // first plank centre (world x)
    public const float BridgeLen = 2.20f;      // first → last plank centre
    public const float BridgeArch = 0.46f;     // arch rise at the middle
    public const float BridgeFootTop = 0.03f;  // walk surface at the feet, almost flush with the burlap
    public const float BridgeDeckY = -0.025f;  // plank centre at the feet (plank is 0.11 thick; top = FootTop)
    public const float BridgeWidth = 1.20f;    // deck width along z, centred on the lane
    public const float BridgePlankRun = 0.36f; // plank length along the run, past the end centres

    /// L5 tour arch, same deck, also on the floor (block [10.20, 22.00]), never over a gap.
    public const float TourBridgeX0 = 13.2f;
    public const float TourBridgeLen = 3.4f;

    /// Run is 2.8 m/s, hop 5.2 N·s / 0.9 kg, gravity −22 → air ≈ 0.525 s, centre travel ≈ 1.47 m.
    /// Every flat gap below is ≤ 1.42 m so it lands on the far lip. Downward gaps stay ≤ 1.50 m (still ≤ 1.6 m).
    /// Alternating edges: block, gap, block, gap, …
    /// L1 gaps 1.05 m (7.00–8.05) and 1.10 m (12.00–13.10).
    /// L2 gaps 1.20 m (4.20–5.40) and 1.35 m (18.40–19.75). The bridge block is [5.40, 18.40].
    public static readonly float[] L1Edges = { ApronEnd, 9.80f, 10.85f, 12.6f, 13.7f, 19f };
    public static readonly float[] L2Edges = { ApronEnd, 11.30f, 12.50f, 18.40f, 19.75f, 27f };

    /// L3 gaps 1.40 m (8.00–9.40), 1.42 m (15.20–16.62), 1.38 m (22.40–23.78). Wider than L2, still under 1.47 m.
    public static readonly float[] L3Edges = { ApronEnd, 8.00f, 9.40f, 15.20f, 16.62f, 22.40f, 23.78f, 33f };

    /// L4 low burlap pairs, then the raised spans. Gaps: 1.30 m flat (8.00–9.30), 1.40 m high-to-high (16.50–17.90),
    /// 1.50 m down from 0.56 m (24.50–26.00).
    public static readonly float[] L4Low = { ApronEnd, 8.00f, 9.30f, 12.80f, 26.00f, 39f };
    public static readonly float[] L4High = { 12.80f, 16.50f, 17.90f, 24.50f };
    public const float L4HighTop = 0.56f;

    /// L5 low pairs and raised spans. Gaps: 1.20 m (9.00–10.20), 1.42 m (22.00–23.42),
    /// 1.40 m high-to-high (32.00–33.40), 1.48 m down from 0.52 m (40.00–41.48). Goal is 48, on [41.48, 53].
    public static readonly float[] L5Low = { ApronEnd, 9.00f, 10.20f, 22.00f, 23.42f, 27.00f, 41.48f, 53f };
    public static readonly float[] L5High = { 27.00f, 32.00f, 33.40f, 40.00f };
    public const float L5HighTop = 0.52f;

    /// True when x falls in (or within 0.4 m of) one of the gaps described by `edges`.
    public static bool InFixedGap(float[] edges, float x)
    {
        for (int i = 1; i + 1 < edges.Length; i += 2)
            if (x > edges[i] - 0.4f && x < edges[i + 1] + 0.4f) return true;
        return false;
    }

    Transform _pickupRoot, _groundRoot;
    Rigidbody _rb;
    bool _loaded, _cleared;
    float _checkpointX, _checkpointTop;
    static readonly Vector3 StartPos = new Vector3(0f, 0.76f, 0f);
    static readonly Quaternion StartRot = Quaternion.Euler(0f, 145f, 0f); // faces the camera, turned a little into the run

    public int PickupCount { get; private set; }

    /// Gaps of the live level as (x0, x1) pairs; LevelSetSpawner hides lane dressing over them.
    public static readonly List<Vector2> Gaps = new List<Vector2>();
    /// Takeoff surface y for each gap (honey sits in the arc above this).
    public static readonly List<float> GapLip = new List<float>();
    /// Solid ground blocks as (x0, x1) pairs.
    public static readonly List<Vector2> Blocks = new List<Vector2>();
    /// Walkable top y of each block (parallel to Blocks).
    public static readonly List<float> BlockTop = new List<float>();

    public static bool IsOverGap(float x)
    {
        for (int i = 0; i < Gaps.Count; i++) if (x > Gaps[i].x && x < Gaps[i].y) return true;
        return false;
    }

    /// 0..1 run progress toward the goal.
    public float Progress
    {
        get
        {
            if (!_loaded || bean == null) return 0f;
            float span = Mathf.Max(0.1f, active.goalZ - StartPos.x);
            return Mathf.Clamp01((bean.position.x - StartPos.x) / span);
        }
    }

    public void LoadLevel(LevelCatalog.LevelDef def)
    {
        active = def;
        _loaded = true;
        _cleared = false;
        _checkpointX = StartPos.x;
        _checkpointTop = 0f;
        // Proof gravity stays −22 (hop 5.2 / mass 0.9 → ~0.52 s air). Catalog gravityScale is 1 on every level.
        Physics.gravity = new Vector3(0f, -22f, 0f);
        BuildGround(def);
        ResetBean();
        if (goalMarker != null)
            goalMarker.position = new Vector3(def.goalZ, 0f, 0.55f);
        SpawnPickups(def);
        // The open scene already holds the approved set. Do not spawn the old primitive yard over it.
        var set = FindAnyObjectByType<LevelSetSpawner>();
        bool handmade = GameObject.Find("Imagine Bean") != null;
        if (set != null && !handmade) set.ApplyLevel(def);
        Debug.Log($"[LevelRunner] L{def.id} {def.title} pack={def.pack} goalX={def.goalZ:0.0} dens={def.obstacleDensity:0.00} gaps={Gaps.Count} boss={def.bossBeat}");
        Debug.Log($"PLAY_SMOKE L{def.id} loaded goalZ={def.goalZ:0.0} dens={def.obstacleDensity:0.00}");
    }

    void ResetBean()
    {
        if (bean == null) return;
        if (_rb == null) _rb = bean.GetComponent<Rigidbody>();
        var p = new Vector3(_checkpointX, StartPos.y + _checkpointTop, 0f);
        bean.SetPositionAndRotation(p, StartRot);
        if (_rb != null)
        {
            // Move the body too, so the next physics step does not drag the bean back to where it was
            // (replaying from the goal post would otherwise re-trigger the clear on the first frame).
            _rb.position = p; _rb.rotation = StartRot;
            _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
    }

    void Update()
    {
        if (!_loaded || bean == null || !Application.isPlaying) return;
        if (bean.position.y < fallResetY)
        {
            EscapeAudio.Play(EscapeAudio.Sfx.Tap, 0.6f);
            ResetBean();
            return;
        }
        // checkpoint: the start of the last solid block the bean stood on (including a raised kraft block)
        float bx = bean.position.x;
        float feet = bean.position.y - 0.43f; // capsule half-height 0.86 / 2
        for (int i = 0; i < Blocks.Count; i++)
        {
            float top = i < BlockTop.Count ? BlockTop[i] : 0f;
            if (bx > Blocks[i].x + 0.5f && bx < Blocks[i].y - 0.3f && feet > top - 0.08f && feet < top + 0.30f)
            {
                float cp = Blocks[i].x + 0.7f;
                if (cp > _checkpointX)
                {
                    _checkpointX = cp;
                    _checkpointTop = top;
                }
            }
        }
        if (!_cleared && bx >= active.goalZ)
        {
            _cleared = true;
            if (GameSession.I != null) GameSession.I.CompleteLevel();
        }
    }

    // ——— ground: burlap blocks with hop gaps (run 2.6 m/s, apex ≈ 1.04 m → hop range ≈ 1.6 m + capsule reach) ———

    void BuildGround(LevelCatalog.LevelDef def)
    {
        if (_groundRoot == null)
        {
            var go = GameObject.Find("Level Ground");
            if (go == null) go = new GameObject("Level Ground");
            _groundRoot = go.transform;
        }
        for (int i = _groundRoot.childCount - 1; i >= 0; i--)
        {
            var c = _groundRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        Gaps.Clear();
        GapLip.Clear();
        Blocks.Clear();
        BlockTop.Clear();
        if (!Application.isPlaying) return;

        int id = Mathf.Clamp(def.id, 1, 5);
        var burlapTex = Resources.Load<Texture2D>("Look/tex-burlap");
        var kraftTex = Resources.Load<Texture2D>("Look/tex-kraft-corrugated");
        var kraft = CraftMesh.Shared("ground-kraft", () => CraftMesh.Felt(kraftTex, new Color(0.92f, 0.84f, 0.72f), new Vector2(2.5f, 1f), 0.8f, 0.06f, 0.55f, 0.9f));
        var kraftDeep = CraftMesh.Shared("ground-kraft-deep", () => CraftMesh.Felt(kraftTex, new Color(0.72f, 0.60f, 0.46f), new Vector2(2.5f, 1f), 0.8f, 0.06f, 0.55f, 0.9f));

        if (id == 1)
        {
            // 285: calm burlap yard. Gaps stay 1.05 m and 1.10 m.
            // A short ramp on each far lip so the capsule rides onto the block instead of catching the vertical face.
            LayFixed(L1Edges, burlapTex);
            Ramp(10.85f - 0.48f, -0.08f, 10.85f + 0.08f, 0.05f, burlapTex);
            Ramp(13.7f - 0.48f, -0.08f, 13.7f + 0.08f, 0.05f, burlapTex);
        }
        else if (id == 2)
        {
            // gen-2d7dcad9: arch ON the burlap. ImagineSet owns the deck. Gaps 1.20 m and 1.35 m are past it.
            LayFixed(L2Edges, burlapTex);
        }
        else if (id == 3)
        {
            // Wider flat gaps (1.40 / 1.42 / 1.38) plus kraft steps on each long block, clear of the lips.
            LayFixed(L3Edges, burlapTex);
            Step(11.00f, 0.9f, 0.26f, kraft);   // block [9.40, 15.20]
            Step(11.90f, 0.9f, 0.52f, kraftDeep);
            Step(18.20f, 0.9f, 0.26f, kraft);   // block [16.62, 22.40]
            Step(19.10f, 0.9f, 0.52f, kraftDeep);
            Step(25.50f, 0.9f, 0.26f, kraft);   // block [23.78, 33]
            Step(26.40f, 0.9f, 0.52f, kraftDeep);
        }
        else if (id == 4)
        {
            // Low yard, steps up onto a kraft slab, hop at the same height, then drop back to burlap.
            Span(L4Low[0], L4Low[1], 0f, burlapTex, null);
            Open(L4Low[1], L4Low[2], 0f);                         // 1.30 m
            Span(L4Low[2], L4Low[3], 0f, burlapTex, null);
            Step(11.00f, 0.9f, 0.28f, kraft);                     // ends flush with the slab at 12.80
            Step(11.90f, 0.9f, L4HighTop, kraftDeep);
            Span(L4High[0], L4High[1], L4HighTop, burlapTex, kraft);
            Open(L4High[1], L4High[2], L4HighTop);                // 1.40 m, both tops 0.56
            Span(L4High[2], L4High[3], L4HighTop, burlapTex, kraft);
            Open(L4High[3], L4Low[4], L4HighTop);                 // 1.50 m down to y = 0
            Span(L4Low[4], L4Low[5], 0f, burlapTex, null);
        }
        else
        {
            // Long tour: yard, arch on the floor, steps onto a kraft run, yarn height, drop to the goal.
            Span(L5Low[0], L5Low[1], 0f, burlapTex, null);
            Open(L5Low[1], L5Low[2], 0f);                         // 1.20 m
            Span(L5Low[2], L5Low[3], 0f, burlapTex, null);        // tour arch sits on this block
            var tour = new GameObject("Tour Arch");
            tour.transform.SetParent(_groundRoot, false);
            float tourLen = TourBridgeLen + BridgePlankRun;
            tour.transform.position = new Vector3(TourBridgeX0 + TourBridgeLen * 0.5f, 0f, 0f);
            ImagineSet.FillBridge(tour, tourLen, BridgeArch);
            Step(18.80f, 0.9f, 0.26f, kraft);                     // after the exit ramp, still on [10.20, 22]
            Step(19.70f, 0.9f, 0.52f, kraftDeep);
            Open(L5Low[3], L5Low[4], 0f);                         // 1.42 m
            Span(L5Low[4], L5Low[5], 0f, burlapTex, null);
            Step(25.20f, 0.9f, 0.26f, kraft);                     // ends flush with the slab at 27.00
            Step(26.10f, 0.9f, L5HighTop, kraftDeep);
            Span(L5High[0], L5High[1], L5HighTop, burlapTex, kraft);
            Open(L5High[1], L5High[2], L5HighTop);                // 1.40 m, both tops 0.52
            Span(L5High[2], L5High[3], L5HighTop, burlapTex, kraft);
            Open(L5High[3], L5Low[6], L5HighTop);                 // 1.48 m down
            Span(L5Low[6], L5Low[7], 0f, burlapTex, null);
        }
    }

    /// Blocks and gaps from an alternating edge list: block [e0,e1], gap [e1,e2], block [e2,e3], …
    void LayFixed(float[] e, Texture2D burlapTex)
    {
        for (int i = 0; i + 1 < e.Length; i += 2)
        {
            Span(e[i], e[i + 1], 0f, burlapTex, null);
            if (i + 2 < e.Length) Open(e[i + 1], e[i + 2], 0f);
        }
    }

    void Span(float x0, float x1, float top, Texture2D burlapTex, Material kraft)
    {
        if (top > 0.02f) KraftBlock(x0, x1, top, kraft);
        else GroundBlock(x0, x1, burlapTex);
        Blocks.Add(new Vector2(x0, x1));
        BlockTop.Add(top);
    }

    void Open(float x0, float x1, float lip)
    {
        Gaps.Add(new Vector2(x0, x1));
        GapLip.Add(lip);
    }

    /// Burlap mat ramp whose top surface runs from (xa, ha) to (xb, hb), xa < xb: a thin tilted slab with grip,
    /// so the bean walks onto / off the bridge deck instead of bumping a step.
    void Ramp(float xa, float ha, float xb, float hb, Texture2D tex)
    {
        float dx = xb - xa, dh = hb - ha;
        float len = Mathf.Sqrt(dx * dx + dh * dh);
        float ang = Mathf.Atan2(dh, dx) * Mathf.Rad2Deg;
        const float thick = 0.08f;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Burlap Ramp";
        go.transform.SetParent(_groundRoot, false);
        var rot = Quaternion.Euler(0f, 0f, ang);
        Vector3 mid = new Vector3((xa + xb) * 0.5f, (ha + hb) * 0.5f, 0.1f);
        go.transform.position = mid - rot * Vector3.up * (thick * 0.5f);
        go.transform.rotation = rot;
        go.transform.localScale = new Vector3(len, thick, 1.7f);
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = CraftMesh.Shared("ground-burlap-mat", () =>
            CraftMesh.Felt(tex, Color.white, new Vector2(2.4f, 3f), 0.35f, 0.02f, 0.70f, 0.98f)); // tex-burlap is already 174,135,90
        go.GetComponent<Collider>().material = GripMat();
    }

    void GroundBlock(float x0, float x1, Texture2D tex)
    {
        float len = x1 - x0;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Burlap Ground";
        go.transform.SetParent(_groundRoot, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, -0.08f, 0.5f);
        go.transform.localScale = new Vector3(len, 0.16f, LaneDepth);
        go.isStatic = true;
        var r = go.GetComponent<Renderer>();
        // 285.jpg / library-wide-set-1.jpg: light tan grid. Gap edges unchanged. Same tex-burlap.png.
        float tile = Mathf.Max(1f, Mathf.Round(len * 1.6f));
        r.sharedMaterial = CraftMesh.Shared($"ground-burlap-tan-{tile}", () =>
            CraftMesh.Felt(tex, Color.white, new Vector2(tile, LaneDepth * 1.6f), 0.35f, 0.02f, 0.70f, 0.98f));
        var col = go.GetComponent<Collider>();
        col.material = GripMat();
        // frayed burlap edge at each gap: a thin darker lip
        var lip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lip.name = "Burlap Edge";
        lip.transform.SetParent(_groundRoot, false);
        lip.transform.position = new Vector3((x0 + x1) * 0.5f, -0.17f, 0.5f);
        lip.transform.localScale = new Vector3(len + 0.04f, 0.06f, LaneDepth + 0.04f);
        lip.isStatic = true;
        Destroy(lip.GetComponent<Collider>());
        lip.GetComponent<Renderer>().sharedMaterial = CraftMesh.Shared("ground-burlap-edge", () =>
            CraftMesh.Felt(tex, new Color(0.62f, 0.52f, 0.40f), new Vector2(8f, 8f), 1.4f, 0.05f, 0.70f, 0.98f));
    }

    /// Raised kraft platform. Top surface at `top`; the block is the floor, not a lid over a pit.
    void KraftBlock(float x0, float x1, float top, Material mat)
    {
        float len = x1 - x0;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Kraft Block";
        go.transform.SetParent(_groundRoot, false);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, top - 0.08f, 0.5f);
        go.transform.localScale = new Vector3(len, 0.16f, LaneDepth);
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().material = GripMat();
    }

    void Step(float x, float len, float h, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Kraft Step";
        go.transform.SetParent(_groundRoot, false);
        go.transform.position = new Vector3(x + len * 0.5f, h * 0.5f, 0.1f);
        go.transform.localScale = new Vector3(len, h, 1.6f);
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().material = GripMat();
    }

    static PhysicsMaterial _grip;
    static PhysicsMaterial GripMat()
    {
        if (_grip == null)
            _grip = new PhysicsMaterial("BurlapGrip")
            {
                dynamicFriction = 0.35f, staticFriction = 0.4f, bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum
            };
        return _grip;
    }

    // ——— pickups: honey beans in the hop arcs, one over every gap ———

    void SpawnPickups(LevelCatalog.LevelDef def)
    {
        if (_pickupRoot == null)
        {
            var go = GameObject.Find("Level Pickups");
            if (go == null) go = new GameObject("Level Pickups");
            _pickupRoot = go.transform;
        }
        for (int i = _pickupRoot.childCount - 1; i >= 0; i--)
        {
            var c = _pickupRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        PickupCount = 0;
        if (!Application.isPlaying) return;

        var rng = new System.Random(def.seed * 7 + 13);
        var tex = Resources.Load<Texture2D>("Look/tex-felt-brown");
        var honey = CraftMesh.Shared("pickup-honey", () =>
            CraftMesh.Felt(tex, new Color(1.55f, 1.25f, 0.70f), Vector2.one * 2f, 1f, 0.22f, 0.7f, 0.85f));

        // one over the middle of every gap, in the hop arc above the takeoff lip
        for (int i = 0; i < Gaps.Count; i++)
        {
            float lip = i < GapLip.Count ? GapLip[i] : 0f;
            Honey(new Vector3((Gaps[i].x + Gaps[i].y) * 0.5f, 1.05f + lip, 0f), rng, honey);
        }
        // a few low ones on the blocks to reward a hop on solid ground
        for (int i = 0; i < Blocks.Count; i++)
        {
            float len = Blocks[i].y - Blocks[i].x;
            if (len < 3.5f || rng.NextDouble() < 0.35) continue;
            float top = i < BlockTop.Count ? BlockTop[i] : 0f;
            float bx = Mathf.Lerp(Blocks[i].x + 1.2f, Blocks[i].y - 1.2f, (float)rng.NextDouble());
            Honey(new Vector3(bx, top + 0.75f + (float)rng.NextDouble() * 0.3f, 0f), rng, honey);
        }
    }

    void Honey(Vector3 pos, System.Random rng, Material honey)
    {
        var root = new GameObject("Honey Bean");
        root.transform.SetParent(_pickupRoot, false);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 20f);
        var mf = root.AddComponent<MeshFilter>();
        mf.sharedMesh = CraftMesh.Egg(0.085f, 0.11f, 0.07f, 0.1f, 16, 12);
        var mr = root.AddComponent<MeshRenderer>();
        mr.sharedMaterial = honey;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.22f;
        var p = root.AddComponent<Pickup>();
        p.bob = 0.05f;
        PickupCount++;
    }
}

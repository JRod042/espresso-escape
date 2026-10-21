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

    Transform _pickupRoot, _groundRoot;
    Rigidbody _rb;
    bool _loaded, _cleared;
    float _checkpointX;
    static readonly Vector3 StartPos = new Vector3(0f, 0.76f, 0f);
    static readonly Quaternion StartRot = Quaternion.Euler(0f, 145f, 0f); // faces the camera, turned a little into the run

    public int PickupCount { get; private set; }

    /// Gaps of the live level as (x0, x1) pairs; LevelSetSpawner hides lane dressing over them.
    public static readonly List<Vector2> Gaps = new List<Vector2>();
    /// Solid ground blocks as (x0, x1) pairs.
    public static readonly List<Vector2> Blocks = new List<Vector2>();

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
        // Gravity feel (base Physics.gravity.y is -22 from scene build)
        float g = -22f * def.gravityScale;
        Physics.gravity = new Vector3(0f, g, 0f);
        BuildGround(def);
        ResetBean();
        if (goalMarker != null)
            goalMarker.position = new Vector3(def.goalZ, 0f, 0.55f);
        // Distinct LOOK-craft density per level (not yarn-sphere spam)
        var set = FindFirstObjectByType<LevelSetSpawner>();
        if (set != null) set.ApplyLevel(def);
        SpawnPickups(def);
        Debug.Log($"[LevelRunner] L{def.id} {def.title} pack={def.pack} goalX={def.goalZ:0.0} dens={def.obstacleDensity:0.00} gaps={Gaps.Count} boss={def.bossBeat}");
        Debug.Log($"PLAY_SMOKE L{def.id} loaded goalZ={def.goalZ:0.0} dens={def.obstacleDensity:0.00}");
    }

    void ResetBean()
    {
        if (bean == null) return;
        if (_rb == null) _rb = bean.GetComponent<Rigidbody>();
        var p = new Vector3(_checkpointX, StartPos.y, 0f);
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
        // checkpoint: the start of the last solid block the bean stood on
        float bx = bean.position.x;
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (bx > Blocks[i].x + 0.5f && bx < Blocks[i].y - 0.3f && bean.position.y < 1.0f)
            {
                float cp = Blocks[i].x + 0.7f;
                if (cp > _checkpointX) _checkpointX = cp;
            }
        }
        if (!_cleared && bx >= active.goalZ)
        {
            _cleared = true;
            if (GameSession.I != null) GameSession.I.CompleteLevel();
        }
    }

    // ——— ground: burlap blocks with hop gaps (hop range ≈ 3.5 m, apex ≈ 1.1 m) ———

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
        Blocks.Clear();
        if (!Application.isPlaying) return;

        var rng = new System.Random(def.seed * 3 + 7);
        int id = Mathf.Clamp(def.id, 1, 5);
        // gap width grows with the level; L1 teaches, L2 asks for edge take-offs
        float gapMin = Mathf.Lerp(1.05f, 1.45f, (id - 1) / 4f);
        float gapMax = Mathf.Lerp(1.35f, 1.95f, (id - 1) / 4f);
        float blockMin = Mathf.Lerp(4.2f, 2.8f, (id - 1) / 4f);
        float blockMax = Mathf.Lerp(6.0f, 4.2f, (id - 1) / 4f);

        var burlapTex = Resources.Load<Texture2D>("Look/tex-burlap");
        var kraftTex = Resources.Load<Texture2D>("Look/tex-kraft-corrugated");
        var kraft = CraftMesh.Shared("ground-kraft", () => CraftMesh.Felt(kraftTex, new Color(0.92f, 0.84f, 0.72f), new Vector2(2.5f, 1f), 0.8f, 0.06f, 0.55f, 0.9f));
        var kraftDeep = CraftMesh.Shared("ground-kraft-deep", () => CraftMesh.Felt(kraftTex, new Color(0.72f, 0.60f, 0.46f), new Vector2(2.5f, 1f), 0.8f, 0.06f, 0.55f, 0.9f));

        float x = ApronEnd;
        float end = def.goalZ + 5f;
        int n = 0;
        while (x < end)
        {
            float len = Mathf.Lerp(blockMin, blockMax, (float)rng.NextDouble());
            if (x + len > end - 0.5f) len = end - x;
            float x1 = x + len;
            GroundBlock(x, x1, burlapTex);
            Blocks.Add(new Vector2(x, x1));
            // L2+: a kraft step at the front of some blocks (set-03 bridge steps), hop up onto it
            if (id >= 2 && n % 2 == 1 && len > 3.2f)
            {
                float sx = x + 0.9f;
                Step(sx, 0.9f, 0.26f, kraft);
                if (id >= 3) Step(sx + 0.9f, 0.9f, 0.52f, kraftDeep);
            }
            n++;
            if (x1 >= end - 0.6f) break;
            float gap = Mathf.Lerp(gapMin, gapMax, (float)rng.NextDouble());
            Gaps.Add(new Vector2(x1, x1 + gap));
            x = x1 + gap;
        }
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
        // weave ≈ 1.8 cells/m like the plates; shared per length bucket so blocks batch
        float tile = Mathf.Round(len * 1.8f);
        r.sharedMaterial = CraftMesh.Shared($"ground-burlap-{tile}", () =>
            CraftMesh.Felt(tex, Color.white, new Vector2(tile, LaneDepth * 1.8f), 1.4f, 0.08f, 0.70f, 0.95f));
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
                dynamicFriction = 0.95f, staticFriction = 1f, bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Maximum, bounceCombine = PhysicsMaterialCombine.Minimum
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

        // one over the middle of every gap, at the top of the hop arc
        for (int i = 0; i < Gaps.Count; i++)
            Honey(new Vector3((Gaps[i].x + Gaps[i].y) * 0.5f, 1.05f, 0f), rng, honey);
        // a few low ones on the blocks to reward a hop on solid ground
        for (int i = 0; i < Blocks.Count; i++)
        {
            float len = Blocks[i].y - Blocks[i].x;
            if (len < 3.5f || rng.NextDouble() < 0.35) continue;
            float bx = Mathf.Lerp(Blocks[i].x + 1.2f, Blocks[i].y - 1.2f, (float)rng.NextDouble());
            Honey(new Vector3(bx, 0.75f + (float)rng.NextDouble() * 0.3f, 0f), rng, honey);
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

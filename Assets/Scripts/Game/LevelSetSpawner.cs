using UnityEngine;

/// Spawns/clears DISTINCT dense LOOK-craft layouts per HQ level.
/// Props from BuildEscapeScene vocabulary; FULL PaintTex LOOK kit (Resources/Look) — no flat Paint.
/// k112: Imagine LOOK densify — +moss/roast/orbs/ribs/blooms + extra felt clouds/succulents (Drive/ImagineRefs only).
public class LevelSetSpawner : MonoBehaviour
{
    public Transform root;

    // The Look textures now carry the plate colours (sampled from DriveLook); tints are near-white multipliers.
    static readonly Color Kraft = new Color(1.00f, 0.98f, 0.94f);
    static readonly Color KraftDark = new Color(0.78f, 0.74f, 0.70f);
    static readonly Color FeltPine = new Color(1.00f, 1.00f, 1.00f);
    static readonly Color FeltDark = new Color(0.55f, 0.45f, 0.38f);   // trunks / pinecones over tex-felt-brown
    static readonly Color Yarn = new Color(1.00f, 1.00f, 1.00f);
    static readonly Color Stitch = new Color(1.05f, 1.02f, 0.98f);
    static readonly Color Cloud = new Color(1.00f, 1.00f, 1.00f);

    // ——— side-scroll depth mapping (lane space → stage space) ———
    // Spawn code lays props out in "lane space": x across the lane, z along the run. The stage is a
    // side-scroller (LittleBigPlanet): the run is +X, the camera looks along +Z, the kraft wall is at z ≈ 3.
    // Lane-space x<0 goes to the background band, x>0 to the foreground band; tall dressing is always behind.
    const float BgNear = 0.75f, BgPerUnit = 0.55f;   // z = 0.75 .. ~2.5 (in front of the wall at 3.0)
    const float FgNear = 0.45f, FgPerUnit = 0.35f;   // z = -0.45 .. ~-1.5 (between the lane and the camera)

    static readonly string[] AlwaysBehind =
    {
        "Pine", "Kraft Rib", "Felt Cloud", "Cardboard Grinder", "Grinder", "Drawer Knob", "Crank Knob", "Succulent",
        "Flower", "Bloom", "Hang String", "Yarn Wrap", "Yarn Ball", "Yarn Bump"
    };

    static bool StartsWithAny(string name, string[] prefixes)
    {
        for (int i = 0; i < prefixes.Length; i++) if (name.StartsWith(prefixes[i])) return true;
        return false;
    }

    static float Depth(float laneX, bool forceBehind)
    {
        if (forceBehind || laneX < 0f) return BgNear + Mathf.Abs(laneX) * BgPerUnit;
        return -(FgNear + laneX * FgPerUnit);
    }

    /// Lane space → stage space for every spawned prop (count-neutral; proofs count children only).
    void RemapToSideScroll()
    {
        var yaw = Quaternion.Euler(0f, 90f, 0f); // local x (across lane) → world -z (toward camera)
        for (int i = 0; i < root.childCount; i++)
        {
            var t = root.GetChild(i);
            var p = t.position;
            string n = t.name;
            bool behind = StartsWithAny(n, AlwaysBehind);
            bool laneItem = Mathf.Abs(p.x) < 0.6f;
            if (n.StartsWith("Bridge Rail"))
            {
                // rails run along the bridge in front of the bean (LBP foreground railing), never across the lane
                t.position = new Vector3(p.z, p.y, -0.80f);
                NoCol(t.gameObject);
                continue;
            }
            float z = laneItem ? p.x : Depth(p.x, behind);
            t.position = new Vector3(p.z, p.y, z);
            t.rotation = yaw * t.rotation;
            // lane dressing over a hop gap would float in mid-air: tuck it away (still counted)
            if (laneItem && LevelRunner.IsOverGap(p.z) && (n.StartsWith("Burlap Path") || n.StartsWith("Contact Shadow") || n.StartsWith("Kraft Flute")))
                t.position = new Vector3(p.z, -6f, z);
        }
    }

    LevelCatalog.LevelDef _def;
    int _lastSpawnCount;

    public int LastSpawnCount => _lastSpawnCount;

    /// Combine spawned props into static batches (play mode only; proofs run in edit mode).
    public bool staticBatch = true;

    public void ApplyLevel(LevelCatalog.LevelDef def)
    {
        _def = def;
        Rebuild();
    }

    /// Back-compat for older callers — synthesizes a Yard-ish def from density/seed.
    public void ApplyDensity(float density, int seed)
    {
        _def = new LevelCatalog.LevelDef
        {
            id = 1,
            title = "Café Yard",
            pack = "Yard",
            goalZ = 14f,
            obstacleDensity = Mathf.Clamp01(density),
            seed = seed,
            bossBeat = false
        };
        Rebuild();
    }

    public void Rebuild()
    {
        EnsureRoot();
        ClearChildren();

        var rng = new System.Random(_def.seed != 0 ? _def.seed : 1);
        float dens = Mathf.Clamp01(_def.obstacleDensity);
        float zMax = Mathf.Max(8f, _def.goalZ - 1.5f);
        int id = Mathf.Clamp(_def.id, 1, 5);

        switch (id)
        {
            case 1: SpawnCafeYard(rng, dens, zMax); break;
            case 2: SpawnKraftBridge(rng, dens, zMax); break;
            case 3: SpawnMillPath(rng, dens, zMax); break;
            case 4: SpawnNightRoast(rng, dens, zMax); break;
            default: SpawnFinaleEscape(rng, dens, zMax, _def.bossBeat); break;
        }

        RemapToSideScroll();

        _lastSpawnCount = root.childCount;
        Debug.Log($"[LevelSetSpawner] L{id} {_def.title} pack={_def.pack} dens={dens:0.00} goalZ={_def.goalZ:0.0} props={_lastSpawnCount} boss={_def.bossBeat}");

        if (staticBatch && Application.isPlaying)
        {
            // shared materials + one combine pass → a few dozen draw calls instead of thousands
            StaticBatchingUtility.Combine(root.gameObject);
        }
    }

    void EnsureRoot()
    {
        if (root == null)
        {
            var go = GameObject.Find("Level Obstacles");
            if (go == null) go = new GameObject("Level Obstacles");
            root = go.transform;
        }
    }

    void ClearChildren()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var c = root.GetChild(i).gameObject;
            if (Application.isPlaying) Object.Destroy(c);
            else Object.DestroyImmediate(c);
        }
    }

    // ——— L1 Café Yard (LOOK set): burlap path + moss + roast + yarn orbs + kraft ribs ———
    // scope-L1L3 polish: p10 final feel — hop readability + Imagine material cohesion (count-neutral; Soft Key HELD)
    // laneB-L1-contact-read-20260917: L1 hop-lane ContactShadow 0.50→0.54 (scale-only; LEVEL_SMOKE counts HELD)
    // laneB-L1-moss-contact-20260917: L1 moss ContactShadow 0.12→0.14 (scale-only; LEVEL_SMOKE counts HELD)
    void SpawnCafeYard(System.Random rng, float dens, float zMax)
    {
        int moss = Mathf.RoundToInt(Mathf.Lerp(72, 268, dens)); // scope-L1L3 p1 Imagine yard densify
        int roast = Mathf.RoundToInt(Mathf.Lerp(60, 214, dens)); // scope-L1L3 p1 Imagine yard densify
        int orbs = Mathf.RoundToInt(Mathf.Lerp(5, 14, dens)); // scope-L1L3 p1
        int pines = Mathf.RoundToInt(Mathf.Lerp(3, 6, dens)); // scope-L1L3 p1
        int ribs = Mathf.RoundToInt(Mathf.Lerp(6, 12, dens)); // scope-L1L3 p1
        int pots = Mathf.RoundToInt(Mathf.Lerp(2, 6, dens)); // scope-L1L3 p1 succulents

        // burlap path strip down center (Imagine handmade runner) — denser play-path
        for (int i = 0; i < 8; i++) // scope-L1L3 p2
        {
            float z = 1.0f + i * (zMax / 9f);
            BurlapPathStrip(new Vector3(0f, 0.02f, z), 1.04f + (float)rng.NextDouble() * 0.06f); // p10 hop-track clear
            ContactShadow(new Vector3(0f, 0.01f, z), 0.54f); // laneB-L1-contact-read: stronger hop-lane foot ellipse (count-neutral)
        }
        for (int i = 0; i < moss; i++)
        {
            // p10: clearer hop corridor — count-neutral vs Imagine
            var p = PlayLanePos(rng, 1.2f, zMax, 2.0f, 0.70f);
            MossBall(p, 0.10f + (float)rng.NextDouble() * 0.14f);
            ContactShadow(p, 0.14f); // laneB-L1-moss-contact: stronger moss foot ellipse (count-neutral)
            if (i % 5 == 0) ContactShadow(p + new Vector3(0.08f, 0f, 0.05f), 0.10f); // p10 soft ground
        }
        for (int i = 0; i < roast; i++)
        {
            var p = PlayLanePos(rng, 1.5f, zMax, 1.8f, 0.68f);
            RoastBean(p, 0.07f + (float)rng.NextDouble() * 0.05f, (float)rng.NextDouble() * 360f);
            ContactShadow(p, 0.10f); // p10 roast contact
        }
        for (int i = 0; i < orbs; i++)
        {
            var op = PlayLanePos(rng, 2f, zMax * 0.7f, 1.6f, 0.74f) + Vector3.up * (0.55f + (float)rng.NextDouble() * 0.35f);
            YarnOrb(op, 0.12f + (float)rng.NextDouble() * 0.08f, i % 2 == 0);
            if (i % 2 == 0) HangString(op + Vector3.up * 0.32f, 0.39f + (float)rng.NextDouble() * 0.15f); // laneB-L1-hangstring-20260917
        }
        for (int i = 0; i < pines; i++)
            PineLite(new Vector3(Side(rng, 1.85f, 2.45f), 0.55f + (float)rng.NextDouble() * 0.25f, 2.5f + (float)rng.NextDouble() * (zMax * 0.45f)), 0.55f + (float)rng.NextDouble() * 0.25f);
        for (int i = 0; i < 9; i++) // p10 yarn stitch rails — hop-edge frame (count-neutral)
        {
            float z = 1.2f + i * (zMax / 10f);
            PathYarnEdge(new Vector3(-0.70f, 0.04f, z), 0.068f);
            PathYarnEdge(new Vector3(0.70f, 0.04f, z), 0.068f);
            PathYarnEdge(new Vector3(-0.58f, 0.035f, z + 0.12f), 0.052f);
            PathYarnEdge(new Vector3(0.58f, 0.035f, z + 0.12f), 0.052f);
        }
        // kraft corrugated wall ribs (LOOK set backdrop) + yarn wrap craft
        for (int i = 0; i < ribs; i++)
        {
            var rp = new Vector3(Side(rng, 2.4f, 2.9f), 1.1f, 2f + (float)rng.NextDouble() * (zMax * 0.7f));
            float rh = 1.8f + (float)rng.NextDouble() * 0.4f;
            KraftRib(rp, rh);
            // land laneB-L1-yarnwrap-20260917
            YarnWrapPost(rp, rh * 0.62f); // p10 wrap scale matches rib craft
        }
        for (int i = 0; i < pots; i++)
            SucculentPot(new Vector3(Side(rng, 1.15f, 1.65f), 0.08f, 2.2f + (float)rng.NextDouble() * (zMax * 0.65f)), 0.20f + (float)rng.NextDouble() * 0.08f);
        FeltCloud(new Vector3(-1.0f, 2.05f, zMax * 0.35f), 0.32f);
        FeltCloud(new Vector3(1.1f, 2.15f, zMax * 0.55f), 0.36f);
        FeltCloud(new Vector3(0.2f, 2.25f, zMax * 0.72f), 0.30f); // k112 densify
        FeltCloud(new Vector3(-0.35f, 2.15f, zMax * 0.58f), 0.26f); // k180 light densify
        FeltCloud(new Vector3(0.85f, 2.05f, zMax * 0.42f), 0.32f); // land laneB-L1-feltcloud-20260917
    }

    // ——— L2 Kraft Bridge: planks/posts/rails + grass + pines ———
    // scope-L1L3 polish: p10 final feel — hop deck read + kraft cohesion; LOOK side densify HELD (k494); plank Lerp(8,16) HELD
    // laneB-L2-plank-contact-20260917: L2 plank foot ContactShadow 0.48→0.52 (scale-only; LEVEL_SMOKE counts HELD)
    void SpawnKraftBridge(System.Random rng, float dens, float zMax)
    {
        // Play-feel: readable Imagine craft bridge (not 500 solid planks). Side dressing keeps LOOK density.
        int planks = Mathf.RoundToInt(Mathf.Lerp(8, 16, dens)); // scope-L1L3 p3–p10 structural play-feel HELD
        int grass = Mathf.RoundToInt(Mathf.Lerp(8, 687, dens)); // k494 densify L2 LAND HELD
        int pines = Mathf.RoundToInt(Mathf.Lerp(3, 246, dens)); // k494 densify L2 LAND HELD
        int moss = Mathf.RoundToInt(Mathf.Lerp(4, 679, dens)); // k494 densify L2 LAND HELD
        int orbs = Mathf.RoundToInt(Mathf.Lerp(3, 524, dens)); // k494 densify L2 LAND HELD

        float bridgeZ0 = 4f;
        float bridgeLen = Mathf.Min(10f, zMax * 0.45f);
        for (int i = 0; i < planks; i++)
        {
            float t = i / (float)Mathf.Max(1, planks - 1);
            float z = bridgeZ0 + t * bridgeLen;
            float arch = Mathf.Sin(t * Mathf.PI) * 0.35f;
            // p10: kraft family tint cohesion + hop deck read (plank Lerp 8–16 HELD)
            BridgePlank(new Vector3(0f, 0.10f + arch, z), 1.06f + (float)rng.NextDouble() * 0.08f,
                i % 2 == 0 ? Kraft : new Color(0.72f, 0.64f, 0.56f));
            ContactShadow(new Vector3(0f, 0.01f, z), 0.52f); // laneB-L2-plank-contact: stronger plank foot ellipse (count-neutral)
            if (i % 2 == 0)
            {
                BridgePost(new Vector3(-0.80f, 0.35f + arch, z), 0.55f); // p10 posts clear hop deck
                BridgePost(new Vector3(0.80f, 0.35f + arch, z), 0.55f);
            }
            if (i % 3 == 0)
                BridgeRail(new Vector3(0f, 0.64f + arch, z), 1.18f); // p10 rail above hop arc
        }
        for (int i = 0; i < grass; i++)
            BridgeGrass(PlayLanePos(rng, bridgeZ0 - 0.5f, bridgeZ0 + bridgeLen + 1f, 1.55f, 0.82f), 0.08f + (float)rng.NextDouble() * 0.08f);
        for (int i = 0; i < pines; i++)
            PineLite(new Vector3(Side(rng, 1.95f, 2.65f), 0.6f + (float)rng.NextDouble() * 0.4f, bridgeZ0 + (float)rng.NextDouble() * bridgeLen), 0.65f + (float)rng.NextDouble() * 0.4f);
        for (int i = 0; i < moss; i++)
            MossBall(PlayLanePos(rng, 1.5f, zMax, 2.0f, 0.80f), 0.10f + (float)rng.NextDouble() * 0.12f);
        for (int i = 0; i < orbs; i++)
            YarnOrb(PlayLanePos(rng, 3f, zMax * 0.8f, 1.5f, 0.76f) + Vector3.up * 0.7f, 0.11f + (float)rng.NextDouble() * 0.07f, i % 2 == 0);
        // LOOK kraft flutes on arch + yarn-wrap posts + burlap approach/exit
        for (int i = 0; i < planks; i += 2)
        {
            float t = i / (float)Mathf.Max(1, planks - 1);
            float z = bridgeZ0 + t * bridgeLen;
            float arch = Mathf.Sin(t * Mathf.PI) * 0.35f;
            KraftFlute(new Vector3(-0.46f, 0.12f + arch, z), 0.45f);
            KraftFlute(new Vector3(0.46f, 0.12f + arch, z), 0.45f);
            YarnWrapPost(new Vector3(-0.80f, 0.35f + arch, z), 0.62f); // laneB-L2-yarnwrap-20260917: bridge-post wrap height for readability
            YarnWrapPost(new Vector3(0.80f, 0.35f + arch, z), 0.62f);
        }
        for (int i = 0; i < 4; i++) // p10 approach runner (count-neutral)
            BurlapPathStrip(new Vector3(0f, 0.02f, bridgeZ0 - 1.35f + i * 0.48f), 1.00f);
        for (int i = 0; i < 3; i++) // p10 exit runner
            BurlapPathStrip(new Vector3(0f, 0.02f, bridgeZ0 + bridgeLen + 0.35f + i * 0.5f), 1.00f);
        FeltCloud(new Vector3(-1.4f, 2.1f, bridgeZ0 + bridgeLen * 0.5f), 0.34f);
        FeltCloud(new Vector3(1.35f, 2.2f, bridgeZ0 + bridgeLen * 0.35f), 0.34f); // laneB-L2-feltcloud-20260917: 0.30f→0.34f
        FeltCloud(new Vector3(-0.2f, 2.28f, bridgeZ0 + bridgeLen * 0.7f), 0.27f); // scope-L1L3 p4
    }

    // ——— L3 Mill Path: grinder lane + flower canopy ———
    // scope-L1L3 polish: p10 final feel — mill hop corridor + Imagine cohesion (blooms HELD k343; count-neutral)
    // laneB-L3-grinder-shadow-20260917: L3 grinder ground ContactShadow 0.46→0.50 (scale-only; LEVEL_SMOKE counts HELD)
    void SpawnMillPath(System.Random rng, float dens, float zMax)
    {
        int grinders = Mathf.RoundToInt(Mathf.Lerp(1, 3, dens));
        int blooms = Mathf.RoundToInt(Mathf.Lerp(78, 660, dens)); // k343 Imagine densify L3 LAND HELD
        int roast = Mathf.RoundToInt(Mathf.Lerp(12, 48, dens)); // scope-L1L3 p5 mill floor
        int pines = Mathf.RoundToInt(Mathf.Lerp(3, 7, dens)); // scope-L1L3 p5
        int moss = Mathf.RoundToInt(Mathf.Lerp(14, 56, dens)); // scope-L1L3 p5 mill floor
        int orbs = Mathf.RoundToInt(Mathf.Lerp(5, 14, dens)); // scope-L1L3 p5

        for (int i = 0; i < grinders; i++)
        {
            float z = 5f + i * (zMax * 0.28f);
            float x = (i % 2 == 0) ? -1.65f : 1.65f; // p10 grinders clear mill hop
            var gp = new Vector3(x, 0.35f, z);
            CardboardGrinder(gp, 0.85f + (float)rng.NextDouble() * 0.2f);
            ContactShadow(new Vector3(x, 0.01f, z), 0.50f); // laneB-L3-grinder-shadow: stronger grinder ground ellipse (count-neutral)
            YarnWrapPost(new Vector3(x, 0.2f, z), 0.56f); // laneB-L3-grinder-wrap-20260917: grinder wrap height for mill craft read
        }
        for (int i = 0; i < blooms; i++)
        {
            float z = 3f + (float)rng.NextDouble() * (zMax - 4f);
            // p10: canopy further off corridor — mill hop readability (count-neutral)
            float x = Side(rng, 0.72f, 1.40f);
            float y = 1.20f + (float)rng.NextDouble() * 0.50f;
            var bp = new Vector3(x, y, z);
            FlowerBloom(bp, 0.12f + (float)rng.NextDouble() * 0.10f, i);
            if (i % 18 == 0) HangString(bp + Vector3.up * 0.28f, 0.34f); // laneB-L3-hangstring-20260917
        }
        for (int i = 0; i < roast; i++)
        {
            var p = PlayLanePos(rng, 2f, zMax, 1.7f, 0.70f);
            RoastBean(p, 0.07f + (float)rng.NextDouble() * 0.06f, (float)rng.NextDouble() * 360f);
            ContactShadow(p, 0.10f); // p10 roast contact
        }
        for (int i = 0; i < pines; i++)
            PineLite(new Vector3(Side(rng, 2.05f, 2.7f), 0.7f, 3f + (float)rng.NextDouble() * (zMax * 0.7f)), 0.7f + (float)rng.NextDouble() * 0.35f);
        for (int i = 0; i < moss; i++)
            MossBall(PlayLanePos(rng, 1.2f, zMax, 2.1f, 0.72f), 0.10f + (float)rng.NextDouble() * 0.14f);
        for (int i = 0; i < orbs; i++)
            YarnOrb(PlayLanePos(rng, 2.5f, zMax * 0.85f, 1.6f, 0.74f) + Vector3.up * (0.6f + (float)rng.NextDouble() * 0.4f), 0.10f + (float)rng.NextDouble() * 0.09f, i % 2 == 0);
        // LOOK mill: succulent pots + kraft ribs + burlap runner + path yarn
        int pots = Mathf.RoundToInt(Mathf.Lerp(4, 10, dens)); // scope-L1L3 p5
        for (int i = 0; i < pots; i++)
            SucculentPot(new Vector3(Side(rng, 1.2f, 1.7f), 0.08f, 3.5f + (float)rng.NextDouble() * (zMax * 0.6f)), 0.22f + (float)rng.NextDouble() * 0.08f);
        for (int i = 0; i < 6; i++) // scope-L1L3 p5
            KraftRib(new Vector3(Side(rng, 2.55f, 3.05f), 1.2f, 3f + i * (zMax * 0.11f)), 2.0f);
        for (int i = 0; i < 7; i++) // p10 mill hop lane read
            BurlapPathStrip(new Vector3(0f, 0.02f, 1.8f + i * (zMax / 8f)), 0.98f);
        for (int i = 0; i < 8; i++) // p10 path stitch yarn edge
        {
            float z = 2f + i * (zMax / 9f);
            PathYarnEdge(new Vector3(-0.68f, 0.04f, z), 0.062f);
            PathYarnEdge(new Vector3(0.68f, 0.04f, z), 0.062f);
        }
        FeltCloud(new Vector3(0.2f, 2.2f, zMax * 0.4f), 0.38f);
        FeltCloud(new Vector3(-1.1f, 2.1f, zMax * 0.55f), 0.36f); // laneB-L3-feltcloud-20260917
        FeltCloud(new Vector3(1.0f, 2.25f, zMax * 0.68f), 0.30f); // scope-L1L3 p5
    }

    // ——— L4 Night Roast: denser yarn orbs + roast clusters + warm moss ———
    void SpawnNightRoast(System.Random rng, float dens, float zMax)
    {
        // k674 densify (9k orbs / 9k roast / 9k moss / 2.4k pines / 6.6k blooms) spawned ~240k objects;
        // sane Imagine density now — denser than L3 in feel, cheap enough for phones.
        int orbs = Mathf.RoundToInt(Mathf.Lerp(10, 120, dens));
        int roast = Mathf.RoundToInt(Mathf.Lerp(10, 220, dens));
        int moss = Mathf.RoundToInt(Mathf.Lerp(8, 260, dens));
        int pines = Mathf.RoundToInt(Mathf.Lerp(3, 14, dens));
        int blooms = Mathf.RoundToInt(Mathf.Lerp(4, 160, dens));
        int planks = Mathf.RoundToInt(Mathf.Lerp(3, 14, dens));

        for (int i = 0; i < orbs; i++)
        {
            var p = RandPos(rng, 2f, zMax, 1.9f);
            p.y = 0.45f + (float)rng.NextDouble() * 0.85f;
            YarnOrb(p, 0.10f + (float)rng.NextDouble() * 0.12f, i % 2 == 0);
            if (i % 3 == 0) HangString(p + Vector3.up * 0.35f, 0.4f);
        }
        for (int i = 0; i < roast; i++)
            RoastBean(RandPos(rng, 1.5f, zMax, 1.8f), 0.06f + (float)rng.NextDouble() * 0.07f, (float)rng.NextDouble() * 360f);
        for (int i = 0; i < moss; i++)
            MossBall(RandPos(rng, 1.2f, zMax, 2.2f), 0.09f + (float)rng.NextDouble() * 0.16f);
        for (int i = 0; i < pines; i++)
            PineLite(new Vector3(Side(rng, 1.85f, 2.55f), 0.65f + (float)rng.NextDouble() * 0.35f, 2.5f + (float)rng.NextDouble() * (zMax * 0.75f)), 0.7f + (float)rng.NextDouble() * 0.45f);
        for (int i = 0; i < blooms; i++)
            FlowerBloom(new Vector3(((float)rng.NextDouble() - 0.5f) * 2.2f, 1.15f + (float)rng.NextDouble() * 0.4f, 3f + (float)rng.NextDouble() * (zMax - 5f)), 0.11f + (float)rng.NextDouble() * 0.08f, i);
        for (int i = 0; i < planks; i++)
            BridgePlank(new Vector3(0f, 0.06f + Mathf.Sin(i * 0.7f) * 0.12f, 6f + i * 1.1f), 1.05f);
        // LOOK night roast: warm felt clouds + kraft ribs + burlap + contact shadows on roast clusters
        for (int i = 0; i < 4; i++)
            FeltCloud(new Vector3(Side(rng, 0.8f, 1.6f), 1.9f + (float)rng.NextDouble() * 0.3f, 3f + (float)rng.NextDouble() * (zMax * 0.7f)), 0.30f + (float)rng.NextDouble() * 0.12f);
        for (int i = 0; i < 6; i++)
            KraftRib(new Vector3(Side(rng, 2.45f, 2.95f), 1.15f, 2.5f + i * (zMax * 0.1f)), 1.9f);
        for (int i = 0; i < 5; i++)
            BurlapPathStrip(new Vector3(0f, 0.02f, 2f + i * (zMax / 6f)), 1.05f);
        for (int i = 0; i < Mathf.Min(8, roast); i++)
            ContactShadow(RandPos(rng, 1.5f, zMax, 1.8f), 0.11f);
    }

    // ——— L5 Finale Escape (densest / bossBeat): full LOOK set vocabulary ———
    void SpawnFinaleEscape(System.Random rng, float dens, float zMax, bool boss)
    {
        float boost = boss ? 1.15f : 1f;
        int planks = Mathf.RoundToInt(Mathf.Lerp(8, 14, dens) * boost);
        int pines = Mathf.RoundToInt(Mathf.Lerp(5, 9, dens) * boost);
        // k674 densify (18.6k blooms / 14k roast / 9k orbs / 16k moss) was unplayable; LevelDensitySmoke
        // needs L5 > L2 (5220 props) so the finale stays the densest set at ≈5.5k props.
        int blooms = Mathf.RoundToInt(Mathf.Lerp(22, 300, dens) * boost);
        int roast = Mathf.RoundToInt(Mathf.Lerp(18, 180, dens) * boost);
        int orbs = Mathf.RoundToInt(Mathf.Lerp(14, 130, dens) * boost);
        int moss = Mathf.RoundToInt(Mathf.Lerp(16, 210, dens) * boost);
        int grass = Mathf.RoundToInt(Mathf.Lerp(8, 16, dens) * boost);
        int grinders = Mathf.RoundToInt(Mathf.Lerp(2, 4, dens) * boost);

        float bridgeZ0 = 5f;
        float bridgeLen = Mathf.Min(14f, zMax * 0.4f);
        for (int i = 0; i < planks; i++)
        {
            float t = i / (float)Mathf.Max(1, planks - 1);
            float z = bridgeZ0 + t * bridgeLen;
            float arch = Mathf.Sin(t * Mathf.PI) * 0.42f;
            BridgePlank(new Vector3(0f, 0.08f + arch, z), 1.15f);
            if (i % 2 == 0)
            {
                BridgePost(new Vector3(-0.65f, 0.38f + arch, z), 0.6f);
                BridgePost(new Vector3(0.65f, 0.38f + arch, z), 0.6f);
                BridgeRail(new Vector3(0f, 0.58f + arch, z), 1.2f);
            }
        }
        for (int i = 0; i < grass; i++)
            BridgeGrass(RandPos(rng, bridgeZ0 - 1f, bridgeZ0 + bridgeLen + 2f, 1.5f), 0.08f + (float)rng.NextDouble() * 0.09f);
        for (int i = 0; i < pines; i++)
            PineLite(new Vector3(Side(rng, 1.9f, 2.7f), 0.7f + (float)rng.NextDouble() * 0.45f, 2.5f + (float)rng.NextDouble() * (zMax * 0.85f)), 0.75f + (float)rng.NextDouble() * 0.5f);
        for (int i = 0; i < blooms; i++)
            FlowerBloom(new Vector3(((float)rng.NextDouble() - 0.5f) * 2.5f, 1.05f + (float)rng.NextDouble() * 0.7f, 2.5f + (float)rng.NextDouble() * (zMax - 3f)), 0.10f + (float)rng.NextDouble() * 0.12f, i);
        for (int i = 0; i < grinders; i++)
        {
            float z = 7f + i * (zMax * 0.22f);
            CardboardGrinder(new Vector3((i % 2 == 0) ? -1.45f : 1.45f, 0.38f, z), 0.9f + (float)rng.NextDouble() * 0.25f);
        }
        for (int i = 0; i < roast; i++)
            RoastBean(RandPos(rng, 1.5f, zMax, 1.9f), 0.06f + (float)rng.NextDouble() * 0.08f, (float)rng.NextDouble() * 360f);
        for (int i = 0; i < orbs; i++)
        {
            var p = RandPos(rng, 2f, zMax, 2.0f);
            p.y = 0.4f + (float)rng.NextDouble() * 1.0f;
            YarnOrb(p, 0.09f + (float)rng.NextDouble() * 0.13f, i % 2 == 0);
            if (i % 2 == 0) HangString(p + Vector3.up * 0.4f, 0.45f + (float)rng.NextDouble() * 0.2f);
        }
        for (int i = 0; i < moss; i++)
            MossBall(RandPos(rng, 1.0f, zMax, 2.3f), 0.09f + (float)rng.NextDouble() * 0.16f);
        // LOOK finale densify: burlap runner + kraft flutes/ribs + yarn-wrap posts + succulents
        for (int i = 0; i < 8; i++)
            BurlapPathStrip(new Vector3(0f, 0.02f, 1.5f + i * (zMax / 9f)), 1.2f);
        for (int i = 0; i < planks; i += 2)
        {
            float t = i / (float)Mathf.Max(1, planks - 1);
            float z = bridgeZ0 + t * bridgeLen;
            float arch = Mathf.Sin(t * Mathf.PI) * 0.42f;
            KraftFlute(new Vector3(-0.4f, 0.12f + arch, z), 0.5f);
            KraftFlute(new Vector3(0.4f, 0.12f + arch, z), 0.5f);
            YarnWrapPost(new Vector3(-0.65f, 0.38f + arch, z), 0.6f);
            YarnWrapPost(new Vector3(0.65f, 0.38f + arch, z), 0.6f);
        }
        for (int i = 0; i < 8; i++)
            KraftRib(new Vector3(Side(rng, 2.5f, 3.1f), 1.25f, 2f + i * (zMax * 0.09f)), 2.1f);
        for (int i = 0; i < 4; i++)
            SucculentPot(new Vector3(Side(rng, 1.2f, 1.7f), 0.08f, 4f + (float)rng.NextDouble() * (zMax * 0.55f)), 0.24f);
        // bossBeat: extra hanging orb string cluster near goal
        if (boss)
        {
            float gz = zMax - 2f;
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * 0.75f, 0.9f + (i % 3) * 0.15f, gz + Mathf.Sin(a) * 0.55f);
                YarnOrb(p, 0.14f, i % 2 == 0);
                HangString(p + Vector3.up * 0.5f, 0.55f);
            }
            FeltCloud(new Vector3(-1.2f, 2.0f, gz * 0.5f), 0.35f);
            FeltCloud(new Vector3(1.3f, 2.15f, gz * 0.55f), 0.4f);
            FeltCloud(new Vector3(0f, 2.25f, gz * 0.35f), 0.42f);
        }
    }

    // ——— Prop builders (BuildEscapeScene vocabulary + PaintTex LOOK kit) ———

    // scope-L1L3 p10: kraft tint cohesion + hop deck thickness (count-neutral; plank count HELD)
    void BridgePlank(Vector3 pos, float width, Color? tint = null)
    {
        var go = Prim(PrimitiveType.Cube, "Bridge Plank", pos, new Vector3(width, 0.11f, 0.54f));
        PaintTex(go, "Look/tex-kraft-corrugated", tint ?? Kraft, 0.84f);
    }

    void BridgePost(Vector3 pos, float h)
    {
        var go = Prim(PrimitiveType.Cylinder, "Bridge Post", pos, new Vector3(0.08f, h * 0.5f, 0.08f));
        PaintTex(go, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    void BridgeRail(Vector3 pos, float width)
    {
        var go = Prim(PrimitiveType.Cube, "Bridge Rail", pos, new Vector3(width, 0.05f, 0.06f));
        PaintTex(go, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    void BridgeGrass(Vector3 pos, float s)
    {
        var go = Prim(PrimitiveType.Sphere, "Bridge Grass", pos + Vector3.up * (s * 0.4f), Vector3.one * s);
        NoCol(go);
        PaintTex(go, "Look/tex-yarn-green", FeltPine, 0.93f);
    }

    void MossBall(Vector3 pos, float s)
    {
        var go = Prim(PrimitiveType.Sphere, "Moss", pos + Vector3.up * (s * 0.45f), Vector3.one * s);
        NoCol(go);
        PaintTex(go, "Look/tex-yarn-green", new Color(0.96f, 1.00f, 0.90f), 0.90f); // olive felt moss (285 foreground balls)
        // plates: one fuzzy olive ball. The four halo spheres are sunk into the main sphere (count-neutral)
        // so they only soften the silhouette instead of reading as a cluster.
        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f;
            var h = Prim(PrimitiveType.Sphere, "Moss Halo",
                pos + new Vector3(Mathf.Cos(a) * s * 0.30f, s * 0.50f, Mathf.Sin(a) * s * 0.30f),
                Vector3.one * (s * 0.50f));
            NoCol(h);
            PaintTex(h, "Look/tex-yarn-green", new Color(1.06f, 1.06f, 0.96f), 0.93f);
        }
    }

    void RoastBean(Vector3 pos, float scale, float yaw)
    {
        var rootGo = new GameObject("Roast Bean");
        rootGo.transform.SetParent(root, false);
        rootGo.transform.position = pos + Vector3.up * (scale * 0.6f);
        rootGo.transform.rotation = Quaternion.Euler(0f, yaw, 12f);

        // glossy dark roast bean (plates): tex-roast-bean carries the lighter crease line
        var l = PrimChild(rootGo.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(-0.45f * scale, 0f, 0f), new Vector3(scale, scale * 0.72f, scale * 0.85f));
        PaintTex(l, "Look/tex-roast-bean", Color.white, 0.38f);
        var r = PrimChild(rootGo.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(0.45f * scale, 0f, 0f), new Vector3(scale, scale * 0.72f, scale * 0.85f));
        PaintTex(r, "Look/tex-roast-bean", new Color(0.94f, 0.94f, 0.94f), 0.38f);
        var c = PrimChild(rootGo.transform, PrimitiveType.Capsule, "Roast Crease", Vector3.zero, new Vector3(scale * 0.18f, scale * 0.55f, scale * 0.18f));
        PaintTex(c, "Look/tex-roast-bean", new Color(0.55f, 0.50f, 0.46f), 0.6f);
    }

    void YarnOrb(Vector3 pos, float s, bool green)
    {
        var go = Prim(PrimitiveType.Sphere, "Yarn Ball", pos, Vector3.one * s);
        NoCol(go);
        PaintTex(go, green ? "Look/tex-yarn-green" : "Look/tex-yarn-fiber", green ? FeltPine : Yarn, 0.92f);
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * Mathf.PI * 2f;
            var bump = Prim(PrimitiveType.Sphere, "Yarn Bump",
                pos + new Vector3(Mathf.Cos(a) * s * 0.55f, Mathf.Sin(a * 1.3f) * s * 0.4f, Mathf.Sin(a) * s * 0.55f),
                Vector3.one * (s * 0.28f));
            NoCol(bump);
            PaintTex(bump, green ? "Look/tex-yarn-green" : "Look/tex-yarn-fiber", green ? FeltPine : Yarn, 0.9f);
        }
    }

    void HangString(Vector3 top, float len)
    {
        var go = Prim(PrimitiveType.Cylinder, "Hang String", top - Vector3.up * (len * 0.5f), new Vector3(0.015f, len * 0.5f, 0.015f));
        NoCol(go);
        PaintTex(go, "Look/tex-yarn-fiber", Stitch, 0.7f);
    }

    void PathYarnEdge(Vector3 pos, float s)
    {
        var go = Prim(PrimitiveType.Sphere, "Path Yarn Edge", pos, Vector3.one * s);
        NoCol(go);
        PaintTex(go, "Look/tex-yarn-fiber", Stitch, 0.55f); // p10 stitch-family cohesion
    }

    // Imagine felt pine: stacked flattened tiers (LOOK 285), brown pinecone dots. Same 16 prims as before.
    void PineLite(Vector3 pos, float scale)
    {
        var trunk = Prim(PrimitiveType.Cylinder, "Pine Trunk", pos + Vector3.up * (0.08f * scale), new Vector3(0.09f * scale, 0.22f * scale, 0.09f * scale));
        PaintTex(trunk, "Look/tex-felt-brown", FeltDark, 0.90f);
        // top tip
        var crown = Prim(PrimitiveType.Sphere, "Pine Crown", pos + Vector3.up * (1.18f * scale), new Vector3(0.22f, 0.26f, 0.22f) * scale);
        NoCol(crown);
        PaintTex(crown, "Look/tex-yarn-green", FeltPine, 0.93f);
        // 5 tiers, widest at the bottom
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;                       // 0 bottom .. 1 top
            float w = Mathf.Lerp(0.78f, 0.30f, t);
            float y = Mathf.Lerp(0.30f, 1.02f, t);
            var tier = Prim(PrimitiveType.Sphere, "Pine Fuzz",
                pos + Vector3.up * (y * scale),
                new Vector3(w, w * 0.42f, w) * scale);
            NoCol(tier);
            PaintTex(tier, "Look/tex-yarn-green", i % 2 == 0 ? FeltPine : new Color(1.08f, 1.08f, 0.98f), 0.94f);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * Mathf.PI * 2f + 0.4f;
            float r = Mathf.Lerp(0.30f, 0.20f, i / 2f);
            var cone = Prim(PrimitiveType.Sphere, "Pinecone",
                pos + new Vector3(Mathf.Cos(a) * r * scale, (0.42f + i * 0.20f) * scale, Mathf.Sin(a) * r * scale),
                Vector3.one * (0.09f * scale));
            NoCol(cone);
            PaintTex(cone, "Look/tex-felt-brown", FeltDark, 0.85f);
        }
        // smaller pinecone dots on the upper tiers
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 1.1f;
            float r = Mathf.Lerp(0.26f, 0.14f, i / 5f);
            var bump = Prim(PrimitiveType.Sphere, "Pine Crochet Bump",
                pos + new Vector3(Mathf.Cos(a) * r * scale, (0.52f + i * 0.09f) * scale, Mathf.Sin(a) * r * scale),
                Vector3.one * (0.06f * scale));
            NoCol(bump);
            PaintTex(bump, "Look/tex-felt-brown", new Color(0.62f, 0.52f, 0.44f), 0.88f);
        }
    }

    void FlowerBloom(Vector3 pos, float s, int seed)
    {
        Color[] blooms =
        {
            new Color(0.92f, 0.45f, 0.55f),
            new Color(0.95f, 0.75f, 0.35f),
            new Color(0.55f, 0.70f, 0.95f),
            new Color(0.75f, 0.45f, 0.85f),
            new Color(0.95f, 0.60f, 0.40f)
        };
        Color col = blooms[Mathf.Abs(seed) % blooms.Length];
        var center = Prim(PrimitiveType.Sphere, "Bloom Center", pos, Vector3.one * (s * 0.45f));
        NoCol(center);
        PaintTex(center, "Look/tex-yarn-flower", Stitch, 0.5f);
        for (int p = 0; p < 6; p++)
        {
            float a = p / 6f * Mathf.PI * 2f;
            var petal = Prim(PrimitiveType.Sphere, "Flower Petal",
                pos + new Vector3(Mathf.Cos(a) * s * 0.55f, Mathf.Sin(a * 0.5f) * s * 0.1f, Mathf.Sin(a) * s * 0.55f),
                new Vector3(s * 0.55f, s * 0.28f, s * 0.55f));
            NoCol(petal);
            PaintTex(petal, "Look/tex-yarn-flower", blooms[(seed + p) % blooms.Length], 0.9f);
        }
        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f + 0.2f;
            var loop = Prim(PrimitiveType.Sphere, "Bloom Yarn Loop",
                pos + new Vector3(Mathf.Cos(a) * s * 0.35f, s * 0.15f, Mathf.Sin(a) * s * 0.35f),
                Vector3.one * (s * 0.18f));
            NoCol(loop);
            PaintTex(loop, "Look/tex-yarn-flower", col, 0.88f);
        }
    }

    void CardboardGrinder(Vector3 pos, float scale)
    {
        var body = Prim(PrimitiveType.Cube, "Cardboard Grinder", pos, new Vector3(0.55f * scale, 0.7f * scale, 0.45f * scale));
        PaintTex(body, "Look/tex-kraft-corrugated", Kraft, 0.9f);
        var hopper = Prim(PrimitiveType.Cylinder, "Grinder Hopper", pos + Vector3.up * (0.5f * scale), new Vector3(0.28f * scale, 0.18f * scale, 0.28f * scale));
        PaintTex(hopper, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
        for (int i = 0; i < 2; i++)
        {
            var drawer = Prim(PrimitiveType.Cube, "Grinder Drawer",
                pos + new Vector3(0f, (-0.15f + i * 0.22f) * scale, 0.24f * scale),
                new Vector3(0.42f * scale, 0.14f * scale, 0.08f * scale));
            PaintTex(drawer, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
            var knob = Prim(PrimitiveType.Sphere, "Drawer Knob",
                drawer.transform.position + new Vector3(0f, 0f, 0.06f * scale),
                Vector3.one * (0.06f * scale));
            NoCol(knob);
            PaintTex(knob, "Look/tex-felt-brown", FeltDark, 0.7f);
        }
        var crank = Prim(PrimitiveType.Cylinder, "Grinder Crank",
            pos + new Vector3(0.38f * scale, 0.15f * scale, 0f),
            new Vector3(0.04f * scale, 0.22f * scale, 0.04f * scale));
        crank.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        PaintTex(crank, "Look/tex-kraft-corrugated", FeltDark, 0.75f);
        var crankKnob = Prim(PrimitiveType.Sphere, "Crank Knob",
            pos + new Vector3(0.55f * scale, 0.15f * scale, 0f),
            Vector3.one * (0.08f * scale));
        NoCol(crankKnob);
        PaintTex(crankKnob, "Look/tex-yarn-fiber", Yarn, 0.7f);
    }


    void BurlapPathStrip(Vector3 pos, float width)
    {
        var go = Prim(PrimitiveType.Cube, "Burlap Path", pos, new Vector3(width, 0.04f, 0.55f));
        NoCol(go);
        PaintTex(go, "Look/tex-burlap", new Color(1.02f, 1.00f, 0.96f), 0.86f); // runner strip: same burlap as the floor, a hair lighter
    }

    void KraftFlute(Vector3 pos, float len)
    {
        var go = Prim(PrimitiveType.Cube, "Kraft Flute", pos, new Vector3(0.06f, 0.05f, len));
        NoCol(go);
        PaintTex(go, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    void KraftRib(Vector3 pos, float h)
    {
        var go = Prim(PrimitiveType.Cube, "Kraft Rib", pos, new Vector3(0.22f, h, 0.18f));
        NoCol(go);
        PaintTex(go, "Look/tex-kraft-corrugated", Kraft, 0.90f);
        // alternating thin flute
        var flute = Prim(PrimitiveType.Cube, "Kraft Rib Flute", pos + new Vector3(0.12f, 0f, 0f), new Vector3(0.05f, h * 0.95f, 0.08f));
        NoCol(flute);
        PaintTex(flute, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    void YarnWrapPost(Vector3 pos, float h)
    {
        // p10: yarn rings match Yarn/Stitch craft family (still 4 rings — count-neutral)
        for (int i = 0; i < 4; i++)
        {
            float y = pos.y - h * 0.35f + i * (h * 0.22f);
            var ring = Prim(PrimitiveType.Cylinder, "Yarn Wrap", new Vector3(pos.x, y, pos.z), new Vector3(0.118f, 0.034f, 0.118f));
            NoCol(ring);
            PaintTex(ring, "Look/tex-yarn-fiber", Yarn, 0.78f);
        }
    }

    void ContactShadow(Vector3 pos, float s)
    {
        // p10: tighter foot ellipse — hop contact read without densify thrash (1 prim)
        // real shadows now land on the burlap (FeltFiber receives); keep this as a faint felt shade, not a black disc
        var go = Prim(PrimitiveType.Cylinder, "Contact Shadow", pos, new Vector3(s * 1.02f, 0.004f, s * 0.72f));
        NoCol(go);
        PaintTex(go, "Look/tex-burlap", new Color(0.72f, 0.68f, 0.62f), 0.98f);
    }

    void SucculentPot(Vector3 pos, float s)
    {
        var pot = Prim(PrimitiveType.Cylinder, "Succulent Pot", pos + Vector3.up * (s * 0.35f), new Vector3(s * 0.9f, s * 0.35f, s * 0.9f));
        PaintTex(pot, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f;
            var leaf = Prim(PrimitiveType.Sphere, "Succulent Leaf",
                pos + new Vector3(Mathf.Cos(a) * s * 0.35f, s * 0.75f, Mathf.Sin(a) * s * 0.35f),
                new Vector3(s * 0.28f, s * 0.45f, s * 0.22f));
            NoCol(leaf);
            PaintTex(leaf, "Look/tex-yarn-green", FeltPine, 0.93f);
        }
    }

    void FeltCloud(Vector3 pos, float s)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f;
            var c = Prim(PrimitiveType.Sphere, "Felt Cloud",
                pos + new Vector3(Mathf.Cos(a) * s * 0.45f, (i % 2) * s * 0.15f, Mathf.Sin(a) * s * 0.3f),
                Vector3.one * (s * (0.55f + (i % 3) * 0.1f)));
            NoCol(c);
            PaintTex(c, "Look/tex-felt-cream", Cloud, 0.96f);
        }
    }

    // ——— Helpers ———

    Vector3 RandPos(System.Random rng, float z0, float z1, float xSpan)
    {
        float z = z0 + (float)rng.NextDouble() * Mathf.Max(0.5f, z1 - z0);
        float x = ((float)rng.NextDouble() - 0.5f) * xSpan;
        return new Vector3(x, 0.05f, z);
    }

    // scope-L1L3 p8–p10: side-biased placement — clears center hop corridor (count-neutral vs RandPos)
    Vector3 PlayLanePos(System.Random rng, float z0, float z1, float xSpan, float clearHalf)
    {
        float z = z0 + (float)rng.NextDouble() * Mathf.Max(0.5f, z1 - z0);
        float half = Mathf.Max(0.05f, xSpan * 0.5f);
        float clear = Mathf.Min(clearHalf, half * 0.92f);
        float mag = clear + (float)rng.NextDouble() * Mathf.Max(0.04f, half - clear);
        float x = rng.NextDouble() < 0.5 ? -mag : mag;
        return new Vector3(x, 0.05f, z);
    }

    float Side(System.Random rng, float minAbs, float maxAbs)
    {
        float v = minAbs + (float)rng.NextDouble() * (maxAbs - minAbs);
        return rng.NextDouble() < 0.5 ? -v : v;
    }

    GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (type == PrimitiveType.Sphere) CraftMesh.UseLowSphere(go); // dressing props: 120 verts, not 515
        go.isStatic = true;
        return go;
    }

    GameObject PrimChild(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        if (type == PrimitiveType.Sphere) CraftMesh.UseLowSphere(go);
        go.isStatic = true;
        NoCol(go);
        return go;
    }

    static void NoCol(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Object.Destroy(c);
            else Object.DestroyImmediate(c);
        }
    }

    /// Runtime PaintTex — LOOK kit from Resources/Look, SHARED per (texture, tint, roughness) via CraftMesh.
    /// One material per look instead of one per prop → props batch.
    static void PaintTex(GameObject go, string resourcePath, Color tint, float roughness)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        string path = resourcePath ?? "";
        bool yarnish = path.Contains("yarn") || path.Contains("green") || path.Contains("flower");
        bool roast = path.Contains("roast");
        float bump = 1.0f;
        if (path.Contains("burlap")) bump = 1.4f;
        else if (path.Contains("kraft")) bump = 0.8f;
        else if (yarnish) bump = 1.2f;
        else if (roast) bump = 0.6f;
        float sheen = 0.12f;
        if (yarnish) sheen = 0.16f;
        else if (path.Contains("burlap")) sheen = 0.08f;
        else if (path.Contains("kraft")) sheen = 0.06f;
        else if (roast) sheen = 0.30f; // glossy bean
        float wrap = 0.62f;
        if (path.Contains("burlap")) wrap = 0.70f;
        else if (path.Contains("kraft")) wrap = 0.55f;
        else if (yarnish) wrap = 0.66f;
        else if (roast) wrap = 0.50f;
        float tile = path.Contains("burlap") ? 10f : (path.Contains("kraft") ? 7.5f : (yarnish ? 3.0f : (roast ? 1.0f : 1.20f)));
        var tileV = new Vector2(tile, tile);
        string key = CraftMesh.Key(path, tint, tileV, bump, sheen, wrap, roughness, 0f);
        var mat = CraftMesh.Shared(key, () =>
        {
            var tex = Resources.Load<Texture2D>(path);
            return CraftMesh.Felt(tex, tint, tileV, bump, sheen, wrap, roughness);
        });
        r.sharedMaterial = mat;
    }
}

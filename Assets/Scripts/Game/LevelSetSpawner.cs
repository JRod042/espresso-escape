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

    /// Low floor dressing that would float in mid-air over a hop gap (tucked below the stage, still counted).
    static readonly string[] FloorClutter =
    {
        "Moss", "Roast Bean", "Contact Shadow", "Grass Blade", "Bridge Grass", "Felt Petal", "Yarn Ball", "Yarn Bump",
        "Burlap Path", "Kraft Flute", "Path Yarn Edge", "Sage"
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
            // Count-held cube bridge. ImagineSet draws the arch; these must not clip it or block the bean.
            if (n.StartsWith("Bridge Plank") || n.StartsWith("Bridge Post") || n.StartsWith("Bridge Rail"))
            {
                var rend = t.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
                var col = t.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
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
            // floor dressing over a hop gap would float in mid-air (the gap cuts the whole lane depth): tuck it away (still counted)
            if (p.y < 0.45f && LevelRunner.IsOverGap(p.z) && StartsWithAny(n, FloorClutter))
                t.position = new Vector3(p.z, -6f, z);
        }
    }

    LevelCatalog.LevelDef _def;
    int _lastSpawnCount;

    public int LastSpawnCount => _lastSpawnCount;

    /// Combine spawned props into static batches (play mode only; proofs run in edit mode).
    public bool staticBatch = true;

    /// Minimum dressed children per level (L1..L5). L4 and L5 carry their raised-slab scatter, so both
    /// sit above L2 / L3 (PR-25 review: L4 used to be the thinnest set at 2254).
    static readonly int[] MinPropsPerLevel = { 1200, 4800, 5000, 5400, 6000 };

    /// Removes every spawned prop (L1 in the handmade scene uses the approved OpeningYard set instead).
    public void ClearSet()
    {
        EnsureRoot();
        ClearChildren();
        _lastSpawnCount = 0;
    }

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
        // Floor check, not an exact count: the lane remap and the edit-mode / play-mode gap lists move a few
        // hundred children between runs (L2 measured 5048 in edit mode against the old exact 5220).
        int floor = MinPropsPerLevel[id - 1];
        if (_lastSpawnCount < floor)
            Debug.LogError($"[LevelSetSpawner] L{id} COUNT {_lastSpawnCount} < floor {floor}");
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
        // Destroy() waits until end of frame, so a second ApplyLevel in the same frame
        // counted the old children plus the new ones (L1 1346 × 2 = 2692).
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    // ——— L1 Café Yard = 285.mp4 ———
    // The clip (0–6 s): three felt pines on the left, red pompom tree + a small pine on the right, one moss ball
    // behind the bean's path and two in the foreground right, four roast beans on the burlap, a pale succulent
    // bottom-left; three felt clouds on twine up top (those come from the camera-following Backdrop twine).
    // That ≈4 m vignette repeats twice along the run and a pine grove frames the goal. Hero props sit off the fixed
    // L1 gaps (LevelRunner: 7.0–8.05 and 12.0–13.1); the small moss/roast scatter over a gap is tucked by Remap.
    // Child count HELD at 1346 (LEVEL_SMOKE / PROOF_SMOKE_L1L3):
    //   2 vignettes × (4 pines 64 + pompom tree 13 + 3 moss+shadow 18 + 4 roast+shadow 8 + succulent 13 = 116) = 232
    //   goal grove 3 pines 48 + pompom tree 13 + succulent 13 = 74
    //   scatter 120 moss+shadow 720 + 160 roast+shadow 320 = 1040           → 232 + 74 + 1040 = 1346
    void SpawnCafeYard(System.Random rng, float dens, float zMax)
    {
        void Shadow(Vector3 p, float s) => ContactShadow(new Vector3(p.x, 0.012f, p.z), s);
        float[] vignette = { 3.5f, 8.45f };
        for (int k = 0; k < vignette.Length; k++)
        {
            float b = vignette[k];
            float yaw = k * 40f;
            // three felt pines left (tall → small, stepped back toward the kraft wall) — 285 frames, left third
            PineLite(new Vector3(-1.55f, 0f, b + 0.00f), 1.05f);
            PineLite(new Vector3(-2.25f, 0f, b + 0.70f), 0.88f);
            PineLite(new Vector3(-1.30f, 0f, b + 1.35f), 0.68f);
            // red pompom tree right of the bean, small pine beside it (285 right third)
            RedPompomTree(new Vector3(-1.45f, 0f, b + 2.45f), 0.95f);
            PineLite(new Vector3(-2.05f, 0f, b + 3.15f), 0.60f);
            // moss balls: one behind the bean's path, two in the foreground right
            Vector3 m0 = new Vector3(-0.85f, 0f, b + 2.70f), m1 = new Vector3(0.95f, 0f, b + 2.95f), m2 = new Vector3(1.45f, 0f, b + 3.30f);
            MossBall(m0, 0.27f); Shadow(m0, 0.30f);
            MossBall(m1, 0.22f); Shadow(m1, 0.25f);
            MossBall(m2, 0.16f); Shadow(m2, 0.18f);
            // roast beans on the burlap: foreground left, behind the path, mid, foreground right
            Vector3 r0 = new Vector3(1.15f, 0f, b + 0.30f), r1 = new Vector3(-0.95f, 0f, b + 1.85f),
                    r2 = new Vector3(0.90f, 0f, b + 2.25f), r3 = new Vector3(1.65f, 0f, b + 3.40f);
            RoastBean(r0, 0.10f, 20f + yaw); Shadow(r0, 0.12f);
            RoastBean(r1, 0.09f, 115f + yaw); Shadow(r1, 0.11f);
            RoastBean(r2, 0.11f, 205f + yaw); Shadow(r2, 0.13f);
            RoastBean(r3, 0.09f, 300f + yaw); Shadow(r3, 0.11f);
            // pale felt succulent bottom-left of the vignette (foreground)
            Succulent(new Vector3(1.30f, 0f, b - 0.35f), 0.26f);
        }
        // goal grove: the pine trio + pompom tree again behind the goal post (goal x = 14)
        const float g = 13.6f;
        PineLite(new Vector3(-1.60f, 0f, g), 1.00f);
        PineLite(new Vector3(-2.30f, 0f, g + 0.80f), 0.85f);
        PineLite(new Vector3(-1.35f, 0f, g + 1.60f), 0.65f);
        RedPompomTree(new Vector3(-1.50f, 0f, g + 2.60f), 0.90f);
        Succulent(new Vector3(1.25f, 0f, g - 0.10f), 0.24f);
        // small moss + roast scatter in both bands keeps the burlap busy like the plates without crowding the lane
        for (int i = 0; i < 120; i++)
        {
            var p = PlayLanePos(rng, 1.0f, zMax + 4.5f, 3.6f, 1.05f);
            MossBall(p, 0.08f + (float)rng.NextDouble() * 0.07f);
            Shadow(p, 0.12f);
        }
        for (int i = 0; i < 160; i++)
        {
            var p = PlayLanePos(rng, 1.0f, zMax + 4.5f, 3.8f, 0.85f);
            RoastBean(p, 0.06f + (float)rng.NextDouble() * 0.035f, (float)rng.NextDouble() * 360f);
            Shadow(p, 0.09f);
        }
        // DriveLook/imagine/285.jpg and frames/vid-285-630.png: the pines and the one red tree
        // are this felt set. Every object stays so the L1 child count stays 1346.
    }

    // ——— L2 Kraft Bridge = gen-2d7dcad9.mp4 / set-01..06 ———
    // Left → right in the clip: two yarn balls hanging top-left, big round felt tree with dot pattern, the cardboard
    // grinder (crank turning) with a tan yarn ball at its foot, two roast beans + a grass tuft bottom-left, more round
    // trees behind, the arched kraft bridge in the middle sitting ON the burlap (plank deck, three post pairs, two rails,
    // burlap mat and grass at both feet — no pit under the arch), the big red/orange felt flower tree right of centre
    // dropping petals onto the burlap, round trees right, a tan yarn ball + two roast beans + grass bottom-right,
    // two yarn balls hanging top-right, cloud clusters stuck on the kraft wall.
    // Fixed L2 gaps are 4.20–5.40 and 18.40–19.75. The bridge block is [5.40, 18.40]. Hero props sit off both gaps.
    // Child count HELD at 5220 (LEVEL_SMOKE / PROOF_SMOKE_L1L3, L5 > L2):
    //   bridge 12 planks + 6 posts + 22 rail segments = 40 · grinder 8 · 2 flower trees 174 · 40 round trees 400
    //   10 hanging balls 50 · 2 floor balls+shadow 10 · 4 clouds 20 · 4 hero roast+shadow 8 · 4 foot tufts 20
    //   scatter 320 tufts 1600 + 300 moss+shadow 1800 + 500 roast+shadow 1000 + 90 petals   → 5220
    void SpawnKraftBridge(System.Random rng, float dens, float zMax)
    {
        void Shadow(Vector3 p, float s) => ContactShadow(new Vector3(p.x, 0.012f, p.z), s);
        float x0 = LevelRunner.BridgeX0, len = LevelRunner.BridgeLen, rise = LevelRunner.BridgeArch, deckY = LevelRunner.BridgeDeckY;
        const int planks = 12;
        float Arch(float t) => Mathf.Sin(t * Mathf.PI) * rise;
        float Slope(float t) => rise * Mathf.PI / len * Mathf.Cos(t * Mathf.PI);

        // deck: overlapping corrugated planks tilted along the arch → a continuous walkable curve (colliders kept)
        for (int i = 0; i < planks; i++)
        {
            float t = i / (float)(planks - 1);
            BridgePlank(new Vector3(0f, deckY + Arch(t), x0 + t * len), 1.06f,
                i % 2 == 0 ? Kraft : new Color(0.80f, 0.72f, 0.62f), -Mathf.Atan(Slope(t)) * Mathf.Rad2Deg);
        }
        // three post pairs (feet + crown); back posts hug the deck edge, front posts stand on the foreground rail line
        float deckTop = deckY + 0.055f;
        const float postH = 0.50f;
        float[] postT = { 0f, 0.5f, 1f };
        for (int i = 0; i < postT.Length; i++)
        {
            float y = deckTop + Arch(postT[i]) + postH * 0.5f, z = x0 + postT[i] * len;
            BridgePost(new Vector3(0.56f, y, z), postH);
            BridgePost(new Vector3(1.00f, y, z), postH);
        }
        // two foreground rails following the arch, one short segment per plank bay
        float[] railH = { 0.46f, 0.26f };
        for (int r = 0; r < railH.Length; r++)
            for (int i = 0; i < planks - 1; i++)
            {
                float ta = i / (float)(planks - 1), tb = (i + 1) / (float)(planks - 1), tm = (ta + tb) * 0.5f;
                float dz = (tb - ta) * len, dy = Arch(tb) - Arch(ta);
                BridgeRail(new Vector3(0f, deckTop + Arch(tm) + railH[r], x0 + tm * len),
                    Mathf.Sqrt(dz * dz + dy * dy) + 0.05f, Mathf.Atan2(dy, dz) * Mathf.Rad2Deg);
            }
        // grass tufts at both bridge feet
        GrassTuft(new Vector3(0.80f, 0f, x0 - 0.55f), 0.95f);
        GrassTuft(new Vector3(-0.80f, 0f, x0 - 0.45f), 0.85f);
        GrassTuft(new Vector3(0.85f, 0f, x0 + len + 0.50f), 0.90f);
        GrassTuft(new Vector3(-0.75f, 0f, x0 + len + 0.60f), 0.80f);

        // left of the bridge: grinder + its yarn ball, two roast beans + tuft bottom-left, round trees behind
        CardboardGrinder(new Vector3(-1.30f, 0.44f, 9.55f), 1.25f);
        Vector3 yb0 = new Vector3(-1.00f, 0.26f, 7.6f);
        YarnOrb(yb0, 0.26f, false); Shadow(yb0, 0.30f);
        Vector3 rl0 = new Vector3(1.20f, 0f, 7.3f), rl1 = new Vector3(1.45f, 0f, 7.7f);
        RoastBean(rl0, 0.11f, 30f); Shadow(rl0, 0.13f);
        RoastBean(rl1, 0.10f, 140f); Shadow(rl1, 0.12f);
        RoundFeltTree(new Vector3(-2.00f, 0f, 1.70f), 1.50f, 0);
        RoundFeltTree(new Vector3(-2.40f, 0f, 9.3f), 1.15f, 1);
        RoundFeltTree(new Vector3(-2.70f, 0f, 10.3f), 0.80f, 2);
        // right of the bridge: flower tree, round trees, yarn ball + roast beans bottom-right
        FlowerTree(new Vector3(-1.50f, 0f, 14.6f), 1.40f);
        RoundFeltTree(new Vector3(-2.50f, 0f, 13.6f), 0.85f, 3);
        RoundFeltTree(new Vector3(-2.10f, 0f, 16.0f), 1.30f, 4);
        RoundFeltTree(new Vector3(-1.90f, 0f, 17.4f), 1.00f, 5);
        Vector3 yb1 = new Vector3(-1.05f, 0.26f, 15.7f); // "Yarn Ball" is always staged behind, so it sits just right of the flower trunk
        YarnOrb(yb1, 0.26f, false); Shadow(yb1, 0.30f);
        Vector3 rr0 = new Vector3(1.10f, 0f, 15.0f), rr1 = new Vector3(1.35f, 0f, 15.4f);
        RoastBean(rr0, 0.11f, 220f); Shadow(rr0, 0.13f);
        RoastBean(rr1, 0.10f, 330f); Shadow(rr1, 0.12f);
        // second flower tree frames the goal (goal x = 22)
        FlowerTree(new Vector3(-1.60f, 0f, 21.3f), 1.20f);
        // a row of smaller round felt trees along the wall for the rest of the run (exactly 34, never over a gap)
        for (int i = 0, placed = 0; placed < 34; i++)
        {
            float z = 2.4f + i * 0.62f;
            if (LevelRunner.InFixedGap(LevelRunner.L2Edges, z)) continue;
            RoundFeltTree(new Vector3(-(2.3f + (i % 4) * 0.25f), 0f, z), 0.55f + (i % 5) * 0.11f, i);
            placed++;
        }
        // yarn balls hanging on strings from above the frame (top-left / top-right of the clip, then along the run)
        HangingBall(new Vector3(-1.20f, 0f, 6.4f), 0.90f, 0.30f, false);
        HangingBall(new Vector3(-1.40f, 0f, 7.3f), 0.60f, 0.24f, false);
        HangingBall(new Vector3(-1.30f, 0f, 15.3f), 0.55f, 0.26f, true);
        HangingBall(new Vector3(-1.10f, 0f, 16.9f), 0.85f, 0.28f, false);
        float[] hangZ = { 3.6f, 6.70f, 11.8f, 17.50f, 22.85f, 20.8f };
        for (int i = 0; i < hangZ.Length; i++)
            HangingBall(new Vector3(-1.15f - (i % 3) * 0.15f, 0f, hangZ[i]), 0.55f + (i % 3) * 0.17f, 0.22f + (i % 2) * 0.05f, i % 2 == 1);
        // cloud clusters stuck high on the kraft wall
        FeltCloud(new Vector3(-2.4f, 2.75f, 8.6f), 0.42f);
        FeltCloud(new Vector3(-2.5f, 2.65f, 15.6f), 0.44f);
        FeltCloud(new Vector3(-2.4f, 2.70f, 3.30f), 0.36f);
        FeltCloud(new Vector3(-2.5f, 2.72f, 23.40f), 0.40f);
        // fallen felt petals on the burlap under both flower trees (clip 3.6–6.0 s)
        // 90 petals, fixed rings under the two flower trees, clear of gaps 4.20–5.40 and 18.40–19.75
        for (int i = 0; i < 90; i++)
        {
            float cz = (i % 2 == 0) ? 14.6f : 21.6f;
            float ang = i * 2.399963f;
            float rad = 0.30f + (i % 8) * 0.11f;
            FeltPetal(new Vector3(Mathf.Cos(ang) * rad, 0f, cz + Mathf.Sin(ang) * rad),
                0.07f + (i % 5) * 0.008f, ang * Mathf.Rad2Deg, i % 3 == 0);
        }
        // scatter: grass tufts, small moss and roast beans in both bands along the whole run
        for (int i = 0; i < 320; i++)
            GrassTuft(PlayLanePos(rng, 1.0f, zMax + 4.5f, 3.4f, 0.90f), 0.55f + (float)rng.NextDouble() * 0.45f);
        for (int i = 0; i < 300; i++)
        {
            var p = PlayLanePos(rng, 1.0f, zMax + 4.5f, 3.6f, 1.05f);
            MossBall(p, 0.08f + (float)rng.NextDouble() * 0.08f);
            Shadow(p, 0.12f);
        }
        for (int i = 0; i < 500; i++)
        {
            var p = PlayLanePos(rng, 1.0f, zMax + 4.5f, 3.8f, 0.85f);
            RoastBean(p, 0.06f + (float)rng.NextDouble() * 0.035f, (float)rng.NextDouble() * 360f);
            Shadow(p, 0.09f);
        }
    }

    // ——— L3 Mill Path: kraft steps, wider gaps, same Drive props ———
    // Fixed layout (no Random). Child count HELD at 5370:
    //   2 grinders × (8 + shadow 1 + wrap 4) = 26
    //   5 pines × 16 = 80
    //   40 moss × 5 = 200
    //   34 roast+shadow × 2 = 68
    //   11 yarn orbs × 4 = 44
    //   439 blooms × 11 = 4829
    //   hang strings i%18==0 → 25 (0,18,…,432)
    //   8 pots × 6 = 48
    //   6 ribs × 2 = 12
    //   7 path strips = 7
    //   16 path-yarn edges = 16
    //   3 clouds × 5 = 15
    //   26+80+200+68+44+4829+25+48+12+7+16+15 = 5370
    void SpawnMillPath(System.Random rng, float dens, float zMax)
    {
        var blocks = LevelRunner.L3Edges;
        float[] gz = { 5.50f, 19.60f };
        float[] gx = { -1.65f, 1.65f };
        for (int i = 0; i < 2; i++)
        {
            CardboardGrinder(new Vector3(gx[i], 0.35f, gz[i]), 0.95f);
            ContactShadow(new Vector3(gx[i], 0.01f, gz[i]), 0.50f);
            YarnWrapPost(new Vector3(gx[i], 0.20f, gz[i]), 0.56f);
        }
        float[] pineZ = { 4.40f, 6.60f, 11.40f, 18.20f, 27.20f };
        float[] pineS = { 0.95f, 0.78f, 1.05f, 0.82f, 0.90f };
        for (int i = 0; i < 5; i++)
            PineLite(new Vector3(-2.15f - (i % 2) * 0.25f, 0f, pineZ[i]), pineS[i]);
        for (int i = 0; i < 439; i++)
        {
            float y = 1.20f + U01(i, 3) * 0.45f;
            var bp = Along(blocks, i, 439, 0.85f + U01(i, 1) * 0.50f, y, 1);
            FlowerBloom(bp, 0.12f + U01(i, 4) * 0.08f, i);
            if (i % 18 == 0) HangString(bp + Vector3.up * 0.28f, 0.34f);
        }
        for (int i = 0; i < 34; i++)
        {
            var p = Along(blocks, i, 34, 1.05f, 0.05f, 2);
            RoastBean(p, 0.07f + U01(i, 5) * 0.05f, U01(i, 6) * 360f);
            ContactShadow(p, 0.10f);
        }
        for (int i = 0; i < 40; i++)
            MossBall(Along(blocks, i, 40, 1.15f, 0.05f, 7), 0.10f + U01(i, 8) * 0.10f);
        for (int i = 0; i < 11; i++)
            YarnOrb(Along(blocks, i, 11, 1.20f, 0.75f + U01(i, 9) * 0.35f, 4), 0.12f, i % 2 == 0);
        float[] potZ = { 4.50f, 7.00f, 10.80f, 14.00f, 17.70f, 21.20f, 26.20f, 30.40f };
        for (int i = 0; i < potZ.Length; i++)
            SucculentPot(new Vector3(i % 2 == 0 ? -1.45f : 1.45f, 0.08f, potZ[i]), 0.24f);
        float[] ribZ = { 4.60f, 7.20f, 11.60f, 14.40f, 19.20f, 28.80f };
        for (int i = 0; i < ribZ.Length; i++)
            KraftRib(new Vector3(-2.70f, 1.20f, ribZ[i]), 2.0f);
        float[] stripZ = { 4.80f, 6.80f, 10.60f, 13.60f, 17.80f, 20.80f, 29.00f };
        for (int i = 0; i < stripZ.Length; i++)
            BurlapPathStrip(new Vector3(0f, 0.02f, stripZ[i]), 0.98f);
        float[] edgeZ = { 4.30f, 6.50f, 10.40f, 13.80f, 17.60f, 20.60f, 26.50f, 30.00f };
        for (int i = 0; i < edgeZ.Length; i++)
        {
            PathYarnEdge(new Vector3(-0.68f, 0.04f, edgeZ[i]), 0.062f);
            PathYarnEdge(new Vector3(0.68f, 0.04f, edgeZ[i]), 0.062f);
        }
        FeltCloud(new Vector3(-2.40f, 2.30f, 6.20f), 0.38f);
        FeltCloud(new Vector3(-2.30f, 2.20f, 12.80f), 0.36f);
        FeltCloud(new Vector3(-2.40f, 2.25f, 28.00f), 0.34f);
    }

    // ——— L4 Night Roast: elevated kraft blocks + one yarn cluster ———
    // Fixed layout (no Random). Child count 2254:
    //   yarn cluster 18×4 + 9 strings = 81
    //   4 clouds × 5 = 20
    //   2 flower trees × 87 = 174
    //   8 round trees × 10 = 80
    //   4 pines × 16 = 64
    //   1 grinder = 8
    //   1 pompom × 13 = 13
    //   2 succulents × 13 = 26
    //   4 grass × 5 = 20
    //   6 ribs × 2 = 12
    //   hero 81+20+174+80+64+8+13+26+20+12 = 498
    //   scatter 160 moss+shadow ×6 = 960, 180 roast+shadow ×2 = 360,
    //            60 grass ×5 = 300, 24 yarn ×4 = 96, 40 petals = 40 → 1756
    //   498+1756 = 2254
    void SpawnNightRoast(System.Random rng, float dens, float zMax)
    {
        var low = LevelRunner.L4Low;
        for (int i = 0; i < 18; i++)
        {
            int col = i % 6, row = i / 6;
            float z = Mathf.Lerp(18.80f, 23.50f, col / 5f);
            float y = 0.90f + row * 0.32f;
            float x = -1.40f - row * 0.10f;
            YarnOrb(new Vector3(x, y, z), 0.16f, (i & 1) == 0);
            if ((i & 1) == 0) HangString(new Vector3(x, y + 0.40f, z), 0.38f);
        }
        FeltCloud(new Vector3(-2.4f, 2.6f, 5.0f), 0.36f);
        FeltCloud(new Vector3(-2.3f, 2.5f, 11.2f), 0.34f);
        FeltCloud(new Vector3(-2.4f, 2.7f, 20.4f), 0.40f);
        FeltCloud(new Vector3(-2.3f, 2.55f, 32.0f), 0.32f);
        FlowerTree(new Vector3(-1.55f, 0f, 6.4f), 1.25f);
        FlowerTree(new Vector3(-1.60f, 0f, 29.0f), 1.15f);
        for (int i = 0; i < 8; i++)
            RoundFeltTree(Along(low, i, 8, 2.15f, 0f, 3), 0.70f + (i % 4) * 0.12f, i);
        float[] pineZ = { 4.6f, 6.8f, 10.6f, 30.0f };
        for (int i = 0; i < 4; i++)
            PineLite(new Vector3(-2.2f, 0f, pineZ[i]), 0.85f + i * 0.05f);
        CardboardGrinder(new Vector3(-1.60f, 0.40f, 5.4f), 1.05f);
        RedPompomTree(new Vector3(-1.50f, 0f, 11.0f), 0.90f);
        Succulent(new Vector3(1.30f, 0f, 7.2f), 0.24f);
        Succulent(new Vector3(1.25f, 0f, 31.0f), 0.22f);
        float[] grassZ = { 5.8f, 7.6f, 10.2f, 28.5f };
        for (int i = 0; i < 4; i++)
            GrassTuft(new Vector3(i % 2 == 0 ? 1.1f : -1.1f, 0f, grassZ[i]), 0.8f);
        float[] ribZ = { 4.4f, 7.4f, 11.4f, 14.2f, 20.0f, 33.0f };
        for (int i = 0; i < 6; i++)
            KraftRib(new Vector3(-2.75f, 1.15f, ribZ[i]), 1.9f);
        for (int i = 0; i < 160; i++)
        {
            var p = Along(low, i, 160, 1.10f, 0.05f, 1);
            MossBall(p, 0.09f + U01(i, 2) * 0.08f);
            ContactShadow(p, 0.12f);
        }
        for (int i = 0; i < 180; i++)
        {
            var p = Along(low, i, 180, 1.20f, 0.05f, 5);
            RoastBean(p, 0.06f + U01(i, 3) * 0.04f, U01(i, 4) * 360f);
            ContactShadow(p, 0.09f);
        }
        for (int i = 0; i < 60; i++)
            GrassTuft(Along(low, i, 60, 1.30f, 0f, 8), 0.55f + U01(i, 6) * 0.35f);
        for (int i = 0; i < 24; i++)
            YarnOrb(Along(low, i, 24, 1.35f, 0.80f + U01(i, 7) * 0.40f, 9), 0.12f, i % 2 == 0);
        for (int i = 0; i < 40; i++)
            FeltPetal(Along(low, i, 40, 0.95f, 0.02f, 11), 0.07f, U01(i, 12) * 360f, i % 3 == 0);
        SpawnRaisedScatter(LevelRunner.L4High, LevelRunner.L4HighTop, 220, 300, 140, 80, 60, 20);
    }

    /// Dressing on top of the raised kraft slabs (the low-span scatter above never reaches them).
    /// Children: moss+shadow ×6, roast+shadow ×2, grass ×5, petals ×1, blooms ×11 (no colliders; off the run lane).
    void SpawnRaisedScatter(float[] high, float top, int moss, int roast, int grass, int petals, int blooms, int salt)
    {
        float y = top + 0.02f;
        for (int i = 0; i < moss; i++)
        {
            var p = Along(high, i, moss, 1.10f, y, salt + 1);
            MossBall(p, 0.08f + U01(i, salt + 2) * 0.07f);
            ContactShadow(new Vector3(p.x, top + 0.012f, p.z), 0.11f);
        }
        for (int i = 0; i < roast; i++)
        {
            var p = Along(high, i, roast, 1.25f, y, salt + 3);
            RoastBean(p, 0.06f + U01(i, salt + 4) * 0.035f, U01(i, salt + 5) * 360f);
            ContactShadow(new Vector3(p.x, top + 0.012f, p.z), 0.09f);
        }
        for (int i = 0; i < grass; i++)
            GrassTuft(Along(high, i, grass, 1.35f, top, salt + 6), 0.50f + U01(i, salt + 7) * 0.35f);
        for (int i = 0; i < petals; i++)
            FeltPetal(Along(high, i, petals, 0.95f, y, salt + 8), 0.07f, U01(i, salt + 9) * 360f, i % 3 == 0);
        for (int i = 0; i < blooms; i++)
            FlowerBloom(Along(high, i, blooms, 1.05f, top + 1.15f + U01(i, salt + 10) * 0.35f, salt + 11), 0.11f, i);
    }

    // ——— L5 Finale: the long set tour (yard, arch on the floor, steps, yarn, goal) ———
    // Fixed layout (no Random). Child count 5422 (> 5220):
    //   6 pines ×16 = 96
    //   2 pompom ×13 = 26
    //   2 succulents ×13 = 26
    //   bridge 12 planks + 6 posts + 22 rails = 40
    //   4 foot tufts ×5 = 20
    //   2 flower trees ×87 = 174
    //   16 round trees ×10 = 160
    //   2 grinders ×8 = 16
    //   2 grinder shadows = 2
    //   2 yarn wraps ×4 = 8
    //   yarn cluster 20×4 + 10 strings = 90
    //   12 hanging ×5 = 60
    //   6 clouds ×5 = 30
    //   8 ribs ×2 = 16
    //   10 path strips = 10
    //   8 flutes = 8
    //   hero 96+26+26+40+20+174+160+16+2+8+90+60+30+16+10+8 = 782
    //   scatter 280 moss+shadow ×6 = 1680, 400 roast+shadow ×2 = 800,
    //            200 grass ×5 = 1000, 120 petals = 120, 80 blooms ×11 = 880, 40 yarn ×4 = 160 → 4640
    //   782+4640 = 5422
    void SpawnFinaleEscape(System.Random rng, float dens, float zMax, bool boss)
    {
        var low = LevelRunner.L5Low;
        float[] pineZ = { 4.2f, 6.5f, 7.8f, 12.5f, 16.0f, 19.0f };
        for (int i = 0; i < 6; i++)
            PineLite(new Vector3(-2.15f - (i % 2) * 0.2f, 0f, pineZ[i]), 0.80f + (i % 3) * 0.1f);
        RedPompomTree(new Vector3(-1.50f, 0f, 5.5f), 0.95f);
        RedPompomTree(new Vector3(-1.45f, 0f, 45.0f), 0.90f);
        Succulent(new Vector3(1.25f, 0f, 7.2f), 0.24f);
        Succulent(new Vector3(1.20f, 0f, 47.0f), 0.22f);
        SpawnArch(LevelRunner.TourBridgeX0, LevelRunner.TourBridgeLen);
        GrassTuft(new Vector3(0.80f, 0f, LevelRunner.TourBridgeX0 - 0.55f), 0.9f);
        GrassTuft(new Vector3(-0.80f, 0f, LevelRunner.TourBridgeX0 - 0.45f), 0.85f);
        GrassTuft(new Vector3(0.85f, 0f, LevelRunner.TourBridgeX0 + LevelRunner.TourBridgeLen + 0.50f), 0.9f);
        GrassTuft(new Vector3(-0.75f, 0f, LevelRunner.TourBridgeX0 + LevelRunner.TourBridgeLen + 0.55f), 0.8f);
        FlowerTree(new Vector3(-1.55f, 0f, 18.2f), 1.30f);
        FlowerTree(new Vector3(-1.50f, 0f, 46.0f), 1.20f);
        for (int i = 0; i < 16; i++)
            RoundFeltTree(Along(low, i, 16, 2.20f, 0f, 2), 0.65f + (i % 5) * 0.1f, i);
        CardboardGrinder(new Vector3(-1.55f, 0.40f, 11.2f), 1.05f);
        ContactShadow(new Vector3(-1.55f, 0.01f, 11.2f), 0.46f);
        YarnWrapPost(new Vector3(-1.55f, 0.20f, 11.2f), 0.56f);
        CardboardGrinder(new Vector3(1.55f, 0.40f, 46.5f), 1.00f);
        ContactShadow(new Vector3(1.55f, 0.01f, 46.5f), 0.46f);
        YarnWrapPost(new Vector3(1.55f, 0.20f, 46.5f), 0.56f);
        for (int i = 0; i < 20; i++)
        {
            int col = i % 5, row = i / 5;
            float z = Mathf.Lerp(34.20f, 39.10f, col / 4f);
            float y = 0.95f + row * 0.28f;
            float x = -1.35f - (row % 2) * 0.16f;
            YarnOrb(new Vector3(x, y, z), 0.15f, (i & 1) == 0);
            if ((i & 1) == 0) HangString(new Vector3(x, y + 0.40f, z), 0.36f);
        }
        float[] hangZ = { 4.0f, 6.8f, 8.4f, 11.6f, 15.0f, 18.8f, 20.8f, 25.2f, 35.2f, 37.8f, 44.5f, 49.5f };
        for (int i = 0; i < hangZ.Length; i++)
            HangingBall(new Vector3(-1.20f - (i % 3) * 0.12f, 0f, hangZ[i]), 0.55f + (i % 3) * 0.15f, 0.22f, i % 2 == 0);
        float[] cloudZ = { 5.2f, 13.5f, 19.2f, 25.4f, 36.8f, 47.5f };
        for (int i = 0; i < cloudZ.Length; i++)
            FeltCloud(new Vector3(-2.4f, 2.55f, cloudZ[i]), 0.34f + (i % 3) * 0.03f);
        float[] ribZ = { 4.5f, 7.5f, 12.0f, 15.5f, 19.5f, 25.0f, 36.0f, 46.0f };
        for (int i = 0; i < ribZ.Length; i++)
            KraftRib(new Vector3(-2.80f, 1.20f, ribZ[i]), 2.0f);
        for (int i = 0; i < 10; i++)
            BurlapPathStrip(new Vector3(0f, 0.02f, Along(low, i, 10, 0.05f, 0.02f, 1).z), 1.1f);
        for (int i = 0; i < 8; i++)
        {
            float t = (i + 0.5f) / 8f;
            float z = LevelRunner.TourBridgeX0 + t * LevelRunner.TourBridgeLen;
            float arch = Mathf.Sin(t * Mathf.PI) * LevelRunner.BridgeArch;
            KraftFlute(new Vector3(-0.42f, LevelRunner.BridgeDeckY + arch + 0.08f, z), 0.45f);
        }
        for (int i = 0; i < 280; i++)
        {
            var p = Along(low, i, 280, 1.15f, 0.05f, 3);
            MossBall(p, 0.08f + U01(i, 2) * 0.07f);
            ContactShadow(p, 0.11f);
        }
        for (int i = 0; i < 400; i++)
        {
            var p = Along(low, i, 400, 1.25f, 0.05f, 6);
            RoastBean(p, 0.06f + U01(i, 4) * 0.035f, U01(i, 5) * 360f);
            ContactShadow(p, 0.09f);
        }
        for (int i = 0; i < 200; i++)
            GrassTuft(Along(low, i, 200, 1.35f, 0f, 8), 0.50f + U01(i, 7) * 0.40f);
        for (int i = 0; i < 120; i++)
            FeltPetal(Along(low, i, 120, 0.90f, 0.02f, 10), 0.07f, U01(i, 11) * 360f, i % 3 == 0);
        for (int i = 0; i < 80; i++)
            FlowerBloom(Along(low, i, 80, 1.05f, 1.15f + U01(i, 13) * 0.40f, 12), 0.11f, i);
        for (int i = 0; i < 40; i++)
            YarnOrb(Along(low, i, 40, 1.30f, 0.80f + U01(i, 14) * 0.35f, 15), 0.11f, i % 2 == 0);
        SpawnRaisedScatter(LevelRunner.L5High, LevelRunner.L5HighTop, 140, 200, 80, 60, 30, 40);
        if (boss) SpawnBossSet();
    }

    /// L5 boss beat dressing (LevelDef.bossBeat): the grinder's run-up on the raised kraft — a wrapped
    /// grinder at the slab foot, warning ribs and a gold-yarn line of orbs leading to the drop.
    /// GrinderChase (LevelFeatures) is the moving hazard; this is the set it rolls through.
    void SpawnBossSet()
    {
        float x0 = LevelRunner.L5High[0];
        CardboardGrinder(new Vector3(-1.70f, 0.40f, x0 - 1.4f), 1.35f);
        ContactShadow(new Vector3(-1.70f, 0.01f, x0 - 1.4f), 0.60f);
        YarnWrapPost(new Vector3(-1.70f, 0.20f, x0 - 1.4f), 0.70f);
        float[] ribZ = { x0 + 0.8f, x0 + 3.0f, LevelRunner.L5High[2] + 1.2f, LevelRunner.L5High[3] - 0.8f };
        for (int i = 0; i < ribZ.Length; i++)
            KraftRib(new Vector3(-2.60f, 1.25f, ribZ[i]), 2.2f);
        for (int i = 0; i < 12; i++)
            YarnOrb(Along(LevelRunner.L5High, i, 12, 1.55f, LevelRunner.L5HighTop + 1.45f, 51), 0.13f, false);
    }

    /// Same 12 planks + 6 posts + 22 rails as L2 (40 children). Feet sit on the burlap; the caller owns the gap.
    void SpawnArch(float x0, float len)
    {
        const int planks = 12;
        float rise = LevelRunner.BridgeArch, deckY = LevelRunner.BridgeDeckY;
        float Arch(float t) => Mathf.Sin(t * Mathf.PI) * rise;
        float Slope(float t) => rise * Mathf.PI / len * Mathf.Cos(t * Mathf.PI);
        for (int i = 0; i < planks; i++)
        {
            float t = i / (float)(planks - 1);
            BridgePlank(new Vector3(0f, deckY + Arch(t), x0 + t * len), 1.06f,
                i % 2 == 0 ? Kraft : new Color(0.80f, 0.72f, 0.62f), -Mathf.Atan(Slope(t)) * Mathf.Rad2Deg);
        }
        float deckTop = deckY + 0.055f;
        const float postH = 0.50f;
        float[] postT = { 0f, 0.5f, 1f };
        for (int i = 0; i < postT.Length; i++)
        {
            float y = deckTop + Arch(postT[i]) + postH * 0.5f, z = x0 + postT[i] * len;
            BridgePost(new Vector3(0.56f, y, z), postH);
            BridgePost(new Vector3(1.00f, y, z), postH);
        }
        float[] railH = { 0.46f, 0.26f };
        for (int r = 0; r < railH.Length; r++)
            for (int i = 0; i < planks - 1; i++)
            {
                float ta = i / (float)(planks - 1), tb = (i + 1) / (float)(planks - 1), tm = (ta + tb) * 0.5f;
                float dz = (tb - ta) * len, dy = Arch(tb) - Arch(ta);
                BridgeRail(new Vector3(0f, deckTop + Arch(tm) + railH[r], x0 + tm * len),
                    Mathf.Sqrt(dz * dz + dy * dy) + 0.05f, Mathf.Atan2(dy, dz) * Mathf.Rad2Deg);
            }
    }

    static float U01(int i, int salt)
    {
        float s = Mathf.Sin((i + 1) * 12.9898f + salt * 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    /// Even spacing along solid span pairs (block, block, …). Never returns a z inside a gap between pairs.
    static Vector3 Along(float[] spans, int i, int n, float xMag, float y, int salt)
    {
        float usable = 0f;
        for (int b = 0; b + 1 < spans.Length; b += 2)
        {
            float len = spans[b + 1] - spans[b] - 1.4f;
            if (len > 0.3f) usable += len;
        }
        if (usable < 0.1f) usable = 0.1f;
        float target = (i + 0.5f) / Mathf.Max(1, n) * usable;
        float acc = 0f;
        float z = spans.Length > 0 ? spans[0] + 0.7f : 0f;
        for (int b = 0; b + 1 < spans.Length; b += 2)
        {
            float a = spans[b] + 0.7f;
            float c = spans[b + 1] - 0.7f;
            float len = c - a;
            if (len <= 0.3f) continue;
            if (target <= acc + len || b + 3 >= spans.Length)
            {
                z = Mathf.Clamp(a + (target - acc), a, c);
                break;
            }
            acc += len;
        }
        float side = ((i + salt) & 1) == 0 ? -1f : 1f;
        return new Vector3(side * xMag, y, z);
    }

    // ——— Prop builders (BuildEscapeScene vocabulary + PaintTex LOOK kit) ———

    // scope-L1L3 p10: kraft tint cohesion + hop deck thickness (count-neutral; plank count HELD)
    /// tiltDeg rotates the plank about the across-lane axis so an arched deck stays a continuous walkable curve.
    void BridgePlank(Vector3 pos, float width, Color? tint = null, float tiltDeg = 0f)
    {
        var go = Prim(PrimitiveType.Cube, "Bridge Plank", pos, new Vector3(width, 0.11f, 0.54f));
        if (tiltDeg != 0f) go.transform.rotation = Quaternion.Euler(tiltDeg, 0f, 0f);
        PaintTex(go, "Look/tex-kraft-corrugated", tint ?? Kraft, 0.84f);
    }

    void BridgePost(Vector3 pos, float h)
    {
        var go = Prim(PrimitiveType.Cylinder, "Bridge Post", pos, new Vector3(0.08f, h * 0.5f, 0.08f));
        PaintTex(go, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    /// Rails are re-laid along the run by RemapToSideScroll (no yaw), so tiltDeg about z follows the arch in world x-y.
    void BridgeRail(Vector3 pos, float width, float tiltDeg = 0f)
    {
        var go = Prim(PrimitiveType.Cube, "Bridge Rail", pos, new Vector3(width, 0.05f, 0.06f));
        if (tiltDeg != 0f) go.transform.rotation = Quaternion.Euler(0f, 0f, tiltDeg);
        PaintTex(go, "Look/tex-kraft-corrugated", KraftDark, 0.88f);
    }

    // ——— video-set builders (285.mp4 / gen-2d7dcad9.mp4) ———

    /// 285 right: one merged red wool cluster on a thicker trunk (13 prims).
    /// DriveLook/imagine/285.jpg and frames/vid-285-630.png. Not separate red/orange/yellow spheres.
    void RedPompomTree(Vector3 pos, float scale)
    {
        // x/z 0.09 → 0.18: the plate trunk is a felt limb, not a stick. Height unchanged so the foot stays on the burlap.
        var trunk = Prim(PrimitiveType.Cylinder, "Red Trunk", pos + Vector3.up * (0.45f * scale), new Vector3(0.18f, 0.45f, 0.18f) * scale);
        PaintTex(trunk, "Look/tex-felt-brown", FeltDark, 0.90f);
        Vector3[] dirs = { new Vector3(-0.55f, 0.75f, 0.1f), new Vector3(0.6f, 0.7f, -0.1f), new Vector3(0.05f, 0.9f, 0.25f) };
        for (int b = 0; b < dirs.Length; b++)
        {
            Vector3 d = dirs[b].normalized;
            // short thick stubs tucked into the wool (vid-285-630.png), not thin sticks between separate balls
            var br = Prim(PrimitiveType.Cylinder, "Red Branch", pos + Vector3.up * (0.72f * scale) + d * (0.10f * scale), new Vector3(0.10f, 0.14f, 0.10f) * scale);
            br.transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
            NoCol(br);
            PaintTex(br, "Look/tex-felt-brown", FeltDark, 0.90f);
        }
        Vector3 heart = new Vector3(0.02f, 1.12f, 0.02f);
        Vector3[] poms =
        {
            new Vector3(-0.42f, 1.30f, 0.05f), new Vector3(-0.18f, 1.52f, -0.08f), new Vector3(0.12f, 1.62f, 0.10f),
            new Vector3(0.42f, 1.42f, -0.05f), new Vector3(0.55f, 1.12f, 0.08f), new Vector3(-0.55f, 1.02f, -0.06f),
            new Vector3(0.05f, 1.22f, 0.22f), new Vector3(-0.30f, 1.08f, 0.18f), new Vector3(0.32f, 1.00f, -0.16f)
        };
        // 285.jpg red band (430,220–680,480) mean R/G 2.83. tex-felt-red mean R/G 5.94, so G multiplier is 2.10.
        var wool = new Color(1.00f, 2.10f, 1.05f);
        for (int i = 0; i < poms.Length; i++)
        {
            Vector3 local = heart + (poms[i] - heart) * 0.36f; // overlap into one cluster
            var pom = Prim(PrimitiveType.Sphere, "Red Pompom", pos + local * scale, Vector3.one * ((0.36f + (i % 3) * 0.04f) * scale));
            NoCol(pom);
            SoftWool(pom, 0.16f, 60 + i);
            PaintTex(pom, "Look/tex-felt-red", wool, 0.93f);
        }
    }

    /// 285 bottom-left: soft sage rosette on the burlap — two leaf rings + bud (13 prims).
    /// DriveLook/imagine/285.jpg. Named "Sage …" so it may sit in the foreground ("Succulent" is forced behind).
    void Succulent(Vector3 pos, float s)
    {
        var sage = new Color(1.18f, 1.14f, 1.06f);
        for (int ring = 0; ring < 2; ring++)
        {
            int n = ring == 0 ? 7 : 5;
            // CraftMesh.Succulent outer/inner lean (46° / 24°): plump leaves, not stretched faceted spheres.
            float lean = ring == 0 ? 46f : 24f;
            float len = ring == 0 ? 0.95f : 0.55f;
            for (int i = 0; i < n; i++)
            {
                float a = i * (Mathf.PI * 2f / n) + ring * 0.45f;
                var leaf = Prim(PrimitiveType.Sphere, "Sage Leaf",
                    pos + new Vector3(Mathf.Cos(a) * 0.05f, 0.02f, Mathf.Sin(a) * 0.05f) * s,
                    Vector3.one * (s * len));
                var mf = leaf.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = CraftMesh.Leaf(1f, 0.78f, 0.10f, 8); // CraftMesh.Succulent leaf width / length
                float leanRad = lean * Mathf.Deg2Rad;
                Vector3 aim = new Vector3(Mathf.Cos(a) * Mathf.Sin(leanRad), Mathf.Cos(leanRad), Mathf.Sin(a) * Mathf.Sin(leanRad));
                leaf.transform.rotation = Quaternion.FromToRotation(Vector3.up, aim);
                NoCol(leaf);
                PaintTex(leaf, "Look/tex-yarn-green", ring == 0 ? sage : new Color(1.24f, 1.20f, 1.10f), 0.93f);
            }
        }
        var bud = Prim(PrimitiveType.Sphere, "Sage Bud", pos + Vector3.up * (0.22f * s), new Vector3(0.28f, 0.30f, 0.28f) * s);
        NoCol(bud);
        SoftWool(bud, 0.12f, 9);
        PaintTex(bud, "Look/tex-yarn-green", new Color(1.28f, 1.24f, 1.12f), 0.93f);
    }

    /// set-0x: round felt tree — burlap-wrapped trunk, egg crown, eight tan/brown felt dots (10 prims).
    void RoundFeltTree(Vector3 pos, float s, int seed)
    {
        var trunk = Prim(PrimitiveType.Cylinder, "Round Tree Trunk", pos + Vector3.up * (0.25f * s), new Vector3(0.11f, 0.25f, 0.11f) * s);
        PaintTex(trunk, "Look/tex-burlap", Kraft, 0.92f);
        Vector3 c = pos + Vector3.up * (0.85f * s);
        var crown = Prim(PrimitiveType.Sphere, "Round Tree Crown", c, new Vector3(0.72f, 0.80f, 0.72f) * s);
        NoCol(crown);
        // DriveLook/imagine/frames/vid-set-2000.png and gen-2d7dcad9.jpg: fuzzy felt canopy, not a smooth ball.
        SoftWool(crown, 0.12f, 8 + seed);
        PaintTex(crown, "Look/tex-yarn-green", seed % 2 == 0 ? FeltPine : new Color(1.06f, 1.06f, 0.96f), 0.94f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 0.9f + seed * 0.7f;
            float e = -0.45f + (i % 4) * 0.30f; // elevation band
            Vector3 n = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
            var dot = Prim(PrimitiveType.Sphere, "Round Tree Dot",
                c + Vector3.Scale(n, new Vector3(0.34f, 0.38f, 0.34f) * s), Vector3.one * (0.12f * s));
            NoCol(dot);
            SoftWool(dot, 0.16f, 40 + i); // vid-set-2000.png: raised felt dots, not pinpricks
            if (i % 3 == 0) PaintTex(dot, "Look/tex-felt-brown", FeltDark, 0.88f);
            else PaintTex(dot, "Look/tex-yarn-fiber", i % 3 == 1 ? Yarn : new Color(0.88f, 0.80f, 0.66f), 0.85f);
        }
    }

    /// Camera-facing felt bloom: overlapping red or orange wool plus four green felt lumps (11 prims).
    /// Petals fan in the y-z plane (lane space) so after the side-scroll remap they face the player.
    void FeltFlower(Vector3 pos, float s, Color petal, Color inner)
    {
        var center = Prim(PrimitiveType.Sphere, "Bloom Center", pos, new Vector3(0.40f, 0.45f, 0.45f) * s);
        NoCol(center);
        SoftWool(center, 0.14f, 3);
        PaintTex(center, "Look/tex-felt-red", petal, 0.92f); // merges with its petals; vid-set-2000.png has no loose yellow ball
        for (int p = 0; p < 6; p++)
        {
            float a = p / 6f * Mathf.PI * 2f;
            // 0.38 (was 0.55): petals overlap into one felt bloom. DriveLook/imagine/gen-2d7dcad9.jpg
            var pt = Prim(PrimitiveType.Sphere, "Flower Petal",
                pos + new Vector3(-0.04f * s, Mathf.Cos(a) * s * 0.38f, Mathf.Sin(a) * s * 0.38f),
                new Vector3(0.22f, 0.48f, 0.48f) * s);
            NoCol(pt);
            SoftWool(pt, 0.14f, 12 + p);
            PaintTex(pt, "Look/tex-felt-red", petal, 0.92f);
        }
        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f + 0.4f;
            var loop = Prim(PrimitiveType.Sphere, "Bloom Yarn Loop",
                pos + new Vector3(0.04f * s, Mathf.Cos(a) * s * 0.22f, Mathf.Sin(a) * s * 0.22f),
                new Vector3(0.16f, 0.28f, 0.28f) * s);
            NoCol(loop);
            SoftWool(loop, 0.12f, 20 + i);
            PaintTex(loop, "Look/tex-yarn-green", inner, 0.90f); // green felt in the bloom, not a yellow sphere
        }
    }

    /// gen-2d7dcad9 right: dense red-orange-green felt on a thicker yarn trunk.
    /// DriveLook/imagine/gen-2d7dcad9.jpg and frames/vid-set-2000.png. Still 1 + 1 + 7×11 + 8 = 87 prims.
    void FlowerTree(Vector3 pos, float s)
    {
        var tree = FeltFlowerTree.Build(pos, s);
        if (root != null) tree.transform.SetParent(root, true);
    }
    /// set-0x bridge feet: spiky green felt grass tuft — five blades fanned out (5 prims).
    void GrassTuft(Vector3 pos, float k)
    {
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.40f;        // fan
            float h = (0.16f - Mathf.Abs(i - 2) * 0.03f) * k;
            var blade = Prim(PrimitiveType.Sphere, "Grass Blade",
                pos + new Vector3(Mathf.Sin(a) * h * 0.55f, h, (i % 2) * 0.02f * k),
                new Vector3(0.035f * k, h, 0.03f * k));
            blade.transform.rotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg * 0.8f);
            NoCol(blade);
            PaintTex(blade, "Look/tex-yarn-green", i % 2 == 0 ? FeltPine : new Color(1.08f, 1.10f, 0.94f), 0.93f);
        }
    }

    /// gen-2d7dcad9 3.6–6 s: felt petals that dropped from the flower tree onto the burlap (1 prim).
    void FeltPetal(Vector3 pos, float s, float yaw, bool orange)
    {
        var go = Prim(PrimitiveType.Sphere, "Felt Petal", pos + Vector3.up * 0.012f, new Vector3(s * 1.6f, s * 0.22f, s));
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        NoCol(go);
        PaintTex(go, "Look/tex-felt-red", orange ? new Color(1.25f, 0.95f, 0.55f) : new Color(1.0f, 0.92f, 0.90f), 0.92f);
    }

    /// Yarn ball hanging from a string that runs up past the top of the frame (set-0x top corners): 4 + 1 prims.
    void HangingBall(Vector3 at, float len, float s, bool green)
    {
        const float top = 3.30f; // Backdrop twine height
        var ball = new Vector3(at.x, top - len, at.z);
        YarnOrb(ball, s, green);
        HangString(new Vector3(at.x, top, at.z), len - s * 0.4f);
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
        // DriveLook/imagine/285.jpg foreground: one fuzzy olive ball. Moss box 480,640–680,860 mean G/R 0.933;
        // tex-yarn-green G/R 1.049, so the G multiplier is 0.89.
        SoftWool(go, 0.20f, 4);
        PaintTex(go, "Look/tex-yarn-green", new Color(1.00f, 0.89f, 0.66f), 0.90f);
        for (int i = 0; i < 4; i++)
        {
            float a = i / 4f * Mathf.PI * 2f;
            // sunk into the main ball (offset 0.14, was 0.30) so the fuzz is one ball, not five smooth spheres
            var h = Prim(PrimitiveType.Sphere, "Moss Halo",
                pos + new Vector3(Mathf.Cos(a) * s * 0.14f, s * 0.48f, Mathf.Sin(a) * s * 0.14f),
                Vector3.one * (s * 0.62f));
            NoCol(h);
            SoftWool(h, 0.18f, 30 + i);
            PaintTex(h, "Look/tex-yarn-green", new Color(1.00f, 0.89f, 0.66f), 0.93f);
        }
    }

    void RoastBean(Vector3 pos, float scale, float yaw)
    {
        var rootGo = new GameObject("Roast Bean");
        rootGo.transform.SetParent(root, false);
        rootGo.transform.position = pos + Vector3.up * (scale * 0.6f);
        rootGo.transform.rotation = Quaternion.Euler(0f, yaw, 12f);

        // glossy dark roast bean (285.jpg / vid-285-630.png): smooth lobes, not faceted dots
        var l = PrimChild(rootGo.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(-0.45f * scale, 0f, 0f), new Vector3(scale, scale * 0.72f, scale * 0.85f));
        GlossLobe(l);
        PaintTex(l, "Look/tex-roast-bean", Color.white, 0.38f);
        var r = PrimChild(rootGo.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(0.45f * scale, 0f, 0f), new Vector3(scale, scale * 0.72f, scale * 0.85f));
        GlossLobe(r);
        PaintTex(r, "Look/tex-roast-bean", new Color(0.94f, 0.94f, 0.94f), 0.38f);
        var c = PrimChild(rootGo.transform, PrimitiveType.Capsule, "Roast Crease", Vector3.zero, new Vector3(scale * 0.18f, scale * 0.55f, scale * 0.18f));
        PaintTex(c, "Look/tex-roast-bean", new Color(0.55f, 0.50f, 0.46f), 0.6f);
    }

    void YarnOrb(Vector3 pos, float s, bool green)
    {
        // DriveLook/imagine/gen-2d7dcad9.jpg and frames/vid-set-2000.png: a wound yarn ball, not a plain sphere.
        var go = Prim(PrimitiveType.Sphere, "Yarn Ball", pos, Vector3.one * s);
        NoCol(go);
        SoftWool(go, 0.05f, green ? 2 : 5);
        PaintTex(go, green ? "Look/tex-yarn-green" : "Look/tex-yarn-fiber", green ? FeltPine : Yarn, 0.92f);
        for (int i = 0; i < 3; i++)
        {
            var wrap = Prim(PrimitiveType.Cylinder, "Yarn Bump", pos, new Vector3(s * 1.02f, s * 0.018f, s * 1.02f));
            wrap.transform.rotation = Quaternion.Euler(52f + i * 36f, i * 60f, 16f + i * 24f);
            NoCol(wrap);
            PaintTex(wrap, green ? "Look/tex-yarn-green" : "Look/tex-yarn-fiber", green ? FeltPine : Yarn, 0.9f);
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

    // DriveLook/imagine/285.jpg and frames/vid-285-630.png: thick felt layers, soft edges, raised brown pompoms.
    // Same 16 prims (trunk + cap + 5 layers + 3 pompoms + 6 smaller pompoms). Not sharp cones or pinprick dots.
    void PineLite(Vector3 pos, float scale)
    {
        var trunk = Prim(PrimitiveType.Cylinder, "Pine Trunk", pos + Vector3.up * (0.08f * scale), new Vector3(0.09f * scale, 0.22f * scale, 0.09f * scale));
        PaintTex(trunk, "Look/tex-felt-brown", FeltDark, 0.90f);
        // 285.jpg pine core (80,360–180,480) mean G/R 0.982. tex-yarn-green G/R 1.049 → G 0.936, B 0.847.
        var sage = new Color(1.00f, 0.936f, 0.847f);
        var crown = Prim(PrimitiveType.Sphere, "Pine Crown", pos + Vector3.up * (1.18f * scale), new Vector3(0.30f, 0.34f, 0.30f) * scale);
        NoCol(crown);
        SoftWool(crown, 0.16f, 2);
        PaintTex(crown, "Look/tex-yarn-green", sage, 0.93f);
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;                       // 0 bottom .. 1 top
            float w = Mathf.Lerp(0.78f, 0.34f, t);
            float y = Mathf.Lerp(0.32f, 1.00f, t);
            // thickness 0.70 of the width: a fat felt layer. The old 0.42 discs read as sharp cones.
            var tier = Prim(PrimitiveType.Sphere, "Pine Fuzz",
                pos + Vector3.up * (y * scale),
                new Vector3(w, w * 0.70f, w) * scale);
            NoCol(tier);
            SoftWool(tier, 0.14f, 10 + i);
            PaintTex(tier, "Look/tex-yarn-green", sage, 0.94f);
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i / 3f * Mathf.PI * 2f + 0.4f;
            float r = Mathf.Lerp(0.34f, 0.22f, i / 2f);
            var cone = Prim(PrimitiveType.Sphere, "Pinecone",
                pos + new Vector3(Mathf.Cos(a) * r * scale, (0.46f + i * 0.18f) * scale, Mathf.Sin(a) * r * scale),
                Vector3.one * (0.20f * scale));
            NoCol(cone);
            SoftWool(cone, 0.18f, 20 + i);
            PaintTex(cone, "Look/tex-felt-brown", FeltDark, 0.85f);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f + 1.1f;
            float r = Mathf.Lerp(0.30f, 0.16f, i / 5f);
            var bump = Prim(PrimitiveType.Sphere, "Pine Crochet Bump",
                pos + new Vector3(Mathf.Cos(a) * r * scale, (0.55f + i * 0.08f) * scale, Mathf.Sin(a) * r * scale),
                Vector3.one * (0.15f * scale));
            NoCol(bump);
            SoftWool(bump, 0.16f, 30 + i);
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

    /// Pompom of diameter 1, so the transform scale stays the primitive's size. Soft felt edge, not a smooth sphere.
    static void SoftWool(GameObject go, float fuzz, int seed)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = CraftMesh.Pompom(0.5f, fuzz, 16, 12, Mathf.Max(1, seed));
    }

    /// Smooth coffee-bean lobe. Replaces the 12×8 sphere that read as a faceted dot. DriveLook/imagine/285.jpg.
    static void GlossLobe(GameObject go)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = CraftMesh.Egg(1f, 1f, 1f, 0.18f, 28, 18);
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

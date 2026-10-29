using UnityEngine;

/// Per-level gameplay on top of the fixed lanes in LevelRunner (the L1–L3 lanes and every gap stay as built):
/// checkpoint pennants on every level, telegraphed steam vents and a felt lift in L4/L5, and the L5 grinder chase.
/// Look sources: steam puffs use the wool-cloud mesh and cream felt (ref-285 wool clouds, rotate-cloud.mp4);
/// the grinder roller uses brown felt with felt-red bands (rotate-grinder.mp4, ref-285 red pompom tree);
/// the lift is the deep kraft step material already used on the lane (ref-285 kraft wall).
public static class LevelFeatures
{
    const string RootName = "Level Features";

    /// Steam vents: (x, block top). Every vent sits ≥ 0.9 m from a checkpoint and ≥ 1.2 m from a gap lip.
    static readonly Vector2[] L4Vents = { new Vector2(5.8f, 0f), new Vector2(15.2f, LevelRunner.L4HighTop), new Vector2(21.0f, LevelRunner.L4HighTop), new Vector2(30.0f, 0f) };
    static readonly Vector2[] L5Vents = { new Vector2(6.8f, 0f), new Vector2(11.9f, 0f) };
    /// Lifts: (x centre, block top). Each carries a bonus honey bean at the top of its travel.
    static readonly Vector2[] L4Lifts = { new Vector2(22.6f, LevelRunner.L4HighTop) };
    static readonly Vector2[] L5Lifts = { new Vector2(36.2f, LevelRunner.L5HighTop) };

    /// L5 boss: the grinder wakes when the bean passes this x (start of the raised kraft run).
    public const float GrinderWakeX = 27.5f;

    /// Phase offset between neighbouring vents so they do not blast in unison.
    const float VentPhaseStep = 0.9f;
    /// Bonus honey sits this far above the lift's top stop.
    const float LiftHoneyRise = 0.85f;

    public static int VentCount { get; private set; }
    public static int LiftCount { get; private set; }
    public static int FlagCount { get; private set; }
    public static GrinderChase Grinder { get; private set; }

    public static void Build(LevelCatalog.LevelDef def, LevelRunner runner)
    {
        var rootGo = GameObject.Find(RootName);
        if (rootGo == null) rootGo = new GameObject(RootName);
        var root = rootGo.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var c = root.GetChild(i).gameObject;
            if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
        }
        VentCount = 0; LiftCount = 0; FlagCount = 0; Grinder = null;
        if (!Application.isPlaying || runner == null) return;

        // a pennant at every checkpoint after the first block
        for (int i = 1; i < LevelRunner.Blocks.Count; i++)
        {
            float top = i < LevelRunner.BlockTop.Count ? LevelRunner.BlockTop[i] : 0f;
            var go = new GameObject("Checkpoint Flag");
            go.transform.SetParent(root, false);
            go.AddComponent<CheckpointFlag>().Init(runner, LevelRunner.Blocks[i].x + 0.7f, top, def.accent);
            FlagCount++;
        }

        int id = Mathf.Clamp(def.id, 1, LevelCatalog.LevelCount);
        Vector2[] vents = id == 4 ? L4Vents : id == 5 ? L5Vents : null;
        Vector2[] lifts = id == 4 ? L4Lifts : id == 5 ? L5Lifts : null;
        if (vents != null)
        {
            for (int i = 0; i < vents.Length; i++)
            {
                var go = new GameObject("Steam Vent");
                go.transform.SetParent(root, false);
                go.AddComponent<SteamVent>().Init(runner, vents[i].x, vents[i].y, i * VentPhaseStep);
                VentCount++;
            }
        }
        if (lifts != null)
        {
            for (int i = 0; i < lifts.Length; i++)
            {
                var go = new GameObject("Felt Lift");
                go.transform.SetParent(root, false);
                var lift = go.AddComponent<LiftPlatform>();
                lift.Init(lifts[i].x, lifts[i].y);
                runner.AddHoney(new Vector3(lifts[i].x, lift.TopHigh + LiftHoneyRise, 0f));
                LiftCount++;
            }
        }
        if (def.bossBeat)
        {
            var go = new GameObject("Grinder Chase");
            go.transform.SetParent(root, false);
            Grinder = go.AddComponent<GrinderChase>();
            Grinder.Init(runner, GrinderWakeX, def.goalZ);
        }
        Debug.Log($"[LevelFeatures] L{def.id} vents={VentCount} lifts={LiftCount} flags={FlagCount} boss={(Grinder != null)}");
    }

    /// Shared felt material from a Look texture (same CraftMesh path the lane uses).
    public static Material Felt(string tex, Color tint, float tile = 1.5f)
    {
        string key = "feature-" + tex + "-" + ColorUtility.ToHtmlStringRGB(tint) + "-" + tile.ToString("0.##");
        return CraftMesh.Shared(key, () =>
            CraftMesh.Felt(Resources.Load<Texture2D>(tex), tint, Vector2.one * tile, 0.8f, 0.10f, 0.6f, 0.92f));
    }

    /// Primitive with its collider removed (decor only).
    public static GameObject Decor(PrimitiveType type, string name, Transform parent, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        Object.Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Batch 3 path pass (Imagine 285 + set frames): makes the run lane readable at the closer batch-2 camera.
///  • Lane Strip: a kraft runner on burlap blocks (burlap runner on raised kraft slabs), 1.1 m wide on the lane,
///    with stitched dark hems. Decoration only (no colliders), laid on LevelRunner.Blocks, so it never bridges a gap.
///  • Gap lips: a warm red felt band on the last 0.14 m of a block before a gap and the first 0.14 m after it, so
///    every hop reads early (hazard-visible-early ask).
///  • Title bleed: the 3D yarn "ESPRESSO ESCAPE" sign shows only in the title/menu phases (it overlapped the HUD in
///    play), and the opening yard's Kraft Bridge is hidden in L3–L5 (it sits on the first solid block, so the floor
///    under it stays; L1 keeps it as yard dressing, L2 is the Kraft Bridge level).
/// Runtime only. Hooks LevelRunner.OnLevelLoaded; one watcher object flips the sign by GameSession phase.
public static class PlayPath
{
    public const float StripHalfWidth = 0.55f;
    public const float LipLen = 0.14f;
    static GameObject _root;
    static int _level;
    public static int StripCount { get; private set; }
    public static int LipCount { get; private set; }
    public static int Level => _level;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        LevelRunner.OnLevelLoaded -= OnLevel;
        LevelRunner.OnLevelLoaded += OnLevel;
        _level = 0;
        if (Object.FindAnyObjectByType<Watcher>() == null)
        {
            var go = new GameObject("Play Path Watch");
            go.AddComponent<Watcher>();
        }
    }

    static void OnLevel(LevelCatalog.LevelDef def)
    {
        _level = def.id;
        Build();
        Apply(Menu());
    }

    static bool Menu()
    {
        if (GameSession.I == null) return true;
        var p = GameSession.I.phase;
        return p == GameSession.Phase.Boot || p == GameSession.Phase.Title
            || p == GameSession.Phase.Options || p == GameSession.Phase.Search;
    }

    /// Scene roots by exact name, inactive ones included.
    public static List<GameObject> SceneRoots(string name)
    {
        var list = new List<GameObject>();
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return list;
        foreach (var go in scene.GetRootGameObjects())
            if (go != null && go.name == name) list.Add(go);
        return list;
    }

    /// Sign on in menus only; yard bridge off in L3–L5 play.
    static void Apply(bool menu)
    {
        foreach (var t in SceneRoots("Yarn Title"))
            if (t.activeSelf != menu) t.SetActive(menu);
        if (_root != null && _root.activeSelf == menu) _root.SetActive(!menu); // 285 title has no runner
        bool bridge = menu || _level <= 2;
        foreach (var b in SceneRoots("Kraft Bridge"))
            if (b.activeSelf != bridge) b.SetActive(bridge);
    }

    public static void Build()
    {
        if (_root != null) Object.Destroy(_root);
        _root = new GameObject("Lane Strip");
        StripCount = 0;
        LipCount = 0;
        var burlapTex = Resources.Load<Texture2D>("Look/tex-burlap");
        var kraftTex = Resources.Load<Texture2D>("Look/tex-kraft-corrugated");
        var kraftRunner = CraftMesh.Shared("lane-strip-kraft", () =>
            CraftMesh.Felt(burlapTex, new Color(0.66f, 0.55f, 0.40f), new Vector2(3f, 2f), 0f, 0.03f, 0.72f, 0.98f)); // b4: worn-burlap patch, not a terracotta quad (audit3 #2)
        var burlapRunner = CraftMesh.Shared("lane-strip-burlap", () =>
            CraftMesh.Felt(burlapTex, new Color(0.86f, 0.74f, 0.56f), new Vector2(3f, 2f), 0f, 0.02f, 0.70f, 0.98f));
        var hem = CraftMesh.Shared("lane-strip-hem", () =>
            CraftMesh.Felt(null, new Color(0.52f, 0.40f, 0.26f), Vector2.one, 0f, 0.03f, 0.6f, 0.95f)); // b4: twine hem
        var lip = CraftMesh.Shared("lane-strip-lip", () =>
            CraftMesh.Felt(null, new Color(0.70f, 0.20f, 0.14f), Vector2.one, 0f, 0.05f, 0.6f, 0.92f));

        // the start apron (BuildEscapeScene floor, x -9..3) is not in Blocks: run the strip from behind the spawn
        float first = float.MaxValue;
        for (int i = 0; i < LevelRunner.Blocks.Count; i++) first = Mathf.Min(first, LevelRunner.Blocks[i].x);
        int n = LevelRunner.Blocks.Count + (first >= LevelRunner.ApronEnd - 0.01f && first < 999f ? 1 : 0);
        for (int k = 0; k < n; k++)
        {
            bool apron = k == LevelRunner.Blocks.Count;
            int i = apron ? 0 : k;
            var b = apron ? new Vector2(-4f, first) : LevelRunner.Blocks[i];
            float top = apron ? 0f : (i < LevelRunner.BlockTop.Count ? LevelRunner.BlockTop[i] : 0f);
            float x0 = b.x, x1 = b.y, len = x1 - x0;
            if (len < 0.3f) continue;
            bool raised = top > 0.02f;
            float y = top + 0.004f;
            Slab("Lane Strip Runner", new Vector3((x0 + x1) * 0.5f, y, 0f), new Vector3(len, 0.006f, StripHalfWidth * 2f),
                raised ? burlapRunner : kraftRunner, new Vector2(len / 1.6f, 1f));
            StripCount++;
            // stitched hems on both sides: short dashes like a running stitch
            for (int side = -1; side <= 1; side += 2)
                for (float x = x0 + 0.06f; x + 0.12f <= x1 - 0.04f; x += 0.22f)
                    Slab("Lane Strip Stitch", new Vector3(x + 0.06f, y + 0.004f, side * (StripHalfWidth - 0.05f)),
                        new Vector3(0.12f, 0.006f, 0.022f), hem, Vector2.one);
            // red felt lips next to every gap (block end followed / preceded by a gap)
            if (NextToGap(x1 + 0.05f))
            { Slab("Lane Strip Lip", new Vector3(x1 - LipLen * 0.5f - 0.01f, y + 0.003f, 0f), new Vector3(LipLen, 0.007f, StripHalfWidth * 2f), lip, Vector2.one); LipCount++; }
            if (NextToGap(x0 - 0.05f))
            { Slab("Lane Strip Lip", new Vector3(x0 + LipLen * 0.5f + 0.01f, y + 0.003f, 0f), new Vector3(LipLen, 0.007f, StripHalfWidth * 2f), lip, Vector2.one); LipCount++; }
        }
    }

    static bool NextToGap(float x)
    {
        for (int i = 0; i < LevelRunner.Gaps.Count; i++)
            if (x > LevelRunner.Gaps[i].x - 0.001f && x < LevelRunner.Gaps[i].y + 0.001f) return true;
        return false;
    }

    static void Slab(string name, Vector3 pos, Vector3 size, Material m, Vector2 tile)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(_root.transform, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.isStatic = true;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    class Watcher : MonoBehaviour
    {
        bool _last = true, _init;
        void Update()
        {
            bool menu = Menu();
            if (_init && menu == _last) return;
            _init = true;
            _last = menu;
            Apply(menu);
        }
    }
}

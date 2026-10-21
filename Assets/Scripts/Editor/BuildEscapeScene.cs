using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// LOOK-driven scene pass — Imagine handmade felt set (DriveLook/imagine/285.jpg, frames/set-*.jpg)
/// staged as a side-scroller (LittleBigPlanet framing): run +X, lane z = 0, kraft wall at z = 3, camera in front.
/// One clean felt egg bean (pale centre stitch, bead eyes, small smile, stub limbs on animated pivots), burlap
/// start apron (LevelRunner builds the per-level lane with hop gaps), kraft wall + twine + clouds that follow
/// the camera, felt tier pines, red pompom tree, grinder, goal post.
/// Physics-critical bits are unchanged: Imagine Bean capsule (r 0.38 / h 0.86 / dir 1), Rigidbody
/// (0.9 / 0.18 / 0.9), BeanImpulse, gravity -22, fixedDeltaTime 0.02.
public static class BuildEscapeScene
{
    // The Look textures carry the plate colours (generated from DriveLook samples); tints are near-white multipliers.
    static readonly Color FeltBrown = new Color(0.92f, 0.90f, 0.92f);     // clean mid-brown over tex-felt-brown (never > 1: no specks)
    static readonly Color FeltDark = new Color(0.55f, 0.45f, 0.38f);      // trunks / pinecones / knobs over tex-felt-brown
    static readonly Color Thread = new Color(0.88f, 0.80f, 0.64f);        // pale centre stitch (no texture)
    static readonly Color Kraft = new Color(1.00f, 0.98f, 0.94f);         // over tex-kraft-corrugated
    static readonly Color KraftDeep = new Color(0.78f, 0.74f, 0.70f);
    static readonly Color KraftBg = new Color(0.66f, 0.52f, 0.36f);       // camera clear (only seen above the wall)
    static readonly Color FeltPine = new Color(1.00f, 1.00f, 1.00f);      // olive felt (tex-yarn-green)
    static readonly Color FeltPineLight = new Color(1.08f, 1.08f, 0.98f);
    static readonly Color FeltMoss = new Color(0.96f, 1.00f, 0.90f);
    static readonly Color FeltRed = new Color(1.00f, 1.00f, 1.00f);       // over tex-felt-red
    static readonly Color FeltRedLight = new Color(1.10f, 1.06f, 1.02f);
    static readonly Color Cloud = new Color(1.00f, 1.00f, 1.00f);         // over tex-felt-cream
    static readonly Color Yarn = new Color(1.00f, 1.00f, 1.00f);          // over tex-yarn-fiber
    static readonly Color YarnCream = new Color(1.05f, 1.03f, 1.00f);
    static readonly Color TitleCream = new Color(1.00f, 0.98f, 0.94f);
    static readonly Color TitleGold = new Color(1.00f, 0.86f, 0.50f);
    static readonly Color Bead = new Color(0.02f, 0.015f, 0.012f);

    // side-scroll stage: lane z = 0, run +X, kraft wall behind the set, camera in front
    const float WallZ = 3.0f;
    const float BgZ = 1.4f;   // background dressing band centre
    const float FgZ = -0.9f;  // foreground dressing band centre (small items only)

    // bean egg (local space; collider is r 0.38 / h 0.86 around the root)
    const float EggRx = 0.31f, EggRy = 0.39f, EggRz = 0.28f, EggTaper = 0.12f;
    static readonly Vector3 EggCenter = new Vector3(0f, 0.04f, 0f);

    [MenuItem("Escape/Build Scene")]
    public static void Build()
    {
        SetupPipeline.Ensure(); // URP asset assigned to graphics + every quality level
        AssetDatabase.ImportAsset("Assets/Look/FeltFiber.shader");
        AssetDatabase.ImportAsset("Assets/Look/Bead.shader");
        foreach (var t in new[] { "tex-felt-brown", "tex-felt-cream", "tex-felt-red", "tex-yarn-green", "tex-yarn-fiber", "tex-yarn-flower", "tex-burlap", "tex-kraft-corrugated", "tex-roast-bean", "tex-felt-normal", "tex-burlap-normal" })
        {
            ImportLookTexture($"Assets/Look/{t}.png", t.EndsWith("-normal"));
            ImportLookTexture($"Assets/Resources/Look/{t}.png", t.EndsWith("-normal"));
        }
        CraftMesh.ClearSharedCache();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Time.fixedDeltaTime = 0.02f;
        Physics.gravity = new Vector3(0f, -22f, 0f);
        Physics.defaultContactOffset = 0.01f;

        Lights();
        Floor();
        HandmadeSet();

        var bean = StitchedBean();
        bean.transform.position = new Vector3(0f, 0.76f, 0f);
        bean.transform.rotation = Quaternion.Euler(0f, 145f, 0f); // faces the camera, turned a little into the run (285-01)

        var backdrop = Backdrop();
        var bf = backdrop.AddComponent<BackdropFollow>();
        bf.target = bean.transform;
        bf.aheadX = 1.05f;

        var camGo = new GameObject("Set Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = KraftBg;
        // 28 mm-equivalent side view (≈46° vertical): whole bean, burlap in front, twine + clouds on top, like the set frames
        cam.usePhysicalProperties = false;
        cam.fieldOfView = 46.4f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 90f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<SetCameraFollow>();
        follow.target = bean.transform;
        camGo.transform.position = new Vector3(follow.offset.x, follow.restY + follow.offset.y, follow.offset.z);
        camGo.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);

        // Scope: GameSession + Title/Search UI + 5 HQ-level runner
        var sessionGo = new GameObject("Game Session");
        var session = sessionGo.AddComponent<GameSession>();
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        session.catalog = cat;
        var runnerGo = new GameObject("Level Runner");
        var runner = runnerGo.AddComponent<LevelRunner>();
        runner.bean = bean.transform;
        runner.goalMarker = GoalPost().transform;
        runnerGo.AddComponent<LevelSetSpawner>();
        var ui = new GameObject("Title Flow");
        ui.AddComponent<TitleFlow>();

        EditorSceneManager.SaveScene(scene, "Assets/Escape.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("BUILD_SCENE_OK Assets/Escape.unity");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    /// Generated Look textures: repeat wrap, trilinear, normal maps imported as tangent-space normals.
    static void ImportLookTexture(string path, bool normal)
    {
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        bool dirty = false;
        var wantType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (imp.textureType != wantType) { imp.textureType = wantType; dirty = true; }
        if (normal && imp.sRGBTexture) { imp.sRGBTexture = false; dirty = true; }
        if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; dirty = true; }
        if (imp.filterMode != FilterMode.Trilinear) { imp.filterMode = FilterMode.Trilinear; dirty = true; }
        if (imp.anisoLevel < 4) { imp.anisoLevel = 4; dirty = true; }
        if (dirty) imp.SaveAndReimport();
    }

    // ——— lights ———

    static void Lights()
    {
        // Imagine plates: one soft warm key from upper-left-front, soft ground shadows, warm flat bounce.
        var key = new GameObject("Warm Key");
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional;
        kl.intensity = 0.95f;
        kl.color = new Color(1.0f, 0.96f, 0.90f);
        kl.shadows = LightShadows.Soft;
        kl.shadowStrength = 0.62f;
        kl.shadowBias = 0.03f;
        kl.shadowNormalBias = 0.3f;
        key.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

        // Bean Soft Key / Soft Fill HELD (k511 / k494) — values untouched.
        var beanKey = new GameObject("Bean Soft Key");
        var bk = beanKey.AddComponent<Light>();
        bk.type = LightType.Point;
        bk.intensity = 2.956f; // k511 Soft Key HELD (no thrash LAND)
        bk.range = 3.30f; // k210 Soft Key range cover capsule mitts
        bk.color = new Color(1.058f, 0.999f, 0.914f); // k348 Soft Key chroma LAND
        beanKey.transform.position = new Vector3(0.04f, 1.36f, 0.28f); // k201 Soft Key =k184 pos
        var beanFill = new GameObject("Bean Soft Fill");
        var bf = beanFill.AddComponent<Light>();
        bf.type = LightType.Point;
        bf.intensity = 2.576f; // k494 Soft Fill HELD (no thrash LAND)
        bf.range = 3.20f; // k210 Soft Fill range cover mitts
        bf.color = new Color(1.050f, 0.997f, 0.914f); // k348 Soft Fill chroma LAND
        beanFill.transform.position = new Vector3(-0.26f, 1.14f, 0.50f); // k188 Soft Fill =k184 pos

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.43f, 0.38f); // warm table bounce
        RenderSettings.fog = false;
    }

    // ——— floor ———

    static void Floor()
    {
        // start apron only: x ∈ [-5, 3]. LevelRunner builds the rest of the lane per level (burlap blocks + hop gaps).
        // weave ≈ 1.8 cells/m like the plates
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Burlap Floor";
        floor.transform.position = new Vector3(-1f, -0.08f, 0.5f);
        floor.transform.localScale = new Vector3(8f, 0.16f, LevelRunner.LaneDepth);
        PaintTexTiled(floor, "Assets/Look/tex-burlap.png", Color.white, 0.95f, new Vector2(14.4f, 10.8f), 1.4f, 0.08f, 0.70f);
        Grip(floor);
        floor.isStatic = true;
        // frayed edge lip under the apron (reads as the cut edge of the burlap at the first gap)
        var lip = Prim(PrimitiveType.Cube, "Burlap Edge", new Vector3(-1f, -0.17f, 0.5f), new Vector3(8.04f, 0.06f, LevelRunner.LaneDepth + 0.04f), false);
        PaintTexTiled(lip, "Assets/Look/tex-burlap.png", new Color(0.62f, 0.52f, 0.40f), 0.98f, new Vector2(8f, 8f), 1.4f, 0.05f, 0.70f);
    }

    // ——— start set dressing: the Imagine set as a side-scroll stage (set-01..06, 285-01..06) ———

    static void HandmadeSet()
    {
        // background band (z ≈ 1.0 .. 2.4): felt pines left, grinder, red pompom tree right — like set-03 read left → right
        Pine(new Vector3(-4.1f, 0f, BgZ + 0.6f), 1.15f);
        Pine(new Vector3(-3.3f, 0f, BgZ + 1.0f), 0.90f);
        Pine(new Vector3(-2.5f, 0f, BgZ + 0.3f), 0.70f);
        Grinder(new Vector3(-1.4f, 0f, BgZ + 0.8f), 0.9f);
        RedTree(new Vector3(1.3f, 0f, BgZ + 0.5f), 1.0f);
        Pine(new Vector3(2.4f, 0f, BgZ + 0.9f), 0.75f);

        // moss balls: a few just behind the lane, two in front (285 foreground right)
        MossBall(new Vector3(0.9f, 0f, BgZ - 0.5f), 0.26f);
        MossBall(new Vector3(1.6f, 0f, BgZ - 0.3f), 0.18f);
        MossBall(new Vector3(-2.0f, 0f, BgZ - 0.4f), 0.22f);
        MossBall(new Vector3(-0.6f, 0f, FgZ), 0.20f);
        MossBall(new Vector3(2.2f, 0f, FgZ - 0.2f), 0.14f);

        // roast beans scattered on the burlap, both bands
        Vector3[] roast = {
            new Vector3(-3.4f, 0f, FgZ + 0.2f), new Vector3(-1.5f, 0f, FgZ - 0.1f), new Vector3(0.4f, 0f, FgZ + 0.1f),
            new Vector3(1.9f, 0f, FgZ - 0.3f), new Vector3(-2.6f, 0f, BgZ - 0.6f), new Vector3(0.0f, 0f, BgZ - 0.7f),
            new Vector3(2.7f, 0f, BgZ - 0.5f), new Vector3(-4.4f, 0f, BgZ - 0.2f)
        };
        for (int i = 0; i < roast.Length; i++)
            RoastBean(roast[i], 0.09f + (i % 3) * 0.02f, i * 37f);

        // pale felt succulents (285 bottom-left, set-03 bridge foot)
        Succulent(new Vector3(-4.3f, 0f, FgZ + 0.1f), 1.0f);
        Succulent(new Vector3(1.0f, 0f, FgZ - 0.25f), 0.7f);
        Succulent(new Vector3(-0.3f, 0f, BgZ - 0.2f), 0.8f);

        // hanging yarn balls on strings from the twine (set-03) — static, at the start of the run
        var hang = new GameObject("Hanging Yarn");
        hang.transform.position = new Vector3(0f, 0f, WallZ - 0.7f);
        HangingBall(hang.transform, new Vector3(-4.0f, 2.35f, 0f), 0.30f, Yarn);
        HangingBall(hang.transform, new Vector3(-2.6f, 2.70f, 0f), 0.24f, FeltPine);
        HangingBall(hang.transform, new Vector3(1.9f, 2.78f, 0f), 0.22f, YarnCream);
        HangingBall(hang.transform, new Vector3(3.3f, 2.40f, 0f), 0.30f, FeltPine);

        // one stitched felt title on the wall above the start (gen-5ee82b85)
        var sign = new GameObject("Title Sign");
        sign.transform.position = new Vector3(0f, 0f, WallZ - 0.30f);
        PlaceWord(sign.transform, "ESPRESSO", new Vector3(-2.35f, 2.30f, 0f), TitleCream, 0.36f);
        PlaceWord(sign.transform, "ESCAPE", new Vector3(-1.86f, 1.72f, 0f), TitleCream, 0.36f);
    }

    // ——— goal: kraft post with a felt flag (crossing it clears the level) ———

    static GameObject GoalPost()
    {
        var root = new GameObject("Goal Marker");
        root.transform.position = new Vector3(14f, 0f, 0.55f);
        var post = Child(root.transform, PrimitiveType.Cylinder, "Goal Post", new Vector3(0f, 0.75f, 0f), new Vector3(0.07f, 0.75f, 0.07f));
        PaintTexTiled(post, "Assets/Look/tex-kraft-corrugated.png", KraftDeep, 0.88f, new Vector2(1f, 4f), 0.8f, 0.06f, 0.55f);
        var flag = Child(root.transform, PrimitiveType.Cube, "Goal Flag", new Vector3(0.26f, 1.32f, 0f), new Vector3(0.48f, 0.30f, 0.03f));
        PaintTex(flag, "Assets/Look/tex-felt-red.png", FeltRed, 0.93f);
        var knot = Child(root.transform, PrimitiveType.Sphere, "Goal Knot", new Vector3(0f, 1.52f, 0f), Vector3.one * 0.11f);
        PaintTex(knot, "Assets/Look/tex-yarn-fiber.png", TitleGold, 0.85f);
        for (int i = 0; i < 4; i++)
        {
            var ring = Child(root.transform, PrimitiveType.Cylinder, "Yarn Wrap", new Vector3(0f, 0.25f + i * 0.14f, 0f), new Vector3(0.10f, 0.03f, 0.10f));
            PaintTex(ring, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.78f);
        }
        return root;
    }

    // ——— backdrop group: kraft wall, flutes, twine, clouds — follows the camera along X ———

    static GameObject Backdrop()
    {
        var root = new GameObject("Backdrop");
        root.transform.position = new Vector3(0f, 0f, WallZ);

        var wall = Child(root.transform, PrimitiveType.Cube, "Kraft Wall", new Vector3(0f, 3.6f, 0f), new Vector3(18f, 7.5f, 0.30f));
        PaintTexTiled(wall, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.94f, new Vector2(7f, 1f), 0.8f, 0.06f, 0.55f);
        // real flutes for parallax over the corrugation texture
        for (int i = 0; i < 30; i++)
        {
            float x = -8.7f + i * 0.60f;
            var flute = Child(root.transform, PrimitiveType.Cube, "Kraft Flute", new Vector3(x, 3.6f, -0.17f), new Vector3(0.09f, 7.4f, 0.05f));
            PaintTexTiled(flute, "Assets/Look/tex-kraft-corrugated.png", i % 2 == 0 ? KraftDeep : Kraft, 0.94f, new Vector2(0.25f, 1f), 0.8f, 0.06f, 0.55f);
        }

        // twine across the top with three white felt clouds (285)
        var twine = Child(root.transform, PrimitiveType.Cylinder, "Twine", new Vector3(0f, TwineY, -0.55f), new Vector3(0.018f, 9.0f, 0.018f));
        twine.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PaintTex(twine, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.7f);
        FeltCloud(root.transform, new Vector3(-3.4f, TwineY - 0.40f, -0.55f), 0.62f);
        FeltCloud(root.transform, new Vector3(-0.3f, TwineY - 0.55f, -0.55f), 0.52f);
        FeltCloud(root.transform, new Vector3(2.9f, TwineY - 0.38f, -0.55f), 0.58f);
        return root;
    }

    static void FeltCloud(Transform parent, Vector3 pos, float s)
    {
        // three lobes + a top puff, cream felt (tex-felt-cream from the plate clouds)
        Vector3[] lobes = { new Vector3(0f, 0f, 0f), new Vector3(-0.42f, -0.06f, 0.02f), new Vector3(0.44f, -0.05f, 0.02f), new Vector3(0.05f, 0.22f, 0.03f) };
        float[] sc = { 0.62f, 0.44f, 0.46f, 0.40f };
        for (int i = 0; i < lobes.Length; i++)
        {
            var c = Child(parent, PrimitiveType.Sphere, "Felt Cloud", pos + lobes[i] * s, new Vector3(sc[i], sc[i] * 0.78f, sc[i] * 0.80f) * s);
            PaintTex(c, "Assets/Look/tex-felt-cream.png", Cloud, 0.97f);
        }
        var str = Child(parent, PrimitiveType.Cylinder, "Cloud String", pos + new Vector3(0f, 0.30f * s, 0f), new Vector3(0.010f, 0.25f * s, 0.010f));
        PaintTex(str, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.7f);
    }

    const float TwineY = 3.30f; // twine height: just inside the top of the 46° side view

    static void HangingBall(Transform parent, Vector3 pos, float s, Color col)
    {
        float top = TwineY;
        var str = Child(parent, PrimitiveType.Cylinder, "Hang String", new Vector3(pos.x, (top + pos.y) * 0.5f, pos.z), new Vector3(0.012f, (top - pos.y) * 0.5f, 0.012f));
        PaintTex(str, "Assets/Look/tex-yarn-fiber.png", YarnCream, 0.7f);
        var ball = Child(parent, PrimitiveType.Sphere, "Yarn Ball", pos, Vector3.one * s);
        PaintTex(ball, col == FeltPine ? "Assets/Look/tex-yarn-green.png" : "Assets/Look/tex-yarn-fiber.png", col, 0.92f);
        for (int k = 0; k < 4; k++)
        {
            float a = k * 1.57f + 0.4f;
            var bump = Child(parent, PrimitiveType.Sphere, "Yarn Bump", pos + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 1.2f) * 0.6f, Mathf.Sin(a)) * (s * 0.42f), Vector3.one * (s * 0.30f));
            PaintTex(bump, col == FeltPine ? "Assets/Look/tex-yarn-green.png" : "Assets/Look/tex-yarn-fiber.png", col, 0.9f);
        }
    }

    // ——— stitched felt letters ———

    static void PlaceWord(Transform parent, string word, Vector3 start, Color color, float size)
    {
        for (int i = 0; i < word.Length; i++)
        {
            var pos = start + new Vector3(i * (size + 0.10f), 0f, 0f);
            StuffedLetter(parent, word[i], pos, size, color);
        }
    }

    static void StuffedLetter(Transform parent, char c, Vector3 lp, float size, Color col)
    {
        var root = new GameObject("Title " + c);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = lp;
        float t = size * 0.17f; // stroke thickness
        float d = size * 0.34f; // depth
        float h = size * 1.05f;
        float w = size * 0.72f;
        void Bar(Vector3 p, Vector3 sc)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            b.name = "GlyphBar";
            b.transform.SetParent(root.transform, false);
            b.transform.localPosition = p;
            bool vertical = sc.y >= sc.x;
            if (vertical)
                b.transform.localScale = new Vector3(Mathf.Max(sc.x, sc.z) * 1.1f, sc.y * 0.55f, Mathf.Max(sc.x, sc.z) * 1.1f);
            else
            {
                b.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                b.transform.localScale = new Vector3(Mathf.Max(sc.y, sc.z) * 1.1f, sc.x * 0.55f, Mathf.Max(sc.y, sc.z) * 1.1f);
            }
            Object.DestroyImmediate(b.GetComponent<Collider>());
            PaintTex(b, null, col, 0.92f);
        }
        void Stem(float x, float y0, float y1) => Bar(new Vector3(x, (y0 + y1) * 0.5f, 0f), new Vector3(t, Mathf.Abs(y1 - y0), d));
        void Cross(float y, float x0, float x1) => Bar(new Vector3((x0 + x1) * 0.5f, y, 0f), new Vector3(Mathf.Abs(x1 - x0), t, d));
        switch (char.ToUpperInvariant(c))
        {
            case 'E':
                Stem(-w * 0.35f, -h * 0.5f, h * 0.5f);
                Cross(h * 0.42f, -w * 0.35f, w * 0.4f);
                Cross(0f, -w * 0.35f, w * 0.28f);
                Cross(-h * 0.42f, -w * 0.35f, w * 0.4f);
                break;
            case 'S':
                Cross(h * 0.42f, -w * 0.35f, w * 0.35f);
                Stem(-w * 0.35f, 0f, h * 0.42f);
                Cross(0f, -w * 0.35f, w * 0.35f);
                Stem(w * 0.35f, -h * 0.42f, 0f);
                Cross(-h * 0.42f, -w * 0.35f, w * 0.35f);
                break;
            case 'P':
                Stem(-w * 0.35f, -h * 0.5f, h * 0.5f);
                Cross(h * 0.42f, -w * 0.35f, w * 0.35f);
                Stem(w * 0.35f, 0f, h * 0.42f);
                Cross(0f, -w * 0.35f, w * 0.35f);
                break;
            case 'R':
                Stem(-w * 0.35f, -h * 0.5f, h * 0.5f);
                Cross(h * 0.42f, -w * 0.35f, w * 0.35f);
                Stem(w * 0.35f, 0f, h * 0.42f);
                Cross(0f, -w * 0.35f, w * 0.35f);
                Bar(new Vector3(w * 0.18f, -h * 0.25f, 0f), new Vector3(t * 1.1f, h * 0.55f, d));
                break;
            case 'O':
                Stem(-w * 0.35f, -h * 0.35f, h * 0.35f);
                Stem(w * 0.35f, -h * 0.35f, h * 0.35f);
                Cross(h * 0.42f, -w * 0.35f, w * 0.35f);
                Cross(-h * 0.42f, -w * 0.35f, w * 0.35f);
                break;
            case 'A':
                Stem(-w * 0.35f, -h * 0.5f, h * 0.35f);
                Stem(w * 0.35f, -h * 0.5f, h * 0.35f);
                Cross(h * 0.42f, -w * 0.2f, w * 0.2f);
                Cross(0f, -w * 0.3f, w * 0.3f);
                break;
            case 'C':
                Stem(-w * 0.35f, -h * 0.35f, h * 0.35f);
                Cross(h * 0.42f, -w * 0.35f, w * 0.35f);
                Cross(-h * 0.42f, -w * 0.35f, w * 0.35f);
                break;
            default:
                Bar(Vector3.zero, new Vector3(w * 0.85f, h * 0.9f, d));
                break;
        }
        // four corner knots read as stitching without clutter
        for (int s = 0; s < 4; s++)
        {
            float a = (s / 4f) * Mathf.PI * 2f + Mathf.PI * 0.25f;
            var knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knot.name = "Title Knot";
            knot.transform.SetParent(root.transform, false);
            knot.transform.localPosition = new Vector3(Mathf.Cos(a) * w * 0.52f, Mathf.Sin(a) * h * 0.52f, -d * 0.45f);
            knot.transform.localScale = Vector3.one * (size * 0.07f);
            Object.DestroyImmediate(knot.GetComponent<Collider>());
            CraftMesh.UseLowSphere(knot);
            PaintTex(knot, null, Thread, 0.6f);
        }
    }

    // ——— the bean ———

    static GameObject StitchedBean()
    {
        var root = new GameObject("Imagine Bean");
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.9f;
        rb.linearDamping = 0.18f;
        rb.angularDamping = 0.9f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.38f;
        col.height = 0.86f; // k64 squat collider
        col.direction = 1;
        root.AddComponent<BeanImpulse>();
        root.AddComponent<LandFeel>();
        root.AddComponent<BeanAnimator>(); // hop poses from frames 285-01..06 (limb pivots + body only)

        // one clean felt egg — LOOK 285 body (no lobes, no trench, no naps)
        var body = new GameObject("Bean Body");
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = EggCenter;
        body.AddComponent<MeshFilter>().sharedMesh = CraftMesh.Egg(EggRx, EggRy, EggRz, EggTaper, 40, 28);
        var bodyR = body.AddComponent<MeshRenderer>();
        bodyR.sharedMaterial = BeanMaterial(true);
        bodyR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // pale centre stitch: 12 short thread dashes down the front meridian (name kept: Dash Stitch)
        var threadMat = CraftMesh.Shared("thread", () => CraftMesh.Felt(null, Thread, Vector2.one, 0.6f, 0.10f, 0.7f, 0.9f));
        for (int i = 0; i < 12; i++)
        {
            float lat = Mathf.Lerp(-58f, 66f, i / 11f);
            Vector3 p0 = CraftMesh.EggPoint(EggRx, EggRy, EggRz, EggTaper, lat, 0f);
            Vector3 p1 = CraftMesh.EggPoint(EggRx, EggRy, EggRz, EggTaper, lat + 4f, 0f);
            Vector3 n = new Vector3(p0.x / (EggRx * EggRx), p0.y / (EggRy * EggRy), p0.z / (EggRz * EggRz)).normalized;
            var dash = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dash.name = "Dash Stitch";
            dash.transform.SetParent(root.transform, false);
            dash.transform.localPosition = EggCenter + p0 + n * 0.004f + Vector3.right * (((i % 3) - 1) * 0.0015f);
            dash.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (p1 - p0).normalized);
            dash.transform.localScale = new Vector3(0.011f, 0.017f, 0.011f);
            Object.DestroyImmediate(dash.GetComponent<Collider>());
            var dr = dash.GetComponent<Renderer>();
            dr.sharedMaterial = threadMat;
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // glossy bead eyes + highlight dots
        var beadMat = CraftMesh.Shared("bead-eye", () => CraftMesh.Bead(Bead, 0.95f));
        var hlMat = CraftMesh.Shared("bead-hl", () => CraftMesh.Bead(new Color(0.98f, 0.97f, 0.94f), 0.2f));
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 p = CraftMesh.EggPoint(EggRx, EggRy, EggRz, EggTaper, 9f, side * 24f);
            Vector3 n = new Vector3(p.x / (EggRx * EggRx), p.y / (EggRy * EggRy), p.z / (EggRz * EggRz)).normalized;
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            e.name = "Eye";
            e.transform.SetParent(root.transform, false);
            e.transform.localPosition = EggCenter + p + n * 0.012f;
            e.transform.localScale = Vector3.one * 0.062f;
            Object.DestroyImmediate(e.GetComponent<Collider>());
            var er = e.GetComponent<Renderer>();
            er.sharedMaterial = beadMat;
            er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var hl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hl.name = "Eye Highlight";
            hl.transform.SetParent(root.transform, false);
            hl.transform.localPosition = e.transform.localPosition + n * 0.027f + Vector3.up * 0.011f + Vector3.left * (side * 0.009f);
            hl.transform.localScale = Vector3.one * 0.015f;
            Object.DestroyImmediate(hl.GetComponent<Collider>());
            var hr = hl.GetComponent<Renderer>();
            hr.sharedMaterial = hlMat;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // small thread smile: a shallow 7-bead arc under the eyes
        var smileMat = CraftMesh.Shared("bead-smile", () => CraftMesh.Bead(new Color(0.07f, 0.04f, 0.03f), 0.5f));
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            float lon = Mathf.Lerp(-11f, 11f, t);
            float lat = -6.5f - Mathf.Sin(t * Mathf.PI) * 2.6f;
            Vector3 p = CraftMesh.EggPoint(EggRx, EggRy, EggRz, EggTaper, lat, lon);
            Vector3 n = new Vector3(p.x / (EggRx * EggRx), p.y / (EggRy * EggRy), p.z / (EggRz * EggRz)).normalized;
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = "Smile";
            s.transform.SetParent(root.transform, false);
            s.transform.localPosition = EggCenter + p + n * 0.005f;
            s.transform.localScale = Vector3.one * 0.012f;
            Object.DestroyImmediate(s.GetComponent<Collider>());
            CraftMesh.UseLowSphere(s);
            var sr = s.GetComponent<Renderer>();
            sr.sharedMaterial = smileMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // short rounded mitt arms and stub legs. Each limb gets one small tucked Junction sphere where it
        // meets the egg and one soft Nap shell (1.15, 1.05, 1.15) so the seam reads as a sewn joint —
        // a soft joint, not a second torso. No Felt Fleck / Stub Fleck.
        var limbMat = BeanMaterial(false);
        Arm(root.transform, -1, limbMat);
        Arm(root.transform, 1, limbMat);
        Leg(root.transform, -1, limbMat);
        Leg(root.transform, 1, limbMat);
        return root;
    }

    static readonly Vector3 JunctionScale = new Vector3(0.28f, 0.22f, 0.26f);
    static readonly Vector3 NapScale = new Vector3(1.15f, 1.05f, 1.15f);

    /// Soft fuzzy shell over a limb capsule: same felt, a touch more rim fuzz, no shadow.
    static Material NapMaterial()
    {
        return CraftMesh.Shared("bean-nap", () =>
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Look/tex-felt-brown.png");
            var m = CraftMesh.Felt(tex, FeltBrown, new Vector2(2.5f, 2.5f), 1.0f, 0.22f, 0.66f, 0.96f, 0f);
            if (m.HasProperty("_FiberAmt")) m.SetFloat("_FiberAmt", 0.06f);
            return m;
        });
    }

    /// Small felt sphere tucked at the root end of a limb capsule (limb-local scale 0.28/0.22/0.26),
    /// half sunk into the egg so the limb reads as sewn on.
    static void Junction(Transform limb, string name, Material mat)
    {
        var j = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        j.name = name;
        j.transform.SetParent(limb, false);
        j.transform.localPosition = new Vector3(0f, -0.92f, 0f); // capsule root end (unit capsule spans y ±1)
        j.transform.localScale = JunctionScale;
        Object.DestroyImmediate(j.GetComponent<Collider>());
        CraftMesh.UseLowSphere(j);
        var r = j.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Nap(Transform limb, string name)
    {
        var n = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        n.name = name;
        n.transform.SetParent(limb, false);
        n.transform.localPosition = Vector3.zero;
        n.transform.localScale = NapScale;
        Object.DestroyImmediate(n.GetComponent<Collider>());
        var r = n.GetComponent<Renderer>();
        r.sharedMaterial = NapMaterial();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Material BeanMaterial(bool seam)
    {
        return CraftMesh.Shared(seam ? "bean-body" : "bean-limb", () =>
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Look/tex-felt-brown.png");
            var m = CraftMesh.Felt(tex, FeltBrown, new Vector2(2.5f, 2.5f), 1.0f, 0.14f, 0.62f, 0.94f, seam ? 0.65f : 0f);
            if (m.HasProperty("_SeamWidth")) m.SetFloat("_SeamWidth", 0.020f);
            if (m.HasProperty("_FiberAmt")) m.SetFloat("_FiberAmt", 0.07f);
            return m;
        });
    }

    // Limbs hang from pivot empties ("Arm Pivot L/R", "Leg Pivot L/R") at the shoulder / hip on the egg
    // surface, identity rotation at rest; BeanAnimator swings and spreads the pivots (285 hop frames).

    static void Arm(Transform parent, int side, Material mat)
    {
        bool left = side < 0;
        var pivot = new GameObject(left ? "Arm Pivot L" : "Arm Pivot R");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(side * 0.255f, 0.02f, 0.01f); // shoulder on the egg
        pivot.transform.localRotation = Quaternion.identity;

        var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        arm.name = "Arm";
        arm.transform.SetParent(pivot.transform, false);
        // capsule +Y (mitt end) points mostly down and a little out: Euler z = -side·160° → (side·0.34, -0.94)
        // BeanAnimator adds ≈20° of outward spread at rest (285-06) and ≈60° in the air (285-03)
        arm.transform.localRotation = Quaternion.Euler(left ? 6f : -4f, 0f, -side * 160f);
        arm.transform.localPosition = arm.transform.localRotation * Vector3.up * 0.115f; // root end sits on the pivot
        arm.transform.localScale = new Vector3(0.105f, 0.13f, 0.105f);
        Object.DestroyImmediate(arm.GetComponent<Collider>());
        arm.GetComponent<Renderer>().sharedMaterial = mat;
        // tucked joint where the arm enters the egg (half sunk into the body), plus the soft nap shell
        Junction(arm.transform, "Arm Junction", mat);
        Nap(arm.transform, "Arm Nap");
        // rounded mitt at the tip
        var mitt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mitt.name = "Arm Mitt";
        mitt.transform.SetParent(arm.transform, false);
        mitt.transform.localPosition = new Vector3(0f, 0.95f, 0f); // along the capsule axis (scaled space)
        mitt.transform.localScale = new Vector3(1.22f, 0.92f, 1.18f);
        Object.DestroyImmediate(mitt.GetComponent<Collider>());
        mitt.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Leg(Transform parent, int side, Material mat)
    {
        bool left = side < 0;
        var pivot = new GameObject(left ? "Leg Pivot L" : "Leg Pivot R");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(side * 0.12f, -0.30f, 0.02f); // hip under the egg
        pivot.transform.localRotation = Quaternion.identity;

        var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leg.name = "Leg";
        leg.transform.SetParent(pivot.transform, false);
        leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 4f);
        leg.transform.localPosition = new Vector3(0f, -0.065f, 0f);
        leg.transform.localScale = new Vector3(0.12f, 0.085f, 0.12f);
        Object.DestroyImmediate(leg.GetComponent<Collider>());
        leg.GetComponent<Renderer>().sharedMaterial = mat;
        // tucked joint under the egg where the leg starts, plus the soft nap shell
        Junction(leg.transform, "Leg Junction", mat);
        Nap(leg.transform, "Leg Nap");
        var foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foot.name = "Foot";
        foot.transform.SetParent(pivot.transform, false);
        foot.transform.localPosition = new Vector3(side * 0.005f, -0.115f, 0.03f); // toe a touch forward (toward the face)
        foot.transform.localScale = new Vector3(0.15f, 0.095f, 0.17f);
        Object.DestroyImmediate(foot.GetComponent<Collider>());
        foot.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ——— props ———

    /// Felt pine: trunk + 5 stacked flattened tiers (wide → narrow) + tip + pinecone dots (LOOK 285).
    static void Pine(Vector3 basePos, float scale)
    {
        var trunk = Prim(PrimitiveType.Cylinder, "Pine Trunk", basePos + Vector3.up * (0.14f * scale), new Vector3(0.11f, 0.16f, 0.11f) * scale, false);
        PaintTex(trunk, "Assets/Look/tex-felt-brown.png", FeltDark, 0.9f);
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            float w = Mathf.Lerp(0.98f, 0.36f, t);
            float y = Mathf.Lerp(0.36f, 1.28f, t);
            var tier = Prim(PrimitiveType.Sphere, "Pine Tier", basePos + Vector3.up * (y * scale), new Vector3(w, w * 0.40f, w) * scale, false);
            PaintTex(tier, "Assets/Look/tex-yarn-green.png", i % 2 == 0 ? FeltPine : FeltPineLight, 0.94f);
        }
        var tip = Prim(PrimitiveType.Sphere, "Pine Tip", basePos + Vector3.up * (1.48f * scale), new Vector3(0.24f, 0.30f, 0.24f) * scale, false);
        PaintTex(tip, "Assets/Look/tex-yarn-green.png", FeltPine, 0.94f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.26f + 0.5f;
            float r = Mathf.Lerp(0.40f, 0.20f, i / 4f);
            var cone = Prim(PrimitiveType.Sphere, "Pinecone", basePos + new Vector3(Mathf.Cos(a) * r, 0.50f + i * 0.20f, Mathf.Sin(a) * r) * scale, Vector3.one * 0.10f * scale, false);
            PaintTex(cone, "Assets/Look/tex-felt-brown.png", FeltDark, 0.85f);
        }
    }

    /// Red pompom tree: brown trunk, three branches, seven red felt balls (LOOK 285 right).
    static void RedTree(Vector3 basePos, float scale)
    {
        var trunk = Prim(PrimitiveType.Cylinder, "Red Trunk", basePos + Vector3.up * (0.45f * scale), new Vector3(0.09f, 0.45f, 0.09f) * scale, false);
        PaintTex(trunk, "Assets/Look/tex-felt-brown.png", FeltDark, 0.9f);
        Vector3[] branchDir = { new Vector3(-0.55f, 0.75f, 0.1f), new Vector3(0.6f, 0.7f, -0.1f), new Vector3(0.05f, 0.9f, 0.25f) };
        for (int b = 0; b < branchDir.Length; b++)
        {
            Vector3 d = branchDir[b].normalized;
            Vector3 mid = basePos + Vector3.up * (0.86f * scale) + d * (0.22f * scale);
            var br = Prim(PrimitiveType.Cylinder, "Red Branch", mid, new Vector3(0.05f, 0.24f, 0.05f) * scale, false);
            br.transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
            PaintTex(br, "Assets/Look/tex-felt-brown.png", FeltDark, 0.9f);
        }
        // nine red felt balls in a loose crown (285 right: 7–9 pompoms on a brown trunk)
        Vector3[] poms = {
            new Vector3(-0.42f, 1.30f, 0.05f), new Vector3(-0.18f, 1.52f, -0.08f), new Vector3(0.12f, 1.62f, 0.10f),
            new Vector3(0.42f, 1.42f, -0.05f), new Vector3(0.55f, 1.12f, 0.08f), new Vector3(-0.55f, 1.02f, -0.06f),
            new Vector3(0.05f, 1.22f, 0.22f), new Vector3(-0.30f, 1.08f, 0.18f), new Vector3(0.32f, 1.00f, -0.16f)
        };
        for (int i = 0; i < poms.Length; i++)
        {
            float s = (0.26f + (i % 3) * 0.04f) * scale;
            var pom = Prim(PrimitiveType.Sphere, "Red Pompom", basePos + poms[i] * scale, Vector3.one * s, false);
            PaintTex(pom, "Assets/Look/tex-felt-red.png", i % 2 == 0 ? FeltRed : FeltRedLight, 0.93f);
        }
    }

    static void MossBall(Vector3 basePos, float s)
    {
        var m = Prim(PrimitiveType.Sphere, "Moss", basePos + Vector3.up * (s * 0.46f), Vector3.one * s, false);
        PaintTex(m, "Assets/Look/tex-yarn-green.png", FeltMoss, 0.94f);
    }

    static void RoastBean(Vector3 basePos, float scale, float yaw)
    {
        var root = new GameObject("Roast Prop");
        root.transform.position = basePos + Vector3.up * (scale * 0.42f);
        root.transform.rotation = Quaternion.Euler(0f, yaw, 18f);
        root.isStatic = true;
        // glossy dark roast bean (plates: near-black brown, one lighter crease): tex-roast-bean carries the crease line
        var tex = "Assets/Look/tex-roast-bean.png";
        var l = Child(root.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(-0.30f * scale, 0f, 0f), new Vector3(scale * 1.0f, scale * 0.78f, scale * 0.68f));
        PaintTexTiled(l, tex, Color.white, 0.38f, Vector2.one, 0.6f, 0.30f, 0.50f);
        var r = Child(root.transform, PrimitiveType.Sphere, "Roast Lobe", new Vector3(0.30f * scale, 0f, 0f), new Vector3(scale * 1.0f, scale * 0.78f, scale * 0.68f));
        PaintTexTiled(r, tex, new Color(0.94f, 0.94f, 0.94f), 0.38f, Vector2.one, 0.6f, 0.30f, 0.50f);
        var crease = Child(root.transform, PrimitiveType.Capsule, "Roast Crease", Vector3.zero, new Vector3(scale * 0.16f, scale * 0.30f, scale * 0.16f));
        crease.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        PaintTexTiled(crease, tex, new Color(0.55f, 0.50f, 0.46f), 0.6f, Vector2.one, 0.6f, 0.10f, 0.50f);
    }

    static void Succulent(Vector3 basePos, float scale)
    {
        var sage = new Color(1.18f, 1.14f, 1.06f); // paler sage over the olive felt (285 bottom-left)
        for (int ring = 0; ring < 2; ring++)
        {
            int n = ring == 0 ? 7 : 5;
            float r = ring == 0 ? 0.13f : 0.06f;
            for (int i = 0; i < n; i++)
            {
                float a = i * (Mathf.PI * 2f / n) + ring * 0.45f;
                var leaf = Prim(PrimitiveType.Sphere, "Succulent Leaf",
                    basePos + new Vector3(Mathf.Cos(a) * r, 0.11f + ring * 0.07f, Mathf.Sin(a) * r) * scale,
                    new Vector3(0.09f, 0.24f - ring * 0.05f, 0.07f) * scale, false);
                leaf.transform.rotation = Quaternion.Euler(Mathf.Sin(a) * 28f, 0f, -Mathf.Cos(a) * 28f);
                PaintTex(leaf, "Assets/Look/tex-yarn-green.png", sage, 0.93f);
            }
        }
    }

    static void Grinder(Vector3 basePos, float scale)
    {
        var body = Prim(PrimitiveType.Cube, "Cardboard Grinder", basePos + Vector3.up * (0.52f * scale), new Vector3(0.80f, 1.0f, 0.70f) * scale);
        PaintTexTiled(body, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.9f, new Vector2(2.5f, 1f), 0.8f, 0.06f, 0.55f);
        for (int d = 0; d < 2; d++)
        {
            var drawer = Prim(PrimitiveType.Cube, "Grinder Drawer", basePos + new Vector3(0f, (0.26f + d * 0.30f) * scale, 0.36f * scale), new Vector3(0.62f, 0.20f, 0.08f) * scale, false);
            PaintTexTiled(drawer, "Assets/Look/tex-kraft-corrugated.png", KraftDeep, 0.88f, new Vector2(2f, 1f), 0.8f, 0.06f, 0.55f);
            var knob = Prim(PrimitiveType.Sphere, "Drawer Knob", drawer.transform.position + new Vector3(0f, 0f, 0.06f * scale), Vector3.one * 0.07f * scale, false);
            PaintTex(knob, "Assets/Look/tex-felt-brown.png", FeltDark, 0.7f);
        }
        // set-03: a corrugated bowl hopper sitting on the box, crank handle out of the top
        var bowl = Prim(PrimitiveType.Cylinder, "Grinder Hopper", basePos + Vector3.up * (1.12f * scale), new Vector3(0.52f, 0.10f, 0.52f) * scale, false);
        PaintTexTiled(bowl, "Assets/Look/tex-kraft-corrugated.png", KraftDeep, 0.88f, new Vector2(3f, 1f), 0.8f, 0.06f, 0.55f);
        var bowlRim = Prim(PrimitiveType.Sphere, "Grinder Hopper Rim", basePos + Vector3.up * (1.20f * scale), new Vector3(0.46f, 0.16f, 0.46f) * scale, false);
        PaintTexTiled(bowlRim, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.88f, new Vector2(3f, 1f), 0.8f, 0.06f, 0.55f);
        var crank = Prim(PrimitiveType.Cylinder, "Grinder Crank", basePos + new Vector3(0.26f, 1.26f, 0f) * scale, new Vector3(0.05f, 0.26f, 0.05f) * scale, false);
        crank.transform.rotation = Quaternion.Euler(0f, 0f, 70f);
        PaintTex(crank, "Assets/Look/tex-felt-brown.png", FeltDark, 0.75f);
        var crankKnob = Prim(PrimitiveType.Sphere, "Crank Knob", basePos + new Vector3(0.50f, 1.34f, 0f) * scale, Vector3.one * 0.10f * scale, false);
        PaintTex(crankKnob, "Assets/Look/tex-felt-brown.png", FeltDark, 0.7f);
    }

    // ——— helpers ———

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, bool keepCollider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static GameObject Child(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Grip(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col == null) return;
        col.material = new PhysicsMaterial("BurlapGrip")
        {
            dynamicFriction = 0.95f,
            staticFriction = 1f,
            bounciness = 0.02f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    /// Shared FeltFiber material per (texture, tint, roughness). texPath null → clean white felt.
    static void PaintTex(GameObject go, string texPath, Color tint, float roughness)
    {
        string path = texPath ?? "";
        bool yarnish = path.Contains("yarn") || path.Contains("green") || path.Contains("flower");
        float bump = 1.0f, sheen = 0.12f, wrap = 0.62f, tile = 1.2f;
        if (path.Contains("burlap")) { bump = 1.4f; sheen = 0.08f; wrap = 0.70f; tile = 10f; }
        else if (path.Contains("kraft")) { bump = 0.8f; sheen = 0.06f; wrap = 0.55f; tile = 7.5f; }
        else if (yarnish) { bump = 1.2f; sheen = 0.16f; wrap = 0.66f; tile = 3.0f; }
        else if (string.IsNullOrEmpty(path)) { bump = 0.8f; sheen = 0.14f; wrap = 0.66f; tile = 1f; }
        PaintTexTiled(go, texPath, tint, roughness, new Vector2(tile, tile), bump, sheen, wrap);
    }

    static void PaintTexTiled(GameObject go, string texPath, Color tint, float roughness, Vector2 tile, float bump, float sheen, float wrap)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        string path = texPath ?? "";
        string key = CraftMesh.Key(path, tint, tile, bump, sheen, wrap, roughness, 0f);
        var mat = CraftMesh.Shared(key, () =>
        {
            var tex = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, roughness);
        });
        r.sharedMaterial = mat;
    }
}

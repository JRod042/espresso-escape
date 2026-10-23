using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// LOOK-driven mesh pass: stitched coffee-bean + dense handmade diorama.
public static class BuildEscapeScene
{
    static readonly Color Felt = new Color(0.45f, 0.28f, 0.17f);
    static readonly Color FeltDark = new Color(0.30f, 0.17f, 0.10f);
    static readonly Color Stitch = new Color(0.92f, 0.86f, 0.72f);
    static readonly Color Burlap = new Color(0.71f, 0.57f, 0.39f);
    static readonly Color Kraft = new Color(0.300f, 0.216f, 0.132f); // k223 Kraft darken dSET ≤+11
    static readonly Color KraftDark = new Color(0.208f, 0.144f, 0.082f); // k223 KraftDark dSET ≤+11
    static readonly Color FeltPine = new Color(0.27f, 0.45f, 0.25f);
    static readonly Color FeltRed = new Color(0.72f, 0.22f, 0.16f);
    static readonly Color Cloud = new Color(0.97f, 0.96f, 0.93f);
    static readonly Color Yarn = new Color(0.80f, 0.64f, 0.44f);
    static readonly Color TitleCream = new Color(0.93f, 0.88f, 0.74f);
    static readonly Color TitleGold = new Color(0.86f, 0.68f, 0.28f);

    [MenuItem("Escape/Build Scene")]
    public static void Build()
    {
        AssetDatabase.ImportAsset("Assets/Look/BeadUnlit.shader");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Time.fixedDeltaTime = 0.02f;
        Physics.gravity = new Vector3(0f, -22f, 0f);
        Physics.defaultContactOffset = 0.01f;

        Lights();
        FloorWall();
        PathBridge();
        HandmadeSet();
        StuffedTitle();

        var bean = StitchedBean();
        bean.transform.position = new Vector3(0f, 0.76f, 0.35f);
        // mid-stride lean like LOOK bean-motion
        bean.transform.rotation = Quaternion.Euler(-10.0f, 180f, 7.0f); // k338 LAND pose lean −10/180/7 (yaw 180; Z≤k308)

        var camGo = new GameObject("Set Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = KraftDark;
        cam.fieldOfView = 36f;
        cam.nearClipPlane = 0.05f;
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0.04f, 1.18f, -2.70f); // k188 =k184 plate framing (pose-only stub poke)
        camGo.transform.rotation = Quaternion.Euler(7f, 0f, 0f); // k188 =k184
        var follow = camGo.AddComponent<SetCameraFollow>();
        follow.target = bean.transform;
        follow.offset = new Vector3(0.04f, 0.98f, -2.50f); // k188 =k184
        // soft tabletop DOF (physical camera - best-effort without URP volume)
        cam.usePhysicalProperties = true;
        cam.focalLength = 50f;
        cam.aperture = 2.2f;
        cam.focusDistance = 2.4f;
        cam.gateFit = Camera.GateFitMode.Horizontal;
        SnapFaceToCamera(bean.transform, cam); // k495 eyes/smile on cam-facing skin (ForwardBase overlay)

        // Scope: GameSession + Title/Search UI + 5 HQ-level runner
        var sessionGo = new GameObject("Game Session");
        var session = sessionGo.AddComponent<GameSession>();
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        session.catalog = cat;
        var runnerGo = new GameObject("Level Runner");
        var runner = runnerGo.AddComponent<LevelRunner>();
        runner.bean = bean.transform;
        var goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goal.name = "Goal Marker";
        goal.transform.position = new Vector3(0f, 0.5f, 12f);
        goal.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
        Object.DestroyImmediate(goal.GetComponent<Collider>());
        PaintTex(goal, "Assets/Look/tex-yarn-fiber.png", TitleGold, 0.85f);
        runner.goalMarker = goal.transform;
        runnerGo.AddComponent<LevelSetSpawner>();
        // Scope UI: TitleFlow + SearchMenu (5 HQ levels) - requires com.unity.ugui
        var ui = new GameObject("Title Flow");
        ui.AddComponent<TitleFlow>();


        EditorSceneManager.SaveScene(scene, "Assets/Escape.unity");
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }

    static void Lights()
    {
        // k43 LAND: Soft Key/Fill EXACT k39; PaintBean/albedo/wrap only (best dBean/-SET tradeoff)
        var key = new GameObject("Warm Key");
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional;
        kl.intensity = 0.672f; // k275 Warm Key cut dSET
        kl.color = new Color(1.010f, 0.976f, 0.934f); // k297 Warm Key LAND
        kl.shadows = LightShadows.Soft;
        key.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

        var fill = new GameObject("Warm Fill");
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.intensity = 0.22f; // k169 Warm Fill LAND
        fl.color = new Color(0.95f, 0.95f, 0.94f);
        fill.transform.rotation = Quaternion.Euler(18f, 120f, 0f);

        var bounce = new GameObject("Bounce");
        var bl = bounce.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.intensity = 0.680f; // k275 Bounce cut dSET
        bl.range = 10f;
        bl.color = new Color(0.985f, 0.965f, 0.940f); // k202 Bounce cooler chroma ~69
        bounce.transform.position = new Vector3(0.15f, 2.6f, 1.4f);
        var rim = new GameObject("Warm Rim");
        var rl = rim.AddComponent<Light>();
        rl.type = LightType.Directional;
        rl.intensity = 0.22f; // k167 Warm Rim LAND
        rl.color = new Color(1f, 0.95f, 0.88f);
        rim.transform.rotation = Quaternion.Euler(-22f, 200f, 0f);
        // soft under-table fill for contact-shadow readability
        var under = new GameObject("Table Fill");
        var ul = under.AddComponent<Light>();
        ul.type = LightType.Point;
        ul.intensity = 0.045f; // k223 Table Fill dSET
        ul.range = 5.2f;
        ul.color = new Color(1f, 0.96f, 0.92f);
        under.transform.position = new Vector3(0.08f, 1.02f, 0.85f); // k146 Table Fill

        // bean Soft Key/Fill EXACT k39 - material-only bean lift (k43 land)
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
        RenderSettings.ambientLight = new Color(0.55f, 0.53f, 0.52f); // k37 locked
        RenderSettings.ambientIntensity = 0.768f; // k275 ambient cut dSET
    }

    static void FloorWall()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Burlap Floor";
        floor.transform.position = new Vector3(0f, -0.08f, 3.2f);
        floor.transform.localScale = new Vector3(11f, 0.16f, 16f);
        // rotate-floor.mp4 opening cloth averages 175,140,95. White on tex-burlap
        // (201,165,116) renders about 174,136,90, so lift green and blue a little.
        PaintTex(floor, "Assets/Look/tex-burlap.png", new Color(1.006f, 1.029f, 1.063f), 0.95f);
        var floorMat = floor.GetComponent<Renderer>().sharedMaterial;
        // tex-burlap is 10 cells per repeat. Square weave, 1.6 repeats per metre
        // on the 11 x 16 top (U along X, V along Z). Replaces the stretched 9x9 tile.
        var floorTile = new Vector2(floor.transform.localScale.x * 1.6f, floor.transform.localScale.z * 1.6f);
        if (floorMat.HasProperty("_BaseMap")) floorMat.SetTextureScale("_BaseMap", floorTile);
        if (floorMat.HasProperty("_MainTex")) floorMat.SetTextureScale("_MainTex", floorTile);
        if (floorMat.HasProperty("_BumpMap")) floorMat.SetTextureScale("_BumpMap", floorTile);
        // Felt fuzz at 2.95 turns this weave into sand. The cloth is the albedo grid.
        if (floorMat.HasProperty("_BumpScale")) floorMat.SetFloat("_BumpScale", 0f);
        if (floorMat.HasProperty("_FiberAmt")) floorMat.SetFloat("_FiberAmt", 0f);
        Grip(floor);
        var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seam.name = "FloorWall Seam";
        seam.transform.position = new Vector3(0f, 0.05f, 8.0f);
        seam.transform.localScale = new Vector3(0.040f, 0.34f, 0.040f); // k482 stub seam soft LAND
        PaintTex(seam, "Assets/Look/tex-kraft-corrugated.png", KraftDark, 0.9f);
        Object.DestroyImmediate(seam.GetComponent<Collider>());

        for (int i = 0; i < 36; i++)
        {
            var rib = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rib.name = "Kraft Rib " + i;
            rib.transform.position = new Vector3(-7.7f + i * 0.44f, 2.5f, 8.4f);
            rib.transform.localScale = new Vector3(0.40f, 6.4f, 0.36f);
            // alternate ridge depth for corrugated cardboard side-read - k27
            if (i % 2 == 1) rib.transform.localScale = new Vector3(0.22f, 6.4f, 0.14f);
            PaintTex(rib, "Assets/Look/tex-kraft-corrugated.png", (i % 2 == 0) ? new Color(0.72f,0.66f,0.58f) : new Color(0.50f,0.42f,0.34f), 0.94f); // k63
            Object.DestroyImmediate(rib.GetComponent<Collider>());
            // thin flute face strip for kraft side corrugation read
            if (i % 2 == 0)
            {
                var flute = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flute.name = "Kraft Flute";
                flute.transform.position = new Vector3(-7.7f + i * 0.44f, 2.5f, 8.18f);
                flute.transform.localScale = new Vector3(0.06f, 6.2f, 0.08f);
                PaintTex(flute, "Assets/Look/tex-kraft-corrugated.png", new Color(0.58f, 0.48f, 0.38f), 0.92f); // k63
                Object.DestroyImmediate(flute.GetComponent<Collider>());
            }
        }
    }

    static void PathBridge()
    {
        var path = GameObject.CreatePrimitive(PrimitiveType.Cube);
        path.name = "Burlap Path";
        path.transform.position = new Vector3(0f, 0.02f, 2.6f);
        path.transform.localScale = new Vector3(2.2f, 0.12f, 12f);
        PaintTex(path, "Assets/Look/tex-burlap.png", new Color(0.82f,0.78f,0.72f), 0.95f); // k67 SET hold
        Grip(path);
        for (int e = 0; e < 18; e++)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var edge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                edge.name = "Path Yarn Edge";
                edge.transform.position = new Vector3(s * 1.05f, 0.09f, 0.4f + e * 0.55f);
                edge.transform.localScale = Vector3.one * 0.05f;
                PaintTex(edge, "Assets/Look/tex-yarn-fiber.png", Stitch, 0.6f); // k63
                Object.DestroyImmediate(edge.GetComponent<Collider>());
            }
        }

        // arched cardboard bridge - planked deck + corrugated rails (LOOK set plate)
        for (int p = 0; p < 11; p++)
        {
            float tArc = (p / 10f) * Mathf.PI;
            float y = 0.22f + Mathf.Sin(tArc) * 0.42f;
            float z = 4.45f + p * 0.16f;
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Bridge Plank";
            plank.transform.position = new Vector3(0f, y, z);
            plank.transform.localScale = new Vector3(1.55f, 0.06f, 0.15f);
            plank.transform.localRotation = Quaternion.Euler(Mathf.Cos(tArc) * -30f, 0f, 0f);
            PaintTex(plank, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.90f);
            Grip(plank);
        }

        for (int s = -1; s <= 1; s += 2)
        {
            for (int p = 0; p < 5; p++)
            {
                float tArc = (p / 4f) * Mathf.PI;
                float y = 0.55f + Mathf.Sin(tArc) * 0.42f;
                float z = 4.7f + p * 0.28f;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Bridge Post";
                post.transform.position = new Vector3(s * 0.78f, y, z);
                post.transform.localScale = new Vector3(0.06f, 0.22f, 0.06f);
                PaintTex(post, "Assets/Look/tex-kraft-corrugated.png", KraftDark, 0.88f);
                Object.DestroyImmediate(post.GetComponent<Collider>());
            }
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Bridge Rail";
            rail.transform.position = new Vector3(s * 0.78f, 0.95f, 5.25f);
            rail.transform.localScale = new Vector3(0.05f, 0.05f, 1.55f);
            PaintTex(rail, "Assets/Look/tex-kraft-corrugated.png", KraftDark, 0.88f);
            Object.DestroyImmediate(rail.GetComponent<Collider>());
        }

        for (int i = 0; i < 3; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Step";
            step.transform.position = new Vector3(0f, 0.10f + i * 0.11f, 4.05f + i * 0.22f);
            step.transform.localScale = new Vector3(1.45f, 0.09f, 0.28f);
            PaintTex(step, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.90f);
            Grip(step);
        }
        // felt grass tufts at bridge foot (LOOK set plate)
        for (int g = 0; g < 8; g++)
        {
            float side = (g % 2 == 0) ? -1f : 1f;
            var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tuft.name = "Bridge Grass";
            tuft.transform.position = new Vector3(side * (0.85f + (g % 4) * 0.12f), 0.10f + (g % 3) * 0.03f, 4.3f + (g % 3) * 0.15f);
            tuft.transform.localScale = new Vector3(0.12f, 0.18f + (g % 2) * 0.05f, 0.10f);
            PaintTex(tuft, "Assets/Look/tex-yarn-green.png", FeltPine, 0.93f);
            Object.DestroyImmediate(tuft.GetComponent<Collider>());
        }
    }

    static void HandmadeSet()
    {
        Pine(new Vector3(-2.15f, 1.0f, 3.5f), 1.2f);
        Pine(new Vector3(-2.6f, 0.78f, 4.5f), 0.92f);
        Pine(new Vector3(2.5f, 0.72f, 5.0f), 0.78f);
        Pine(new Vector3(-1.7f, 0.65f, 5.4f), 0.70f);
        Pine(new Vector3(1.85f, 0.68f, 4.7f), 0.65f);
        RedTree(new Vector3(2.2f, 1.08f, 3.55f));
        // secondary bloom tree (LOOK set density)
        RedTree(new Vector3(2.85f, 0.72f, 4.4f));

        for (int i = 0; i < 4; i++)
        {
            Vector3 basePos = new Vector3(-1.9f + i * 1.15f, 3.7f + (i % 2) * 0.15f, 7.25f);
            for (int p = 0; p < 5; p++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.name = "Felt Cloud";
                float a = p * 1.4f;
                c.transform.position = basePos + new Vector3(Mathf.Cos(a) * 0.22f, (p % 2) * 0.08f, Mathf.Sin(a) * 0.12f);
                c.transform.localScale = new Vector3(0.55f + (p % 3) * 0.12f, 0.32f + (p % 2) * 0.08f, 0.40f);
                PaintTex(c, "Assets/Look/tex-felt-brown.png", Cloud, 0.96f);
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
        }
        var twine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        twine.name = "Twine";
        twine.transform.position = new Vector3(0f, 3.9f, 7.3f);
        twine.transform.localScale = new Vector3(6.2f, 0.025f, 0.025f);
        PaintTex(twine, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.7f); // k63
        Object.DestroyImmediate(twine.GetComponent<Collider>());

        for (int i = 0; i < 4; i++)
        {
            Vector3 ballPos = new Vector3(-1.85f + i * 1.15f, 3.15f - (i % 2) * 0.15f, 7.05f);
            var stringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stringGo.name = "Hang String";
            stringGo.transform.position = new Vector3(ballPos.x, (3.9f + ballPos.y) * 0.5f, ballPos.z);
            stringGo.transform.localScale = new Vector3(0.012f, Mathf.Abs(3.9f - ballPos.y) * 0.5f, 0.012f);
            PaintTex(stringGo, "Assets/Look/tex-yarn-fiber.png", Stitch, 0.7f); // k63
            Object.DestroyImmediate(stringGo.GetComponent<Collider>());
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Yarn Ball";
            ball.transform.position = ballPos;
            ball.transform.localScale = Vector3.one * (0.2f + (i % 2) * 0.07f);
            PaintTex(ball, "Assets/Look/tex-yarn-fiber.png", i % 2 == 0 ? Yarn : FeltPine, 0.92f);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            // crochet surface bumps on hanging orbs
            for (int k = 0; k < 6; k++)
            {
                float a = k * 1.05f;
                var bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bump.name = "Yarn Bump";
                bump.transform.position = ballPos + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 1.2f), Mathf.Sin(a)) * (0.09f + (i % 2) * 0.02f);
                bump.transform.localScale = Vector3.one * 0.06f;
                PaintTex(bump, "Assets/Look/tex-yarn-fiber.png", i % 2 == 0 ? Yarn : FeltPine, 0.9f);
                Object.DestroyImmediate(bump.GetComponent<Collider>());
            }
        }

        MossBall(new Vector3(-1.05f, 0.16f, 1.55f), 0.26f);
        MossBall(new Vector3(-0.5f, 0.12f, 1.95f), 0.18f);
        MossBall(new Vector3(1.2f, 0.15f, 1.65f), 0.22f);
        MossBall(new Vector3(0.55f, 0.11f, 1.35f), 0.14f);
        MossBall(new Vector3(-1.35f, 0.10f, 2.1f), 0.15f);
        MossBall(new Vector3(0.2f, 0.09f, 4.15f), 0.12f);
        // soft felt contact shadows (tabletop LOOK read without URP)
        Vector3[] shadowPts = {
            new Vector3(0f, 0.025f, 0.35f), new Vector3(-1.6f, 0.025f, 1.1f),
            new Vector3(1.65f, 0.025f, 1.3f), new Vector3(-2.7f, 0.025f, 2.25f),
            new Vector3(-2.15f, 0.025f, 3.5f), new Vector3(2.2f, 0.025f, 3.55f),
            new Vector3(2.5f, 0.025f, 5.0f), new Vector3(1.55f, 0.025f, 1.05f),
            new Vector3(-1.05f, 0.025f, 1.55f), new Vector3(0.9f, 0.025f, 2.35f)
        };
        float[] shadowSc = { 0.76f, 0.24f, 0.24f, 0.78f, 0.55f, 0.50f, 0.40f, 0.28f, 0.20f, 0.16f };
        for (int i = 0; i < shadowPts.Length; i++)
        {
            var sh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sh.name = "Contact Shadow";
            sh.transform.position = shadowPts[i];
            sh.transform.localScale = new Vector3(shadowSc[i], 0.004f, shadowSc[i] * 0.72f);
            Paint(sh, new Color(0.22f, 0.15f, 0.10f, 1f), 0.99f);
            Object.DestroyImmediate(sh.GetComponent<Collider>());
        }
        Vector3[] roast = {
            new Vector3(-1.6f, 0.07f, 1.1f), new Vector3(1.65f, 0.07f, 1.3f),
            new Vector3(0.9f, 0.07f, 2.35f), new Vector3(-0.8f, 0.07f, 1.4f),
            new Vector3(0.35f, 0.07f, 1.8f), new Vector3(1.2f, 0.07f, 0.95f),
            new Vector3(-1.2f, 0.07f, 2.5f), new Vector3(0.1f, 0.07f, 3.1f),
            new Vector3(1.9f, 0.07f, 2.8f), new Vector3(-2.0f, 0.07f, 1.7f)
        };
        for (int i = 0; i < roast.Length; i++)
            RoastBean(roast[i], 0.09f + (i % 3) * 0.02f, i * 37f);

        var grinder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grinder.name = "Cardboard Grinder";
        grinder.transform.position = new Vector3(-2.7f, 0.55f, 2.25f);
        grinder.transform.localScale = new Vector3(0.95f, 1.05f, 0.72f);
        PaintTex(grinder, "Assets/Look/tex-kraft-corrugated.png", Kraft, 0.9f);

        for (int d = 0; d < 2; d++)
        {
            var drawer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drawer.name = "Grinder Drawer";
            drawer.transform.position = new Vector3(-2.7f, 0.28f + d * 0.32f, 2.55f);
            drawer.transform.localScale = new Vector3(0.78f, 0.22f, 0.12f);
            PaintTex(drawer, "Assets/Look/tex-kraft-corrugated.png", KraftDark, 0.88f);
            Object.DestroyImmediate(drawer.GetComponent<Collider>());
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Drawer Knob";
            knob.transform.position = drawer.transform.position + new Vector3(0f, 0f, 0.08f);
            knob.transform.localScale = Vector3.one * 0.07f;
            PaintTex(knob, "Assets/Look/tex-felt-brown.png", FeltDark, 0.7f); // k63
            Object.DestroyImmediate(knob.GetComponent<Collider>());
        }

        var hopper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hopper.name = "Grinder Hopper";
        hopper.transform.position = new Vector3(-2.7f, 1.15f, 2.25f);
        hopper.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
        PaintTex(hopper, "Assets/Look/tex-kraft-corrugated.png", KraftDark, 0.88f);
        Object.DestroyImmediate(hopper.GetComponent<Collider>());
        var crank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crank.name = "Grinder Crank";
        crank.transform.position = new Vector3(-2.45f, 1.28f, 2.25f);
        crank.transform.localScale = new Vector3(0.06f, 0.32f, 0.06f);
        crank.transform.rotation = Quaternion.Euler(0f, 0f, 75f);
        PaintTex(crank, "Assets/Look/tex-kraft-corrugated.png", FeltDark, 0.75f); // k63
        Object.DestroyImmediate(crank.GetComponent<Collider>());
        var crankKnob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crankKnob.name = "Crank Knob";
        crankKnob.transform.position = new Vector3(-2.15f, 1.38f, 2.25f);
        crankKnob.transform.localScale = Vector3.one * 0.10f;
        PaintTex(crankKnob, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.7f); // k63
        Object.DestroyImmediate(crankKnob.GetComponent<Collider>());

        // pale felt succulent in yarn pot (LOOK set/285)
        var pot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pot.name = "Succulent Pot";
        pot.transform.position = new Vector3(1.55f, 0.12f, 1.05f);
        pot.transform.localScale = new Vector3(0.32f, 0.16f, 0.32f);
        PaintTex(pot, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.9f);
        Object.DestroyImmediate(pot.GetComponent<Collider>());
        for (int i = 0; i < 7; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Succulent Leaf";
            float a = i * 0.95f;
            leaf.transform.position = new Vector3(1.55f + Mathf.Cos(a) * 0.10f, 0.22f + (i % 3) * 0.05f, 1.05f + Mathf.Sin(a) * 0.10f);
            leaf.transform.localScale = new Vector3(0.10f, 0.20f + (i % 2) * 0.04f, 0.08f);
            leaf.transform.localRotation = Quaternion.Euler(15f, a * Mathf.Rad2Deg, 10f);
            PaintTex(leaf, "Assets/Look/tex-yarn-green.png", new Color(0.62f, 0.72f, 0.48f), 0.92f);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }
        // keep a small left succulent cluster too
        for (int i = 0; i < 4; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "Succulent Leaf";
            float a = i * 1.4f;
            leaf.transform.position = new Vector3(-1.9f + Mathf.Cos(a) * 0.10f, 0.16f + (i % 2) * 0.05f, 1.0f + Mathf.Sin(a) * 0.10f);
            leaf.transform.localScale = new Vector3(0.12f, 0.18f, 0.09f);
            PaintTex(leaf, "Assets/Look/tex-yarn-green.png", new Color(0.62f, 0.72f, 0.48f), 0.92f);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }
    }

    static void StuffedTitle()
    {
        // stacked stuffed titles like LOOK bean-motion plate
        PlaceWord("ESPRESSO", new Vector3(-2.85f, 3.15f, 7.85f), TitleCream, 0.42f, false);
        PlaceWord("ESCAPE", new Vector3(-1.95f, 2.55f, 7.85f), TitleGold, 0.42f, true);
        PlaceWord("ESPRESSO", new Vector3(-2.55f, 1.95f, 7.70f), TitleCream, 0.48f, false);
        PlaceWord("ESCAPE", new Vector3(-1.65f, 1.30f, 7.70f), TitleGold, 0.48f, true);
        PlaceWord("CASA", new Vector3(-0.75f, 0.75f, 7.55f), Stitch, 0.24f, false);
    }

    static void PlaceWord(string word, Vector3 start, Color color, float size, bool alternate)
    {
        for (int i = 0; i < word.Length; i++)
        {
            var pos = start + new Vector3(i * (size + 0.12f), 0f, 0f);
            Color letterCol = color;
            if (alternate && (i % 2 == 1)) letterCol = new Color(0.92f, 0.45f, 0.18f);
            StuffedLetter(word[i], pos, size, letterCol);
        }
    }

    // letter-shaped stuffed felt glyphs (LOOK bean-motion plate - not cubes)
    static void StuffedLetter(char c, Vector3 pos, float size, Color col)
    {
        var root = new GameObject("Title " + c);
        root.transform.position = pos;
        float t = size * 0.16f; // stroke thickness
        float d = size * 0.38f; // depth
        float h = size * 1.05f;
        float w = size * 0.72f;
        void Bar(Vector3 lp, Vector3 sc)
        {
            // capsule stroke = stuffed sausage letter (LOOK bean-motion glyphs)
            var b = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            b.name = "GlyphBar";
            b.transform.SetParent(root.transform, false);
            b.transform.localPosition = lp;
            bool vertical = sc.y >= sc.x;
            if (vertical)
            {
                b.transform.localScale = new Vector3(Mathf.Max(sc.x, sc.z) * 1.1f, sc.y * 0.55f, Mathf.Max(sc.x, sc.z) * 1.1f);
            }
            else
            {
                b.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                b.transform.localScale = new Vector3(Mathf.Max(sc.y, sc.z) * 1.1f, sc.x * 0.55f, Mathf.Max(sc.y, sc.z) * 1.1f);
            }
            PaintTex(b, "Assets/Look/tex-felt-brown.png", col, 0.92f);
            Object.DestroyImmediate(b.GetComponent<Collider>());
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "GlyphPuff";
            puff.transform.SetParent(root.transform, false);
            puff.transform.localPosition = lp;
            puff.transform.localScale = new Vector3(sc.x * 1.2f, sc.y * 0.65f, sc.z * 1.15f);
            PaintTex(puff, "Assets/Look/tex-felt-brown.png", col, 0.94f);
            Object.DestroyImmediate(puff.GetComponent<Collider>());
        }
        void Stem(float x, float y0, float y1)
        {
            float mid = (y0 + y1) * 0.5f;
            float len = Mathf.Abs(y1 - y0);
            Bar(new Vector3(x, mid, 0f), new Vector3(t, len, d));
        }
        void Cross(float y, float x0, float x1)
        {
            float mid = (x0 + x1) * 0.5f;
            float len = Mathf.Abs(x1 - x0);
            Bar(new Vector3(mid, y, 0f), new Vector3(len, t, d));
        }
        char u = char.ToUpperInvariant(c);
        switch (u)
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
            case 'T':
                Stem(0f, -h * 0.5f, h * 0.35f);
                Cross(h * 0.42f, -w * 0.4f, w * 0.4f);
                break;
            default:
                // rounded stuffed block fallback
                Bar(Vector3.zero, new Vector3(w * 0.85f, h * 0.9f, d));
                break;
        }
        // French-knot perimeter stitches
        for (int s = 0; s < 8; s++)
        {
            float a = (s / 8f) * Mathf.PI * 2f;
            var knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knot.name = "Title Knot";
            knot.transform.SetParent(root.transform, false);
            knot.transform.localPosition = new Vector3(Mathf.Cos(a) * w * 0.55f, Mathf.Sin(a) * h * 0.55f, d * 0.55f);
            knot.transform.localScale = Vector3.one * (size * 0.08f);
            PaintTex(knot, "Assets/Look/tex-felt-brown.png", Stitch, 0.55f); // k63
            Object.DestroyImmediate(knot.GetComponent<Collider>());
        }
    }

    static GameObject StitchedBean()
    {
        // k489 PIVOT: LOOK 285 stranger silhouette FIRST (eyes/smile/seam) — wool metric ignored
        // upright ovoid coffee-bean + vertical tan dash stitches + bead eyes + stub mid-stride limbs
        // soft needle-felt via FeltFiber - REMOVE spike/orbiting fringe/halo that made lumpy CG ball
        // cam-k48 structure FAIL - no soft structure wins; Prompt 10 REJECT until stranger-match
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

        // k501 MESH: LOOK 285 two-lobe coffee-bean (NOT single potato ovoid)
        // left/right lobes + mesh cleft valley; Soft Key 2.956 HELD; Soft Nap cliffs HELD
        void SoftNapMat(GameObject go)
        {
            var nr = go.GetComponent<Renderer>();
            if (nr == null || nr.sharedMaterial == null) return;
            var m = new Material(nr.sharedMaterial);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0.92f); // k494 Soft Nap bump HELD
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1.148f, 1.102f, 1.006f)); // k333 =k323 HELD
            if (m.HasProperty("_Sheen")) m.SetFloat("_Sheen", 0.217f); // k468 Soft Nap sheen HELD
            if (m.HasProperty("_Wrap")) m.SetFloat("_Wrap", 0.858f); // k328 Soft Nap wrap HELD
            nr.sharedMaterial = m;
        }

        // k674 MESH CLEFTNEST-DARK: CleftNestDark on k664 mid-strip HELD — Soft Key 2.956 HELD; padX/open86/bodyX/yaw45 HELD; NO Face Pad remesh; NO BeadUnlit Overlay; NO cheek lift
        // Bean Cleft REMOVED; Soft Nap front-Z cut; no bunny-ear Y; Soft Key 2.956 HELD.
        // k556 Bean Core SPLIT — continuous core was the mid-gap fill behind open V; L/R spines leave AIR mid
        var coreL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreL.name = "Bean Core L";
        coreL.transform.SetParent(root.transform, false);
        coreL.transform.localPosition = new Vector3(-0.172f, 0.007f, -0.205f); // k664 core Z mid-carve AO (no pad remesh)
        coreL.transform.localScale = new Vector3(0.312f, 0.362f, 0.108f); // k664 core X AO wall mid-carve
        coreL.transform.localRotation = Quaternion.Euler(0f, -17.0f, 0f);
        Object.DestroyImmediate(coreL.GetComponent<Collider>());
        PaintBean(coreL);
        var coreR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreR.name = "Bean Core R";
        coreR.transform.SetParent(root.transform, false);
        coreR.transform.localPosition = new Vector3(0.172f, 0.007f, -0.205f); // k664 core Z mid-carve AO (no pad remesh)
        coreR.transform.localScale = new Vector3(0.312f, 0.362f, 0.108f); // k664 core X AO wall mid-carve
        coreR.transform.localRotation = Quaternion.Euler(0f, 17.0f, 0f);
        Object.DestroyImmediate(coreR.GetComponent<Collider>());
        PaintBean(coreR);

        var bodyL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bodyL.name = "Bean Lobe L";
        bodyL.transform.SetParent(root.transform, false);
        bodyL.transform.localPosition = new Vector3(-0.218f, 0.02f, -0.112f); // k664 body Z mid-carve AO (X HELD; no peanut)
        bodyL.transform.localScale = new Vector3(0.460f, 0.355f, 0.236f); // k664 body Z thin mid-carve shade
        bodyL.transform.localRotation = Quaternion.Euler(0f, -35.0f, 0f); // k586 LAND yaw // k585 fuse yaw // k584 body yaw max // k583 body yaw open // k581 body yaw cleft // k576 body yaw cleft // k571 body yaw cleft // k566 body yaw cleft // k561 body yaw cleft // k555 body yaw cleft // k551 body yaw outward // k546 body yaw outward — geometric cleft // k541 body yaw outward — geometric cleft // k536 body yaw outward — geometric cleft
        Object.DestroyImmediate(bodyL.GetComponent<Collider>());
        PaintBean(bodyL);

        var bodyR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bodyR.name = "Bean Lobe R";
        bodyR.transform.SetParent(root.transform, false);
        bodyR.transform.localPosition = new Vector3(0.218f, 0.02f, -0.112f); // k664 body Z mid-carve AO (X HELD; no peanut)
        bodyR.transform.localScale = new Vector3(0.460f, 0.355f, 0.236f); // k664 body Z thin mid-carve shade
        bodyR.transform.localRotation = Quaternion.Euler(0f, 35.0f, 0f); // k586 LAND yaw // k585 fuse yaw // k584 body yaw max // k583 body yaw open // k581 body yaw cleft // k576 body yaw cleft // k571 body yaw cleft // k566 body yaw cleft // k561 body yaw cleft // k555 body yaw cleft // k551 body yaw outward // k546 body yaw outward — geometric cleft // k541 body yaw outward — geometric cleft // k536 body yaw outward — geometric cleft
        Object.DestroyImmediate(bodyR.GetComponent<Collider>());
        PaintBean(bodyR);

        // Soft Nap on cheeks, front-Z CUT so nap does not occupy crease plane
        var napL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        napL.name = "Felt Nap L";
        napL.transform.SetParent(root.transform, false);
        napL.transform.localPosition = new Vector3(-0.205f, 0.02f, -0.210f); // k586 LAND nap // k585 nap fuse // k584 nap with body // k583 nap with body — no Soft Nap X-gap // k581 nap behind — no Soft Nap X-gap // k576 nap behind — no Soft Nap X-gap // k571 nap behind — no Soft Nap X-gap // k566 nap behind — no Soft Nap X-gap // k561 nap behind — no Soft Nap X-gap // k551 nap behind — no Soft Nap X-gap // k546 nap behind — no Soft Nap X-gap // k541 nap behind — no Soft Nap X-gap // k536 nap behind — no Soft Nap X-gap // k529 nap behind — no Soft Nap X-gap // k524 nap behind // k523 nap behind cheek // k522 nap behind cheek // k521 nap behind cheek // k516 behind cheek front
        napL.transform.localScale = new Vector3(0.205f, 0.338f, 0.045f); // k581 front-Z cut HELD // k576 front-Z cut HELD // k571 front-Z cut HELD // k566 front-Z cut HELD // k561 front-Z cut HELD // k551 front-Z cut HELD // k546 front-Z cut HELD // k541 front-Z cut HELD // k536 front-Z cut HELD // k529 front-Z cut HELD // k524 front-Z cut HELD // k523 front-Z cut HELD // k522 front-Z cut HELD // k521 front-Z cut HELD // k516 front-Z cut
        napL.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(napL.GetComponent<Collider>());
        PaintBean(napL); SoftNapMat(napL);

        var napR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        napR.name = "Felt Nap R";
        napR.transform.SetParent(root.transform, false);
        napR.transform.localPosition = new Vector3(0.205f, 0.02f, -0.210f); // k586 LAND nap // k585 nap fuse // k584 nap with body // k583 nap with body
        napR.transform.localScale = new Vector3(0.205f, 0.338f, 0.045f); // k581 front-Z cut HELD // k576 front-Z cut HELD // k571 front-Z cut HELD // k566 front-Z cut HELD // k561 front-Z cut HELD // k551 front-Z cut HELD // k546 front-Z cut HELD // k541 front-Z cut HELD // k536 front-Z cut HELD // k529 front-Z cut HELD // k524 front-Z cut HELD // k523 front-Z cut HELD // k522 front-Z cut HELD // k521 front-Z cut HELD // k516 front-Z cut
        napR.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(napR.GetComponent<Collider>());
        PaintBean(napR); SoftNapMat(napR);

        // k582 Face Pad L/R DUAL-LOBE shells — widen X + thin medial so mid is CAM-EMPTY AIR (frontRecess NOT primary; face punch on shells)
        var padL = new GameObject("Face Pad L");
        padL.transform.SetParent(root.transform, false);
        padL.transform.localPosition = new Vector3(-0.305f, 0.005f, 0.150f); // k586 LAND dual-lobe padX
        padL.transform.localScale = new Vector3(0.148f, 0.248f, 0.218f); // k586 LAND lobe shell
        padL.transform.localRotation = Quaternion.Euler(0f, -45.0f, 0f); // k586 LAND shell yaw
        var padLmf = padL.AddComponent<MeshFilter>();
        padLmf.sharedMesh = MakeFacePadHalfShellMesh(-1, 0.500f, 0.500f, 0.430f, 86.0f, 0.260f, 16, 12, 0.240f, 0.860f); // k633 medialFloor=0.240 lipProud=0.860 midShade=0.080 open86 HELD
        padL.AddComponent<MeshRenderer>();
        PaintBean(padL);
        var padR = new GameObject("Face Pad R");
        padR.transform.SetParent(root.transform, false);
        padR.transform.localPosition = new Vector3(0.305f, 0.005f, 0.150f); // k586 LAND dual-lobe padX
        padR.transform.localScale = new Vector3(0.148f, 0.248f, 0.218f); // k586 LAND lobe shell
        padR.transform.localRotation = Quaternion.Euler(0f, 45.0f, 0f); // k586 LAND shell yaw
        var padRmf = padR.AddComponent<MeshFilter>();
        padRmf.sharedMesh = MakeFacePadHalfShellMesh(1, 0.500f, 0.500f, 0.430f, 86.0f, 0.260f, 16, 12, 0.240f, 0.860f); // k633 medialFloor=0.240 lipProud=0.860 midShade=0.080 open86 HELD
        padR.AddComponent<MeshRenderer>();
        PaintBean(padR);

        // k552 Pad Channel L/R PARKED tiny rear — medial felt walls were re-filling carved mid-air (k547-551 FAIL); empty AIR between Face Pads is the trough
        var chL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chL.name = "Pad Channel L";
        chL.transform.SetParent(root.transform, false);
        chL.transform.localPosition = new Vector3(-0.020f, 0.009f, -0.620f); // k581 PARKED rear — mid AIR // k576 PARKED rear — mid AIR // k571 PARKED rear — mid AIR // k566 PARKED rear — mid AIR // k561 PARKED rear — mid AIR // k552 PARKED rear — mid AIR
        chL.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k581 PARKED tiny // k576 PARKED tiny // k571 PARKED tiny // k566 PARKED tiny // k561 PARKED tiny // k552 PARKED tiny
        chL.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(chL.GetComponent<Collider>());
        PaintBean(chL);
        var chR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chR.name = "Pad Channel R";
        chR.transform.SetParent(root.transform, false);
        chR.transform.localPosition = new Vector3(0.020f, 0.009f, -0.620f); // k581 PARKED rear — mid AIR // k576 PARKED rear — mid AIR // k571 PARKED rear — mid AIR // k566 PARKED rear — mid AIR // k561 PARKED rear — mid AIR // k552 PARKED rear — mid AIR
        chR.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k581 PARKED tiny // k576 PARKED tiny // k571 PARKED tiny // k566 PARKED tiny // k561 PARKED tiny // k552 PARKED tiny
        chR.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(chR.GetComponent<Collider>());
        PaintBean(chR);

        // k582 Cheek Shell PARKED — medial walls were re-filling cam mid (want empty AIR cleft, not felt walls)
        var shL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shL.name = "Cheek Shell L";
        shL.transform.SetParent(root.transform, false);
        shL.transform.localPosition = new Vector3(-0.020f, 0.005f, -0.620f); // k582 PARKED rear — mid AIR
        shL.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k582 PARKED tiny
        shL.transform.localRotation = Quaternion.Euler(0f, 38.5f, 0f);
        Object.DestroyImmediate(shL.GetComponent<Collider>());
        Paint(shL, new Color(0.200f, 0.110f, 0.060f), 0.96f);
        var shR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shR.name = "Cheek Shell R";
        shR.transform.SetParent(root.transform, false);
        shR.transform.localPosition = new Vector3(0.020f, 0.005f, -0.620f); // k582 PARKED rear — mid AIR
        shR.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k582 PARKED tiny
        shR.transform.localRotation = Quaternion.Euler(0f, -38.5f, 0f);
        Object.DestroyImmediate(shR.GetComponent<Collider>());
        Paint(shR, new Color(0.200f, 0.110f, 0.060f), 0.96f);

        // k633 Bean Trench = SeamFelt/FeltFiber DARK SEAM FLOOR between proud lips (not Valley Shade; not Pad Channel; not peanut; not sky hole; not Overlay sticker)
        var trenchGo = new GameObject("Bean Trench");
        trenchGo.transform.SetParent(root.transform, false);
        trenchGo.transform.localPosition = new Vector3(0f, 0.010f, 0.152f); // k633 dark seam floor cam-Z
        trenchGo.transform.localRotation = Quaternion.identity;
        var trenchFilter = trenchGo.AddComponent<MeshFilter>();
        trenchFilter.sharedMesh = MakeBeanTrenchMesh(0.180f, 0.260f, 0.360f, 0.080f, 6, 8); // k633 SeamFelt dark seam cavity
        var trenchRend = trenchGo.AddComponent<MeshRenderer>();
        PaintSeamFelt(trenchGo, new Color(0.006f, 0.002f, 0.001f), 0.280f, 0.020f, 0.985f); // k633 SeamFelt dark seam floor

        // k633 SOLID SEAM PLUG — SeamFelt/FeltFiber depth-tested MID-BAND floor (NOT Valley Shade; NOT Pad Channel; NOT SoftNap-X; not peanut; NOT BeadUnlit Overlay)
        var seamPlug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seamPlug.name = "Seam Plug";
        seamPlug.transform.SetParent(root.transform, false);
        seamPlug.transform.localPosition = new Vector3(0f, 0.010f, 0.170f); // k633 mid-band plug cam-Z
        seamPlug.transform.localScale = new Vector3(0.300f, 0.420f, 0.260f); // k633 mid-band FeltFiber occluder (not full-height sticker)
        Object.DestroyImmediate(seamPlug.GetComponent<Collider>());
        PaintSeamFelt(seamPlug, new Color(0.003f, 0.001f, 0.000f), 0.280f, 0.020f, 0.985f); // k633 SeamFelt depth-tested mid-band seam (NO Overlay Always)







        // k586 Valley Recess PARKED — dual-lobe empty AIR cleft (not capsule strip)
        var valley = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        valley.name = "Valley Recess";
        valley.transform.SetParent(root.transform, false);
        valley.transform.localPosition = new Vector3(0.000f, 0.020f, -0.620f); // k581 PARKED rear // k576 PARKED rear // k571 PARKED rear // k566 PARKED rear // k561 PARKED rear // k551 PARKED rear // k546 PARKED rear // k541 PARKED rear // k536 PARKED rear // k529 cam-visible trough // k525 recessed in pad valley // k524 parked rear (no fill) // k523 between pads
        valley.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k581 PARKED tiny // k576 PARKED tiny // k571 PARKED tiny // k566 PARKED tiny // k561 PARKED tiny // k551 PARKED tiny // k546 PARKED tiny // k541 PARKED tiny // k536 PARKED tiny // k529 wide shade trough // k525 thin shade trough // k524 disabled
        Object.DestroyImmediate(valley.GetComponent<Collider>());
        Paint(valley, new Color(0.040f, 0.020f, 0.010f), 0.99f); // k551 PARKED // k546 PARKED // k541 PARKED // k536 PARKED // k529 dark Valley Shade


        // modest crown notch — not antlers
        var tipL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tipL.name = "Crown Tip L";
        tipL.transform.SetParent(root.transform, false);
        tipL.transform.localPosition = new Vector3(-0.058f, 0.160f, -0.052f);
        tipL.transform.localScale = new Vector3(0.094f, 0.034f, 0.078f);
        Object.DestroyImmediate(tipL.GetComponent<Collider>());
        PaintBean(tipL);
        var tipR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tipR.name = "Crown Tip R";
        tipR.transform.SetParent(root.transform, false);
        tipR.transform.localPosition = new Vector3(0.058f, 0.160f, -0.052f);
        tipR.transform.localScale = new Vector3(0.094f, 0.034f, 0.078f);
        Object.DestroyImmediate(tipR.GetComponent<Collider>());
        PaintBean(tipR);

        // Bean Cleft fill-capsule REMOVED (k512+) — valley is the gap between front cheeks over core

        // dark crease in the cheek valley (short — do not spear out the crown)
        var crease = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        crease.name = "Crease";
        crease.transform.SetParent(root.transform, false);
        crease.transform.localPosition = new Vector3(0f, 0.02f, -0.620f); // k582 PARKED rear — mid AIR (dashes are LOOK seam)
        crease.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f); // k582 PARKED tiny
        Object.DestroyImmediate(crease.GetComponent<Collider>());
        Paint(crease, new Color(0.034f, 0.015f, 0.007f), 0.99f); // k582 PARKED

        // LOOK 285: vertical tan DASH stitches down center (NOT baseball V / side zigzag)
        // k60: LOOK tan yarn dashes - warm brown-tan, short, irregular yaw
        for (int i = 0; i < 12; i++)
        {
            float y = -0.32f + i * 0.060f;
            float jig = ((i % 3) - 1) * 0.006f;
            var dash = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dash.name = "Dash Stitch";
            dash.transform.SetParent(root.transform, false);
            dash.transform.localPosition = new Vector3(jig, y, 0.075f); // k633 yarn IN dark seam recess
            dash.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f); // k633 yarn seed
            dash.transform.localRotation = Quaternion.Euler(8f, 0f, -18f + i * 3f);
            Object.DestroyImmediate(dash.GetComponent<Collider>());
            PaintTex(dash, "Assets/Look/tex-yarn-fiber.png", new Color(0.72f, 0.58f, 0.38f), 0.88f); // k63 yarn dash
        }

        // stub mid-stride limbs (LOOK: short rounded, L forward)
        Arm(root.transform, -0.40f);
        Arm(root.transform, 0.40f);
        Foot(root.transform, -0.14f);
        Foot(root.transform, 0.14f);

        // k495: face beads created in SnapFaceToCamera after MainCamera exists
        // (local +Z Unlit/Color never punched through FeltFiber on cam-k494)

        // k61: sparse short surface fibers (flush felt flecks - NOT orbiting spike/halo ring)
        for (int i = 0; i < 148; i++) // k247 flecks hold — continuous no spike
        {
            float u = (i + 0.5f) / 110f;
            float yaw = u * Mathf.PI * 2f * 1.7f;
            float pitch = Mathf.Lerp(-0.85f, 0.95f, (i % 11) / 10f);
            float rx = Mathf.Cos(pitch) * Mathf.Sin(yaw);
            float ry = Mathf.Sin(pitch);
            float rz = Mathf.Cos(pitch) * Mathf.Cos(yaw);
            // only keep mostly front-ish flecks for nap read toward cam (local +Z)
            if (rz < -0.12f) continue;
            if (Mathf.Abs(rx) < 0.660f && rz > -0.340f) continue; // k633 fleck mid AO shade (less strip = more mid dark)
            var fib = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fib.name = "Felt Fleck";
            fib.transform.SetParent(root.transform, false);
            float rad = 0.325f;
            fib.transform.localPosition = new Vector3(rx * rad * 0.74f, ry * rad * 1.00f + 0.02f, rz * rad * 0.64f);
            fib.transform.localScale = new Vector3(0.0001f, 0.035f, 0.0001f); // k494 body fleck length-profile LAND
            fib.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(rx, ry, rz));
            Object.DestroyImmediate(fib.GetComponent<Collider>());
            PaintBean(fib);
        }

        return root;
    }

    // k170: stub FeltFiber/albedo nap — continuous felt with body (shared grain, soft edges, less CG sphere)
    // k221: SoftStubNap bump clamp ≤3.0 (FeltFiber Range) — kill spike CG; continuous nap bury held
    static void SoftStubNap(GameObject nap, float bump, Color baseMul)
    {
        var r = nap.GetComponent<Renderer>();
        if (r == null || r.sharedMaterial == null) return;
        var m = new Material(r.sharedMaterial);
        if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", Mathf.Min(bump, 2.95f)); // k221 Range clamp
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseMul);
        if (m.HasProperty("_Sheen")) m.SetFloat("_Sheen", 0.028f); // k468 SoftStubNap sheen 0.028 HELD soft/hard-safe
        if (m.HasProperty("_Wrap")) m.SetFloat("_Wrap", 0.901f); // k468 SoftStubNap wrap 0.901 HELD soft/hard-safe (cliff≥0.902)
        r.sharedMaterial = m;
    }

    static void StubFlecks(Transform parent, int count, float rad)
    {
        // sparse flush flecks on stub surface — shared FeltFiber grain with body flecks
        for (int i = 0; i < count; i++)
        {
            float u = (i + 0.5f) / count;
            float yaw = u * Mathf.PI * 2f * 1.9f;
            float pitch = Mathf.Lerp(-0.95f, 0.95f, (i % 7) / 6f);
            float rx = Mathf.Cos(pitch) * Mathf.Sin(yaw);
            float ry = Mathf.Sin(pitch);
            float rz = Mathf.Cos(pitch) * Mathf.Cos(yaw);
            var fib = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fib.name = "Stub Fleck";
            fib.transform.SetParent(parent, false);
            fib.transform.localPosition = new Vector3(rx * rad, ry * rad * 0.92f, rz * rad);
            fib.transform.localScale = new Vector3(0.0001f, 1.060f, 0.0001f); // k468 stub fleck length-profile×13 xz-floor LAND
            fib.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(rx, ry, rz));
            Object.DestroyImmediate(fib.GetComponent<Collider>());
            PaintBean(fib);
            SoftStubNap(fib, 5.42f, new Color(1.000f, 1.000f, 1.000f)); // k468 fleck SoftStubNap felt-merge bump LAND
        }
    }

    static void Arm(Transform parent, float x)
    {
        // k494 MESH LAND: eyes-in-front ovoid + FeltFiber tiny-nap + SoftNapBump 0.92; Soft Key 2.956 HELD; Capsule KEEP; fleck×13
        bool left = x < 0f;
        var jn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jn.name = "Arm Junction";
        jn.transform.SetParent(parent, false);
        // flush bury — junction swallows short side-nub roots (scales held)
        jn.transform.localPosition = new Vector3(x * 0.136f, left ? -0.016f : -0.033f, left ? 0.052f : -0.032f);
        jn.transform.localScale = new Vector3(0.870f, 0.345f, 0.837f); // k468 junction join-XZ+cap-round bury Y-floor HELD
        jn.transform.localRotation = Quaternion.Euler(left ? 11.5f : -10.5f, 0f, left ? 17.5f : -17.5f);
        Object.DestroyImmediate(jn.GetComponent<Collider>());
        PaintBean(jn);
        SoftStubNap(jn, 5.60f, new Color(1.030f, 1.020f, 1.004f)); // k468 junction SoftStubNap HELD

        // LOOK285 mitt: Capsule felt-nub + SoftCap round — soft-pad =k297 HELD
        var a = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        a.name = "Arm";
        a.transform.SetParent(parent, false);
        a.transform.localPosition = new Vector3(x * (left ? 0.338f : 0.278f), left ? -0.050f : -0.140f, left ? 0.228f : -0.180f); // k312 Z-stride =k308 X=k307
        a.transform.localScale = new Vector3(0.258f, 0.222f, 0.248f); // soft-pad =k297 HELD soft/hard-safe
        // mitt hang: tip along +Y local after rotate — LOOK side-nub mitt profile
        a.transform.localRotation = Quaternion.Euler(left ? 19f : -15f, left ? 8f : -8f, left ? 25.5f : -25.5f); // hang =k308
        Object.DestroyImmediate(a.GetComponent<Collider>());
        PaintBean(a);
        SoftStubNap(a, 5.80f, new Color(1.044f, 1.032f, 1.012f)); // SoftStubNap HELD
        StubFlecks(a.transform, 13, 0.230f); // fleck×13 HELD
        // k482 subtle mitt lobe — soft/hard recover toward 0.42 (k469 path; soft-pad=k297 HELD)
        var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lobe.name = "Arm Mitt Lobe";
        lobe.transform.SetParent(a.transform, false);
        lobe.transform.localPosition = new Vector3(left ? 0.04f : -0.04f, 0.44f, 0.02f);
        lobe.transform.localScale = new Vector3(0.82f, 0.62f, 0.78f); // k488 MittLobe LAND
        Object.DestroyImmediate(lobe.GetComponent<Collider>());
        PaintBean(lobe);
        SoftStubNap(lobe, 4.80f, new Color(1.028f, 1.018f, 1.008f));

        var softCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        softCap.name = "Arm SoftCap";
        softCap.transform.SetParent(a.transform, false);
        softCap.transform.localPosition = new Vector3(0f, 0.60f, 0f);
        softCap.transform.localScale = new Vector3(0.78f, 0.84f, 0.78f); // k488 SoftCap LAND
        Object.DestroyImmediate(softCap.GetComponent<Collider>());
        PaintBean(softCap);
        SoftStubNap(softCap, 4.60f, new Color(1.024f, 1.016f, 1.008f));
        StubSeamStitch(a.transform, left, true);

        var an = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        an.name = "Arm Nap";
        an.transform.SetParent(a.transform, false);
        an.transform.localPosition = new Vector3(left ? 0.02f : -0.02f, 0.024f, 0.006f);
        an.transform.localScale = new Vector3(2.72f, 2.58f, 2.64f); // Arm Nap =k297 HELD soft/hard cliff
        Object.DestroyImmediate(an.GetComponent<Collider>());
        PaintBean(an);
        SoftStubNap(an, 4.55f, new Color(1.016f, 1.010f, 1.006f)); // Arm Nap =k323 LAND
    }

    static void Foot(Transform parent, float x)
    {
        // k494 MESH LAND: eyes-in-front ovoid + FeltFiber tiny-nap + SoftNapBump 0.92; Soft Key 2.956 HELD; Capsule KEEP; fleck×13
        bool left = x < 0f;
        var hj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hj.name = "Leg Junction";
        hj.transform.SetParent(parent, false);
        hj.transform.localPosition = new Vector3(x * 0.121f, left ? -0.025f : -0.108f, left ? 0.122f : -0.066f);
        hj.transform.localScale = new Vector3(0.854f, 0.345f, 0.823f); // k468 junction Y-floor HELD
        hj.transform.localRotation = Quaternion.Euler(left ? 15.5f : -13.5f, 0f, left ? -11.5f : 11.5f);
        Object.DestroyImmediate(hj.GetComponent<Collider>());
        PaintBean(hj);
        SoftStubNap(hj, 5.60f, new Color(1.030f, 1.020f, 1.004f)); // SoftStubNap HELD

        // LOOK285 walk mitt: Capsule felt-nub + SoftCap round — soft-pad walk =k297 HELD
        var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leg.name = "Leg";
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = new Vector3(x * (left ? 0.288f : 0.228f), left ? -0.066f : -0.292f, left ? 0.490f : -0.236f); // Z-stride =k308 X=k307
        leg.transform.localScale = new Vector3(0.238f, 0.222f, 0.268f); // soft-pad walk =k297 HELD
        leg.transform.localRotation = Quaternion.Euler(left ? -22f : 16f, left ? 10f : -9f, left ? -10f : 10f); // =k308
        Object.DestroyImmediate(leg.GetComponent<Collider>());
        PaintBean(leg);
        SoftStubNap(leg, 5.80f, new Color(1.044f, 1.032f, 1.012f)); // SoftStubNap HELD
        StubFlecks(leg.transform, 13, 0.230f); // fleck×13 HELD
        // k482 subtle mitt lobe walk — soft/hard recover
        var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lobe.name = "Leg Mitt Lobe";
        lobe.transform.SetParent(leg.transform, false);
        lobe.transform.localPosition = new Vector3(left ? 0.03f : -0.03f, 0.42f, left ? -0.04f : 0.04f);
        lobe.transform.localScale = new Vector3(0.80f, 0.60f, 0.84f); // k488 MittLobe LAND
        Object.DestroyImmediate(lobe.GetComponent<Collider>());
        PaintBean(lobe);
        SoftStubNap(lobe, 4.80f, new Color(1.028f, 1.018f, 1.008f));

        var softCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        softCap.name = "Leg SoftCap";
        softCap.transform.SetParent(leg.transform, false);
        softCap.transform.localPosition = new Vector3(0f, 0.60f, 0f);
        softCap.transform.localScale = new Vector3(0.78f, 0.84f, 0.78f); // k488 SoftCap LAND
        Object.DestroyImmediate(softCap.GetComponent<Collider>());
        PaintBean(softCap);
        SoftStubNap(softCap, 4.60f, new Color(1.024f, 1.016f, 1.008f));
        StubSeamStitch(leg.transform, left, false);

        var ln = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ln.name = "Leg Nap";
        ln.transform.SetParent(leg.transform, false);
        ln.transform.localPosition = new Vector3(left ? 0.02f : -0.02f, 0.034f, left ? -0.012f : 0.012f);
        ln.transform.localScale = new Vector3(2.72f, 2.58f, 2.64f); // Leg Nap =k297 HELD
        Object.DestroyImmediate(ln.GetComponent<Collider>());
        PaintBean(ln);
        SoftStubNap(ln, 4.55f, new Color(1.016f, 1.010f, 1.006f)); // Leg Nap =k323
        // FOOT ABSORBED — no discrete Foot/FootNap (high-cam CG kill held)
    }

    // LOOK285 stub seam + tan yarn stitch path hints along mitt outer (not baseball V)
    static void StubSeamStitch(Transform stub, bool left, bool isArm)
    {
        var seam = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        seam.name = isArm ? "Arm Stub Seam" : "Leg Stub Seam";
        seam.transform.SetParent(stub, false);
        seam.transform.localPosition = new Vector3(left ? 0.12f : -0.12f, 0.08f, 0.18f);
        seam.transform.localScale = new Vector3(0.045f, 0.38f, 0.045f);
        seam.transform.localRotation = Quaternion.Euler(8f, 0f, left ? 6f : -6f);
        Object.DestroyImmediate(seam.GetComponent<Collider>());
        Paint(seam, new Color(0.22f, 0.12f, 0.07f), 0.95f);
        SoftStubNap(seam, 4.20f, new Color(0.92f, 0.90f, 0.88f));

        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * 0.16f;
            var dash = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dash.name = isArm ? "Arm Stub Stitch" : "Leg Stub Stitch";
            dash.transform.SetParent(stub, false);
            dash.transform.localPosition = new Vector3(left ? 0.14f : -0.14f, 0.02f + t, 0.22f);
            dash.transform.localScale = new Vector3(0.055f, 0.070f, 0.055f);
            dash.transform.localRotation = Quaternion.Euler(12f, left ? 18f : -18f, left ? -22f + i * 4f : 22f - i * 4f);
            Object.DestroyImmediate(dash.GetComponent<Collider>());
            PaintTex(dash, "Assets/Look/tex-yarn-fiber.png", new Color(0.72f, 0.58f, 0.38f), 0.88f);
        }
    }




    // k597+ Face Pad HALF-SHELL — HARDER mid-occlusion cut + proud seam LIPS (LOOK stitched trough); open86/fr kept
    static Mesh MakeFacePadHalfShellMesh(int side, float rx, float ry, float rz, float openMedialDeg, float frontRecess, int segU, int segV, float medialFloorCut, float lipProud)
    {
        // side -1 = left (open rim toward midline), +1 = right
        // frontRecess 0..1: pull +Z (cam-front) verts back, stronger near medial rim
        // medialFloorCut 0..1: HARDER — cut/open medial pad floor toward mid AIR so dark floor shades between lips
        // lipProud 0..1: push rim lips proud toward cam + midline (seam lips catch light; cavity shades between)
        segU = Mathf.Max(8, segU);
        segV = Mathf.Max(6, segV);
        float open = Mathf.Clamp(openMedialDeg, 8f, 110f) * Mathf.Deg2Rad;
        float fr = Mathf.Clamp01(frontRecess);
        float fc = Mathf.Clamp01(medialFloorCut);
        float lp = Mathf.Clamp01(lipProud);
        var verts = new System.Collections.Generic.List<Vector3>();
        var norms = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        int cols = segU + 1;
        for (int iv = 0; iv <= segV; iv++)
        {
            float v = iv / (float)segV;
            float theta = v * Mathf.PI;
            float st = Mathf.Sin(theta);
            float ct = Mathf.Cos(theta);
            float midBand = 1f - Mathf.Abs(2f * v - 1f);
            midBand = Mathf.Clamp01(midBand * 1.15f);
            for (int iu = 0; iu <= segU; iu++)
            {
                float u = iu / (float)segU;
                float phi = open + u * (Mathf.PI * 2f - 2f * open);
                float cosP = Mathf.Cos(phi);
                float sinP = Mathf.Sin(phi);
                float x = side * rx * cosP * st;
                float y = ry * ct;
                float z = rz * sinP * st;
                float medialW = 1f - 2f * Mathf.Min(u, 1f - u);
                if (fr > 0f && z > 0f)
                {
                    float frontW = Mathf.Clamp01(z / Mathf.Max(1e-4f, rz));
                    float w = fr * frontW * (0.30f + 0.70f * medialW) * Mathf.Clamp01(st + 0.15f);
                    z *= (1f - 0.92f * w);
                    float shrink = 1f - 0.38f * w;
                    x *= shrink;
                    y *= (1f - 0.12f * w);
                    z *= shrink;
                }
                if (fc > 0f && z > -0.04f)
                {
                    float frontW2 = Mathf.Clamp01((z + 0.04f) / Mathf.Max(1e-4f, rz + 0.04f));
                    float cut = fc * medialW * (0.20f + 0.80f * frontW2) * Mathf.Clamp01(st + 0.08f);
                    cut *= (0.70f + 0.55f * midBand);
                    cut = Mathf.Clamp01(cut);
                    z *= (1f - 0.96f * cut);
                    x *= (1f - 0.82f * cut);
                    y *= (1f - 0.14f * cut);
                }
                verts.Add(new Vector3(x, y, z));
                Vector3 n = new Vector3(side * cosP * st / Mathf.Max(1e-4f, rx),
                                        ct / Mathf.Max(1e-4f, ry),
                                        sinP * st / Mathf.Max(1e-4f, rz));
                if (n.sqrMagnitude > 1e-8f) n.Normalize();
                else n = Vector3.forward;
                if ((fr > 0f || fc > 0f) && z > -0.04f)
                {
                    float tip = Mathf.Max(fr, fc) * medialW * (0.75f + 0.40f * midBand);
                    n = (n + new Vector3(-side * 0.48f * tip, 0f, -0.70f * tip)).normalized;
                }
                norms.Add(n);
                uvs.Add(new Vector2(u, v));
            }
        }
        for (int iv = 0; iv < segV; iv++)
        {
            for (int iu = 0; iu < segU; iu++)
            {
                int i0 = iv * cols + iu;
                int i1 = i0 + 1;
                int i2 = i0 + cols;
                int i3 = i2 + 1;
                if (side < 0)
                {
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
                else
                {
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }
        }
        int rimBase = verts.Count;
        float lip = 0.08f + 0.06f * fr + 0.14f * lp;
        for (int iv = 0; iv <= segV; iv++)
        {
            float v = iv / (float)segV;
            float theta = v * Mathf.PI;
            float st = Mathf.Sin(theta);
            float ct = Mathf.Cos(theta);
            float midBand = 1f - Mathf.Abs(2f * v - 1f);
            midBand = Mathf.Clamp01(midBand * 1.15f);
            for (int edge = 0; edge < 2; edge++)
            {
                float phi = edge == 0 ? open : (Mathf.PI * 2f - open);
                float cosP = Mathf.Cos(phi);
                float sinP = Mathf.Sin(phi);
                float x0 = side * rx * cosP * st;
                float y0 = ry * ct;
                float z0 = rz * sinP * st;
                if (fr > 0f && z0 > 0f)
                {
                    float frontW = Mathf.Clamp01(z0 / Mathf.Max(1e-4f, rz));
                    float w = fr * frontW * 0.95f * Mathf.Clamp01(st + 0.15f);
                    z0 *= (1f - 0.92f * w);
                    float shrink = 1f - 0.38f * w;
                    x0 *= shrink; y0 *= (1f - 0.12f * w); z0 *= shrink;
                }
                if (lp > 0f)
                {
                    float puff = lp * Mathf.Clamp01(st + 0.05f) * (0.85f + 0.25f * midBand);
                    z0 += 0.095f * puff * rz;
                    x0 += -side * 0.062f * puff * rx;
                }
                Vector3 outer = new Vector3(x0, y0, z0);
                Vector3 inner = outer * (1f - lip);
                if (lp > 0f)
                {
                    inner = Vector3.Lerp(inner, new Vector3(x0 * (1f - lip * 1.20f), y0 * (1f - lip * 0.6f), z0 * (1f - lip * 1.30f)), lp);
                }
                verts.Add(outer);
                verts.Add(inner);
                Vector3 rn = (outer - inner);
                if (rn.sqrMagnitude > 1e-8f) rn.Normalize();
                else rn = side < 0 ? Vector3.right : Vector3.left;
                norms.Add(rn); norms.Add(rn);
                uvs.Add(new Vector2(edge, v)); uvs.Add(new Vector2(edge + 0.5f, v));
            }
        }
        int rimStride = 4;
        for (int iv = 0; iv < segV; iv++)
        {
            for (int edge = 0; edge < 2; edge++)
            {
                int o0 = rimBase + iv * rimStride + edge * 2;
                int i0 = o0 + 1;
                int o1 = rimBase + (iv + 1) * rimStride + edge * 2;
                int i1 = o1 + 1;
                if (side < 0)
                {
                    tris.Add(o0); tris.Add(o1); tris.Add(i0);
                    tris.Add(i0); tris.Add(o1); tris.Add(i1);
                }
                else
                {
                    tris.Add(o0); tris.Add(i0); tris.Add(o1);
                    tris.Add(i0); tris.Add(i1); tris.Add(o1);
                }
            }
        }
        var mesh = new Mesh();
        mesh.name = side < 0 ? "FacePadHalfShellL" : "FacePadHalfShellR";
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }


    // k627+ SOLID-FILL FeltFiber/SeamFelt dark seam — closed volume occluder (NOT Pad Channel / NOT Valley Shade / NOT Overlay sticker)
    static Mesh MakeBeanTrenchMesh(float halfW, float halfH, float depth, float wall, int segX, int segY)
    {
        float front = Mathf.Max(0.012f, wall * 0.45f);
        var verts = new System.Collections.Generic.List<Vector3>();
        var norms = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfW, -halfH, -depth),
            new Vector3( halfW, -halfH, -depth),
            new Vector3( halfW,  halfH, -depth),
            new Vector3(-halfW,  halfH, -depth),
            new Vector3(-halfW, -halfH,  front),
            new Vector3( halfW, -halfH,  front),
            new Vector3( halfW,  halfH,  front),
            new Vector3(-halfW,  halfH,  front),
        };
        Vector3[] faceN = new Vector3[]
        {
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
        };
        int[] faceV = new int[]
        {
            0,1,2,3,
            4,5,6,7,
            0,3,7,4,
            1,5,6,2,
            0,4,5,1,
            3,2,6,7,
        };
        for (int f = 0; f < 6; f++)
        {
            int b = verts.Count;
            for (int i = 0; i < 4; i++)
            {
                int ci = faceV[f * 4 + i];
                verts.Add(corners[ci]);
                norms.Add(faceN[f * 4 + i]);
                uvs.Add(new Vector2(i % 2, i / 2));
            }
            if (f == 1)
            {
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }
            else
            {
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
        }
        var mesh = new Mesh();
        mesh.name = "BeanTrenchSolidFill";
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // k495: place LOOK 285 bead eyes + U-smile on the CAMERA-FACING skin.
    // cam-k494 local +Z Unlit/Color was a no-draw in ForwardBase capture.
    // k522+: HOLLOW face plane — beads on Face Pad L/R (lobes); crease/dashes in Valley Recess (no front disc).
    static void SnapFaceToCamera(Transform bean, Camera cam)
    {
        if (bean == null || cam == null) return;
        Vector3 toCam = (cam.transform.position - bean.position);
        if (toCam.sqrMagnitude < 1e-6f) toCam = -bean.forward;
        Vector3 n = bean.InverseTransformDirection(toCam.normalized);
        n.Normalize();
        Vector3 upGuess = Vector3.up;
        Vector3 right = Vector3.Cross(upGuess, n);
        if (right.sqrMagnitude < 1e-4f) right = Vector3.right;
        right.Normalize();
        Vector3 localUp = Vector3.Cross(n, right).normalized;

        float eyeY = 0.064f; // k582 on dual-lobe peaks
        float eyePush = 0.078f; // k633 on lobe peaks
        float eyeScale = 0.082f; // k506 LOOK bead punch held
        float hlScale = 0.018f;
        float smileRec = 0.013f;
        float creaseRec = 0.008f; // k582 crease PARKED — SnapFace no-op if tiny
        float dashRec = 0.020f; // k633 yarn nest IN dark floor recess

        var padL = bean.Find("Face Pad L");
        var padR = bean.Find("Face Pad R");
        var lobeL = bean.Find("Bean Lobe L");
        var lobeR = bean.Find("Bean Lobe R");

        for (int side = -1; side <= 1; side += 2)
        {
            Transform anc = side < 0
                ? (padL != null ? padL : lobeL)
                : (padR != null ? padR : lobeR);
            Vector3 basePos = anc != null
                ? anc.localPosition
                : (right * (side * 0.160f));
            // attach bead to lobe/pad surface toward cam — hollow midline (no shared skin sphere)
            Vector3 eyePos = basePos + n * eyePush + localUp * eyeY;
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            e.name = "Eye";
            e.transform.SetParent(bean, false);
            e.transform.localPosition = eyePos;
            e.transform.localScale = Vector3.one * eyeScale;
            Object.DestroyImmediate(e.GetComponent<Collider>());
            PaintBead(e, new Color(0.02f, 0.015f, 0.012f));

            var hl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hl.name = "Eye Highlight";
            hl.transform.SetParent(bean, false);
            hl.transform.localPosition = eyePos + n * 0.022f + localUp * 0.014f + right * (-0.012f);
            hl.transform.localScale = Vector3.one * hlScale;
            Object.DestroyImmediate(hl.GetComponent<Collider>());
            PaintBead(hl, new Color(0.98f, 0.97f, 0.94f));
        }

        // k504: NO Eye Socket spheres (were front-potato fill); BeadUnlit punch + deep skin = felted-in

        // k581+ STRIP mid front fill — smile arcs on L/R cheek shells only (skip Cos≈0 bridge over trench)
        float mouthY = eyeY - 0.128f;
        int ns = 13;
        float smileHalf = 0.043f;
        float smileMidSkip = 0.255f; // k633 dark cleft keep
        float smilePadOff = 0.125f; // k585 fuse arcs on dual-lobe shells
        for (int i = 0; i < ns; i++)
        {
            float t = (i / (float)(ns - 1)) * Mathf.PI;
            float cx = Mathf.Cos(t);
            if (Mathf.Abs(cx) < smileMidSkip) continue; // air mid — no smile spheres flooring trench
            float side = cx < 0f ? -1f : 1f;
            Vector3 p = n * smileRec
                + right * (cx * smileHalf + side * smilePadOff)
                + localUp * (mouthY - Mathf.Sin(t) * 0.028f); // k581 smile on shells
            var smile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            smile.name = "Smile";
            smile.transform.SetParent(bean, false);
            smile.transform.localPosition = p;
            smile.transform.localScale = Vector3.one * 0.016f;
            Object.DestroyImmediate(smile.GetComponent<Collider>());
            PaintBead(smile, new Color(0.04f, 0.025f, 0.02f));
        }

        // crease + dashes live IN the valley recess (shade as trough, not drawn seam on disc)
        var crease = bean.Find("Crease");
        if (crease != null)
        {
            // k582: crease PARKED — do not re-inflate mid fill; empty AIR cleft + dashes = LOOK seam
            if (crease.localScale.y < 0.02f)
            {
                // leave parked tiny rear
            }
            else
            {
                crease.localPosition = n * creaseRec;
                crease.localRotation = Quaternion.FromToRotation(Vector3.up, localUp);
                crease.localScale = new Vector3(0.005f, 0.150f, 0.005f);
                Paint(crease.gameObject, new Color(0.034f, 0.015f, 0.007f), 0.99f);
            }
        }
        var valley = bean.Find("Valley Recess");
        if (valley != null)
        {
            valley.localRotation = Quaternion.FromToRotation(Vector3.up, localUp);
            // keep mesh-local recess; nudge toward cam normal for shade lock
            Vector3 vp = valley.localPosition;
            valley.localPosition = new Vector3(0f, vp.y, vp.z);
        }
        var dashes = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in bean)
            if (t.name == "Dash Stitch") dashes.Add(t);
        var slots = new System.Collections.Generic.List<float>();
        for (int k = 0; k < 24; k++)
        {
            float y = -0.38f + k * 0.048f; // full-height LOOK crease dashes
            if (y > 0.40f) break;
            if (Mathf.Abs(y - mouthY) < 0.040f) continue; // don't zipper the U-smile
            slots.Add(y);
        }
        for (int di = 0; di < dashes.Count; di++)
        {
            var t = dashes[di];
            if (di >= slots.Count)
            {
                t.localScale = Vector3.one * 0.0001f;
                continue;
            }
            float y = slots[di];
            t.localPosition = n * dashRec + localUp * y + right * (((di % 3) - 1) * 0.002f); // k633 yarn IN dark recess
            t.localRotation = Quaternion.FromToRotation(Vector3.up, localUp) * Quaternion.Euler(8f, 0f, -18f + di * 3f);
            t.localScale = new Vector3(0.006f, 0.010f, 0.006f); // k633 LOOK yarn in recess
            PaintBead(t.gameObject, new Color(0.280f, 0.160f, 0.060f)); // k633 warm yarn
        }

        // k587+: seat Bean Trench seam cavity on cam normal (narrow U stays mid)
        var trench = bean.Find("Bean Trench");
        if (trench != null)
        {
            trench.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
            Vector3 tp = trench.localPosition;
            float seamZ = Mathf.Abs(tp.z) > 0.02f ? Mathf.Abs(tp.z) : 0.050f;
            trench.localPosition = n * seamZ;
        }

        // k633+: seat Seam Plug/Walls/Floor on cam normal (SeamFelt depth-tested mid-band)
        var seamPlug = bean.Find("Seam Plug");
        if (seamPlug != null)
        {
            seamPlug.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
            Vector3 sp = seamPlug.localPosition;
            float plugZ = Mathf.Abs(sp.z) > 0.02f ? Mathf.Abs(sp.z) : 0.090f;
            seamPlug.localPosition = n * plugZ + localUp * 0.010f;
        }
        foreach (string wn in new string[] { "Seam Wall L", "Seam Wall R" })
        {
            var sw = bean.Find(wn);
            if (sw == null) continue;
            float side = wn.EndsWith("L") ? -1f : 1f;
            Vector3 swp = sw.localPosition;
            float wallZ = Mathf.Abs(swp.z) > 0.02f ? Mathf.Abs(swp.z) : 0.110f;
            float wallX = Mathf.Abs(swp.x) > 0.02f ? Mathf.Abs(swp.x) : 0.120f;
            sw.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
            sw.localPosition = n * wallZ + right * (side * wallX) + localUp * 0.010f;
        }
        var seamPlugDeep = bean.Find("Seam Plug Deep");
        if (seamPlugDeep != null)
        {
            seamPlugDeep.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
            Vector3 spd = seamPlugDeep.localPosition;
            float deepZ = Mathf.Abs(spd.z) > 0.02f ? Mathf.Abs(spd.z) : 0.080f;
            seamPlugDeep.localPosition = n * deepZ + localUp * 0.010f;
        }
        var seamFloor = bean.Find("Seam Floor");
        if (seamFloor != null)
        {
            seamFloor.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
            Vector3 sf = seamFloor.localPosition;
            float floorZ = Mathf.Abs(sf.z) > 0.02f ? Mathf.Abs(sf.z) : 0.080f;
            seamFloor.localPosition = n * floorZ + localUp * 0.010f;
        }

    }

    static void Pine(Vector3 pos, float scale)
    {
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Pine Trunk";
        Vector3 trunkPos = pos + Vector3.down * 0.42f * scale;
        trunk.transform.position = trunkPos;
        trunk.transform.localScale = new Vector3(0.15f, 0.42f, 0.15f) * scale;
        PaintTex(trunk, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.90f);
        Object.DestroyImmediate(trunk.GetComponent<Collider>());
        YarnWrapTrunk(trunkPos, 0.10f * scale, 0.38f * scale, 20, Yarn);
        // layered amigurumi crown (LOOK set density)
        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Pine Crown";
        crown.transform.position = pos + Vector3.up * 0.22f * scale;
        crown.transform.localScale = new Vector3(0.78f, 1.22f, 0.78f) * scale;
        PaintTex(crown, "Assets/Look/tex-yarn-green.png", Color.white, 0.93f);
        Object.DestroyImmediate(crown.GetComponent<Collider>());
        for (int layer = 0; layer < 3; layer++)
        {
            var fuzz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fuzz.name = "Pine Fuzz";
            float ly = 0.05f + layer * 0.22f;
            float sc = 0.55f - layer * 0.08f;
            fuzz.transform.position = pos + Vector3.up * ly * scale;
            fuzz.transform.localScale = new Vector3(sc, sc * 0.7f, sc) * scale * 1.15f;
            PaintTex(fuzz, "Assets/Look/tex-yarn-green.png", new Color(0.85f, 1f, 0.85f), 0.94f);
            Object.DestroyImmediate(fuzz.GetComponent<Collider>());
        }
        // denser layered felt flap rings (LOOK 285 evergreen) - k24
        for (int ring = 0; ring < 5; ring++)
        {
            int flaps = 7 + ring;
            float ry = 0.58f - ring * 0.15f;
            float rad = 0.20f + ring * 0.09f;
            for (int f = 0; f < flaps; f++)
            {
                float a = f * (Mathf.PI * 2f / flaps) + ring * 0.2f;
                var flap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flap.name = "Pine Flap";
                flap.transform.position = pos + new Vector3(Mathf.Cos(a) * rad, ry, Mathf.Sin(a) * rad) * scale;
                flap.transform.localScale = new Vector3(0.18f, 0.09f, 0.26f) * scale;
                flap.transform.localRotation = Quaternion.Euler(25f, a * Mathf.Rad2Deg, 0f);
                PaintTex(flap, "Assets/Look/tex-yarn-green.png", new Color(0.9f, 1f, 0.9f), 0.93f);
                Object.DestroyImmediate(flap.GetComponent<Collider>());
            }
        }
        for (int i = 0; i < 10; i++)
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float a = i * 0.72f;
            float rr = 0.28f + (i % 3) * 0.05f;
            dot.transform.position = crown.transform.position + new Vector3(Mathf.Cos(a) * rr, (i % 4) * 0.10f - 0.1f, Mathf.Sin(a) * rr) * scale;
            dot.transform.localScale = Vector3.one * (0.07f + (i % 2) * 0.03f) * scale;
            Color dcol = (i % 3 == 0) ? FeltRed : ((i % 3 == 1) ? new Color(0.75f, 0.7f, 0.3f) : new Color(0.95f, 0.55f, 0.2f));
            PaintTex(dot, (i % 3 == 0) ? "Assets/Look/tex-felt-brown.png" : "Assets/Look/tex-yarn-flower.png", dcol, 0.9f); // k63
            Object.DestroyImmediate(dot.GetComponent<Collider>());
        }
        for (int i = 0; i < 4; i++)
        {
            float a = i * 1.7f;
            var cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cone.name = "Pinecone";
            cone.transform.position = crown.transform.position + new Vector3(Mathf.Cos(a) * 0.22f, -0.25f, Mathf.Sin(a) * 0.22f) * scale;
            cone.transform.localScale = Vector3.one * 0.08f * scale;
            PaintTex(cone, "Assets/Look/tex-felt-brown.png", FeltDark, 0.85f); // k63
            Object.DestroyImmediate(cone.GetComponent<Collider>());
        }
        // amigurumi crochet loop density on crown - k46 tinier denser microstructure
        for (int i = 0; i < 72; i++)
        {
            float a = i * 0.31f;
            float rr = 0.14f + (i % 8) * 0.048f;
            float yy = -0.22f + (i % 10) * 0.062f;
            var loop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            loop.name = "Pine Yarn Loop";
            loop.transform.position = crown.transform.position + new Vector3(Mathf.Cos(a) * rr, yy, Mathf.Sin(a) * rr) * scale;
            loop.transform.localScale = Vector3.one * (0.020f + (i % 5) * 0.006f) * scale;
            PaintTex(loop, "Assets/Look/tex-yarn-green.png", new Color(0.86f, 1f, 0.86f), 0.92f);
            Object.DestroyImmediate(loop.GetComponent<Collider>());
        }
        // micro crochet bump spheres for close-up yarn-loop read - k46 denser
        for (int i = 0; i < 60; i++)
        {
            float a = i * 0.41f + 0.15f;
            float rr = 0.18f + (i % 7) * 0.035f;
            float yy = -0.12f + (i % 8) * 0.065f;
            var bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bump.name = "Pine Crochet Bump";
            bump.transform.position = crown.transform.position + new Vector3(Mathf.Cos(a) * rr, yy, Mathf.Sin(a) * rr) * scale;
            bump.transform.localScale = Vector3.one * (0.012f + (i % 4) * 0.004f) * scale;
            PaintTex(bump, "Assets/Look/tex-yarn-green.png", new Color(0.72f, 0.94f, 0.70f), 0.94f);
            Object.DestroyImmediate(bump.GetComponent<Collider>());
        }
        // tiny pine needle-tip puffs (amigurumi silhouette)
        for (int i = 0; i < 28; i++)
        {
            float a = i * 0.55f;
            float rr = 0.30f + (i % 4) * 0.03f;
            float yy = -0.08f + (i % 5) * 0.10f;
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Pine Needle Tip";
            tip.transform.position = crown.transform.position + new Vector3(Mathf.Cos(a) * rr, yy, Mathf.Sin(a) * rr) * scale;
            tip.transform.localScale = Vector3.one * (0.010f + (i % 3) * 0.003f) * scale;
            PaintTex(tip, "Assets/Look/tex-yarn-green.png", new Color(0.55f, 0.78f, 0.48f), 0.95f);
            Object.DestroyImmediate(tip.GetComponent<Collider>());
        }
    }

    static void YarnWrapTrunk(Vector3 center, float radius, float halfH, int rings, Color col)
    {
        for (int i = 0; i < rings; i++)
        {
            float t = i / (float)(rings - 1);
            float y = center.y - halfH + t * halfH * 2f;
            float a0 = t * Mathf.PI * 6f;
            for (int k = 0; k < 5; k++)
            {
                float a = a0 + k * (Mathf.PI * 2f / 5f);
                var loop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                loop.name = "Yarn Loop";
                loop.transform.position = new Vector3(center.x + Mathf.Cos(a) * radius * 1.15f, y, center.z + Mathf.Sin(a) * radius * 1.15f);
                loop.transform.localScale = Vector3.one * (radius * 0.55f);
                PaintTex(loop, "Assets/Look/tex-yarn-fiber.png", col, 0.88f);
                Object.DestroyImmediate(loop.GetComponent<Collider>());
            }
        }
    }

    static void RedTree(Vector3 pos)
    {
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Red Trunk";
        Vector3 trunkPos = pos + Vector3.down * 0.48f;
        trunk.transform.position = trunkPos;
        trunk.transform.localScale = new Vector3(0.22f, 0.58f, 0.22f);
        PaintTex(trunk, "Assets/Look/tex-yarn-fiber.png", Yarn, 0.90f);
        Object.DestroyImmediate(trunk.GetComponent<Collider>());
        YarnWrapTrunk(trunkPos, 0.14f, 0.52f, 22, Yarn);
        // oversized knitted flower canopy (LOOK set: red/orange/yellow blooms)
        Color[] blooms = {
            FeltRed,
            new Color(0.92f, 0.45f, 0.18f),
            new Color(0.95f, 0.72f, 0.22f),
            new Color(0.85f, 0.22f, 0.18f),
            new Color(0.98f, 0.55f, 0.15f),
            new Color(0.78f, 0.18f, 0.35f)
        };
        for (int i = 0; i < 28; i++)
        {
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float a = i * 0.65f;
            float r = 0.22f + (i % 5) * 0.07f;
            leaf.transform.position = pos + new Vector3(Mathf.Cos(a) * r, 0.12f + (i % 6) * 0.12f, Mathf.Sin(a) * r);
            leaf.transform.localScale = Vector3.one * (0.18f + (i % 4) * 0.05f);
            PaintTex(leaf, "Assets/Look/tex-yarn-flower.png", blooms[i % blooms.Length], 0.92f);
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
            var center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.name = "Bloom Center";
            center.transform.position = leaf.transform.position + new Vector3(0f, 0.02f, 0.02f);
            center.transform.localScale = Vector3.one * 0.05f;
            PaintTex(center, "Assets/Look/tex-yarn-flower.png", Stitch, 0.5f); // k63
            Object.DestroyImmediate(center.GetComponent<Collider>());
            // crochet petal + yarn-loop ring on every bloom (amigurumi density)
            for (int p = 0; p < 6; p++)
            {
                float pa = p * (Mathf.PI * 2f / 6f) + i * 0.1f;
                var petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                petal.name = "Flower Petal";
                petal.transform.position = leaf.transform.position + new Vector3(Mathf.Cos(pa), 0.02f, Mathf.Sin(pa)) * 0.085f;
                petal.transform.localScale = Vector3.one * (0.065f + (p % 2) * 0.015f);
                PaintTex(petal, "Assets/Look/tex-yarn-flower.png", blooms[(i + p) % blooms.Length], 0.9f);
                Object.DestroyImmediate(petal.GetComponent<Collider>());
            }
            // k46: denser yarn-loop / crochet bumps on every bloom (amigurumi microstructure)
            for (int p = 0; p < 8; p++)
            {
                float pa = p * (Mathf.PI * 2f / 8f) + i * 0.07f;
                var yarnLoop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                yarnLoop.name = "Bloom Yarn Loop";
                yarnLoop.transform.position = leaf.transform.position + new Vector3(Mathf.Cos(pa), 0.040f + (p % 2) * 0.012f, Mathf.Sin(pa)) * (0.048f + (p % 3) * 0.008f);
                yarnLoop.transform.localScale = Vector3.one * (0.016f + (p % 3) * 0.005f);
                PaintTex(yarnLoop, "Assets/Look/tex-yarn-flower.png", blooms[(i + 1) % blooms.Length], 0.88f);
                Object.DestroyImmediate(yarnLoop.GetComponent<Collider>());
            }
            for (int p = 0; p < 7; p++)
            {
                float pa = p * 0.90f + 0.25f;
                var bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bump.name = "Bloom Crochet Bump";
                bump.transform.position = leaf.transform.position + new Vector3(Mathf.Cos(pa), 0.055f, Mathf.Sin(pa)) * 0.032f;
                bump.transform.localScale = Vector3.one * (0.011f + (p % 2) * 0.003f);
                PaintTex(bump, "Assets/Look/tex-yarn-flower.png", blooms[(i + 2) % blooms.Length], 0.9f);
                Object.DestroyImmediate(bump.GetComponent<Collider>());
            }
        }
    }

    static void RoastBean(Vector3 pos, float scale, float yaw)
    {
        var root = new GameObject("Roast Prop");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(12f, yaw, 25f);
        var l = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        l.name = "Roast Lobe";
        l.transform.SetParent(root.transform, false);
        l.transform.localPosition = new Vector3(-0.35f * scale, 0f, 0f);
        l.transform.localScale = new Vector3(scale * 1.1f, scale * 0.85f, scale * 0.75f);
        PaintTex(l, "Assets/Look/tex-felt-brown.png", FeltDark, 0.55f); // k63 Roast Lobe
        Object.DestroyImmediate(l.GetComponent<Collider>());
        var r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        r.name = "Roast Lobe";
        r.transform.SetParent(root.transform, false);
        r.transform.localPosition = new Vector3(0.35f * scale, 0f, 0f);
        r.transform.localScale = new Vector3(scale * 1.1f, scale * 0.85f, scale * 0.75f);
        PaintTex(r, "Assets/Look/tex-felt-brown.png", FeltDark, 0.55f); // k63 Roast Lobe
        Object.DestroyImmediate(r.GetComponent<Collider>());
        var crease = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        crease.name = "Roast Crease";
        crease.transform.SetParent(root.transform, false);
        crease.transform.localPosition = Vector3.zero;
        crease.transform.localScale = new Vector3(scale * 0.22f, scale * 0.55f, scale * 0.22f);
        crease.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        PaintTex(crease, "Assets/Look/tex-felt-brown.png", new Color(0.18f, 0.10f, 0.06f), 0.7f); // k63 Roast Crease
        Object.DestroyImmediate(crease.GetComponent<Collider>());
    }

    static void MossBall(Vector3 pos, float scale)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "Moss";
        s.transform.position = pos;
        s.transform.localScale = Vector3.one * scale;
        PaintTex(s, "Assets/Look/tex-yarn-green.png", FeltPine, 0.94f);
        Object.DestroyImmediate(s.GetComponent<Collider>());
        // soft felt halo puffs
        for (int i = 0; i < 8; i++)
        {
            float a = i * 0.85f;
            var h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            h.name = "Moss Halo";
            h.transform.position = pos + new Vector3(Mathf.Cos(a), 0.35f + (i % 3) * 0.15f, Mathf.Sin(a)) * (scale * 0.45f);
            h.transform.localScale = Vector3.one * (scale * (0.28f + (i % 3) * 0.06f));
            PaintTex(h, "Assets/Look/tex-yarn-green.png", new Color(0.45f, 0.58f, 0.38f), 0.96f);
            Object.DestroyImmediate(h.GetComponent<Collider>());
        }
    }

    static void Sphere(string name, Vector3 pos, float scale, Color color)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = name;
        s.transform.position = pos;
        s.transform.localScale = Vector3.one * scale;
        // k63: no flat Paint for interior props - LOOK kit felt/yarn
        string tex = (color.g > color.r + 0.05f) ? "Assets/Look/tex-yarn-green.png" : "Assets/Look/tex-felt-brown.png";
        PaintTex(s, tex, color, 0.9f);
        Object.DestroyImmediate(s.GetComponent<Collider>());
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




    // k627+: SeamFelt/FeltFiber dark seam floor (ForwardBase + depth-tested CullOff ZWrite Offset) — NOT BeadUnlit Overlay Always sticker
    static void PaintSeamFelt(GameObject go, Color baseMul, float bump, float sheen, float wrap)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Look/imagine-bean-albedo.png");
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Escape/SeamFelt");
        if (shader == null) shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) shader = Shader.Find("Diffuse");
        var mat = new Material(shader);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseMul);
        if (mat.HasProperty("_Color")) mat.color = baseMul;
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", Mathf.Clamp(bump, 0.05f, 2.95f));
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", sheen);
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", wrap);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.02f);
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        r.receiveShadows = true;
    }

    static void PaintBean(GameObject go)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Look/imagine-bean-albedo.png");
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1.226f, 1.176f, 1.040f)); // k333 PaintBean body albedo LAND
        if (mat.HasProperty("_Color")) mat.color = new Color(0.990f, 0.994f, 0.998f);
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.90f); // k494 PaintBean bump eyes-first LAND
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", 0.213f); // k468 PaintBean sheen LAND
        if (mat.HasProperty("_Wrap")) mat.SetFloat("_Wrap", 0.760f); // k328 PaintBean wrap LAND =k317
        if (mat.HasProperty("_MidStripAmt")) mat.SetFloat("_MidStripAmt", 0.520f); // k664 mid-strip HELD
        if (mat.HasProperty("_MidStripWidth")) mat.SetFloat("_MidStripWidth", 0.055f); // k664 mid-strip HELD
        if (mat.HasProperty("_MidStripYPow")) mat.SetFloat("_MidStripYPow", 1.850f); // k664 mid-strip HELD
        if (mat.HasProperty("_MidStripYBand")) mat.SetFloat("_MidStripYBand", 0.180f); // k664 mid-strip HELD
        if (mat.HasProperty("_CleftNestAmt")) mat.SetFloat("_CleftNestAmt", 0.480f); // k674 CleftNestDark
        if (mat.HasProperty("_CleftNestWidth")) mat.SetFloat("_CleftNestWidth", 0.038f); // k674 cleft w
        if (mat.HasProperty("_WallSep")) mat.SetFloat("_WallSep", 0.102f); // k674 wall sep
        if (mat.HasProperty("_WallWidth")) mat.SetFloat("_WallWidth", 0.046f); // k674 wall w
        if (mat.HasProperty("_YarnNestAmt")) mat.SetFloat("_YarnNestAmt", 0.550f); // k674 yarn nest
        if (mat.HasProperty("_YarnNestFreq")) mat.SetFloat("_YarnNestFreq", 14.000f); // k674 yarn freq
        if (mat.HasProperty("_YarnNestWidth")) mat.SetFloat("_YarnNestWidth", 0.016f); // k674 yarn width
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.06f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.06f);
        r.sharedMaterial = mat;
    }


    static void PaintBead(GameObject go, Color color)
    {
        // k495: Escape/BeadUnlit is ForwardBase+Overlay+ZTest Always so cam.Render punches through FeltFiber.
        // Unlit/Color was a silent no-draw on cam-k494. FeltFiber fallback still has ForwardBase.
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Escape/BeadUnlit");
        if (shader == null)
        {
            AssetDatabase.ImportAsset("Assets/Look/BeadUnlit.shader");
            shader = Shader.Find("Escape/BeadUnlit");
        }
        if (shader == null) shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) shader = Shader.Find("Diffuse");
        var mat = new Material(shader);
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static void Paint(GameObject go, Color color, float roughness)
    {
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - roughness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - roughness);
        r.sharedMaterial = mat;
    }

    static void PaintTex(GameObject go, string texPath, Color tint, float roughness)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Escape/FeltFiber");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BumpMap"))
            {
                string nPath = texPath.Contains("yarn") || texPath.Contains("green")
                    ? "Assets/Look/tex-crochet-normal.png"
                    : "Assets/Look/tex-felt-normal.png";
                var n = AssetDatabase.LoadAssetAtPath<Texture2D>(nPath);
                if (n == null) n = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Look/tex-felt-normal.png");
                if (n != null) { mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); }
            }
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.color = tint;
        float bump = 1.55f;
        if (texPath.Contains("burlap")) bump = 2.95f; // k54 coarser burlap
        else if (texPath.Contains("kraft")) bump = 2.50f; // k47 kraft flute read
        else if (texPath.Contains("yarn") || texPath.Contains("green") || texPath.Contains("flower")) bump = 1.95f;
        if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", bump);
        if (mat.HasProperty("_Sheen")) mat.SetFloat("_Sheen", texPath.Contains("yarn") || texPath.Contains("flower") ? 0.18f : 0.12f); // k46 crochet sheen
        if (mat.HasProperty("_BaseMap"))
        {
            float tile = texPath.Contains("burlap") ? 9f : (texPath.Contains("kraft") ? 7f : (texPath.Contains("yarn") || texPath.Contains("green") || texPath.Contains("flower") ? 2.8f : 1f)); // k54 coarser burlap
            mat.SetTextureScale("_BaseMap", new Vector2(tile, tile));
        }
        if (mat.HasProperty("_MainTex"))
        {
            float tile = texPath.Contains("burlap") ? 9f : (texPath.Contains("kraft") ? 7f : (texPath.Contains("yarn") || texPath.Contains("green") || texPath.Contains("flower") ? 2.8f : 1f)); // k54 coarser burlap
            mat.SetTextureScale("_MainTex", new Vector2(tile, tile));
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - roughness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - roughness);
        r.sharedMaterial = mat;
    }
}

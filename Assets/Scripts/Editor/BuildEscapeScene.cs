using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// LOOK-driven scene builder — Imagine handmade felt diorama (DriveLook/imagine/285.jpg, frames/set-*.jpg, library/rotate-*.mp4)
/// staged as a wide side-scroller (LittleBigPlanet framing): run +X, lane z = 0, kraft wall behind the set, camera in front.
/// Physics-critical settings are held: Imagine Bean capsule (r 0.38 / h 0.86 / dir 1), Rigidbody
/// (0.9 / 0.18 / 0.9), BeanImpulse, Bean Soft Key (2.956), gravity -22, fixedDeltaTime 0.02.
public static class BuildEscapeScene
{
    static readonly Color FeltBrown = new Color(0.76f, 0.58f, 0.44f);
    static readonly Color Thread = new Color(0.88f, 0.80f, 0.64f);
    static readonly Color KraftBg = new Color(0.66f, 0.52f, 0.36f);
    static readonly Color Bead = new Color(0.02f, 0.015f, 0.012f);

    const float EggRx = 0.31f, EggRy = 0.39f, EggRz = 0.28f, EggTaper = 0.12f;
    static readonly Vector3 EggCenter = new Vector3(0f, 0.04f, 0f);
    static readonly Vector3 JunctionScale = new Vector3(0.24f, 0.18f, 0.22f);

    [MenuItem("Escape/Build Scene")]
    public static void Build()
    {
        SetupPipeline.Ensure();
        AssetDatabase.ImportAsset("Assets/Look/FeltFiber.shader");
        AssetDatabase.ImportAsset("Assets/Look/PineWool.shader");
        AssetDatabase.ImportAsset("Assets/Look/Bead.shader");
        foreach (var t in new[]
        {
            "tex-felt-brown", "tex-felt-cream", "tex-felt-red", "tex-yarn-green",
            "tex-yarn-fiber", "tex-yarn-flower", "tex-burlap", "tex-kraft-corrugated",
            "tex-kraft-flute", "tex-roast-bean", "tex-felt-normal", "tex-burlap-normal"
        })
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

        var bean = StitchedBean();
        bean.transform.position = new Vector3(0f, 0.46f, 0f);
        bean.transform.rotation = Quaternion.Euler(0f, 145f, 0f);

        var camGo = new GameObject("Set Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = KraftBg;
        cam.usePhysicalProperties = false;
        cam.fieldOfView = 46.4f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 90f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        var follow = camGo.AddComponent<SetCameraFollow>();
        follow.target = bean.transform;
        follow.offset = new Vector3(0.55f, 1.35f, -5.45f);
        follow.restY = 0.46f;
        follow.pitch = 7.5f;
        camGo.transform.position = new Vector3(follow.offset.x, follow.restY + follow.offset.y, follow.offset.z);
        camGo.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);

        var sessionGo = new GameObject("Game Session");
        var session = sessionGo.AddComponent<GameSession>();
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        session.catalog = cat;

        var runnerGo = new GameObject("Level Runner");
        var runner = runnerGo.AddComponent<LevelRunner>();
        runner.bean = bean.transform;
        runnerGo.AddComponent<LevelSetSpawner>();

        var ui = new GameObject("Title Flow");
        ui.AddComponent<TitleFlow>();

        // Populate all opening-yard felt props, wall, clouds/cord, title, floor, goal, and bean surface
        OpeningYard.Keep();

        if ( bean.GetComponent<Collider>() != null )
            Grip(bean);
        var floor = GameObject.Find("Burlap Floor");
        if (floor != null) Grip(floor);

        EditorSceneManager.SaveScene(scene, "Assets/Escape.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("BUILD_SCENE_OK Assets/Escape.unity");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void ImportLookTexture(string path, bool normal)
    {
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

    static void Lights()
    {
        var key = new GameObject("Warm Key");
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional;
        kl.intensity = 0.92f;
        kl.color = new Color(1.0f, 0.96f, 0.90f);
        kl.shadows = LightShadows.Soft;
        kl.shadowStrength = 0.60f;
        kl.shadowBias = 0.03f;
        kl.shadowNormalBias = 0.3f;
        key.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

        // Bean Soft Key / Soft Fill HELD (k511 / k494) — values untouched.
        var beanKey = new GameObject("Bean Soft Key");
        var bk = beanKey.AddComponent<Light>();
        bk.type = LightType.Point;
        bk.intensity = 2.956f; // k511 Soft Key HELD
        bk.range = 3.30f;
        bk.color = new Color(1.058f, 0.999f, 0.914f);
        beanKey.transform.position = new Vector3(0.04f, 1.36f, 0.28f);

        var beanFill = new GameObject("Bean Soft Fill");
        var bf = beanFill.AddComponent<Light>();
        bf.type = LightType.Point;
        bf.intensity = 2.576f; // k494 Soft Fill HELD
        bf.range = 3.20f;
        bf.color = new Color(1.050f, 0.997f, 0.914f);
        beanFill.transform.position = new Vector3(-0.26f, 1.14f, 0.50f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.43f, 0.38f);
        RenderSettings.fog = false;
    }

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
        col.height = 0.86f;
        col.direction = 1;
        root.AddComponent<BeanImpulse>();
        root.AddComponent<LandFeel>();
        root.AddComponent<BeanAnimator>();

        var body = new GameObject("Bean Body");
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = EggCenter;
        body.AddComponent<MeshFilter>().sharedMesh = CraftMesh.Egg(EggRx, EggRy, EggRz, EggTaper, 40, 28);
        var bodyR = body.AddComponent<MeshRenderer>();
        bodyR.sharedMaterial = BeanMaterial();
        bodyR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        // 12 Dash Stitch transforms kept on Imagine Bean (disabled by FeltBeanSurface in favor of Yarn Seam)
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
            dr.enabled = false;
        }

        var beadMat = CraftMesh.Shared("bead-eye", () => CraftMesh.Bead(Bead, 0.95f));
        var hlMat = CraftMesh.Shared("bead-hl", () => CraftMesh.Bead(new Color(0.98f, 0.97f, 0.94f), 0.2f));
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 p = CraftMesh.EggPoint(EggRx, EggRy, EggRz, EggTaper, 9f, side * 24f);
            Vector3 n = new Vector3(p.x / (EggRx * EggRx), p0Y(p.y), p.z / (EggRz * EggRz)).normalized;
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

        var limbMat = BeanMaterial();
        Arm(root.transform, -1, limbMat);
        Arm(root.transform, 1, limbMat);
        Leg(root.transform, -1, limbMat);
        Leg(root.transform, 1, limbMat);

        FeltBeanSurface.Apply(root);
        return root;
    }

    static float p0Y(float y) => y / (EggRy * EggRy);

    static Material BeanMaterial()
    {
        return CraftMesh.Shared("bean-body-felt", () =>
        {
            var tex = Resources.Load<Texture2D>("Look/tex-felt-brown");
            var m = CraftMesh.Felt(tex, FeltBrown, new Vector2(2.2f, 2.2f), 1.8f, 0.12f, 0.86f, 0.98f, 0f);
            if (m.HasProperty("_FiberAmt")) m.SetFloat("_FiberAmt", 0.12f);
            return m;
        });
    }

    static void Junction(Transform limb, string name, Material mat)
    {
        var j = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        j.name = name;
        j.transform.SetParent(limb, false);
        j.transform.localPosition = new Vector3(0f, -0.92f, 0f);
        j.transform.localScale = JunctionScale;
        Object.DestroyImmediate(j.GetComponent<Collider>());
        CraftMesh.UseLowSphere(j);
        var r = j.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Arm(Transform parent, int side, Material mat)
    {
        bool left = side < 0;
        var pivot = new GameObject(left ? "Arm Pivot L" : "Arm Pivot R");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(side * 0.255f, 0.02f, 0.01f);
        pivot.transform.localRotation = Quaternion.identity;

        var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        arm.name = "Arm";
        arm.transform.SetParent(pivot.transform, false);
        arm.transform.localRotation = Quaternion.Euler(left ? 6f : -4f, 0f, -side * 160f);
        arm.transform.localPosition = arm.transform.localRotation * Vector3.up * 0.115f;
        arm.transform.localScale = new Vector3(0.105f, 0.13f, 0.105f);
        Object.DestroyImmediate(arm.GetComponent<Collider>());
        arm.GetComponent<Renderer>().sharedMaterial = mat;

        Junction(arm.transform, "Arm Junction", mat);

        var mitt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mitt.name = "Arm Mitt";
        mitt.transform.SetParent(arm.transform, false);
        mitt.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        mitt.transform.localScale = new Vector3(1.22f, 0.92f, 1.18f);
        Object.DestroyImmediate(mitt.GetComponent<Collider>());
        mitt.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Leg(Transform parent, int side, Material mat)
    {
        bool left = side < 0;
        var pivot = new GameObject(left ? "Leg Pivot L" : "Leg Pivot R");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = new Vector3(side * 0.12f, -0.30f, 0.02f);
        pivot.transform.localRotation = Quaternion.identity;

        var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leg.name = "Leg";
        leg.transform.SetParent(pivot.transform, false);
        leg.transform.localRotation = Quaternion.Euler(0f, 0f, side * 4f);
        leg.transform.localPosition = new Vector3(0f, -0.065f, 0f);
        leg.transform.localScale = new Vector3(0.12f, 0.085f, 0.12f);
        Object.DestroyImmediate(leg.GetComponent<Collider>());
        leg.GetComponent<Renderer>().sharedMaterial = mat;

        Junction(leg.transform, "Leg Junction", mat);

        var foot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foot.name = "Foot";
        foot.transform.SetParent(pivot.transform, false);
        foot.transform.localPosition = new Vector3(side * 0.005f, -0.115f, 0.03f);
        foot.transform.localScale = new Vector3(0.15f, 0.095f, 0.17f);
        Object.DestroyImmediate(foot.GetComponent<Collider>());
        foot.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Grip(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col == null) return;
        col.material = new PhysicsMaterial("BurlapGrip")
        {
            dynamicFriction = 0.35f,
            staticFriction = 0.4f,
            bounciness = 0.02f,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }
}

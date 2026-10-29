using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Opening handmade felt diorama (DriveLook/imagine/285.jpg, frames/set-01..06.jpg, library/rotate-*.mp4).
/// Cleans any leftover primitive roots and builds/rebuilds the opening props from the Props/ builders,
/// including when a root already exists. Ground colliders, hop gaps, and BeanImpulse stay untouched.
public static class OpeningYard
{
    static readonly string[] ReplaceRoots =
    {
        "Kraft Backdrop", "Backdrop", "Yarn Title", "Title Sign", "Hanging Yarn",
        "Pine", "Red Tree", "Flower Tree", "Dotted Tree",
        "Cardboard Grinder", "Kraft Bridge", "Bean Walkway",
        "Moss", "Sage", "Roast Bean", "Yarn Ball", "Goal Marker",
        "FloorWall Seam", "Twine", "Felt Cloud", "Burlap Path", "Step", "Contact Shadow", "Sphere"
    };

    static readonly string[] ReplacePrefixes =
    {
        "Kraft Rib", "Kraft Flute", "Pine ", "Pinecone", "Yarn Loop", "Red Trunk",
        "Bloom ", "Flower Petal", "Path Yarn Edge", "Bridge ",
        "Grinder ", "Drawer Knob", "Crank Knob", "Succulent ", "Title "
    };

    public static void Keep()
    {
        CleanSceneRoots();

        var bean = GameObject.Find("Imagine Bean");
        if (bean != null)
        {
            if (!Application.isPlaying)
            {
                bean.transform.position = new Vector3(0f, 0.46f, 0f);
                bean.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
            }
            FeltBeanSurface.Apply(bean);
        }

        EnsureFloor();

        // 1. Corrugated vertical-flute kraft wall + jute cord + small pillow clouds on upper wall (rotate-wall / rotate-cloud / rotate-cord)
        var wall = FeltWall.Build(new Vector3(0f, -0.05f, 2.25f), 24f);
        var bf = wall.AddComponent<BackdropFollow>();
        bf.aheadX = 0.55f;
        if (bean != null) bf.target = bean.transform;
        if (wall.GetComponent<AmbientMotion>() == null)
            wall.AddComponent<AmbientMotion>();

        // 2. One stitched felt title on the upper wall, no mirrored copy (rotate-title)
        FeltTitle.Build(new Vector3(-0.35f, 1.92f, 1.92f), 0.36f);

        YardProps.Clear();
        // 3. Scalloped felt pines with cones and short trunk (rotate-pine, 285.jpg)
        Yard(FeltPine.Build(new Vector3(-1.85f, 0f, 1.38f), 0.96f));
        Yard(FeltPine.Build(new Vector3(-1.12f, 0f, 1.52f), 1.08f));
        Yard(FeltPine.Build(new Vector3(1.65f, 0f, 1.28f), 0.74f));

        // 4. Red pompom tree on flared knitted trunk (rotate-red-tree, 285.jpg)
        Yard(FeltRedTree.Build(new Vector3(0.96f, 0f, 1.42f), 1.05f));

        // 5. Cardboard grinder on left of yard (rotate-grinder, set-01.jpg)
        Yard(FeltSetProps.CardboardGrinder(new Vector3(-2.35f, 0f, 0.82f), 0.84f));

        // 6. Kraft bridge down the path on right of opening yard (FeltSetProps.KraftBridge untouched)
        FeltSetProps.KraftBridge(new Vector3(2.55f, 0f, 0.25f));

        // 7. Flower tree & dotted trees framing the yard and bridge (rotate-flower-tree, rotate-dotted-tree, set-01.jpg)
        Yard(FeltFlowerTree.Build(new Vector3(3.85f, 0f, 1.28f), 0.92f));
        Yard(FeltDottedTree.Build(new Vector3(-3.05f, 0f, 1.38f), 0.92f));
        Yard(FeltDottedTree.Build(new Vector3(2.85f, 0f, 1.58f), 0.72f));
        Yard(FeltDottedTree.Build(new Vector3(4.58f, 0f, 1.35f), 0.84f));

        // 8. Moss balls in right foreground/midground (rotate-moss, 285.jpg)
        Yard(FeltSetProps.MossBall(new Vector3(0.98f, 0f, -0.10f), 0.26f, 1));
        Yard(FeltSetProps.MossBall(new Vector3(1.10f, 0f, -0.58f), 0.24f, 2));
        Yard(FeltSetProps.MossBall(new Vector3(0.58f, 0f, -0.64f), 0.18f, 3));

        // 9. Plump sage rosettes on the burlap (rotate-sage, 285.jpg, set-01.jpg)
        Yard(FeltSetProps.Sage(new Vector3(-1.18f, 0f, -0.62f), 0.70f));
        Yard(FeltSetProps.Sage(new Vector3(-2.65f, 0f, -0.45f), 0.68f));
        Yard(FeltSetProps.Sage(new Vector3(3.55f, 0f, -0.38f), 0.66f));

        // 10. Small dark roast beans on the burlap (rotate-roast-bean, 285.jpg, set-01.jpg)
        Yard(FeltSetProps.RoastBean(new Vector3(-0.92f, 0f, -0.26f), 0.11f, 28f));
        Yard(FeltSetProps.RoastBean(new Vector3(0.78f, 0f, -0.78f), 0.11f, -34f));
        Yard(FeltSetProps.RoastBean(new Vector3(-2.45f, 0f, -0.62f), 0.10f, 18f));
        Yard(FeltSetProps.RoastBean(new Vector3(3.32f, 0f, -0.55f), 0.11f, 42f));
        Yard(FeltSetProps.RoastBean(new Vector3(3.78f, 0f, -0.42f), 0.11f, -52f));

        // 11. Small crocheted yarn balls (rotate-yarn-ball, set-01.jpg)
        Yard(FeltSetProps.YarnBall(new Vector3(-2.78f, 0f, 0.92f), 0.16f, false));
        Yard(FeltSetProps.YarnBall(new Vector3(3.25f, 0f, 1.05f), 0.15f, false));
        Yard(FeltSetProps.YarnBall(new Vector3(3.55f, 0f, -0.52f), 0.18f, false));
        Yard(FeltSetProps.YarnBall(new Vector3(4.82f, 0f, 0.85f), 0.17f, false));

        // 12. Fluted tan felt goal post on square stitched mat (rotate-goal.mp4)
        var goal = FeltSetProps.GoalPost(new Vector3(14f, 0f, 0.55f));

        var runner = Object.FindAnyObjectByType<LevelRunner>();
        if (runner != null)
        {
            if (runner.bean == null && bean != null) runner.bean = bean.transform;
            runner.goalMarker = goal.transform;
        }

        EnsureWideCamera(bean != null ? bean.transform : null);
    }

    /// L1 hero props built by Keep (pines, red tree, grinder, flower / dotted trees, moss, sage, roast, yarn).
    /// The wall, title, floor, kraft bridge and goal stay for every level; these are L1-only and are hidden while
    /// LevelSetSpawner dresses L2–L5 (PR-25 P0: the handmade yard used to stand in for every level).
    static readonly List<GameObject> YardProps = new List<GameObject>();

    static void Yard(GameObject go)
    {
        if (go != null) YardProps.Add(go);
    }

    /// Show the L1 hero yard (true) or hide it while another level's set is spawned (false).
    public static void SetYardPropsVisible(bool visible)
    {
        for (int i = YardProps.Count - 1; i >= 0; i--)
        {
            if (YardProps[i] == null) { YardProps.RemoveAt(i); continue; }
            if (YardProps[i].activeSelf != visible) YardProps[i].SetActive(visible);
        }
    }

    /// Number of live L1 hero props (tests and smoke logs).
    public static int YardPropCount => YardProps.Count;

    static void CleanSceneRoots()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;
        var roots = scene.GetRootGameObjects();
        var kill = new List<GameObject>(64);
        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (go == null) continue;
            if (ShouldReplaceRoot(go.name))
                kill.Add(go);
        }
        for (int i = 0; i < kill.Count; i++)
        {
            if (Application.isPlaying) Object.Destroy(kill[i]);
            else Object.DestroyImmediate(kill[i]);
        }
    }

    static bool ShouldReplaceRoot(string name)
    {
        for (int i = 0; i < ReplaceRoots.Length; i++)
            if (name == ReplaceRoots[i]) return true;
        for (int i = 0; i < ReplacePrefixes.Length; i++)
            if (name.StartsWith(ReplacePrefixes[i])) return true;
        return false;
    }

    /// Square tan burlap weave from rotate-floor.mp4, fuzz off (_BumpScale 0, _FiberAmt 0).
    public static void EnsureFloor()
    {
        var burlapTex = Resources.Load<Texture2D>("Look/tex-burlap");
        var floor = GameObject.Find("Burlap Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Burlap Floor";
        }
        floor.transform.position = new Vector3(-3f, -0.08f, 0f);
        floor.transform.rotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(12f, 0.16f, LevelRunner.LaneDepth);
        floor.isStatic = true;

        float tileX = 12f * 1.6f;
        float tileZ = LevelRunner.LaneDepth * 1.6f;
        var floorMat = CraftMesh.Shared("opening-burlap-floor-sq", () =>
        {
            var m = CraftMesh.Felt(burlapTex, Color.white, new Vector2(tileX, tileZ), 0f, 0.02f, 0.70f, 0.98f, 0f);
            if (m != null && m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0f);
            if (m != null && m.HasProperty("_FiberAmt")) m.SetFloat("_FiberAmt", 0f);
            return m;
        });
        var r = floor.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = floorMat;

        // Ensure edit-mode ground under the right side of the yard (LevelRunner replaces Level Ground in Play Mode)
        if (!Application.isPlaying)
        {
            var groundRootGo = GameObject.Find("Level Ground");
            if (groundRootGo == null) groundRootGo = new GameObject("Level Ground");
            var groundRoot = groundRootGo.transform;
            for (int i = groundRoot.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(groundRoot.GetChild(i).gameObject);

            float[] edges = LevelRunner.L1Edges;
            for (int i = 0; i + 1 < edges.Length; i += 2)
            {
                float x0 = edges[i], x1 = edges[i + 1], len = x1 - x0;
                var blk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blk.name = "Burlap Ground";
                blk.transform.SetParent(groundRoot, false);
                blk.transform.position = new Vector3((x0 + x1) * 0.5f, -0.08f, 0f);
                blk.transform.localScale = new Vector3(len, 0.16f, LevelRunner.LaneDepth);
                blk.isStatic = true;
                float bxTile = Mathf.Max(1f, len * 1.6f);
                blk.GetComponent<Renderer>().sharedMaterial = CraftMesh.Shared($"opening-burlap-blk-{bxTile:0.##}", () =>
                {
                    var m = CraftMesh.Felt(burlapTex, Color.white, new Vector2(bxTile, tileZ), 0f, 0.02f, 0.70f, 0.98f, 0f);
                    if (m != null && m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0f);
                    if (m != null && m.HasProperty("_FiberAmt")) m.SetFloat("_FiberAmt", 0f);
                    return m;
                });
            }
        }
    }

    public static void EnsureWideCamera(Transform bean)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camObj = GameObject.Find("Set Camera");
            if (camObj != null) cam = camObj.GetComponent<Camera>();
        }
        if (cam == null) return;

        cam.usePhysicalProperties = false;
        // Batch 2 (285.jpg match): closer, tilted 16 deg down into the set, bean ~1/3 of frame height.
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 90f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.66f, 0.52f, 0.36f);

        var follow = cam.GetComponent<SetCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<SetCameraFollow>();
        if (bean != null) follow.target = bean;
        follow.offset = new Vector3(0.0f, 1.15f, -3.56f); // look step00 (spec D22): bean centred 0.50; z kept (-3.10 made the bean 59 % of frame, ref 34 %); was (0.80, 1.15, -3.56)
        follow.restY = 0.46f;
        follow.pitch = 17f;                                 // look step00 (spec section 6, 18 +- 2): 17 puts the bean feet at 0.71 (tight-vertex log); was 16
        follow.leadBySpeed = 0.16f;                         // was 0.14: a little more look-ahead in the tighter frame

        if (!Application.isPlaying)
        {
            float bx = bean != null ? bean.position.x : 0f;
            cam.transform.position = new Vector3(bx + follow.offset.x, follow.restY + follow.offset.y, follow.offset.z);
            cam.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);
        }
    }
}

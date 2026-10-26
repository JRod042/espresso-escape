using System.Collections.Generic;
using UnityEngine;

/// Opening-yard props. No colliders.
/// DriveLook/grok/library-bridge-1.jpg — arched bridge down the path, not the 285.jpg opening crop.
/// Assets/Look/refs/library/rotate-grinder.mp4 — square kraft box, flute only on the cut rims,
/// two front drawers, a side door with a loop, a domed bowl, and a wood crank.
/// rotate-moss.mp4 — one dense moss-green felt ball.
/// rotate-sage.mp4 — plump pale sage rosette, flat outer leaves, pointed heart.
/// rotate-roast-bean.mp4 — one dark roast bean with a gold crease.
/// rotate-yarn-ball.mp4 — cream crocheted ball with one wound spiral.
///
/// `pos` is the burlap contact. Grinder and Sage put `scale` on the root.
/// MossBall, RoastBean, and YarnBall keep scale 1 and treat `scale` as metres.
/// Escape/FeltFiber through CraftMesh.Felt. Look textures from Resources.
public static class FeltSetProps
{
    // Yarn-fiber carries the bridge grain. A warm brown multiplier keeps it wood, not pale yarn.
    static readonly Color Wood = new Color(0.74f, 0.58f, 0.42f);
    static readonly Color WoodDeep = new Color(0.62f, 0.48f, 0.34f);
    static readonly Color Kraft = new Color(0.98f, 0.93f, 0.86f);
    static readonly Color KraftDeep = new Color(0.78f, 0.68f, 0.56f);
    static readonly Color Burlap = new Color(0.64f, 0.62f, 0.58f);
    static readonly Color MossOlive = new Color(0.70f, 0.92f, 0.42f); // rotate-moss.mp4: medium moss, fibre in the light
    static readonly Color SageGreen = new Color(1.18f, 1.16f, 1.08f); // bridge tufts; sage leaves use SageLeaf
    static readonly Color SageLeaf = new Color(0.94f, 1.08f, 0.84f); // rotate-sage.mp4 pale knitted sage
    static readonly Color SageTip = new Color(1.04f, 1.14f, 0.90f);
    static readonly Color Bean = new Color(0.78f, 0.62f, 0.50f); // rotate-roast-bean.mp4 chocolate, keeps the roast texture
    static readonly Color BeanShade = new Color(0.62f, 0.46f, 0.36f);
    static readonly Color Crease = new Color(0.16f, 0.10f, 0.07f);
    static readonly Color Chaff = new Color(1.08f, 0.78f, 0.40f); // gold fissure down the crease

    const string KraftTex = "Look/tex-kraft-corrugated";
    const string BurlapTex = "Look/tex-burlap";
    const string GreenTex = "Look/tex-yarn-green";
    const string YarnTex = "Look/tex-yarn-fiber";
    const string RoastTex = "Look/tex-roast-bean";
    const string BrownTex = "Look/tex-felt-brown";
    const string CreamTex = "Look/tex-felt-cream";

    static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

    /// library-bridge-1.jpg, detail-bridge-closer.png, library-wide-bridge-1.jpg.
    /// 2.10 m along X, 0.86 m across Z, rise 0.42. Caller places it down the path.
    /// This mesh does not move `pos` toward the opening camera.
    public static GameObject KraftBridge(Vector3 pos) => KraftBridge(pos, 2.10f, 0.86f, 0.42f);

    /// Arched kraft bridge. Round wood-grain planks with a dark seam, three posts a side,
    /// two rails, open onto a burlap runner. Not a slab and not a flat box.
    public static GameObject KraftBridge(Vector3 pos, float length, float width, float rise)
    {
        length = Mathf.Max(0.8f, length);
        width = Mathf.Max(0.4f, width);
        rise = Mathf.Max(0.05f, rise);

        var root = Root("Kraft Bridge", pos, 1f);
        var host = root.transform;
        float half = length * 0.5f;
        float deckW = width * 0.70f;
        const int planks = 15;
        const float postH = 0.30f;
        const float postR = 0.040f;
        const float plankR = 0.046f;
        const float ground = 0.012f;
        float endT = 0.5f / planks;
        // -X is the near foot (toward the opening). +X is farther down the path.
        float nearX = -half - 0.04f;
        float farX = half + 0.08f;
        float hz = deckW * 0.5f + postR * 0.35f;

        Color plank = new Color(0.86f, 0.70f, 0.52f);
        Color postTint = new Color(0.46f, 0.34f, 0.24f);
        Color seam = new Color(0.20f, 0.14f, 0.10f);
        Color railTint = new Color(0.56f, 0.42f, 0.30f);

        // Path cloth under the opening. End mats sit on top of it.
        ClothPad(host, "Bridge Burlap", length * 0.92f, 0.008f, width * 0.50f, 2.4f, 1.7f,
            new Vector3(0f, 0.004f, 0f), Burlap, 0.40f);
        // Near foot: lighter rectangle with a darker hem showing around it.
        float matL = Mathf.Clamp(length * 0.26f, 0.38f, 0.58f);
        float matW = Mathf.Max(0.46f, width * 0.98f);
        ClothPad(host, "Bridge Near Hem", matL, 0.008f, matW, 2.0f, 1.5f,
            new Vector3(nearX, 0.006f, 0f), new Color(0.52f, 0.46f, 0.38f), 0.55f);
        ClothPad(host, "Bridge Near Cloth", matL * 0.78f, 0.012f, matW * 0.78f, 3.4f, 2.6f,
            new Vector3(nearX, 0.012f, 0f), new Color(0.86f, 0.78f, 0.66f), 0.22f);
        // Far foot: bigger weave, same floor cloth, not the hemmed mat.
        ClothPad(host, "Bridge Far Cloth", matL * 1.2f, 0.010f, matW * 1.08f, 1.05f, 0.75f,
            new Vector3(farX, 0.007f, 0f), new Color(0.58f, 0.50f, 0.42f), 0.95f);

        float RawDeck(float t) => rise * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        float seat = RawDeck(endT);
        float Deck(float t) => RawDeck(t) - seat;
        Vector3 OnDeck(float t, float z, float extraY) =>
            new Vector3(Mathf.Lerp(-half, half, t), Deck(t) + ground + extraY, z);
        Vector3 Butt(Vector3 end, Vector3 other, float inset)
        {
            Vector3 flat = other - end;
            flat.y = 0f;
            float mag = flat.magnitude;
            if (mag < 0.001f) return end;
            return end + flat * (inset / mag);
        }

        float span = deckW * 0.5f;
        for (int i = 0; i < planks; i++)
        {
            float t = (i + 0.5f) / planks;
            Vector3 a = OnDeck(t, -span, plankR);
            Vector3 b = OnDeck(t, span, plankR);
            Tube(host, "Bridge Plank", a, b, plankR, new Vector2(4f, 1.15f), plank);
            if (i == planks - 1) continue;
            float ts = (i + 1f) / planks;
            Tube(host, "Bridge Seam", OnDeck(ts, -span * 0.92f, plankR * 0.42f), OnDeck(ts, span * 0.92f, plankR * 0.42f),
                plankR * 0.28f, Vector2.one, seam, YarnTex, 0.3f, 0.02f, 0.2f, 0.97f, 0.01f);
        }

        // Edge curves only. The middle stays open so the runner shows through.
        const int archSegs = 10;
        const float beamR = 0.016f;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int layer = 0; layer < 2; layer++)
            {
                float z = side * (span - 0.01f - layer * 0.022f);
                for (int i = 0; i < archSegs; i++)
                {
                    float ta = Mathf.Lerp(endT, 1f - endT, i / (float)archSegs);
                    float tb = Mathf.Lerp(endT, 1f - endT, (i + 1f) / archSegs);
                    float drop = 0.004f + layer * 0.030f;
                    Vector3 a = OnDeck(ta, z, -drop);
                    Vector3 b = OnDeck(tb, z, -drop);
                    a.y = Mathf.Max(ground, a.y);
                    b.y = Mathf.Max(ground, b.y);
                    Tube(host, "Bridge Arch", a, b, beamR, new Vector2(3f, 1f), layer == 0 ? railTint : postTint);
                }
            }
        }

        float railLo = plankR * 2f + 0.020f;
        float railHi = plankR * 2f + postH * 0.78f;
        float[] postT = { endT, 0.5f, 1f - endT };
        const int railSegs = 4;
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * hz;
            for (int i = 0; i < postT.Length; i++)
            {
                Vector3 foot = OnDeck(postT[i], z, plankR * 2f);
                Tube(host, "Bridge Post", foot, foot + Vector3.up * postH, postR, new Vector2(7f, 1.3f), postTint);
            }

            for (int bay = 0; bay < postT.Length - 1; bay++)
            {
                for (int s = 0; s < railSegs; s++)
                {
                    float ta = Mathf.Lerp(postT[bay], postT[bay + 1], s / (float)railSegs);
                    float tb = Mathf.Lerp(postT[bay], postT[bay + 1], (s + 1f) / railSegs);
                    Vector3 aHi = OnDeck(ta, z, railHi);
                    Vector3 bHi = OnDeck(tb, z, railHi);
                    Vector3 aLo = OnDeck(ta, z, railLo);
                    Vector3 bLo = OnDeck(tb, z, railLo);
                    if (s == 0)
                    {
                        aHi = Butt(aHi, bHi, postR);
                        aLo = Butt(aLo, bLo, postR);
                    }
                    if (s == railSegs - 1)
                    {
                        bHi = Butt(bHi, aHi, postR);
                        bLo = Butt(bLo, aLo, postR);
                    }
                    Tube(host, "Bridge Handrail", aHi, bHi, 0.022f, new Vector2(5f, 1f), railTint);
                    Tube(host, "Bridge Rail", aLo, bLo, 0.011f, new Vector2(5f, 1f), postTint);
                }

                for (int u = 1; u <= 2; u++)
                {
                    float t = Mathf.Lerp(postT[bay], postT[bay + 1], u / 3f);
                    Tube(host, "Bridge Upright", OnDeck(t, z, railLo), OnDeck(t, z, railHi),
                        0.007f, new Vector2(2f, 1f), railTint);
                }
            }
        }

        float off = span + 0.05f;
        Tuft(host, new Vector3(nearX - 0.06f, 0.016f, off), 0.52f, 11);
        Tuft(host, new Vector3(nearX + 0.04f, 0.016f, -off), 0.46f, 23);
        Tuft(host, new Vector3(farX + 0.05f, 0.014f, off * 0.94f), 0.50f, 37);
        Tuft(host, new Vector3(farX - 0.08f, 0.014f, -off), 0.44f, 41);
        return root;
    }

    static void ClothPad(Transform host, string name, float sx, float sy, float sz, float tileX, float tileZ, Vector3 at, Color tint, float bump)
    {
        var pad = Mesh(host, Cloth(sx, sy, sz, tileX, tileZ), name, at, Quaternion.identity, Vector3.one);
        Paint(pad, BurlapTex, tint, Vector2.one, bump, 0.02f, 0.70f, 0.98f, 0f);
    }

    /// library/rotate-grinder.mp4. Square kraft box. The flute is the cut rim on the cap and the base,
    /// not ribs down the walls. Two drawers face -Z. The door and its loop are on +X.
    /// A turned-wood bowl with a dome and a crank sits on the cap. `scale` is on the root.
    public static GameObject Grinder(Vector3 pos, float scale) => CardboardGrinder(pos, scale);

    public static GameObject CardboardGrinder(Vector3 pos, float scale)
    {
        scale = Mathf.Max(0.05f, scale);
        var root = Root("Cardboard Grinder", pos, scale);
        var host = root.transform;

        const float baseH = 0.078f;
        const float body = 0.42f;
        const float bodyW = 0.46f;
        const float capH = 0.082f;
        const float baseW = 0.62f;
        const float capW = 0.54f;
        Color paper = new Color(1.06f, 0.84f, 0.62f);
        Color wood = new Color(1.02f, 0.76f, 0.46f);
        Color woodDeep = new Color(0.78f, 0.56f, 0.34f);
        float top = baseH + body + capH;
        float faceZ = -bodyW * 0.5f;
        float faceX = bodyW * 0.5f;

        GameObject WoodBar(string name, Vector3 a, Vector3 b, float radius, Color tint)
        {
            return Tube(host, name, a, b, radius, new Vector2(2.2f, 1f), tint, BrownTex, 0.22f, 0.26f, 0.24f, 0.52f, 0.04f);
        }

        void FluteBand(string name, float y, float width, float height, float amp)
        {
            var band = Mesh(host, FlutedBox(width, height, width, amp, 0.018f, true), name,
                new Vector3(0f, y, 0f), Quaternion.identity, Vector3.one);
            Paint(band, KraftTex, Kraft, new Vector2(1.6f, 2.4f), 0.9f, 0.05f, 0.38f, 0.92f, 0.03f);
        }

        void PaperBox(string name, float sx, float sy, float sz, int axis, Vector3 at, Color tint, string tex)
        {
            var box = Mesh(host, GrainBox(sx, sy, sz, axis, 8f), name, at, Quaternion.identity, Vector3.one);
            Paint(box, tex, tint, new Vector2(1.4f, 1.7f), 0.32f, 0.08f, 0.42f, 0.9f, 0.04f);
        }

        // Cut corrugated edges, then the smooth kraft cube between them.
        FluteBand("Grinder Base", 0f, baseW, baseH, 0.011f);
        PaperBox("Grinder Box", bodyW, body, bodyW, 1, new Vector3(0f, baseH + body * 0.5f, 0f), paper, BrownTex);
        FluteBand("Grinder Cap", baseH + body, capW, capH, 0.010f);

        // Front (-Z): pale upper drawer, warmer lower drawer, each with a wood knob.
        float yLo = baseH + body * 0.30f;
        float yUp = baseH + body * 0.66f;
        const float drawerW = 0.32f;
        const float drawerH = 0.105f;
        PaperBox("Grinder Drawer", drawerW, drawerH, 0.028f, 0,
            new Vector3(0f, yUp, faceZ - 0.016f), new Color(1.08f, 0.98f, 0.86f), CreamTex);
        PaperBox("Grinder Drawer", drawerW, drawerH * 0.96f, 0.030f, 0,
            new Vector3(0f, yLo, faceZ - 0.018f), new Color(0.98f, 0.78f, 0.52f), YarnTex);
        Knob(host, "Drawer Knob", new Vector3(0f, yUp, faceZ - 0.046f), 0.020f);
        Knob(host, "Drawer Knob", new Vector3(0f, yLo, faceZ - 0.050f), 0.024f);

        // Right (+X): square door, a cardboard loop, and the round wood knob in front of it.
        float doorY = baseH + body * 0.50f;
        PaperBox("Grinder Door", 0.022f, body * 0.74f, bodyW * 0.74f, 1,
            new Vector3(faceX + 0.012f, doorY, 0f), paper, BrownTex);
        Vector3 strap = new Vector3(faceX + 0.03f, doorY, -0.07f);
        Color strapTint = new Color(0.86f, 0.66f, 0.46f);
        WoodBar("Door Strap", strap + new Vector3(0f, 0.055f, 0f), strap + new Vector3(0.028f, 0.055f, 0f), 0.007f, strapTint);
        WoodBar("Door Strap", strap + new Vector3(0.028f, 0.055f, 0f), strap + new Vector3(0.028f, -0.05f, 0f), 0.007f, strapTint);
        WoodBar("Door Strap", strap + new Vector3(0.028f, -0.05f, 0f), strap + new Vector3(0f, -0.05f, 0f), 0.007f, strapTint);
        Knob(host, "Door Knob", strap + new Vector3(0.055f, -0.01f, 0f), 0.034f);

        // Turned bowl, domed lid, crank off to +X (the right side in the front view).
        Color bowlTint = new Color(0.98f, 0.74f, 0.48f);
        WoodBar("Hopper Foot", new Vector3(0f, top, 0f), new Vector3(0f, top + 0.022f, 0f), 0.11f, woodDeep);
        const float bowlH = 0.15f;
        const float bowlR = 0.21f;
        var bowl = Mesh(host, CraftMesh.Bowl(bowlR, 0.10f, bowlH, 0.016f, 0.055f, 28, 8),
            "Grinder Hopper", new Vector3(0f, top + 0.012f, 0f), Quaternion.identity, Vector3.one);
        Paint(bowl, BrownTex, bowlTint, new Vector2(2.4f, 1.6f), 0.28f, 0.22f, 0.30f, 0.58f, 0.04f);
        var rim = Mesh(host, Torus(bowlR * 0.98f, 0.012f, 28, 8), "Grinder Rim",
            new Vector3(0f, top + 0.012f + bowlH, 0f), Quaternion.identity, Vector3.one);
        Paint(rim, BrownTex, woodDeep, Vector2.one, 0.2f, 0.18f, 0.28f, 0.6f, 0.03f);

        float rimY = top + 0.012f + bowlH;
        float domeY = rimY - 0.006f;
        var dome = Mesh(host, CraftMesh.Egg(0.150f, 0.070f, 0.150f, 0.06f, 22, 12), "Grinder Dome",
            new Vector3(0f, domeY, 0f), Quaternion.identity, Vector3.one);
        Paint(dome, BrownTex, wood, new Vector2(2f, 1.4f), 0.22f, 0.24f, 0.26f, 0.55f, 0.03f);

        float hubY = domeY + 0.084f;
        WoodBar("Grinder Shaft", new Vector3(0f, domeY + 0.02f, 0f), new Vector3(0f, hubY, 0f), 0.018f, wood);
        WoodBar("Grinder Nut", new Vector3(0f, hubY - 0.028f, 0f), new Vector3(0f, hubY - 0.006f, 0f), 0.032f, woodDeep);
        WoodBar("Grinder Nut", new Vector3(0f, hubY, 0f), new Vector3(0f, hubY + 0.022f, 0f), 0.026f, wood);
        Vector3 armA = new Vector3(0.02f, hubY + 0.006f, 0f);
        Vector3 armB = new Vector3(0.16f, hubY + 0.028f, -0.01f);
        Vector3 armC = new Vector3(0.30f, hubY + 0.04f, -0.015f);
        WoodBar("Grinder Crank", armA, armB, 0.016f, wood);
        WoodBar("Grinder Crank", armB, armC, 0.014f, wood);
        Knob(host, "Crank Knob", armC + new Vector3(0.012f, 0.01f, 0f), 0.042f);
        return root;
    }

    /// library/rotate-moss.mp4. One dense round felt ball. `scale` is the diameter.
    public static GameObject MossBall(Vector3 pos, float scale, int seed = 1)
    {
        if (scale > 0.40f) scale *= 0.24f;
        scale = Mathf.Max(0.04f, scale);
        float radius = scale * 0.5f;
        var root = Root("Moss", pos, 1f);
        // Fuzz stays small. The turnaround is a sphere with fibre on the skin, not a shaggy pompom.
        var ball = Mesh(root.transform, CraftMesh.Pompom(radius, 0.045f, 22, 16, Mathf.Max(1, seed)),
            "Moss Ball", new Vector3(0f, radius * 0.97f, 0f), Quaternion.Euler(0f, seed * 37f, 0f), Vector3.one);
        Paint(ball, GreenTex, MossOlive, new Vector2(2.6f, 2.6f), 1.7f, 0.48f, 0.70f, 0.94f, 0.20f);
        return root;
    }

    /// library/rotate-sage.mp4. Plump felt rosette, no pot. Outer leaves lie on the cloth.
    /// `scale` is on the root; 0.70 is the opening-yard size.
    public static GameObject Sage(Vector3 pos, float scale) => Succulent(pos, scale);

    public static GameObject Succulent(Vector3 pos, float scale)
    {
        scale = Mathf.Max(0.05f, scale);
        var root = Root("Sage", pos, scale);
        var host = root.transform;
        // Tip reach stays near the old rosette so yard scale still fits. The lean is what changed.
        SageWhorl(host, 10, 74f, 0.17f, 0.088f, 0.048f, 0f, SageLeaf, 3, new Vector3(0f, 0.006f, 0f), 0.12f);
        SageWhorl(host, 8, 48f, 0.13f, 0.062f, 0.040f, 16f, SageLeaf, 11, new Vector3(0f, 0.018f, 0f), 0.30f);
        SageWhorl(host, 6, 26f, 0.135f, 0.046f, 0.034f, 8f, SageTip, 19, new Vector3(0f, 0.036f, 0f), 0.50f);
        return root;
    }

    /// library/rotate-roast-bean.mp4. One dark roast bean. `scale` is the length. `yaw` is degrees.
    public static GameObject RoastBean(Vector3 pos, float scale, float yaw)
    {
        if (scale > 0.25f) scale *= 0.11f;
        scale = Mathf.Max(0.02f, scale);
        var root = Root("Roast Bean", pos, 1f);
        root.transform.rotation = Quaternion.Euler(12f, yaw, 6f);
        var host = root.transform;

        // Two lobes overlap into one oval. Long axis is local X after the lay-down rotation.
        float rx = scale * 0.19f, ry = scale * 0.50f, rz = scale * 0.17f;
        var lobeMesh = CraftMesh.Egg(rx, ry, rz, 0.06f, 20, 14);
        var lay = Quaternion.Euler(0f, 0f, 90f);
        float y = rx * 0.92f;
        float sep = scale * 0.145f;
        var left = Mesh(host, lobeMesh, "Roast Lobe", new Vector3(0f, y, -sep), lay, Vector3.one);
        var right = Mesh(host, lobeMesh, "Roast Lobe", new Vector3(0f, y, sep), lay, Vector3.one);
        // Tile stays inside the brown field of tex-roast-bean so the texture's centre line is not a second crease.
        var roastTile = new Vector2(0.42f, 1.7f);
        Paint(left, RoastTex, Bean, roastTile, 0.22f, 0.32f, 0.20f, 0.48f, 0.05f);
        Paint(right, RoastTex, BeanShade, roastTile, 0.22f, 0.32f, 0.20f, 0.48f, 0.05f);

        float cleft = y + rx * 0.48f;
        Vector3 creaseA = new Vector3(-scale * 0.34f, cleft, 0f);
        Vector3 creaseB = new Vector3(scale * 0.34f, cleft, 0f);
        Tube(host, "Roast Crease", creaseA, creaseB, scale * 0.032f, Vector2.one, Crease, RoastTex, 0.08f, 0.06f, 0.12f, 0.72f, 0.02f);
        Vector3 chaffLift = Vector3.up * (scale * 0.026f);
        Tube(host, "Roast Chaff", creaseA + chaffLift, creaseB + chaffLift, scale * 0.011f, Vector2.one, Chaff, CreamTex, 0.12f, 0.16f, 0.18f, 0.55f, 0.03f);

        void Crack(Vector3 origin, Vector3 dir)
        {
            Tube(host, "Roast Crack", origin, origin + dir, scale * 0.010f, Vector2.one, Crease, RoastTex, 0.05f, 0.04f, 0.1f, 0.8f, 0.01f);
        }
        Crack(creaseA, new Vector3(-scale * 0.07f, -rx * 0.45f, scale * 0.07f));
        Crack(creaseA, new Vector3(-scale * 0.05f, -rx * 0.25f, -scale * 0.08f));
        Crack(creaseB, new Vector3(scale * 0.08f, -rx * 0.40f, scale * 0.06f));
        Crack(creaseB, new Vector3(scale * 0.05f, -rx * 0.22f, -scale * 0.07f));
        return root;
    }

    /// library/rotate-yarn-ball.mp4. Cream crocheted ball with one wound spiral.
    /// `scale` is the diameter. `pos` is the burlap contact.
    /// The long string in the video is the hanger, not part of the ball.
    public static GameObject YarnBall(Vector3 pos, float scale, bool green = false) => YarnOrb(pos, scale, green);

    public static GameObject YarnOrb(Vector3 pos, float scale, bool green)
    {
        if (scale > 0.30f) scale *= 0.18f;
        scale = Mathf.Max(0.04f, scale);
        float radius = scale * 0.5f;
        var root = Root("Yarn Ball", pos, 1f);
        var host = root.transform;
        Vector3 center = Vector3.up * (radius * 0.98f);
        var core = Mesh(host, CraftMesh.Pompom(radius * 0.96f, 0.05f, 20, 14, green ? 2 : 5),
            "Yarn Ball", center, Quaternion.identity, Vector3.one);
        PaintYarn(core, green, new Vector2(3.2f, 3.2f));

        // Nested loops on one side. From that side they read as the spiral; the other faces stay bumpy.
        var strand = Torus(1f, 0.046f, 32, 8);
        Vector3 wind = center + new Vector3(radius * 0.22f, radius * 0.02f, 0f);
        var windRot = Quaternion.Euler(8f, 0f, 88f);
        float[] turn = { 0.78f, 0.62f, 0.46f, 0.32f };
        for (int i = 0; i < turn.Length; i++)
        {
            var wrap = Mesh(host, strand, "Yarn Wrap", wind, windRot, Vector3.one * (radius * turn[i]));
            PaintYarn(wrap, green, new Vector2(1.2f, 5f));
        }
        Vector3 tail0 = center + Vector3.up * (radius * 0.9f);
        Vector3 tail1 = tail0 + new Vector3(radius * 0.06f, radius * 0.42f, radius * 0.04f);
        Color tail = green ? new Color(0.94f, 1.02f, 0.86f) : new Color(1.02f, 0.96f, 0.86f);
        Tube(host, "Yarn Tail", tail0, tail1, radius * 0.04f, new Vector2(1f, 3f), tail, green ? GreenTex : YarnTex, 1.2f, 0.14f, 0.55f, 0.9f, 0.12f);
        return root;
    }

    /// library/rotate-goal.mp4. Fluted vertical tan felt post with narrower fluted upper shaft on a square stitched felt mat.
    public static GameObject GoalPost(Vector3 pos)
    {
        var root = Root("Goal Marker", pos, 1f);
        root.isStatic = false;
        var host = root.transform;

        Color matCream = new Color(0.90f, 0.84f, 0.74f);
        Color borderCream = new Color(0.94f, 0.88f, 0.78f);
        Color tanPost = new Color(0.90f, 0.70f, 0.44f);
        Color tanCap = new Color(0.94f, 0.76f, 0.52f);

        // Square stitched felt mat on the burlap
        const float matW = 0.64f;
        const float matH = 0.014f;
        var matPad = Mesh(host, Cloth(matW, matH, matW, 2.2f, 2.2f), "Goal Mat", new Vector3(0f, matH * 0.5f, 0f), Quaternion.identity, Vector3.one);
        matPad.isStatic = false;
        Paint(matPad, BurlapTex, new Color(0.96f, 0.90f, 0.80f), Vector2.one, 0f, 0.02f, 0.70f, 0.98f, 0f);

        float half = matW * 0.5f;
        const float borderW = 0.038f;
        const float borderH = 0.022f;
        Vector3[] borderPos =
        {
            new Vector3(0f, borderH * 0.5f, -half),
            new Vector3(0f, borderH * 0.5f,  half),
            new Vector3(-half, borderH * 0.5f, 0f),
            new Vector3( half, borderH * 0.5f, 0f)
        };
        Vector3[] borderScale =
        {
            new Vector3(matW + borderW, borderH, borderW),
            new Vector3(matW + borderW, borderH, borderW),
            new Vector3(borderW, borderH, matW - borderW),
            new Vector3(borderW, borderH, matW - borderW)
        };
        for (int i = 0; i < 4; i++)
        {
            var rim = Mesh(host, GrainBox(borderScale[i].x, borderScale[i].y, borderScale[i].z, i < 2 ? 0 : 2, 6f),
                "Goal Mat Border", borderPos[i], Quaternion.identity, Vector3.one);
            rim.isStatic = false;
            Paint(rim, CreamTex, i < 2 ? borderCream : matCream, new Vector2(2f, 1f), 0.8f, 0.08f, 0.78f, 0.96f, 0.08f);
        }

        // Lower fluted tan felt cylinder (radius 0.125, height 0.62, 22 vertical felt pleats)
        var lower = Mesh(host, FlutedColumn(0.125f, 0.62f, 22, 0.012f), "Goal Post", new Vector3(0f, 0.014f, 0f), Quaternion.identity, Vector3.one);
        lower.isStatic = false;
        Paint(lower, BrownTex, tanPost, new Vector2(3f, 2.4f), 1.4f, 0.12f, 0.76f, 0.95f, 0.14f);

        // Rounded felt shoulder ring at the top of the lower cylinder
        var shoulder = Mesh(host, Torus(0.088f, 0.036f, 28, 12), "Goal Shoulder", new Vector3(0f, 0.622f, 0f), Quaternion.identity, new Vector3(1f, 0.45f, 1f));
        shoulder.isStatic = false;
        Paint(shoulder, BrownTex, tanCap, new Vector2(2f, 2f), 1.2f, 0.12f, 0.78f, 0.95f, 0.12f);

        // Upper narrower fluted tan felt shaft (radius 0.048, height 0.68, 14 vertical felt pleats)
        var upper = Mesh(host, FlutedColumn(0.048f, 0.68f, 14, 0.006f), "Goal Shaft", new Vector3(0f, 0.62f, 0f), Quaternion.identity, Vector3.one);
        upper.isStatic = false;
        Paint(upper, BrownTex, tanPost, new Vector2(2f, 2.6f), 1.4f, 0.12f, 0.76f, 0.95f, 0.14f);

        return root;
    }

    static Mesh FlutedColumn(float radius, float height, int flutes, float depth)
    {
        string key = string.Format("goal-flute-{0:0.###}-{1:0.###}-{2}-{3:0.###}", radius, height, flutes, depth);
        return Cached(key, () =>
        {
            int su = flutes * 6;
            const int sv = 18;
            int cols = su + 1;
            var v = new List<Vector3>((sv + 1) * cols + 2);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(su * sv * 6 + su * 6);
            for (int iv = 0; iv <= sv; iv++)
            {
                float fv = iv / (float)sv;
                float y = fv * height;
                float bevel = Mathf.Clamp01(fv / 0.05f) * Mathf.Clamp01((1f - fv) / 0.06f);
                float endTaper = Mathf.Lerp(0.88f, 1f, bevel);
                for (int iu = 0; iu <= su; iu++)
                {
                    float u = iu / (float)su;
                    float a = u * Mathf.PI * 2f;
                    float wave = Mathf.Abs(Mathf.Cos(a * flutes * 0.5f));
                    float pleat = Mathf.Pow(wave, 0.55f);
                    float split = 1f - 0.15f * Mathf.Sin(fv * Mathf.PI) * Mathf.Sin(a * 3f);
                    float r = (radius * endTaper) - depth * (1f - pleat) * split;
                    v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    uv.Add(new Vector2(u * 2f, fv * 2f));
                }
            }
            for (int iv = 0; iv < sv; iv++)
                for (int iu = 0; iu < su; iu++)
                {
                    int a = iv * cols + iu;
                    Vector3 outDir = new Vector3(v[a].x, 0f, v[a].z);
                    Face(v, tris, a, a + 1, a + 1 + cols, a + cols, outDir);
                }
            int bot = v.Count;
            v.Add(new Vector3(0f, 0f, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            int top = v.Count;
            v.Add(new Vector3(0f, height, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            int topRow = sv * cols;
            for (int iu = 0; iu < su; iu++)
            {
                Tri(v, tris, bot, iu + 1, iu, Vector3.down);
                Tri(v, tris, top, topRow + iu, topRow + iu + 1, Vector3.up);
            }
            return Build(v, uv, tris, key);
        });
    }

    /// Felt succulent leaf. `taper` 0 keeps a round tip; higher taper points the heart leaves.
    /// Bridge tufts stay on Whorl / FleshyLeaf so the kraft bridge does not change.
    static void SageWhorl(Transform host, int count, float leanDeg, float length, float width, float thick, float yaw0, Color tint, int seed, Vector3 at, float taper)
    {
        float lean = leanDeg * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            float yaw = (yaw0 + (Hash(seed, i) - 0.5f) * 14f) * Mathf.Deg2Rad + (i + 0.5f) / count * Mathf.PI * 2f;
            float stretch = 0.92f + Hash(seed, i + 3) * 0.14f;
            float ry = length * stretch * 0.5f;
            float w = width * (0.94f + Hash(seed, i + 5) * 0.12f);
            var mesh = CraftMesh.Egg(w * 0.5f, ry, thick * 0.5f, taper, 16, 10);
            float leanJ = lean + (Hash(seed, i + 9) - 0.5f) * 0.12f;
            Vector3 aim = new Vector3(Mathf.Cos(yaw) * Mathf.Sin(leanJ), Mathf.Cos(leanJ), Mathf.Sin(yaw) * Mathf.Sin(leanJ));
            if (aim.sqrMagnitude < 1e-6f) aim = Vector3.up;
            aim.Normalize();
            var leaf = Mesh(host, mesh, "Sage Leaf", at + aim * ry + Vector3.up * 0.008f, LeafRot(aim), Vector3.one);
            Paint(leaf, GreenTex, tint, new Vector2(1.5f, 2.8f), 1.2f, 0.36f, 0.66f, 0.94f, 0.14f);
        }
    }

    // ——— small pieces ———

    static void Tuft(Transform host, Vector3 at, float k, int seed)
    {
        Whorl(host, 4, 62f, 0.11f * k, 0.04f * k, 0.024f * k, seed * 17f, SageGreen, seed, at, "Bridge Grass");
        var puff = Mesh(host, CraftMesh.Pompom(0.026f * k, 0.14f, 10, 8, seed), "Bridge Grass",
            at + Vector3.up * (0.02f * k), Quaternion.identity, Vector3.one);
        PaintWool(puff, GreenTex, SageGreen);
    }

    static void Whorl(Transform host, int count, float leanDeg, float length, float width, float thick, float yaw0, Color tint, int seed, Vector3 at, string leafName)
    {
        float lean = leanDeg * Mathf.Deg2Rad;
        for (int i = 0; i < count; i++)
        {
            float yaw = yaw0 * Mathf.Deg2Rad + (i + 0.5f) / count * Mathf.PI * 2f;
            float stretch = 0.92f + Hash(seed, i) * 0.14f;
            FleshyLeaf(host, leafName, at, yaw, lean, length * stretch, width, thick, tint);
        }
    }

    /// Plump felt leaf. The tapered egg tip (+Y) aims outward. The wide base sits at `at`.
    static void FleshyLeaf(Transform host, string name, Vector3 at, float yaw, float lean, float length, float width, float thick, Color tint)
    {
        float ry = length * 0.5f;
        var mesh = CraftMesh.Egg(width * 0.5f, ry, thick * 0.5f, 0.55f, 14, 10);
        Vector3 aim = new Vector3(Mathf.Cos(yaw) * Mathf.Sin(lean), Mathf.Cos(lean), Mathf.Sin(yaw) * Mathf.Sin(lean));
        if (aim.sqrMagnitude < 1e-6f) aim = Vector3.up;
        aim.Normalize();
        var leaf = Mesh(host, mesh, name, at + aim * ry + Vector3.up * 0.012f, LeafRot(aim), Vector3.one);
        PaintWool(leaf, GreenTex, tint);
    }

    static Quaternion LeafRot(Vector3 aim)
    {
        Vector3 side = Vector3.Cross(Vector3.up, aim);
        if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
        side.Normalize();
        return Quaternion.LookRotation(Vector3.Cross(aim, side), aim);
    }

    static void Knob(Transform host, string name, Vector3 localPos, float radius)
    {
        var go = Mesh(host, CraftMesh.Egg(radius, radius, radius, 0f, 14, 10), name, localPos, Quaternion.identity, Vector3.one);
        // rotate-grinder.mp4 knobs are turned wood, warmer than the pale kraft button.
        Paint(go, BrownTex, new Color(1.02f, 0.78f, 0.48f), new Vector2(1.2f, 1.2f), 0.2f, 0.26f, 0.22f, 0.48f, 0.03f);
    }

    static void PaintWood(GameObject go, Color tint)
    {
        // UVs already carry the grain repeat. Tile stays 1 so the lines are not doubled into a blur.
        Paint(go, YarnTex, tint, Vector2.one, 1.45f, 0.05f, 0.46f, 0.9f, 0.04f);
    }

    static void PaintKraft(GameObject go, float bump) => PaintKraft(go, bump, Kraft);

    static void PaintKraft(GameObject go, float bump, Color tint)
    {
        Paint(go, KraftTex, tint, Vector2.one, bump, 0.04f, 0.5f, 0.94f, 0.02f);
    }

    static void PaintWool(GameObject go, string tex, Color tint)
    {
        Paint(go, tex, tint, new Vector2(1.6f, 1.6f), 2.2f, 0.8f, 0.78f, 0.96f, 0.14f);
    }

    static void PaintYarn(GameObject go, bool green, Vector2 tile)
    {
        // rotate-yarn-ball.mp4 is cream knit. The green flag stays the yard's green ball.
        Paint(go, green ? GreenTex : YarnTex, green ? new Color(0.92f, 1.02f, 0.84f) : new Color(1.04f, 0.96f, 0.86f),
            tile, 1.7f, 0.18f, 0.58f, 0.9f, 0.20f);
    }

    // ——— meshes (no colliders) ———

    /// Centred box. `axis` 0/1/2 makes the yarn grooves follow X, Y, or Z. `linesPerM` is how many lines cross one metre.
    static Mesh GrainBox(float sx, float sy, float sz, int axis, float linesPerM)
    {
        string key = string.Format("grain-{0:0.###}-{1:0.###}-{2:0.###}-{3}-{4:0.#}", sx, sy, sz, axis, linesPerM);
        return Cached(key, () =>
        {
            float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;
            var v = new List<Vector3>(24);
            var uv = new List<Vector2>(24);
            var tris = new List<int>(36);
            void Side(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(GrainUv(a, n, axis, linesPerM));
                uv.Add(GrainUv(b, n, axis, linesPerM));
                uv.Add(GrainUv(c, n, axis, linesPerM));
                uv.Add(GrainUv(d, n, axis, linesPerM));
                Face(v, tris, i, i + 1, i + 2, i + 3, n);
            }
            Side(new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.up);
            Side(new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), Vector3.down);
            Side(new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), Vector3.right);
            Side(new Vector3(-hx, -hy, hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), Vector3.left);
            Side(new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.forward);
            Side(new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), Vector3.back);
            return Build(v, uv, tris, key);
        });
    }

    static Vector2 GrainUv(Vector3 p, Vector3 outward, int axis, float linesPerM)
    {
        Vector3 grain = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        Vector3 across = Vector3.Cross(outward, grain);
        if (across.sqrMagnitude < 1e-6f)
        {
            Vector3 u = Mathf.Abs(outward.y) > 0.5f ? Vector3.right : Vector3.up;
            Vector3 w = Vector3.Cross(outward, u).normalized;
            return new Vector2(Vector3.Dot(p, u) * linesPerM, Vector3.Dot(p, w) * linesPerM);
        }
        across.Normalize();
        // Texture grooves follow V, so V is the grain and U is the line repeat.
        return new Vector2(Vector3.Dot(p, across) * linesPerM, Vector3.Dot(p, grain) * 1.6f);
    }

    /// Thin cloth pad, centred. UV repeats are the burlap weave, not wood lines.
    static Mesh Cloth(float sx, float sy, float sz, float tileX, float tileZ)
    {
        string key = string.Format("cloth-{0:0.##}-{1:0.###}-{2:0.##}-{3:0.#}-{4:0.#}", sx, sy, sz, tileX, tileZ);
        return Cached(key, () =>
        {
            float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;
            var v = new List<Vector3>(24);
            var uv = new List<Vector2>(24);
            var tris = new List<int>(36);
            void Side(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(new Vector2((a.x / sx + 0.5f) * tileX, (a.z / sz + 0.5f) * tileZ));
                uv.Add(new Vector2((b.x / sx + 0.5f) * tileX, (b.z / sz + 0.5f) * tileZ));
                uv.Add(new Vector2((c.x / sx + 0.5f) * tileX, (c.z / sz + 0.5f) * tileZ));
                uv.Add(new Vector2((d.x / sx + 0.5f) * tileX, (d.z / sz + 0.5f) * tileZ));
                Face(v, tris, i, i + 1, i + 2, i + 3, n);
            }
            Side(new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.up);
            Side(new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), Vector3.down);
            Side(new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), Vector3.right);
            Side(new Vector3(-hx, -hy, hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(-hx, hy, hz), Vector3.left);
            Side(new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz), Vector3.forward);
            Side(new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), Vector3.back);
            return Build(v, uv, tris, key);
        });
    }

    /// Radius 1, height 1 along Y, centred. Scale (radius, length, radius).
    static Mesh UnitTube(int sides = 16)
    {
        string key = "unit-tube-" + sides;
        return Cached(key, () =>
        {
            int n = Mathf.Max(8, sides);
            int cols = n + 1;
            var v = new List<Vector3>(cols * 2 + 2);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(n * 12);
            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? -0.5f : 0.5f;
                for (int i = 0; i <= n; i++)
                {
                    float u = i / (float)n;
                    float a = u * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a), y, Mathf.Sin(a)));
                    uv.Add(new Vector2(u, ring));
                }
            }
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) / n * Mathf.PI * 2f;
                Face(v, tris, i, i + 1, i + 1 + cols, i + cols, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)));
            }
            int bot = v.Count;
            v.Add(new Vector3(0f, -0.5f, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            int top = v.Count;
            v.Add(new Vector3(0f, 0.5f, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < n; i++)
            {
                Tri(v, tris, bot, i + 1, i, Vector3.down);
                Tri(v, tris, top, cols + i, cols + i + 1, Vector3.up);
            }
            return Build(v, uv, tris, key);
        });
    }

    /// Fluted cardboard box. Bottom at local y = 0, centred on XZ.
    /// `horizontalRidges` is the lid rim. Otherwise the ribs run up the wall.
    static Mesh FlutedBox(float width, float height, float depth, float amp, float pitch, bool horizontalRidges)
    {
        string key = string.Format("flute-{0:0.###}-{1:0.###}-{2:0.###}-{3:0.###}-{4:0.###}-{5}",
            width, height, depth, amp, pitch, horizontalRidges ? 1 : 0);
        return Cached(key, () =>
        {
            var v = new List<Vector3>(900);
            var uv = new List<Vector2>(900);
            var t = new List<int>(1800);
            float hw = width * 0.5f, hd = depth * 0.5f;
            Wall(v, uv, t, new Vector3(-hw, 0f, -hd), Vector3.right, Vector3.up, width, height, pitch, amp, Vector3.back, horizontalRidges);
            Wall(v, uv, t, new Vector3(hw, 0f, hd), Vector3.left, Vector3.up, width, height, pitch, amp, Vector3.forward, horizontalRidges);
            Wall(v, uv, t, new Vector3(-hw, 0f, hd), Vector3.back, Vector3.up, depth, height, pitch, amp, Vector3.left, horizontalRidges);
            Wall(v, uv, t, new Vector3(hw, 0f, -hd), Vector3.forward, Vector3.up, depth, height, pitch, amp, Vector3.right, horizontalRidges);
            Cap(v, uv, t, hw, hd, 0.001f, Vector3.down);
            Cap(v, uv, t, hw, hd, Mathf.Max(0.002f, height - 0.001f), Vector3.up);
            return Build(v, uv, t, key);
        });
    }

    static void Wall(List<Vector3> v, List<Vector2> uv, List<int> tris, Vector3 origin, Vector3 axisU, Vector3 axisV,
        float lenU, float lenV, float pitch, float amp, Vector3 outward, bool horizontalRidges)
    {
        pitch = Mathf.Max(0.01f, pitch);
        int wavesU = Mathf.Max(2, Mathf.RoundToInt(lenU / pitch));
        int wavesV = Mathf.Max(2, Mathf.RoundToInt(lenV / pitch));
        int nu = horizontalRidges ? 2 : wavesU * 4;
        int nv = horizontalRidges ? wavesV * 4 : 2;
        int start = v.Count;
        for (int iy = 0; iy <= nv; iy++)
        {
            float fv = iy / (float)nv;
            for (int ix = 0; ix <= nu; ix++)
            {
                float fu = ix / (float)nu;
                float wave = horizontalRidges
                    ? Mathf.Sin(fv * wavesV * Mathf.PI * 2f)
                    : Mathf.Sin(fu * wavesU * Mathf.PI * 2f);
                v.Add(origin + axisU * (fu * lenU) + axisV * (fv * lenV) + outward * (wave * amp));
                uv.Add(horizontalRidges
                    ? new Vector2(fv * wavesV * 0.45f, fu)
                    : new Vector2(fu * wavesU * 0.35f, fv * 1.3f));
            }
        }
        int cols = nu + 1;
        for (int iy = 0; iy < nv; iy++)
            for (int ix = 0; ix < nu; ix++)
            {
                int a = start + iy * cols + ix;
                Face(v, tris, a, a + 1, a + 1 + cols, a + cols, outward);
            }
    }

    static void Cap(List<Vector3> v, List<Vector2> uv, List<int> tris, float hw, float hd, float y, Vector3 outward)
    {
        int a = v.Count;
        v.Add(new Vector3(-hw, y, -hd)); uv.Add(new Vector2(0f, 0f));
        v.Add(new Vector3(hw, y, -hd)); uv.Add(new Vector2(2f, 0f));
        v.Add(new Vector3(hw, y, hd)); uv.Add(new Vector2(2f, 2f));
        v.Add(new Vector3(-hw, y, hd)); uv.Add(new Vector2(0f, 2f));
        Face(v, tris, a, a + 1, a + 2, a + 3, outward);
    }

    /// Wavy cardboard edge, centred. Long axis is local Y, wave is local Z.
    static Mesh WavyStrip(float length, float width, float amp, int waves)
    {
        string key = string.Format("edge-{0:0.###}-{1:0.###}-{2:0.###}-{3}", length, width, amp, waves);
        return Cached(key, () =>
        {
            int nv = Mathf.Max(8, waves * 4);
            var v = new List<Vector3>((nv + 1) * 4);
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(nv * 12);
            const float thick = 0.008f;
            for (int side = 0; side < 2; side++)
            {
                float zOff = side == 0 ? 0f : -thick;
                Vector3 n = side == 0 ? Vector3.forward : Vector3.back;
                int start = v.Count;
                for (int iy = 0; iy <= nv; iy++)
                {
                    float fv = iy / (float)nv;
                    float wave = Mathf.Sin(fv * waves * Mathf.PI * 2f) * amp;
                    for (int ix = 0; ix <= 1; ix++)
                    {
                        v.Add(new Vector3((ix - 0.5f) * width, (fv - 0.5f) * length, wave + zOff));
                        uv.Add(new Vector2(ix, fv * waves * 0.35f));
                    }
                }
                for (int iy = 0; iy < nv; iy++)
                {
                    int a = start + iy * 2;
                    Face(v, tris, a, a + 2, a + 3, a + 1, n);
                }
            }
            return Build(v, uv, tris, key);
        });
    }

    /// Ring in the XZ plane. UV V runs along the strand.
    static Mesh Torus(float major, float minor, int segU, int segV)
    {
        string key = string.Format("torus-{0:0.###}-{1:0.###}-{2}-{3}", major, minor, segU, segV);
        return Cached(key, () =>
        {
            int su = Mathf.Max(8, segU), sv = Mathf.Max(6, segV), cols = su + 1;
            var v = new List<Vector3>((su + 1) * (sv + 1));
            var uv = new List<Vector2>(v.Capacity);
            var tris = new List<int>(su * sv * 6);
            for (int iv = 0; iv <= sv; iv++)
            {
                float vAng = iv / (float)sv * Mathf.PI * 2f;
                float cv = Mathf.Cos(vAng), svv = Mathf.Sin(vAng);
                for (int iu = 0; iu <= su; iu++)
                {
                    float uAng = iu / (float)su * Mathf.PI * 2f;
                    float cu = Mathf.Cos(uAng), suu = Mathf.Sin(uAng);
                    float r = major + minor * cv;
                    v.Add(new Vector3(cu * r, svv * minor, suu * r));
                    uv.Add(new Vector2(iv / (float)sv, iu / (float)su * 5f));
                }
            }
            for (int iv = 0; iv < sv; iv++)
                for (int iu = 0; iu < su; iu++)
                {
                    int a = iv * cols + iu;
                    Vector3 ring = new Vector3(v[a].x, 0f, v[a].z);
                    Vector3 outward = v[a] - (ring.sqrMagnitude > 1e-6f ? ring.normalized * major : Vector3.zero);
                    Face(v, tris, a, a + 1, a + 1 + cols, a + cols, outward);
                }
            return Build(v, uv, tris, key);
        });
    }

    static void Face(List<Vector3> v, List<int> tris, int a, int b, int c, int d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }
        else
        {
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(a); tris.Add(d); tris.Add(c);
        }
    }

    static void Tri(List<Vector3> v, List<int> tris, int a, int b, int c, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f) { tris.Add(a); tris.Add(b); tris.Add(c); }
        else { tris.Add(a); tris.Add(c); tris.Add(b); }
    }

    static Mesh Build(List<Vector3> v, List<Vector2> uv, List<int> tris, string name)
    {
        var mesh = new Mesh { name = name };
        if (v.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Mesh Cached(string key, System.Func<Mesh> build)
    {
        if (Meshes.TryGetValue(key, out var found) && found != null) return found;
        var mesh = build();
        Meshes[key] = mesh;
        return mesh;
    }

    static float Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)b * 668265263u + 0x9E3779B9u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 65535f;
        }
    }

    static GameObject Root(string name, Vector3 pos, float scale)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.isStatic = true;
        return go;
    }

    static GameObject Mesh(Transform parent, Mesh mesh, string name, Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        go.isStatic = true;
        return go;
    }

    /// Round wood or kraft bar. Mesh radius is 1 and height is 1, so scale is (radius, length, radius). No collider.
    static GameObject Tube(Transform parent, string name, Vector3 a, Vector3 b, float radius, Vector2 tile, Color tint)
    {
        return Tube(parent, name, a, b, radius, tile, tint, YarnTex, 1.35f, 0.05f, 0.46f, 0.9f, 0.04f);
    }

    static GameObject Tube(Transform parent, string name, Vector3 a, Vector3 b, float radius, Vector2 tile, Color tint,
        string tex, float bump, float sheen, float wrap, float rough, float fiber)
    {
        Vector3 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.0005f || radius <= 0f) return null;
        var go = Mesh(parent, UnitTube(), name, (a + b) * 0.5f,
            Quaternion.FromToRotation(Vector3.up, delta / len),
            new Vector3(radius, len, radius));
        Paint(go, tex, tint, tile, bump, sheen, wrap, rough, fiber);
        return go;
    }

    /// Escape/FeltFiber via CraftMesh.Felt. `resource` is a Resources path such as Look/tex-yarn-fiber.
    static void Paint(GameObject go, string resource, Color tint, Vector2 tile, float bump, float sheen, float wrap, float rough, float fiber)
    {
        if (go == null) return;
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        string key = "FeltSet|" + CraftMesh.Key(resource, tint, tile, bump, sheen, wrap, rough, 0f) + "|f" + fiber.ToString("0.###");
        rend.sharedMaterial = CraftMesh.Shared(key, () =>
        {
            var tex = Resources.Load<Texture2D>(resource);
            var mat = CraftMesh.Felt(tex, tint, tile, bump, sheen, wrap, rough);
            if (mat != null && mat.HasProperty("_FiberAmt")) mat.SetFloat("_FiberAmt", fiber);
            return mat;
        });
    }
}

using UnityEngine;

/// Opening diorama. The prop builders live in Props/; this only places them
/// when the scene still has the old cubes. Existing Pine / tree roots stay put.
/// The ground colliders, hop gaps, and BeanImpulse stay as authored.
public static class OpeningYard
{
    public static void Keep()
    {
        HideBakedKraft();
        HideLoosePines();
        if (!Any("Kraft Backdrop"))
        {
            // Floor point of the old rib row (cubes were centred at y 2.5, z 8.4).
            var wall = FeltWall.Build(new Vector3(0f, 0f, 8.4f));
            var backdrop = wall.AddComponent<BackdropFollow>();
            var beanNow = GameObject.Find("Imagine Bean");
            if (beanNow != null) backdrop.target = beanNow.transform;
        }

        if (!Any("Pine"))
        {
            FeltPine.Build(new Vector3(-2.05f, 0f, 1.55f), 0.95f);
            FeltPine.Build(new Vector3(-1.05f, 0f, 1.15f), 1.08f);
            FeltPine.Build(new Vector3(2.25f, 0f, 1.40f), 0.62f);
        }
        if (!Any("Red Tree"))
            FeltRedTree.Build(new Vector3(1.25f, 0f, 1.25f), 1.08f);
        if (!Any("Flower Tree"))
            FeltFlowerTree.Build(new Vector3(12.40f, 0f, 1.70f), 0.90f);
        if (!Any("Dotted Tree"))
            FeltDottedTree.Build(new Vector3(3.55f, 0f, 2.05f), 1.05f);

        // One short arch ahead of the bean. Rise is 0.30 of the span, so this
        // stays a little guide. The hop gaps and the ground collider are untouched.
        if (!Any("Bean Walkway"))
            FeltWalkway.Build(1.15f, 3f, 0f);

        var bean = GameObject.Find("Imagine Bean");
        if (bean != null && bean.transform.Find("Bean Body/Yarn Seam") == null)
            FeltBeanSurface.Apply(bean);
        var runner = Object.FindAnyObjectByType<LevelRunner>();
        if (runner != null && runner.bean == null && bean != null)
            runner.bean = bean.transform;
        var follow = Object.FindAnyObjectByType<SetCameraFollow>();
        if (follow != null && follow.target == null && bean != null)
            follow.target = bean.transform;
    }

    /// The baked opening wall is numbered cubes (Kraft Rib 0, Kraft Flute).
    /// LevelSetSpawner later adds its own "Kraft Rib" props; those names do not match.
    static void HideBakedKraft()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            string n = t.name;
            bool numberedRib = n.StartsWith("Kraft Rib ") && n.Length > 10 && char.IsDigit(n[10]);
            if (!numberedRib && n != "Kraft Flute" && n != "FloorWall Seam") continue;
            var renderer = t.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }

    /// Old pine flaps and yarn loops are loose in the scene. The new pines
    /// parent their pieces under a root named Pine, so those stay visible.
    static void HideLoosePines()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            string n = t.name;
            if (!n.StartsWith("Pine ") && n != "Pinecone" && !n.StartsWith("Pinecone")) continue;
            bool under = false;
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.name == "Pine") { under = true; break; }
            }
            if (under) continue;
            var renderer = t.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.enabled = false;
        }
    }

    static bool Any(string name)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name == name) return true;
        return false;
    }
}

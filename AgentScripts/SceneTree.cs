using UnityEngine;

public static class SceneTree
{
    public static string Main()
    {
        var sb = new System.Text.StringBuilder();
        Dump("Pine", sb);
        Dump("Red Tree", sb);
        Dump("Cardboard Grinder", sb);
        Dump("Kraft Bridge", sb);
        Dump("Burlap Floor", sb);
        Dump("Burlap Path", sb);
        Dump("Bean Walkway", sb);
        Dump("Moss", sb);
        return sb.ToString();
    }

    static void Dump(string name, System.Text.StringBuilder sb)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name != name) continue;
            n++;
            if (n > 2) continue;
            sb.Append(name).Append(" @").Append(all[i].position.x.ToString("0.0")).Append(" kids=");
            var t = all[i];
            for (int c = 0; c < t.childCount; c++) sb.Append(t.GetChild(c).name).Append(",");
            var r = t.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                var tex = r.sharedMaterial.HasProperty("_BaseMap") ? r.sharedMaterial.GetTexture("_BaseMap") : null;
                sb.Append(" mat=").Append(r.sharedMaterial.name);
                if (tex != null) sb.Append(" tex=").Append(tex.name);
            }
            sb.Append(" | ");
        }
        sb.Append(name).Append(" count=").Append(n).Append("\n");
    }
}

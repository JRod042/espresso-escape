using UnityEngine;

public static class PineTint
{
    public static string Main()
    {
        var tiers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        int n = 0;
        Color c = Color.black;
        string tex = "none";
        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i].name != "Pine Tier") continue;
            var m = tiers[i].sharedMaterial;
            if (m == null || !m.HasProperty("_BaseColor")) continue;
            c = m.GetColor("_BaseColor");
            var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            tex = t != null ? t.name : "none";
            n++;
        }
        return "tiers=" + n + " tint=" + c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00") + " map=" + tex;
    }
}

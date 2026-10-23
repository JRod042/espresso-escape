using System.Text;
using UnityEngine;

public static class SceneProbe
{
    public static string Main()
    {
        var sb = new StringBuilder();
        sb.Append("playing=").Append(Application.isPlaying);
        var cam = Camera.main;
        sb.Append(" cam=").Append(cam != null ? cam.transform.position.ToString() : "null");
        string[] names = {
            "Imagine Bean", "Set Camera", "Kraft Bridge", "Cardboard Grinder",
            "Burlap Floor", "Kraft Wall", "Backdrop", "Pine", "Red Tree", "Cloud"
        };
        foreach (var n in names)
        {
            int c = 0;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
            foreach (var t in all)
            {
                if (t.name != n) continue;
                if (c < 4) sb.Append(" | ").Append(n).Append("@").Append(t.position.ToString("F2"));
                c++;
            }
            sb.Append(" #").Append(n).Append("=").Append(c);
        }
        int grounds = 0, gaps = 0;
        var trs = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (var t in trs)
        {
            if (t.name == "Burlap Ground") grounds++;
            if (t.name.Contains("Gap")) gaps++;
        }
        sb.Append(" grounds=").Append(grounds).Append(" gapNames=").Append(gaps);
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        sb.Append(" canvases=").Append(canvases.Length);
        return sb.ToString();
    }
}

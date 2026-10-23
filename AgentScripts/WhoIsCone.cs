using UnityEngine;

public static class WhoIsCone
{
    public static string Main()
    {
        var sb = new System.Text.StringBuilder();
        var all = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i].transform;
            float x = t.position.x;
            if (x < -4.2f || x > 6.2f) continue;
            if (t.position.y < 0.15f) continue;
            string n = t.name;
            if (n.Contains("Walkway") || n.Contains("Yarn ") || n.Contains("Title")) continue;
            var mesh = all[i].sharedMesh;
            string meshName = mesh != null ? mesh.name : "none";
            int verts = mesh != null ? mesh.vertexCount : 0;
            sb.Append(n).Append(" parent=").Append(t.parent != null ? t.parent.name : "root");
            sb.Append(" x=").Append(x.ToString("0.00"));
            sb.Append(" y=").Append(t.position.y.ToString("0.00"));
            sb.Append(" mesh=").Append(meshName);
            sb.Append(" v=").Append(verts);
            sb.Append("\n");
        }
        return sb.ToString();
    }
}

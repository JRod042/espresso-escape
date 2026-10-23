using System.Text;
using UnityEngine;

public static class ShapeProbe
{
    public static string Main()
    {
        int pineFuzz = 0, pineTiers = 0, pineCone = 0, redPom = 0, sage = 0, moss = 0, yarnBump = 0;
        var sb = new StringBuilder();
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (var t in all)
        {
            if (t.name == "Pine Fuzz") pineFuzz++;
            else if (t.name == "Pine Tiers") pineTiers++;
            else if (t.name == "Pinecone") pineCone++;
            else if (t.name == "Red Pompom") redPom++;
            else if (t.name.StartsWith("Sage")) sage++;
            else if (t.name == "Moss" || t.name == "Moss Halo") moss++;
            else if (t.name == "Yarn Bump") yarnBump++;
        }
        sb.Append("fuzz=").Append(pineFuzz)
          .Append(" tiers=").Append(pineTiers)
          .Append(" pinecone=").Append(pineCone)
          .Append(" redPom=").Append(redPom)
          .Append(" sage=").Append(sage)
          .Append(" moss=").Append(moss)
          .Append(" yarnBump=").Append(yarnBump);
        var red = GameObject.Find("Red Pompom");
        if (red != null)
        {
            var mf = red.GetComponent<MeshFilter>();
            sb.Append(" redMesh=").Append(mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "none");
            sb.Append(" redAt=").Append(red.transform.position.ToString("F2"));
        }
        var fuzz = GameObject.Find("Pine Fuzz");
        if (fuzz != null)
        {
            var mf = fuzz.GetComponent<MeshFilter>();
            sb.Append(" pineMesh=").Append(mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "none");
            sb.Append(" pineAt=").Append(fuzz.transform.position.ToString("F2"));
        }
        return sb.ToString();
    }
}

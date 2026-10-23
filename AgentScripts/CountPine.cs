using UnityEngine;

public static class CountPine
{
    public static string Main()
    {
        int pine = 0, wool = 0, tier = 0, pom = 0, trunk = 0;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n == "Pine" && all[i].parent == null) pine++;
            else if (n == "Pine Wool") wool++;
            else if (n == "Pine Tier") tier++;
            else if (n == "Pompom") pom++;
            else if (n == "Pine Trunk") trunk++;
        }
        return "pines=" + pine + " wool=" + wool + " tiers=" + tier + " trunks=" + trunk + " pompoms=" + pom;
    }
}

using UnityEngine;

public static class CountProps
{
    public static string Main()
    {
        int pine = 0, tier = 0, pom = 0, red = 0, cloud = 0, cord = 0;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n == "Pine" && all[i].parent == null) pine++;
            else if (n == "Pine Tier") tier++;
            else if (n == "Pompom") pom++;
            else if (n == "Red Tree") red++;
            else if (n == "Felt Cloud") cloud++;
            else if (n == "Jute Cord" || n == "Cord" || n == "Felt Cord") cord++;
        }
        return "pine=" + pine + " tiers=" + tier + " pompoms=" + pom + " red=" + red + " clouds=" + cloud + " cordish=" + cord;
    }
}

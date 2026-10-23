using UnityEngine;

public static class CountYard
{
    public static string Main()
    {
        int walk = 0, wall = 0, cord = 0, cloud = 0, title = 0;
        string where = "";
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n == "Bean Walkway") walk++;
            else if (n == "Kraft Backdrop") wall++;
            else if (n == "Jute Cord") cord++;
            else if (n == "Felt Cloud") cloud++;
            else if (n == "Yarn Title")
            {
                title++;
                where = all[i].position.ToString();
            }
        }
        return "walkways=" + walk + " walls=" + wall + " cords=" + cord + " clouds=" + cloud + " titles=" + title + " at " + where;
    }
}

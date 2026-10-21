using System;
using UnityEngine;

/// 5 high-quality LOOK-craft levels (Jorge scope 2026-09-10 — NOT 100).
/// Each level: dense handmade set, full LOOK materials, weighted Rigidbody skip playable.
[CreateAssetMenu(menuName = "Escape/Level Catalog", fileName = "LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    public const int LevelCount = 5;

    [Serializable]
    public struct LevelDef
    {
        public int id;           // 1..5
        public string title;
        public string pack;
        public string blurb;     // search/menu subtitle
        public float goalZ;
        public float obstacleDensity;
        public float gravityScale;
        public int seed;
        public bool bossBeat;
    }

    public LevelDef[] levels = new LevelDef[LevelCount];

    public static LevelDef Make(int id)
    {
        // Five dense LOOK-craft stages — quality > quantity
        switch (Mathf.Clamp(id, 1, 5))
        {
            case 1:
                return new LevelDef
                {
                    id = 1, title = "Café Yard", pack = "Yard",
                    blurb = "Burlap path, moss, roast beans — learn the hop",
                    goalZ = 14f, obstacleDensity = 0.35f, gravityScale = 1f, seed = 680901, bossBeat = false
                };
            case 2:
                return new LevelDef
                {
                    id = 2, title = "Kraft Bridge", pack = "Bridge",
                    blurb = "Arched corrugated bridge + yarn pines",
                    goalZ = 22f, obstacleDensity = 0.50f, gravityScale = 1.02f, seed = 680902, bossBeat = false
                };
            case 3:
                return new LevelDef
                {
                    id = 3, title = "Mill Path", pack = "Mill",
                    blurb = "Cardboard grinder lane + flower canopy",
                    goalZ = 28f, obstacleDensity = 0.62f, gravityScale = 1.05f, seed = 680903, bossBeat = false
                };
            case 4:
                return new LevelDef
                {
                    id = 4, title = "Night Roast", pack = "Roast",
                    blurb = "Warm bounce light, denser yarn orbs",
                    goalZ = 34f, obstacleDensity = 0.74f, gravityScale = 1.08f, seed = 680904, bossBeat = false
                };
            default:
                return new LevelDef
                {
                    id = 5, title = "Finale Escape", pack = "Finale",
                    blurb = "Full LOOK set density — stranger should feel Imagine",
                    goalZ = 42f, obstacleDensity = 0.88f, gravityScale = 1.10f, seed = 680905, bossBeat = true
                };
        }
    }

    public void RebuildAll()
    {
        levels = new LevelDef[LevelCount];
        for (int i = 0; i < LevelCount; i++)
            levels[i] = Make(i + 1);
    }

    public LevelDef Get(int id)
    {
        if (levels == null || levels.Length != LevelCount) RebuildAll();
        id = Mathf.Clamp(id, 1, LevelCount);
        return levels[id - 1];
    }

    public LevelDef[] Search(string query)
    {
        if (levels == null || levels.Length != LevelCount) RebuildAll();
        if (string.IsNullOrWhiteSpace(query)) return (LevelDef[])levels.Clone();
        query = query.Trim().ToLowerInvariant();
        var list = new System.Collections.Generic.List<LevelDef>();
        // PackChips: exact pack match first (Night chip → "Roast" must not hit L1 blurb "roast beans")
        bool exactPack = false;
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].pack != null && levels[i].pack.ToLowerInvariant() == query)
            {
                exactPack = true;
                break;
            }
        }
        if (exactPack)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                var L = levels[i];
                if (L.pack != null && L.pack.ToLowerInvariant() == query)
                    list.Add(L);
            }
            return list.ToArray();
        }
        for (int i = 0; i < levels.Length; i++)
        {
            var L = levels[i];
            if (L.title.ToLowerInvariant().Contains(query)
                || L.pack.ToLowerInvariant().Contains(query)
                || (L.blurb != null && L.blurb.ToLowerInvariant().Contains(query))
                || L.id.ToString() == query)
                list.Add(L);
        }
        return list.ToArray();
    }
}

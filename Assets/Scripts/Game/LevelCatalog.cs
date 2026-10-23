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
        // Five fixed LOOK-craft stages. obstacleDensity is menu metadata only —
        // LevelSetSpawner does not lerp prop counts from it (L1–L3 are 1346 / 5220 / 5370).
        // gravityScale stays 1 on every level so play gravity is −22 and airtime stays ~0.52 s.
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
                    blurb = "Arched kraft bridge sitting on the burlap",
                    goalZ = 22f, obstacleDensity = 0.50f, gravityScale = 1f, seed = 680902, bossBeat = false
                };
            case 3:
                return new LevelDef
                {
                    id = 3, title = "Mill Path", pack = "Mill",
                    blurb = "Kraft steps and wider hops along the grinder lane",
                    goalZ = 28f, obstacleDensity = 0.62f, gravityScale = 1f, seed = 680903, bossBeat = false
                };
            case 4:
                return new LevelDef
                {
                    id = 4, title = "Night Roast", pack = "Roast",
                    blurb = "Elevated kraft blocks and a yarn cluster",
                    goalZ = 34f, obstacleDensity = 0.74f, gravityScale = 1f, seed = 680904, bossBeat = false
                };
            default:
                return new LevelDef
                {
                    id = 5, title = "Finale Escape", pack = "Finale",
                    blurb = "Long set tour — yard, bridge, steps, yarn, goal",
                    goalZ = 48f, obstacleDensity = 0.88f, gravityScale = 1f, seed = 680905, bossBeat = true
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

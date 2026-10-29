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
        public float parSeconds; // third star: clear at or under this time
        public Color accent;     // art direction 2026-10-28: one accent per level, primary button only
        public string mood;      // level-select subtitle for the lighting mood
    }

    /// Art-direction accents (ops/2026-10-28-escape-art-direction.md, Levels table).
    public static readonly Color AccentL1 = new Color32(0xB5, 0x47, 0x3A, 0xFF); // felt red
    public static readonly Color AccentL2 = new Color32(0xC4, 0x6A, 0x2B, 0xFF); // burnt orange
    public static readonly Color AccentL3 = new Color32(0x6E, 0x7F, 0x4A, 0xFF); // moss
    public static readonly Color AccentL4 = new Color32(0xB0, 0x7A, 0x45, 0xFF); // tungsten
    public static readonly Color AccentL5 = new Color32(0x2E, 0x3A, 0x5C, 0xFF); // night blue
    public static readonly Color GoldL5 = new Color32(0xE2, 0xB3, 0x5A, 0xFF);   // L5 secondary gold (stars, boss banner)

    public LevelDef[] levels = new LevelDef[LevelCount];

    public static LevelDef Make(int id)
    {
        // Five fixed LOOK-craft stages. obstacleDensity is menu metadata only —
        // LevelSetSpawner does not lerp prop counts from it (L1–L3 are 1346 / 5220 / 5370).
        // gravityScale rises 1.00 → 1.04 (LevelRunner applies −22 × scale and scales the hop by √scale so the apex
        // holds; L5 air time 0.515 s still clears the 1.42 m gaps). Kept small on purpose — see LevelRunner.
        switch (Mathf.Clamp(id, 1, 5))
        {
            case 1:
                return new LevelDef
                {
                    id = 1, title = "Café Yard", pack = "Yard",
                    blurb = "Burlap path, moss, roast beans — learn the hop",
                    goalZ = 14f, obstacleDensity = 0.35f, gravityScale = 1.00f, seed = 680901, bossBeat = false,
                    parSeconds = 13f, accent = AccentL1, mood = "Warm morning"
                };
            case 2:
                return new LevelDef
                {
                    id = 2, title = "Kraft Bridge", pack = "Bridge",
                    blurb = "Arched kraft bridge sitting on the burlap",
                    goalZ = 22f, obstacleDensity = 0.50f, gravityScale = 1.01f, seed = 680902, bossBeat = false,
                    parSeconds = 19f, accent = AccentL2, mood = "Amber afternoon"
                };
            case 3:
                return new LevelDef
                {
                    id = 3, title = "Mill Path", pack = "Mill",
                    blurb = "Kraft steps and wider hops along the grinder lane",
                    goalZ = 28f, obstacleDensity = 0.62f, gravityScale = 1.02f, seed = 680903, bossBeat = false,
                    parSeconds = 23f, accent = AccentL3, mood = "Cool dappled light"
                };
            case 4:
                return new LevelDef
                {
                    id = 4, title = "Night Roast", pack = "Roast",
                    blurb = "Kraft slabs, steam vents and a felt lift",
                    goalZ = 34f, obstacleDensity = 0.74f, gravityScale = 1.03f, seed = 680904, bossBeat = false,
                    parSeconds = 28f, accent = AccentL4, mood = "Tungsten night shift"
                };
            default:
                return new LevelDef
                {
                    id = 5, title = "Finale Escape", pack = "Finale",
                    blurb = "The whole set, then outrun the grinder",
                    goalZ = 48f, obstacleDensity = 0.88f, gravityScale = 1.04f, seed = 680905, bossBeat = true,
                    parSeconds = 40f, accent = AccentL5, mood = "Night blue and gold"
                };
        }
    }

    public void RebuildAll()
    {
        levels = new LevelDef[LevelCount];
        for (int i = 0; i < LevelCount; i++)
            levels[i] = Make(i + 1);
    }

    /// True when the serialized array is missing, the wrong size, or holds blank rows (a fresh
    /// CreateInstance / an asset saved before the rows were filled): those rebuild from Make.
    bool NeedsRebuild()
    {
        if (levels == null || levels.Length != LevelCount) return true;
        for (int i = 0; i < levels.Length; i++)
            if (levels[i].id != i + 1 || string.IsNullOrEmpty(levels[i].title) || levels[i].goalZ <= 0f) return true;
        return false;
    }

    public LevelDef Get(int id)
    {
        if (NeedsRebuild()) RebuildAll();
        id = Mathf.Clamp(id, 1, LevelCount);
        return levels[id - 1];
    }

    public LevelDef[] Search(string query)
    {
        if (NeedsRebuild()) RebuildAll();
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
            if ((L.title != null && L.title.ToLowerInvariant().Contains(query))
                || (L.pack != null && L.pack.ToLowerInvariant().Contains(query))
                || (L.blurb != null && L.blurb.ToLowerInvariant().Contains(query))
                || L.id.ToString() == query)
                list.Add(L);
        }
        return list.ToArray();
    }
}

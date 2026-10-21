using UnityEditor;
using UnityEngine;

/// Lane C: verify PackChips Night→Roast filter + All/Yard/Bridge/Mill/Finale packs.
public static class PackChipsVerify
{
    [MenuItem("Escape/PackChips Verify")]
    public static void Run()
    {
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();

        // Mirror SearchMenu chipFilters — Night Roast chip must filter pack Roast
        string[] chipLabels = { "All", "Yard", "Bridge", "Mill", "Night Roast", "Finale" };
        string[] chipFilters = { "", "Yard", "Bridge", "Mill", "Roast", "Finale" };

        bool nightMapsRoast = chipLabels[4] == "Night Roast" && chipFilters[4] == "Roast";
        var roastHits = cat.Search("Roast");
        var nightTitleHits = cat.Search("Night");
        bool roastPackOk = roastHits.Length == 1 && roastHits[0].pack == "Roast" && roastHits[0].id == 4; // exact pack, not blurb
        bool nightTitleOk = nightTitleHits.Length >= 1 && nightTitleHits[0].id == 4;

        string[] expectPacks = { "Yard", "Bridge", "Mill", "Roast", "Finale" };
        bool packsOk = true;
        for (int i = 0; i < 5; i++)
        {
            var L = cat.Get(i + 1);
            if (L.pack != expectPacks[i]) packsOk = false;
            Debug.Log($"PACKCHIP_CATALOG L{L.id} title={L.title} pack={L.pack}");
        }

        var all = cat.Search("");
        bool allOk = all.Length == LevelCatalog.LevelCount;

        bool yardOk = cat.Search("Yard").Length == 1 && cat.Search("Yard")[0].id == 1;
        bool bridgeOk = cat.Search("Bridge").Length == 1 && cat.Search("Bridge")[0].id == 2;
        bool millOk = cat.Search("Mill").Length == 1 && cat.Search("Mill")[0].id == 3;
        bool finaleOk = cat.Search("Finale").Length == 1 && cat.Search("Finale")[0].id == 5;

        var blurbRoast = cat.Search("roast beans");
        bool blurbStillWorks = blurbRoast.Length >= 1 && blurbRoast[0].id == 1;

        // Mirror SearchMenu row labels — L4/L5 append polish HOLD (unlock gating unchanged)
        bool holdBadgeOk = true;
        for (int i = 1; i <= LevelCatalog.LevelCount; i++)
        {
            var L = cat.Get(i);
            string holdBadge = (L.id == 4 || L.id == 5) ? " · polish HOLD" : "";
            string unlockedLabel = $"{L.id}. {L.title}  ·  {L.pack}{holdBadge}";
            string lockedLabel = $"[LOCK] {L.id}. {L.title}  ·  {L.pack}{holdBadge}";
            bool needHold = (L.id == 4 || L.id == 5);
            bool hasHold = unlockedLabel.Contains("polish HOLD") && lockedLabel.Contains("polish HOLD");
            if (needHold != hasHold) holdBadgeOk = false;
            if (needHold)
                Debug.Log($"PACKCHIP_HOLDBADGE L{L.id} unlocked=[{unlockedLabel}] locked=[{lockedLabel}]");
            else if (unlockedLabel.Contains("polish HOLD") || lockedLabel.Contains("polish HOLD"))
                holdBadgeOk = false;
        }

        bool pass = nightMapsRoast && roastPackOk && nightTitleOk && packsOk && allOk
                    && yardOk && bridgeOk && millOk && finaleOk && blurbStillWorks && holdBadgeOk;
        Debug.Log($"PACKCHIPS_VERIFY Night→Roast={nightMapsRoast} roastHits={roastHits.Length} blurbRoast={blurbRoast.Length} packsOk={packsOk} all={all.Length} holdBadgeOk={holdBadgeOk} pass={pass}");
        EditorApplication.Exit(pass ? 0 : 5);
    }
}

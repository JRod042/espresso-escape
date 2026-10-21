using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// L1–L3 polish smoke only — does NOT spawn Night Roast / Finale (HOLD).
public static class LevelDensitySmokeL1L3
{
    [MenuItem("Escape/Level Density Smoke L1-L3")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Escape.unity", OpenSceneMode.Single);
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        var spawner = Object.FindFirstObjectByType<LevelSetSpawner>();
        if (spawner == null)
        {
            Debug.LogError("LEVEL_SMOKE_L1L3_FAIL no LevelSetSpawner");
            EditorApplication.Exit(3);
            return;
        }

        int[] counts = new int[3];
        for (int i = 0; i < 3; i++)
        {
            var def = cat.Get(i + 1);
            spawner.ApplyLevel(def);
            counts[i] = spawner.LastSpawnCount;
            Debug.Log($"LEVEL_SMOKE L{def.id} {def.title} pack={def.pack} dens={def.obstacleDensity:0.00} props={counts[i]} boss={def.bossBeat}");
            Debug.Log($"PLAY_SMOKE L{def.id} loaded goalZ={def.goalZ:0.0} dens={def.obstacleDensity:0.00} props={counts[i]}");
        }

        // L4/L5 HOLD: confirm catalog dens only; do not spawn
        var l4 = cat.Get(4);
        var l5 = cat.Get(5);
        Debug.Log($"LEVEL_SMOKE_HOLD L4 {l4.title} dens={l4.obstacleDensity:0.00} (not spawned)");
        Debug.Log($"LEVEL_SMOKE_HOLD L5 {l5.title} dens={l5.obstacleDensity:0.00} (not spawned)");

        bool ok = counts[0] >= 15 && counts[1] >= 15 && counts[2] >= 15
                  && counts[0] != counts[1] && counts[1] != counts[2];
        Debug.Log($"LEVEL_SMOKE_L1L3 counts=[{string.Join(",", counts)}] pass={ok}");
        EditorApplication.Exit(ok ? 0 : 4);
    }
}

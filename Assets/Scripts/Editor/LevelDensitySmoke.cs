using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Smoke: LevelCatalog == 5 + each HQ level gets DISTINCT LOOK-craft spawn counts.
public static class LevelDensitySmoke
{
    [MenuItem("Escape/Level Density Smoke")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Escape.unity", OpenSceneMode.Single);
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        if (cat.levels == null || cat.levels.Length != LevelCatalog.LevelCount)
        {
            Debug.LogError($"LEVEL_SMOKE_FAIL catalog len={(cat.levels == null ? -1 : cat.levels.Length)} expected={LevelCatalog.LevelCount}");
            EditorApplication.Exit(2);
            return;
        }

        var spawner = Object.FindAnyObjectByType<LevelSetSpawner>();
        if (spawner == null)
        {
            Debug.LogError("LEVEL_SMOKE_FAIL no LevelSetSpawner");
            EditorApplication.Exit(3);
            return;
        }

        int[] counts = new int[LevelCatalog.LevelCount];
        for (int i = 0; i < LevelCatalog.LevelCount; i++)
        {
            var def = cat.Get(i + 1);
            spawner.ApplyLevel(def);
            counts[i] = spawner.LastSpawnCount;
            Debug.Log($"LEVEL_SMOKE L{def.id} {def.title} pack={def.pack} dens={def.obstacleDensity:0.00} props={counts[i]} boss={def.bossBeat}");
        }

        bool rising = counts[0] > 0 && counts[4] > counts[0] && counts[4] > counts[1];
        bool distinct = counts[0] != counts[2] || counts[1] != counts[3];
        bool ok = rising && distinct && counts[0] >= 15 && counts[4] >= 80;
        Debug.Log($"LEVEL_SMOKE catalog=5 counts=[{string.Join(",", counts)}] rising={rising} distinct={distinct} pass={ok}");
        EditorApplication.Exit(ok ? 0 : 4);
    }
}

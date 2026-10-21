using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Batchmode CLI entry points (build + proofs). No look/scene-generation changes.
/// Usage: Unity.exe -batchmode -nographics -quit -projectPath <p> -executeMethod EscapeCLI.BuildWindows -logFile <log>
public static class EscapeCLI
{
    const string Scene = "Assets/Escape.unity";
    const string BundleId = "com.jrod042.espressoescape";

    // ---------- Windows ----------
    public static void BuildWindows()
    {
        try
        {
            Directory.CreateDirectory("Builds/Windows");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = "Builds/Windows/EspressoEscape.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            Finish("WINDOWS", BuildPipeline.BuildPlayer(opts));
        }
        catch (Exception e)
        {
            Debug.LogError("ESCAPE_CLI WINDOWS_BUILD_EXCEPTION " + e);
            EditorApplication.Exit(1);
        }
    }

    // ---------- iOS (Xcode project export only) ----------
    public static void ExportIOS()
    {
        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.Log("ESCAPE_CLI IOS_MODULE_MISSING (iOS Build Support not installed for this editor)");
                EditorApplication.Exit(0);
                return;
            }
            Directory.CreateDirectory("Builds/iOS-Xcode");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = "Builds/iOS-Xcode",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };
            Finish("IOS", BuildPipeline.BuildPlayer(opts));
        }
        catch (Exception e)
        {
            Debug.LogError("ESCAPE_CLI IOS_EXPORT_EXCEPTION " + e);
            EditorApplication.Exit(1);
        }
    }

    static void Finish(string tag, BuildReport report)
    {
        var s = report.summary;
        Debug.Log($"ESCAPE_CLI {tag}_BUILD result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} size={s.totalSize} time={s.totalTime} out={s.outputPath}");
        if (s.result != BuildResult.Succeeded)
        {
            foreach (var step in report.steps)
                foreach (var m in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception))
                    Debug.LogError($"ESCAPE_CLI {tag}_BUILD_ERROR [{step.name}] {m.content}");
            EditorApplication.Exit(1);
            return;
        }
        EditorApplication.Exit(0);
    }

    // ---------- Proofs ----------
    // The existing proof classes each call EditorApplication.Exit, so they cannot be chained.
    // This mirrors their checks (same inputs/thresholds) and prints one summary per proof.
    public static void RunProofs()
    {
        bool phys = false, chips = false, smoke = false;
        try { phys = Physics_(); } catch (Exception e) { Debug.LogError("PROOF_PHYSICS EXCEPTION " + e); }
        try { chips = PackChips_(); } catch (Exception e) { Debug.LogError("PROOF_PACKCHIPS EXCEPTION " + e); }
        try { smoke = SmokeL1L3_(); } catch (Exception e) { Debug.LogError("PROOF_SMOKE_L1L3 EXCEPTION " + e); }
        bool all = phys && chips && smoke;
        Debug.Log($"PROOF_SUMMARY physics={phys} packchips={chips} smokeL1L3={smoke} all={all}  (baseline Sep17: physics 1.038, PackChips GREEN, L1-L3 [1346,5220,5370] GREEN)");
        EditorApplication.Exit(all ? 0 : 6);
    }

    static bool Physics_()
    {
        EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var bean = GameObject.Find("Imagine Bean");
        if (bean == null) { Debug.LogError("PROOF_PHYSICS FAIL no Imagine Bean"); return false; }
        var rb = bean.GetComponent<Rigidbody>();
        var impulse = bean.GetComponent<BeanImpulse>();
        Physics.gravity = new Vector3(0f, -22f, 0f);
        Time.fixedDeltaTime = 0.02f;
        var prevMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        float a1 = Apex(bean, rb, impulse);
        float a2 = Apex(bean, rb, impulse);
        Physics.simulationMode = prevMode;
        float delta = Mathf.Abs(a1 - a2);
        bool pass = a1 > 0.35f && delta < 0.1f;
        Debug.Log($"PROOF_PHYSICS apex1={a1:F3} apex2={a2:F3} delta={delta:F3} pass={pass} (baseline 1.038)");
        return pass;
    }

    static float Apex(GameObject bean, Rigidbody rb, BeanImpulse impulse)
    {
        bean.transform.position = new Vector3(0f, 1.2f, 0.35f);
        bean.transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        float hop = impulse != null ? impulse.hopImpulse : 6.4f;
        float fwd = impulse != null ? impulse.forwardImpulse : 5.2f;
        rb.AddForce(Vector3.forward * fwd + Vector3.up * hop, ForceMode.Impulse);
        float startY = bean.transform.position.y, peak = startY;
        for (int i = 0; i < 120; i++)
        {
            Physics.Simulate(0.02f);
            if (bean.transform.position.y > peak) peak = bean.transform.position.y;
        }
        return peak - startY;
    }

    static bool PackChips_()
    {
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        var roast = cat.Search("Roast");
        var night = cat.Search("Night");
        bool roastOk = roast.Length == 1 && roast[0].pack == "Roast" && roast[0].id == 4;
        bool nightOk = night.Length >= 1 && night[0].id == 4;
        string[] expect = { "Yard", "Bridge", "Mill", "Roast", "Finale" };
        bool packsOk = true;
        for (int i = 0; i < 5; i++) if (cat.Get(i + 1).pack != expect[i]) packsOk = false;
        var all = cat.Search("");
        bool allOk = all.Length == LevelCatalog.LevelCount;
        bool singles = cat.Search("Yard").Length == 1 && cat.Search("Yard")[0].id == 1
                    && cat.Search("Bridge").Length == 1 && cat.Search("Bridge")[0].id == 2
                    && cat.Search("Mill").Length == 1 && cat.Search("Mill")[0].id == 3
                    && cat.Search("Finale").Length == 1 && cat.Search("Finale")[0].id == 5;
        var blurb = cat.Search("roast beans");
        bool blurbOk = blurb.Length >= 1 && blurb[0].id == 1;
        bool pass = roastOk && nightOk && packsOk && allOk && singles && blurbOk;
        Debug.Log($"PROOF_PACKCHIPS roastHits={roast.Length} nightOk={nightOk} packsOk={packsOk} all={all.Length} singles={singles} blurbRoast={blurb.Length} pass={pass} (baseline GREEN)");
        return pass;
    }

    static bool SmokeL1L3_()
    {
        EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var cat = ScriptableObject.CreateInstance<LevelCatalog>();
        cat.RebuildAll();
        var spawner = UnityEngine.Object.FindFirstObjectByType<LevelSetSpawner>();
        if (spawner == null) { Debug.LogError("PROOF_SMOKE_L1L3 FAIL no LevelSetSpawner"); return false; }
        int[] c = new int[3];
        for (int i = 0; i < 3; i++)
        {
            var def = cat.Get(i + 1);
            spawner.ApplyLevel(def);
            c[i] = spawner.LastSpawnCount;
            Debug.Log($"PROOF_SMOKE_L1L3 L{def.id} {def.title} dens={def.obstacleDensity:0.00} props={c[i]}");
        }
        bool ok = c[0] >= 15 && c[1] >= 15 && c[2] >= 15 && c[0] != c[1] && c[1] != c[2];
        Debug.Log($"PROOF_SMOKE_L1L3 counts=[{string.Join(",", c)}] pass={ok} (baseline [1346,5220,5370])");
        return ok;
    }
}

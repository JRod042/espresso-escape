using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Batch 4, one-shot: adds URP Screen Space Ambient Occlusion to Assets/Settings/EscapeURP_Renderer.asset
/// (285's soft contact darkening under felt props). Mobile settings: downsampled, low samples, small radius.
/// Also assigns URP's default PostProcessData: it was null, and URP 17 then disables ALL post-processing
/// (UniversalRenderer: postProcessEnabled = data.postProcessData != null), so the EscapeLook DOF/grade/vignette never rendered.
/// Run in batchmode: -executeMethod LookRendererSetup.Run. Idempotent.
public static class LookRendererSetup
{
    public const string RendererPath = "Assets/Settings/EscapeURP_Renderer.asset";
    public const string DefaultPostPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

    public static void Run()
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (data == null) { Debug.Log("[LookRendererSetup] renderer not found"); EditorApplication.Exit(3); return; }
        if (data.postProcessData == null)
        {
            var pp = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostPath);
            if (pp == null) { Debug.Log("[LookRendererSetup] default PostProcessData not found"); EditorApplication.Exit(4); return; }
            data.postProcessData = pp;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[LookRendererSetup] postProcessData assigned " + AssetDatabase.GetAssetPath(pp));
        }
        else Debug.Log("[LookRendererSetup] postProcessData already set");
        AddSsao();
    }

    public static void AddSsao()
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (data == null) { Debug.Log("[LookRendererSetup] renderer not found"); EditorApplication.Exit(3); return; }
        foreach (var f in data.rendererFeatures)
            if (f is ScreenSpaceAmbientOcclusion) { Debug.Log("[LookRendererSetup] SSAO already present"); EditorApplication.Exit(0); return; }

        var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
        ssao.name = "Felt SSAO";
        AssetDatabase.AddObjectToAsset(ssao, data);
        data.rendererFeatures.Add(ssao);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out string _, out long id);
        var so = new SerializedObject(data);
        var map = so.FindProperty("m_RendererFeatureMap");
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();

        var fs = new SerializedObject(ssao);
        Set(fs, "m_Settings.Intensity", 0.9f);
        Set(fs, "m_Settings.DirectLightingStrength", 0.25f);
        Set(fs, "m_Settings.Radius", 0.18f);
        Set(fs, "m_Settings.Falloff", 6f);
        SetB(fs, "m_Settings.Downsample", true);
        SetB(fs, "m_Settings.AfterOpaque", false);
        SetI(fs, "m_Settings.Samples", 2);      // Low
        SetI(fs, "m_Settings.BlurQuality", 2);  // Low
        SetI(fs, "m_Settings.AOMethod", 0);     // Blue noise
        fs.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(ssao);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LookRendererSetup] SSAO added id {id}, features {data.rendererFeatures.Count}");
        EditorApplication.Exit(0);
    }

    static void Set(SerializedObject so, string p, float v) { var x = so.FindProperty(p); if (x != null) x.floatValue = v; else Debug.Log("[LookRendererSetup] missing " + p); }
    static void SetB(SerializedObject so, string p, bool v) { var x = so.FindProperty(p); if (x != null) x.boolValue = v; else Debug.Log("[LookRendererSetup] missing " + p); }
    static void SetI(SerializedObject so, string p, int v) { var x = so.FindProperty(p); if (x != null) x.intValue = v; else Debug.Log("[LookRendererSetup] missing " + p); }
}

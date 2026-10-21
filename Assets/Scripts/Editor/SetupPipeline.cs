using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// URP for the felt diorama (Built-in RP is deprecated in 6000.6).
/// Creates Assets/Settings/EscapeURP.asset + EscapeURP_Renderer.asset once, then assigns the asset to
/// GraphicsSettings.defaultRenderPipeline and every quality level. Soft shadows, 30 m shadow distance,
/// MSAA 4x, HDR off, SRP Batcher on, per-pixel additional lights (Bean Soft Key / Fill point lights HELD).
public static class SetupPipeline
{
    public const string AssetPath = "Assets/Settings/EscapeURP.asset";
    public const string RendererPath = "Assets/Settings/EscapeURP_Renderer.asset";

    [MenuItem("Escape/Setup URP")]
    public static void Setup()
    {
        Ensure();
        Debug.Log("URP_SETUP_OK " + AssetPath);
    }

    public static UniversalRenderPipelineAsset Ensure()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = "EscapeURP_Renderer";
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }
        renderer.renderingMode = RenderingMode.Forward;
        renderer.depthPrimingMode = DepthPrimingMode.Disabled;
        renderer.shadowTransparentReceive = true;
        EditorUtility.SetDirty(renderer);

        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
        if (asset == null)
        {
            asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.name = "EscapeURP";
            AssetDatabase.CreateAsset(asset, AssetPath);
        }

        // public setters
        asset.supportsHDR = false;
        asset.msaaSampleCount = 4;
        asset.shadowDistance = 30f;
        asset.shadowCascadeCount = 2;
        asset.mainLightShadowmapResolution = 2048;
        asset.maxAdditionalLightsCount = 4;
        asset.useSRPBatcher = true;
        asset.supportsCameraDepthTexture = false;
        asset.supportsCameraOpaqueTexture = false;
        asset.renderScale = 1f;
        asset.shadowDepthBias = 1.0f;
        asset.shadowNormalBias = 0.6f;
        asset.cascadeBorder = 0.1f;

        // internal setters → serialized fields
        var so = new SerializedObject(asset);
        Set(so, "m_MainLightShadowsSupported", true);
        Set(so, "m_SoftShadowsSupported", true);
        Set(so, "m_AdditionalLightShadowsSupported", false);
        Set(so, "m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel);
        Set(so, "m_MainLightRenderingMode", (int)LightRenderingMode.PerPixel);
        Set(so, "m_SupportsLightLayers", false);
        var list = so.FindProperty("m_RendererDataList");
        if (list != null)
        {
            if (list.arraySize == 0) list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        GraphicsSettings.defaultRenderPipeline = asset;
        string[] names = QualitySettings.names;
        int prev = QualitySettings.GetQualityLevel();
        for (int i = 0; i < names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(prev, false);
        AssetDatabase.SaveAssets();
        return asset;
    }

    static void Set(SerializedObject so, string name, bool v)
    {
        var p = so.FindProperty(name);
        if (p != null) p.boolValue = v;
    }

    static void Set(SerializedObject so, string name, int v)
    {
        var p = so.FindProperty(name);
        if (p != null) p.intValue = v; // intValue sets the underlying value for enum properties too
    }
}

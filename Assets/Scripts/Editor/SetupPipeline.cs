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
        // 285.mp4 / set plates: the key light is a soft box — ground shadows are wide blurs, not crisp ellipses.
        // One cascade at 1024 over 18 m + high-quality PCF gives a ~4 cm penumbra at bean range.
        asset.shadowDistance = 18f;
        asset.shadowCascadeCount = 1;
        asset.mainLightShadowmapResolution = 1024;
        asset.maxAdditionalLightsCount = 4;
        asset.useSRPBatcher = true;
        asset.supportsCameraDepthTexture = true; // depth of field (plates have shallow focus on the bean)
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
        Set(so, "m_SoftShadowQuality", (int)SoftShadowQuality.High);
        var list = so.FindProperty("m_RendererDataList");
        if (list != null)
        {
            if (list.arraySize == 0) list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);

        GraphicsSettings.defaultRenderPipeline = asset;
        EnsureLookVolume();
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

    public const string LookVolumePath = "Assets/Settings/EscapeLook.asset";

    /// Default volume profile = the plates' camera: shallow focus on the bean (kraft wall and foreground
    /// dressing go soft), warm low-contrast grade, gentle vignette. Assigned as the URP default so no scene
    /// object is needed; the camera only has to render post-processing.
    static void EnsureLookVolume()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LookVolumePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "EscapeLook";
            AssetDatabase.CreateAsset(profile, LookVolumePath);
        }

        // Bokeh aperture is clamped to f/1, so 0.45 never reached the shot.
        // vid-285-630.png wall is soft behind the bean. Gaussian starts past the bean and is full at the wall.
        var dof = Override<DepthOfField>(profile);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(5.4f);
        dof.gaussianEnd.Override(7.0f);
        dof.gaussianMaxRadius.Override(1.5f);
        dof.highQualitySampling.Override(true);
        dof.focusDistance.Override(5.0f);
        dof.focalLength.Override(50f);
        dof.aperture.Override(1f);

        var grade = Override<ColorAdjustments>(profile);
        grade.postExposure.Override(0f);
        grade.contrast.Override(-6f);
        grade.saturation.Override(-6f);
        grade.colorFilter.Override(Color.white);

        var vig = Override<Vignette>(profile);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.45f);
        vig.color.Override(new Color(0.16f, 0.10f, 0.05f));

        EditorUtility.SetDirty(profile);

        var defaults = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();
        if (defaults != null && defaults.volumeProfile != profile)
            defaults.volumeProfile = profile;
    }

    static T Override<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet<T>(out var existing))
        {
            existing.active = true;
            return existing;
        }
        var c = profile.Add<T>(true);
        c.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        AssetDatabase.AddObjectToAsset(c, profile);
        return c;
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// Liquid Glass panel driver for a uGUI Graphic (usually an Image with the LiquidGlass material).
/// Writes per-panel rect size, corner radius and blur into UV1/UV2 (so panels still batch on one material),
/// applies tint/opacity through vertex colour, and picks the blur or fallback shader variant.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
[AddComponentMenu("UI/Effects/Liquid Glass Panel")]
public class LiquidGlassPanel : BaseMeshEffect, IMaterialModifier
{
    public enum BlurMode
    {
        /// Blur only when URP's Opaque Texture is on and the canvas is not Screen Space - Overlay.
        Auto,
        ForceBlur,
        ForceFallback
    }

    const string BlurKeyword = "LIQUIDGLASS_BLUR";

    [Range(0f, 128f)] public float cornerRadius = 28f;
    [Range(0f, 32f)] [Tooltip("Backdrop blur radius in opaque-texture pixels (blur mode only).")]
    public float blur = 10f;
    [ColorUsage(false)] public Color tint = new Color(1f, 1f, 1f, 1f);
    [Range(0f, 1f)] public float opacity = 0.9f;
    public BlurMode blurMode = BlurMode.Auto;

    /// Low graphics tier: every Auto panel uses the fallback (no opaque-texture reads at all).
    public static bool GlobalFallback;
    static readonly HashSet<LiquidGlassPanel> s_Live = new HashSet<LiquidGlassPanel>();

    static readonly Dictionary<Material, Material> s_BlurVariants = new Dictionary<Material, Material>();
    static readonly Dictionary<Material, Material> s_FallbackVariants = new Dictionary<Material, Material>();

    public float CornerRadius { get => cornerRadius; set { cornerRadius = value; Refresh(); } }
    public float Blur { get => blur; set { blur = value; Refresh(); } }
    public Color Tint { get => tint; set { tint = value; Refresh(); } }
    public float Opacity { get => opacity; set { opacity = value; Refresh(); } }

    /// True when the blurred-backdrop variant is in use for this panel.
    public bool UsingBlur => ResolveBlur();

    protected override void OnEnable()
    {
        base.OnEnable();
        s_Live.Add(this);
        EnsureCanvasChannels();
        Refresh();
    }

    protected override void OnDisable()
    {
        s_Live.Remove(this);
        base.OnDisable();
    }

    /// Re-pick blur/fallback on every live panel (after a tier change or a canvas render-mode switch).
    public static void RefreshAll()
    {
        foreach (var p in s_Live) if (p != null) p.Refresh();
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        EnsureCanvasChannels();
        Refresh();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        EnsureCanvasChannels();
        Refresh();
    }
#endif

    void Refresh()
    {
        if (graphic == null) return;
        graphic.SetVerticesDirty();
        graphic.SetMaterialDirty();
    }

    void EnsureCanvasChannels()
    {
        var g = graphic;
        if (g == null || g.canvas == null) return;
        const AdditionalCanvasShaderChannels needed =
            AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        var c = g.canvas;
        if ((c.additionalShaderChannels & needed) != needed) c.additionalShaderChannels |= needed;
        var root = c.rootCanvas;
        if (root != null && (root.additionalShaderChannels & needed) != needed) root.additionalShaderChannels |= needed;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;
        Rect r = graphic.rectTransform.rect;
        float w = Mathf.Max(r.width, 0.001f), h = Mathf.Max(r.height, 0.001f);
        var mul = new Color(tint.r, tint.g, tint.b, opacity);
        var v = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            v.uv1 = new Vector4((v.position.x - r.xMin) / w, (v.position.y - r.yMin) / h, w, h);
            v.uv2 = new Vector4(Mathf.Max(0f, cornerRadius), Mathf.Max(0f, blur), 0f, 0f);
            v.color = (Color32)((Color)v.color * mul);
            vh.SetUIVertex(v, i);
        }
    }

    bool ResolveBlur()
    {
        switch (blurMode)
        {
            case BlurMode.ForceBlur: return true;
            case BlurMode.ForceFallback: return false;
        }
        if (GlobalFallback) return false;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return false;
        var g = graphic;
        var canvas = g != null && g.canvas != null ? g.canvas.rootCanvas : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return false;
        if (urp.supportsCameraOpaqueTexture) return true;
        // or the canvas camera asks for the opaque copy itself (menus turn this on only while they are open)
        var cam = canvas.worldCamera;
        if (cam == null || !cam.TryGetComponent<UniversalAdditionalCameraData>(out var data)) return false;
        return data.requiresColorOption == CameraOverrideOption.On;
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!IsActive() || baseMaterial == null || !baseMaterial.HasProperty("_RimColor")) return baseMaterial;
        bool useBlur = ResolveBlur();
        var cache = useBlur ? s_BlurVariants : s_FallbackVariants;
        if (!cache.TryGetValue(baseMaterial, out var m) || m == null)
        {
            m = new Material(baseMaterial) { hideFlags = HideFlags.HideAndDontSave };
            m.name = baseMaterial.name + (useBlur ? " (Blur)" : " (Fallback)");
            cache[baseMaterial] = m;
        }
        else
        {
            m.CopyPropertiesFromMaterial(baseMaterial); // keep Inspector edits to the base material live
        }
        if (useBlur) m.EnableKeyword(BlurKeyword); else m.DisableKeyword(BlurKeyword);
        return m;
    }
}
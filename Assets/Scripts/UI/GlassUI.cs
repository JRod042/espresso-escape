using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// Light "Liquid Glass" UI kit for menus (art direction 2026-10-28, UI section):
/// frosted light glass (blur ≈ 24 screen px, 16% cream fill, 1 px light rim), Phosphor Regular icons tinted ink
/// #171717 or cream #F7F3EC, kraft card #A47C59, and one accent per level used only on the primary button.
/// Real blur is only switched on for the out-of-play menus (SetMenuBlur); the HUD, pause and level-clear
/// cards sit over gameplay and always use the translucent fallback.
public static class GlassUI
{
    public static readonly Color Ink = FeltUI.Hex(0x171717);
    public static readonly Color Cream = FeltUI.Hex(0xF7F3EC);
    public static readonly Color Kraft = FeltUI.Hex(0xA47C59);
    public static readonly Color InkSoft = FeltUI.Hex(0x171717, 0.68f);

    public const float CardRadius = 28f;
    public const float ButtonRadius = 22f;
    public const float ChipRadius = 24f;
    /// Blur radius in opaque-texture pixels. URP downsamples the opaque copy 2x, so 12 ≈ 24 screen px.
    public const float BlurPx = 12f;
    /// Cream fill strength over the blurred backdrop (brief: 12–18% white).
    public const float FillStrength = 0.16f;
    /// Backdrop brightness lift under glass so ink text stays readable over the darker set.
    public const float BackdropBrightness = 1.3f;
    /// Fallback glass opacity (no blur source: HUD, pause, clear, Low tier).
    public const float FallbackOpacity = 0.86f;
    /// End-of-level / pause dim (brief gap G1: max ≈ 25%).
    public const float DimAlpha = 0.22f;
    /// Larger-text multiplier.
    public const float LargeTextScale = 1.15f;
    /// Plane distance for the Screen Space - Camera menu canvas (camera near clip is 0.05).
    const float MenuPlaneDistance = 0.1f;

    static Material _glass;
    static readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Texture2D> _art = new Dictionary<string, Texture2D>();
    static readonly List<Text> _texts = new List<Text>();
    static readonly List<int> _baseSizes = new List<int>();

    public static Font Font { get; set; }

    /// LiquidGlass material for runtime panels (Resources/UI/LiquidGlass, a copy of Assets/UI/LiquidGlass).
    public static Material GlassMaterial
    {
        get
        {
            if (_glass != null) return _glass;
            var src = Resources.Load<Material>("UI/LiquidGlass");
            if (src == null)
            {
                var sh = Shader.Find("UI/LiquidGlass");
                if (sh == null) return null;
                src = new Material(sh);
            }
            _glass = new Material(src) { name = "LiquidGlass (Menus)" };
            _glass.SetFloat("_TintStrength", FillStrength);
            _glass.SetFloat("_FallbackOpacity", FallbackOpacity);
            _glass.SetFloat("_Brightness", BackdropBrightness);
            _glass.SetFloat("_Saturation", 1.1f);
            _glass.SetFloat("_RimWidth", 1f);
            return _glass;
        }
    }

    /// Phosphor icon by file name (e.g. "ph-play"), loaded from Resources/UI/Icons as a sprite.
    public static Sprite Icon(string name)
    {
        if (_icons.TryGetValue(name, out var s) && s != null) return s;
        var tex = Resources.Load<Texture2D>("UI/Icons/" + name);
        if (tex == null) { Debug.LogWarning("[GlassUI] missing icon " + name); return null; }
        s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        s.name = name;
        _icons[name] = s;
        return s;
    }

    /// Level card art (Resources/UI/LevelArt/level-N or badge-bean).
    public static Texture2D Art(string name)
    {
        if (_art.TryGetValue(name, out var t) && t != null) return t;
        t = Resources.Load<Texture2D>("UI/LevelArt/" + name);
        _art[name] = t;
        return t;
    }

    public static RectTransform Rect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    /// Frosted glass card. Blur when the canvas allows it, cream translucent fallback otherwise.
    public static Image Card(Transform parent, string name, float radius = CardRadius, bool raycast = true)
    {
        var rt = Rect(parent, name);
        var img = rt.gameObject.AddComponent<Image>();
        img.material = GlassMaterial;
        img.raycastTarget = raycast; // cards swallow taps so the bean does not hop under a menu
        var p = rt.gameObject.AddComponent<LiquidGlassPanel>();
        p.cornerRadius = radius;
        p.blur = BlurPx;
        p.tint = Cream;
        p.opacity = 1f;
        return img;
    }

    /// Full-screen dim (≤ 25%) behind pause / clear cards.
    public static Image Dim(Transform parent)
    {
        var rt = Rect(parent, "Dim");
        FeltUI.Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, DimAlpha);
        img.raycastTarget = true;
        return img;
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
    {
        var rt = Rect(parent, "Text");
        FeltUI.Stretch(rt);
        var t = rt.gameObject.AddComponent<Text>();
        t.text = text;
        t.font = Font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        _texts.Add(t);
        _baseSizes.Add(size);
        if (GameSettings.LargeText) t.fontSize = Mathf.RoundToInt(size * LargeTextScale);
        return t;
    }

    /// Re-applies the Larger text setting to every label built through the kit.
    public static void ApplyTextScale()
    {
        float k = GameSettings.LargeText ? LargeTextScale : 1f;
        for (int i = _texts.Count - 1; i >= 0; i--)
        {
            if (_texts[i] == null) { _texts.RemoveAt(i); _baseSizes.RemoveAt(i); continue; }
            _texts[i].fontSize = Mathf.RoundToInt(_baseSizes[i] * k);
        }
    }

    public static Image IconImage(Transform parent, string icon, float size, Color color)
    {
        var rt = Rect(parent, "Icon " + icon);
        rt.sizeDelta = new Vector2(size, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Icon(icon);
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size; le.minWidth = size;
        le.preferredHeight = size; le.minHeight = size;
        le.flexibleWidth = 0f;
        return img;
    }

    /// Primary (accent fill, cream ink) or glass (frosted, ink) button with an optional leading icon.
    public static Button Button(Transform parent, string text, string icon, UnityEngine.Events.UnityAction onClick,
        bool primary = false, Color accent = default, float width = 420f, float height = FeltUI.ButtonHeight, int fontSize = 26)
    {
        Image img;
        if (primary)
        {
            img = FeltUI.PlateImage(parent, "Btn " + text, accent, true);
        }
        else
        {
            img = Card(parent, "Btn " + text, ButtonRadius);
        }
        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(width, height);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(onClick);
        btn.onClick.AddListener(() => EscapeAudio.Play(EscapeAudio.Sfx.Tap, 0.6f));
        img.gameObject.AddComponent<ButtonPress>();
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.minWidth = Mathf.Min(width, 120f);
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleWidth = 1f;

        Color ink = primary ? Cream : Ink;
        var row = Rect(img.transform, "Row");
        FeltUI.Stretch(row);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 10f;
        h.padding = new RectOffset(18, 18, 0, 0);
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        if (!string.IsNullOrEmpty(icon)) IconImage(row, icon, height * 0.46f, ink);
        if (!string.IsNullOrEmpty(text))
        {
            var t = Label(row, text, fontSize, ink, TextAnchor.MiddleCenter, primary ? FontStyle.Bold : FontStyle.Normal);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tle = t.gameObject.AddComponent<LayoutElement>();
            tle.preferredHeight = height;
        }
        return btn;
    }

    /// Vertical layout inside a card with padding.
    public static VerticalLayoutGroup Stack(RectTransform rt, int pad, float spacing, TextAnchor align = TextAnchor.UpperCenter)
    {
        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(pad, pad, pad, pad);
        v.spacing = spacing;
        v.childAlignment = align;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup Row(Transform parent, string name, float height, float spacing)
    {
        var rt = Rect(parent, name);
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.minHeight = height; le.flexibleWidth = 1f;
        return h;
    }

    public static LayoutElement Size(Component c, float w, float h, float flexW = 0f)
    {
        var le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        if (w > 0f) { le.preferredWidth = w; le.minWidth = w; }
        if (h > 0f) { le.preferredHeight = h; le.minHeight = h; }
        le.flexibleWidth = flexW;
        return le;
    }

    /// Menus get real backdrop blur: the canvas renders in Screen Space - Camera on the main camera and that camera
    /// alone requests URP's opaque copy. Gameplay (HUD, pause, clear) goes back to Overlay and the copy is switched
    /// off, so nothing samples or pays for the opaque texture during play. The URP asset is not modified.
    public static void SetMenuBlur(Canvas canvas, bool on)
    {
        if (canvas == null) return;
        var cam = Camera.main;
        bool blur = on && cam != null && !LiquidGlassPanel.GlobalFallback;
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.requiresColorOption = blur ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
        }
        if (blur)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = MenuPlaneDistance;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        LiquidGlassPanel.RefreshAll();
    }

    /// m:ss for a time in seconds; strings cached per whole second (no per-frame allocation).
    static readonly Dictionary<int, string> _clock = new Dictionary<int, string>();
    public static string Clock(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        if (!_clock.TryGetValue(s, out var str))
        {
            str = (s / 60) + ":" + (s % 60).ToString("00");
            _clock[s] = str;
        }
        return str;
    }
}

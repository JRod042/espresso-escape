using UnityEngine;
using UnityEngine.UI;

/// Runtime felt UI kit — no sprite assets. Rounded 9-slice plate (anti-aliased 20-px corners, soft 2-px darker
/// edge), a tiny honey-egg icon, the plate palette sampled from the Imagine plates, and shared builders for the
/// 56-px buttons / chips used by TitleFlow and SearchMenu. Legacy uGUI Text only (no TMP).
public static class FeltUI
{
    public static readonly Color KraftTan = Hex(0xB88C5C);
    public static readonly Color Burlap = Hex(0xCDA876);
    public static readonly Color FeltCream = Hex(0xEEE6DA);
    public static readonly Color PompomRed = Hex(0xB22E26);
    public static readonly Color Olive = Hex(0x6C7638);
    public static readonly Color DarkRoast = Hex(0x3A2416);
    /// pickup egg colour (LevelRunner honey tint over brown felt, clamped for UI)
    public static readonly Color Honey = new Color(0.97f, 0.80f, 0.44f);

    public const float ButtonHeight = 56f;
    public const float ButtonWidth = 420f;
    public const float Radius = 20f;
    public const float Gap = 12f;
    public const float SafeMargin = 24f;

    static Sprite _plate, _egg;

    public static Color Hex(int rgb, float a = 1f) =>
        new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

    public static Color With(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// 64×64 rounded-rect, radius 20, 9-slice border 22. White so Image.color tints it; rim 2 px darker.
    public static Sprite Plate()
    {
        if (_plate != null) return _plate;
        const int S = 64;
        const float R = Radius, Edge = 2f;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "felt-plate", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[S * S];
        Vector2 half = new Vector2(S * 0.5f - 0.5f, S * 0.5f - 0.5f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                var p = new Vector2(x + 0.5f - S * 0.5f, y + 0.5f - S * 0.5f);
                var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * R);
                float d = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - R;
                float a = Mathf.Clamp01(0.5f - d);                       // anti-aliased coverage
                float rim = Mathf.Clamp01((d + Edge) / Edge);            // 0 inside → 1 at the border
                float c = Mathf.Lerp(1f, 0.70f, rim);
                px[y * S + x] = new Color(c, c, c, a);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        _plate = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        _plate.name = "felt-plate";
        return _plate;
    }

    /// 40×48 egg (narrower top, soft shade bottom-right). White; tint with Honey.
    public static Sprite Egg()
    {
        if (_egg != null) return _egg;
        const int W = 40, H = 48;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "felt-egg", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[W * H];
        float rx = 16.5f, ry = 21.5f;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x + 0.5f - W * 0.5f, dy = y + 0.5f - H * 0.5f;
                float ny = dy / ry;
                float rxe = rx * (1f - 0.11f * Mathf.Clamp(ny, -1f, 1f));   // taper toward the top
                float d = (Mathf.Sqrt((dx * dx) / (rxe * rxe) + (dy * dy) / (ry * ry)) - 1f) * rx;
                float a = Mathf.Clamp01(0.5f - d);
                float shade = Mathf.Clamp01((dx / rx - ny) * 0.45f + 0.15f);
                float c = Mathf.Lerp(1f, 0.74f, shade);
                px[y * W + x] = new Color(c, c, c, a);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        _egg = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        _egg.name = "felt-egg";
        return _egg;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// Rounded translucent plate image (chips, hint zones, panels).
    public static Image PlateImage(Transform parent, string name, Color tint, bool raycast)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.sprite = Plate();
        img.type = Image.Type.Sliced;
        img.color = tint;
        img.raycastTarget = raycast;
        return img;
    }

    public static Text Label(Transform parent, string text, Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Stretch(rt);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    /// 56-px rounded felt button with press scale, hover / press tints and the UI tap click.
    public static Button Button(Transform parent, string text, Font font, Color plate, Color textColor,
        UnityEngine.Events.UnityAction onClick, float width = ButtonWidth, float height = ButtonHeight, int fontSize = 28)
    {
        var img = PlateImage(parent, "Btn " + text, plate, true);
        var rt = img.rectTransform;
        rt.sizeDelta = new Vector2(width, height);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(onClick);
        btn.onClick.AddListener(() => EscapeAudio.Play(EscapeAudio.Sfx.Tap, 0.6f));
        img.gameObject.AddComponent<ButtonPress>();
        var le = img.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 160f;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;
        Label(img.transform, text, font, fontSize, textColor);
        return btn;
    }

    /// Vertical felt stack. Width is fixed; height fits the children plus Gap so plates cannot land on each other.
    /// pinBottom keeps the last child on the safe-area floor (Level Clear). Otherwise the stack is centered.
    public static RectTransform Column(Transform parent, string name, float width, bool pinBottom)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (pinBottom)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        rt.sizeDelta = new Vector2(width, 0f);
        rt.anchoredPosition = Vector2.zero;
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = Gap;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var fit = go.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    /// Anchor a rect to a corner/edge of its parent (anchor and pivot share the same normalized point).
    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }
}

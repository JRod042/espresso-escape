using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// Boot → Title → Settings / Levels / Play → Pause / Level clear. Light Liquid Glass menus (art direction 2026-10-28):
/// frosted cards with Phosphor icons, ink text, one level accent on the primary button only, fades that respect
/// Reduce motion. Real backdrop blur only on the out-of-play menus; the HUD, pause and clear cards use the fallback.
public class TitleFlow : MonoBehaviour
{
    public enum Screen { None, Title, Settings, Levels, Hud, Pause, Clear, Info, Loading }

    public Screen Current { get; private set; }
    public bool IsBuilt => _canvas != null;

    const float TitleCardW = 520f; // look step00: 520 so the landscape card (right edge) clears the 285 centre crop (was 560)
    const float SettingsCardW = 700f;
    const float PauseCardW = 460f;
    const float ClearCardW = 860f;
    const float InfoCardW = 720f;
    const float InfoBodyH = 400f;   // fits the 13-line credits at Larger text (22 px × 1.15)
    const int InfoFont = 22;
    const float RowH = 56f;
    const float HudH = 52f;
    const float BannerSeconds = 2.8f;
    const float StarStepSeconds = 0.22f;
    const float ProgressEase = 8f;

    Canvas _canvas;
    CanvasScaler _scaler;
    RectTransform _safe;
    Rect _safeSeen;
    int _safeW, _safeH;
    bool _landscape, _dollyOn;
    Font _font;
    LevelRunner _runner;

    GameObject _title, _settings, _levels, _hud, _pause, _clear, _info, _loading;
    SearchMenu _searchMenu;
    Screen _settingsReturn = Screen.Title;
    Screen _infoReturn = Screen.Settings;

    // title
    Button _continueBtn;
    Text _continueLabel, _titleStars;
    // settings values
    Text _vMusic, _vSfx, _vMute, _vHaptics, _vMotion, _vText, _vTier;
    // info
    Text _infoTitle, _infoBody;
    // hud
    Text _hudLevel, _hudBeans, _hudTime;
    RectTransform _progressFill;
    CanvasGroup _banner;
    Text _bannerText;
    float _bannerUntil;
    string _lastClock;
    int _hintStage;
    // pause
    Button _resumeBtn;
    // clear
    Text _clearTitle, _clearStats, _clearHoney, _clearBest;
    Image[] _clearStars;
    RawImage _clearArt;
    Button _nextBtn;
    Text _nextLabel;
    Coroutine _starAnim;

    void Start()
    {
        EnsureEventSystem();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GlassUI.Font = _font;
        GameSettings.Apply();
        BuildChrome();
        ShowTitle();
        _runner = FindAnyObjectByType<LevelRunner>();
    }

    void OnEnable()
    {
        GameSession.OnLevelClear += OnLevelClear;
        GameSession.OnBeansChanged += OnBeansChanged;
        GameSettings.Changed += OnSettingsChanged;
        GrinderChase.OnWake += OnGrinderWake;
    }

    void OnDisable()
    {
        GameSession.OnLevelClear -= OnLevelClear;
        GameSession.OnBeansChanged -= OnBeansChanged;
        GameSettings.Changed -= OnSettingsChanged;
        GrinderChase.OnWake -= OnGrinderWake;
    }

    // ───────────────────────── lifecycle ─────────────────────────

    void OnApplicationPause(bool paused) { if (paused) AutoPause(); }
    void OnApplicationFocus(bool focus) { if (!focus) AutoPause(); }

    /// Backgrounding the app (or losing focus) mid-level opens Pause so the bean is not left running.
    void AutoPause()
    {
        if (Application.isBatchMode || Current != Screen.Hud) return;
        if (GameSession.I != null && GameSession.I.phase != GameSession.Phase.Play) return;
        ShowPause();
    }

    void Update()
    {
        if (!_dollyOn)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<TitleDolly>() == null) cam.gameObject.AddComponent<TitleDolly>();
                _dollyOn = true;
            }
        }
        if (_safe != null && (_safeW != UnityEngine.Screen.width || _safeH != UnityEngine.Screen.height || _safeSeen != UnityEngine.Screen.safeArea))
            ApplySafeAreaPad(_safe);
        if (_scaler != null && (UnityEngine.Screen.width > UnityEngine.Screen.height) != _landscape) FitScaler();

        if (_banner != null && _banner.gameObject.activeSelf && Time.unscaledTime > _bannerUntil)
        {
            _banner.alpha = Mathf.MoveTowards(_banner.alpha, 0f, Time.unscaledDeltaTime * 4f);
            if (_banner.alpha <= 0f) _banner.gameObject.SetActive(false);
        }
        if (Current != Screen.Hud) return;
        if (_runner == null) _runner = FindAnyObjectByType<LevelRunner>();
        if (_progressFill != null && _runner != null)
        {
            var a = _progressFill.anchorMax;
            a.x = Mathf.Lerp(a.x, _runner.Progress, 1f - Mathf.Exp(-ProgressEase * Time.unscaledDeltaTime));
            _progressFill.anchorMax = a;
        }
        if (_hudTime != null && GameSession.I != null)
        {
            string c = GlassUI.Clock(GameSession.I.ElapsedSeconds);
            if (!ReferenceEquals(c, _lastClock)) { _hudTime.text = c; _lastClock = c; }
        }
        UpdateHints();
    }

    // ───────────────────────── build ─────────────────────────

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void FitScaler()
    {
        if (_scaler == null) return;
        _landscape = UnityEngine.Screen.width > UnityEngine.Screen.height;
        _scaler.referenceResolution = _landscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
        _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _scaler.matchWidthOrHeight = _landscape ? 1f : 0f;
        PlaceTitleCard();
    }

    RectTransform _titleCard;
    /// look step00: in landscape the menu card sits at the right edge (x >= 1384 of 1920), clear of the centred bean and of the
    /// 285 centre crop; portrait keeps it on the safe-area floor.
    void PlaceTitleCard()
    {
        if (_titleCard == null) return;
        var a = _landscape ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0f);
        _titleCard.anchorMin = _titleCard.anchorMax = _titleCard.pivot = a;
        _titleCard.anchoredPosition = _landscape ? new Vector2(-16f, 0f) : Vector2.zero;
    }

    void BuildChrome()
    {
        var root = new GameObject("UI Root");
        root.transform.SetParent(transform, false);
        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
        _scaler = root.AddComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        FitScaler();
        root.AddComponent<GraphicRaycaster>();

        // Overlays that dim the game span the whole screen; their cards and every other screen sit in the safe area.
        _pause = Panel("Pause Overlay", root.transform, true);
        _clear = Panel("Level Clear", root.transform, true);
        _loading = Panel("Loading Overlay", root.transform, true);

        _safe = GlassUI.Rect(root.transform, "SafeArea");
        ApplySafeAreaPad(_safe);
        _title = Panel("Title Screen", _safe, false);
        _settings = Panel("Settings", _safe, false);
        _levels = Panel("Levels", _safe, false);
        _hud = Panel("Play HUD", _safe, false);
        _info = Panel("Info Overlay", _safe, false);
        // overlays draw above the HUD
        _pause.transform.SetAsLastSibling();
        _clear.transform.SetAsLastSibling();
        _info.transform.SetParent(root.transform, false);
        _info.transform.SetAsLastSibling();
        _loading.transform.SetAsLastSibling();

        BuildTitle();
        BuildSettings();
        _searchMenu = _levels.AddComponent<SearchMenu>();
        _searchMenu.Build(_levels.transform, _font, OnPickLevel, ShowTitle);
        BuildHud();
        BuildPause();
        BuildClear();
        BuildInfo();
        BuildLoading();
    }

    GameObject Panel(string name, Transform parent, bool dim)
    {
        var rt = GlassUI.Rect(parent, name);
        FeltUI.Stretch(rt);
        rt.gameObject.AddComponent<CanvasGroup>();
        rt.gameObject.AddComponent<UIFade>();
        if (dim)
        {
            var d = GlassUI.Dim(rt);
            d.rectTransform.offsetMin = new Vector2(-64f, -64f); // past the canvas edge
            d.rectTransform.offsetMax = new Vector2(64f, 64f);
        }
        rt.gameObject.SetActive(false);
        return rt.gameObject;
    }

    RectTransform CardAt(Transform parent, string name, float width, Vector2 anchor, int pad = 24, float spacing = 12f)
    {
        var card = GlassUI.Card(parent, name);
        var rt = card.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = new Vector2(width, 0f);
        GlassUI.Stack(rt, pad, spacing);
        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    Text Heading(Transform parent, string icon, string text, int size = 36)
    {
        var row = GlassUI.Row(parent, "Heading", size + 16f, 12f);
        if (!string.IsNullOrEmpty(icon)) GlassUI.IconImage(row.transform, icon, size * 0.85f, GlassUI.Ink);
        var t = GlassUI.Label(row.transform, text, size, GlassUI.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GlassUI.Size(t, 0f, size + 16f, 1f);
        return t;
    }

    void BuildTitle()
    {
        // The yarn ESPRESSO / ESCAPE sign is 3D (FeltTitle); this card holds the controls only, on the safe-area floor.
        var card = CardAt(_title.transform, "Title Card", TitleCardW, new Vector2(0.5f, 0f), 20, 12f);
        _titleCard = card;
        PlaceTitleCard();
        var top = GlassUI.Row(card, "Badge Row", 64f, 14f);
        var badge = GlassUI.Rect(top.transform, "Badge").gameObject.AddComponent<RawImage>();
        badge.texture = GlassUI.Art("badge-bean");
        badge.raycastTarget = false;
        GlassUI.Size(badge, 64f, 64f);
        var tag = GlassUI.Label(top.transform, "Casa Rústico", 24, GlassUI.InkSoft, TextAnchor.MiddleLeft);
        GlassUI.Size(tag, 0f, 64f, 1f);
        GlassUI.IconImage(top.transform, "ph-star-fill", 26f, GlassUI.Ink);
        _titleStars = GlassUI.Label(top.transform, "", 26, GlassUI.Ink, TextAnchor.MiddleRight);
        GlassUI.Size(_titleStars, 80f, 64f);

        _continueBtn = GlassUI.Button(card, "Continue", "ph-play", PlayNow, true, LevelCatalog.AccentL1, TitleCardW - 40f, 64f, 28);
        _continueBtn.name = "Btn Continue";
        _continueLabel = _continueBtn.GetComponentInChildren<Text>();
        var row = GlassUI.Row(card, "Row 2", RowH, 12f);
        row.childForceExpandWidth = true;
        GlassUI.Button(row.transform, "New game", "ph-arrow-counter-clockwise", NewGame).name = "Btn New game";
        GlassUI.Button(row.transform, "Levels", "ph-list-bullets", ShowSearch).name = "Btn Levels";
        GlassUI.Button(card, "Settings", "ph-gear-six", ShowOptions).name = "Btn Settings";
    }

    Text SettingRow(Transform parent, string icon, string label, UnityEngine.Events.UnityAction onClick)
    {
        var row = GlassUI.Row(parent, "Row " + label, RowH, 14f);
        row.padding = new RectOffset(8, 0, 0, 0);
        GlassUI.IconImage(row.transform, icon, 30f, GlassUI.Ink);
        var t = GlassUI.Label(row.transform, label, 26, GlassUI.Ink, TextAnchor.MiddleLeft);
        GlassUI.Size(t, 0f, RowH, 1f);
        var btn = GlassUI.Button(row.transform, "", null, onClick, false, default, 200f, 48f, 24);
        btn.name = "Btn " + label;
        btn.GetComponent<LayoutElement>().flexibleWidth = 0f;
        var v = GlassUI.Label(btn.transform, "", 24, GlassUI.Ink);
        return v;
    }

    void BuildSettings()
    {
        var card = CardAt(_settings.transform, "Settings Card", SettingsCardW, new Vector2(0.5f, 0.5f), 24, 8f);
        Heading(card, "ph-gear-six", "Settings");
        _vMusic = SettingRow(card, "ph-music-notes", "Music", () => { GameSettings.MusicVolume = GameSettings.NextStep(GameSettings.MusicVolume); GameSettings.Apply(); });
        _vSfx = SettingRow(card, "ph-speaker-high", "Sound effects", () => { GameSettings.SfxVolume = GameSettings.NextStep(GameSettings.SfxVolume); GameSettings.Apply(); });
        _vMute = SettingRow(card, "ph-speaker-slash", "Mute all", () => { GameSettings.Muted = !GameSettings.Muted; GameSettings.Apply(); });
        _vHaptics = SettingRow(card, "ph-vibrate", "Haptics", () => { GameSettings.Haptics = !GameSettings.Haptics; GameSettings.Apply(); Haptics.Pulse(); });
        _vMotion = SettingRow(card, "ph-eye", "Reduce motion", () => { GameSettings.ReduceMotion = !GameSettings.ReduceMotion; GameSettings.Apply(); });
        _vText = SettingRow(card, "ph-text-aa", "Larger text", () => { GameSettings.LargeText = !GameSettings.LargeText; GameSettings.Apply(); });
        _vTier = SettingRow(card, "ph-timer", "Graphics", () => { GameSettings.Quality = (GameSettings.Tier)(((int)GameSettings.Quality + 1) % 3); GameSettings.Apply(); });

        var info = GlassUI.Row(card, "Info Row", 48f, 8f);
        info.childForceExpandWidth = true;
        InfoBtn(info.transform, "How to play", ShowHowToPlay);
        InfoBtn(info.transform, "Credits", ShowCredits);
        InfoBtn(info.transform, "Privacy", ShowPrivacy);
        InfoBtn(info.transform, "Support", ShowSupport);
        InfoBtn(info.transform, "Age rating", ShowAgeRating);
        GlassUI.Button(card, "Back", "ph-arrow-left", CloseSettings).name = "Btn Back";
        SyncSettings();
    }

    void InfoBtn(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var b = GlassUI.Button(parent, label, null, onClick, false, default, 120f, 48f, 19);
        b.name = "Btn " + label;
    }

    static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    static string OnOff(bool v) => v ? "On" : "Off";

    void SyncSettings()
    {
        if (_vMusic == null) return;
        _vMusic.text = Pct(GameSettings.MusicVolume);
        _vSfx.text = Pct(GameSettings.SfxVolume);
        _vMute.text = GameSettings.Muted ? "Muted" : "Off";
        _vHaptics.text = OnOff(GameSettings.Haptics);
        _vMotion.text = OnOff(GameSettings.ReduceMotion);
        _vText.text = OnOff(GameSettings.LargeText);
        var q = GameSettings.Quality;
        _vTier.text = q == GameSettings.Tier.Auto ? "Auto (" + GameSettings.EffectiveTier + ")" : q.ToString();
    }

    void OnSettingsChanged()
    {
        SyncSettings();
        GlassUI.ApplyTextScale();
        ApplyBlurFor(Current);
    }

    RectTransform HudChip(Transform parent, string name, string icon, out Text label, float width)
    {
        var chip = GlassUI.Card(parent, name, GlassUI.ChipRadius, false);
        GlassUI.Size(chip, width, HudH).minWidth = width * 0.6f;
        var row = GlassUI.Rect(chip.transform, "Row");
        FeltUI.Stretch(row);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(16, 16, 0, 0);
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        GlassUI.IconImage(row, icon, 26f, GlassUI.Ink);
        label = GlassUI.Label(row, "", 24, GlassUI.Ink, TextAnchor.MiddleLeft);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        GlassUI.Size(label, 0f, HudH, 1f);
        return chip.rectTransform;
    }

    void BuildHud()
    {
        var bar = GlassUI.Rect(_hud.transform, "HUD Row");
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(0f, HudH);
        bar.anchoredPosition = Vector2.zero;
        var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        HudChip(bar, "Level Chip", "ph-flag", out _hudLevel, 340f);
        HudChip(bar, "Bean Chip", "ph-coffee-bean", out _hudBeans, 150f);
        // look step00: the timer moves right of the spacer so the top band over the bean (285 garland/cloud band) stays clear
        var spacer = GlassUI.Rect(bar, "Spacer");
        GlassUI.Size(spacer, 0f, HudH, 1f);
        HudChip(bar, "Time Chip", "ph-timer", out _hudTime, 130f);
        var pause = GlassUI.Button(bar, "", "ph-pause", ShowPause, false, default, 64f, 64f);
        pause.name = "Btn Menu";
        pause.GetComponent<LayoutElement>().flexibleWidth = 0f;

        // progress to the goal under the chips
        var track = GlassUI.Card(_hud.transform, "Progress Track", 7f, false);
        var trt = track.rectTransform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(1f, 1f);   // look step00: right corner, off the garland band (was top centre)
        trt.sizeDelta = new Vector2(480f, 14f);
        trt.anchoredPosition = new Vector2(-64f, -HudH - 14f);
        var fill = GlassUI.Rect(track.transform, "Progress Fill");
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        var fimg = fill.gameObject.AddComponent<Image>();
        fimg.sprite = FeltUI.Plate(); fimg.type = Image.Type.Sliced; fimg.pixelsPerUnitMultiplier = 6f;
        fimg.color = GlassUI.Ink;
        fimg.raycastTarget = false;
        _progressFill = fill;
        var flag = GlassUI.IconImage(track.transform, "ph-flag", 26f, GlassUI.Ink);
        var frt = flag.rectTransform;
        frt.anchorMin = frt.anchorMax = new Vector2(1f, 0.5f);
        frt.anchoredPosition = new Vector2(18f, 0f);

        // hint / boss banner
        var banner = GlassUI.Card(_hud.transform, "Banner", GlassUI.ChipRadius, false);
        var brt = banner.rectTransform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0f, 1f);   // look step00: left corner under the chips, off the garland band (was top centre, 620 wide)
        brt.sizeDelta = new Vector2(560f, 60f);
        brt.anchoredPosition = new Vector2(16f, -HudH - 48f);
        _banner = banner.gameObject.AddComponent<CanvasGroup>();
        _banner.blocksRaycasts = false;
        _bannerText = GlassUI.Label(banner.transform, "", 26, GlassUI.Ink);
        _bannerText.rectTransform.offsetMin = new Vector2(20f, 0f);
        _bannerText.rectTransform.offsetMax = new Vector2(-20f, 0f);
        banner.gameObject.SetActive(false);
    }

    void BuildPause()
    {
        var card = CardAt(_pause.transform, "Pause Card", PauseCardW, new Vector2(0.5f, 0.5f));
        Heading(card, "ph-pause", "Paused", 40);
        _resumeBtn = GlassUI.Button(card, "Resume", "ph-play", ResumeFromPause, true, LevelCatalog.AccentL1, PauseCardW - 48f, 60f, 28);
        _resumeBtn.name = "Btn Resume";
        GlassUI.Button(card, "Restart level", "ph-arrow-counter-clockwise", RestartFromPause).name = "Btn Restart";
        GlassUI.Button(card, "Settings", "ph-gear-six", () => OpenSettings(Screen.Pause)).name = "Btn Settings";
        GlassUI.Button(card, "Title", "ph-house", TitleFromPause).name = "Btn Title";
    }

    void BuildClear()
    {
        var card = CardAt(_clear.transform, "Clear Card", ClearCardW, new Vector2(0.5f, 0.5f), 24, 16f);
        var top = GlassUI.Row(card, "Top", 214f, 24f);
        top.childAlignment = TextAnchor.UpperLeft;
        _clearArt = GlassUI.Rect(top.transform, "Art").gameObject.AddComponent<RawImage>();
        _clearArt.raycastTarget = false;
        GlassUI.Size(_clearArt, 368f, 207f);
        var col = GlassUI.Rect(top.transform, "Stats");
        GlassUI.Size(col, 0f, 207f, 1f);
        GlassUI.Stack(col, 0, 6f, TextAnchor.UpperLeft);
        _clearTitle = GlassUI.Label(col, "", 34, GlassUI.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GlassUI.Size(_clearTitle, 0f, 48f, 1f);
        var stars = GlassUI.Row(col, "Stars", 52f, 8f);
        stars.childAlignment = TextAnchor.MiddleLeft;
        _clearStars = new Image[3];
        for (int i = 0; i < 3; i++) _clearStars[i] = GlassUI.IconImage(stars.transform, "ph-star", 46f, GlassUI.Ink);
        _clearStats = GlassUI.Label(col, "", 22, GlassUI.Ink, TextAnchor.MiddleLeft);
        GlassUI.Size(_clearStats, 0f, 32f, 1f);
        _clearHoney = GlassUI.Label(col, "", 22, GlassUI.Ink, TextAnchor.MiddleLeft);
        GlassUI.Size(_clearHoney, 0f, 30f, 1f);
        _clearBest = GlassUI.Label(col, "", 22, GlassUI.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GlassUI.Size(_clearBest, 0f, 30f, 1f);

        var row = GlassUI.Row(card, "Buttons", 60f, 12f);
        row.childForceExpandWidth = true;
        _nextBtn = GlassUI.Button(row.transform, "Next", "ph-arrow-right", NextFromClear, true, LevelCatalog.AccentL2, 300f, 60f, 28);
        _nextBtn.name = "Btn Next";
        _nextLabel = _nextBtn.GetComponentInChildren<Text>();
        GlassUI.Button(row.transform, "Replay", "ph-arrow-counter-clockwise", ReplayFromClear, false, default, 220f, 60f).name = "Btn Replay";
        GlassUI.Button(row.transform, "Title", "ph-house", ShowTitle, false, default, 220f, 60f).name = "Btn Title";
    }

    void BuildInfo()
    {
        GlassUI.Dim(_info.transform);
        var card = CardAt(_info.transform, "Info Card", InfoCardW, new Vector2(0.5f, 0.5f));
        _infoTitle = Heading(card, "ph-question", "Info");
        _infoBody = GlassUI.Label(card, "", InfoFont, GlassUI.Ink);
        GlassUI.Size(_infoBody, 0f, InfoBodyH, 1f);
        GlassUI.Button(card, "Close", "ph-x", CloseInfo).name = "Btn Close";
    }

    void BuildLoading()
    {
        var pill = GlassUI.Card(_loading.transform, "Loading Pill", GlassUI.ChipRadius, false);
        var rt = pill.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280f, 72f);
        GlassUI.Label(pill.transform, "Loading…", 30, GlassUI.Ink);
    }

    // ───────────────────────── navigation ─────────────────────────

    void HideAll()
    {
        _title.SetActive(false);
        _settings.SetActive(false);
        _levels.SetActive(false);
        _hud.SetActive(false);
        _pause.SetActive(false);
        _info.SetActive(false);
        _loading.SetActive(false);
        _clear.SetActive(false);
    }

    void Show(Screen s, GameObject panel)
    {
        Current = s;
        panel.SetActive(true);
        ApplyBlurFor(s);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.transform as RectTransform);
    }

    /// Blur for the out-of-play menus only. Settings / info opened from Pause stay on the fallback over gameplay.
    void ApplyBlurFor(Screen s)
    {
        bool menu = s == Screen.Title || s == Screen.Levels
            || (s == Screen.Settings && _settingsReturn == Screen.Title)
            || (s == Screen.Info && _settingsReturn == Screen.Title);
        GlassUI.SetMenuBlur(_canvas, menu);
    }

    public void ShowTitle()
    {
        Time.timeScale = 1f;
        HideAll();
        _settingsReturn = Screen.Title;
        int last = PlayerProgress.LastPlayed;
        var def = LevelCatalog.Make(last);
        if (_continueLabel != null) _continueLabel.text = "Continue  ·  " + def.title;
        if (_continueBtn != null) _continueBtn.targetGraphic.color = def.accent;
        if (_titleStars != null) _titleStars.text = PlayerProgress.TotalStars() + "/" + (LevelCatalog.LevelCount * 3);
        Show(Screen.Title, _title);
        if (GameSession.I != null) GameSession.I.GoTitle();
    }

    public void ShowOptions() => OpenSettings(Screen.Title);

    public void OpenSettings(Screen from)
    {
        _settingsReturn = from;
        if (from == Screen.Pause) _pause.SetActive(false);
        else HideAll();
        SyncSettings();
        EscapeAudio.Play(EscapeAudio.Sfx.Open, 0.5f);
        Show(Screen.Settings, _settings);
        if (from != Screen.Pause && GameSession.I != null) GameSession.I.GoOptions();
    }

    void CloseSettings()
    {
        _settings.SetActive(false);
        EscapeAudio.Play(EscapeAudio.Sfx.Close, 0.5f);
        if (_settingsReturn == Screen.Pause) ShowPause();
        else ShowTitle();
    }

    public void ShowSearch()
    {
        HideAll();
        _settingsReturn = Screen.Title;
        if (_searchMenu != null) { _searchMenu.FitWidth(ContentWidth()); _searchMenu.Refresh(); }
        Show(Screen.Levels, _levels);
        if (GameSession.I != null) GameSession.I.GoSearch();
    }

    public void ShowPause()
    {
        if (Current == Screen.Clear) return;
        Time.timeScale = 0f;
        HideAll();
        _hud.SetActive(true); // the HUD stays under the pause card
        var def = GameSession.I != null ? GameSession.I.Current : LevelCatalog.Make(1);
        if (_resumeBtn != null) _resumeBtn.targetGraphic.color = def.accent;
        EscapeAudio.Play(EscapeAudio.Sfx.Open, 0.5f);
        Show(Screen.Pause, _pause);
    }

    public void ResumeFromPause()
    {
        Time.timeScale = 1f;
        _pause.SetActive(false);
        EscapeAudio.Play(EscapeAudio.Sfx.Close, 0.5f);
        Current = Screen.Hud;
        ApplyBlurFor(Screen.Hud);
    }

    void RestartFromPause()
    {
        int id = GameSession.I != null ? GameSession.I.currentLevelId : 1;
        EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.PlayLevel(id); });
    }

    void TitleFromPause()
    {
        Time.timeScale = 1f;
        ShowTitle();
    }

    void PlayNow() => EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.Continue(); });
    void NewGame() => EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.NewGame(); });
    void OnPickLevel(int id) => EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.PlayLevel(id); });
    void NextFromClear()
    {
        if (GameSession.I != null && GameSession.I.currentLevelId >= LevelCatalog.LevelCount) { ShowSearch(); return; }
        EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.NextLevel(); });
    }
    void ReplayFromClear() => EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.PlayLevel(GameSession.I.currentLevelId); });

    /// Brief Loading pill, start the level, then the HUD on the next frame.
    void EnterPlayFromMenu(System.Action startPlay)
    {
        HideAll();
        Time.timeScale = 1f;
        if (_starAnim != null) { StopCoroutine(_starAnim); _starAnim = null; }
        Show(Screen.Loading, _loading);
        startPlay?.Invoke();
        StartCoroutine(RevealHudNextFrame());
    }

    IEnumerator RevealHudNextFrame()
    {
        yield return null;
        _loading.SetActive(false);
        ShowHud();
    }

    void ShowHud()
    {
        HideAll();
        _hintStage = 0;
        _lastClock = null;
        if (GameSession.I != null)
        {
            var def = GameSession.I.Current;
            if (_hudLevel != null) _hudLevel.text = def.id + ". " + def.title;
            OnBeansChanged(GameSession.I.beansCollected, GameSession.I.beansTotal);
        }
        if (_progressFill != null) { var a = _progressFill.anchorMax; a.x = 0f; _progressFill.anchorMax = a; }
        if (_banner != null) _banner.gameObject.SetActive(false);
        Show(Screen.Hud, _hud);
    }

    void OnBeansChanged(int got, int total)
    {
        if (_hudBeans != null) _hudBeans.text = total > 0 ? got + " / " + total : "";
    }

    void OnLevelClear(int id, int got, int total)
    {
        if (_clear == null) return;
        HideAll();
        var gs = GameSession.I;
        var def = gs != null ? gs.Current : LevelCatalog.Make(id);
        bool last = id >= LevelCatalog.LevelCount;
        float secs = gs != null ? gs.lastSeconds : 0f;
        int stars = gs != null ? gs.lastStars : 1;
        _clearArt.texture = GlassUI.Art("level-" + id);
        _clearTitle.text = last ? "Escape complete!" : def.title + " cleared";
        float best = PlayerProgress.BestTime(id);
        _clearStats.text = "Time " + GlassUI.Clock(secs) + "   ·   Best " + GlassUI.Clock(best) + "   ·   Par " + GlassUI.Clock(def.parSeconds);
        _clearHoney.text = total > 0 ? "Honey beans " + got + " / " + total : "";
        _clearBest.text = gs != null && gs.lastImproved ? "New best!" : "";
        var next = last ? def : LevelCatalog.Make(id + 1);
        _nextBtn.targetGraphic.color = next.accent;
        if (_nextLabel != null) _nextLabel.text = last ? "Levels" : "Next";
        Show(Screen.Clear, _clear);
        if (_starAnim != null) StopCoroutine(_starAnim);
        _starAnim = StartCoroutine(RevealStars(stars, last));
    }

    IEnumerator RevealStars(int count, bool finale)
    {
        Color on = finale ? LevelCatalog.GoldL5 : GlassUI.Ink; // L5 secondary gold (brief) on the finale stars only
        for (int i = 0; i < 3; i++)
        {
            _clearStars[i].sprite = GlassUI.Icon("ph-star");
            _clearStars[i].color = GlassUI.InkSoft;
            _clearStars[i].rectTransform.localScale = Vector3.one;
        }
        bool instant = GameSettings.ReduceMotion;
        if (instant && count > 0) EscapeAudio.PlayStar(count - 1);
        for (int i = 0; i < count && i < 3; i++)
        {
            if (!instant) yield return new WaitForSecondsRealtime(StarStepSeconds);
            var img = _clearStars[i];
            img.sprite = GlassUI.Icon("ph-star-fill");
            img.color = on;
            if (instant) continue;
            EscapeAudio.PlayStar(i);
            float t = 0f;
            while (t < StarStepSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / StarStepSeconds);
                img.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            img.rectTransform.localScale = Vector3.one;
        }
        _starAnim = null;
    }

    // ───────────────────────── hints ─────────────────────────

    public void ShowBanner(string text, float seconds = BannerSeconds)
    {
        if (_banner == null) return;
        _bannerText.text = text;
        _banner.alpha = 1f;
        _banner.gameObject.SetActive(true);
        _bannerUntil = Time.unscaledTime + seconds;
    }

    static bool TouchUi => Application.isMobilePlatform;

    /// L1 tutorial driven by how far the bean has run, plus the first-vent and lift hints on L4.
    void UpdateHints()
    {
        if (GameSession.I == null || _runner == null || _runner.bean == null) return;
        int id = GameSession.I.currentLevelId;
        float x = _runner.bean.position.x;
        if (id == 1)
        {
            if (_hintStage == 0) { ShowBanner(TouchUi ? "Hold the right side to run" : "Hold D or → to run", 3.5f); _hintStage = 1; }
            else if (_hintStage == 1 && x > 7.4f) { ShowBanner(TouchUi ? "Tap to hop the gap" : "Press Space to hop the gap"); _hintStage = 2; }
            else if (_hintStage == 2 && x > 11.0f) { ShowBanner(TouchUi ? "Quick tap for a short hop" : "Tap Space for a short hop"); _hintStage = 3; }
            else if (_hintStage == 3 && x > 12.8f) { ShowBanner("Grab the honey, reach the flag"); _hintStage = 4; }
        }
        else if (id == 4)
        {
            if (_hintStage == 0 && x > 4.0f) { ShowBanner("Steam vent! Wait for the puff, then go"); _hintStage = 1; }
            else if (_hintStage == 1 && x > 21.4f) { ShowBanner("Ride the lift for bonus honey"); _hintStage = 2; }
        }
    }

    void OnGrinderWake() => ShowBanner("The grinder is awake. Run!", 3f);

    // ───────────────────────── info ─────────────────────────

    /// Mirrors Assets/Resources/Audio/CREDITS-AUDIO.txt. Every audio source is CC0 or Casa original; no CC-BY audio is used.
    void ShowCredits() => OpenInfo("Credits",
        "Espresso Escape · Casa Rústico\n\n" +
        "Gameplay sounds and haptics: Casa sound (original)\n" +
        "UI sounds and jingles: Kenney (www.kenney.nl), CC0\n" +
        "Music via OpenGameArt.org, CC0:\n" +
        "  chill guitar, jazz improvisation looped: Alex McCulloch (Pro Sensory)\n" +
        "  Coffee House Bump: OatCog\n" +
        "  Lofi Hip Hop Loop: omfgdude (OMF-Games)\n" +
        "  In the Lobby: Umplix\n" +
        "  Calm fireplace guitar song: ShggothSlave\n" +
        "Icons: Phosphor Icons (MIT)");
    // rusticopr.com/privacy returned 404 (2026-09-14): in-app stub only, no invented legal copy
    void ShowPrivacy() => OpenInfo("Privacy", "Privacy policy coming soon");
    // ASC stub: no invented IARC copy
    void ShowAgeRating() => OpenInfo("Age rating", "Age rating: TBD");
    void ShowSupport() => OpenInfo("Support", "Support: casarusticocorp@gmail.com");
    void ShowHowToPlay() => OpenInfo("How to play",
        "Hold the right half to run, the left half to walk back.\nTap or swipe up to hop. Release early for a short hop.\nKeyboard: A / D or arrows to move, Space to hop.\nReach the flag. Honey beans and a fast time earn stars.");

    void OpenInfo(string title, string body)
    {
        _infoReturn = Screen.Settings;
        _infoTitle.text = title;
        _infoBody.text = body;
        _settings.SetActive(true);
        Show(Screen.Info, _info);
    }

    void CloseInfo()
    {
        _info.SetActive(false);
        Current = _infoReturn;
        ApplyBlurFor(Current);
    }

    // ───────────────────────── safe area ─────────────────────────

    float ContentWidth()
    {
        float sw = Mathf.Max(1f, UnityEngine.Screen.width), sh = Mathf.Max(1f, UnityEngine.Screen.height);
        float cw = sw > sh ? 1080f * sw / sh : 1080f;
        Rect sa = UnityEngine.Screen.safeArea;
        if (sa.width < 1f) sa = new Rect(0f, 0f, sw, sh);
        return Mathf.Max(320f, cw * (sa.width / sw) - FeltUI.SafeMargin * 2f);
    }

    /// Safe area (notch / home indicator, landscape side insets) plus a 24-px margin.
    void ApplySafeAreaPad(RectTransform rt)
    {
        Rect sa = UnityEngine.Screen.safeArea;
        float w = Mathf.Max(1f, UnityEngine.Screen.width);
        float h = Mathf.Max(1f, UnityEngine.Screen.height);
        if (sa.width < 1f || sa.height < 1f) sa = new Rect(0f, 0f, w, h);
        Vector2 amin = new Vector2(sa.xMin / w, sa.yMin / h);
        Vector2 amax = new Vector2(sa.xMax / w, sa.yMax / h);
        amin = Vector2.Max(Vector2.zero, Vector2.Min(amin, Vector2.one));
        amax = Vector2.Max(amin, Vector2.Min(amax, Vector2.one));
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.offsetMin = new Vector2(FeltUI.SafeMargin, FeltUI.SafeMargin);
        rt.offsetMax = new Vector2(-FeltUI.SafeMargin, -FeltUI.SafeMargin);
        _safeSeen = sa;
        _safeW = UnityEngine.Screen.width;
        _safeH = UnityEngine.Screen.height;
        if (_searchMenu != null) _searchMenu.FitWidth(ContentWidth());
    }
}

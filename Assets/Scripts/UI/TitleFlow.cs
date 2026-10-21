using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// Boot → Title → Startup Options / Search / Play. Real screens, not placeholders.
public class TitleFlow : MonoBehaviour
{
    Canvas _canvas;
    GameObject _title;
    GameObject _options;
    GameObject _search;
    GameObject _hud;
    GameObject _pause;
    SearchMenu _searchMenu;
    Font _font;
    Text _muteBtnLabel;
    Text _infoTitle;
    Text _infoBody;
    GameObject _info;
    GameObject _loading;
    GameObject _clear;
    Text _hudLevel;
    Text _hudBeans;
    Text _clearTitle;
    Text _clearBody;
    RectTransform _progressFill;
    LevelRunner _runner;
    string _infoReturn = "options"; // title | options
    const string PrefMute = "escape.mute";
    static readonly Color Cream = new Color(0.93f, 0.88f, 0.74f);
    static readonly Color KraftBtn = new Color(0.58f, 0.42f, 0.27f, 1f);

    void Start()
    {
        EnsureEventSystem();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ApplyMute(IsMuted());
        BuildChrome();
        ShowTitle();
        if (GameSession.I != null) GameSession.I.GoTitle();
        _runner = FindFirstObjectByType<LevelRunner>();
    }

    void OnEnable()
    {
        GameSession.OnLevelClear += OnLevelClear;
        GameSession.OnBeansChanged += OnBeansChanged;
    }

    void OnDisable()
    {
        GameSession.OnLevelClear -= OnLevelClear;
        GameSession.OnBeansChanged -= OnBeansChanged;
    }

    CanvasScaler _scaler;
    bool _landscape;

    // Side-scroller runs landscape: match height there so the stacked panels (Next / Replay / Title)
    // keep their portrait spacing instead of folding onto each other in a short canvas.
    void FitScaler()
    {
        if (_scaler == null) return;
        _landscape = Screen.width > Screen.height;
        _scaler.referenceResolution = _landscape ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
        _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _scaler.matchWidthOrHeight = _landscape ? 1f : 0f;
    }

    void Update()
    {
        if (_scaler != null && (Screen.width > Screen.height) != _landscape) FitScaler();
        if (_hud == null || !_hud.activeSelf) return;
        if (_runner == null) _runner = FindFirstObjectByType<LevelRunner>();
        if (_progressFill != null && _runner != null)
        {
            var a = _progressFill.anchorMax;
            a.x = Mathf.Lerp(a.x, _runner.Progress, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            _progressFill.anchorMax = a;
        }
    }

    void OnBeansChanged(int got, int total)
    {
        if (_hudBeans != null) _hudBeans.text = total > 0 ? $"Honey beans {got}/{total}" : "";
    }

    void OnLevelClear(int id, int got, int total)
    {
        if (_clear == null) return;
        HideAll();
        var def = GameSession.I != null ? GameSession.I.Current : LevelCatalog.Make(id);
        bool last = id >= LevelCatalog.LevelCount;
        if (_clearTitle != null) _clearTitle.text = last ? "Escape complete!" : $"{def.title} cleared";
        if (_clearBody != null)
            _clearBody.text = total > 0
                ? $"Honey beans {got}/{total}\n{(last ? "Every pack hopped. Casa Rústico thanks you." : $"Next: {LevelCatalog.Make(id + 1).title}")}"
                : (last ? "Every pack hopped." : $"Next: {LevelCatalog.Make(id + 1).title}");
        _clear.SetActive(true);
        var next = _clear.transform.Find("Btn Next level");
        if (next != null) next.gameObject.SetActive(!last);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildChrome()
    {
        var root = new GameObject("UI Root");
        root.transform.SetParent(transform, false);
        _canvas = root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _scaler = scaler;
        FitScaler();
        root.AddComponent<GraphicRaycaster>();

        // Trivial SafeArea pad (no package) — insets chrome for notch/home indicator
        var safeGo = new GameObject("SafeArea");
        safeGo.transform.SetParent(root.transform, false);
        var safeRt = safeGo.AddComponent<RectTransform>();
        ApplySafeAreaPad(safeRt);
        Transform chrome = safeGo.transform;

        _title = Panel("Title Screen", chrome);
        // Title chrome is a translucent kraft wash so the felt diorama (stitched wall title) shows through — gen-5ee82b85
        var titleBg = _title.GetComponent<Image>();
        if (titleBg != null) titleBg.color = new Color(0.16f, 0.11f, 0.06f, 0.42f);
        // LAND laneC-title-size-20260917: increase title label size for readable title chrome.
        Label(_title.transform, "ESPRESSO ESCAPE", 66, new Vector2(0, 300));
        // LAND laneC-subtitle-size-20260917: increase subtitle label size for readable title chrome.
        Label(_title.transform, "Casa Rústico · handmade hop", 30, new Vector2(0, 220));
        // LAND laneC-packsline-size-20260917: increase packs line font size for readable title chrome.
        Label(_title.transform, "5 packs · Yard · Bridge · Mill · Night · Finale", 24, new Vector2(0, 160));
        // Play ≠ Options: Continue-into-play (skip Startup Options)
        Btn(_title.transform, "Play", new Vector2(0, 50), PlayNow);
        Btn(_title.transform, "Search Levels", new Vector2(0, -55), ShowSearch);
        Btn(_title.transform, "Options", new Vector2(0, -160), ShowOptions);

        _options = Panel("Startup Options", chrome);
        Label(_options.transform, "Startup Options", 48, new Vector2(0, 360));
        // LAND laneC-version-font-20260917: increase version label font for readable title chrome.
        Label(_options.transform, "v0.1 Unity WIP", 20, new Vector2(0, 320));
        Label(_options.transform, "Continue keeps skip progress · New Game resets unlocks", 22, new Vector2(0, 285));
        Btn(_options.transform, "Continue", new Vector2(0, 160), () =>
        {
            HideAll();
            if (GameSession.I != null) GameSession.I.Continue();
            ShowHud();
        });
        Btn(_options.transform, "New Game", new Vector2(0, 50), () =>
        {
            HideAll();
            if (GameSession.I != null) GameSession.I.NewGame();
            ShowHud();
        });
        // ASC stub: Mute only (AudioListener) — Sound lane owns SFX/Music assets
        MuteBtn(_options.transform, new Vector2(0, -40));
        // ASC stub item 10: Credits + Privacy + Age rating + Support overlays (no legal invent; rusticopr.com/privacy 404)
        BtnBottom(_options.transform, "Credits", 710f, ShowCredits);
        BtnBottom(_options.transform, "Privacy", 610f, ShowPrivacy);
        BtnBottom(_options.transform, "Age rating", 510f, ShowAgeRating);
        BtnBottom(_options.transform, "Support", 410f, ShowSupport);
        BtnBottom(_options.transform, "How to play", 310f, ShowHowToPlay);
        // LAND laneC-options-tip-clear-20260917: raise tips clear of the bottom-anchored buttons; held systems untouched.
        // LAND laneC-opt-tip-font-20260917: increase only the hop-lane tip font 20→22; second tip remains unchanged.
        Label(_options.transform, "Tap to hop the clear lane · weighted skip · handmade felt set", 22, new Vector2(0, -610));
        // LAND laneC-opt-packs-tip-font-20260917: increase only the Packs tip font 18→20; held systems untouched.
        Label(_options.transform, "Packs: All · Yard · Bridge · Mill · Night Roast→Roast · Finale", 20, new Vector2(0, -655));
        BtnBottom(_options.transform, "Search Levels", 210f, ShowSearch);
        // Responsive bottom-center Back button with hop-safe pad 110.
        BtnBottom(_options.transform, "Back", 110f, ShowTitle);

        _search = Panel("Search Menu", chrome);
        _searchMenu = _search.AddComponent<SearchMenu>();
        _searchMenu.Build(_search.transform, _font, OnPickLevel, ShowOptions);

        _hud = Panel("Play HUD", chrome);
        var hudBg = _hud.GetComponent<Image>();
        if (hudBg != null) { hudBg.color = new Color(0, 0, 0, 0); hudBg.raycastTarget = false; }
        // Responsive bottom anchor (not fixed Y=-780) — less clip on short canvas
        // LAND laneC-taphop-pad-20260917: raise HUD prompt bottom pad for safe-area clearance.
        // LAND laneC-taphop-font-20260917: increase HUD prompt font size for readability.
        LabelBottom(_hud.transform, "Tap anywhere to hop", 24, 120f);
        // level title + honey bean count, top-left under the safe area
        _hudLevel = TopLabel(_hud.transform, "", 30, TextAnchor.MiddleLeft, new Vector2(60, -40));
        _hudBeans = TopLabel(_hud.transform, "", 24, TextAnchor.MiddleLeft, new Vector2(60, -84));
        // stitched progress bar to the goal, top-centre
        BuildProgressBar(_hud.transform);
        // Menu opens Pause overlay (Resume + Title) — Title path preserved
        var menuBtn = SmallBtn(_hud.transform, "Menu", new Vector2(-70, -60), ShowPause);
        menuBtn.anchorMin = new Vector2(1f, 1f); menuBtn.anchorMax = new Vector2(1f, 1f); menuBtn.pivot = new Vector2(1f, 1f);

        // Level Clear overlay — unlock feedback + Next / Title
        _clear = Panel("Level Clear", chrome);
        var clearBg = _clear.GetComponent<Image>();
        if (clearBg != null) clearBg.color = new Color(0.10f, 0.07f, 0.04f, 0.80f);
        _clearTitle = MakeLabel(_clear.transform, "Cleared", 56, new Vector2(0, 220));
        _clearBody = BodyLabel(_clear.transform, "", 28, new Vector2(0, 80), new Vector2(860, 200));
        Btn(_clear.transform, "Next level", new Vector2(0, -80), () =>
        {
            EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.NextLevel(); });
        });
        Btn(_clear.transform, "Replay", new Vector2(0, -185), () =>
        {
            EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.PlayLevel(GameSession.I.currentLevelId); });
        });
        BtnBottom(_clear.transform, "Title", 110f, ShowTitle);

        _pause = Panel("Pause Overlay", chrome);
        var pauseBg = _pause.GetComponent<Image>();
        if (pauseBg != null) pauseBg.color = new Color(0.10f, 0.07f, 0.04f, 0.88f);
        // LAND laneC-paused-size-20260917: increase pause label size for readable pause state.
        Label(_pause.transform, "Paused", 56, new Vector2(0, 180));
        Btn(_pause.transform, "Resume", new Vector2(0, 40), ResumeFromPause);
        BtnBottom(_pause.transform, "Title", 110f, TitleFromPause);

        // ASC stub: simple Credits/Privacy/Age rating/Support/How to play overlay (instant show/hide; no fade/SFX/TMP)
        _info = Panel("Info Overlay", chrome);
        var infoBg = _info.GetComponent<Image>();
        if (infoBg != null) infoBg.color = new Color(0.10f, 0.07f, 0.04f, 0.94f);
        // LAND laneC-info-title-font-20260917: increase Info overlay title readability.
        _infoTitle = MakeLabel(_info.transform, "Info", 50, new Vector2(0, 360));
        _infoBody = BodyLabel(_info.transform, "", 28, new Vector2(0, 40), new Vector2(860, 420));
        BtnBottom(_info.transform, "Close", 110f, CloseInfo);

        // ASC stub item 9: brief Loading… when Title/Search → Play (instant; no fade/spinner/TMP) [land: laneC-loading-size-20260917]
        _loading = Panel("Loading Overlay", chrome);
        var loadBg = _loading.GetComponent<Image>();
        if (loadBg != null) loadBg.color = new Color(0.10f, 0.07f, 0.04f, 0.94f);
        Label(_loading.transform, "Loading…", 52, Vector2.zero);
    }

    void PlayNow()
    {
        EnterPlayFromMenu(() =>
        {
            if (GameSession.I != null) GameSession.I.Continue();
        });
    }

    void ShowPause()
    {
        if (_pause == null) { ShowTitle(); return; }
        Time.timeScale = 0f;
        _pause.SetActive(true);
        // keep HUD under overlay so Resume returns to play chrome
        if (_hud != null) _hud.SetActive(true);
        if (_title != null) _title.SetActive(false);
        if (_options != null) _options.SetActive(false);
        if (_search != null) _search.SetActive(false);
    }

    void ResumeFromPause()
    {
        Time.timeScale = 1f;
        if (_pause != null) _pause.SetActive(false);
        ShowHud();
    }

    void TitleFromPause()
    {
        Time.timeScale = 1f;
        if (_pause != null) _pause.SetActive(false);
        ShowTitle();
    }

    void OnPickLevel(int id)
    {
        EnterPlayFromMenu(() =>
        {
            if (GameSession.I != null) GameSession.I.PlayLevel(id);
        });
    }

    // Title Play / Search pick → brief Loading… then HUD next frame (no fade/asset thrash)
    void EnterPlayFromMenu(System.Action startPlay)
    {
        HideAll();
        Time.timeScale = 1f;
        if (_loading != null) _loading.SetActive(true);
        startPlay?.Invoke();
        StartCoroutine(RevealHudNextFrame());
    }

    IEnumerator RevealHudNextFrame()
    {
        yield return null; // HUD ready / next frame
        if (_loading != null) _loading.SetActive(false);
        ShowHud();
    }

    void HideAll()
    {
        _title.SetActive(false);
        _options.SetActive(false);
        _search.SetActive(false);
        _hud.SetActive(false);
        if (_pause != null) _pause.SetActive(false);
        if (_info != null) _info.SetActive(false);
        if (_loading != null) _loading.SetActive(false);
        if (_clear != null) _clear.SetActive(false);
    }

    public void ShowTitle()
    {
        Time.timeScale = 1f;
        HideAll();
        _title.SetActive(true);
        if (GameSession.I != null) GameSession.I.GoTitle();
    }

    public void ShowOptions()
    {
        HideAll();
        _options.SetActive(true);
        if (GameSession.I != null) GameSession.I.GoOptions();
    }

    public void ShowSearch()
    {
        HideAll();
        _search.SetActive(true);
        if (_searchMenu != null) _searchMenu.Refresh("");
        if (GameSession.I != null) GameSession.I.GoSearch();
    }

    void ShowHud()
    {
        HideAll();
        _hud.SetActive(true);
        if (GameSession.I != null)
        {
            var def = GameSession.I.Current;
            if (_hudLevel != null) _hudLevel.text = $"{def.id}. {def.title}";
            OnBeansChanged(GameSession.I.beansCollected, GameSession.I.beansTotal);
        }
        if (_progressFill != null) { var a = _progressFill.anchorMax; a.x = 0f; _progressFill.anchorMax = a; }
    }

    Text TopLabel(Transform parent, string text, int size, TextAnchor align, Vector2 pos)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(700, size + 16);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Cream;
        t.raycastTarget = false;
        return t;
    }

    void BuildProgressBar(Transform parent)
    {
        var track = new GameObject("Progress Track");
        track.transform.SetParent(parent, false);
        var trt = track.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(520, 14);
        trt.anchoredPosition = new Vector2(0, -150);
        var timg = track.AddComponent<Image>();
        timg.color = new Color(0.30f, 0.22f, 0.14f, 0.75f);
        timg.raycastTarget = false;
        var fill = new GameObject("Progress Fill");
        fill.transform.SetParent(track.transform, false);
        _progressFill = fill.AddComponent<RectTransform>();
        _progressFill.anchorMin = Vector2.zero;
        _progressFill.anchorMax = new Vector2(0f, 1f);
        _progressFill.offsetMin = new Vector2(2, 2);
        _progressFill.offsetMax = new Vector2(-2, -2);
        var fimg = fill.AddComponent<Image>();
        fimg.color = Cream;
        fimg.raycastTarget = false;
        // goal bead at the end of the stitch
        var bead = new GameObject("Goal Bead");
        bead.transform.SetParent(track.transform, false);
        var brt = bead.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0.5f);
        brt.anchorMax = new Vector2(1f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(26, 26);
        var bimg = bead.AddComponent<Image>();
        bimg.color = new Color(0.86f, 0.68f, 0.28f);
        bimg.raycastTarget = false;
    }

    RectTransform SmallBtn(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn " + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 76);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(KraftBtn.r, KraftBtn.g, KraftBtn.b, 0.92f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        btn.onClick.AddListener(() => EscapeAudio.Play(EscapeAudio.Sfx.Tap, 0.6f));
        var label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        Stretch(lrt);
        var t = label.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = 30;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return rt;
    }

    GameObject Panel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Stretch(rt);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.11f, 0.06f, 0.96f); // kraft dark wash p9
        go.SetActive(false);
        return go;
    }

    void Label(Transform parent, string text, int size, Vector2 pos)
    {
        MakeLabel(parent, text, size, pos);
    }

    Text MakeLabel(Transform parent, string text, int size, Vector2 pos)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, size + 20);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.93f, 0.88f, 0.74f);
        t.raycastTarget = false; // labels never eat hop taps
        return t;
    }

    // Bottom-center responsive label — survives short/tall aspect without densify
    void LabelBottom(Transform parent, string text, int size, float bottomPad)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900, size + 20);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomPad);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.93f, 0.88f, 0.74f);
        t.raycastTarget = false;
    }

    // Bottom-center responsive button — keeps the hit target inside short canvases.
    void BtnBottom(Transform parent, string text, float bottomPad, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn " + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(540, 92);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomPad);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.58f, 0.42f, 0.27f, 1f); // p9 kraft button
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        Stretch(lrt);
        var t = label.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = 34;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
    }

    static void ApplySafeAreaPad(RectTransform rt)
    {
        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1f, Screen.width);
        float h = Mathf.Max(1f, Screen.height);
        Vector2 amin = new Vector2(sa.xMin / w, sa.yMin / h);
        Vector2 amax = new Vector2(sa.xMax / w, sa.yMax / h);
        amin = Vector2.Max(Vector2.zero, Vector2.Min(amin, Vector2.one));
        amax = Vector2.Max(amin, Vector2.Min(amax, Vector2.one));
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void ShowCredits()
    {
        _infoReturn = "options";
        OpenInfo(
            "Credits",
            "Espresso Escape\nCasa Rústico\n\nCredits placeholder.");
    }

    void ShowPrivacy()
    {
        _infoReturn = "options";
        // rusticopr.com/privacy returned 404 (2026-09-14) — in-app stub only; no invented legal copy
        OpenInfo(
            "Privacy",
            "Privacy policy coming soon");
    }

    void ShowAgeRating()
    {
        _infoReturn = "options";
        // ASC stub — no invented legal / IARC copy; body is TBD placeholder only
        OpenInfo(
            "Age rating",
            "Age rating: TBD");
    }

    void ShowSupport()
    {
        _infoReturn = "options";
        // ASC contact email (git author / corp) preferred over rusticopr.com Support URL stub
        OpenInfo(
            "Support",
            "Support: casarusticocorp@gmail.com");
    }

    void ShowHowToPlay()
    {
        _infoReturn = "options";
        OpenInfo(
            "How to play",
            "Tap anywhere to hop. Every hop is the same weighted skip.\nRide the burlap lane and the kraft bridge to the gold goal.\nCollect the honey beans floating in your hop arc.");
    }

    void OpenInfo(string title, string body)
    {
        if (_info == null) return;
        // keep Startup Options under overlay so Close returns there
        if (_options != null) _options.SetActive(true);
        if (_title != null) _title.SetActive(false);
        if (_search != null) _search.SetActive(false);
        if (_hud != null) _hud.SetActive(false);
        if (_pause != null) _pause.SetActive(false);
        if (_infoTitle != null) _infoTitle.text = title;
        if (_infoBody != null) _infoBody.text = body;
        _info.SetActive(true);
    }

    void CloseInfo()
    {
        if (_info != null) _info.SetActive(false);
        if (_infoReturn == "title") ShowTitle();
        else ShowOptions();
    }

    Text BodyLabel(Transform parent, string text, int size, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject("Body");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.93f, 0.88f, 0.74f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static bool IsMuted() => PlayerPrefs.GetInt(PrefMute, 0) != 0;

    static string MuteLabel(bool muted) => muted ? "Mute: On" : "Mute: Off";

    static void ApplyMute(bool muted)
    {
        AudioListener.volume = muted ? 0f : 1f;
    }

    void ToggleMute()
    {
        bool muted = !IsMuted();
        PlayerPrefs.SetInt(PrefMute, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute(muted);
        if (_muteBtnLabel != null) _muteBtnLabel.text = MuteLabel(muted);
    }

    void MuteBtn(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Btn Mute");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(540, 92);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.58f, 0.42f, 0.27f, 1f); // p9 kraft button
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleMute);
        var label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        Stretch(lrt);
        var t = label.AddComponent<Text>();
        t.text = MuteLabel(IsMuted());
        t.font = _font;
        t.fontSize = 34;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        _muteBtnLabel = t;
    }

    void Btn(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn " + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(540, 92);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.58f, 0.42f, 0.27f, 1f); // p9 kraft button
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        Stretch(lrt);
        var t = label.AddComponent<Text>();
        t.text = text;
        t.font = _font;
        t.fontSize = 34;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

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
    Button _nextLevelBtn;
    RectTransform _hudLevelRt;
    RectTransform _hudBeanRt;
    RectTransform _menuRt;
    RectTransform _progressRt;
    RectTransform _progressFill;
    LevelRunner _runner;
    Image _mutePlate;
    string _infoReturn = "options"; // title | options
    const string PrefMute = "escape.mute";
    static readonly Color Cream = FeltUI.FeltCream;
    static readonly Color KraftBtn = FeltUI.KraftTan;

    void Start()
    {
        EnsureEventSystem();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ApplyMute(IsMuted());
        BuildChrome();
        ShowTitle();
        if (GameSession.I != null) GameSession.I.GoTitle();
        _runner = FindAnyObjectByType<LevelRunner>();
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
    RectTransform _safe;
    Rect _safeSeen;
    int _safeW, _safeH;
    bool _dollyOn;

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
        if (!_dollyOn)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<TitleDolly>() == null) cam.gameObject.AddComponent<TitleDolly>();
                _dollyOn = true;
            }
        }
        if (_safe != null && (_safeW != Screen.width || _safeH != Screen.height || _safeSeen != Screen.safeArea))
            ApplySafeAreaPad(_safe);
        if (_scaler != null && (Screen.width > Screen.height) != _landscape) FitScaler();
        if (_hud == null || !_hud.activeSelf) return;
        if (_runner == null) _runner = FindAnyObjectByType<LevelRunner>();
        if (_progressFill != null && _runner != null)
        {
            var a = _progressFill.anchorMax;
            a.x = Mathf.Lerp(a.x, _runner.Progress, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            _progressFill.anchorMax = a;
        }
    }

    void OnBeansChanged(int got, int total)
    {
        if (_hudBeans != null) _hudBeans.text = total > 0 ? $"{got} / {total}" : "";
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
        // Last level has nowhere to go, but Next stays on screen so the three plates cannot collapse onto each other.
        if (_nextLevelBtn != null)
        {
            _nextLevelBtn.interactable = !last;
            if (last)
            {
                var cb = _nextLevelBtn.colors;
                cb.disabledColor = new Color(0.82f, 0.82f, 0.82f, 1f);
                _nextLevelBtn.colors = cb;
            }
        }
        Rebuild(_clear);
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
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
        _safe = safeRt;
        ApplySafeAreaPad(safeRt);
        Transform chrome = safeGo.transform;

        _title = Panel("Title Screen", chrome);
        // Yarn ESPRESSO / ESCAPE / Casa Rústico is the 3D sign (FeltTitle). This screen adds no second title.
        // Phone references keep the burlap open and put the controls on the floor. Plates, top to bottom:
        // Continue / New game / Levels / Options.
        var titleBg = _title.GetComponent<Image>();
        if (titleBg != null) { titleBg.color = new Color(0f, 0f, 0f, 0f); titleBg.raycastTarget = false; }
        float step = FeltUI.ButtonHeight + FeltUI.Gap; // 68
        BtnBottom(_title.transform, "Continue", 3f * step, PlayNow, FeltUI.PompomRed, Cream);
        BtnBottom(_title.transform, "New game", 2f * step, () =>
        {
            EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.NewGame(); });
        });
        BtnBottom(_title.transform, "Levels", 1f * step, ShowSearch);
        BtnBottom(_title.transform, "Options", 0f, ShowOptions, FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast);

        _options = Panel("Startup Options", chrome);
        // Bottom of the safe area, same floor as Back on the other screens. Continue / New game / Levels are not repeated here.
        var optCol = FeltUI.Column(_options.transform, "Options Column", 520f, true);
        StackLabel(optCol, "Options", 40, 56f);
        MuteBtn(optCol, Vector2.zero);
        StackBtn(optCol, "How to play", ShowHowToPlay, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(optCol, "Credits", ShowCredits, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(optCol, "Privacy", ShowPrivacy, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(optCol, "Age rating", ShowAgeRating, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(optCol, "Support", ShowSupport, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(optCol, "Back", ShowTitle, FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast);

        _search = Panel("Search Menu", chrome);
        _searchMenu = _search.AddComponent<SearchMenu>();
        _searchMenu.Build(_search.transform, _font, OnPickLevel, ShowTitle);

        _hud = Panel("Play HUD", chrome);
        var hudBg = _hud.GetComponent<Image>();
        if (hudBg != null) { hudBg.color = new Color(0, 0, 0, 0); hudBg.raycastTarget = false; }
        // top-left: level title chip. Width is fitted in LayoutHud so it cannot meet the honey chip.
        _hudLevel = Chip(_hud.transform, "Level Chip", new Vector2(0f, 1f), Vector2.zero, new Vector2(380, 48), "", 26, TextAnchor.MiddleLeft, 22f);
        _hudLevelRt = _hudLevel.transform.parent as RectTransform;
        // top-centre: honey-bean counter with a tiny egg icon (pickup colour)
        var counter = Chip(_hud.transform, "Bean Chip", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(190, 48), "", 26, TextAnchor.MiddleLeft, 62f);
        _hudBeans = counter;
        _hudBeanRt = counter.transform.parent as RectTransform;
        var eggGo = new GameObject("Egg Icon");
        eggGo.transform.SetParent(counter.transform.parent, false);
        var eggRt = eggGo.AddComponent<RectTransform>();
        FeltUI.Anchor(eggRt, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(24, 29));
        var eggImg = eggGo.AddComponent<Image>();
        eggImg.sprite = FeltUI.Egg(); eggImg.color = FeltUI.Honey; eggImg.raycastTarget = false; eggImg.preserveAspect = true;
        // stitched progress bar to the goal, under the counter
        BuildProgressBar(_hud.transform);
        // top-right: Menu opens Pause overlay (Resume + Title) — Title path preserved
        _menuRt = SmallBtn(_hud.transform, "Menu", Vector2.zero, ShowPause);

        // Level Clear — one bottom column so Next / Replay / Title each keep their own plate.
        _clear = Panel("Level Clear", chrome);
        var clearBg = _clear.GetComponent<Image>();
        if (clearBg != null) clearBg.color = new Color(0.10f, 0.07f, 0.04f, 0.80f);
        var clearCol = FeltUI.Column(_clear.transform, "Clear Column", 520f, true);
        _clearTitle = StackLabel(clearCol, "Cleared", 36, 52f);
        _clearBody = StackBody(clearCol, "", 22, 84f);
        _nextLevelBtn = StackBtn(clearCol, "Next", () =>
        {
            EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.NextLevel(); });
        }, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(clearCol, "Replay", () =>
        {
            EnterPlayFromMenu(() => { if (GameSession.I != null) GameSession.I.PlayLevel(GameSession.I.currentLevelId); });
        }, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(clearCol, "Title", ShowTitle, FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast);

        _pause = Panel("Pause Overlay", chrome);
        var pauseBg = _pause.GetComponent<Image>();
        if (pauseBg != null) pauseBg.color = new Color(0.10f, 0.07f, 0.04f, 0.88f);
        var pauseCol = FeltUI.Column(_pause.transform, "Pause Column", 520f, false);
        StackLabel(pauseCol, "Paused", 48, 64f);
        StackBtn(pauseCol, "Resume", ResumeFromPause, FeltUI.With(KraftBtn, 0.92f), Cream);
        StackBtn(pauseCol, "Title", TitleFromPause, FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast);

        // Credits / Privacy / Age rating / Support / How to play. Close sits under the body, not on it.
        _info = Panel("Info Overlay", chrome);
        var infoBg = _info.GetComponent<Image>();
        if (infoBg != null) infoBg.color = new Color(0.10f, 0.07f, 0.04f, 0.94f);
        var infoCol = FeltUI.Column(_info.transform, "Info Column", 640f, false);
        _infoTitle = StackLabel(infoCol, "Info", 40, 56f);
        _infoBody = StackBody(infoCol, "", 26, 300f);
        StackBtn(infoCol, "Close", CloseInfo, FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast);

        // ASC stub item 9: brief Loading… when Title/Search → Play (instant; no fade/spinner/TMP) [land: laneC-loading-size-20260917]
        _loading = Panel("Loading Overlay", chrome);
        var loadBg = _loading.GetComponent<Image>();
        if (loadBg != null) loadBg.color = new Color(0.10f, 0.07f, 0.04f, 0.94f);
        Label(_loading.transform, "Loading…", 52, Vector2.zero);
        LayoutHud(ContentWidth());
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
        Rebuild(_pause);
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
        Rebuild(_options);
        if (GameSession.I != null) GameSession.I.GoOptions();
    }

    public void ShowSearch()
    {
        HideAll();
        _search.SetActive(true);
        if (_searchMenu != null) _searchMenu.Refresh();
        Rebuild(_search);
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

    /// Rounded translucent dark-roast chip anchored to a HUD corner/edge, with a cream label (never a raycast target).
    Text Chip(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, string text, int fontSize, TextAnchor align, float padLeft)
    {
        var plate = FeltUI.PlateImage(parent, name, FeltUI.With(FeltUI.DarkRoast, 0.58f), false);
        FeltUI.Anchor(plate.rectTransform, anchor, offset, size);
        var t = FeltUI.Label(plate.transform, text, _font, fontSize, Cream, align);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.rectTransform.offsetMin = new Vector2(padLeft, 0f);
        t.rectTransform.offsetMax = new Vector2(-18f, 0f);
        return t;
    }

    // Top row: level chip | honey count | Menu, with the progress stitch under the honey chip.
    // Widths shrink so a landscape notch cannot push one plate onto another.
    void LayoutHud(float w)
    {
        if (_hudLevelRt == null || w < 1f) return;
        float gap = 16f;
        float menuW = 168f;
        float honeyW = 188f;
        float levelW = w * 0.5f - honeyW * 0.5f - gap;
        levelW = Mathf.Clamp(levelW, 120f, 420f);
        float honeyRightGap = w * 0.5f - honeyW * 0.5f - menuW;
        if (levelW + honeyW + menuW + gap * 2f > w || honeyRightGap < gap)
        {
            float budget = Mathf.Max(300f, w - gap * 2f);
            menuW = Mathf.Clamp(budget * 0.26f, 120f, 168f);
            honeyW = Mathf.Clamp(budget * 0.30f, 140f, 188f);
            levelW = Mathf.Max(120f, budget - menuW - honeyW);
        }
        FeltUI.Anchor(_hudLevelRt, new Vector2(0f, 1f), Vector2.zero, new Vector2(levelW, 48f));
        if (_hudBeanRt != null)
            FeltUI.Anchor(_hudBeanRt, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(honeyW, 48f));
        if (_menuRt != null)
            FeltUI.Anchor(_menuRt, new Vector2(1f, 1f), Vector2.zero, new Vector2(menuW, FeltUI.ButtonHeight));
        if (_progressRt != null)
        {
            float barW = Mathf.Min(480f, Mathf.Max(120f, w - 2f * (menuW + gap)));
            _progressRt.anchorMin = new Vector2(0.5f, 1f);
            _progressRt.anchorMax = new Vector2(0.5f, 1f);
            _progressRt.pivot = new Vector2(0.5f, 1f);
            _progressRt.sizeDelta = new Vector2(barW, 14f);
            _progressRt.anchoredPosition = new Vector2(0f, -72f);
        }
    }

    float ContentWidth()
    {
        CanvasUnits(out float cw, out _);
        float sw = Mathf.Max(1f, Screen.width);
        Rect sa = Screen.safeArea;
        if (sa.width < 1f) sa = new Rect(0f, 0f, sw, Mathf.Max(1f, Screen.height));
        return Mathf.Max(320f, cw * (sa.width / sw) - FeltUI.SafeMargin * 2f);
    }

    static void CanvasUnits(out float w, out float h)
    {
        float sw = Mathf.Max(1f, Screen.width);
        float sh = Mathf.Max(1f, Screen.height);
        if (sw > sh) { h = 1080f; w = 1080f * sw / sh; }
        else { w = 1080f; h = 1080f * sh / sw; }
    }

    static void Rebuild(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(go.transform as RectTransform);
    }

    Text StackLabel(Transform parent, string text, int size, float height)
    {
        var t = MakeLabel(parent, text, size, Vector2.zero);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 1f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    Text StackBody(Transform parent, string text, int size, float height)
    {
        var t = BodyLabel(parent, text, size, Vector2.zero, new Vector2(680f, height));
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 1f;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    Button StackBtn(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Color plate, Color textColor)
    {
        return FeltUI.Button(parent, text, _font, plate, textColor, onClick, 480f, FeltUI.ButtonHeight, 28);
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
        trt.anchoredPosition = new Vector2(0, -72); // under the top chips, clear of the Menu plate
        var timg = track.AddComponent<Image>();
        timg.sprite = FeltUI.Plate(); timg.type = Image.Type.Sliced; timg.pixelsPerUnitMultiplier = 3f;
        timg.color = FeltUI.With(FeltUI.DarkRoast, 0.60f);
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
        _progressRt = trt;
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

    // Compact HUD button (Menu): same 56-px felt plate, narrower.
    RectTransform SmallBtn(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var btn = FeltUI.Button(parent, text, _font, FeltUI.With(KraftBtn, 0.90f), Cream, onClick, 170f, FeltUI.ButtonHeight, 26);
        var rt = (RectTransform)btn.transform;
        rt.anchoredPosition = pos;
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

    // Bottom-center responsive button (56-px rounded felt plate) — bottomPad is measured from the 24-px safe margin.
    void BtnBottom(Transform parent, string text, float bottomPad, UnityEngine.Events.UnityAction onClick)
    {
        BtnBottom(parent, text, bottomPad, onClick, FeltUI.With(KraftBtn, 0.92f), Cream);
    }

    void BtnBottom(Transform parent, string text, float bottomPad, UnityEngine.Events.UnityAction onClick, Color plate, Color textColor)
    {
        var btn = FeltUI.Button(parent, text, _font, plate, textColor, onClick);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomPad);
    }

    // Safe area (notch / home indicator, including landscape side insets) plus a 24-px margin, in canvas units.
    void ApplySafeAreaPad(RectTransform rt)
    {
        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1f, Screen.width);
        float h = Mathf.Max(1f, Screen.height);
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
        _safeW = Screen.width;
        _safeH = Screen.height;
        CanvasUnits(out float cw, out _);
        LayoutHud(Mathf.Max(320f, cw * (amax.x - amin.x) - FeltUI.SafeMargin * 2f));
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
            "Hold to run. The right half runs forward, the left half walks back.\nTap or swipe up to jump. Release early for a shorter hop.\nKeyboard: A / D or arrows to move, Space to jump.\nCross to the goal and collect the honey beans.");
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
        Rebuild(_info);
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
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.raycastTarget = false;
        return t;
    }

    static bool IsMuted() => PlayerPrefs.GetInt(PrefMute, 0) != 0;

    static string MuteLabel() => "Mute";

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
        SyncMutePlate();
    }

    void MuteBtn(Transform parent, Vector2 pos)
    {
        var btn = FeltUI.Button(parent, "Mute", _font, FeltUI.With(FeltUI.Olive, 0.92f), Cream, ToggleMute);
        btn.name = "Btn Mute";
        ((RectTransform)btn.transform).anchoredPosition = pos;
        _mutePlate = btn.GetComponent<Image>();
        _muteBtnLabel = btn.GetComponentInChildren<Text>();
        SyncMutePlate();
    }

    // The options row is the word Mute. Olive while sound is on, dark roast while it is muted.
    void SyncMutePlate()
    {
        if (_muteBtnLabel != null) _muteBtnLabel.text = MuteLabel();
        if (_mutePlate != null)
            _mutePlate.color = IsMuted()
                ? FeltUI.With(FeltUI.DarkRoast, 0.92f)
                : FeltUI.With(FeltUI.Olive, 0.92f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

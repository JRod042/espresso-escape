using System;
using UnityEngine;
using UnityEngine.UI;

/// Polished level search/browse for 5 levels — filter by id, title, pack.
public class SearchMenu : MonoBehaviour
{
    Font _font;
    InputField _input;
    Transform _listRoot;
    Action<int> _onPick;
    Action _onBack;
    Text _meta;
    Image[] _chipImgs;
    string[] _chipNames;
    string _packQ;

    public void Build(Transform parent, Font font, Action<int> onPick, Action onBack)
    {
        _font = font;
        _onPick = onPick;
        _onBack = onBack;

        var title = MakeText(parent, "Search Levels", 44, new Vector2(0, 780));
        title.color = new Color(0.93f, 0.88f, 0.74f);

        // Search field
        var fieldGo = new GameObject("Search Field");
        fieldGo.transform.SetParent(parent, false);
        var frt = fieldGo.AddComponent<RectTransform>();
        frt.sizeDelta = new Vector2(860, 72);
        frt.anchoredPosition = new Vector2(0, 680);
        var fimg = fieldGo.AddComponent<Image>();
        fimg.color = new Color(0.35f, 0.25f, 0.16f, 1f);
        _input = fieldGo.AddComponent<InputField>();
        var placeholder = MakeText(fieldGo.transform, "Search · All Yard Bridge Mill Night(=Roast) Finale", 24, Vector2.zero);
        placeholder.color = new Color(1, 1, 1, 0.45f);
        var text = MakeText(fieldGo.transform, "", 28, Vector2.zero);
        text.color = Color.white;
        text.supportRichText = false;
        _input.textComponent = text;
        _input.placeholder = placeholder;
        _input.onValueChanged.AddListener(Refresh);

        _meta = MakeText(parent, "", 22, new Vector2(0, 560));
        _meta.color = new Color(0.85f, 0.78f, 0.65f);
        // k494 Lane C: pack quick-filter selected-state LAND
        var chips = new GameObject("PackChips");
        chips.transform.SetParent(parent, false);
        var crtChips = chips.AddComponent<RectTransform>();
        crtChips.sizeDelta = new Vector2(980, 56);
        crtChips.anchoredPosition = new Vector2(0, 620);
        var hlg = chips.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        // Lane C gapfix: catalog L4 pack="Roast" — chip label Night, filter Roast (not "Night")
        string[] chipLabels = { "All", "Yard", "Bridge", "Mill", "Night Roast", "Finale" };
        string[] chipFilters = { "", "Yard", "Bridge", "Mill", "Roast", "Finale" };
        _chipNames = chipLabels;
        _chipImgs = new Image[chipLabels.Length];
        int ci = 0;
        for (int pi = 0; pi < chipLabels.Length; pi++)
        {
            string p = chipLabels[pi];
            string filt = chipFilters[pi];
            var chip = new GameObject("Chip_" + p);
            chip.transform.SetParent(chips.transform, false);
            var cle = chip.AddComponent<LayoutElement>();
            bool wide = p.Length > 8;
            cle.minWidth = wide ? 168 : 122; cle.preferredWidth = wide ? 196 : 142; cle.minHeight = 54; // Night Roast wider
            var cimg = chip.AddComponent<Image>();
            cimg.color = new Color(0.40f, 0.28f, 0.16f, 0.96f);
            _chipImgs[ci] = cimg;
            int idx = ci;
            ci++;
            var cbtn = chip.AddComponent<Button>();
            cbtn.targetGraphic = cimg;
            cbtn.onClick.AddListener(() => {
                _packQ = filt;
                if (_input != null) _input.text = _packQ;
                TintChips(idx);
                Refresh(_input != null ? _input.text : "");
            });
            var ct = MakeText(chip.transform, p, 22, Vector2.zero);
            ct.color = new Color(0.96f, 0.92f, 0.82f);
            Stretch(ct.rectTransform);
        }
        TintChips(0); // k511 LAND Lane C: All chip selected on open


        // Scroll list
        var scrollGo = new GameObject("Level List");
        scrollGo.transform.SetParent(parent, false);
        var srt = scrollGo.AddComponent<RectTransform>();
        srt.sizeDelta = new Vector2(920, 1100);
        srt.anchoredPosition = new Vector2(0, -40);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vrt = viewport.AddComponent<RectTransform>();
        Stretch(vrt);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        _listRoot = content.transform;
        var crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = new Vector2(0, 2000);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 12; // laneC-search-spacing-20260917
        vlg.padding = new RectOffset(14, 14, 14, 14); // laneC-search-padrect-20260917
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = vrt;
        scroll.content = crt;
        scroll.horizontal = false;

        var back = new GameObject("Back");
        back.transform.SetParent(parent, false);
        var brt = back.AddComponent<RectTransform>();
        brt.sizeDelta = new Vector2(360, 80);
        // Lane C: responsive bottom-center Back button with hop-safe pad 110.
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 110f);
        var bimg = back.AddComponent<Image>();
        bimg.color = new Color(0.55f, 0.40f, 0.27f);
        var bbtn = back.AddComponent<Button>();
        bbtn.targetGraphic = bimg;
        bbtn.onClick.AddListener(() => _onBack?.Invoke());
        var bt = MakeText(back.transform, "Back", 32, Vector2.zero);
        bt.color = Color.white;
    }

    void TintChips(int sel)
    {
        if (_chipImgs == null) return;
        for (int i = 0; i < _chipImgs.Length; i++)
        {
            if (_chipImgs[i] == null) continue;
            _chipImgs[i].color = (i == sel)
                ? new Color(0.72f, 0.50f, 0.26f, 1f) // p8 selected PackChip
                : new Color(0.38f, 0.26f, 0.16f, 0.95f);
        }
    }

    public void Refresh(string query)
    {
        if (_listRoot == null) return;
        for (int i = _listRoot.childCount - 1; i >= 0; i--)
            Destroy(_listRoot.GetChild(i).gameObject);

        LevelCatalog cat = null;
        if (GameSession.I != null) cat = GameSession.I.catalog;
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<LevelCatalog>();
            cat.RebuildAll();
        }
        var hits = cat.Search(query);
        int unlocked = PlayerProgress.Unlocked;
        // p8: Night chip filter is Roast — surface that in meta when exact-pack hits
        bool roastPackChip = !string.IsNullOrEmpty(query)
            && query.Trim().Equals("Roast", System.StringComparison.OrdinalIgnoreCase)
            && hits.Length == 1 && hits[0].pack == "Roast";
        if (string.IsNullOrEmpty(query))
            _meta.text = $"{LevelCatalog.LevelCount} levels · unlocked through {unlocked}";
        else if (roastPackChip)
            _meta.text = $"Night → Roast pack · 1 match · unlocked through {unlocked}";
        else
            _meta.text = $"{hits.Length} match(es) · unlocked through {unlocked}";

        if (hits.Length == 0)
        {
            var empty = new GameObject("No levels match");
            empty.transform.SetParent(_listRoot, false);
            var emptyLayout = empty.AddComponent<LayoutElement>();
            emptyLayout.minHeight = 88;
            emptyLayout.preferredHeight = 88;
            var emptyLabel = MakeText(empty.transform, "No levels match", 26, Vector2.zero);
            emptyLabel.color = new Color(0.96f, 0.92f, 0.82f);
            Stretch(emptyLabel.rectTransform);
        }

        for (int i = 0; i < hits.Length; i++)
        {
            var L = hits[i];
            bool locked = L.id > unlocked;
            var row = new GameObject($"L{L.id}");
            row.transform.SetParent(_listRoot, false);
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 90; // laneC-search-rowh-20260917
            le.preferredHeight = 90; // laneC-search-rowh-20260917
            var img = row.AddComponent<Image>();
            img.color = locked
                ? new Color(0.25f, 0.18f, 0.12f, 0.9f)
                : new Color(0.45f, 0.32f, 0.20f, 0.95f);
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = !locked;
            int id = L.id;
            btn.onClick.AddListener(() => _onPick?.Invoke(id));
            // L4/L5 densities are player-ready now; the internal "polish HOLD" badge is gone from player UI
            string rowLabel = locked
                ? $"[LOCK] {L.id}. {L.title}  ·  {L.pack}"
                : $"{L.id}. {L.title}  ·  {L.pack}";
            var label = MakeText(row.transform, rowLabel, 26, Vector2.zero);
            label.color = locked ? new Color(1, 1, 1, 0.4f) : new Color(0.96f, 0.92f, 0.82f);
            Stretch(label.rectTransform);
        }
    }

    Text MakeText(Transform parent, string s, int size, Vector2 pos)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(860, size + 16);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = s;
        t.font = _font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

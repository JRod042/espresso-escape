using System;
using UnityEngine;
using UnityEngine.UI;

/// Levels screen: five glass level cards (art, name, mood, stars, best time, lock), then Back.
/// Café Yard, Kraft Bridge, Mill Path, Night Roast, Finale Escape. Locked cards stay visible but cannot be picked.
public class SearchMenu : MonoBehaviour
{
    const float CardW = 248f;
    const float CardH = 318f;
    const float ArtH = 140f;
    const float CardGap = 16f;
    const int CardPad = 14;

    Action<int> _onPick;
    Button[] _cards;
    Image[][] _stars;
    Text[] _best;
    GameObject[] _locks;
    RawImage[] _arts;
    LayoutElement[] _cardSize;
    RectTransform _panel;
    Text _total;

    public void Build(Transform parent, Font font, Action<int> onPick, Action onBack)
    {
        _onPick = onPick;
        int n = LevelCatalog.LevelCount;
        _cards = new Button[n];
        _stars = new Image[n][];
        _best = new Text[n];
        _locks = new GameObject[n];
        _arts = new RawImage[n];
        _cardSize = new LayoutElement[n];

        var card = GlassUI.Card(parent, "Levels Card");
        _panel = card.rectTransform;
        _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0.5f);
        GlassUI.Stack(_panel, 24, 16f);
        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var head = GlassUI.Row(_panel, "Heading", 52f, 12f);
        GlassUI.IconImage(head.transform, "ph-list-bullets", 34f, GlassUI.Ink);
        var title = GlassUI.Label(head.transform, "Levels", 40, GlassUI.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GlassUI.Size(title, 0f, 52f, 1f);
        GlassUI.IconImage(head.transform, "ph-star-fill", 28f, GlassUI.Ink);
        _total = GlassUI.Label(head.transform, "", 28, GlassUI.Ink, TextAnchor.MiddleRight);
        GlassUI.Size(_total, 90f, 52f);

        var row = GlassUI.Row(_panel, "Cards", CardH, CardGap);
        for (int id = 1; id <= n; id++) BuildCard(row.transform, id);

        var back = GlassUI.Button(_panel, "Back", "ph-arrow-left", () => onBack?.Invoke(), false, default, 260f);
        back.name = "Btn Back";
        var ble = back.GetComponent<LayoutElement>();
        ble.flexibleWidth = 0f;
        Refresh();
    }

    void BuildCard(Transform row, int id)
    {
        int i = id - 1;
        var def = LevelCatalog.Make(id);
        var img = GlassUI.Card(row, "Btn " + def.title, 22f);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        int picked = id;
        btn.onClick.AddListener(() =>
        {
            if (picked > PlayerProgress.Unlocked) { EscapeAudio.Play(EscapeAudio.Sfx.Locked, 0.4f); return; }
            EscapeAudio.Play(EscapeAudio.Sfx.Tap, 0.6f);
            _onPick?.Invoke(picked);
        });
        img.gameObject.AddComponent<ButtonPress>();
        _cardSize[i] = GlassUI.Size(img, CardW, CardH);
        _cards[i] = btn;
        GlassUI.Stack(img.rectTransform, CardPad, 6f);

        var artRt = GlassUI.Rect(img.transform, "Art");
        var art = artRt.gameObject.AddComponent<RawImage>();
        art.texture = GlassUI.Art("level-" + id);
        art.raycastTarget = false;
        GlassUI.Size(art, 0f, ArtH, 1f);
        _arts[i] = art;

        var name = GlassUI.Label(img.transform, id + ". " + def.title, 24, GlassUI.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
        GlassUI.Size(name, 0f, 32f, 1f);
        var mood = GlassUI.Label(img.transform, def.mood, 18, GlassUI.InkSoft, TextAnchor.MiddleLeft);
        GlassUI.Size(mood, 0f, 24f, 1f);

        var stars = GlassUI.Row(img.transform, "Stars", 30f, 4f);
        stars.childAlignment = TextAnchor.MiddleLeft;
        _stars[i] = new Image[3];
        for (int s = 0; s < 3; s++) _stars[i][s] = GlassUI.IconImage(stars.transform, "ph-star", 26f, GlassUI.Ink);
        var best = GlassUI.Label(stars.transform, "", 18, GlassUI.InkSoft, TextAnchor.MiddleRight);
        GlassUI.Size(best, 0f, 30f, 1f);
        _best[i] = best;

        // lock badge over the art (ignored by the layout)
        var lockRt = GlassUI.Rect(img.transform, "Lock");
        lockRt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        lockRt.anchorMin = lockRt.anchorMax = lockRt.pivot = new Vector2(0.5f, 1f);
        lockRt.sizeDelta = new Vector2(64f, 64f);
        lockRt.anchoredPosition = new Vector2(0f, -CardPad - ArtH * 0.5f + 32f);
        var lockBg = GlassUI.Card(lockRt, "Lock Glass", 32f, false);
        FeltUI.Stretch(lockBg.rectTransform);
        var li = GlassUI.IconImage(lockRt, "ph-lock-simple", 34f, GlassUI.Ink);
        li.GetComponent<LayoutElement>().ignoreLayout = true;
        li.rectTransform.anchorMin = li.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        li.rectTransform.anchoredPosition = Vector2.zero;
        _locks[i] = lockRt.gameObject;
    }

    /// Fit the five cards into the safe content width (4:3 tablets down to 21:9 phones).
    public void FitWidth(float contentWidth)
    {
        if (_cardSize == null) return;
        float w = Mathf.Min(CardW, (contentWidth - 48f - CardGap * 4f) / LevelCatalog.LevelCount);
        w = Mathf.Max(150f, w);
        for (int i = 0; i < _cardSize.Length; i++)
            if (_cardSize[i] != null) { _cardSize[i].preferredWidth = w; _cardSize[i].minWidth = w; }
    }

    /// Lock state, stars and best time from PlayerProgress.
    public void Refresh()
    {
        if (_cards == null) return;
        int unlocked = PlayerProgress.Unlocked;
        for (int i = 0; i < _cards.Length; i++)
        {
            int id = i + 1;
            bool open = id <= unlocked;
            if (_locks[i] != null) _locks[i].SetActive(!open);
            if (_arts[i] != null) _arts[i].color = open ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            int stars = PlayerProgress.Stars(id);
            for (int s = 0; s < 3; s++)
            {
                var img = _stars[i][s];
                if (img == null) continue;
                img.sprite = GlassUI.Icon(s < stars ? "ph-star-fill" : "ph-star");
                img.color = s < stars ? GlassUI.Ink : GlassUI.InkSoft;
            }
            float best = PlayerProgress.BestTime(id);
            if (_best[i] != null) _best[i].text = best > 0f ? GlassUI.Clock(best) : (open ? "" : "Locked");
        }
        if (_total != null) _total.text = PlayerProgress.TotalStars() + "/" + (LevelCatalog.LevelCount * 3);
    }

    public int UnlockedCardCount()
    {
        int n = 0;
        if (_locks == null) return 0;
        for (int i = 0; i < _locks.Length; i++) if (_locks[i] != null && !_locks[i].activeSelf) n++;
        return n;
    }
}

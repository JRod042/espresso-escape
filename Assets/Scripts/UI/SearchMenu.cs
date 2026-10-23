using System;
using UnityEngine;
using UnityEngine.UI;

/// Levels screen: the five stage names on felt plates, then Back.
/// Café Yard, Kraft Bridge, Mill Path, Night Roast, Finale Escape.
public class SearchMenu : MonoBehaviour
{
    Action<int> _onPick;
    Button[] _rows;

    public void Build(Transform parent, Font font, Action<int> onPick, Action onBack)
    {
        _onPick = onPick;
        var col = FeltUI.Column(parent, "Levels Column", 520f, true);
        Heading(col, font, "Levels");

        _rows = new Button[LevelCatalog.LevelCount];
        for (int id = 1; id <= LevelCatalog.LevelCount; id++)
        {
            var def = LevelCatalog.Make(id);
            int picked = def.id;
            var btn = FeltUI.Button(
                col, def.title, font,
                FeltUI.With(FeltUI.KraftTan, 0.92f), FeltUI.FeltCream,
                () => _onPick?.Invoke(picked),
                480f, FeltUI.ButtonHeight, 28);
            btn.name = "Btn " + def.title;
            _rows[id - 1] = btn;
        }

        FeltUI.Button(
            col, "Back", font,
            FeltUI.With(FeltUI.Burlap, 0.82f), FeltUI.DarkRoast,
            () => onBack?.Invoke(),
            480f, FeltUI.ButtonHeight, 28);
        Refresh();
    }

    /// Lock tint only. The five titles stay on screen; there is no search field or pack row.
    public void Refresh()
    {
        if (_rows == null) return;
        int unlocked = PlayerProgress.Unlocked;
        for (int i = 0; i < _rows.Length; i++)
        {
            var btn = _rows[i];
            if (btn == null) continue;
            bool open = (i + 1) <= unlocked;
            btn.interactable = open;
            var press = btn.GetComponent<ButtonPress>();
            if (press != null) press.enabled = open;
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = open
                    ? FeltUI.FeltCream
                    : new Color(FeltUI.FeltCream.r, FeltUI.FeltCream.g, FeltUI.FeltCream.b, 0.45f);
            }
        }
    }

    static void Heading(Transform parent, Font font, string text)
    {
        var t = FeltUI.Label(parent, text, font, 40, FeltUI.FeltCream);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 56f;
        le.minHeight = 56f;
        le.flexibleWidth = 1f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
    }
}

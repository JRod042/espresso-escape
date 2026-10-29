using System.Collections.Generic;
using UnityEngine;

/// Snapshot of every escape.* PlayerPrefs key the tests touch, so a test run never changes Jorge's saved progress.
public class PrefsSnapshot
{
    static readonly string[] IntKeys = { "escape.unlocked", "escape.last", "escape.mute", "escape.haptics",
        "escape.reducemotion", "escape.largetext", "escape.tier",
        "escape.stars.1", "escape.stars.2", "escape.stars.3", "escape.stars.4", "escape.stars.5" };
    static readonly string[] FloatKeys = { "escape.vol.music", "escape.vol.sfx",
        "escape.best.1", "escape.best.2", "escape.best.3", "escape.best.4", "escape.best.5" };

    readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
    readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

    public static PrefsSnapshot Take()
    {
        var s = new PrefsSnapshot();
        foreach (var k in IntKeys) if (PlayerPrefs.HasKey(k)) s._ints[k] = PlayerPrefs.GetInt(k);
        foreach (var k in FloatKeys) if (PlayerPrefs.HasKey(k)) s._floats[k] = PlayerPrefs.GetFloat(k);
        return s;
    }

    public static void ClearAll()
    {
        foreach (var k in IntKeys) PlayerPrefs.DeleteKey(k);
        foreach (var k in FloatKeys) PlayerPrefs.DeleteKey(k);
    }

    public void Restore()
    {
        ClearAll();
        foreach (var kv in _ints) PlayerPrefs.SetInt(kv.Key, kv.Value);
        foreach (var kv in _floats) PlayerPrefs.SetFloat(kv.Key, kv.Value);
        PlayerPrefs.Save();
    }

    // Survives the domain reload of entering play mode.
    const string SessionKey = "escape.tests.prefs";
    public void Stash() => UnityEditor.SessionState.SetString(SessionKey, Serialize());
    public static void RestoreStashed()
    {
        string raw = UnityEditor.SessionState.GetString(SessionKey, null);
        if (string.IsNullOrEmpty(raw)) return;
        Deserialize(raw).Restore();
        UnityEditor.SessionState.EraseString(SessionKey);
    }

    string Serialize()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in _ints) sb.Append("i|").Append(kv.Key).Append('|').Append(kv.Value).Append('\n');
        foreach (var kv in _floats) sb.Append("f|").Append(kv.Key).Append('|')
            .Append(kv.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        return sb.Length == 0 ? "-" : sb.ToString();
    }

    static PrefsSnapshot Deserialize(string raw)
    {
        var s = new PrefsSnapshot();
        foreach (var line in raw.Split('\n'))
        {
            var p = line.Split('|');
            if (p.Length != 3) continue;
            if (p[0] == "i") s._ints[p[1]] = int.Parse(p[2]);
            else s._floats[p[1]] = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
        }
        return s;
    }
}

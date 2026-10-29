using UnityEngine;

/// Persists highest unlocked level (1..5), last played, and per-level stars / best time.
public static class PlayerProgress
{
    const string KeyUnlocked = "escape.unlocked";
    const string KeyLast = "escape.last";
    const string KeyStars = "escape.stars.";   // + level id
    const string KeyBest = "escape.best.";     // + level id, seconds

    public static int Unlocked
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlocked, 1), 1, LevelCatalog.LevelCount);
        set
        {
            PlayerPrefs.SetInt(KeyUnlocked, Mathf.Clamp(value, 1, LevelCatalog.LevelCount));
            PlayerPrefs.Save();
        }
    }

    public static int LastPlayed
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(KeyLast, 1), 1, LevelCatalog.LevelCount);
        set
        {
            PlayerPrefs.SetInt(KeyLast, Mathf.Clamp(value, 1, LevelCatalog.LevelCount));
            PlayerPrefs.Save();
        }
    }

    public static void ClearBeat(int levelId)
    {
        if (levelId >= Unlocked && levelId < LevelCatalog.LevelCount)
            Unlocked = levelId + 1;
        LastPlayed = levelId;
    }

    /// Best star count (0..3) earned on a level.
    public static int Stars(int levelId) => Mathf.Clamp(PlayerPrefs.GetInt(KeyStars + levelId, 0), 0, 3);

    /// Best clear time in seconds, or 0 when never cleared.
    public static float BestTime(int levelId) => Mathf.Max(0f, PlayerPrefs.GetFloat(KeyBest + levelId, 0f));

    /// Stars for a clear: one for finishing, one for every honey bean, one for beating par.
    public static int StarsFor(int got, int total, float seconds, float par)
    {
        int s = 1;
        if (total > 0 && got >= total) s++;
        if (par > 0f && seconds <= par) s++;
        return s;
    }

    /// Keeps the best stars and the best time. Returns true when either improved.
    public static bool RecordClear(int levelId, int stars, float seconds)
    {
        bool better = false;
        if (stars > Stars(levelId)) { PlayerPrefs.SetInt(KeyStars + levelId, Mathf.Clamp(stars, 0, 3)); better = true; }
        float best = BestTime(levelId);
        if (seconds > 0f && (best <= 0f || seconds < best)) { PlayerPrefs.SetFloat(KeyBest + levelId, seconds); better = true; }
        if (better) PlayerPrefs.Save();
        return better;
    }

    public static int TotalStars()
    {
        int n = 0;
        for (int id = 1; id <= LevelCatalog.LevelCount; id++) n += Stars(id);
        return n;
    }
}

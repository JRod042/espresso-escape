using UnityEngine;

/// Persists highest unlocked level (1..5) and last played.
public static class PlayerProgress
{
    const string KeyUnlocked = "escape.unlocked";
    const string KeyLast = "escape.last";

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
}

using UnityEngine;
using UnityEngine.Assertions;

/// In-process PlayerPrefs round trip. This does not restart the editor.
public static class PlayerProgressContract
{
    const string KeyUnlocked = "escape.unlocked";
    const string KeyLast = "escape.last";

    public static void RoundTripAndClamp()
    {
        bool hadUnlocked = PlayerPrefs.HasKey(KeyUnlocked);
        bool hadLast = PlayerPrefs.HasKey(KeyLast);
        int prevUnlocked = PlayerPrefs.GetInt(KeyUnlocked, 1);
        int prevLast = PlayerPrefs.GetInt(KeyLast, 1);
        try
        {
            GameTypes.Unlocked = 4;
            GameTypes.LastPlayed = 2;
            Assert.AreEqual(4, GameTypes.Unlocked, "Unlocked read-back");
            Assert.AreEqual(2, GameTypes.LastPlayed, "LastPlayed read-back");
            Assert.AreEqual(4, PlayerPrefs.GetInt(KeyUnlocked, -1), "prefs unlocked");
            Assert.AreEqual(2, PlayerPrefs.GetInt(KeyLast, -1), "prefs last");

            GameTypes.Unlocked = 0;
            Assert.AreEqual(1, GameTypes.Unlocked, "Unlocked clamps below 1");
            Assert.AreEqual(1, PlayerPrefs.GetInt(KeyUnlocked, -1), "prefs clamp low");

            GameTypes.Unlocked = 99;
            Assert.AreEqual(GameTypes.LevelCount, GameTypes.Unlocked, "Unlocked clamps above LevelCount");
            Assert.AreEqual(GameTypes.LevelCount, PlayerPrefs.GetInt(KeyUnlocked, -1), "prefs clamp high");

            GameTypes.LastPlayed = 0;
            Assert.AreEqual(1, GameTypes.LastPlayed, "LastPlayed clamps below 1");
            GameTypes.LastPlayed = 99;
            Assert.AreEqual(GameTypes.LevelCount, GameTypes.LastPlayed, "LastPlayed clamps above LevelCount");
        }
        finally
        {
            if (hadUnlocked) PlayerPrefs.SetInt(KeyUnlocked, prevUnlocked);
            else PlayerPrefs.DeleteKey(KeyUnlocked);
            if (hadLast) PlayerPrefs.SetInt(KeyLast, prevLast);
            else PlayerPrefs.DeleteKey(KeyLast);
            PlayerPrefs.Save();
        }
    }
}

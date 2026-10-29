using NUnit.Framework;
using UnityEngine;

/// Pure-logic gates: catalog shape, save/load round trips, settings. PlayerPrefs are snapshotted and restored.
public class EscapeEditModeTests
{
    PrefsSnapshot _snap;

    [SetUp] public void SetUp() { _snap = PrefsSnapshot.Take(); }
    [TearDown] public void TearDown() { _snap.Restore(); }

    [Test]
    public void LevelCatalog_HasFiveNamedLevels()
    {
        Assert.AreEqual(5, LevelCatalog.LevelCount);
        string[] names = { "Café Yard", "Kraft Bridge", "Mill Path", "Night Roast", "Finale Escape" };
        float lastGoal = 0f;
        for (int id = 1; id <= 5; id++)
        {
            var def = LevelCatalog.Make(id);
            Assert.AreEqual(id, def.id, "id");
            Assert.AreEqual(names[id - 1], def.title, "title L" + id);
            Assert.Greater(def.parSeconds, 0f, "par L" + id);
            Assert.Greater(def.goalZ, lastGoal - 20f, "goal L" + id);
            Assert.IsFalse(string.IsNullOrEmpty(def.mood), "mood L" + id);
            Assert.AreEqual(1f, def.accent.a, 0.001f, "accent alpha L" + id);
            lastGoal = def.goalZ;
        }
        Assert.IsTrue(LevelCatalog.Make(5).bossBeat, "L5 boss beat");
    }

    [Test]
    public void LevelCatalog_AccentsMatchArtDirection()
    {
        Assert.AreEqual("B5473A", ColorUtility.ToHtmlStringRGB(LevelCatalog.Make(1).accent));
        Assert.AreEqual("C46A2B", ColorUtility.ToHtmlStringRGB(LevelCatalog.Make(2).accent));
        Assert.AreEqual("6E7F4A", ColorUtility.ToHtmlStringRGB(LevelCatalog.Make(3).accent));
        Assert.AreEqual("B07A45", ColorUtility.ToHtmlStringRGB(LevelCatalog.Make(4).accent));
        Assert.AreEqual("2E3A5C", ColorUtility.ToHtmlStringRGB(LevelCatalog.Make(5).accent));
        Assert.AreEqual("E2B35A", ColorUtility.ToHtmlStringRGB(LevelCatalog.GoldL5));
    }

    [Test]
    public void PlayerProgress_SaveLoadRoundTrip()
    {
        PrefsSnapshot.ClearAll();
        Assert.AreEqual(1, PlayerProgress.Unlocked);
        Assert.AreEqual(0, PlayerProgress.TotalStars());
        PlayerProgress.ClearBeat(1);
        Assert.AreEqual(2, PlayerProgress.Unlocked, "L1 clear unlocks L2");
        Assert.IsTrue(PlayerProgress.RecordClear(1, 2, 15.5f), "first clear improves");
        Assert.IsFalse(PlayerProgress.RecordClear(1, 1, 20f), "worse run keeps best");
        Assert.IsTrue(PlayerProgress.RecordClear(1, 3, 12f), "better run improves");
        PlayerPrefs.Save();
        Assert.AreEqual(3, PlayerProgress.Stars(1));
        Assert.AreEqual(12f, PlayerProgress.BestTime(1), 0.001f);
        Assert.AreEqual(3, PlayerProgress.TotalStars());
        PlayerProgress.ClearBeat(2);
        Assert.AreEqual(3, PlayerProgress.Unlocked, "L2 clear unlocks L3");
        Assert.AreEqual(2, PlayerProgress.LastPlayed);
        PlayerProgress.Unlocked = 9;
        Assert.AreEqual(5, PlayerProgress.Unlocked, "clamped to level count");
    }

    [Test]
    public void PlayerProgress_StarRules()
    {
        Assert.AreEqual(1, PlayerProgress.StarsFor(0, 6, 99f, 13f), "finish only");
        Assert.AreEqual(2, PlayerProgress.StarsFor(6, 6, 99f, 13f), "all honey");
        Assert.AreEqual(2, PlayerProgress.StarsFor(0, 6, 10f, 13f), "under par");
        Assert.AreEqual(3, PlayerProgress.StarsFor(6, 6, 10f, 13f), "all three");
    }

    [Test]
    public void GameSettings_RoundTripAndSteps()
    {
        PrefsSnapshot.ClearAll();
        Assert.AreEqual(0.75f, GameSettings.MusicVolume, 0.001f);
        Assert.AreEqual(1f, GameSettings.SfxVolume, 0.001f);
        Assert.IsTrue(GameSettings.Haptics);
        GameSettings.MusicVolume = GameSettings.NextStep(GameSettings.MusicVolume);
        Assert.AreEqual(1f, GameSettings.MusicVolume, 0.001f);
        GameSettings.MusicVolume = GameSettings.NextStep(GameSettings.MusicVolume);
        Assert.AreEqual(0f, GameSettings.MusicVolume, 0.001f, "wraps to 0");
        GameSettings.ReduceMotion = true;
        GameSettings.LargeText = true;
        GameSettings.Quality = GameSettings.Tier.Low;
        Assert.IsTrue(GameSettings.ReduceMotion);
        Assert.IsTrue(GameSettings.LargeText);
        Assert.AreEqual(GameSettings.Tier.Low, GameSettings.EffectiveTier);
        GameSettings.Apply();
        Assert.IsTrue(LiquidGlassPanel.GlobalFallback, "Low tier forces glass fallback");
        GameSettings.Quality = GameSettings.Tier.High;
        GameSettings.Apply();
        Assert.IsFalse(LiquidGlassPanel.GlobalFallback);
    }

    [Test]
    public void UiResources_Present()
    {
        string[] icons = { "ph-play", "ph-pause", "ph-gear-six", "ph-list-bullets", "ph-star", "ph-star-fill", "ph-lock-simple",
            "ph-flag", "ph-coffee-bean", "ph-timer", "ph-house", "ph-arrow-left", "ph-arrow-right", "ph-arrow-counter-clockwise",
            "ph-music-notes", "ph-speaker-high", "ph-speaker-slash", "ph-vibrate", "ph-eye", "ph-text-aa", "ph-question", "ph-x" };
        foreach (var n in icons) Assert.IsNotNull(Resources.Load<Texture2D>("UI/Icons/" + n), "icon " + n);
        for (int id = 1; id <= 5; id++) Assert.IsNotNull(Resources.Load<Texture2D>("UI/LevelArt/level-" + id), "art L" + id);
        Assert.IsNotNull(Resources.Load<Texture2D>("UI/LevelArt/badge-bean"), "badge");
        string[] ui = { "select_001", "back_001", "toggle_001", "toggle_002", "open_001", "close_001", "glass_005", "glass_002",
            "error_004", "confirmation_001", "jingles_PIZZI07", "jingles_PIZZI00" };
        foreach (var n in ui) Assert.IsNotNull(Resources.Load<AudioClip>("Audio/UI/" + n), "ui sfx " + n);
        string[] p0 = { "sfx_hop", "sfx_land", "sfx_bean", "sfx_whoosh", "sfx_stamp", "sfx_steam", "bed_room" };
        foreach (var n in p0) Assert.IsNotNull(Resources.Load<AudioClip>("Audio/P0/" + n), "P0 " + n);
        string[] music = { "music_menu", "music_L1", "music_L2", "music_L3", "music_L4", "music_L5" };
        foreach (var n in music) Assert.IsNotNull(Resources.Load<AudioClip>("Audio/Music/" + n), "music " + n);
        Assert.IsNotNull(Resources.Load<Material>("UI/LiquidGlass"), "glass material");
    }

    [Test]
    public void Clock_FormatsAndCaches()
    {
        Assert.AreEqual("0:00", GlassUI.Clock(0f));
        Assert.AreEqual("1:05", GlassUI.Clock(65.4f));
        Assert.AreSame(GlassUI.Clock(12.2f), GlassUI.Clock(12.9f), "cached string, no per-frame alloc");
    }

    [Test]
    public void AudioImport_MusicStreamsAndSfxPreload()
    {
        var music = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Resources/Audio/Music/music_L1.ogg");
        Assert.IsNotNull(music, "music importer");
        Assert.AreEqual(AudioClipLoadType.Streaming, music.defaultSampleSettings.loadType);
        Assert.AreEqual(AudioCompressionFormat.Vorbis, music.defaultSampleSettings.compressionFormat);
        var tap = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Resources/Audio/UI/select_001.ogg");
        Assert.IsNotNull(tap, "ui importer");
        Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, tap.defaultSampleSettings.loadType);
        Assert.AreEqual(AudioCompressionFormat.ADPCM, tap.defaultSampleSettings.compressionFormat);
        Assert.IsTrue(tap.forceToMono);
        Assert.IsTrue(tap.defaultSampleSettings.preloadAudioData);
    }
}

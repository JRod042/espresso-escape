using UnityEngine;

/// Player settings persisted in PlayerPrefs: volumes, mute, haptics, reduce motion, large text, graphics tier.
/// Apply() pushes them to audio, glass blur and quality. TitleFlow's settings screen is the only writer.
public static class GameSettings
{
    public enum Tier { Auto = 0, Low = 1, High = 2 }

    const string KeyMusic = "escape.vol.music";
    const string KeySfx = "escape.vol.sfx";
    const string KeyMute = "escape.mute";          // same key the old Options mute used
    const string KeyHaptics = "escape.haptics";
    const string KeyReduceMotion = "escape.reducemotion";
    const string KeyLargeText = "escape.largetext";
    const string KeyTier = "escape.tier";

    /// Volume steps the settings screen cycles through.
    public static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };

    /// Below this system RAM (MB) Auto picks the Low tier.
    const int LowTierMemoryMb = 3500;
    /// Shader model below 4.5 (no compute) also means Low.
    const int LowTierShaderLevel = 45;

    public static event System.Action Changed;

    public static float MusicVolume { get => PlayerPrefs.GetFloat(KeyMusic, 0.75f); set => SetF(KeyMusic, Mathf.Clamp01(value)); }
    public static float SfxVolume { get => PlayerPrefs.GetFloat(KeySfx, 1f); set => SetF(KeySfx, Mathf.Clamp01(value)); }
    public static bool Muted { get => PlayerPrefs.GetInt(KeyMute, 0) != 0; set => SetI(KeyMute, value ? 1 : 0); }
    public static bool Haptics { get => PlayerPrefs.GetInt(KeyHaptics, 1) != 0; set => SetI(KeyHaptics, value ? 1 : 0); }
    public static bool ReduceMotion { get => PlayerPrefs.GetInt(KeyReduceMotion, 0) != 0; set => SetI(KeyReduceMotion, value ? 1 : 0); }
    public static bool LargeText { get => PlayerPrefs.GetInt(KeyLargeText, 0) != 0; set => SetI(KeyLargeText, value ? 1 : 0); }
    public static Tier Quality { get => (Tier)Mathf.Clamp(PlayerPrefs.GetInt(KeyTier, 0), 0, 2); set => SetI(KeyTier, (int)value); }

    /// The tier actually in use once Auto has looked at the device.
    public static Tier EffectiveTier
    {
        get
        {
            var q = Quality;
            if (q != Tier.Auto) return q;
            bool weak = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < LowTierMemoryMb;
            weak |= SystemInfo.graphicsShaderLevel < LowTierShaderLevel;
            return weak ? Tier.Low : Tier.High;
        }
    }

    /// Next value in VolumeSteps after `v`, wrapping to 0.
    public static float NextStep(float v)
    {
        for (int i = 0; i < VolumeSteps.Length; i++)
            if (VolumeSteps[i] > v + 0.01f) return VolumeSteps[i];
        return VolumeSteps[0];
    }

    public static void Apply()
    {
        AudioListener.volume = Muted ? 0f : 1f;
        if (EscapeAudio.I != null) EscapeAudio.I.ApplyVolumes(MusicVolume, SfxVolume);
        ApplyQuality();
        Changed?.Invoke();
    }

    /// Low: hard shadows at short range and no backdrop blur. High: soft shadows (BootQuality values) and blur on menus.
    /// The URP asset itself (MSAA, render scale) is not touched at runtime.
    public static void ApplyQuality()
    {
        bool low = EffectiveTier == Tier.Low;
        QualitySettings.shadows = low ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.shadowDistance = low ? 15f : 30f;
        LiquidGlassPanel.GlobalFallback = low;
        LiquidGlassPanel.RefreshAll();
    }

    static void SetF(string k, float v) { PlayerPrefs.SetFloat(k, v); PlayerPrefs.Save(); }
    static void SetI(string k, int v) { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); }
}

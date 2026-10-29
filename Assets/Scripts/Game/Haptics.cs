using UnityEngine;

/// One short buzz on level clear and hazard hits. Device builds only, behind the Haptics setting.
public static class Haptics
{
    /// Minimum seconds between buzzes so a vent and a respawn in the same beat do not double up.
    const float MinGap = 0.25f;
    static float _last = -1f;

    public static void Pulse()
    {
        if (!GameSettings.Haptics) return;
        if (Time.unscaledTime - _last < MinGap) return;
        _last = Time.unscaledTime;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}

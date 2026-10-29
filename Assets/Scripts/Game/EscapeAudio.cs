using UnityEngine;

/// Game audio. Layers, in priority order (the P0 kit wins wherever it overlaps another source):
///   1. Gameplay foley: Casa P0 kit (original) under Resources/Audio/P0.
///   2. Glass UI and jingles: Kenney Interface Sounds and Music Jingles (CC0) under Resources/Audio/UI.
///   3. Music: OpenGameArt CC0 tracks under Resources/Audio/Music (menu theme plus one loop per level).
/// Every clip has a synthesised fallback or is optional, so a missing file never throws.
/// Mute is AudioListener.volume (GameSettings owns the pref); Music/SFX sliders call ApplyVolumes.
public class EscapeAudio : MonoBehaviour
{
    public enum Sfx { Hop, Land, Pickup, Clear, Tap, Open, Close, Hazard, Checkpoint, Star, Steam, Whoosh, Back, Toggle, Locked }

    const string P0Folder = "Audio/P0/";
    const string UiFolder = "Audio/UI/";
    const string MusicFolder = "Audio/Music/";
    /// Index 0 is the menu theme, 1..5 the level loops (see CREDITS-AUDIO.txt for sources).
    static readonly string[] MusicNames = { "music_menu", "music_L1", "music_L2", "music_L3", "music_L4", "music_L5" };

    // Mix (linear gain). Music sits around -22 LUFS and the room bed well under it (SOUND-PLAN.md section 5).
    const float MusicBase = 0.55f;
    const float BedBase = 0.12f;
    const float JingleDuck = 0.25f;      // -12 dB under the level-complete jingle
    const float PauseDuck = 0.5f;        // -6 dB while the game is paused
    const float JingleDuckSeconds = 1.6f;
    const float CrossfadeSeconds = 1.2f;
    const float PitchJitter = 0.04f;
    const float StarSemitoneStep = 2f;

    public static EscapeAudio I { get; private set; }

    const int Rate = 22050;
    AudioSource _sfx;
    AudioSource _ui;                      // fixed pitch: UI clicks and the clear jingle
    readonly AudioSource[] _stars = new AudioSource[3]; // one per star so pitch steps don't bend each other
    AudioSource _bed;
    AudioSource _musicA, _musicB;
    AudioSource _musicLive;
    readonly AudioClip[] _music = new AudioClip[6];
    int _musicIndex = -1;
    float _fade = 1f;
    float _duckUntil;
    float _musicSetting = 1f;
    float _sfxVolume = 1f;

    // P0 foley (fallbacks are synth)
    AudioClip _hop, _land, _bean, _stamp, _whoosh, _steam, _room;
    // Kenney UI
    AudioClip _select, _back, _toggleOn, _open, _close, _tick, _locked, _confirm, _jingleClear, _jingleStar;
    // synth fallbacks
    AudioClip _sHop, _sLand, _sPickup, _sClear, _sTap, _sAmbient;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        _sfx = NewSource(false);
        _ui = NewSource(false);
        for (int i = 0; i < _stars.Length; i++) _stars[i] = NewSource(false);
        _bed = NewSource(true);
        _musicA = NewSource(true);
        _musicB = NewSource(true);
        _musicLive = _musicA;
        Synth();
        LoadClips();
        _bed.clip = _room != null ? _room : _sAmbient;
        ApplyVolumes(GameSettings.MusicVolume, GameSettings.SfxVolume);
        _bed.Play();
        BeanImpulse.OnHop += OnHop;
        BeanController.OnJump += OnHop;
        LandFeel.OnLand += OnLand;
    }

    void OnDestroy()
    {
        BeanImpulse.OnHop -= OnHop;
        BeanController.OnJump -= OnHop;
        LandFeel.OnLand -= OnLand;
        if (I == this) I = null;
    }

    AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.ignoreListenerPause = true;
        return s;
    }

    void LoadClips()
    {
        _hop = Resources.Load<AudioClip>(P0Folder + "sfx_hop");
        _land = Resources.Load<AudioClip>(P0Folder + "sfx_land");
        _bean = Resources.Load<AudioClip>(P0Folder + "sfx_bean");
        _stamp = Resources.Load<AudioClip>(P0Folder + "sfx_stamp");
        _whoosh = Resources.Load<AudioClip>(P0Folder + "sfx_whoosh");
        _steam = Resources.Load<AudioClip>(P0Folder + "sfx_steam");
        _room = Resources.Load<AudioClip>(P0Folder + "bed_room");
        _select = Resources.Load<AudioClip>(UiFolder + "select_001");
        _back = Resources.Load<AudioClip>(UiFolder + "back_001");
        _toggleOn = Resources.Load<AudioClip>(UiFolder + "toggle_001");
        _open = Resources.Load<AudioClip>(UiFolder + "open_001");
        _close = Resources.Load<AudioClip>(UiFolder + "close_001");
        _tick = Resources.Load<AudioClip>(UiFolder + "glass_005");
        _locked = Resources.Load<AudioClip>(UiFolder + "error_004");
        _confirm = Resources.Load<AudioClip>(UiFolder + "confirmation_001");
        _jingleClear = Resources.Load<AudioClip>(UiFolder + "jingles_PIZZI07");
        _jingleStar = Resources.Load<AudioClip>(UiFolder + "jingles_PIZZI00");
    }

    /// True when the P0 foley and Kenney UI clips were all found in Resources (used by the tests).
    public bool HasUiClips => _select != null && _confirm != null && _locked != null && _jingleClear != null;
    public bool HasFoleyClips => _hop != null && _land != null && _bean != null && _stamp != null && _steam != null && _whoosh != null;
    /// Index of the music track that is playing (0 menu, 1..5 levels), -1 before the first track.
    public int MusicIndex => _musicIndex;

    /// Music scales the music and the room bed; SFX scales every one-shot.
    public void ApplyVolumes(float music, float sfx)
    {
        _sfxVolume = Mathf.Clamp01(sfx);
        _musicSetting = Mathf.Clamp01(music);
        if (_bed != null) _bed.volume = BedBase * _musicSetting;
        MixMusic();
    }

    void Update()
    {
        var s = GameSession.I;
        int want = 0;
        if (s != null && (s.phase == GameSession.Phase.Play || s.phase == GameSession.Phase.Clear))
            want = Mathf.Clamp(s.currentLevelId, 1, MusicNames.Length - 1);
        if (s != null && s.phase == GameSession.Phase.Boot) want = -1;
        if (want != _musicIndex && want >= 0) SwitchMusic(want);
        if (_fade < 1f) _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / CrossfadeSeconds);
        MixMusic();
    }

    void SwitchMusic(int index)
    {
        _musicIndex = index;
        if (_music[index] == null) _music[index] = Resources.Load<AudioClip>(MusicFolder + MusicNames[index]);
        var next = _musicLive == _musicA ? _musicB : _musicA;
        next.clip = _music[index];
        if (next.clip == null) return;
        next.Play();
        _musicLive = next;
        _fade = 0f;
    }

    void MixMusic()
    {
        if (_musicA == null) return;
        float duck = Time.unscaledTime < _duckUntil ? JingleDuck : 1f;
        if (Time.timeScale == 0f) duck *= PauseDuck;
        float top = MusicBase * _musicSetting * duck;
        var old = _musicLive == _musicA ? _musicB : _musicA;
        _musicLive.volume = top * _fade;
        old.volume = top * (1f - _fade);
        if (_fade >= 1f && old.isPlaying) old.Stop();
    }

    void OnHop() => Play(Sfx.Hop);
    void OnLand(float speed) => Play(Sfx.Land, Mathf.Clamp01(speed / 7f) * 0.6f + 0.3f);

    public static void Play(Sfx s, float volume = 1f)
    {
        if (I == null) return;
        var clip = I.Clip(s);
        if (clip == null) return;
        if (s == Sfx.Clear) I._duckUntil = Time.unscaledTime + JingleDuckSeconds;
        if (IsGameplay(s))
        {
            I._sfx.pitch = 1f + Random.Range(-PitchJitter, PitchJitter);
            I._sfx.PlayOneShot(clip, volume * I._sfxVolume);
        }
        else I._ui.PlayOneShot(clip, volume * I._sfxVolume);
    }

    /// Star reward: the Kenney pizzicato jingle stepped up two semitones per star (SOUND-PLAN.md 4b).
    public static void PlayStar(int starIndex, float volume = 0.6f)
    {
        if (I == null) return;
        var clip = I._jingleStar != null ? I._jingleStar : I._sPickup;
        var src = I._stars[Mathf.Clamp(starIndex, 0, I._stars.Length - 1)];
        src.pitch = Mathf.Pow(2f, Mathf.Clamp(starIndex, 0, 2) * StarSemitoneStep / 12f);
        src.PlayOneShot(clip, volume * I._sfxVolume);
    }

    static bool IsGameplay(Sfx s) =>
        s == Sfx.Hop || s == Sfx.Land || s == Sfx.Pickup || s == Sfx.Hazard || s == Sfx.Steam || s == Sfx.Whoosh;

    AudioClip Clip(Sfx s)
    {
        switch (s)
        {
            case Sfx.Hop: return _hop != null ? _hop : _sHop;
            case Sfx.Land: return _land != null ? _land : _sLand;
            case Sfx.Pickup: return _bean != null ? _bean : _sPickup;
            case Sfx.Hazard: return _stamp != null ? _stamp : _sLand;
            case Sfx.Steam: return _steam != null ? _steam : _sTap;
            case Sfx.Whoosh: return _whoosh != null ? _whoosh : _sTap;
            case Sfx.Clear: return _jingleClear != null ? _jingleClear : _sClear;
            case Sfx.Star: return _jingleStar != null ? _jingleStar : _sPickup;
            case Sfx.Checkpoint: return _confirm != null ? _confirm : _sPickup;
            case Sfx.Open: return _open != null ? _open : _sTap;
            case Sfx.Close: return _close != null ? _close : _sTap;
            case Sfx.Back: return _back != null ? _back : _sTap;
            case Sfx.Toggle: return _toggleOn != null ? _toggleOn : (_tick != null ? _tick : _sTap);
            case Sfx.Locked: return _locked != null ? _locked : _sTap;
            default: return _select != null ? _select : _sTap;
        }
    }

    void Synth()
    {
        _sHop = Make("hop", 0.16f, (t, d) =>
        {
            float f = Mathf.Lerp(260f, 520f, t / d);
            float env = Mathf.Exp(-t * 18f);
            return Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.6f + Noise() * env * 0.05f;
        });
        _sLand = Make("land", 0.14f, (t, d) =>
        {
            float f = Mathf.Lerp(150f, 70f, t / d);
            float env = Mathf.Exp(-t * 26f);
            return Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.7f + Noise() * Mathf.Exp(-t * 40f) * 0.12f;
        });
        _sPickup = Make("pickup", 0.30f, (t, d) =>
        {
            float f = t < 0.09f ? 880f : 1318.5f;
            float env = Mathf.Exp(-(t % 0.09f) * 20f) * Mathf.Exp(-t * 4f);
            return (Mathf.Sin(t * f * Mathf.PI * 2f) + 0.3f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f)) * env * 0.35f;
        });
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        _sClear = Make("clear", 0.9f, (t, d) =>
        {
            int idx = Mathf.Min(3, (int)(t / 0.2f));
            float lt = t - idx * 0.2f;
            float env = Mathf.Exp(-lt * 7f) * Mathf.Exp(-t * 1.5f);
            float f = notes[idx];
            return (Mathf.Sin(t * f * Mathf.PI * 2f) + 0.25f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f)) * env * 0.35f;
        });
        _sTap = Make("tap", 0.05f, (t, d) => Mathf.Sin(t * 1400f * Mathf.PI * 2f) * Mathf.Exp(-t * 90f) * 0.25f);
        _sAmbient = Make("ambient", 6f, (t, d) =>
        {
            float ph = t / d * Mathf.PI * 2f;
            float a = Mathf.Sin(t * 55f * Mathf.PI * 2f) * 0.18f + Mathf.Sin(t * 55.6f * Mathf.PI * 2f) * 0.14f;
            float swell = 0.65f + 0.35f * Mathf.Sin(ph * 2f);
            return (a * swell + Noise() * 0.06f * swell) * 0.5f;
        });
    }

    static float _nz;
    static float Noise()
    {
        _nz = Mathf.Lerp(_nz, Random.Range(-1f, 1f), 0.25f);
        return _nz;
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, seconds), -1f, 1f);
        int fade = Mathf.Min(n / 2, Rate / 500); // 2 ms fade at both ends to avoid clicks
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            data[i] *= k;
            data[n - 1 - i] *= k;
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

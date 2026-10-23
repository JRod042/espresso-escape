using UnityEngine;

/// Procedural SFX + soft ambient bed. No audio assets required; clips are synthesised once at start.
/// Mute is AudioListener.volume (TitleFlow owns the pref).
public class EscapeAudio : MonoBehaviour
{
    public enum Sfx { Hop, Land, Pickup, Clear, Tap }

    public static EscapeAudio I { get; private set; }

    const int Rate = 22050;
    AudioSource _sfx;
    AudioSource _amb;
    AudioClip _hop, _land, _pickup, _clear, _tap, _ambient;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
        _amb = gameObject.AddComponent<AudioSource>();
        _amb.playOnAwake = false;
        _amb.loop = true;
        _amb.spatialBlend = 0f;
        _amb.volume = 0.16f;
        Synth();
        _amb.clip = _ambient;
        _amb.Play();
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

    void OnHop() => Play(Sfx.Hop);
    void OnLand(float speed) => Play(Sfx.Land, Mathf.Clamp01(speed / 7f) * 0.6f + 0.3f);

    public static void Play(Sfx s, float volume = 1f)
    {
        if (I == null) return;
        var clip = I.Clip(s);
        if (clip == null) return;
        I._sfx.pitch = 1f + Random.Range(-0.04f, 0.04f);
        I._sfx.PlayOneShot(clip, volume);
    }

    AudioClip Clip(Sfx s)
    {
        switch (s)
        {
            case Sfx.Hop: return _hop;
            case Sfx.Land: return _land;
            case Sfx.Pickup: return _pickup;
            case Sfx.Clear: return _clear;
            default: return _tap;
        }
    }

    void Synth()
    {
        // hop: short soft "boing" — felt plush, pitch rises
        _hop = Make("hop", 0.16f, (t, d) =>
        {
            float f = Mathf.Lerp(260f, 520f, t / d);
            float env = Mathf.Exp(-t * 18f);
            return Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.6f + Noise() * env * 0.05f;
        });
        // land: low thud with felt hush
        _land = Make("land", 0.14f, (t, d) =>
        {
            float f = Mathf.Lerp(150f, 70f, t / d);
            float env = Mathf.Exp(-t * 26f);
            return Mathf.Sin(t * f * Mathf.PI * 2f) * env * 0.7f + Noise() * Mathf.Exp(-t * 40f) * 0.12f;
        });
        // pickup: two-note bell
        _pickup = Make("pickup", 0.30f, (t, d) =>
        {
            float f = t < 0.09f ? 880f : 1318.5f;
            float env = Mathf.Exp(-(t % 0.09f) * 20f) * Mathf.Exp(-t * 4f);
            return (Mathf.Sin(t * f * Mathf.PI * 2f) + 0.3f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f)) * env * 0.35f;
        });
        // clear: four-note major arpeggio
        _clear = Make("clear", 0.9f, (t, d) =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int idx = Mathf.Min(3, (int)(t / 0.2f));
            float lt = t - idx * 0.2f;
            float env = Mathf.Exp(-lt * 7f) * Mathf.Exp(-t * 1.5f);
            float f = notes[idx];
            return (Mathf.Sin(t * f * Mathf.PI * 2f) + 0.25f * Mathf.Sin(t * f * 2f * Mathf.PI * 2f)) * env * 0.35f;
        });
        // tap: soft click for UI
        _tap = Make("tap", 0.05f, (t, d) =>
        {
            float env = Mathf.Exp(-t * 90f);
            return Mathf.Sin(t * 1400f * Mathf.PI * 2f) * env * 0.25f;
        });
        // ambient: warm café hum — two detuned low sines + slow filtered noise, seamless loop
        _ambient = Make("ambient", 6f, (t, d) =>
        {
            float ph = t / d * Mathf.PI * 2f;
            float a = Mathf.Sin(t * 55f * Mathf.PI * 2f) * 0.18f + Mathf.Sin(t * 55.6f * Mathf.PI * 2f) * 0.14f;
            float swell = 0.65f + 0.35f * Mathf.Sin(ph * 2f);
            float n = Noise() * 0.06f * swell;
            return (a * swell + n) * 0.5f;
        });
    }

    static float _nz;
    static float Noise()
    {
        // cheap filtered noise
        _nz = Mathf.Lerp(_nz, Random.Range(-1f, 1f), 0.25f);
        return _nz;
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            data[i] = Mathf.Clamp(f(t, seconds), -1f, 1f);
        }
        // 2 ms fade at both ends to avoid clicks
        int fade = Mathf.Min(n / 2, Rate / 500);
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

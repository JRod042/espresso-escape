using UnityEngine;

/// Telegraphed steam vent. Cycle: quiet → warning wisp (puff wobbles) → blast column of wool-cloud puffs.
/// Touching the column during the blast sends the bean back to its checkpoint.
/// Look: cream felt wool clouds (ref-285 clouds, rotate-cloud.mp4) over a dark kraft grate.
public class SteamVent : MonoBehaviour
{
    public float period = 2.8f;
    public float warn = 0.6f;
    public float blast = 0.8f;
    public float height = 1.5f;       // taller than the hop apex (≈1.04 m): wait it out, do not jump it
    public float hitHalfWidth = 0.62f; // vent half-width 0.3 + bean capsule radius ≈ 0.32
    const int PuffCount = 4;
    const float GraceAfterRespawn = 0.8f;
    const float FeetOffset = 0.43f;
    const float AudibleRange = 8f;   // vents further than this from the bean stay silent
    const float SteamVolume = 0.55f; // P0 sfx_steam is the hazard onset (SOUND-PLAN.md section 0)

    LevelRunner _runner;
    float _top, _offset;
    readonly Transform[] _puffs = new Transform[PuffCount];
    readonly Vector3[] _puffScale = new Vector3[PuffCount];
    static readonly Mesh[] PuffMeshes = new Mesh[PuffCount]; // built once, shared by every vent

    public enum State { Quiet, Warn, Blast }
    public State Current { get; private set; }

    public void Init(LevelRunner runner, float x, float top, float phase)
    {
        _runner = runner;
        _top = top;
        _offset = phase;
        transform.position = new Vector3(x, top, 0f);
        var grate = LevelFeatures.Decor(PrimitiveType.Cylinder, "Vent Grate", transform,
            LevelFeatures.Felt("Look/tex-kraft-flute", new Color(0.35f, 0.25f, 0.18f)));
        grate.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        grate.transform.localScale = new Vector3(0.6f, 0.012f, 0.6f);
        var mat = LevelFeatures.Felt("Look/tex-felt-cream", Color.white, 1f);
        for (int i = 0; i < PuffCount; i++)
        {
            var go = new GameObject("Steam Puff");
            go.transform.SetParent(transform, false);
            if (PuffMeshes[i] == null) PuffMeshes[i] = CraftMesh.Cloud(0.55f, 0.32f, 0.45f, 4, 11 + i);
            go.AddComponent<MeshFilter>().sharedMesh = PuffMeshes[i];
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _puffs[i] = go.transform;
            _puffScale[i] = Vector3.one * (1f - i * 0.12f);
            go.transform.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        float t = Mathf.Repeat(Time.time + _offset, period);
        float quiet = period - warn - blast;
        State s = t < quiet ? State.Quiet : t < quiet + warn ? State.Warn : State.Blast;
        if (s == State.Blast && Current != State.Blast) BlastSound();
        Current = s;
        float k = s == State.Blast ? (t - quiet - warn) / blast : 0f;
        for (int i = 0; i < PuffCount; i++)
        {
            float frac = (i + 1f) / PuffCount;
            Vector3 scale;
            float y;
            if (s == State.Quiet) { scale = Vector3.zero; y = 0.1f; }
            else if (s == State.Warn)
            {
                float wob = 0.25f + 0.08f * Mathf.Sin(Time.time * 30f);
                scale = i == 0 ? _puffScale[0] * wob : Vector3.zero;
                y = 0.12f;
            }
            else
            {
                float grow = Mathf.Clamp01(k * 4f);
                float fade = k > 0.85f ? (1f - k) / 0.15f : 1f;
                scale = _puffScale[i] * (grow * fade);
                y = 0.15f + frac * height * Mathf.Min(1f, 0.6f + k);
            }
            _puffs[i].localScale = scale;
            _puffs[i].localPosition = new Vector3(0f, y, 0f);
        }
        if (s == State.Blast) TryHit();
    }

    void BlastSound()
    {
        if (_runner == null || _runner.bean == null) return;
        if (GameSession.I != null && GameSession.I.phase != GameSession.Phase.Play) return;
        float d = Mathf.Abs(_runner.bean.position.x - transform.position.x);
        if (d > AudibleRange) return;
        EscapeAudio.Play(EscapeAudio.Sfx.Steam, SteamVolume * (1f - d / AudibleRange));
    }

    void TryHit()
    {
        if (_runner == null || _runner.bean == null) return;
        if (GameSession.I != null && GameSession.I.phase != GameSession.Phase.Play) return;
        if (_runner.SinceRespawn < GraceAfterRespawn) return;
        Vector3 b = _runner.bean.position;
        float feet = b.y - FeetOffset;
        if (Mathf.Abs(b.x - transform.position.x) < hitHalfWidth && feet < _top + height && feet > _top - 0.5f)
            _runner.Respawn();
    }
}

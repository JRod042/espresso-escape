using UnityEngine;

/// Felt pennant at a checkpoint. It sits low on its pole until the bean claims the checkpoint, then rises
/// (snaps when Reduce motion is on) with a soft select sound. Unsourced shape: a plain pennant on a kraft pole,
/// coloured with the level accent; flagged in the report as not taken from a reference frame.
public class CheckpointFlag : MonoBehaviour
{
    const float PoleHeight = 1.1f;
    const float LowY = 0.25f;
    const float RiseSpeed = 4f;   // 1/s toward the top
    const float LaneBehindZ = 0.9f;

    LevelRunner _runner;
    float _x;
    bool _raised;
    Transform _pennant;
    float _t;

    public bool Raised => _raised;

    public void Init(LevelRunner runner, float x, float top, Color accent)
    {
        _runner = runner;
        _x = x;
        transform.position = new Vector3(x, top, LaneBehindZ);
        var pole = LevelFeatures.Decor(PrimitiveType.Cylinder, "Pole", transform,
            LevelFeatures.Felt("Look/tex-kraft-corrugated", new Color(0.72f, 0.60f, 0.46f)));
        pole.transform.localPosition = new Vector3(0f, PoleHeight * 0.5f, 0f);
        pole.transform.localScale = new Vector3(0.05f, PoleHeight * 0.5f, 0.05f);
        var bead = LevelFeatures.Decor(PrimitiveType.Sphere, "Pompom", transform,
            LevelFeatures.Felt("Look/tex-felt-red", Color.white));
        bead.transform.localPosition = new Vector3(0f, PoleHeight + 0.04f, 0f);
        bead.transform.localScale = Vector3.one * 0.12f;
        var p = LevelFeatures.Decor(PrimitiveType.Cube, "Pennant", transform,
            LevelFeatures.Felt("Look/tex-felt-cream", accent));
        p.transform.localScale = new Vector3(0.36f, 0.22f, 0.03f);
        _pennant = p.transform;
        SetPennant(0f);
    }

    void SetPennant(float t)
    {
        if (_pennant == null) return;
        float y = Mathf.Lerp(LowY, PoleHeight - 0.14f, t);
        _pennant.localPosition = new Vector3(0.2f, y, 0f);
    }

    void Update()
    {
        if (_runner == null) return;
        if (!_raised && _runner.CheckpointX >= _x - 0.01f)
        {
            _raised = true;
            EscapeAudio.Play(EscapeAudio.Sfx.Checkpoint, 0.5f);
        }
        if (_raised && _t < 1f)
        {
            _t = GameSettings.ReduceMotion ? 1f : Mathf.Min(1f, _t + Time.deltaTime * RiseSpeed);
            SetPennant(_t);
        }
    }
}

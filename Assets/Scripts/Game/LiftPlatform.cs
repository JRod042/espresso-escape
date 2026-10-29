using UnityEngine;

/// Felt-and-kraft lift: a kinematic slab that holds at the bottom, rises, holds at the top, and comes back down.
/// The bean rides it up to a bonus honey bean. Look: deep kraft step material from the lane (ref-285 kraft wall).
public class LiftPlatform : MonoBehaviour
{
    public float rise = 1.35f;
    public float hold = 1.0f;
    public float travel = 1.3f;
    const float Width = 1.0f;
    const float Depth = 1.6f;
    const float Thick = 0.16f;
    const float FlushLift = 0.03f; // a hair above the block so the bean walks on, not into it

    Rigidbody _rb;
    float _x, _low;

    public float TopLow => _low + Thick * 0.5f;
    public float TopHigh => _low + rise + Thick * 0.5f;

    public void Init(float x, float blockTop)
    {
        _x = x;
        _low = blockTop + FlushLift - Thick * 0.5f;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Lift Slab";
        slab.transform.SetParent(transform, false);
        slab.transform.localScale = new Vector3(Width, Thick, Depth);
        slab.GetComponent<Renderer>().sharedMaterial = LevelFeatures.Felt("Look/tex-kraft-corrugated", new Color(0.72f, 0.60f, 0.46f), 1f);
        // felt-red edge so the moving piece reads against the static kraft
        var trim = LevelFeatures.Decor(PrimitiveType.Cube, "Lift Trim", slab.transform, LevelFeatures.Felt("Look/tex-felt-red", Color.white));
        trim.transform.localPosition = new Vector3(0f, -0.4f, -0.52f);
        trim.transform.localScale = new Vector3(1.02f, 0.25f, 0.02f);
        transform.position = new Vector3(_x, _low, 0.1f);
        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// 0..1 height along the cycle: hold low, ease up, hold high, ease down.
    public float Phase01(float time)
    {
        float cycle = 2f * (hold + travel);
        float t = Mathf.Repeat(time, cycle);
        if (t < hold) return 0f;
        t -= hold;
        if (t < travel) return Mathf.SmoothStep(0f, 1f, t / travel);
        t -= travel;
        if (t < hold) return 1f;
        t -= hold;
        return Mathf.SmoothStep(1f, 0f, t / travel);
    }

    void FixedUpdate()
    {
        if (_rb == null) return;
        _rb.MovePosition(new Vector3(_x, _low + rise * Phase01(Time.time), 0.1f));
    }
}

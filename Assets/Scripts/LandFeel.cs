using UnityEngine;

/// Land contact drives feel (Prompt 6/7). Weight on contact — not animation-only.
/// Adds a take-off stretch and an OnLand event; squash numbers unchanged.
public class LandFeel : MonoBehaviour
{
    public float landSpeedGate = 2.6f; // laneB-landfeel-gate-20260917
    public float squash = 0.14f; // laneB-landfeel-squash-20260917
    public float recover = 11f; // laneB-landfeel-recover-20260917
    public float stretch = 0.10f; // take-off: taller/thinner for a beat

    /// Fired on landing with the impact speed (m/s).
    public static event System.Action<float> OnLand;

    Rigidbody body;
    Vector3 baseScale;
    float squashAmt;   // >0 squash, <0 stretch
    bool wasAir;
    float lastVy;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        baseScale = transform.localScale;
    }

    void OnEnable() { BeanImpulse.OnHop += Hop; }
    void OnDisable() { BeanImpulse.OnHop -= Hop; }

    void Hop() { squashAmt = -stretch; }

    void FixedUpdate()
    {
        float vy = body.linearVelocity.y;
        bool air = Mathf.Abs(vy) > 0.15f || !Grounded();
        if (wasAir && !air && vy <= 0.05f)
        {
            float impact = Mathf.Abs(lastVy);
            if (impact >= landSpeedGate) squashAmt = squash;
            else squashAmt = squash * Mathf.Clamp01(impact / landSpeedGate) * 0.6f;
            OnLand?.Invoke(impact);
        }
        wasAir = air;
        lastVy = vy;
        squashAmt = Mathf.MoveTowards(squashAmt, 0f, Time.fixedDeltaTime * recover);
        var s = baseScale;
        s.y *= 1f - squashAmt;
        s.x *= 1f + squashAmt * 0.5f;
        s.z *= 1f + squashAmt * 0.5f;
        transform.localScale = s;
    }

    bool Grounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.55f);
    }
}

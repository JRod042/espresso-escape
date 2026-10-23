using UnityEngine;

/// Land contact drives feel. The felt body only settles a little — 285.mp4 has no squash or stretch.
/// The root scale (capsule collider) is never changed.
public class LandFeel : MonoBehaviour
{
    public float landSpeedGate = 2.6f; // laneB-landfeel-gate-20260917
    public float squash = 0.03f;  // very small settle on the felt body only
    public float recover = 9.5f;  // plush recovery, a beat slower than the old 11
    public float stretch = 0f;    // 285.mp4: the felt body does not elongate on the push-off

    /// Fired on landing with the impact speed (m/s).
    public static event System.Action<float> OnLand;

    Rigidbody body;
    Transform _felt;
    Vector3 _rootBase, _feltBase;
    float squashAmt;   // >0 settle, <0 stretch (stretch stays 0)
    bool wasAir;
    float lastVy;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        _rootBase = transform.localScale;
        _felt = transform.Find("Bean Body");
        _feltBase = _felt != null ? _felt.localScale : Vector3.one;
    }

    void OnEnable() { BeanImpulse.OnHop += Hop; BeanController.OnJump += Hop; }
    void OnDisable() { BeanImpulse.OnHop -= Hop; BeanController.OnJump -= Hop; }

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
    }

    void LateUpdate()
    {
        // keep the capsule at its authored scale
        transform.localScale = _rootBase;
        if (_felt == null) return;
        float amt = squashAmt;
        var s = _feltBase;
        s.y *= 1f - amt;
        s.x *= 1f + amt * 0.45f;
        s.z *= 1f + amt * 0.45f;
        _felt.localScale = s;
    }

    bool Grounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.55f);
    }
}

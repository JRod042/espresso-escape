using UnityEngine;

/// Locked feel: one tap = same impulse every time. Not a tween.
public class BeanImpulse : MonoBehaviour
{
    public float forwardImpulse = 5.2f;
    public float hopImpulse = 6.4f;
    public float coolSeconds = 0.13f; // laneB-hop-cool-20260917
    /// Fired after each player hop (audio / stretch / pose hooks).
    public static event System.Action OnHop;

    Rigidbody body;
    float cool;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        cool -= Time.deltaTime;
        if (cool > 0f) return;
        if (!(Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
            return;
        var v = body.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        body.linearVelocity = v;
        body.AddForce(Vector3.forward * forwardImpulse + Vector3.up * hopImpulse, ForceMode.Impulse);
        cool = coolSeconds;
        OnHop?.Invoke();
    }
}

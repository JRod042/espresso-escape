using UnityEngine;
using UnityEngine.EventSystems;

/// Locked feel: one tap = same impulse every time. Not a tween.
/// Proof-critical values (forwardImpulse, hopImpulse, cooldown) are unchanged; PhysicsProof reads them and
/// applies the same AddForce directly, so the direction, the UI guard and the event below do not affect the proof.
/// Side-scroll: the run direction is +X (left → right like the Imagine set frames); the lane is locked at z = 0.
public class BeanImpulse : MonoBehaviour
{
    public float forwardImpulse = 5.2f;
    public float hopImpulse = 6.4f;
    public float coolSeconds = 0.13f; // laneB-hop-cool-20260917

    /// Run direction (side-scroll: +X). Proof applies its own +Z impulse, so this is play-only.
    public Vector3 runDir = Vector3.right;

    /// Fired after each player hop (audio / stretch / pose hooks).
    public static event System.Action OnHop;

    Rigidbody body;
    float cool;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // side-scroll lane: no roll/yaw from contacts, never drift off the z = 0 lane
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
        cool -= Time.deltaTime;
        if (cool > 0f) return;
        if (Time.timeScale <= 0f) return;
        bool tap = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        bool touch = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        if (!(tap || touch)) return;
        // taps on UI chrome (Menu, overlays) must not hop the bean
        if (EventSystem.current != null)
        {
            if (touch && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            if (!touch && Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject()) return;
        }
        var v = body.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        // every hop covers the same ground: drop the carried run speed so gaps read the same each time
        var along = Vector3.Project(v, runDir.normalized);
        v -= along;
        body.linearVelocity = v;
        body.AddForce(runDir.normalized * forwardImpulse + Vector3.up * hopImpulse, ForceMode.Impulse);
        cool = coolSeconds;
        OnHop?.Invoke();
    }
}

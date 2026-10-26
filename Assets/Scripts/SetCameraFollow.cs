using UnityEngine;

/// Side-scroll diorama camera (LittleBigPlanet framing on the Imagine set):
/// looks straight into the set from the front, slides along X with the bean, eases only, never cuts.
/// The bean sits about a third in from the left so the player sees the next gap coming.
/// TitleDolly turns this off during the title shot and turns it back on for Play.
public class SetCameraFollow : MonoBehaviour
{
    public Transform target;
    // Wide side-scroll diorama framing (285.jpg + set-01.jpg): bean, pines, grinder, red tree, bridge, and clouds on upper wall.
    public Vector3 offset = new Vector3(0.55f, 1.35f, -5.45f);
    public float restY = 0.46f;       // bean root height when the feet rest on the burlap
    public float pitch = 7.5f;        // gentle downward tilt into the handmade stage
    public float posDamp = 6.0f;      // slower, heavier pan like the clip (was 8.5)
    public float lookDamp = 10.5f; // laneB-cam-lookdamp-20260917
    public float ySoft = 0.22f;       // follow even less of the hop height so the twine/clouds stay put
    public float leadBySpeed = 0.14f; // running (2.6 m/s) needs more look-ahead than the old fixed hop

    Rigidbody _rb;

    void LateUpdate()
    {
        if (target == null) return;
        if (_rb == null) _rb = target.GetComponent<Rigidbody>();
        var tp = target.position;
        float vx = _rb != null ? _rb.linearVelocity.x : 0f;
        float softY = restY * (1f - ySoft) + tp.y * ySoft;
        var want = new Vector3(tp.x + offset.x + Mathf.Clamp(vx, -4f, 6f) * leadBySpeed, softY + offset.y, offset.z);
        transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-posDamp * Time.deltaTime));
        var look = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-lookDamp * Time.deltaTime));
    }
}

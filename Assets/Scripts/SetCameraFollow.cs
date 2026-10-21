using UnityEngine;

/// Side-scroll diorama camera (LittleBigPlanet framing on the Imagine set):
/// looks straight into the set from the front, slides along X with the bean, eases only, never cuts.
/// The bean sits about a third in from the left so the player sees the next gap coming.
public class SetCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(1.05f, 1.15f, -4.30f); // ahead, up, back from the lane (z = 0)
    public float restY = 0.51f;       // bean root height when the capsule (r 0.38, h 0.86) rests on the burlap
    public float pitch = 8.5f;        // look slightly down onto the burlap like the set frames
    public float posDamp = 8.5f; // laneB-cam-posdamp-20260917
    public float lookDamp = 10.5f; // laneB-cam-lookdamp-20260917
    public float ySoft = 0.28f;       // follow only part of the hop height so the horizon stays calm
    public float leadBySpeed = 0.08f; // a little extra lead when the bean is moving fast

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

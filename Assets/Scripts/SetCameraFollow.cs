using UnityEngine;

/// Side-scroll diorama camera (LittleBigPlanet framing on the Imagine set):
/// looks straight into the set from the front, slides along X with the bean, eases only, never cuts.
/// The bean sits about a third in from the left so the player sees the next gap coming.
/// TitleDolly turns this off during the title shot and turns it back on for Play.
public class SetCameraFollow : MonoBehaviour
{
    public Transform target;
    // 285.mp4: bean just left of centre, lens near eye level, a slow pan that trails the walk; gen-2d7dcad9: slow dolly.
    public Vector3 offset = new Vector3(0.15f, 1.05f, -4.35f); // 9:02 PM cam-shot framing that match.ps1 scores
    public float restY = 0.51f;       // bean root height when the capsule (r 0.38, h 0.86) rests on the burlap
    public float pitch = 6.5f;        // 285: camera barely above the bean's eyes, not looking down on it
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

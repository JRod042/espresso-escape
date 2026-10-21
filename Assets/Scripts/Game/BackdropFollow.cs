using UnityEngine;

/// Keeps the kraft wall / twine / clouds centred on the camera along X (side-scroll),
/// so the Imagine diorama framing (corrugated wall behind the set) holds for the whole run.
/// Optional parallax: a factor below 1 makes the layer slide slower than the bean (LittleBigPlanet depth).
public class BackdropFollow : MonoBehaviour
{
    public Transform target;
    public float aheadX = 0.9f;
    public float parallax = 1f;      // 1 = glued to the camera, 0.6 = mid layer, 0 = static
    public float damp = 6f;
    public float swayAmp = 0.012f;   // clouds on twine breathe a little
    public float swaySpeed = 0.35f;
    float _baseY;
    float _baseX;

    void Start() { _baseY = transform.position.y; _baseX = transform.position.x; }

    void LateUpdate()
    {
        if (target == null) return;
        var p = transform.position;
        float wantX = _baseX + (target.position.x + aheadX - _baseX) * parallax;
        p.x = Mathf.Lerp(p.x, wantX, 1f - Mathf.Exp(-damp * Time.deltaTime));
        p.y = _baseY + Mathf.Sin(Time.time * swaySpeed * Mathf.PI * 2f) * swayAmp;
        transform.position = p;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

/// Subtle press feedback for uGUI buttons: scales to pressScale while held, eases back on release.
/// Unscaled time so it keeps working under the paused (timeScale 0) overlay.
public class ButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressScale = 0.96f;
    public float speed = 18f;

    float _target = 1f;
    Vector3 _base = Vector3.one;
    bool _init;

    void OnEnable()
    {
        if (!_init) { _base = transform.localScale; _init = true; }
        _target = 1f;
        transform.localScale = _base;
    }

    public void OnPointerDown(PointerEventData e) { _target = pressScale; }
    public void OnPointerUp(PointerEventData e) { _target = 1f; }
    public void OnPointerExit(PointerEventData e) { _target = 1f; }

    void Update()
    {
        float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, _base * _target, k);
    }
}

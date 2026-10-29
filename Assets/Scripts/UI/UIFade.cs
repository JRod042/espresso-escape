using UnityEngine;

/// Fades (and lifts) a panel in when it is shown. Unscaled time so it works under pause.
/// Reduce motion: shows instantly with no lift.
[RequireComponent(typeof(CanvasGroup))]
public class UIFade : MonoBehaviour
{
    public float duration = 0.18f;
    public float lift = 18f;

    CanvasGroup _group;
    RectTransform _rt;
    Vector2 _home;
    bool _homeSet;
    float _t;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rt = transform as RectTransform;
    }

    void OnEnable()
    {
        if (_rt != null && !_homeSet) { _home = _rt.anchoredPosition; _homeSet = true; }
        _t = GameSettings.ReduceMotion ? duration : 0f;
        Apply();
    }

    void OnDisable()
    {
        if (_rt != null && _homeSet) _rt.anchoredPosition = _home;
    }

    /// Call after moving the panel in code so the lift returns to the new spot.
    public void SetHome(Vector2 pos)
    {
        _home = pos;
        _homeSet = true;
        if (_rt != null) _rt.anchoredPosition = pos;
    }

    void Update()
    {
        if (_t >= duration) return;
        _t += Time.unscaledDeltaTime;
        Apply();
    }

    void Apply()
    {
        float k = duration <= 0f ? 1f : Mathf.Clamp01(_t / duration);
        float e = 1f - (1f - k) * (1f - k); // ease out
        if (_group != null) _group.alpha = e;
        if (_rt != null && _homeSet) _rt.anchoredPosition = _home + new Vector2(0f, -lift * (1f - e));
    }
}

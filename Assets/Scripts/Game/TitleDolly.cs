using System.Collections.Generic;
using UnityEngine;

/// gen-2d7dcad9.mp4 (6 s, landscape): a slow dolly right (~0.06 frame-widths per second)
/// and a small push-in (~4% closer) across the grinder, kraft bridge, and flower tree.
/// Owns the camera while the session is on the title menus. Play and Level Clear
/// hand the camera back to SetCameraFollow. The shot plays once, then holds.
[DefaultExecutionOrder(50)]
public class TitleDolly : MonoBehaviour
{
    public float clipSeconds = 6f;
    public float widthsPerSecond = 0.06f;
    public float pushIn = 0.04f;

    float _t;
    bool _shot;
    bool _partsCached;
    Camera _cam;
    SetCameraFollow _follow;
    GameSession.Phase _prev = GameSession.Phase.Boot;
    float _focusX, _travel, _y, _z0, _z1, _pitch;
    static readonly List<Transform> Grinders = new List<Transform>(4);
    static readonly List<Transform> Planks = new List<Transform>(16);
    static readonly List<Transform> Flowers = new List<Transform>(4);

    void CacheParts()
    {
        if (_partsCached) return;
        _cam = GetComponent<Camera>();
        if (_cam == null) return;
        _follow = GetComponent<SetCameraFollow>();
        _partsCached = true;
    }

    void LateUpdate()
    {
        CacheParts();
        if (_cam == null) return;
        var phase = GameSession.I != null ? GameSession.I.phase : GameSession.Phase.Title;
        bool menu = phase == GameSession.Phase.Boot
            || phase == GameSession.Phase.Title
            || phase == GameSession.Phase.Options
            || phase == GameSession.Phase.Search;
        var follow = _follow;
        if (!menu)
        {
            if (follow != null) follow.enabled = true;
            _shot = false;
            _prev = phase;
            return;
        }
        bool entered = !_shot || _prev == GameSession.Phase.Play || _prev == GameSession.Phase.Clear;
        _prev = phase;
        if (entered)
        {
            _t = 0f;
            _shot = true;
            Measure();
        }
        if (follow != null) follow.enabled = false;
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / Mathf.Max(0.01f, clipSeconds));
        u = u * u * (3f - 2f * u);
        float x = _focusX + Mathf.Lerp(-_travel * 0.5f, _travel * 0.5f, u);
        transform.SetPositionAndRotation(
            new Vector3(x, _y, Mathf.Lerp(_z0, _z1, u)),
            Quaternion.Euler(_pitch, 0f, 0f));
    }

    void Measure()
    {
        CacheParts();
        var cam = _cam;
        var follow = _follow;
        _z0 = follow != null && follow.offset.z <= -4.5f ? follow.offset.z : -5.45f;
        _y = follow != null ? follow.restY + follow.offset.y : 1.91f;
        _pitch = follow != null ? follow.pitch : 7.5f;
        float dist = Mathf.Max(0.5f, Mathf.Abs(_z0));
        // closer to the set: 4% of the rest distance, toward z = 0
        _z1 = _z0 + Mathf.Sign(-_z0) * dist * pushIn;
        float fov = cam != null ? cam.fieldOfView : 46.4f;
        float aspect = cam != null ? Mathf.Max(0.5f, cam.aspect) : 16f / 9f;
        float width = 2f * dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect;
        _travel = widthsPerSecond * width * clipSeconds;
        _focusX = FocusX(follow);
    }

    /// Centers on the wide opening yard when Imagine Bean is present, or the bridge/grinder span.
    static float FocusX(SetCameraFollow follow)
    {
        if (follow != null && follow.target != null)
            return follow.target.position.x + follow.offset.x;
        Grinders.Clear();
        Planks.Clear();
        Flowers.Clear();
        SetNameQuery.Collect("Cardboard Grinder", Grinders, "Bridge Plank", Planks, "Flower Trunk", Flowers);
        var grinders = Grinders;
        var planks = Planks;
        var flowers = Flowers;
        if (planks.Count > 0 && grinders.Count > 0 && flowers.Count > 0)
        {
            float bx = AvgX(planks);
            var g = ClosestSide(grinders, bx, -1);
            var f = ClosestSide(flowers, bx, +1);
            if (g != null && f != null && f.position.x - g.position.x > 2f)
                return bx;
        }
        float left = float.PositiveInfinity;
        bool any = false;
        for (int i = 0; i < grinders.Count; i++)
        {
            if (grinders[i] == null) continue;
            float x = grinders[i].position.x;
            if (x < left) { left = x; any = true; }
        }
        return any ? left + 1.7f : 0.55f;
    }

    static float AvgX(List<Transform> list)
    {
        float s = 0f;
        int n = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null) { s += list[i].position.x; n++; }
        return n > 0 ? s / n : 0f;
    }

    static Transform ClosestSide(List<Transform> list, float origin, int side)
    {
        Transform best = null;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null) continue;
            float d = (t.position.x - origin) * side;
            if (d <= 0.2f) continue;
            if (d < bestD) { bestD = d; best = t; }
        }
        return best;
    }
}

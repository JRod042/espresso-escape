using System.Collections.Generic;
using UnityEngine;

/// Procedural hop poses lifted from the Imagine hop frames (DriveLook/imagine/frames/285-01..06):
///  01/02 push-off  – lean into the hop, arms swing back, one leg forward one back
///  03    airborne  – arms held out and up, legs split, eyes squeezed shut
///  04/05 landing   – feet come together, arms drop, LandFeel squash
///  06    idle      – upright, arms a touch out, slow breathing
/// Rotates only the limb pivots / body child, so the capsule collider and BeanImpulse stay untouched.
public class BeanAnimator : MonoBehaviour
{
    [Header("Pivots (found by name if empty)")]
    public Transform body;
    public Transform armL, armR, legL, legR;
    public Transform[] eyes;

    [Header("Pose")]
    public float leanPerSpeed = 2.4f;    // degrees of lean per m/s of run speed
    public float maxLean = 14f;
    public float armRestSpread = 20f;    // 285-06: arms slightly out from the egg
    public float armAirSpread = 62f;     // 285-03: arms held out & up
    public float armBackSwing = -28f;    // 285-01: arms trail on push-off
    public float armReach = 18f;         // falling: arms come forward a little
    public float legSplit = 38f;         // 285-02/03: stride in the air
    public float legTuck = 12f;          // landing: feet together, knees soft
    public float breathAmp = 0.012f;
    public float breathHz = 0.9f;
    public float poseDamp = 12f;

    Rigidbody _rb;
    Quaternion _baseYaw;
    Vector3 _bodyBaseScale;
    readonly List<Vector3> _eyeBase = new List<Vector3>();
    float _air, _rise, _fall, _squint;
    float _armSpread, _armSwing, _legFront, _legBack;
    float _lean;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _baseYaw = transform.rotation;
        if (body == null) body = transform.Find("Bean Body");
        if (armL == null) armL = transform.Find("Arm Pivot L");
        if (armR == null) armR = transform.Find("Arm Pivot R");
        if (legL == null) legL = transform.Find("Leg Pivot L");
        if (legR == null) legR = transform.Find("Leg Pivot R");
        if (body != null) _bodyBaseScale = body.localScale;
        if (eyes == null || eyes.Length == 0)
        {
            var list = new List<Transform>();
            foreach (Transform c in transform) if (c.name == "Eye") list.Add(c);
            eyes = list.ToArray();
        }
        _eyeBase.Clear();
        foreach (var e in eyes) _eyeBase.Add(e != null ? e.localScale : Vector3.one);
        _armSpread = armRestSpread;
    }

    void OnEnable() { BeanImpulse.OnHop += OnHop; }
    void OnDisable() { BeanImpulse.OnHop -= OnHop; }

    void OnHop()
    {
        // 285-01: snap into the push-off pose, the damp eases into the airborne pose from here
        _armSwing = armBackSwing;
        _legFront = legSplit * 0.6f;
        _legBack = -legSplit * 0.6f;
    }

    bool Grounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.56f, ~0, QueryTriggerInteraction.Ignore);
    }

    void LateUpdate()
    {
        if (_rb == null) return;
        float dt = Time.deltaTime;
        float k = 1f - Mathf.Exp(-poseDamp * dt);
        var v = _rb.linearVelocity;
        bool grounded = Grounded() && Mathf.Abs(v.y) < 0.6f;

        float airTarget = grounded ? 0f : 1f;
        _air = Mathf.Lerp(_air, airTarget, k);
        _rise = Mathf.Lerp(_rise, grounded ? 0f : Mathf.Clamp01(v.y / 5f), k);
        _fall = Mathf.Lerp(_fall, grounded ? 0f : Mathf.Clamp01(-v.y / 5f), k);
        // 285-03: eyes squeeze shut around the apex
        float apex = (!grounded && Mathf.Abs(v.y) < 2.2f) ? 1f : 0f;
        _squint = Mathf.Lerp(_squint, apex, k * 1.4f);

        // body lean into the run (world Z axis tips the top toward +X)
        float leanTarget = Mathf.Clamp(v.x * leanPerSpeed, -maxLean, maxLean) + _rise * 4f - _fall * 3f;
        _lean = Mathf.Lerp(_lean, leanTarget, k);
        transform.rotation = Quaternion.AngleAxis(-_lean, Vector3.forward) * _baseYaw;

        // arms: rest → out & up in the air, trailing on the way up, reaching a little on the way down
        float spreadT = armRestSpread + (armAirSpread - armRestSpread) * _air;
        float swingT = armBackSwing * _rise + armReach * _fall;
        if (grounded) swingT += Mathf.Sin(Time.time * 1.7f) * 2.5f; // idle sway
        _armSpread = Mathf.Lerp(_armSpread, spreadT + (grounded ? Mathf.Sin(Time.time * breathHz * Mathf.PI * 2f) * 2f : 0f), k);
        _armSwing = Mathf.Lerp(_armSwing, swingT, k);

        // legs: stride apart while rising, together and soft for the landing (285-04/05)
        float frontT = _rise * legSplit + _fall * legTuck;
        float backT = -_rise * legSplit * 0.8f + _fall * legTuck;
        _legFront = Mathf.Lerp(_legFront, frontT, k);
        _legBack = Mathf.Lerp(_legBack, backT, k);

        // root-local axes for "toward travel (+X)" and "out to the side"
        Vector3 travelL = transform.InverseTransformDirection(Vector3.right);
        Vector3 swingAxis = Vector3.Cross(Vector3.down, travelL).normalized;

        Pose(armL, _armSwing, _armSpread, -1, swingAxis);
        Pose(armR, _armSwing, _armSpread, +1, swingAxis);
        // left leg leads (frames 285-01/02: left foot forward)
        Pose(legL, _legFront, 0f, -1, swingAxis);
        Pose(legR, _legBack, 0f, +1, swingAxis);

        // breathing on the felt body, LandFeel owns the root squash
        if (body != null)
        {
            float b = 1f + Mathf.Sin(Time.time * breathHz * Mathf.PI * 2f) * breathAmp * (1f - _air);
            body.localScale = new Vector3(_bodyBaseScale.x * (1f + (b - 1f) * 0.5f), _bodyBaseScale.y * b, _bodyBaseScale.z * (1f + (b - 1f) * 0.5f));
        }
        for (int i = 0; i < eyes.Length; i++)
        {
            if (eyes[i] == null) continue;
            var s = _eyeBase[i];
            eyes[i].localScale = new Vector3(s.x, s.y * Mathf.Lerp(1f, 0.28f, _squint), s.z);
        }
    }

    static void Pose(Transform pivot, float swing, float spread, int side, Vector3 swingAxis)
    {
        if (pivot == null) return;
        // spread: tip moves away from the body, outward along the pivot's side
        Vector3 sideL = Vector3.right * side;
        Vector3 spreadAxis = Vector3.Cross(Vector3.down, sideL).normalized;
        pivot.localRotation = Quaternion.AngleAxis(swing, swingAxis) * Quaternion.AngleAxis(spread, spreadAxis);
    }
}

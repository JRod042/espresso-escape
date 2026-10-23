using System.Collections.Generic;
using UnityEngine;

/// Procedural hop poses lifted from the Imagine hop frames (DriveLook/imagine/frames/285-01..06):
///  01/02 push-off  – lean into the hop, arms swing back, one leg forward one back
///  03    airborne  – arms held out and up, legs split, eyes squeezed shut
///  04/05 landing   – feet come together, arms drop (felt body does not squash)
/// Standing and the title (before play) use the 285.jpg walk, not frame 06: rounded arms
/// held in front of the egg, left foot forward, right foot back. 285-01 is that same arm shape.
/// The smile is the Smile mesh; this script does not move it. Hop angles are unchanged.
/// Rotates only the limb pivots / body child, so the capsule collider and BeanImpulse stay untouched.
public class BeanAnimator : MonoBehaviour
{
    [Header("Pivots (found by name if empty)")]
    public Transform body;
    public Transform armL, armR, legL, legR;
    public Transform[] eyes;

    [Header("Pose")]
    public float leanPerSpeed = 3.2f;    // degrees of lean per m/s of run speed (285.mp4: 5–10° into the travel)
    public float maxLean = 9f;
    public float armRestSpread = 20f;    // 285-06: arms slightly out from the egg
    public float armAirSpread = 62f;     // 285-03: arms held out & up
    public float armBackSwing = -28f;    // 285-01: arms trail on push-off
    public float armReach = 18f;         // falling: arms come forward a little
    public float legSplit = 38f;         // 285-02/03: stride in the air
    public float legTuck = 12f;          // landing: feet together, knees soft
    public float breathAmp = 0.012f;
    public float breathHz = 0.9f;
    public float poseDamp = 12f;

    [Header("Idle stance (285.jpg walk — title and standing; hop angles stay as they are)")]
    public float idleArmSpread = 28f;   // 285.jpg: mitts clear the egg. 20° sits on the side and reads as a flat nub
    public float idleArmForward = 20f;  // tip toward the face (+Z, the camera) so the arm sits in front of the body
    public float idleLegFront = 40f;    // left foot forward, about 5 cm up — the 285.jpg step, not the 120° hop swing
    public float idleLegBack = -18f;    // right foot back, still on the cloth

    [Header("Run cycle (grounded, |vx| > 0.3)")]
    // 285.mp4 at 1x: a footfall every 0.45–0.5 s, body bob 0.07–0.08 m, ±4.5° side rock,
    // arms stay out through the step and reach a T at the top (3.25 s); one foot lifts, the other stays down.
    public float runLegSwing = 120f;     // short nub: 120° clears the foot by ~0.06 m at run speed
    public float runArmSwing = 10f;      // small counter-swing so the mitts stay out, not reaching
    public float runArmLift = 60f;       // top of the step: rest + this = 80° (a T)
    public float runArmFloor = 46f;      // through the step the mitts stay out (285 at 0.6 s), then rise to the T
    public float runBob = 0.080f;        // metres at the top of the step
    public float runRock = 4.5f;         // side rock per step (deg, about the travel axis)
    public float footfallWalk = 0.50f;   // seconds between steps at backSpeed
    public float footfallRun = 0.45f;    // seconds between steps at runSpeed (stays inside 0.45–0.5)
    public float turnDamp = 9f;          // yaw lerp toward camera-right (run) / camera-left (walk back)

    [Header("Blink (285.mp4: ≈ every 1.4 s, 0.12 s)")]
    public float blinkEveryMin = 1.3f;
    public float blinkEveryMax = 1.7f;
    public float blinkSeconds = 0.12f;

    Rigidbody _rb;
    BeanController _ctl;
    Quaternion _baseYaw;
    float _yawRest, _yaw;                // base yaw (145 → faces camera, turned into the run) and its live mirror
    Vector3 _bodyBasePos;
    readonly List<Vector3> _eyeBase = new List<Vector3>();
    float _air, _rise, _fall, _squint;
    float _armSpread, _armSwing, _legFront, _legBack;
    float _lean, _run, _runPhase;
    float _nextBlink, _blinkUntil;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _ctl = GetComponent<BeanController>();
        _baseYaw = transform.rotation;
        _yawRest = _baseYaw.eulerAngles.y;
        _yaw = _yawRest;
        if (body == null) body = transform.Find("Bean Body");
        if (armL == null) armL = transform.Find("Arm Pivot L");
        if (armR == null) armR = transform.Find("Arm Pivot R");
        if (legL == null) legL = transform.Find("Leg Pivot L");
        if (legR == null) legR = transform.Find("Leg Pivot R");
        if (body != null) _bodyBasePos = body.localPosition;
        if (eyes == null || eyes.Length == 0)
        {
            var list = new List<Transform>();
            foreach (Transform c in transform) if (c.name == "Eye") list.Add(c);
            eyes = list.ToArray();
        }
        _eyeBase.Clear();
        foreach (var e in eyes) _eyeBase.Add(e != null ? e.localScale : Vector3.one);
        // Title runs with the menu up. Seed the stance here so the first frame is the walk,
        // not feet together at the 20° side nubs.
        _armSpread = idleArmSpread;
        _legFront = idleLegFront;
        _legBack = idleLegBack;
        SnapIdle();
        _nextBlink = Time.time + Random.Range(blinkEveryMin, blinkEveryMax);
        // Escape.unity still serializes 55°. Play measured that as the foot staying in the burlap
        // (foot clear −0.07 m). 120° is the swing that put one foot up about 0.06 m at 2.6 m/s.
        runLegSwing = 120f;
    }

    void OnEnable() { BeanImpulse.OnHop += OnHop; BeanController.OnJump += OnHop; }
    void OnDisable() { BeanImpulse.OnHop -= OnHop; BeanController.OnJump -= OnHop; }

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
        if (_ctl == null) _ctl = GetComponent<BeanController>(); // GameSession may add the controller after Awake
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

        // run cycle weight: grounded and actually moving (285.mp4 0–2.75 s: stepping strides along the burlap)
        float speed = Mathf.Abs(v.x);
        bool running = grounded && speed > 0.3f;
        _run = Mathf.Lerp(_run, running ? 1f : 0f, k);
        if (running)
        {
            // 285.mp4 footfall stays 0.45–0.5 s from the walk through the run (two |sin| peaks per cycle)
            float slow = _ctl != null ? _ctl.backSpeed : 1.6f;
            float fast = _ctl != null ? Mathf.Max(slow + 0.01f, _ctl.runSpeed) : 2.8f;
            float period = Mathf.Lerp(footfallWalk, footfallRun, Mathf.Clamp01(Mathf.InverseLerp(slow, fast, speed)));
            _runPhase += dt / (Mathf.Max(0.2f, period) * 2f);
        }
        float stride = Mathf.Sin(_runPhase * Mathf.PI * 2f);
        // 285 step-hop is the high-speed pose: full foot swing, 0.08 m bob, and mitts to a T
        // once speed is in the measured run band. 0.9 × runSpeed (2.52 m/s) is already full.
        float topSpeed = _ctl != null ? Mathf.Max(0.01f, _ctl.runSpeed) : 2.8f;
        float pace = Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.8f, topSpeed * 0.9f, speed)));
        float strideW = _run * pace;
        // 1 while standing or on the title, 0 in the run and in the air, so the hop targets stay put
        float idleW = (1f - _run) * (1f - _air);

        // face camera-right while running (+X), camera-left while walking back: mirror the rest yaw about the camera axis
        int face = _ctl != null ? _ctl.Facing : 1;
        float yawT = face > 0 ? _yawRest : 360f - _yawRest;
        _yaw = Mathf.LerpAngle(_yaw, yawT, 1f - Mathf.Exp(-turnDamp * dt));

        // body lean into the run (world Z axis tips the top toward the travel direction) + a small side rock per step
        float leanTarget = Mathf.Clamp(v.x * leanPerSpeed, -maxLean, maxLean) + _rise * 4f - _fall * 3f;
        _lean = Mathf.Lerp(_lean, leanTarget, k);
        float rock = stride * runRock * strideW;
        transform.rotation = Quaternion.AngleAxis(-_lean, Vector3.forward) * Quaternion.AngleAxis(rock, Vector3.right) * Quaternion.Euler(0f, _yaw, 0f);

        // arms: standing holds the 285.jpg spread and forward pitch; a run still reaches a T at the top of the step
        float runOut = Mathf.Lerp(runArmFloor, armRestSpread + runArmLift, Mathf.Abs(stride));
        float hopSpread = Mathf.Lerp(armRestSpread, runOut, strideW) + (armAirSpread - armRestSpread) * _air;
        hopSpread = Mathf.Min(hopSpread, armRestSpread + runArmLift); // near-T at the top of the step, not past it
        float spreadT = Mathf.Lerp(idleArmSpread, hopSpread, 1f - idleW);
        float swingT = armBackSwing * _rise + armReach * _fall;
        if (grounded) swingT += Mathf.Sin(Time.time * 1.7f) * 2.5f * (1f - _run); // idle sway
        swingT += -stride * runArmSwing * strideW;
        float armBreath = grounded ? Mathf.Sin(Time.time * breathHz * Mathf.PI * 2f) * (breathAmp * 170f) * (1f - _run) : 0f;
        _armSpread = Mathf.Lerp(_armSpread, spreadT + armBreath, k);
        _armSwing = Mathf.Lerp(_armSwing, swingT, k);

        // legs: split in the air; on the ground one foot lifts and the other stays planted (285 at 0.6 s and 3.25 s)
        float lift = runLegSwing * strideW;
        float frontT = idleLegFront * idleW + _rise * legSplit + _fall * legTuck + Mathf.Max(0f, stride) * lift;
        float backT = idleLegBack * idleW - _rise * legSplit * 0.8f + _fall * legTuck + Mathf.Max(0f, -stride) * lift;
        _legFront = Mathf.Lerp(_legFront, frontT, k);
        _legBack = Mathf.Lerp(_legBack, backT, k);

        // root-local axes for "toward travel (+X)" and "out to the side"
        Vector3 travelL = transform.InverseTransformDirection(Vector3.right);
        Vector3 swingAxis = Vector3.Cross(Vector3.down, travelL).normalized;
        // positive swing carries the foot forward along the run and up off the burlap
        Vector3 liftAxis = swingAxis;
        if (liftAxis.sqrMagnitude < 1e-6f) liftAxis = Vector3.right;
        if (Vector3.Dot(Quaternion.AngleAxis(50f, liftAxis) * Vector3.down, travelL) < 0f)
            liftAxis = -liftAxis;

        float armForward = idleArmForward * idleW;
        Pose(armL, _armSwing, _armSpread, -1, swingAxis, armForward);
        Pose(armR, _armSwing, _armSpread, +1, swingAxis, armForward);
        // left leg leads (frames 285-01/02: left foot forward); one foot up, the other planted
        Pose(legL, _legFront, 0f, -1, liftAxis);
        Pose(legR, _legBack, 0f, +1, liftAxis);

        // bob only. The felt body is not scaled here — LandFeel applies a very small settle on landing.
        if (body != null)
            body.localPosition = _bodyBasePos + Vector3.up * (Mathf.Abs(stride) * runBob * strideW);
        // blink: bead eyes close for ~0.12 s every 1.3–1.7 s (285.mp4 3.55 s / 4.95 s), never mid-apex squint
        float now = Time.time;
        if (now >= _nextBlink && _squint < 0.3f)
        {
            _blinkUntil = now + blinkSeconds;
            _nextBlink = now + Random.Range(blinkEveryMin, blinkEveryMax);
        }
        float blink = now < _blinkUntil ? 1f - Mathf.Abs((_blinkUntil - now) / blinkSeconds * 2f - 1f) : 0f; // triangle 0→1→0
        float lidY = Mathf.Min(Mathf.Lerp(1f, 0.28f, _squint), Mathf.Lerp(1f, 0.08f, blink));
        int eyeN = eyes != null ? Mathf.Min(eyes.Length, _eyeBase.Count) : 0;
        for (int i = 0; i < eyeN; i++)
        {
            if (eyes[i] == null) continue;
            var s = _eyeBase[i];
            eyes[i].localScale = new Vector3(s.x, s.y * lidY, s.z);
        }
    }

    /// Editor build and the first frame. LateUpdate does not run until Play, so the
    /// saved scene would otherwise show feet together.
    public void SnapIdle()
    {
        if (armL == null) armL = transform.Find("Arm Pivot L");
        if (armR == null) armR = transform.Find("Arm Pivot R");
        if (legL == null) legL = transform.Find("Leg Pivot L");
        if (legR == null) legR = transform.Find("Leg Pivot R");
        Vector3 travelL = transform.InverseTransformDirection(Vector3.right);
        Vector3 swingAxis = Vector3.Cross(Vector3.down, travelL).normalized;
        if (swingAxis.sqrMagnitude < 1e-6f) swingAxis = Vector3.right;
        Vector3 liftAxis = swingAxis;
        if (Vector3.Dot(Quaternion.AngleAxis(50f, liftAxis) * Vector3.down, travelL) < 0f)
            liftAxis = -liftAxis;
        Pose(armL, 0f, idleArmSpread, -1, swingAxis, idleArmForward);
        Pose(armR, 0f, idleArmSpread, +1, swingAxis, idleArmForward);
        Pose(legL, idleLegFront, 0f, -1, liftAxis);
        Pose(legR, idleLegBack, 0f, +1, liftAxis);
    }

    static void Pose(Transform pivot, float swing, float spread, int side, Vector3 swingAxis, float forward = 0f)
    {
        if (pivot == null) return;
        // spread: tip moves away from the body, outward along the pivot's side
        Vector3 sideL = Vector3.right * side;
        Vector3 spreadAxis = Vector3.Cross(Vector3.down, sideL).normalized;
        var rot = Quaternion.AngleAxis(swing, swingAxis) * Quaternion.AngleAxis(spread, spreadAxis);
        // forward 0 leaves the hop quaternion unchanged. Positive tips a hanging arm toward the face (+Z).
        if (forward != 0f)
            rot = Quaternion.AngleAxis(forward, Vector3.left) * rot;
        pivot.localRotation = rot;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// Run AND jump by touching the screen (touch + mouse + keyboard).
///  • hold anywhere on the RIGHT half (or D / →)  = run forward (+X) up to runSpeed, ground acceleration, reduced air control
///  • hold anywhere on the LEFT half  (or A / ←)  = walk back at backSpeed
///  • quick tap anywhere (shorter than tapMaxSeconds, little movement), swipe up, or Space = jump
///    (play hopImpulse 5.2 straight up — BeanImpulse keeps 6.4 for the proofs — plus the run speed the bean
///     already has). A tap is a skip-hop only when |vx| < 2; a tap at run speed keeps the full arc.
///    Coyote time, jump buffer, variable height (release early → vy × cutFactor).
///  • touches that begin over UI (EventSystem.IsPointerOverGameObject) are ignored.
/// BeanImpulse stays on the bean for the physics proofs (they read its fields and AddForce themselves); this
/// component disables it in Start. Rigidbody mass / drag / collider are never touched here.
public class BeanController : MonoBehaviour
{
    [Header("Run")]
    public float runSpeed = 2.8f;       // m/s, hold right half. Measured run 2.6–2.8. Damped air time ~0.50 s → 1.40 m centre travel (capsule r 0.38)
    public float backSpeed = 1.6f;      // m/s, hold left half
    public float groundAccel = 16f;     // m/s² toward the wanted speed on the burlap
    public float groundBrake = 22f;     // m/s² back to rest when nothing is held
    public float airAccel = 5f;         // reduced air control

    [Header("Jump (play values — BeanImpulse keeps 6.4 for the proofs)")]
    // Measured on the bean (mass 0.9, linear damping 0.18, gravity -22): 5.2 N·s rises 0.68 m, ~0.50 s aloft.
    public float hopImpulse = 5.2f;
    public float tapCutAtFraction = 0.75f; // quick tap = skip-hop: trim vy once it falls below this × take-off vy (apex ≈ 0.46 m)
    public float coyoteTime = 0.10f;
    public float jumpBuffer = 0.10f;
    public float cutFactor = 0.55f;     // release early → vertical velocity × this

    [Header("Touch")]
    public float tapMaxSeconds = 0.18f;
    public float tapMaxMoveInches = 0.10f;
    public float swipeUpInches = 0.28f;
    public float swipeMaxSeconds = 0.35f;

    /// Fired after every jump (audio / stretch / pose hooks — the BeanImpulse.OnHop equivalent).
    public static event System.Action OnJump;

    /// +1 facing camera-right (run direction), −1 facing camera-left (walking back).
    public int Facing { get; private set; } = 1;
    /// −1 .. +1 horizontal input this frame (−1 = walk back, +1 = run).
    public float InputX { get; private set; }
    public bool Grounded { get; private set; }
    /// Seconds spent running on the ground; set once the player has run and then jumped (HUD hint fade).
    public float RunTime { get; private set; }
    public bool HasRunAndJumped { get; private set; }

    class Pointer
    {
        public int id;             // touch fingerId, −1 for the mouse
        public Vector2 start, pos;
        public float t0;
        public bool overUi, jumped;
    }

    readonly List<Pointer> _ptrs = new List<Pointer>();
    Rigidbody _rb;
    float _coyote, _buffer;
    bool _jumpHeld;        // a holdable jump source (Space / swipe finger) is still down
    bool _cutDone;
    bool _releaseQueued;
    bool _tapRequested, _tapHop;   // quick tap → skip-hop (trimmed arc), hold / swipe → full arc
    float _takeoffVy;
    float _tapMovePx, _swipePx;
    int _holdJumpPointer = int.MinValue; // pointer id that owns the current held jump (swipe)
    bool _spaceJump;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // side-scroll lane: no roll/yaw from contacts, never drift off the z = 0 lane (same as BeanImpulse)
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        float dpi = Screen.dpi > 20f ? Screen.dpi : 160f;
        _tapMovePx = tapMaxMoveInches * dpi;
        _swipePx = swipeUpInches * dpi;
    }

    void Start()
    {
        var impulse = GetComponent<BeanImpulse>();
        if (impulse != null)
            impulse.enabled = false; // taps are read here now; component (and its 5.2 / 6.4 / 0.13) stays for PhysicsProof / EscapeCLI
    }

    void OnDisable()
    {
        _ptrs.Clear();
        InputX = 0f;
        _buffer = 0f;
        _jumpHeld = false;
        _spaceJump = false;
        _holdJumpPointer = int.MinValue;
    }

    // ——— input (Update: EventSystem queries and Input.* are frame-based) ———

    void Update()
    {
        if (Time.timeScale <= 0f) { InputX = 0f; return; }
        float now = Time.unscaledTime;

        // touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    Begin(t.fingerId, t.position, now, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId));
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    Move(t.fingerId, t.position, now);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    End(t.fingerId, t.position, now, t.phase == TouchPhase.Ended);
                    break;
            }
        }
        // mouse (desktop; skipped while real touches are active so simulated mouse events never double up)
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0))
                Begin(-1, Input.mousePosition, now, EventSystem.current != null && EventSystem.current.IsPointerOverGameObject());
            else if (Input.GetMouseButton(0))
                Move(-1, Input.mousePosition, now);
            if (Input.GetMouseButtonUp(0))
                End(-1, Input.mousePosition, now, true);
        }

        // keyboard
        float key = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) key += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) key -= 1f;
        if (Input.GetKeyDown(KeyCode.Space)) { RequestJump(); _spaceJump = true; _jumpHeld = true; }
        if (Input.GetKeyUp(KeyCode.Space) && _spaceJump) { _spaceJump = false; Release(); }

        // held pointers → run / walk back by screen half (right half wins when both are held)
        float hold = 0f;
        float half = Screen.width * 0.5f;
        for (int i = 0; i < _ptrs.Count; i++)
        {
            var p = _ptrs[i];
            if (p.overUi) continue;
            bool isHold = (now - p.t0) >= tapMaxSeconds || (p.pos - p.start).sqrMagnitude > _tapMovePx * _tapMovePx;
            if (!isHold) continue;
            if (p.pos.x >= half) hold = 1f;
            else if (hold <= 0f) hold = -1f;
        }
        InputX = Mathf.Clamp(key + hold, -1f, 1f);
        if (InputX > 0.1f) Facing = 1;
        else if (InputX < -0.1f) Facing = -1;
    }

    void Begin(int id, Vector2 pos, float now, bool overUi)
    {
        var p = Find(id);
        if (p == null) { p = new Pointer(); _ptrs.Add(p); }
        p.id = id; p.start = pos; p.pos = pos; p.t0 = now; p.overUi = overUi; p.jumped = false;
    }

    void Move(int id, Vector2 pos, float now)
    {
        var p = Find(id);
        if (p == null) return;
        p.pos = pos;
        if (p.overUi || p.jumped) return;
        // swipe up = jump, finger stays down → holdable (release early cuts the height)
        if (pos.y - p.start.y > _swipePx && now - p.t0 <= swipeMaxSeconds)
        {
            p.jumped = true;
            RequestJump();
            _holdJumpPointer = id;
            _jumpHeld = true;
        }
    }

    void End(int id, Vector2 pos, float now, bool clean)
    {
        var p = Find(id);
        if (p == null) return;
        _ptrs.Remove(p);
        if (p.overUi) return;
        if (id == _holdJumpPointer) { _holdJumpPointer = int.MinValue; Release(); }
        if (p.jumped || !clean) return;
        bool quick = now - p.t0 < tapMaxSeconds;
        bool still = (pos - p.start).sqrMagnitude <= _tapMovePx * _tapMovePx;
        if (quick && still) RequestJump(true); // tap = skip-hop (285.mp4 step-hop scale); hold / swipe = full arc
    }

    Pointer Find(int id)
    {
        for (int i = 0; i < _ptrs.Count; i++) if (_ptrs[i].id == id) return _ptrs[i];
        return null;
    }

    void RequestJump(bool tap = false) { _buffer = jumpBuffer; _tapRequested = tap; }

    void Release()
    {
        if (!_jumpHeld) return;
        _jumpHeld = _spaceJump || _holdJumpPointer != int.MinValue;
        if (!_jumpHeld) _releaseQueued = true;
    }

    // ——— physics ———

    bool CheckGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, 0.58f, ~0, QueryTriggerInteraction.Ignore);
    }

    void FixedUpdate()
    {
        if (_rb == null) return;
        float dt = Time.fixedDeltaTime;
        Grounded = CheckGrounded();
        var v = _rb.linearVelocity;

        if (Grounded && v.y <= 0.3f) _coyote = coyoteTime;
        else _coyote -= dt;
        _buffer -= dt;

        // horizontal: force toward the wanted speed, softer in the air, brake on the burlap when nothing is held
        float want = InputX > 0.1f ? runSpeed : (InputX < -0.1f ? -backSpeed : 0f);
        float accel;
        if (Grounded) accel = want != 0f ? groundAccel : groundBrake;
        else accel = want != 0f ? airAccel : 0f;
        if (accel > 0f) v.x = Mathf.MoveTowards(v.x, want, accel * dt);
        if (Grounded && Mathf.Abs(v.x) > 1.2f) RunTime += dt;

        // jump: buffered request + coyote window
        if (_buffer > 0f && _coyote > 0f)
        {
            if (v.y < 0f) v.y = 0f;
            _rb.linearVelocity = v;
            _rb.AddForce(Vector3.up * hopImpulse, ForceMode.Impulse);
            _takeoffVy = hopImpulse / Mathf.Max(0.01f, _rb.mass);
            // a tap while standing or strolling is the small 285 step-hop; a tap at run speed is the full
            // gap-clearing arc so players never fall short at a gap for tapping instead of swiping
            _tapHop = _tapRequested && Mathf.Abs(v.x) < 2.0f;
            _tapRequested = false;
            _coyote = 0f;
            _buffer = 0f;
            _cutDone = false;
            _releaseQueued = false;
            if (RunTime > 0.4f) HasRunAndJumped = true;
            OnJump?.Invoke();
            return;
        }

        // variable height: an early release while still rising trims the arc; a quick tap trims it automatically
        if (_tapHop && !_cutDone && v.y > 0f && v.y < _takeoffVy * tapCutAtFraction) _releaseQueued = true;
        if (_releaseQueued && !_cutDone)
        {
            _releaseQueued = false;
            _cutDone = true;
            _tapHop = false;
            if (v.y > 0.5f) v.y *= cutFactor;
        }
        _rb.linearVelocity = v;
    }
}

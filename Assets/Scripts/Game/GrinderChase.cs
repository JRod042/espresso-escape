using UnityEngine;

/// L5 boss beat: a giant felt grinder roller wakes when the bean reaches the raised kraft run and rolls after it
/// to the goal. If it catches the bean, the bean goes back to its checkpoint and the roller drops back behind it.
/// Look: brown felt drum with felt-red bands and cream teeth (rotate-grinder.mp4 burr, ref-285 red pompom tree).
public class GrinderChase : MonoBehaviour
{
    public float speed = 2.1f;        // bean runs 2.8 m/s, so a clean run pulls ahead
    public float catchUpSpeed = 3.0f; // rubber band when the bean is far ahead
    public float catchUpGap = 8f;
    public float startBehind = 6f;
    const float Radius = 1.05f;
    const float Length = 2.2f;
    const float CatchReach = 0.35f;  // roller face to bean centre
    const int Teeth = 10;
    const float YFollow = 6f;

    LevelRunner _runner;
    float _wakeX, _stopX, _front, _y;
    Transform _drum;

    public bool IsAwake { get; private set; }
    public float Front => _front;

    /// Fired once when the grinder wakes, for the HUD banner.
    public static event System.Action OnWake;

    public void Init(LevelRunner runner, float wakeX, float stopX)
    {
        _runner = runner;
        _wakeX = wakeX;
        _stopX = stopX - 1f;
        _drum = new GameObject("Grinder Drum").transform;
        _drum.SetParent(transform, false);
        var body = LevelFeatures.Decor(PrimitiveType.Cylinder, "Drum", _drum, LevelFeatures.Felt("Look/tex-felt-brown", Color.white, 2f));
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(Radius * 2f, Length * 0.5f, Radius * 2f);
        var red = LevelFeatures.Felt("Look/tex-felt-red", Color.white);
        for (int i = -1; i <= 1; i += 2)
        {
            var band = LevelFeatures.Decor(PrimitiveType.Cylinder, "Band", _drum, red);
            band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            band.transform.localPosition = new Vector3(0f, 0f, i * Length * 0.36f);
            band.transform.localScale = new Vector3(Radius * 2.08f, 0.09f, Radius * 2.08f);
        }
        var cream = LevelFeatures.Felt("Look/tex-felt-cream", Color.white);
        for (int i = 0; i < Teeth; i++)
        {
            float a = i * Mathf.PI * 2f / Teeth;
            var tooth = LevelFeatures.Decor(PrimitiveType.Cube, "Tooth", _drum, cream);
            tooth.transform.localPosition = new Vector3(Mathf.Cos(a) * Radius, Mathf.Sin(a) * Radius, 0f);
            tooth.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            tooth.transform.localScale = new Vector3(0.22f, 0.16f, Length * 0.55f);
        }
        _drum.gameObject.SetActive(false);
        LevelRunner.OnRespawn += HandleRespawn;
    }

    void OnDestroy() => LevelRunner.OnRespawn -= HandleRespawn;

    void HandleRespawn()
    {
        if (!IsAwake || _runner == null) return;
        if (_runner.CheckpointX < _wakeX)
        {
            IsAwake = false; // fell back before the run: re-arm
            _drum.gameObject.SetActive(false);
            return;
        }
        _front = _runner.CheckpointX - startBehind;
    }

    static float TopAt(float x)
    {
        for (int i = 0; i < LevelRunner.Blocks.Count; i++)
        {
            var b = LevelRunner.Blocks[i];
            if (x >= b.x && x <= b.y) return i < LevelRunner.BlockTop.Count ? LevelRunner.BlockTop[i] : 0f;
        }
        return 0f;
    }

    void Update()
    {
        if (_runner == null || _runner.bean == null) return;
        bool playing = GameSession.I == null || GameSession.I.phase == GameSession.Phase.Play;
        float bx = _runner.bean.position.x;
        if (!IsAwake)
        {
            if (!playing || bx < _wakeX) return;
            IsAwake = true;
            _front = bx - startBehind;
            _y = Radius + TopAt(_front - Radius);
            _drum.gameObject.SetActive(true);
            EscapeAudio.Play(EscapeAudio.Sfx.Whoosh, 0.9f); // P0 whoosh; the CC0 grinder loop (Freesound #201600) needs a login, not imported
            OnWake?.Invoke();
        }
        if (!playing) return;

        float v = bx - _front > catchUpGap ? catchUpSpeed : speed;
        float step = v * Time.deltaTime;
        _front = Mathf.Min(_front + step, _stopX);
        float centre = _front - Radius;
        _y = Mathf.Lerp(_y, Radius + TopAt(centre), 1f - Mathf.Exp(-YFollow * Time.deltaTime));
        _drum.position = new Vector3(centre, _y, 0f);
        _drum.Rotate(0f, 0f, -step / Radius * Mathf.Rad2Deg, Space.World);

        if (_front >= bx - CatchReach && _runner.SinceRespawn > 0.5f)
            _runner.Respawn();
    }
}

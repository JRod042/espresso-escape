using UnityEngine;

/// Honey bean collectible: trigger sphere, slow bob + spin, counted by GameSession.
public class Pickup : MonoBehaviour
{
    public float bob = 0.05f;
    public float spin = 70f;
    Vector3 _base;
    float _phase;
    bool _taken;

    void Start()
    {
        _base = transform.position;
        _phase = Random.value * Mathf.PI * 2f;
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.19f;
            col = sc;
        }
        col.isTrigger = true;
    }

    void Update()
    {
        if (_taken) return;
        float t = Time.time + _phase;
        transform.position = _base + Vector3.up * (Mathf.Sin(t * 2.2f) * bob);
        transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_taken) return;
        if (other.GetComponent<BeanImpulse>() == null && other.GetComponentInParent<BeanImpulse>() == null) return;
        _taken = true;
        if (GameSession.I != null) GameSession.I.CollectBean();
        EscapeAudio.Play(EscapeAudio.Sfx.Pickup);
        Destroy(gameObject);
    }
}

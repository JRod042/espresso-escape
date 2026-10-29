using System.Collections.Generic;
using UnityEngine;

/// Imagine 285.mp4: the walking bean nudges felt moss balls and they roll away. This drops a few light,
/// kickable moss balls and roast beans on long flat stretches of each level (never near a gap, step,
/// checkpoint, vent or the goal) and matches the bean's step cadence to bean-motion.mp4 (0.52–0.55 s/step).
/// Runtime only: no scene or builder edits. Hooks LevelRunner.OnLevelLoaded (fires after LevelFeatures.Build).
public static class FeltKickables
{
    public const float StepRun = 0.52f, StepWalk = 0.56f; // bean-motion.mp4 eye bob 1.83–1.94 Hz
    public const float MossDiameter = 0.22f, MossMass = 0.12f, BeanLength = 0.17f, BeanMass = 0.03f;
    static GameObject _root;
    public static readonly List<Rigidbody> Bodies = new List<Rigidbody>();
    static PhysicsMaterial _felt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        LevelRunner.OnLevelLoaded -= Build;
        LevelRunner.OnLevelLoaded += Build;
    }

    static void Build(LevelCatalog.LevelDef def)
    {
        if (_root != null) Object.Destroy(_root);
        Bodies.Clear();
        var anim = Object.FindAnyObjectByType<BeanAnimator>();
        if (anim != null) { anim.footfallRun = StepRun; anim.footfallWalk = StepWalk; }

        _root = new GameObject("Felt Kickables");
        if (_felt == null)
            _felt = new PhysicsMaterial("Felt Kick") { dynamicFriction = 0.7f, staticFriction = 0.8f, bounciness = 0.04f,
                frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum };
        var rng = new System.Random(def.seed ^ 0x5eed);
        int moss = 0, beans = 0;
        for (int i = 0; i < LevelRunner.Blocks.Count && (moss < 5 || beans < 6); i++)
        {
            var b = LevelRunner.Blocks[i];
            float top = i < LevelRunner.BlockTop.Count ? LevelRunner.BlockTop[i] : 0f;
            for (float x = b.x + 1.4f; x < b.y - 2.2f; x += 3.2f + (float)rng.NextDouble() * 2f)
            {
                if (x > def.goalZ - 2.5f || x < 2.0f) break;
                if (!Clear(x, top)) continue;
                float z = -0.15f + (float)rng.NextDouble() * 0.35f;
                if (moss < 5 && (beans >= 6 || rng.NextDouble() < 0.5))
                {
                    var go = FeltSetProps.MossBall(new Vector3(x, top, z), MossDiameter, rng.Next(1, 90));
                    var col = go.AddComponent<SphereCollider>();
                    col.radius = MossDiameter * 0.5f; col.center = new Vector3(0f, MossDiameter * 0.5f, 0f);
                    Add(go, col, MossMass, 0.45f, 0.9f); moss++;
                }
                else
                {
                    var go = FeltSetProps.RoastBean(new Vector3(x, top, z), BeanLength, (float)rng.NextDouble() * 180f);
                    var col = go.AddComponent<BoxCollider>();
                    col.size = new Vector3(BeanLength * 0.95f, BeanLength * 0.36f, BeanLength * 0.62f);
                    col.center = new Vector3(0f, BeanLength * 0.18f, 0f);
                    Add(go, col, BeanMass, 0.8f, 1.6f); beans++;
                }
            }
        }
    }

    // the whole kick-run ahead [x−0.6, x+2] must be flat block top (no gap, step or ramp), and nothing gameplay nearby
    static bool Clear(float x, float top)
    {
        for (float s = x - 0.6f; s <= x + 2.0f; s += 0.25f)
        {
            if (LevelRunner.IsOverGap(s)) return false;
            if (!Physics.Raycast(new Vector3(s, top + 2f, 0f), Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(hit.point.y - top) > 0.07f) return false;
        }
        foreach (var c in Physics.OverlapBox(new Vector3(x + 0.5f, top + 0.6f, 0f), new Vector3(1.4f, 0.55f, 1.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
            if ((c.isTrigger || c.attachedRigidbody != null) && c.GetComponentInParent<BeanController>() == null) return false; // honey, flags, vents, lifts, goal (not the bean)
        return true;
    }

    static void Add(GameObject go, Collider col, float mass, float drag, float angDrag)
    {
        go.transform.SetParent(_root.transform, true);
        col.sharedMaterial = _felt;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass; rb.linearDamping = drag; rb.angularDamping = angDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.sleepThreshold = 0.02f;
        go.AddComponent<KickableReset>();
        Bodies.Add(rb);
    }
}

/// Fallen kickables are put to sleep off-screen instead of falling forever.
public class KickableReset : MonoBehaviour
{
    void FixedUpdate() { if (transform.position.y < -4f) gameObject.SetActive(false); }
}

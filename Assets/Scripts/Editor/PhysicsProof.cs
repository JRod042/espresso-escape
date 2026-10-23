using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PhysicsProof
{
    [MenuItem("Escape/Physics Proof")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Escape.unity", OpenSceneMode.Single);
        var bean = GameObject.Find("Imagine Bean");
        if (bean == null)
        {
            Debug.LogError("PHYSICS_PROOF_FAIL no Imagine Bean");
            EditorApplication.Exit(2);
            return;
        }
        var rb = bean.GetComponent<Rigidbody>();
        var impulse = bean.GetComponent<BeanImpulse>();
        Physics.gravity = new Vector3(0f, -22f, 0f);
        Time.fixedDeltaTime = 0.02f;
        Physics.simulationMode = SimulationMode.Script;

        float a1 = Apex(bean, rb, impulse);
        float a2 = Apex(bean, rb, impulse);
        float delta = Mathf.Abs(a1 - a2);
        bool pass = a1 > 0.35f && delta < 0.1f;
        Debug.Log($"PHYSICS_PROOF apex1={a1:F3} apex2={a2:F3} delta={delta:F3} pass={pass}");
        Physics.simulationMode = SimulationMode.FixedUpdate;
        EditorApplication.Exit(pass ? 0 : 3);
    }

    static float Apex(GameObject bean, Rigidbody rb, BeanImpulse impulse)
    {
        bean.transform.position = new Vector3(0f, 1.2f, 0.35f);
        bean.transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();

        float hop = impulse != null ? impulse.hopImpulse : 6.4f;
        float fwd = impulse != null ? impulse.forwardImpulse : 5.2f;
        // ForceMode.Impulse with mass 0.9
        rb.AddForce(Vector3.forward * fwd + Vector3.up * hop, ForceMode.Impulse);

        float startY = bean.transform.position.y;
        float peak = startY;
        for (int i = 0; i < 120; i++)
        {
            Physics.Simulate(0.02f);
            if (bean.transform.position.y > peak) peak = bean.transform.position.y;
        }
        return peak - startY;
    }
}

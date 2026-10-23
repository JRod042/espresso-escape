using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Finds named set pieces by walking the loaded scene.
/// Replaces FindObjectsByType of every Transform, which allocates and sorts that whole array
/// (AmbientMotion was doing it every few seconds; TitleDolly did it on each title shot).
public static class SetNameQuery
{
    const int MaxNames = 4;
    static readonly string[] Names = new string[MaxNames];
    static readonly List<Transform>[] Lists = new List<Transform>[MaxNames];
    static readonly List<Transform> Stack = new List<Transform>(512);
    static int _count;

    public static void Collect(string n0, List<Transform> a0, string n1, List<Transform> a1, string n2, List<Transform> a2)
    {
        Names[0] = n0; Lists[0] = a0;
        Names[1] = n1; Lists[1] = a1;
        Names[2] = n2; Lists[2] = a2;
        _count = 3;
        Walk();
    }

    public static void Collect(string n0, List<Transform> a0, string n1, List<Transform> a1, string n2, List<Transform> a2, string n3, List<Transform> a3)
    {
        Names[0] = n0; Lists[0] = a0;
        Names[1] = n1; Lists[1] = a1;
        Names[2] = n2; Lists[2] = a2;
        Names[3] = n3; Lists[3] = a3;
        _count = 4;
        Walk();
    }

    static void Walk()
    {
        Stack.Clear();
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var go = roots[i];
            if (go != null && go.activeInHierarchy) Stack.Add(go.transform);
        }
        while (Stack.Count > 0)
        {
            int last = Stack.Count - 1;
            var t = Stack[last];
            Stack.RemoveAt(last);
            if (t == null) continue;
            string n = t.name;
            for (int i = 0; i < _count; i++)
            {
                if (n == Names[i] && Lists[i] != null) Lists[i].Add(t);
            }
            int children = t.childCount;
            for (int c = 0; c < children; c++)
            {
                var child = t.GetChild(c);
                if (child != null && child.gameObject.activeInHierarchy) Stack.Add(child);
            }
        }
    }
}

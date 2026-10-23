using System.IO;
using UnityEditor;

// One-shot for look-match round 4. Deletes its own flag, builds, then captures.
[InitializeOnLoad]
public static class LookRound1
{
    const string Flag = "Temp/look-round-1.flag";

    static LookRound1()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        string flag = Path.GetFullPath(Flag);
        if (!File.Exists(flag)) return;
        File.Delete(flag);
        BuildEscapeScene.Build();
        CaptureLookShot.Capture();
    }
}

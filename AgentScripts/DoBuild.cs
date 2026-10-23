using UnityEditor;

public static class DoBuild
{
    public static string Main()
    {
        if (EditorApplication.isPlaying) return "STILL_PLAYING";
        BuildEscapeScene.Build();
        return "BUILT";
    }
}

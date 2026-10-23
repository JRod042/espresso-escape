using UnityEditor;

public static class ReimportBurlap
{
    public static string Main()
    {
        string[] paths =
        {
            "Assets/Look/tex-burlap.png",
            "Assets/Look/tex-burlap-normal.png",
            "Assets/Resources/Look/tex-burlap.png",
            "Assets/Resources/Look/tex-burlap-normal.png"
        };
        for (int i = 0; i < paths.Length; i++)
            AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        return "REIMPORT_OK " + paths.Length;
    }
}

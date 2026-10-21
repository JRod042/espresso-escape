using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CaptureLookShot
{
    [MenuItem("Escape/Capture Look Shot")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Escape.unity", OpenSceneMode.Single);
        var cam = Camera.main;
        if (cam == null)
        {
            var go = GameObject.Find("Set Camera");
            if (go != null) cam = go.GetComponent<Camera>();
        }
        if (cam == null)
        {
            Debug.LogError("CAPTURE_FAIL no camera");
            EditorApplication.Exit(2);
            return;
        }

        int w = 1280, h = 720;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        var prevActive = RenderTexture.active;
        cam.targetTexture = rt;
        // Null gfx (-nographics) fatals in Soft shadow maps; keep shadows on real DISPLAY
        bool nullGfx = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        Light[] lights = null;
        LightShadows[] prevShadows = null;
        ShadowQuality prevQ = QualitySettings.shadows;
        if (nullGfx)
        {
            lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            prevShadows = new LightShadows[lights.Length];
            for (int i = 0; i < lights.Length; i++) { prevShadows[i] = lights[i].shadows; lights[i].shadows = LightShadows.None; }
            QualitySettings.shadows = ShadowQuality.Disable;
        }
        cam.Render();
        if (nullGfx)
        {
            for (int i = 0; i < lights.Length; i++) lights[i].shadows = prevShadows[i];
            QualitySettings.shadows = prevQ;
        }
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = prev;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        var outPath = Path.GetFullPath("Assets/Look/refs/cam-shot.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"CAPTURE_OK {outPath}");
        AssetDatabase.Refresh();
        // only quit when driven from the command line; from the menu this must not close the Editor
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}

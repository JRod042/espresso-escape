using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CaptureLookShot
{
    public static bool LastBlank { get; private set; }

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
            if (Application.isBatchMode) EditorApplication.Exit(2);
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
            lights = Object.FindObjectsByType<Light>();
            prevShadows = new LightShadows[lights.Length];
            for (int i = 0; i < lights.Length; i++) { prevShadows[i] = lights[i].shadows; lights[i].shadows = LightShadows.None; }
            QualitySettings.shadows = ShadowQuality.Disable;
        }
        // Camera.Render skips the URP post stack, so depth of field never reached cam-shot.
        var postRequest = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = rt };
        bool posted = RenderPipeline.SupportsRenderRequest(cam, postRequest);
        if (posted) RenderPipeline.SubmitRenderRequest(cam, postRequest);
        else
        {
            cam.Render();
            cam.Render();
        }
        if (nullGfx)
        {
            for (int i = 0; i < lights.Length; i++) lights[i].shadows = prevShadows[i];
            QualitySettings.shadows = prevQ;
        }
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        LastBlank = NearlyClear(tex);
        cam.targetTexture = prev;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        var outPath = Path.GetFullPath("Assets/Look/refs/cam-shot.png");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        // The imported PNG stays memory-mapped. Release it or the overwrite throws Win32 1224.
        AssetDatabase.ReleaseCachedFileHandles();
        var png = tex.EncodeToPNG();
        var tmp = outPath + ".writing";
        File.WriteAllBytes(tmp, png);
        if (File.Exists(outPath)) File.Delete(outPath);
        File.Move(tmp, outPath);
        Object.DestroyImmediate(tex);
        Debug.Log(LastBlank ? "CAPTURE_BLANK" : $"CAPTURE_OK {outPath}");
        AssetDatabase.Refresh();
        // only quit when driven from the command line; from the menu this must not close the Editor
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static bool NearlyClear(Texture2D tex)
    {
        var px = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;
        int differ = 0;
        int samples = 0;
        var origin = px[(w / 2) + (h / 2) * w];
        for (int y = 8; y < h; y += 24)
        {
            for (int x = 8; x < w; x += 24)
            {
                samples++;
                var c = px[x + y * w];
                int d = Mathf.Abs(c.r - origin.r) + Mathf.Abs(c.g - origin.g) + Mathf.Abs(c.b - origin.b);
                if (d > 28) differ++;
            }
        }
        return samples == 0 || differ * 20 < samples;
    }
}

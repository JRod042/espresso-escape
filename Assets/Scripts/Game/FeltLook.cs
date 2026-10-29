using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Batch 4 graphics match (Imagine 285.jpg), runtime only. Assets/Settings/EscapeLook.asset is NOT edited.
///  • Post-processing was never rendered: the camera's renderPostProcessing was off, so the EscapeLook DOF,
///    grade and vignette never reached a frame. This turns it on for the main camera.
///  • A global "Felt Look" volume (priority 10, profile built in code) tuned for the batch-2 camera: Gaussian DOF
///    soft from just behind the bean to the kraft wall (285's soft trees and garland), a warm low-contrast grade,
///    warm lifted shadows and a soft brown vignette. Gaussian, low-quality sampling: cheap enough for iOS.
///  • Moss: dark fuzzy olive like 285 (was lime). One darkened clone per shared moss material.
///  • Play garland: 285's cloud garland on twine, hung low on the camera-following kraft wall in play
///    (the wall's own garland at y 3.1 sits above the closer batch-2 play frame). Hidden in menus.
public static class FeltLook
{
    public const int VolumePriority = 10;
    public const bool DofEnabled = true;
    // Look step 01 (lighting + volume). Bokeh DoF = 285's near AND far blur; focus follows the bean's camera depth.
    // Works only because the Felt SSAO feature now uses Source = Depth (DepthNormals left the depth texture empty:
    // the felt shaders have no DepthNormals pass, so every pixel read as far).
    // Step 01 r2 (spec D1/D2/D3/D4/D5/D7/D8/D9/D10). Starting values are the spec ESTIMATES; final values are the
    // ones tuned against the measured ref-285 targets (step01-results.md, Diag_LookSweep captures).
    public const float DofFocalMm = 85f, DofAperture = 1.4f, DofFocusDefault = 3.6f;
    public const float PostExposure = 0f, Contrast = 15f, Saturation = -16f; // step01 sweep 3 c03 (measured: L50 42.4, p2 9.2, C 28.2)
    public const TonemappingMode Tone = TonemappingMode.Neutral; // D5: soft, hue-preserving shoulder (ACES twists hue)
    public const float WbTemperature = 3f, WbTint = -8f;         // step01 sweep 3 c03: a* 9.7 (tint +5 gave 11.4)
    public const float BloomThreshold = 1.1f, BloomIntensity = 0.05f; // D10: no visible glow in 285
    public const float VignetteIntensity = 0.15f;                // D9: edge/centre 0.93
    public static readonly Vector3 KeyEuler = new Vector3(58f, 330f, 0f); // step01 sweep 1: moss bright_off matches ref (yaw 208 back-lit the bean)
    public const float KeyShadowStrength = 0.85f;
    public static readonly Color Ambient = new Color(0.18f, 0.165f, 0.145f); // D3 (Bean Soft Key / Soft Fill HELD)
    public static float DofFocus { get; private set; } = DofFocusDefault;
    /// Test-only (Diag_LookSweep): ambient to apply instead of Ambient while set; null in the shipped game.
    public static Color? AmbientOverride;
    static DepthOfField s_Dof;
    static bool s_LightsDone;
    // 285 moss box ~#5E6B2E; tex-yarn-green × MossOlive rendered lime, so pull R/G/B down toward olive.
    public static readonly Color MossMul = new Color(0.66f, 0.56f, 0.46f);
    public const float GarlandViewportY = 0.84f; // clouds 8–20% from the top of the play frame
    public static Volume LookVolume { get; private set; }
    /// Menu canvases render in Screen Space - Camera (GlassUI real blur). They go on this layer, which the renderer's
    /// transparent pass skips and UiAfterPostFeature draws after post-processing, so DoF/grade never touch menu text.
    public const int UiLayer = 5;
    /// Moss renderers switched to an olive clone in the latest pass (per level, not cumulative).
    public static int MossDarkened { get; private set; }
    /// Distinct olive clones alive (one per distinct moss look; bounded, no per-level growth).
    public static int MossClones => SigDark.Count;
    public static GameObject Garland { get; private set; }
    static readonly Dictionary<string, Material> SigDark = new Dictionary<string, Material>();
    static readonly HashSet<Material> DarkSet = new HashSet<Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        LevelRunner.OnLevelLoaded -= OnLevel;
        LevelRunner.OnLevelLoaded += OnLevel;
        SigDark.Clear();
        DarkSet.Clear();
        MossDarkened = 0;
        s_LightsDone = false;
        EnsureCamera();
        EnsureLights();
        EnsureVolume();
        DarkenMoss();
        if (Object.FindAnyObjectByType<Watcher>() == null)
            new GameObject("Felt Look Watch").AddComponent<Watcher>();
    }

    static void OnLevel(LevelCatalog.LevelDef def)
    {
        EnsureCamera();
        EnsureLights();
        EnsureVolume();
        DarkenMoss();
        EnsureGarland();
    }

    public static bool CameraPostOn()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        var data = cam.GetUniversalAdditionalCameraData();
        return data != null && data.renderPostProcessing;
    }

    static void EnsureCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null && !data.renderPostProcessing) data.renderPostProcessing = true;
        if ((cam.cullingMask & (1 << UiLayer)) == 0) cam.cullingMask |= 1 << UiLayer;
    }

    static readonly List<Canvas> s_Canvases = new List<Canvas>();
    static int s_CanvasScan = -1000;
    /// Camera-space canvases on the main camera -> UiLayer (Overlay canvases ignore layers, so the HUD is unchanged).
    public static void EnsureUiLayer()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (Time.frameCount - s_CanvasScan > 30 || s_Canvases.Count == 0)
        {
            s_CanvasScan = Time.frameCount;
            s_Canvases.Clear();
            foreach (var c in Object.FindObjectsByType<Canvas>()) if (c.isRootCanvas) s_Canvases.Add(c);
        }
        foreach (var c in s_Canvases)
            if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == cam && c.gameObject.layer != UiLayer)
                c.gameObject.layer = UiLayer;
    }

    static void EnsureVolume()
    {
        if (LookVolume != null) return;
        var go = new GameObject("Felt Look Volume");
        var v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = VolumePriority;
        v.weight = 1f;
        var p = ScriptableObject.CreateInstance<VolumeProfile>();
        p.name = "Felt Look (runtime)";

        var dof = p.Add<DepthOfField>(true);
        dof.mode.Override(DofEnabled ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Off);
        dof.focusDistance.Override(DofFocusDefault);
        dof.focalLength.Override(DofFocalMm);
        dof.aperture.Override(DofAperture);
        dof.bladeCount.Override(6);
        s_Dof = dof;

        var tone = p.Add<Tonemapping>(true);
        tone.mode.Override(Tone);

        var bloom = p.Add<Bloom>(true);
        bloom.threshold.Override(BloomThreshold);
        bloom.intensity.Override(BloomIntensity);
        bloom.scatter.Override(0.6f);

        var wb = p.Add<WhiteBalance>(true);
        wb.temperature.Override(WbTemperature);
        wb.tint.Override(WbTint);

        var grade = p.Add<ColorAdjustments>(true);
        grade.contrast.Override(Contrast);
        grade.saturation.Override(Saturation);
        grade.postExposure.Override(PostExposure);

        var smh = p.Add<ShadowsMidtonesHighlights>(true);
        smh.shadows.Override(new Vector4(1f, 1f, 1f, 0f)); // neutral: 285's blacks are deep, not lifted

        var vig = p.Add<Vignette>(true);
        vig.color.Override(new Color(0.16f, 0.10f, 0.05f));
        vig.intensity.Override(VignetteIntensity);
        vig.smoothness.Override(0.50f);

        v.sharedProfile = p;
        LookVolume = v;
    }

    /// Runtime key/ambient for 285 (BuildEscapeScene untouched; Bean Soft Key / Soft Fill are not modified).
    static void EnsureLights()
    {
        if (s_LightsDone) return;
        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional || l.gameObject.name != "Warm Key") continue;
            l.transform.rotation = Quaternion.Euler(KeyEuler);
            l.shadowStrength = KeyShadowStrength;
            s_LightsDone = true;
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientOverride ?? Ambient;
    }

    /// Bokeh focus = the bean's depth along the camera axis (285: bean tack-sharp, wall and front floor soft).
    static void TrackFocus()
    {
        var cam = Camera.main;
        if (s_Dof == null || cam == null) return;
        var runner = Object.FindAnyObjectByType<LevelRunner>();
        float d = DofFocusDefault;
        if (runner != null && runner.bean != null && runner.bean.gameObject.activeInHierarchy)
        {
            var c = runner.bean.position + Vector3.up * 0.45f;
            d = Vector3.Dot(c - cam.transform.position, cam.transform.forward);
            if (d < 0.5f || d > 30f) d = DofFocusDefault;
        }
        DofFocus = d;
        s_Dof.focusDistance.Override(d);
    }

    static bool IsMoss(string n) => n == "Moss" || n == "Moss Ball" || n == "Moss Halo";

    /// Darken every moss renderer once (idempotent: darkened clones are never darkened again).
    /// Step 01 r2: the old version keyed clones by material instance, so per-level material instances cloned
    /// again every load (mossDarkened 20 -> 5764 by L5). Clones are now keyed by the look (shader, texture, colour)
    /// and reused, and the counter is per pass.
    public static void DarkenMoss()
    {
        int n = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (r == null || !IsMoss(r.gameObject.name)) continue;
            var src = r.sharedMaterial;
            if (src == null || DarkSet.Contains(src)) continue;
            string sig = MossSig(src);
            if (!SigDark.TryGetValue(sig, out var dark) || dark == null)
            {
                dark = new Material(src) { name = src.name.Replace(" (Instance)", "") + " (285 olive)" };
                if (dark.HasProperty("_BaseColor"))
                {
                    var c = dark.GetColor("_BaseColor");
                    dark.SetColor("_BaseColor", new Color(c.r * MossMul.r, c.g * MossMul.g, c.b * MossMul.b, c.a));
                }
                SigDark[sig] = dark;
                DarkSet.Add(dark);
            }
            r.sharedMaterial = dark;
            n++;
        }
        MossDarkened = n;
    }

    static string MossSig(Material m)
    {
        var tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
        var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
        return (m.shader != null ? m.shader.name : "") + "|" + (tex != null ? tex.name : "") + "|" + ColorUtility.ToHtmlStringRGBA(col);
    }

    static void EnsureGarland()
    {
        if (Garland != null) return;
        GameObject wall = null;
        foreach (var w in PlayPath.SceneRoots("Kraft Backdrop")) { wall = w; break; }
        if (wall == null) return;
        var root = new GameObject("Play Garland");
        root.transform.SetParent(wall.transform, false);
        // Local frame of the wall: x along the run, clouds just in front (local -z).
        const float span = 3.6f, half = 7.2f;
        int seed = 40;
        for (float x = -half; x < half - 0.01f; x += span)
            FeltCloudCord.Cord(root.transform, new Vector3(x, 0f, -0.16f), new Vector3(x + span, 0f, -0.16f), seed++);
        for (float x = -half + span * 0.5f; x < half; x += span)
            FeltCloudCord.Cloud(root.transform, new Vector3(x, -0.10f - span * 0.11f, -0.14f), 0.62f, seed++);
        foreach (var c in root.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        foreach (var r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        Garland = root;
        root.SetActive(false);
    }

    /// Cord height on the wall so the clouds sit at GarlandViewportY of the current play frame.
    static void PlaceGarland()
    {
        var cam = Camera.main;
        if (Garland == null || cam == null) return;
        var wall = Garland.transform.parent;
        float wallZ = wall.position.z - 0.16f;
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, GarlandViewportY, 0f));
        if (Mathf.Abs(ray.direction.z) < 1e-4f) return;
        float t = (wallZ - ray.origin.z) / ray.direction.z;
        float worldY = ray.origin.y + ray.direction.y * t + 0.10f + 3.6f * 0.11f * 0.5f; // cloud hangs below the cord
        var lp = Garland.transform.localPosition;
        float wantY = worldY - wall.position.y;
        lp.y = Mathf.Abs(lp.y) < 0.001f ? wantY : Mathf.Lerp(lp.y, wantY, 1f - Mathf.Exp(-3f * Time.deltaTime));
        Garland.transform.localPosition = lp;
    }

    public static bool Menu()
    {
        if (GameSession.I == null) return true;
        var p = GameSession.I.phase;
        return p == GameSession.Phase.Boot || p == GameSession.Phase.Title
            || p == GameSession.Phase.Options || p == GameSession.Phase.Search;
    }

    class Watcher : MonoBehaviour
    {
        void LateUpdate()
        {
            EnsureCamera();
            EnsureLights();
            EnsureUiLayer();
            TrackFocus();
            if (Garland == null) return;
            bool show = !Menu();
            if (Garland.activeSelf != show) Garland.SetActive(show);
            if (show) PlaceGarland();
        }
    }
}

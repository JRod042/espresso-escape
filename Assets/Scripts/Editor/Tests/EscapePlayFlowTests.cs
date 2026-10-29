using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// Play-mode gates driven from EditMode (EnterPlayMode), because the game has no asmdef and lives in Assembly-CSharp.
/// Any Debug.LogError / exception during a test fails it (Unity Test Framework LogAssert), which is the null-ref gate.
/// Captures go to <project>/Logs/grok-captures (outside Assets).
public class EscapePlayFlowTests
{
    const string ScenePath = "Assets/Escape.unity";
    static int CaptureW = 1920, CaptureH = 1080;
    static int CapDepthBits = 24, CapAA = 1;
    static Color32[] LastPixels; static int LastW, LastH;
    const string LookStep = "look-step01"; // PROCESS.md step captures, 1280x720
    const float SmokeSeconds = 8f;
    // Batch 1 (Imagine motion match): measured BeanController values, not the old 1.04 m / 2.8 m/s guesses.
    // hopImpulse 5.2 → ~0.68 m apex; run 2.2 m/s; capsule front reach past the lip ~0.38 m. The real gate is
    // Physics_RunHopAndEveryGap, which runs and jumps every gap with the actual controller.
    const float Gravity = 22f, HopApex = 0.68f, RunSpeed = 2.2f, FootReach = 0.38f, MaxStep = 0.9f;

    static readonly HashSet<string> s_Errors = new HashSet<string>();

    // EnterPlayMode / ExitPlayMode must be yielded directly from the [UnityTest] body:
    // the edit-mode runner does not act on them inside a nested IEnumerator (Gate 2 run 1: play mode never started).
    // Harness-only mute (Jorge, 3:17 PM): runs are silent. run-tests.ps1 passes -nosound; this also mutes the listener and the
    // Game view ('Mute Audio') for the run and restores the editor toggle after. Shipped audio, mixer and assets are untouched.
    static bool _prevEditorMute;
    static void PreBoot()
    {
        _prevEditorMute = EditorUtility.audioMasterMute;
        EditorUtility.audioMasterMute = true;
        AudioListener.volume = 0f;
        PrefsSnapshot.Take().Stash();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static void PostShutdown()
    {
        PrefsSnapshot.RestoreStashed();
        EditorUtility.audioMasterMute = _prevEditorMute;
    }

    const int BootFrames = 10; // Title builds in Start; give it a few frames.
    const int BootTimeoutFrames = 600; // first play entry can stall on shader/asset warm-up

    // Unity AI Assistant's optional perf recorder (EditorUserSettings, git-ignored) keeps Logs/assistant-perf.jsonl open
    // across the play-mode domain reload and logs a sharing-violation error that fails whichever test is entering play.
    // Switch it off for this fixture only and restore the user's value afterwards.
    const string AssistantPerfKey = "Assistant.Perf.Instrumentation";
    const string AssistantPerfStash = "EscapePlayFlowTests.AssistantPerfWasOn"; // SessionState survives domain reloads

    [OneTimeSetUp]
    public void QuietAssistantPerf()
    {
        if (EditorUserSettings.GetConfigValue(AssistantPerfKey) != bool.TrueString) return;
        SessionState.SetBool(AssistantPerfStash, true);
        EditorUserSettings.SetConfigValue(AssistantPerfKey, bool.FalseString);
    }

    [OneTimeTearDown]
    public void RestoreAssistantPerf()
    {
        if (!SessionState.GetBool(AssistantPerfStash, false)) return;
        EditorUserSettings.SetConfigValue(AssistantPerfKey, bool.TrueString);
        SessionState.EraseBool(AssistantPerfStash);
    }

    /// Waits until the title UI is built and the session singleton exists, then a few settle frames.
    static IEnumerator Boot()
    {
        for (int i = 0; i < BootTimeoutFrames; i++)
        {
            var f = Flow();
            if (f != null && f.IsBuilt && GameSession.I != null) break;
            yield return null;
        }
        yield return Frames(BootFrames);
    }

    static TitleFlow Flow() => Object.FindAnyObjectByType<TitleFlow>();
    static LevelRunner Runner() => Object.FindAnyObjectByType<LevelRunner>();

    // EditMode [UnityTest] yields are editor ticks; in batchmode those do not always step the player loop
    // (Gate 2 run g2e: Time.frameCount stayed at 73 across ten yields, so LevelRunner.Update never saw the bean).
    // Wait for n real player frames, nudging the loop, with a wall-clock cap so a stall fails instead of hanging.
    const float FrameWaitCapSeconds = 10f;
    static IEnumerator Frames(int n)
    {
        int target = Time.frameCount + n;
        float cap = Time.realtimeSinceStartup + FrameWaitCapSeconds;
        while (Time.frameCount < target && Time.realtimeSinceStartup < cap)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
    }

    static IEnumerator Seconds(float s)
    {
        float end = Time.realtimeSinceStartup + s;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    // Start a level the way a player does (Levels menu -> TitleFlow.OnPickLevel), so captures show the HUD, not the title card.
    static readonly System.Reflection.MethodInfo s_PickLevel =
        typeof(TitleFlow).GetMethod("OnPickLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    static void PickLevel(TitleFlow flow, int id)
    {
        Assert.IsNotNull(s_PickLevel, "TitleFlow.OnPickLevel");
        s_PickLevel.Invoke(flow, new object[] { id });
    }

    static void Teleport(LevelRunner r, float x, float top)
    {
        var p = new Vector3(x, top + 0.8f, 0f);
        var rb = r.bean.GetComponent<Rigidbody>();
        if (rb != null) { rb.position = p; rb.linearVelocity = Vector3.zero; }
        r.bean.position = p;
    }

    static float TopAt(float x)
    {
        float top = 0f;
        for (int i = 0; i < LevelRunner.Blocks.Count; i++)
            if (x >= LevelRunner.Blocks[i].x && x <= LevelRunner.Blocks[i].y) top = Mathf.Max(top, LevelRunner.BlockTop[i]);
        return top;
    }

    // ───────────── menus ─────────────

    [UnityTest]
    public IEnumerator Menus_OpenAndClose()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        Assert.IsNotNull(flow, "TitleFlow in scene");
        Assert.IsTrue(flow.IsBuilt, "UI built");
        Assert.AreEqual(TitleFlow.Screen.Title, flow.Current);
        Assert.IsNotNull(GameSession.I, "GameSession");
        Assert.AreEqual(GameSession.Phase.Title, GameSession.I.phase);
        yield return Capture("menu-title");

        flow.ShowOptions();
        yield return Frames(3);
        Assert.AreEqual(TitleFlow.Screen.Settings, flow.Current);
        yield return Capture("menu-settings");
        flow.ShowTitle();
        yield return Frames(2);
        Assert.AreEqual(TitleFlow.Screen.Title, flow.Current);

        flow.ShowSearch();
        yield return Frames(3);
        Assert.AreEqual(TitleFlow.Screen.Levels, flow.Current);
        Assert.AreEqual(GameSession.Phase.Search, GameSession.I.phase);
        yield return Capture("menu-levels");
        flow.ShowTitle();
        yield return Frames(2);

        // play, pause (timeScale 0), settings from pause, back, resume
        GameSession.I.PlayLevel(1);
        yield return Frames(3);
        flow.ShowPause();
        yield return Frames(2);
        Assert.AreEqual(TitleFlow.Screen.Pause, flow.Current);
        Assert.AreEqual(0f, Time.timeScale, "pause freezes time");
        yield return Capture("menu-pause");
        flow.OpenSettings(TitleFlow.Screen.Pause);
        yield return Frames(2);
        Assert.AreEqual(TitleFlow.Screen.Settings, flow.Current);
        flow.ShowPause();
        flow.ResumeFromPause();
        yield return Frames(2);
        Assert.AreEqual(TitleFlow.Screen.Hud, flow.Current);
        Assert.AreEqual(1f, Time.timeScale, "resume restores time");

        // backgrounding mid-level pauses (OnApplicationPause path, batch guard bypassed by calling ShowPause directly)
        flow.ShowTitle();
        yield return Frames(2);
        Assert.AreEqual(TitleFlow.Screen.Title, flow.Current);
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    // ───────────── levels: load, reach, clear ─────────────

    static int s_Clears, s_LastClearId;
    static void OnClear(int id, int got, int total) { s_Clears++; s_LastClearId = id; }

    [UnityTest]
    public IEnumerator Levels_LoadReachableAndClear()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        Assert.IsNotNull(flow, "TitleFlow in scene");
        Assert.IsNotNull(GameSession.I, "GameSession");
        // No lambdas/closures here: after the play-mode domain reload the runner restores the iterator's own fields
        // but not a hoisted closure object, which made `clears` a null dereference (Gate 2 runs 2-3).
        s_Clears = 0; s_LastClearId = 0;
        GameSession.OnLevelClear += OnClear;
        try
        {
            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                var def = LevelCatalog.Make(id);
                PickLevel(flow, id); // the Levels-menu path: Loading pill, then HUD (no title card left over)
                yield return Frames(5);
                var r = Runner();
                Assert.IsNotNull(r, "LevelRunner");
                Assert.AreEqual(id, r.active.id, "loaded L" + id);
                Assert.AreEqual(GameSession.Phase.Play, GameSession.I.phase);
                Assert.Greater(LevelRunner.Blocks.Count, 0, "blocks L" + id);
                AssertReachable(def);
                if (id == 4) { Assert.GreaterOrEqual(LevelFeatures.VentCount, 3, "L4 vents"); Assert.GreaterOrEqual(LevelFeatures.LiftCount, 1, "L4 lift"); }
                if (id == 5) Assert.IsNotNull(LevelFeatures.Grinder, "L5 grinder boss");
                Assert.Greater(LevelFeatures.FlagCount, 0, "checkpoints L" + id);

                int before = s_Clears;
                Teleport(r, def.goalZ + 0.2f, TopAt(def.goalZ - 0.5f));
                yield return Frames(10);
                Assert.AreEqual(before + 1, s_Clears, "level-complete fired L" + id);
                Assert.AreEqual(id, s_LastClearId);
                Assert.AreEqual(GameSession.Phase.Clear, GameSession.I.phase);
                Assert.AreEqual(TitleFlow.Screen.Clear, flow.Current, "clear card shown L" + id);
                Assert.GreaterOrEqual(GameSession.I.lastStars, 1);
                Assert.GreaterOrEqual(PlayerProgress.Stars(id), 1, "stars saved L" + id);
                if (id < LevelCatalog.LevelCount) Assert.GreaterOrEqual(PlayerProgress.Unlocked, id + 1, "unlock after L" + id);
                yield return Seconds(1f); // let the stars pop
                yield return Capture("clear-L" + id);
            }
        }
        finally { GameSession.OnLevelClear -= OnClear; }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    // ───────────── physics: the real controller, measured in physics time ─────────────

    static System.Text.StringBuilder s_Phys;
    static string PhysPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "grok-physics.txt");

    static IEnumerator FixedSeconds(float s)
    {
        float end = Time.fixedTime + s, cap = Time.realtimeSinceStartup + 8f;
        while (Time.fixedTime < end && Time.realtimeSinceStartup < cap) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
    }

    static Rigidbody s_Rb; static BeanController s_Ctl;
    static float s_Mark, s_Val;

    [UnityTest]
    public IEnumerator Physics_RunHopAndEveryGap()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        Assert.IsNotNull(flow, "TitleFlow in scene");
        s_Phys = new System.Text.StringBuilder();
        BeanController.TestHoldX = 0f; BeanController.TestJumpAtX = float.NaN;
        try
        {
            // L2 long first block: accel, steady speed, glide-to-stop, standing hop
            PickLevel(flow, 2);
            yield return Frames(5);
            var r = Runner();
            s_Rb = r.bean.GetComponent<Rigidbody>(); s_Ctl = r.bean.GetComponent<BeanController>();
            Assert.IsNotNull(s_Ctl, "BeanController"); Assert.IsNotNull(s_Rb, "bean Rigidbody");
            s_Phys.AppendLine($"controller run {s_Ctl.runSpeed} accel {s_Ctl.groundAccel} brake {s_Ctl.groundBrake} hop {s_Ctl.hopImpulse} mass {s_Rb.mass} damping {s_Rb.linearDamping} g {Physics.gravity.y:0.00} fixedDt {Time.fixedDeltaTime}");
            float t95 = 0f, vRun = 0f, x0 = 0f, tStop = 0f, glide = 0f, apex = 0f, air = 0f, cap;
            float run1 = s_Ctl.runSpeed, acc1 = s_Ctl.groundAccel, brk1 = s_Ctl.groundBrake;
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0) { s_Ctl.runSpeed = 2.8f; s_Ctl.groundAccel = 16f; s_Ctl.groundBrake = 22f; }
                else { s_Ctl.runSpeed = run1; s_Ctl.groundAccel = acc1; s_Ctl.groundBrake = brk1; }
                Teleport(r, 6.75f, TopAt(6.75f));
                yield return FixedSeconds(0.6f);
                s_Mark = Time.fixedTime; BeanController.TestHoldX = 1f; s_Val = -1f;
                cap = Time.realtimeSinceStartup + 6f;
                while (Time.realtimeSinceStartup < cap && Time.fixedTime - s_Mark < 2f)
                {
                    if (s_Val < 0f && s_Rb.linearVelocity.x >= 0.90f * s_Ctl.runSpeed) s_Val = Time.fixedTime - s_Mark;
                    if (s_Val >= 0f && Time.fixedTime - s_Mark > 0.9f || s_Rb.position.x > 9.4f) break;
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
                t95 = s_Val;
                vRun = s_Rb.linearVelocity.x;
                x0 = s_Rb.position.x; s_Mark = Time.fixedTime; BeanController.TestHoldX = 0f; s_Val = -1f;
                cap = Time.realtimeSinceStartup + 6f;
                while (Time.realtimeSinceStartup < cap && Time.fixedTime - s_Mark < 2f)
                {
                    if (Mathf.Abs(s_Rb.linearVelocity.x) < 0.05f) { s_Val = Time.fixedTime - s_Mark; break; }
                    EditorApplication.QueuePlayerLoopUpdate(); yield return null;
                }
                tStop = s_Val; glide = s_Rb.position.x - x0;
                yield return FixedSeconds(0.4f);
                BeanController.TestLandT = -1f;
                s_Ctl.TestJump();
                cap = Time.realtimeSinceStartup + 6f;
                while (Time.realtimeSinceStartup < cap && BeanController.TestLandT < 0f) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
                apex = BeanController.TestApexY - BeanController.TestTakeoffY; air = BeanController.TestLandT - BeanController.TestTakeoffT;
                s_Phys.AppendLine((pass == 0 ? "BEFORE (2.8/16/22) " : "AFTER  (2.36/13.5/1) ") + $"accel 0->90% {t95:0.000}s  run {vRun:0.000} m/s  stop {tStop:0.000}s glide {glide:0.000} m  standing hop apex {apex:0.000} m air {air:0.000}s");
            }
            Assert.That(t95, Is.InRange(0.2f, 0.6f), "ease-in to run speed");
            Assert.That(vRun, Is.InRange(2.0f, 2.3f), "steady run speed");
            Assert.That(tStop, Is.InRange(0.15f, 0.6f), "glide to rest");
            Assert.That(apex, Is.InRange(0.55f, 0.85f), "held hop apex");
            Assert.That(air, Is.InRange(0.38f, 0.65f), "hop air time");

            // every gap of every level, the way a player takes it: run up, jump ~0.15 m before the lip (coyote time adds ~0.2 m more slack)
            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                PickLevel(flow, id);
                yield return Frames(5);
                r = Runner();
                s_Rb = r.bean.GetComponent<Rigidbody>(); s_Ctl = r.bean.GetComponent<BeanController>();
                s_Phys.AppendLine($"L{id} gravity {Physics.gravity.y:0.00} hopScale {LevelRunner.HopScale:0.000} kickables {FeltKickables.Bodies.Count}");
                var gaps = new List<Vector2>(LevelRunner.Gaps);
                var lips = new List<float>(LevelRunner.GapLip);
                for (int g = 0; g < gaps.Count; g++)
                {
                    float lip = gaps[g].x, far = gaps[g].y, lipY = lips[g];
                    float start = lip - 2.6f;
                    for (int i = 0; i < LevelRunner.Blocks.Count; i++)
                        if (Mathf.Abs(LevelRunner.Blocks[i].y - lip) < 0.05f && Mathf.Abs(LevelRunner.BlockTop[i] - lipY) < 0.05f)
                            start = Mathf.Max(start, LevelRunner.Blocks[i].x + 0.45f);
                    BeanController.TestHoldX = 0f;
                    Teleport(r, start, lipY);
                    yield return FixedSeconds(0.5f);
                    BeanController.TestLandT = -1f; BeanController.TestLandX = float.NaN;
                    BeanController.TestJumpAtX = lip - 0.15f;
                    BeanController.TestHoldX = 1f;
                    cap = Time.realtimeSinceStartup + 8f;
                    while (Time.realtimeSinceStartup < cap && BeanController.TestLandT < 0f) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
                    BeanController.TestHoldX = 0f;
                    yield return FixedSeconds(0.25f);
                    float landX = BeanController.TestLandX, endX = s_Rb.position.x, endY = s_Rb.position.y;
                    bool ok = BeanController.TestLandT > 0f && landX > lip + 0.3f && endX > lip + 0.3f && endY > TopAt(far + 0.3f) + 0.3f && s_Ctl.Grounded; // grounded rules out hanging on the far corner // standing on the far side (ramps land short of the lip line)
                    s_Phys.AppendLine($"  gap {lip:0.00}->{far:0.00} ({far - lip:0.00} m, lip y {lipY:0.00}) run-up {lip - start:0.00} m: take-off x {BeanController.TestTakeoffX:0.00} land x {landX:0.00} air {BeanController.TestLandT - BeanController.TestTakeoffT:0.000}s  margin {landX - far:+0.00;-0.00} m {(ok ? "OK" : "FAIL")}");
                    Assert.IsTrue(ok, $"L{id} gap {lip:0.00}->{far:0.00}: land x {landX:0.00} end ({endX:0.00},{endY:0.00})");
                }
            }

            // prop reaction: walk into a kickable and see it roll (285.mp4 moss-ball kick)
            bool kicked = false;
            for (int id = 1; id <= LevelCatalog.LevelCount && !kicked; id++)
            {
                PickLevel(flow, id);
                yield return Frames(5);
                r = Runner(); s_Rb = r.bean.GetComponent<Rigidbody>();
                if (FeltKickables.Bodies.Count == 0) continue;
                var k = FeltKickables.Bodies[0];
                Vector3 k0 = k.position;
                Teleport(r, k0.x - 1.3f, TopAt(k0.x));
                yield return FixedSeconds(0.4f);
                BeanController.TestHoldX = 1f;
                yield return FixedSeconds(1.4f);
                BeanController.TestHoldX = 0f;
                yield return FixedSeconds(0.8f);
                float moved = (k.position - k0).magnitude;
                s_Phys.AppendLine($"kick L{id} {k.name} mass {k.mass} moved {moved:0.00} m (dx {k.position.x - k0.x:+0.00;-0.00}) bean x {s_Rb.position.x:0.00}");
                kicked = moved > 0.15f;
            }
            Assert.IsTrue(kicked, "a felt kickable rolls when the bean walks into it");
        }
        finally
        {
            BeanController.TestHoldX = 0f; BeanController.TestJumpAtX = float.NaN;
            if (s_Phys != null) File.WriteAllText(PhysPath, s_Phys.ToString());
        }
        yield return new ExitPlayMode();
        PostShutdown();
    }

    /// Every gap and step on the lane fits a single hop: v0 = sqrt(2 g apex), run 2.2 m/s, capsule front reach.
    static void AssertReachable(LevelCatalog.LevelDef def)
    {
        var idx = new List<int>();
        for (int i = 0; i < LevelRunner.Blocks.Count; i++) idx.Add(i);
        idx.Sort((a, b) => LevelRunner.Blocks[a].x.CompareTo(LevelRunner.Blocks[b].x));
        float v0 = Mathf.Sqrt(2f * Gravity * HopApex);
        float reach = LevelRunner.Blocks[idx[0]].y;
        float reachTop = LevelRunner.BlockTop[idx[0]];
        for (int k = 1; k < idx.Count; k++)
        {
            var b = LevelRunner.Blocks[idx[k]];
            float top = LevelRunner.BlockTop[idx[k]];
            if (b.y <= reach) continue; // fully covered by an earlier block
            float gap = b.x - reach;
            float dh = top - reachTop;
            if (gap <= 0.02f)
                Assert.LessOrEqual(dh, MaxStep, $"L{def.id} step at x={b.x:0.00} too tall ({dh:0.00} m)");
            else
            {
                float disc = v0 * v0 - 2f * Gravity * dh;
                Assert.Greater(disc, 0f, $"L{def.id} ledge at x={b.x:0.00} above hop apex");
                float t = (v0 + Mathf.Sqrt(disc)) / Gravity;
                float range = RunSpeed * t + FootReach;
                Assert.LessOrEqual(gap, range, $"L{def.id} gap {reach:0.00}->{b.x:0.00} = {gap:0.00} m > hop range {range:0.00} m");
            }
            reach = b.y;
            reachTop = top;
        }
        Assert.GreaterOrEqual(reach, def.goalZ, $"L{def.id} lane ends ({reach:0.0}) before the goal ({def.goalZ:0.0})");
    }

    // ───────────── per-level smoke + captures ─────────────

    [UnityTest]
    public IEnumerator Smoke_EachLevelRunsWithoutErrors()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        Assert.IsNotNull(flow, "TitleFlow in scene");
        Assert.IsNotNull(GameSession.I, "GameSession");
        for (int id = 1; id <= LevelCatalog.LevelCount; id++)
        {
            var def = LevelCatalog.Make(id);
            PickLevel(flow, id); // the Levels-menu path: Loading pill, then HUD (no title card left over)
            yield return Frames(5);
            var r = Runner();
            Debug.Log($"SMOKE_BEGIN L{id} {def.title}");
            yield return Seconds(1.5f);
            Assert.IsNotNull(EscapeAudio.I, "audio");
            Assert.IsTrue(EscapeAudio.I.HasFoleyClips && EscapeAudio.I.HasUiClips, "P0 foley and Kenney UI clips loaded");
            Assert.AreEqual(id, EscapeAudio.I.MusicIndex, "level music track");
            yield return Capture($"level-L{id}-start");
            // Walk the bean through the lane in steps so vents, lifts, flags and the grinder all tick.
            int steps = 8;
            for (int s = 1; s <= steps; s++)
            {
                if (GameSession.I.phase != GameSession.Phase.Play) break;
                float x = Mathf.Lerp(1f, def.goalZ - 2f, s / (float)steps);
                if (LevelRunner.IsOverGap(x)) x += 1.6f;
                Teleport(r, x, TopAt(x));
                yield return Seconds(SmokeSeconds / steps);
                if (s == steps / 2) yield return Capture($"level-L{id}-mid");
            }
            if (id == 5) Assert.IsTrue(LevelFeatures.Grinder != null && LevelFeatures.Grinder.IsAwake, "grinder woke in L5");
            Assert.AreNotEqual(TitleFlow.Screen.None, flow.Current);
            Debug.Log($"SMOKE_END L{id} phase={GameSession.I.phase} flags={LevelFeatures.FlagCount} vents={LevelFeatures.VentCount} lifts={LevelFeatures.LiftCount}");
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    /// Batch 1 frame strip: ours vs bean-motion.mp4. Walk from rest (8 frames, 0.125 s apart, like the 24 fps ref every
    /// 3rd frame) and one gap jump on L1 (8 frames, 0.07 s apart). maximumDeltaTime = one fixed step keeps timing exact.
    [UnityTest]
    public IEnumerator Physics_FrameStrip()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        var sb = new System.Text.StringBuilder();
        float prevMaxDt = Time.maximumDeltaTime;
        Time.maximumDeltaTime = Time.fixedDeltaTime; // one physics step per frame, so slow 1080p captures never batch steps without Update
        try
        {
            PickLevel(flow, 1);
            yield return Frames(5);
            yield return Seconds(3.0f); // past the level intro
            var r = Runner();
            var rb = r.bean.GetComponent<Rigidbody>();
            BeanController.TestHoldX = 0f; BeanController.TestJumpAtX = float.NaN;
            Teleport(r, 6.9f, 0f);
            yield return FixedSeconds(0.6f);
            BeanController.TestHoldX = 1f;
            float t0 = Time.fixedTime;
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) yield return FixedSeconds(0.125f);
                sb.AppendLine($"walk {i} t {Time.fixedTime - t0:0.000} x {rb.position.x:0.000} y {rb.position.y:0.000} vx {rb.linearVelocity.x:0.00}");
                yield return Capture($"strip-walk-{i}");
            }
            BeanController.TestJumpAtX = 9.95f; BeanController.TestLandT = -1f;
            float cap = Time.realtimeSinceStartup + 6f;
            while (rb.position.x < 9.95f && Time.realtimeSinceStartup < cap) { EditorApplication.QueuePlayerLoopUpdate(); yield return null; }
            float j0 = Time.fixedTime;
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) yield return FixedSeconds(0.07f);
                sb.AppendLine($"jump {i} t {Time.fixedTime - j0:0.000} x {rb.position.x:0.000} y {rb.position.y:0.000} vy {rb.linearVelocity.y:0.00}");
                yield return Capture($"strip-jump-{i}");
            }
            Assert.Greater(rb.position.x, 10.85f, "landed across the L1 gap");
        }
        finally
        {
            Time.maximumDeltaTime = prevMaxDt;
            BeanController.TestHoldX = 0f; BeanController.TestJumpAtX = float.NaN;
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "grok-strip.txt")), sb.ToString());
        }
        yield return new ExitPlayMode();
        PostShutdown();
    }

    // ───────────── batch 2: camera framing vs 285.jpg ─────────────

    /// Fraction of the screen height the bean covers (projected renderer bounds), and its centre in viewport space.
    static float BeanScreenFrac(Camera cam, Transform bean, out Vector2 centre)
    {
        centre = new Vector2(-1f, -1f);
        var rs = bean.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 0f;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float lo = float.PositiveInfinity, hi = float.NegativeInfinity, cx = 0f;
        for (int k = 0; k < 8; k++)
        {
            var c = new Vector3((k & 1) == 0 ? b.min.x : b.max.x, (k & 2) == 0 ? b.min.y : b.max.y, (k & 4) == 0 ? b.min.z : b.max.z);
            var v = cam.WorldToViewportPoint(c);
            lo = Mathf.Min(lo, v.y); hi = Mathf.Max(hi, v.y); cx += v.x / 8f;
        }
        centre = new Vector2(cx, (lo + hi) * 0.5f);
        return hi - lo;
    }

    /// Foreground felt never rises above the lane on screen (it can't hide the path), and stays out of the top HUD band.
    static int ForegroundBlocks(Camera cam, System.Text.StringBuilder sb)
    {
        var root = GameObject.Find("Felt Foreground");
        if (root == null) return -1;
        int bad = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            var b = r.bounds;
            var top = cam.WorldToViewportPoint(new Vector3(b.center.x, b.max.y, b.min.z));
            if (top.z <= 0f || top.x < 0f || top.x > 1f) continue;
            var lane = cam.WorldToViewportPoint(new Vector3(b.center.x, TopAt(b.center.x), 0f));
            if (top.y > lane.y || top.y > 0.85f) { bad++; sb.AppendLine($"  FG over lane: {r.name} x {b.center.x:0.00} topV {top.y:0.000} laneV {lane.y:0.000}"); }
        }
        return bad;
    }

    [UnityTest]
    public IEnumerator Camera_FramingMatches285()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        var sb = new System.Text.StringBuilder();
        try
        {
            yield return Seconds(3.0f); // mid title dolly
            var cam = Camera.main;
            var bean = GameObject.Find("Imagine Bean");
            Assert.IsNotNull(cam, "camera"); Assert.IsNotNull(bean, "bean");
            float tf = BeanScreenFrac(cam, bean.transform, out var tc);
            int tbad = ForegroundBlocks(cam, sb);
            sb.AppendLine($"title fov {cam.fieldOfView:0.0} pos {cam.transform.position} pitch {cam.transform.eulerAngles.x:0.0} beanFrac {tf:0.000} centre {tc} fgCount {FeltForeground.Count} fgOverLane {tbad}");
            yield return Capture("b2-title");
            Assert.That(tf, Is.InRange(0.30f, 0.55f), "title bean >= ~1/3 frame height");
            Assert.AreEqual(0, tbad, "title foreground under the lane");

            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                var def = LevelCatalog.Make(id);
                PickLevel(flow, id);
                yield return Frames(5);
                yield return Seconds(2.5f);
                var r = Runner();
                cam = Camera.main;
                float f = BeanScreenFrac(cam, r.bean, out var c);
                var follow = cam.GetComponent<SetCameraFollow>();
                int bad = ForegroundBlocks(cam, sb);
                sb.AppendLine($"L{id} fov {cam.fieldOfView:0.0} offset {follow.offset} pitch {follow.pitch:0.0} beanFrac {f:0.000} centre {c} fgCount {FeltForeground.Count} fgOverLane {bad}");
                yield return Capture($"b2-L{id}-start");
                // renderer bounds include arms/legs and AABB slack: 0.43 bounds = ~0.33 visible bean (pixel-measured on b2 captures)
                Assert.That(f, Is.InRange(0.30f, 0.50f), $"L{id} bean about 1/3 of frame height");
                // look step00 (spec D22): ref-285 bean x-centre 0.51 of width -> 0.50 +- 0.03 (replaces "c.x < 0.5", which the ref contradicts)
                Assert.That(c.x, Is.InRange(0.47f, 0.53f), $"L{id} bean centred like ref-285 (spec D22)");
                Assert.Greater(FeltForeground.Count, 0, $"L{id} foreground layer");
                Assert.AreEqual(0, bad, $"L{id} foreground under the lane");
            }
        }
        finally
        {
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "grok-camera.txt")), sb.ToString());
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    // ───────────── batch 3: paths ─────────────

    static string PathsLog => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "grok-paths.txt");

    /// Lane x seen at the right edge of the view (lane plane z = 0, bean height): how far ahead a hazard shows.
    /// Shipped screen: iOS landscape-left (PlayerSettings defaultScreenOrientation 3), iPhone 2556x1179.
    /// Approved test-condition change (look step00, Cacique for Jorge, 3:05 PM CT): measure at the shipped aspect, not the
    /// batchmode 4:3 default screen. The 2.5 m threshold is unchanged.
    const int ShipW = 2556, ShipH = 1179;
    static float LookAheadX(Camera cam, float y) => LookAheadX(cam, y, (float)ShipW / ShipH);
    static float LookAheadX(Camera cam, float y, float aspect)
    {
        cam.aspect = aspect;
        var ray = cam.ViewportPointToRay(new Vector3(1f, 0.5f, 0f));
        cam.ResetAspect();
        if (Mathf.Abs(ray.direction.z) < 1e-4f) return float.NaN;
        float t = -ray.origin.z / ray.direction.z;
        return ray.origin.x + ray.direction.x * t;
    }

    /// Batch 3: readable kraft/burlap strip with red lips at every gap, no title sign / yard bridge bleed in play,
    /// hazards in view early, every pickup inside the hop reach. Captures b3-L{id}-start / -mid.
    [UnityTest]
    public IEnumerator Paths_StripReadableNoTitleBleed()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        var sb = new System.Text.StringBuilder();
        try
        {
            yield return Seconds(1.0f);
            int titleSigns = 0;
            foreach (var t in PlayPath.SceneRoots("Yarn Title")) if (t.activeInHierarchy) titleSigns++;
            sb.AppendLine($"title signActive {titleSigns}");
            Assert.Greater(titleSigns, 0, "yarn title sign shows on the title screen");

            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                var def = LevelCatalog.Make(id);
                PickLevel(flow, id);
                yield return Frames(5);
                yield return Seconds(2.5f);
                var r = Runner();
                var cam = Camera.main;
                int signs = 0, bridges = 0;
                foreach (var t in PlayPath.SceneRoots("Yarn Title")) if (t.activeInHierarchy) signs++;
                foreach (var t in PlayPath.SceneRoots("Kraft Bridge")) if (t.activeInHierarchy) bridges++;
                var strip = GameObject.Find("Lane Strip");
                Assert.IsNotNull(strip, $"L{id} lane strip");
                int overGap = 0;
                foreach (var rend in strip.GetComponentsInChildren<Renderer>())
                {
                    if (!rend.name.StartsWith("Lane Strip Runner")) continue;
                    var bb = rend.bounds;
                    for (int g = 0; g < LevelRunner.Gaps.Count; g++)
                        if (bb.max.x > LevelRunner.Gaps[g].x + 0.01f && bb.min.x < LevelRunner.Gaps[g].y - 0.01f) overGap++;
                }
                float ahead = LookAheadX(cam, 0f) - r.bean.position.x;
                // info only (auditor round-2 asked for per-aspect numbers; Jorge chose option A, so only the shipped iPhone aspect is asserted)
                float ahead43 = LookAheadX(cam, 0f, 4f / 3f) - r.bean.position.x, ahead169 = LookAheadX(cam, 0f, 16f / 9f) - r.bean.position.x;
                // first gap ahead of the spawn: distance from the spawn where it first enters view
                float firstGap = LevelRunner.Gaps.Count > 0 ? LevelRunner.Gaps[0].x : float.NaN;
                for (int g = 0; g < LevelRunner.Gaps.Count; g++) firstGap = Mathf.Min(firstGap, LevelRunner.Gaps[g].x);
                // pickups inside reach: rest centre 0.46 + apex 0.68 + body half 0.30
                int pk = 0, outOfReach = 0;
                float worst = 0f;
                foreach (var p in Object.FindObjectsByType<Pickup>())
                {
                    if (!p.gameObject.activeInHierarchy) continue;
                    pk++;
                    var pos = p.transform.position;
                    float baseY = TopAt(pos.x);
                    for (int g = 0; g < LevelRunner.Gaps.Count; g++)
                        if (pos.x > LevelRunner.Gaps[g].x && pos.x < LevelRunner.Gaps[g].y) baseY = g < LevelRunner.GapLip.Count ? LevelRunner.GapLip[g] : 0f;
                    foreach (var lift in Object.FindObjectsByType<LiftPlatform>()) // lift honey: reached by riding the lift
                        if (Mathf.Abs(lift.transform.position.x - pos.x) < 0.9f) baseY = Mathf.Max(baseY, lift.TopHigh);
                    float h = pos.y - baseY;
                    worst = Mathf.Max(worst, h);
                    if (h > 0.46f + HopApex + 0.30f) outOfReach++;
                }
                var gw = new System.Text.StringBuilder();
                for (int g = 0; g < LevelRunner.Gaps.Count; g++) gw.Append($"{LevelRunner.Gaps[g].y - LevelRunner.Gaps[g].x:0.00} ");
                sb.AppendLine($"L{id} {def.title} goal {def.goalZ:0.0} blocks {LevelRunner.Blocks.Count} gaps [{gw.ToString().Trim()}] strip {PlayPath.StripCount} lips {PlayPath.LipCount} overGap {overGap} signActive {signs} yardBridgeActive {bridges} lookAhead {ahead:0.00} m @{ShipW}x{ShipH} [info 4:3 {ahead43:0.00} 16:9 {ahead169:0.00}] ({ahead / RunSpeed:0.00} s at {RunSpeed} m/s) firstGapX {firstGap:0.00} pickups {pk} worstPickupH {worst:0.00} outOfReach {outOfReach} dens {def.obstacleDensity:0.00}");
                yield return Capture($"b3-L{id}-start");
                float mx = Mathf.Lerp(1f, def.goalZ - 2f, 0.5f);
                if (LevelRunner.IsOverGap(mx)) mx += 1.6f;
                Teleport(r, mx, TopAt(mx));
                yield return Seconds(1.6f);
                yield return Capture($"b3-L{id}-mid");

                Assert.AreEqual(0, signs, $"L{id} title sign hidden in play (HUD overlap)");
                if (id >= 3) Assert.AreEqual(0, bridges, $"L{id} yard bridge hidden");
                Assert.GreaterOrEqual(PlayPath.StripCount, LevelRunner.Blocks.Count, $"L{id} strip on every block");
                Assert.AreEqual(2 * LevelRunner.Gaps.Count, PlayPath.LipCount, $"L{id} red lip on both sides of every gap");
                Assert.AreEqual(0, overGap, $"L{id} strip never spans a gap");
                Assert.GreaterOrEqual(ahead, 2.5f, $"L{id} hazards in view at least 2.5 m ahead");
                Assert.AreEqual(0, outOfReach, $"L{id} every pickup inside hop reach");
            }
        }
        finally
        {
            File.WriteAllText(PathsLog, sb.ToString());
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    static string LookLog => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "grok-look.txt");

    /// Batch 4: post-processing actually renders (camera flag + Felt Look volume: Gaussian DOF, warm grade,
    /// vignette), SSAO on the renderer, moss darkened to 285 olive, play garland in the upper frame and clear of
    /// the HUD strip. Captures b4-title, b4-L{id}-start / -mid.
    static string DiagPath(Transform t)
    {
        var parts = new List<string>();
        for (var c = t; c != null; c = c.parent) parts.Insert(0, c.name);
        return string.Join("/", parts);
    }

    /// TEMP look-step01 tuning sweep (Explicit: never part of the suite run). Reads Logs/look-sweep.txt, one variant per
    /// line "name key=value ...", applies it to the runtime look (volume, ambient, key, SSAO) at L1 start and captures
    /// sweep-<name>.png; restores the SSAO asset settings afterwards. Values are measured on the box (aud_brief.py).
    static object s_SweepAo;
    static System.Reflection.FieldInfo AoField(string k) => s_SweepAo?.GetType().GetField(k, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    static float AoGet(string k) { var f = AoField(k); return f == null ? 0f : (float)f.GetValue(s_SweepAo); }
    static void AoSet(string k, float v) { var f = AoField(k); if (f != null) f.SetValue(s_SweepAo, v); }
    static float SweepF(string[] v, int j) => float.Parse(v[j], System.Globalization.CultureInfo.InvariantCulture);

    [UnityTest, Explicit]
    public IEnumerator Diag_LookSweep()
    {
        // No lambdas/closures: after the play-mode domain reload a hoisted closure object comes back null.
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        string file = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "look-sweep.txt"));
        Assert.IsTrue(File.Exists(file), "Logs/look-sweep.txt");
        var lines = File.ReadAllLines(file);
        var flow = Flow();
        CaptureW = 1280; CaptureH = 720;
        var sb = new System.Text.StringBuilder();
        var rdata = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/EscapeURP_Renderer.asset");
        Assert.IsNotNull(rdata, "renderer data");
        Assert.IsNotNull(FeltLook.LookVolume, "LookVolume");
        s_SweepAo = null;
        foreach (var f in rdata.rendererFeatures)
            if (f is UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion)
            {
                var fi = f.GetType().GetField("m_Settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                s_SweepAo = fi?.GetValue(f);
            }
        float aoI0 = AoGet("Intensity"), aoR0 = AoGet("Radius"), aoD0 = AoGet("DirectLightingStrength");
        foreach (var l in Object.FindObjectsByType<Light>())
            sb.AppendLine($"light {l.gameObject.name} {l.type} I {l.intensity:0.000} rot {l.transform.eulerAngles.ToString("F1")} col {l.color} mask {l.cullingMask}");
        try
        {
            yield return Seconds(1.0f);
            int lvl = 1; string pos = "start";
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var tok = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (tok[0] == "level") { lvl = int.Parse(tok[1]); pos = tok.Length > 2 ? tok[2] : "start"; PickLevel(flow, lvl); yield return Frames(5); yield return Seconds(2.5f);
                    if (pos == "mid") { var r = Runner(); var def = LevelCatalog.Make(lvl); float mx = Mathf.Lerp(1f, def.goalZ - 2f, 0.5f); if (LevelRunner.IsOverGap(mx)) mx += 1.6f; Teleport(r, mx, TopAt(mx)); yield return Seconds(1.6f); }
                    continue; }
                var vol = FeltLook.LookVolume.profile;
                vol.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var ca);
                vol.TryGet<UnityEngine.Rendering.Universal.WhiteBalance>(out var wb);
                vol.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vg);
                vol.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bl);
                vol.TryGet<UnityEngine.Rendering.Universal.DepthOfField>(out var dof);
                vol.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var tm);
                Light key = null;
                foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional && l.gameObject.name == "Warm Key") key = l;
                for (int i = 1; i < tok.Length; i++)
                {
                    var kv = tok[i].Split('='); var k = kv[0]; var v = kv[1].Split(',');
                    switch (k)
                    {
                        case "exp": ca.postExposure.Override(SweepF(v, 0)); break;
                        case "sat": ca.saturation.Override(SweepF(v, 0)); break;
                        case "con": ca.contrast.Override(SweepF(v, 0)); break;
                        case "temp": wb.temperature.Override(SweepF(v, 0)); break;
                        case "tint": wb.tint.Override(SweepF(v, 0)); break;
                        case "vig": vg.intensity.Override(SweepF(v, 0)); break;
                        case "bloom": bl.intensity.Override(SweepF(v, 0)); break;
                        case "focal": dof.focalLength.Override(SweepF(v, 0)); break;
                        case "ap": dof.aperture.Override(SweepF(v, 0)); break;
                        case "tone": tm.mode.Override(v[0] == "aces" ? UnityEngine.Rendering.Universal.TonemappingMode.ACES : UnityEngine.Rendering.Universal.TonemappingMode.Neutral); break;
                        case "amb": FeltLook.AmbientOverride = new Color(SweepF(v, 0), SweepF(v, 1), SweepF(v, 2)); RenderSettings.ambientLight = FeltLook.AmbientOverride.Value; break;
                        case "key": if (key != null) key.transform.rotation = Quaternion.Euler(SweepF(v, 0), SweepF(v, 1), 0f); break;
                        case "keyi": if (key != null) key.intensity = SweepF(v, 0); break;
                        case "ks": if (key != null) key.shadowStrength = SweepF(v, 0); break;
                        case "ao": AoSet("Intensity", SweepF(v, 0)); break;
                        case "aorad": AoSet("Radius", SweepF(v, 0)); break;
                        case "aodir": AoSet("DirectLightingStrength", SweepF(v, 0)); break;
                    }
                }
                yield return Frames(3);
                yield return Capture($"sweep-L{lvl}{pos}-{tok[0]}");
                sb.AppendLine($"{tok[0]} L{lvl}{pos} {line} | key {(key != null ? key.transform.eulerAngles.ToString("F1") : "-")} keyI {(key != null ? key.intensity : 0f):0.00} amb {RenderSettings.ambientLight} focus {FeltLook.DofFocus:0.00}");
            }
        }
        finally
        {
            FeltLook.AmbientOverride = null;
            if (s_SweepAo != null) { AoSet("Intensity", aoI0); AoSet("Radius", aoR0); AoSet("DirectLightingStrength", aoD0); }
            CaptureW = 1920; CaptureH = 1080;
            File.WriteAllText(file.Replace("look-sweep.txt", "look-sweep-log.txt"), sb.ToString());
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    /// TEMP b4 diagnostic: what decor sits between camera and lane, what reaches the HUD band, which materials lack textures.
    [UnityTest]
    public IEnumerator Diag_ViewDump()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        var sb = new System.Text.StringBuilder();
        try
        {
            yield return Seconds(1.0f);
            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                PickLevel(flow, id);
                yield return Frames(5);
                yield return Seconds(1.5f);
                var cam = Camera.main;
                var r = Runner();
                var def = LevelCatalog.Make(id);
                var occ = new Dictionary<string, string>();
                var hud = new Dictionary<string, string>();
                var big = new Dictionary<string, string>();
                for (float x = 0f; x < def.goalZ; x += 3f)
                {
                    float mx = x;
                    if (LevelRunner.IsOverGap(mx)) mx += 1.6f;
                    Teleport(r, mx, TopAt(mx));
                    yield return Seconds(0.6f);
                    var cp = cam.transform.position;
                    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                    foreach (var rend in Object.FindObjectsByType<Renderer>())
                    {
                        if (!rend.enabled || !rend.gameObject.activeInHierarchy) continue;
                        var b = rend.bounds;
                        if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                        var path = DiagPath(rend.transform);
                        if (path.Contains("Bean") || path.StartsWith("Level Pickups") || path.Contains("Canvas")) continue;
                        for (float d = 0f; d <= 4f; d += 0.5f)
                        {
                            var target = new Vector3(mx + d, TopAt(mx + d) + 0.45f, 0f);
                            var ray = new Ray(cp, target - cp);
                            float len = Vector3.Distance(cp, target);
                            if (b.IntersectRay(ray, out float hit) && hit < len - 0.35f)
                            { occ[path] = $"x{mx:0} d{d:0.0} hit{hit:0.00}/{len:0.00} c{b.center} s{b.size}"; break; }
                        }
                        var vmax = cam.WorldToViewportPoint(b.center + new Vector3(0, b.extents.y, 0));
                        if (vmax.y > 0.86f && vmax.z > 0 && vmax.x > 0 && vmax.x < 1) hud[path] = $"x{mx:0} vy{vmax.y:0.00} c{b.center} s{b.size}";
                        var m = rend.sharedMaterial;
                        bool noTex = m != null && (m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") == null : m.mainTexture == null);
                        var vmin = cam.WorldToViewportPoint(b.min); var vmx = cam.WorldToViewportPoint(b.max);
                        float area = Mathf.Abs((vmx.x - vmin.x) * (vmx.y - vmin.y));
                        if (noTex && area > 0.02f) big[path] = $"x{mx:0} area{area:0.00} mat {m.name} shader {m.shader.name} col {(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")} c{b.center} s{b.size}";
                    }
                }
                sb.AppendLine($"== L{id} occluders {occ.Count}");
                foreach (var kv in occ) sb.AppendLine("OCC " + kv.Key + " | " + kv.Value);
                sb.AppendLine($"== L{id} hudBand {hud.Count}");
                foreach (var kv in hud) sb.AppendLine("HUD " + kv.Key + " | " + kv.Value);
                sb.AppendLine($"== L{id} untexturedBig {big.Count}");
                foreach (var kv in big) sb.AppendLine("UNTEX " + kv.Key + " | " + kv.Value);
            }
        }
        finally
        {
            File.WriteAllText(LookLog.Replace("look.txt", "diag.txt"), sb.ToString());
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Look_PostDofMossGarland()
    {
        PreBoot();
        yield return new EnterPlayMode();
        yield return Boot();
        var flow = Flow();
        var sb = new System.Text.StringBuilder();
        CaptureW = 1280; CaptureH = 720;
        try
        {
            yield return Seconds(1.0f);
            var rdata = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/EscapeURP_Renderer.asset");
            int ssao = 0;
            if (rdata != null) foreach (var f in rdata.rendererFeatures) if (f is UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion && f.isActive) ssao++;
            sb.AppendLine($"title postData {(rdata != null && rdata.postProcessData != null)} post {FeltLook.CameraPostOn()} volume {(FeltLook.LookVolume != null)} ssao {ssao} mossDarkened {FeltLook.MossDarkened} garland {(FeltLook.Garland != null && FeltLook.Garland.activeInHierarchy)} dofEnabled {FeltLook.DofEnabled}");
            yield return Capture(LookStep + "-title");
            // step01 fix #1: menu text must not be blurred/graded by post (UiAfterPostFeature). Compare the text
            // sharpness of the title menu with the look volume on vs off (weight 0 = no DoF/grade).
            float menuOn = MenuTextSharp(flow), menuOff;
            FeltLook.LookVolume.weight = 0f;
            yield return Frames(2);
            yield return Capture(LookStep + "-title-postoff");
            menuOff = MenuTextSharp(flow);
            FeltLook.LookVolume.weight = 1f;
            yield return Frames(2);
            int uiLayerScene = 0;
            foreach (var rend in Object.FindObjectsByType<Renderer>()) if (rend.gameObject.layer == FeltLook.UiLayer) uiLayerScene++;
            var menuCanvas = flow.GetComponentInChildren<Canvas>(true);
            sb.AppendLine($"step01 menuTextSharp postOn {menuOn:0.000} postOff {menuOff:0.000} ratio {(menuOff > 0f ? menuOn / menuOff : 0f):0.000} canvasMode {menuCanvas.renderMode} canvasLayer {menuCanvas.gameObject.layer} sceneRenderersOnUiLayer {uiLayerScene}");
            Assert.Greater(menuOff, 0f, "menu text found in the title capture");
            Assert.That(menuOn, Is.GreaterThanOrEqualTo(menuOff * 0.9f), "menu text as sharp with post on as off (UI drawn after post)");
            Assert.AreEqual(0, uiLayerScene, "no scene renderer on the UI layer (it is drawn after post, undimmed)");
            var urp = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string rdText = File.ReadAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets", "Settings", "EscapeURP_Renderer.asset"));
            FeltLook.LookVolume.profile.TryGet<UnityEngine.Rendering.Universal.DepthOfField>(out var lookDof);
            FeltLook.LookVolume.profile.TryGet<UnityEngine.Rendering.Universal.Tonemapping>(out var lookTone);
            sb.AppendLine($"step01 hdr {urp.supportsHDR} ssaoSourceDepth {rdText.Contains("    Source: 0")} dof {lookDof?.mode.value} tone {lookTone?.mode.value}");
            sb.AppendLine($"step01 colorGrading {urp.colorGradingMode} shadowRes {urp.mainLightShadowmapResolution} cascades {urp.shadowCascadeCount} uiAfterPost {rdText.Contains("UiAfterPostFeature")} transparentMask {rdata.transparentLayerMask.value}");
            Assert.IsTrue(urp.supportsHDR, "HDR on (Neutral tonemapping)");
            Assert.AreEqual(UnityEngine.Rendering.Universal.ColorGradingMode.HighDynamicRange, urp.colorGradingMode, "spec D5: HDR grading");
            Assert.IsTrue(rdText.Contains("UiAfterPostFeature"), "UI after post renderer feature");
            Assert.AreEqual(0, rdata.transparentLayerMask.value & (1 << FeltLook.UiLayer), "UI layer out of the in-scene transparent pass");
            Assert.IsTrue(rdText.Contains("    Source: 0"), "Felt SSAO Source = Depth (DepthNormals empties the depth texture for felt shaders -> DoF blurs everything)");
            Assert.AreEqual(UnityEngine.Rendering.Universal.DepthOfFieldMode.Bokeh, lookDof.mode.value, "285 DoF: Bokeh near+far");
            Assert.AreEqual(UnityEngine.Rendering.Universal.TonemappingMode.Neutral, lookTone.mode.value, "spec D5: Neutral tonemapping");
            Assert.IsTrue(FeltLook.CameraPostOn(), "camera renders post-processing");
            Assert.IsNotNull(FeltLook.LookVolume, "Felt Look volume");
            Assert.AreEqual(1, ssao, "SSAO renderer feature");
            Assert.IsNotNull(rdata.postProcessData, "renderer PostProcessData (null disables all URP post-processing)");

            for (int id = 1; id <= LevelCatalog.LevelCount; id++)
            {
                PickLevel(flow, id);
                yield return Frames(5);
                yield return Seconds(2.5f);
                var cam = Camera.main;
                var g = FeltLook.Garland;
                bool gOn = g != null && g.activeInHierarchy;
                float vMin = 1f, vMax = 0f; int clouds = 0;
                if (gOn)
                    foreach (var rend in g.GetComponentsInChildren<Renderer>())
                    {
                        var vp = cam.WorldToViewportPoint(rend.bounds.center);
                        if (vp.x < -0.1f || vp.x > 1.1f || vp.z <= 0f) continue;
                        clouds++;
                        vMin = Mathf.Min(vMin, vp.y); vMax = Mathf.Max(vMax, vp.y);
                    }
                int mossLeft = 0;
                foreach (var rend in Object.FindObjectsByType<Renderer>())
                {
                    var n = rend.gameObject.name;
                    if ((n == "Moss" || n == "Moss Ball" || n == "Moss Halo") && rend.sharedMaterial != null && !rend.sharedMaterial.name.EndsWith("(285 olive)")) mossLeft++;
                }
                sb.AppendLine($"L{id} post {FeltLook.CameraPostOn()} mossDarkened {FeltLook.MossDarkened} mossClones {FeltLook.MossClones} mossLeft {mossLeft} garland {gOn} garlandParts {clouds} garlandViewY {vMin:0.00}-{vMax:0.00} fgCount {FeltForeground.Count}");
                yield return Capture($"{LookStep}-L{id}-start");
                float beanSharp = 0f, bandSharp = 0f;
                var beanR = Runner().bean.GetComponentsInChildren<Renderer>();
                if (beanR.Length > 0 && LastPixels != null)
                {
                    var bb = beanR[0].bounds; foreach (var br in beanR) bb.Encapsulate(br.bounds);
                    var a = cam.WorldToViewportPoint(bb.center - bb.extents * 0.45f);
                    var b = cam.WorldToViewportPoint(bb.center + bb.extents * 0.45f);
                    beanSharp = GradMean(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                    bandSharp = GradMean(0.05f, 0.66f, 0.95f, 0.76f);
                }
                sb.AppendLine($"L{id} dofFocus {FeltLook.DofFocus:0.00} beanSharp {beanSharp:0.00} backgroundBandSharp {bandSharp:0.00}");
                var r = Runner();
                var def = LevelCatalog.Make(id);
                float mx = Mathf.Lerp(1f, def.goalZ - 2f, 0.5f);
                if (LevelRunner.IsOverGap(mx)) mx += 1.6f;
                // look step00: the 285 pose is on floor level. If mid-level lands on a raised block/lift, capture that too
                // (-mid-raised, logged as is), then take -mid at the nearest floor-level spot.
                float floorY = TopAt(1f);
                // the bean keeps running during the 1.6 s settle, so the next 4 m must be floor level too (s00i: L4 ran onto a crate)
                System.Func<float, bool> clear = a =>
                {
                    for (float d = 0f; d <= 4f; d += 0.25f)
                    {
                        if (Mathf.Abs(TopAt(a + d) - floorY) > 0.05f || LevelRunner.IsOverGap(a + d)) return false;
                        // crates/obstacles are not in LevelRunner.Blocks (s00j: L3 mid landed on one), so probe the colliders too
                        foreach (var h in Physics.RaycastAll(new Vector3(a + d, floorY + 5f, 0f), Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                            if (!h.collider.transform.IsChildOf(r.bean) && h.point.y > floorY + 0.12f) return false;
                    }
                    return true;
                };
                if (!clear(mx))
                {
                    if (Mathf.Abs(TopAt(mx) - floorY) > 0.05f)
                    {
                        Teleport(r, mx, TopAt(mx));
                        yield return Seconds(1.6f);
                        yield return Capture($"{LookStep}-L{id}-mid-raised");
                    }
                    for (int k = 1; k <= 40; k++)
                    {
                        float a1 = mx + k * 0.5f, a2 = mx - k * 0.5f;
                        if (clear(a1)) { mx = a1; break; }
                        if (clear(a2)) { mx = a2; break; }
                    }
                }
                Teleport(r, mx, TopAt(mx));
                yield return Seconds(1.6f);
                sb.AppendLine($"L{id} midX {mx:0.00} floorY {floorY:0.00} beanY {r.bean.position.y:0.00} beanX {r.bean.position.x:0.00}");
                yield return Capture($"{LookStep}-L{id}-mid");

                Assert.IsTrue(FeltLook.CameraPostOn(), $"L{id} post-processing on");
                Assert.AreEqual(0, mossLeft, $"L{id} every moss renderer darkened");
                Assert.That(FeltLook.MossClones, Is.LessThanOrEqualTo(32), $"L{id} olive moss clones bounded (no per-level material growth)");
                Assert.IsTrue(gOn, $"L{id} play garland shown");
                Assert.Greater(clouds, 0, $"L{id} garland in frame");
                Assert.That(vMin, Is.GreaterThan(0.70f), $"L{id} garland stays in the upper frame");
                Assert.That(vMax, Is.LessThan(0.95f), $"L{id} garland under the frame top");
                Assert.That(FeltLook.DofFocus, Is.InRange(2.5f, 6f), $"L{id} DoF focus tracks the bean");
                Assert.That(beanSharp, Is.GreaterThan(bandSharp * 1.5f), $"L{id} bean sharper than the far background band (depth-aware DoF)");
            }
        }
        finally
        {
            CaptureW = 1920; CaptureH = 1080;
            File.WriteAllText(LookLog, sb.ToString());
        }
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
        PostShutdown();
    }

    /// Mean luma gradient over the active menu Text rects of the last capture (screen-space-camera canvas).
    static float MenuTextSharp(TitleFlow flow)
    {
        var cam = Camera.main; if (cam == null || flow == null) return 0f;
        double sum = 0; int n = 0; var wc = new Vector3[4];
        foreach (var t in flow.GetComponentsInChildren<UnityEngine.UI.Text>())
        {
            if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.color.a < 0.5f) continue;
            t.rectTransform.GetWorldCorners(wc);
            var c = t.canvas; Camera uc = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(uc, wc[0]), b = RectTransformUtility.WorldToScreenPoint(uc, wc[2]);
            float sw = uc != null ? uc.pixelWidth : Screen.width, sh = uc != null ? uc.pixelHeight : Screen.height;
            float x0 = Mathf.Clamp01(Mathf.Min(a.x, b.x) / sw), x1 = Mathf.Clamp01(Mathf.Max(a.x, b.x) / sw);
            float y0 = Mathf.Clamp01(Mathf.Min(a.y, b.y) / sh), y1 = Mathf.Clamp01(Mathf.Max(a.y, b.y) / sh);
            if (x1 - x0 < 0.01f || y1 - y0 < 0.01f) continue;
            float g = GradMean(x0, y0, x1, y1); float area = (x1 - x0) * (y1 - y0);
            sum += g * area; n++;
            MenuTextArea += area;
        }
        float res = MenuTextArea > 0f ? (float)(sum / MenuTextArea) : 0f; MenuTextArea = 0f;
        return res;
    }
    static float MenuTextArea;

    /// Mean luma gradient inside a viewport rect of the last capture (sharpness probe).
    static float GradMean(float x0, float y0, float x1, float y1)
    {
        if (LastPixels == null) return 0f;
        int ix0 = Mathf.Clamp((int)(x0 * LastW), 1, LastW - 2), ix1 = Mathf.Clamp((int)(x1 * LastW), 1, LastW - 2);
        int iy0 = Mathf.Clamp((int)(y0 * LastH), 1, LastH - 2), iy1 = Mathf.Clamp((int)(y1 * LastH), 1, LastH - 2);
        double sum = 0; int n = 0;
        for (int y = iy0; y < iy1; y++)
            for (int x = ix0; x < ix1; x++)
            {
                var c = LastPixels[y * LastW + x]; var r = LastPixels[y * LastW + x + 1]; var u = LastPixels[(y + 1) * LastW + x];
                float l = c.r + c.g + c.b, lr = r.r + r.g + r.b, lu = u.r + u.g + u.b;
                sum += Mathf.Abs(lr - l) + Mathf.Abs(lu - l); n++;
            }
        return n > 0 ? (float)(sum / n / 3.0) : 0f;
    }

    // ───────────── capture ─────────────

    /// Renders Camera.main plus the UI canvas into a 1920x1080 PNG. Overlay canvases are moved onto the camera for the
    /// shot only, then restored, so HUD / pause / clear cards appear in the image as they do on screen.
    /// look step00: the bean's silhouette from the SAME camera, RT size and frame as the capture (bean renderers only, no post),
    /// saved as NAME-beanmask.png, plus its pixel box (image rows from the top) as NAME-beanbox.txt. Proof for framing numbers.
    const int CaptureBeanLayer = 30;
    static string BeanMaskPass(Camera cam, string name)
    {
        var rb = Runner();
        Transform bean = rb != null && rb.bean != null && rb.bean.gameObject.activeInHierarchy ? rb.bean : null;
        if (bean == null) { var go = GameObject.Find("Imagine Bean"); if (go != null) bean = go.transform; }
        if (bean == null) return null;
        var rs = bean.GetComponentsInChildren<Renderer>(false);
        var prev = new int[rs.Length];
        for (int i = 0; i < rs.Length; i++) { prev[i] = rs[i].gameObject.layer; rs[i].gameObject.layer = CaptureBeanLayer; }
        var mRt = new RenderTexture(CaptureW, CaptureH, 24, RenderTextureFormat.ARGB32);
        var mGo = new GameObject("CaptureBeanMaskCam");
        var mCam = mGo.AddComponent<Camera>();
        mCam.CopyFrom(cam);
        mCam.cullingMask = 1 << CaptureBeanLayer;
        mCam.clearFlags = CameraClearFlags.SolidColor;
        mCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        mCam.allowHDR = false; mCam.allowMSAA = false;
        mCam.targetTexture = mRt;
        var md = mGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        md.renderPostProcessing = false; md.renderShadows = false;
        mCam.Render();
        for (int i = 0; i < rs.Length; i++) rs[i].gameObject.layer = prev[i];
        var pa = RenderTexture.active; RenderTexture.active = mRt;
        var mt = new Texture2D(CaptureW, CaptureH, TextureFormat.RGBA32, false);
        mt.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0); mt.Apply();
        RenderTexture.active = pa;
        var px = mt.GetPixels32();
        int top = int.MaxValue, bot = -1, left = int.MaxValue, right = -1, n = 0;
        var outPx = new Color32[px.Length];
        for (int y = 0; y < CaptureH; y++)
            for (int x = 0; x < CaptureW; x++)
            {
                var c = px[y * CaptureW + x];
                bool on = c.a > 0 || c.r + c.g + c.b > 6;
                outPx[y * CaptureW + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
                if (!on) continue;
                int row = CaptureH - 1 - y; n++;
                if (row < top) top = row; if (row > bot) bot = row; if (x < left) left = x; if (x > right) right = x;
            }
        mt.SetPixels32(outPx); mt.Apply();
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "grok-captures"));
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, name + "-beanmask.png"), mt.EncodeToPNG());
        Object.Destroy(mt); Object.DestroyImmediate(mGo); mRt.Release(); Object.Destroy(mRt);
        return n == 0 ? $"{name} W {CaptureW} H {CaptureH} pixels 0\n"
            : $"{name} W {CaptureW} H {CaptureH} pixels {n} top {top} feet {bot} left {left} right {right} beanPos {bean.position} camPos {cam.transform.position} camPitch {cam.transform.eulerAngles.x:0.00} fov {cam.fieldOfView:0.0}\n";
    }

    const int CaptureUiLayer = 31; // a layer the scene does not use; only the HUD is moved onto it for the UI pass
    static IEnumerator Capture(string name)
    {
        yield return null;
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("CAPTURE_SKIP no camera " + name); yield break; }
        var flow = Flow();
        var canvas = flow != null ? flow.GetComponentInChildren<Canvas>(true) : null;
        var rt = new RenderTexture(CaptureW, CaptureH, CapDepthBits, RenderTextureFormat.ARGB32) { antiAliasing = CapAA };
        var prevTarget = cam.targetTexture;
        RenderMode prevMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
        Camera prevCam = canvas != null ? canvas.worldCamera : null;
        float prevPlane = canvas != null ? canvas.planeDistance : 0f;
        cam.targetTexture = rt;
        // Pass 1: the 3D scene with its post stack (DoF/bloom/ACES) - the Overlay HUD is not in this pass, as on device.
        cam.Render();
        cam.Render();
        // Pass 2: the HUD. On device it is a Screen Space Overlay canvas, never touched by DoF/post. URP base cameras
        // clear their target, so the HUD is drawn by a post-free UI-only camera into its own transparent RT and
        // composited on the CPU (UI blend SrcAlpha/OneMinusSrcAlpha onto (0,0,0,0) -> rgb = Cs*a, alpha = a*a).
        GameObject uiGo = null; int prevLayer = -1; RenderTexture uiRt = null;
        if (canvas != null && prevMode == RenderMode.ScreenSpaceOverlay)
        {
            uiRt = new RenderTexture(CaptureW, CaptureH, 24, RenderTextureFormat.ARGB32);
            uiGo = new GameObject("CaptureUICam");
            var uiCam = uiGo.AddComponent<Camera>();
            uiCam.CopyFrom(cam);
            prevLayer = canvas.gameObject.layer;
            canvas.gameObject.layer = CaptureUiLayer;          // the HUD canvas sits on Default; isolate it so this pass draws no scene
            uiCam.cullingMask = 1 << CaptureUiLayer;
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.allowHDR = false; uiCam.allowMSAA = false;
            uiCam.targetTexture = uiRt;
            var uiData = uiGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            uiData.renderPostProcessing = false;
            uiData.renderShadows = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 0.1f;
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);
            uiCam.Render();
        }
        string beanBox = BeanMaskPass(cam, name);
        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(CaptureW, CaptureH, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0);
        tex.Apply();
        if (uiRt != null)
        {
            RenderTexture.active = uiRt;
            var ui = new Texture2D(CaptureW, CaptureH, TextureFormat.RGBA32, false);
            ui.ReadPixels(new Rect(0, 0, CaptureW, CaptureH), 0, 0);
            ui.Apply();
            var sp = tex.GetPixels32(); var up = ui.GetPixels32();
            for (int i = 0; i < sp.Length; i++)
            {
                float A = up[i].a / 255f; if (A <= 0f) continue;
                float al = Mathf.Sqrt(A), k = 1f - al;
                sp[i] = new Color32((byte)Mathf.Min(255, sp[i].r * k + up[i].r), (byte)Mathf.Min(255, sp[i].g * k + up[i].g),
                                    (byte)Mathf.Min(255, sp[i].b * k + up[i].b), 255);
            }
            tex.SetPixels32(sp); tex.Apply();
            Object.Destroy(ui);
        }
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;
        if (canvas != null && prevMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.planeDistance = prevPlane;
        }
        if (prevLayer >= 0) canvas.gameObject.layer = prevLayer;
        if (uiGo != null) Object.DestroyImmediate(uiGo);
        if (uiRt != null) { uiRt.Release(); Object.Destroy(uiRt); }
        rt.Release();
        Object.Destroy(rt);
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "grok-captures"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name + ".png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        LastPixels = tex.GetPixels32(); LastW = CaptureW; LastH = CaptureH;
        Object.Destroy(tex);
        if (beanBox != null) File.WriteAllText(Path.Combine(dir, name + "-beanbox.txt"), beanBox);
        Debug.Log($"CAPTURE_OK {path} drawCalls={UnityEditor.UnityStats.drawCalls} setPass={UnityEditor.UnityStats.setPassCalls} tris={UnityEditor.UnityStats.triangles}");
    }
}

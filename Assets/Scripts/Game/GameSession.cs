using UnityEngine;

/// Runtime session — which level is live, boot phase, bean count, level-clear flow.
public class GameSession : MonoBehaviour
{
    public enum Phase { Boot, Title, Options, Search, Play, Clear }

    public static GameSession I { get; private set; }

    public Phase phase = Phase.Boot;
    public int currentLevelId = 1;
    public LevelCatalog catalog;
    public int beansCollected;
    public int beansTotal;

    /// Result of the most recent clear, filled in before OnLevelClear fires.
    public int lastStars;
    public float lastSeconds;
    public bool lastImproved;

    float _levelStart;

    /// Seconds spent in the live level (scaled time, so the pause menu does not count).
    public float ElapsedSeconds => phase == Phase.Play ? Time.time - _levelStart : lastSeconds;

    /// (levelId, beansCollected, beansTotal)
    public static event System.Action<int, int, int> OnLevelClear;
    public static event System.Action<int, int> OnBeansChanged;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        OpeningYard.Keep();
        DontDestroyOnLoad(gameObject);
        if (catalog == null)
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        // Always rebuild from LevelCatalog.Make so a scene copy cannot keep the old
        // per-level gravity (1.02–1.10) or the short L5 goal. Play gravity stays −22.
        catalog.RebuildAll();
        currentLevelId = PlayerProgress.LastPlayed;
        BootQuality();
        if (GetComponent<EscapeAudio>() == null) gameObject.AddComponent<EscapeAudio>();
        GameSettings.Apply(); // volumes, mute, graphics tier (Low overrides the BootQuality shadows)
        EnsureTitleFlow();
    }

    /// The committed Escape.unity predates the "Title Flow" object that BuildEscapeScene adds,
    /// so without this the scene plays with no menus. One lookup at boot, never per frame.
    /// iOS can kill a backgrounded app without another callback: flush progress and settings now.
    /// TitleFlow.OnApplicationPause opens the Pause card (timeScale 0) for the same event.
    void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    static void EnsureTitleFlow()
    {
        if (FindAnyObjectByType<TitleFlow>() == null)
            new GameObject("Title Flow").AddComponent<TitleFlow>();
    }

    static void BootQuality()
    {
        // Active tier "Ultra" ships with shadows off; the felt look needs soft ground shadows.
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 30f;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // side-scroller: landscape like the Imagine set frames (no-op in the Editor / on desktop)
        if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void SetPhase(Phase p)
    {
        phase = p;
        // the bean only moves in Play; menus must not spend taps on the physics body.
        // BeanController (run + jump) owns input when present; BeanImpulse stays on the bean for the proofs, disabled.
        var hop = FindAnyObjectByType<BeanImpulse>(FindObjectsInactive.Include);
        var ctl = FindAnyObjectByType<BeanController>(FindObjectsInactive.Include);
        if (ctl == null && hop != null) ctl = hop.gameObject.AddComponent<BeanController>(); // scenes built before the controller existed
        if (ctl != null)
        {
            ctl.enabled = p == Phase.Play;
            if (hop != null) hop.enabled = false;
        }
        else if (hop != null) hop.enabled = p == Phase.Play;
    }

    public void GoTitle() { SetPhase(Phase.Title); }
    public void GoOptions() { SetPhase(Phase.Options); }
    public void GoSearch() { SetPhase(Phase.Search); }

    public void PlayLevel(int id)
    {
        currentLevelId = Mathf.Clamp(id, 1, LevelCatalog.LevelCount);
        PlayerProgress.LastPlayed = currentLevelId;
        beansCollected = 0;
        var runner = FindAnyObjectByType<LevelRunner>();
        if (runner != null) runner.LoadLevel(catalog.Get(currentLevelId));
        beansTotal = runner != null ? runner.PickupCount : 0;
        OnBeansChanged?.Invoke(beansCollected, beansTotal);
        _levelStart = Time.time;
        SetPhase(Phase.Play);
    }

    public void Continue() { PlayLevel(PlayerProgress.LastPlayed); }

    public void NewGame()
    {
        PlayerProgress.Unlocked = 1;
        PlayerProgress.LastPlayed = 1;
        PlayLevel(1);
    }

    /// L1 → L2 → L3 → L4 → L5. CompleteLevel has already unlocked current+1.
    public void NextLevel()
    {
        int next = Mathf.Min(LevelCatalog.LevelCount, currentLevelId + 1);
        PlayLevel(next);
    }

    public void CollectBean()
    {
        beansCollected++;
        OnBeansChanged?.Invoke(beansCollected, beansTotal);
    }

    /// Called by LevelRunner when the bean crosses goalZ.
    public void CompleteLevel()
    {
        if (phase != Phase.Play) return;
        lastSeconds = Time.time - _levelStart;
        PlayerProgress.ClearBeat(currentLevelId);
        lastStars = PlayerProgress.StarsFor(beansCollected, beansTotal, lastSeconds, Current.parSeconds);
        lastImproved = PlayerProgress.RecordClear(currentLevelId, lastStars, lastSeconds);
        SetPhase(Phase.Clear);
        EscapeAudio.Play(EscapeAudio.Sfx.Clear);
        Haptics.Pulse();
        Debug.Log($"LEVEL_CLEAR L{currentLevelId} beans={beansCollected}/{beansTotal} stars={lastStars} time={lastSeconds:0.0}s unlocked={PlayerProgress.Unlocked}");
        OnLevelClear?.Invoke(currentLevelId, beansCollected, beansTotal);
    }

    public LevelCatalog.LevelDef Current => catalog != null ? catalog.Get(currentLevelId) : LevelCatalog.Make(currentLevelId);
}

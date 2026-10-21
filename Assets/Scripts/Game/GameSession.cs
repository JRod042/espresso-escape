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

    /// (levelId, beansCollected, beansTotal)
    public static event System.Action<int, int, int> OnLevelClear;
    public static event System.Action<int, int> OnBeansChanged;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.RebuildAll();
        }
        else if (catalog.levels == null || catalog.levels.Length != LevelCatalog.LevelCount)
            catalog.RebuildAll();
        currentLevelId = PlayerProgress.LastPlayed;
        BootQuality();
        if (GetComponent<EscapeAudio>() == null) gameObject.AddComponent<EscapeAudio>();
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
        // the bean only hops in Play; menus must not spend taps on the physics body
        var hop = FindFirstObjectByType<BeanImpulse>(FindObjectsInactive.Include);
        if (hop != null) hop.enabled = p == Phase.Play;
    }

    public void GoTitle() { SetPhase(Phase.Title); }
    public void GoOptions() { SetPhase(Phase.Options); }
    public void GoSearch() { SetPhase(Phase.Search); }

    public void PlayLevel(int id)
    {
        currentLevelId = Mathf.Clamp(id, 1, LevelCatalog.LevelCount);
        PlayerProgress.LastPlayed = currentLevelId;
        beansCollected = 0;
        var runner = FindFirstObjectByType<LevelRunner>();
        if (runner != null) runner.LoadLevel(catalog.Get(currentLevelId));
        beansTotal = runner != null ? runner.PickupCount : 0;
        OnBeansChanged?.Invoke(beansCollected, beansTotal);
        SetPhase(Phase.Play);
    }

    public void Continue() { PlayLevel(PlayerProgress.LastPlayed); }

    public void NewGame()
    {
        PlayerProgress.Unlocked = 1;
        PlayerProgress.LastPlayed = 1;
        PlayLevel(1);
    }

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
        PlayerProgress.ClearBeat(currentLevelId);
        SetPhase(Phase.Clear);
        EscapeAudio.Play(EscapeAudio.Sfx.Clear);
        Debug.Log($"LEVEL_CLEAR L{currentLevelId} beans={beansCollected}/{beansTotal} unlocked={PlayerProgress.Unlocked}");
        OnLevelClear?.Invoke(currentLevelId, beansCollected, beansTotal);
    }

    public LevelCatalog.LevelDef Current => catalog != null ? catalog.Get(currentLevelId) : LevelCatalog.Make(currentLevelId);
}

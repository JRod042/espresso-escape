using System;
using System.Reflection;
using UnityEngine;

/// Resolves gameplay types out of the predefined Assembly-CSharp assembly.
/// A test asmdef cannot reference Assembly-CSharp. The game scripts stay in that
/// predefined assembly so this suite does not add an asmdef under Assets/Scripts.
public static class GameTypes
{
    public struct LevelRow
    {
        public int id;
        public string title;
        public string pack;
        public string blurb;
        public float goalZ;
        public float obstacleDensity;
        public float gravityScale;
        public int seed;
        public bool bossBeat;
    }

    static Assembly _game;

    public static Assembly GameAssembly
    {
        get
        {
            if (_game != null) return _game;
            _game = Assembly.Load("Assembly-CSharp");
            return _game;
        }
    }

    public static Type Require(string name)
    {
        var type = GameAssembly.GetType(name, throwOnError: false);
        if (type == null)
            throw new InvalidOperationException("Assembly-CSharp has no public type " + name);
        return type;
    }

    public static int LevelCount
    {
        get
        {
            var field = Require("LevelCatalog").GetField("LevelCount", BindingFlags.Public | BindingFlags.Static);
            if (field == null) throw new MissingFieldException("LevelCatalog.LevelCount");
            return (int)field.GetValue(null);
        }
    }

    public static LevelRow Make(int id)
    {
        var method = Require("LevelCatalog").GetMethod("Make", BindingFlags.Public | BindingFlags.Static);
        if (method == null) throw new MissingMethodException("LevelCatalog.Make");
        return Read(method.Invoke(null, new object[] { id }));
    }

    public static LevelRow[] MakeAll()
    {
        int n = LevelCount;
        var rows = new LevelRow[n];
        for (int id = 1; id <= n; id++)
            rows[id - 1] = Make(id);
        return rows;
    }

    /// RebuildAll then Get(1..LevelCount). This is the path GameSession.Awake uses.
    public static LevelRow[] RebuiltGets()
    {
        var type = Require("LevelCatalog");
        var cat = ScriptableObject.CreateInstance(type);
        try
        {
            Invoke(type, "RebuildAll", cat);
            var get = type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);
            if (get == null) throw new MissingMethodException("LevelCatalog.Get");
            int n = LevelCount;
            var rows = new LevelRow[n];
            for (int id = 1; id <= n; id++)
                rows[id - 1] = Read(get.Invoke(cat, new object[] { id }));
            return rows;
        }
        finally
        {
            Destroy(cat);
        }
    }

    public static int[] SearchIds(string query)
    {
        var type = Require("LevelCatalog");
        var cat = ScriptableObject.CreateInstance(type);
        try
        {
            Invoke(type, "RebuildAll", cat);
            var search = type.GetMethod("Search", BindingFlags.Public | BindingFlags.Instance);
            if (search == null) throw new MissingMethodException("LevelCatalog.Search");
            var result = search.Invoke(cat, new object[] { query }) as Array;
            if (result == null) throw new InvalidOperationException("LevelCatalog.Search returned null");
            var ids = new int[result.Length];
            for (int i = 0; i < result.Length; i++)
                ids[i] = Read(result.GetValue(i)).id;
            return ids;
        }
        finally
        {
            Destroy(cat);
        }
    }

    public static int Unlocked
    {
        get { return (int)ProgressProp("Unlocked").GetValue(null); }
        set { ProgressProp("Unlocked").SetValue(null, value); }
    }

    public static int LastPlayed
    {
        get { return (int)ProgressProp("LastPlayed").GetValue(null); }
        set { ProgressProp("LastPlayed").SetValue(null, value); }
    }

    static PropertyInfo ProgressProp(string name)
    {
        var prop = Require("PlayerProgress").GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (prop == null) throw new MissingMemberException("PlayerProgress." + name);
        return prop;
    }

    static void Invoke(Type type, string name, object target)
    {
        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
        if (method == null) throw new MissingMethodException("LevelCatalog." + name);
        method.Invoke(target, null);
    }

    static LevelRow Read(object boxed)
    {
        if (boxed == null) throw new InvalidOperationException("LevelDef was null");
        var type = boxed.GetType();
        return new LevelRow
        {
            id = (int)Field(type, boxed, "id"),
            title = (string)Field(type, boxed, "title"),
            pack = (string)Field(type, boxed, "pack"),
            blurb = (string)Field(type, boxed, "blurb"),
            goalZ = (float)Field(type, boxed, "goalZ"),
            obstacleDensity = (float)Field(type, boxed, "obstacleDensity"),
            gravityScale = (float)Field(type, boxed, "gravityScale"),
            seed = (int)Field(type, boxed, "seed"),
            bossBeat = (bool)Field(type, boxed, "bossBeat")
        };
    }

    static object Field(Type type, object boxed, string name)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field == null) throw new MissingFieldException(type.Name + "." + name);
        return field.GetValue(boxed);
    }

    static void Destroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
        else UnityEngine.Object.DestroyImmediate(obj);
    }
}

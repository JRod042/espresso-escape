using UnityEngine.Assertions;

/// Shared checks so EditMode and PlayMode assert the same contract.
public static class CatalogContract
{
    public static void LevelCountIsFive()
    {
        Assert.AreEqual(5, GameTypes.LevelCount, "LevelCatalog.LevelCount");
        Assert.AreEqual(5, GameTypes.MakeAll().Length, "Make(1..LevelCount)");
    }

    public static void GoalZStrictlyAscending()
    {
        var rows = GameTypes.MakeAll();
        for (int i = 1; i < rows.Length; i++)
        {
            Assert.IsTrue(rows[i].goalZ > rows[i - 1].goalZ,
                "L" + rows[i].id + " goalZ " + rows[i].goalZ + " is not greater than L" + rows[i - 1].id + " " + rows[i - 1].goalZ);
        }
    }

    public static void DensityStrictlyAscending()
    {
        var rows = GameTypes.MakeAll();
        for (int i = 1; i < rows.Length; i++)
        {
            Assert.IsTrue(rows[i].obstacleDensity > rows[i - 1].obstacleDensity,
                "L" + rows[i].id + " obstacleDensity " + rows[i].obstacleDensity
                + " is not greater than L" + rows[i - 1].id + " " + rows[i - 1].obstacleDensity);
        }
    }

    /// Requested difficulty contract. LevelCatalog.Make currently returns gravityScale 1 on every level,
    /// so this check fails until those values strictly increase.
    public static void GravityScaleStrictlyAscending()
    {
        var rows = GameTypes.MakeAll();
        for (int i = 1; i < rows.Length; i++)
        {
            Assert.IsTrue(rows[i].gravityScale > rows[i - 1].gravityScale,
                "L" + rows[i].id + " gravityScale " + rows[i].gravityScale
                + " is not greater than L" + rows[i - 1].id + " " + rows[i - 1].gravityScale
                + ". Make() returns 1 on every level.");
        }
    }

    public static void FinaleIsBossAndEarlierAreNot()
    {
        var rows = GameTypes.MakeAll();
        Assert.AreEqual(5, rows.Length, "expected 5 levels before the boss check");
        for (int i = 0; i < rows.Length; i++)
        {
            bool boss = rows[i].id == 5;
            Assert.AreEqual(boss, rows[i].bossBeat, "L" + rows[i].id + " bossBeat");
        }
        Assert.IsTrue(rows[rows.Length - 1].bossBeat, "L5 bossBeat");
        Assert.AreEqual(5, rows[rows.Length - 1].id, "last row id");
    }

    public static void EachConfigValid()
    {
        var rows = GameTypes.MakeAll();
        Assert.AreEqual(5, rows.Length, "config count");
        for (int i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            Assert.AreEqual(i + 1, row.id, "id matches slot");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.title), "L" + row.id + " title");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.pack), "L" + row.id + " pack");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.blurb), "L" + row.id + " blurb");
            Assert.IsTrue(row.goalZ > 0f, "L" + row.id + " goalZ");
            Assert.IsTrue(row.obstacleDensity > 0f && row.obstacleDensity <= 1f, "L" + row.id + " obstacleDensity");
            Assert.IsTrue(row.gravityScale > 0f, "L" + row.id + " gravityScale");
            Assert.IsTrue(row.seed > 0, "L" + row.id + " seed");
            for (int j = 0; j < i; j++)
            {
                Assert.IsTrue(rows[j].seed != row.seed, "duplicate seed L" + row.id);
                Assert.IsTrue(rows[j].title != row.title, "duplicate title L" + row.id);
                Assert.IsTrue(rows[j].pack != row.pack, "duplicate pack L" + row.id);
            }
        }
    }

    public static void RebuiltGetMatchesMake()
    {
        var made = GameTypes.MakeAll();
        var got = GameTypes.RebuiltGets();
        Assert.AreEqual(made.Length, got.Length, "rebuilt length");
        for (int i = 0; i < made.Length; i++)
        {
            Assert.AreEqual(made[i].id, got[i].id, "id");
            Assert.AreEqual(made[i].title, got[i].title, "title");
            Assert.AreEqual(made[i].pack, got[i].pack, "pack");
            Assert.AreEqual(made[i].blurb, got[i].blurb, "blurb");
            Assert.IsTrue(made[i].goalZ == got[i].goalZ, "goalZ L" + made[i].id);
            Assert.IsTrue(made[i].obstacleDensity == got[i].obstacleDensity, "density L" + made[i].id);
            Assert.IsTrue(made[i].gravityScale == got[i].gravityScale, "gravity L" + made[i].id);
            Assert.AreEqual(made[i].seed, got[i].seed, "seed");
            Assert.AreEqual(made[i].bossBeat, got[i].bossBeat, "boss");
        }
    }

    public static void PackSearchHitsThatLevel()
    {
        var rows = GameTypes.MakeAll();
        var all = GameTypes.SearchIds("");
        Assert.AreEqual(rows.Length, all.Length, "empty search returns every level");
        for (int i = 0; i < rows.Length; i++)
        {
            var hits = GameTypes.SearchIds(rows[i].pack);
            Assert.AreEqual(1, hits.Length, "pack search " + rows[i].pack);
            Assert.AreEqual(rows[i].id, hits[0], "pack search id " + rows[i].pack);
        }
    }
}

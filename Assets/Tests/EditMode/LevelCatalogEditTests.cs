using NUnit.Framework;

public class LevelCatalogEditTests
{
    [Test]
    public void LevelCount_IsFive()
    {
        CatalogContract.LevelCountIsFive();
    }

    [Test]
    public void GoalZ_StrictlyAscending()
    {
        CatalogContract.GoalZStrictlyAscending();
    }

    [Test]
    public void ObstacleDensity_StrictlyAscending()
    {
        CatalogContract.DensityStrictlyAscending();
    }

    [Test]
    public void GravityScale_StrictlyAscending()
    {
        CatalogContract.GravityScaleStrictlyAscending();
    }

    [Test]
    public void Finale_BossBeatTrue_EarlierLevelsFalse()
    {
        CatalogContract.FinaleIsBossAndEarlierAreNot();
    }

    [Test]
    public void EachLevel_ConfigIsValid()
    {
        CatalogContract.EachConfigValid();
    }

    [Test]
    public void RebuiltCatalog_Get_MatchesMake()
    {
        CatalogContract.RebuiltGetMatchesMake();
    }

    [Test]
    public void PackSearch_ReturnsThatLevel()
    {
        CatalogContract.PackSearchHitsThatLevel();
    }
}

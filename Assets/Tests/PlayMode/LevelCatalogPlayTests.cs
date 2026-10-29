using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class LevelCatalogPlayTests
{
    [UnityTest]
    public IEnumerator LevelCount_IsFive()
    {
        yield return null;
        CatalogContract.LevelCountIsFive();
    }

    [UnityTest]
    public IEnumerator GoalZ_StrictlyAscending()
    {
        yield return null;
        CatalogContract.GoalZStrictlyAscending();
    }

    [UnityTest]
    public IEnumerator ObstacleDensity_StrictlyAscending()
    {
        yield return null;
        CatalogContract.DensityStrictlyAscending();
    }

    [UnityTest]
    public IEnumerator GravityScale_StrictlyAscending()
    {
        yield return null;
        CatalogContract.GravityScaleStrictlyAscending();
    }

    [UnityTest]
    public IEnumerator Finale_BossBeatTrue_EarlierLevelsFalse()
    {
        yield return null;
        CatalogContract.FinaleIsBossAndEarlierAreNot();
    }

    [UnityTest]
    public IEnumerator EachLevel_ConfigIsValid()
    {
        yield return null;
        CatalogContract.EachConfigValid();
    }

    [UnityTest]
    public IEnumerator RebuiltCatalog_Get_MatchesMake()
    {
        yield return null;
        CatalogContract.RebuiltGetMatchesMake();
    }

    [UnityTest]
    public IEnumerator PackSearch_ReturnsThatLevel()
    {
        yield return null;
        CatalogContract.PackSearchHitsThatLevel();
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class PlayerProgressPlayTests
{
    [UnityTest]
    public IEnumerator SaveLoad_RoundTrip_AndClamp()
    {
        yield return null;
        PlayerProgressContract.RoundTripAndClamp();
    }
}

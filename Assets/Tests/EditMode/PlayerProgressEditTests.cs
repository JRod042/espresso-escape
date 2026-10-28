using NUnit.Framework;

public class PlayerProgressEditTests
{
    [Test]
    public void SaveLoad_RoundTrip_AndClamp()
    {
        PlayerProgressContract.RoundTripAndClamp();
    }
}

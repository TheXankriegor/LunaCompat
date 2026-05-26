using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class ScanSatSyncDataMessageTests : ModMessageTestBase<ScanSatSyncDataMessage>
{
    #region Non-Public Methods

    protected override ScanSatSyncDataMessage CreateMessage()
    {
        return new ScanSatSyncDataMessage
        {
            Body = "Mun",
            Map = new short[ScanSatConstants.CoverageSizeX, ScanSatConstants.CoverageSizeY]
        };
    }

    protected override void AssertEqual(ScanSatSyncDataMessage expected, ScanSatSyncDataMessage actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.Map.Length, actual.Map.Length);
    }

    #endregion
}

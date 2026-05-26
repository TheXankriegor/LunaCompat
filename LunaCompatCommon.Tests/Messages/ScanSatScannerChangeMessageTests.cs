using System;

using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class ScanSatScannerChangeMessageTests : ModMessageTestBase<ScanSatScannerChangeMessage>
{
    #region Non-Public Methods

    protected override ScanSatScannerChangeMessage CreateMessage()
    {
        return new ScanSatScannerChangeMessage
        {
            Loaded = true,
            Vessel = Guid.NewGuid(),
            Sensor = 3,
            Fov = 45.5f,
            MinAlt = 1000f,
            MaxAlt = 20000f,
            BestAlt = 5000f,
            RequireLight = false
        };
    }

    protected override void AssertEqual(ScanSatScannerChangeMessage expected, ScanSatScannerChangeMessage actual)
    {
        Assert.Equal(expected.Loaded, actual.Loaded);
        Assert.Equal(expected.Vessel, actual.Vessel);
        Assert.Equal(expected.Sensor, actual.Sensor);
        Assert.Equal(expected.Fov, actual.Fov);
        Assert.Equal(expected.MinAlt, actual.MinAlt);
        Assert.Equal(expected.MaxAlt, actual.MaxAlt);
        Assert.Equal(expected.BestAlt, actual.BestAlt);
        Assert.Equal(expected.RequireLight, actual.RequireLight);
    }

    #endregion
}

using System;

using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class ScanSatDataMessageTests : ModMessageTestBase<ScanSatDataMessage>
{
    #region Non-Public Methods

    protected override ScanSatDataMessage CreateMessage()
    {
        return new ScanSatDataMessage
        {
            Body = "Kerbin"
        };
    }

    protected override void AssertEqual(ScanSatDataMessage expected, ScanSatDataMessage actual)
    {
        Assert.Equal(expected.Body, actual.Body);
    }

    #endregion
}

public class ScanSatRequestDataMessageTests : ModMessageTestBase<ScanSatRequestDataMessage>
{
    #region Non-Public Methods

    protected override ScanSatRequestDataMessage CreateMessage()
    {
        return new ScanSatRequestDataMessage();
    }

    protected override void AssertEqual(ScanSatRequestDataMessage expected, ScanSatRequestDataMessage actual)
    {
        // No properties to assert
    }

    #endregion
}

public class ScanSatResetDataMessageTests : ModMessageTestBase<ScanSatResetDataMessage>
{
    #region Non-Public Methods

    protected override ScanSatResetDataMessage CreateMessage()
    {
        return new ScanSatResetDataMessage
        {
            Body = "Minmus",
            Type = 2
        };
    }

    protected override void AssertEqual(ScanSatResetDataMessage expected, ScanSatResetDataMessage actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.Type, actual.Type);
    }

    #endregion
}

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

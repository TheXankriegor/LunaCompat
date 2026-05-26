using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

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

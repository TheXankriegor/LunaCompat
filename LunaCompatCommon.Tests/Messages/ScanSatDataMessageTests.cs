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

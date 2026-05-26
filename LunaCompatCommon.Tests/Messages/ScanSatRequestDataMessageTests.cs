using LunaCompatCommon.Messages.ModMessages;

namespace LunaCompatCommon.Tests.Messages;

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

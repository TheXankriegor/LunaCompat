using LunaCompatCommon.Messages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class SegmentedMessageTests : ModMessageTestBase<SegmentedMessage>
{
    #region Non-Public Methods

    protected override SegmentedMessage CreateMessage()
    {
        return new SegmentedMessage
        {
            OriginalType = "TestType",
            PartCount = 3,
            MessageId = 42,
            PartId = 1,
            PartData = new byte[]
            {
                1, 2, 3, 4
            }
        };
    }

    protected override void AssertEqual(SegmentedMessage expected, SegmentedMessage actual)
    {
        Assert.Equal(expected.OriginalType, actual.OriginalType);
        Assert.Equal(expected.PartCount, actual.PartCount);
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.PartId, actual.PartId);
        Assert.Equal(expected.PartData, actual.PartData);
    }

    #endregion
}

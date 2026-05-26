using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsDeleteGroupCenterMessageTests : ModMessageTestBase<KerbalKonstructsDeleteGroupCenterMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteGroupCenterMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteGroupCenterMessage
        {
            Identifier = "group-1"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteGroupCenterMessage expected, KerbalKonstructsDeleteGroupCenterMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

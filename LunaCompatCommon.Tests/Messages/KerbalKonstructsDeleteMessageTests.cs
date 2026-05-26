using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsDeleteMessageTests : ModMessageTestBase<KerbalKonstructsDeleteMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteMessage
        {
            Identifier = "id-123"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteMessage expected, KerbalKonstructsDeleteMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

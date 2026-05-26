using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsDeleteMapDecalMessageTests : ModMessageTestBase<KerbalKonstructsDeleteMapDecalMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteMapDecalMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteMapDecalMessage
        {
            Identifier = "dec-1"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteMapDecalMessage expected, KerbalKonstructsDeleteMapDecalMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

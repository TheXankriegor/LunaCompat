using LunaCompatCommon.Messages.ModMessages;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalColoniesRequestColoniesMessageTests : ModMessageTestBase<KerbalColoniesRequestColoniesMessage>
{
    #region Non-Public Methods

    protected override KerbalColoniesRequestColoniesMessage CreateMessage()
    {
        return new KerbalColoniesRequestColoniesMessage();
    }

    protected override void AssertEqual(KerbalColoniesRequestColoniesMessage expected, KerbalColoniesRequestColoniesMessage actual)
    {
        // No properties to assert
    }

    #endregion
}

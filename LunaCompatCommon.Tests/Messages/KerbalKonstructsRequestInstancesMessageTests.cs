using LunaCompatCommon.Messages.ModMessages;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsRequestInstancesMessageTests : ModMessageTestBase<KerbalKonstructsRequestInstancesMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsRequestInstancesMessage CreateMessage()
    {
        return new KerbalKonstructsRequestInstancesMessage();
    }

    protected override void AssertEqual(KerbalKonstructsRequestInstancesMessage expected, KerbalKonstructsRequestInstancesMessage actual)
    {
        // No properties to assert
    }

    #endregion
}

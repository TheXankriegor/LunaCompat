using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsDeleteStaticInstanceMessageTests : ModMessageTestBase<KerbalKonstructsDeleteStaticInstanceMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteStaticInstanceMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteStaticInstanceMessage
        {
            Identifier = "id-static",
            ModelName = "Model"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteStaticInstanceMessage expected, KerbalKonstructsDeleteStaticInstanceMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
        Assert.Equal(expected.ModelName, actual.ModelName);
    }

    #endregion
}

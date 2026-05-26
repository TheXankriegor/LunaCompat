using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsChangeStaticInstanceMessageTests : ModMessageTestBase<KerbalKonstructsChangeStaticInstanceMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsChangeStaticInstanceMessage CreateMessage()
    {
        return new KerbalKonstructsChangeStaticInstanceMessage
        {
            Content = "{}",
            Name = "StaticInstance"
        };
    }

    protected override void AssertEqual(KerbalKonstructsChangeStaticInstanceMessage expected, KerbalKonstructsChangeStaticInstanceMessage actual)
    {
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.Name, actual.Name);
    }

    #endregion
}

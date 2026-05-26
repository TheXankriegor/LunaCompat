using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsChangeMapDecalMessageTests : ModMessageTestBase<KerbalKonstructsChangeMapDecalMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsChangeMapDecalMessage CreateMessage()
    {
        return new KerbalKonstructsChangeMapDecalMessage
        {
            Content = "{}",
            Name = "Decal"
        };
    }

    protected override void AssertEqual(KerbalKonstructsChangeMapDecalMessage expected, KerbalKonstructsChangeMapDecalMessage actual)
    {
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.Name, actual.Name);
    }

    #endregion
}

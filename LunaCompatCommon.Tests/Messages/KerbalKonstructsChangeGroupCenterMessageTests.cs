using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsChangeGroupCenterMessageTests : ModMessageTestBase<KerbalKonstructsChangeGroupCenterMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsChangeGroupCenterMessage CreateMessage()
    {
        return new KerbalKonstructsChangeGroupCenterMessage
        {
            Content = "{}",
            Name = "Group",
            Uuid = "uuid-1"
        };
    }

    protected override void AssertEqual(KerbalKonstructsChangeGroupCenterMessage expected, KerbalKonstructsChangeGroupCenterMessage actual)
    {
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Uuid, actual.Uuid);
    }

    #endregion
}

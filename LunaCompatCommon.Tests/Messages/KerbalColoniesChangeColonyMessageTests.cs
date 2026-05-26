using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalColoniesChangeColonyMessageTests : ModMessageTestBase<KerbalColoniesChangeColonyMessage>
{
    #region Non-Public Methods

    protected override KerbalColoniesChangeColonyMessage CreateMessage()
    {
        return new KerbalColoniesChangeColonyMessage
        {
            Body = "Mun",
            Content = "{}",
            ColonyName = "MyColony"
        };
    }

    protected override void AssertEqual(KerbalColoniesChangeColonyMessage expected, KerbalColoniesChangeColonyMessage actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.ColonyName, actual.ColonyName);
    }

    #endregion
}

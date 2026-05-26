using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsChangeMessageTests : ModMessageTestBase<KerbalKonstructsChangeMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsChangeMessage CreateMessage()
    {
        return new KerbalKonstructsChangeMessage
        {
            Content = "{}",
            Name = "Facility"
        };
    }

    protected override void AssertEqual(KerbalKonstructsChangeMessage expected, KerbalKonstructsChangeMessage actual)
    {
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.Name, actual.Name);
    }

    #endregion
}

using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsSaveFacilitiesMessageTests : ModMessageTestBase<KerbalKonstructsSaveFacilitiesMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsSaveFacilitiesMessage CreateMessage()
    {
        return new KerbalKonstructsSaveFacilitiesMessage
        {
            Facilities = "[]",
            LaunchSites = "[]"
        };
    }

    protected override void AssertEqual(KerbalKonstructsSaveFacilitiesMessage expected, KerbalKonstructsSaveFacilitiesMessage actual)
    {
        Assert.Equal(expected.Facilities, actual.Facilities);
        Assert.Equal(expected.LaunchSites, actual.LaunchSites);
    }

    #endregion
}

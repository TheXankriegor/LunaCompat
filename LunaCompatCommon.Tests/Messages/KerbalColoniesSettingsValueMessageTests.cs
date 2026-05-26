using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalColoniesSettingsValueMessageTests : ModMessageTestBase<KerbalColoniesSettingsValueMessage>
{
    #region Non-Public Methods

    protected override KerbalColoniesSettingsValueMessage CreateMessage()
    {
        return new KerbalColoniesSettingsValueMessage
        {
            Key = "FacilityCostMultiplier",
            Value = "1.5"
        };
    }

    protected override void AssertEqual(KerbalColoniesSettingsValueMessage expected, KerbalColoniesSettingsValueMessage actual)
    {
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Value, actual.Value);
    }

    #endregion
}

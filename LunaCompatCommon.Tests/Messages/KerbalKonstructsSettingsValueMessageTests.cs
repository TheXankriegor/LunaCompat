using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class KerbalKonstructsSettingsValueMessageTests : ModMessageTestBase<KerbalKonstructsSettingsValueMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsSettingsValueMessage CreateMessage()
    {
        return new KerbalKonstructsSettingsValueMessage
        {
            Key = "DisableRemoteRecovery",
            Value = "true"
        };
    }

    protected override void AssertEqual(KerbalKonstructsSettingsValueMessage expected, KerbalKonstructsSettingsValueMessage actual)
    {
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Value, actual.Value);
    }

    #endregion
}

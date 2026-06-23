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

public class KerbalColoniesRequestColoniesMessageTests : ModMessageTestBase<KerbalColoniesRequestColoniesMessage>
{
    #region Non-Public Methods

    protected override KerbalColoniesRequestColoniesMessage CreateMessage()
    {
        return new KerbalColoniesRequestColoniesMessage();
    }

    protected override void AssertEqual(KerbalColoniesRequestColoniesMessage expected, KerbalColoniesRequestColoniesMessage actual)
    {
        // No properties to assert
    }

    #endregion
}

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

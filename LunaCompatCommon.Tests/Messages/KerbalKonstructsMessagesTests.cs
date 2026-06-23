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

public class KerbalKonstructsDeleteMessageTests : ModMessageTestBase<KerbalKonstructsDeleteMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteMessage
        {
            Identifier = "id-123"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteMessage expected, KerbalKonstructsDeleteMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

public class KerbalKonstructsDeleteGroupCenterMessageTests : ModMessageTestBase<KerbalKonstructsDeleteGroupCenterMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteGroupCenterMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteGroupCenterMessage
        {
            Identifier = "group-1"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteGroupCenterMessage expected, KerbalKonstructsDeleteGroupCenterMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

public class KerbalKonstructsDeleteMapDecalMessageTests : ModMessageTestBase<KerbalKonstructsDeleteMapDecalMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsDeleteMapDecalMessage CreateMessage()
    {
        return new KerbalKonstructsDeleteMapDecalMessage
        {
            Identifier = "dec-1"
        };
    }

    protected override void AssertEqual(KerbalKonstructsDeleteMapDecalMessage expected, KerbalKonstructsDeleteMapDecalMessage actual)
    {
        Assert.Equal(expected.Identifier, actual.Identifier);
    }

    #endregion
}

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

public class KerbalKonstructsRequestInstancesMessageTests : ModMessageTestBase<KerbalKonstructsRequestInstancesMessage>
{
    #region Non-Public Methods

    protected override KerbalKonstructsRequestInstancesMessage CreateMessage()
    {
        return new KerbalKonstructsRequestInstancesMessage();
    }

    protected override void AssertEqual(KerbalKonstructsRequestInstancesMessage expected, KerbalKonstructsRequestInstancesMessage actual)
    {
        // No properties to assert
    }

    #endregion
}

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

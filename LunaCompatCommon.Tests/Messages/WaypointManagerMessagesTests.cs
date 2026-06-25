using System;

using LunaCompatCommon.Messages.ModMessages;

using Xunit;

namespace LunaCompatCommon.Tests.Messages;

public class WpManagerRequestWaypointsMessageTests : ModMessageTestBase<WaypointManagerRequestMessage>
{
    #region Non-Public Methods

    protected override WaypointManagerRequestMessage CreateMessage()
    {
        return new WaypointManagerRequestMessage();
    }

    protected override void AssertEqual(WaypointManagerRequestMessage expected, WaypointManagerRequestMessage actual)
    {
        Assert.True(true);
    }

    #endregion
}

public class WpManagerSyncWaypointsMessageTests : ModMessageTestBase<WaypointManagerChangeMessage>
{
    #region Non-Public Methods

    protected override WaypointManagerChangeMessage CreateMessage()
    {
        return new WaypointManagerChangeMessage
        {
            ConfigNodeString = "WAYPOINT\n{name}test_waypoint\n}\n",
            NavigationId = Guid.NewGuid()
        };
    }

    protected override void AssertEqual(WaypointManagerChangeMessage expected, WaypointManagerChangeMessage actual)
    {
        Assert.Equal(expected.ConfigNodeString, actual.ConfigNodeString);
        Assert.Equal(expected.NavigationId, actual.NavigationId);
    }

    #endregion
}

public class WpManagerRemoveWaypointMessageTests : ModMessageTestBase<WaypointManagerDeleteMessage>
{
    #region Non-Public Methods

    protected override WaypointManagerDeleteMessage CreateMessage()
    {
        return new WaypointManagerDeleteMessage
        {
            NavigationId = Guid.NewGuid()
        };
    }

    protected override void AssertEqual(WaypointManagerDeleteMessage expected, WaypointManagerDeleteMessage actual)
    {
        Assert.Equal(expected.NavigationId, actual.NavigationId);
    }

    #endregion
}

// ReSharper disable once RedundantUsingDirective

using System;

namespace LunaCompatCommon.Messages.ModMessages;

public class WaypointManagerRequestMessage : IModMessage
{
}

public class WaypointManagerChangeMessage : WaypointManagerBaseMessage
{
    #region Properties

    public string ConfigNodeString { get; set; }

    #endregion
}

public class WaypointManagerDeleteMessage : WaypointManagerBaseMessage
{
}

public abstract class WaypointManagerBaseMessage : IModMessage
{
    #region Properties

    public Guid NavigationId { get; set; }

    #endregion
}

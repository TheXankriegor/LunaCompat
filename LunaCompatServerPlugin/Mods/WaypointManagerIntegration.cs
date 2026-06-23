using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using Server.Client;
using Server.System;

using ILogger = LunaCompatCommon.Utils.ILogger;

namespace LunaCompatServerPlugin.Mods;

internal class WaypointManagerIntegration : ServerModIntegration
{
    #region Fields

    private readonly string _basePath;
    private readonly Dictionary<Guid, string> _waypoints;

    #endregion

    #region Constructors

    public WaypointManagerIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider, messageHandler)
    {
        _basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "WaypointManager");
        _waypoints = new Dictionary<Guid, string>();
    }

    #endregion

    #region Properties

    public override string PackageName => "WaypointManager";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<WaypointManagerRequestMessage>(OnRequestWaypointsMessageReceived);
        _messageHandler.RegisterModMessageListener<WaypointManagerChangeMessage>(OnChangeWaypointsMessageReceived);
        _messageHandler.RegisterModMessageListener<WaypointManagerDeleteMessage>(OnDeleteWaypointMessageReceived);

        if (!FileHandler.FolderExists(_basePath))
            FileHandler.FolderCreate(_basePath);

        var waypointFiles = Directory.GetFiles(_basePath, "*.cfg", SearchOption.AllDirectories);

        foreach (var waypointFile in waypointFiles)
        {
            try
            {
                var navId = Guid.Parse(Path.GetFileNameWithoutExtension(waypointFile));
                _waypoints[navId] = FileHandler.ReadFileText(waypointFile);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load waypoint file '{waypointFile}': {ex}", PackageName);
            }
        }
    }

    public override void Destroy()
    {
        _messageHandler.UnregisterModMessageListener<WaypointManagerRequestMessage>();
        _messageHandler.UnregisterModMessageListener<WaypointManagerChangeMessage>();
        _messageHandler.UnregisterModMessageListener<WaypointManagerDeleteMessage>();

        _waypoints.Clear();

        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private void OnRequestWaypointsMessageReceived(ClientStructure client, WaypointManagerRequestMessage msg)
    {
        try
        {
            _logger.Info($"Sending {_waypoints.Count} waypoints to {client.PlayerName}", PackageName);

            // Send each waypoint as its own message
            foreach (var waypointEntry in _waypoints)
            {
                _messageHandler.SendCompatMessage(client, new WaypointManagerChangeMessage
                {
                    ConfigNodeString = waypointEntry.Value,
                    NavigationId = waypointEntry.Key
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnChangeWaypointsMessageReceived(ClientStructure client, WaypointManagerChangeMessage msg)
    {
        try
        {
            _logger.Info($"Received waypoint update from {client.PlayerName} (ID: {msg.NavigationId})", PackageName);

            var targetPath = Path.Combine(_basePath, $"{msg.NavigationId}.cfg");
            FileHandler.WriteToFile(targetPath, msg.ConfigNodeString);

            _waypoints[msg.NavigationId] = msg.ConfigNodeString;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnDeleteWaypointMessageReceived(ClientStructure client, WaypointManagerDeleteMessage msg)
    {
        try
        {
            _logger.Info($"Received waypoint removal from {client.PlayerName} (ID: {msg.NavigationId})", PackageName);

            _waypoints.Remove(msg.NavigationId);

            var targetPath = Path.Combine(_basePath, $"{msg.NavigationId}.cfg");
            if (FileHandler.FileExists(targetPath))
                FileHandler.FileDelete(targetPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    #endregion
}

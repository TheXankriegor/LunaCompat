using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods;

internal class KerbalColoniesIntegration : ServerModIntegration
{
    #region Fields

    private readonly string _basePath;

    #endregion

    #region Constructors

    public KerbalColoniesIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider, messageHandler)
    {
        _basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalColonies");
    }

    #endregion

    #region Properties

    public override string PackageName => "KerbalColonies";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<KerbalColoniesRequestColoniesMessage>(OnRequestColoniesMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalColoniesChangeColonyMessage>(OnChangeColonyMessageReceived);

        if (!FileHandler.FolderExists(_basePath))
            FileHandler.FolderCreate(_basePath);
    }

    public override void Destroy()
    {
        _messageHandler.UnregisterModMessageListener<KerbalColoniesRequestColoniesMessage>();
        _messageHandler.UnregisterModMessageListener<KerbalColoniesChangeColonyMessage>();

        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private void SendServerSettings(ClientStructure client)
    {
        _logger.Debug($"Sending KerbalColonies server settings to {client.PlayerName}", PackageName);

        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.FacilityCostMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.FacilityTimeMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.FacilityRangeMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.EditorRangeMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.VesselCostMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.VesselTimeMultiplier, 1f);
        SendSettingsValue<KerbalColoniesSettingsValueMessage>(client, KerbalColoniesConstants.MaxColoniesPerBody, 10);
    }

    private void OnRequestColoniesMessageReceived(ClientStructure client, KerbalColoniesRequestColoniesMessage msg)
    {
        SendServerSettings(client);
        SendAllColonies(client);

        Task.Run(async () =>
        {
            // Ensure all messages have been sent. At worst, this message arrived before others and allows editing too early (unlikely during join). 
            await Task.Delay(1000);

            _messageHandler.SendCompatMessage(client, new KerbalColoniesRequestColoniesMessage());
        });
    }

    private void SendAllColonies(ClientStructure client)
    {
        var colonies = Directory.GetFiles(_basePath, "*.cfg", SearchOption.AllDirectories);

        foreach (var group in colonies)
        {
            var relativePath = group.Substring(_basePath.Length + 1);

            var parts = relativePath.Split(new[]
            {
                '/', '\\'
            }, StringSplitOptions.TrimEntries);

            var body = parts[0];
            var colonyName = parts[1];

            _logger.Debug($"Sending {colonyName} ({body}) to {client.PlayerName}", PackageName);

            var baseMessage = new KerbalColoniesChangeColonyMessage
            {
                Body = body,
                ColonyName = Path.GetFileNameWithoutExtension(colonyName),
                Content = FileHandler.ReadFileText(group)
            };

            _messageHandler.SendCompatMessage(client, baseMessage);
        }
    }

    private void OnChangeColonyMessageReceived(ClientStructure client, KerbalColoniesChangeColonyMessage msg)
    {
        try
        {
            _logger.Debug($"Received colony update for {msg.ColonyName} ({msg.Body}) from {client.PlayerName}", PackageName);

            var targetPath = Path.Combine(_basePath, msg.Body);

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            var groupPath = Path.Combine(targetPath, $"{msg.ColonyName}.cfg");
            FileHandler.WriteToFile(groupPath, msg.Content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    #endregion
}

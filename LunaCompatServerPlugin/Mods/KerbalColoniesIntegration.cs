using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;
using Server.System;
using System;
using System.Collections.Generic;
using System.Text;

using Server.Client;

namespace LunaCompatServerPlugin.Mods;

internal class KerbalColoniesIntegration : ServerModIntegration
{
    private readonly string _basePath;

    public KerbalColoniesIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider, messageHandler)
    {
        _basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalColonies");
    }

    public override string PackageName => "KerbalColonies";

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<KerbalColoniesRequestColoniesMessage>(OnRequestColoniesMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalColoniesChangeColonyMessage>(OnChangeColonyMessageReceived);

        if (!FileHandler.FolderExists(_basePath))
            FileHandler.FolderCreate(_basePath);
    }

    private void OnRequestColoniesMessageReceived(ClientStructure client, KerbalColoniesRequestColoniesMessage msg)
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

            _logger.Debug($"Sending {colonyName} ({body})", PackageName);

            var baseMessage = new KerbalColoniesChangeColonyMessage
            {
                Body = body,
                ColonyName = Path.GetFileNameWithoutExtension(colonyName),
                Content = FileHandler.ReadFileText(group)
            }; 
            
            _messageHandler.SendCompatMessage(client, baseMessage);
        }

        _messageHandler.SendCompatMessage(client, new KerbalColoniesRequestColoniesMessage());
    }

    private void OnChangeColonyMessageReceived(ClientStructure client, KerbalColoniesChangeColonyMessage msg)
    {
        try
        {
            _logger.Debug($"Received colony update for {msg.ColonyName} ({msg.Body})", PackageName);

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
}
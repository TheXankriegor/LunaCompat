using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.Utils;

using LunaConfigNode.CfgNode;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods;

internal class KerbalKonstructsIntegration : ServerModIntegration
{
    #region Fields

    private readonly string _baseInstancePath;
    private readonly string _baseGroupsPath;

    #endregion

    #region Constructors

    public KerbalKonstructsIntegration(ILogger logger, ServerMessageHandler messageHandler)
        : base(logger, messageHandler)
    {
        _baseInstancePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "NewInstances");
        _baseGroupsPath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "Groups");
    }

    #endregion

    #region Properties

    public override string PackageName => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<KerbalKonstructsRequestInstancesMessage>(OnRequestInstancesMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>(OnChangeGroupCenterMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>(OnDeleteGroupCenterMessageReceived);

        if (!FileHandler.FolderExists(_baseInstancePath))
            FileHandler.FolderCreate(_baseInstancePath);
        if (!FileHandler.FolderExists(_baseGroupsPath))
            FileHandler.FolderCreate(_baseGroupsPath);
    }

    #endregion

    #region Non-Public Methods

    private void OnChangeGroupCenterMessageReceived(ClientStructure client, KerbalKonstructsChangeGroupCenterMessage msg)
    {
        try
        {
            _logger.Debug($"Received group center update for {msg.ModelName} ({msg.Uuid})", PackageName);

            var uniquePath = Path.Combine(_baseGroupsPath, msg.Uuid);

            if (!Directory.Exists(uniquePath))
                Directory.CreateDirectory(uniquePath);

            var groupPath = Path.Combine(uniquePath, $"{msg.ModelName}.cfg");

            // for groups, we can just replace the whole thing
            FileHandler.WriteToFile(groupPath, msg.Content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void OnChangeStaticInstanceMessageReceived(ClientStructure client, KerbalKonstructsChangeStaticInstanceMessage msg)
    {
        try
        {
            _logger.Debug($"Received static instance update for {msg.ModelName}", PackageName);

            var instancePath = Path.Combine(_baseInstancePath, $"{msg.ModelName}.cfg");

            if (File.Exists(instancePath))
            {
                var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));

                var existingInstances = existing.GetNode("root")?.Value?.GetNode("STATIC")?.Value;

                if (existingInstances == null)
                {
                    _logger.Warning($"Received empty static instance definition from {client.PlayerName} for {msg.ModelName}: {msg.Content}", PackageName);
                    return;
                }

                var eN = existingInstances.GetNodes("Instances");
                var update = new ConfigNode(msg.Content);
                var uN = update.GetNode("root").Value.GetNode("STATIC").Value.GetNodes("Instances");

                foreach (var newInstance in uN)
                {
                    var match = eN.FirstOrDefault(x => x.Value.GetValue("UUID") == newInstance.Value.GetValue("UUID"));

                    if (match != null)
                        existingInstances.ReplaceNode(match.Value, newInstance.Value);
                    else
                        existingInstances.AddNode(newInstance.Value);
                }

                FileHandler.WriteToFile(instancePath, existing.ToString());
            }
            else
                FileHandler.WriteToFile(instancePath, msg.Content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void OnDeleteGroupCenterMessageReceived(ClientStructure client, KerbalKonstructsDeleteGroupCenterMessage msg)
    {
        try
        {
            _logger.Debug($"Received group delete for {msg.Uuid}", PackageName);

            var groupPath = Path.Combine(_baseGroupsPath, msg.Uuid);

            if (!Directory.Exists(groupPath))
            {
                _logger.Warning($"Trying to delete group center which does not exist on the server ({msg.Uuid}).", PackageName);
                return;
            }

            Directory.Delete(groupPath, true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void OnDeleteStaticInstanceMessageReceived(ClientStructure client, KerbalKonstructsDeleteStaticInstanceMessage msg)
    {
        try
        {
            _logger.Debug($"Received static instance delete for {msg.ModelName} ({msg.Uuid})", PackageName);

            var instancePath = Path.Combine(_baseInstancePath, $"{msg.ModelName}.cfg");

            if (!File.Exists(instancePath))
            {
                _logger.Warning($"Trying to delete static instance which does not exist on the server ({msg.ModelName}).", PackageName);
                return;
            }

            var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));
            var root = existing.GetNode("root");
            var statics = root.Value.GetNode("STATIC").Value;
            var instances = statics.GetNodes("Instances");

            foreach (var instance in instances)
            {
                if (instance.Value.GetValue("UUID").Value != msg.Uuid)
                    continue;

                // Workaround for RepeatedItems in the MixedCollection not deleting properly
                instance.Key = "RemovedInstance";
                statics.RemoveNode("RemovedInstance");
                break;
            }

            if (statics.GetNodes("Instances").Count <= 1)
                File.Delete(instancePath);
            else
                FileHandler.WriteToFile(instancePath, existing.ToString());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void OnRequestInstancesMessageReceived(ClientStructure client, KerbalKonstructsRequestInstancesMessage msg)
    {
        try
        {
            _logger.Info($"Sending all KK instances to {client.PlayerName}", PackageName);

            SendAllGroupCenters(client);
            SendAllStaticInstances(client);

            _messageHandler.SendCompatMessage(client, new KerbalKonstructsRequestInstancesMessage());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void SendAllStaticInstances(ClientStructure client)
    {
        var files = Directory.GetFiles(_baseInstancePath, "*.cfg", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                _logger.Debug($"Sending {file}", PackageName);

                var writtenFileName = file.Substring(_baseInstancePath.Length + 1);
                var baseMessage = new KerbalKonstructsChangeStaticInstanceMessage
                {
                    ModelName = Path.GetFileNameWithoutExtension(writtenFileName),
                    Content = FileHandler.ReadFileText(file)
                };
                _messageHandler.SendCompatMessage(client, baseMessage);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send {file}: {ex}", PackageName);
            }
        }
    }

    private void SendAllGroupCenters(ClientStructure client)
    {
        var groups = Directory.GetFiles(_baseGroupsPath, "*.cfg", SearchOption.AllDirectories);

        foreach (var group in groups)
        {
            try
            {
                var relativePath = group.Substring(_baseGroupsPath.Length + 1);

                var parts = relativePath.Split(new[]
                {
                    '/', '\\'
                }, StringSplitOptions.TrimEntries);

                var uuid = parts[0];
                var modelName = parts[1];

                _logger.Debug($"Sending {modelName} - {uuid}", PackageName);

                var baseMessage = new KerbalKonstructsChangeGroupCenterMessage
                {
                    Uuid = uuid,
                    ModelName = Path.GetFileNameWithoutExtension(modelName),
                    Content = FileHandler.ReadFileText(group)
                };
                _messageHandler.SendCompatMessage(client, baseMessage);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send {group}: {ex}", PackageName);
            }
        }
    }

    #endregion
}

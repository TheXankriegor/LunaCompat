using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

using LunaCompatServerPlugin.ModSettings;

using LunaConfigNode.CfgNode;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods;

internal class KerbalKonstructsIntegration : ServerModIntegration
{
    #region Fields

    private readonly string _baseInstancePath;
    private readonly string _baseGroupsPath;
    private readonly string _baseMapDecalsPath;
    private readonly string _basePath;

    #endregion

    #region Constructors

    public KerbalKonstructsIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider, messageHandler)
    {
        _basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs");
        _baseInstancePath = Path.Combine(_basePath, "NewInstances");
        _baseMapDecalsPath = Path.Combine(_basePath, "MapDecals");
        _baseGroupsPath = Path.Combine(_basePath, "Groups");
    }

    #endregion

    #region Properties

    public override string PackageName => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void InitializeSettings(ModSettingsProvider settingsProvider)
    {
        base.InitializeSettings(settingsProvider);

        settingsProvider.SetValue(PackageName, KerbalKonstructsConstants.EnableRT, false);
        settingsProvider.SetValue(PackageName, KerbalKonstructsConstants.EnableCommNet, false);
        settingsProvider.SetValue(PackageName, KerbalKonstructsConstants.DisableRemoteBaseOpening, false);
        settingsProvider.SetValue(PackageName, KerbalKonstructsConstants.FacilityUseRange, 300);
        settingsProvider.SetValue(PackageName, KerbalKonstructsConstants.DisableRemoteRecovery, false);
    }

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<KerbalKonstructsRequestInstancesMessage>(OnRequestInstancesMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>(OnChangeGroupCenterMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>(OnDeleteGroupCenterMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsChangeMapDecalMessage>(OnChangeMapDecalMessageReceived);
        _messageHandler.RegisterModMessageListener<KerbalKonstructsDeleteMapDecalMessage>(OnDeleteMapDecalMessageReceived);

        _messageHandler.RegisterModMessageListener<KerbalKonstructsSaveFacilitiesMessage>(OnSaveFacilitiesMessageReceived);

        if (!FileHandler.FolderExists(_baseInstancePath))
            FileHandler.FolderCreate(_baseInstancePath);
        if (!FileHandler.FolderExists(_baseGroupsPath))
            FileHandler.FolderCreate(_baseGroupsPath);
        if (!FileHandler.FolderExists(_baseMapDecalsPath))
            FileHandler.FolderCreate(_baseMapDecalsPath);
    }

    public override void Destroy()
    {
        _messageHandler.UnregisterModMessageListener<KerbalKonstructsRequestInstancesMessage>();

        _messageHandler.UnregisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>();
        _messageHandler.UnregisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>();

        _messageHandler.UnregisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>();
        _messageHandler.UnregisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>();

        _messageHandler.UnregisterModMessageListener<KerbalKonstructsChangeMapDecalMessage>();
        _messageHandler.UnregisterModMessageListener<KerbalKonstructsDeleteMapDecalMessage>();

        _messageHandler.UnregisterModMessageListener<KerbalKonstructsSaveFacilitiesMessage>();

        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private void SendScenarioData(ClientStructure client)
    {
        try
        {
            var facPath = Path.Combine(_basePath, "Facilities.cfg");
            var lsPath = Path.Combine(_basePath, "LaunchSite.cfg");
            var fac = string.Empty;
            if (File.Exists(facPath))
                fac = FileHandler.ReadFileText(facPath);
            var ls = string.Empty;
            if (File.Exists(lsPath))
                ls = FileHandler.ReadFileText(lsPath);

            var baseMessage = new KerbalKonstructsSaveFacilitiesMessage
            {
                LaunchSites = ls,
                Facilities = fac
            };
            _messageHandler.SendCompatMessage(client, baseMessage);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to send scenario data: {ex}", PackageName);
        }
    }

    private void OnSaveFacilitiesMessageReceived(ClientStructure client, KerbalKonstructsSaveFacilitiesMessage msg)
    {
        try
        {
            _logger.Debug($"Received facility update from {client.PlayerName}.", PackageName);

            var facPath = Path.Combine(_basePath, "Facilities.cfg");
            var lsPath = Path.Combine(_basePath, "LaunchSite.cfg");

            UpdateScenarioData(facPath, msg.Facilities);
            UpdateScenarioData(lsPath, msg.LaunchSites);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }

        void UpdateScenarioData(string existingPath, string content)
        {
            if (!File.Exists(existingPath))
            {
                FileHandler.WriteToFile(existingPath, content);
                return;
            }

            var existing = new ConfigNode(FileHandler.ReadFileText(existingPath));
            var eN = existing.GetAllNodes();
            var currentKeys = existing.Nodes.GetAllKeys();

            var update = new ConfigNode(content);

            foreach (var newInstance in update.GetAllNodes())
            {
                var match = eN.SingleOrDefault(x => x.GetValue("UUID") == newInstance.GetValue("UUID"));

                if (match != null)
                {
                    currentKeys.Remove(match.Name);
                    existing.ReplaceNode(match, newInstance);
                }
                else
                    existing.AddNode(newInstance);
            }

            // remove discarded
            foreach (var remainingKey in currentKeys)
                existing.RemoveNode(remainingKey);

            FileHandler.WriteToFile(existingPath, existing.ToString());
        }
    }

    private void OnChangeGroupCenterMessageReceived(ClientStructure client, KerbalKonstructsChangeGroupCenterMessage msg)
    {
        try
        {
            _logger.Debug($"Received group center update for {msg.Name} ({msg.Uuid})", PackageName);

            var uniquePath = Path.Combine(_baseGroupsPath, msg.Uuid);

            if (!Directory.Exists(uniquePath))
                Directory.CreateDirectory(uniquePath);

            var groupPath = Path.Combine(uniquePath, $"{msg.Name}.cfg");

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
            _logger.Debug($"Received static instance update for {msg.Name}", PackageName);

            var instancePath = Path.Combine(_baseInstancePath, $"{msg.Name}.cfg");

            if (File.Exists(instancePath))
            {
                var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));

                var existingInstances = existing.GetNode("root")?.Value?.GetNode("STATIC")?.Value;

                if (existingInstances == null)
                {
                    _logger.Warning($"Existing static instance definition for {msg.Name} is empty. Overwriting.", PackageName);
                    FileHandler.WriteToFile(instancePath, msg.Content);
                    return;
                }

                var eN = existingInstances.GetNodes("Instances");
                var update = new ConfigNode(msg.Content);
                var uN = update.GetNode("root")?.Value?.GetNode("STATIC")?.Value?.GetNodes("Instances");

                if (uN == null)
                {
                    _logger.Warning($"Received empty static instance definition from {client.PlayerName} for {msg.Name}: {msg.Content}", PackageName);
                    return;
                }

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

    private void OnChangeMapDecalMessageReceived(ClientStructure client, KerbalKonstructsChangeMapDecalMessage msg)
    {
        try
        {
            _logger.Debug($"Received map decal update for {msg.Name}", PackageName);

            var decalPath = Path.Combine(_baseMapDecalsPath, $"{msg.Name}.cfg");
            FileHandler.WriteToFile(decalPath, msg.Content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void OnDeleteMapDecalMessageReceived(ClientStructure client, KerbalKonstructsDeleteMapDecalMessage msg)
    {
        try
        {
            _logger.Debug($"Received map decal delete for {msg.Identifier}", PackageName);

            var decalPath = Path.Combine(_baseMapDecalsPath, $"{msg.Identifier}.cfg");

            if (!File.Exists(decalPath))
            {
                _logger.Warning($"Trying to delete map decal which does not exist on the server ({msg.Identifier}).", PackageName);
                return;
            }

            File.Delete(decalPath);
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
            _logger.Debug($"Received group delete for {msg.Identifier}", PackageName);

            var groupPath = Path.Combine(_baseGroupsPath, msg.Identifier);

            if (!Directory.Exists(groupPath))
            {
                _logger.Warning($"Trying to delete group center which does not exist on the server ({msg.Identifier}).", PackageName);
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
            _logger.Debug($"Received static instance delete for {msg.ModelName} ({msg.Identifier})", PackageName);

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
                if (instance.Value.GetValue("UUID").Value != msg.Identifier)
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

            SendServerSettings(client);
            SendAllGroupCenters(client);
            SendAllStaticInstances(client);
            SendAllMapDecals(client);
            SendScenarioData(client);

            Task.Run(async () =>
            {
                // Ensure all messages have been sent. At worst, this message arrived before others and allows editing too early (unlikely during join). 
                await Task.Delay(1000);

                _messageHandler.SendCompatMessage(client, new KerbalKonstructsRequestInstancesMessage());
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString(), PackageName);
        }
    }

    private void SendServerSettings(ClientStructure client)
    {
        _logger.Debug($"Sending KerbalKonstructs server settings to {client.PlayerName}...", PackageName);

        SendSettingsValue<KerbalKonstructsSettingsValueMessage>(client, KerbalKonstructsConstants.EnableRT, false);
        SendSettingsValue<KerbalKonstructsSettingsValueMessage>(client, KerbalKonstructsConstants.EnableCommNet, false);
        SendSettingsValue<KerbalKonstructsSettingsValueMessage>(client, KerbalKonstructsConstants.DisableRemoteBaseOpening, false);
        SendSettingsValue<KerbalKonstructsSettingsValueMessage>(client, KerbalKonstructsConstants.FacilityUseRange, 300);
        SendSettingsValue<KerbalKonstructsSettingsValueMessage>(client, KerbalKonstructsConstants.DisableRemoteRecovery, false);
    }

    private void SendAllMapDecals(ClientStructure client)
    {
        var files = Directory.GetFiles(_baseMapDecalsPath, "*.cfg", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                _logger.Debug($"Sending {file}", PackageName);

                var writtenFileName = file.Substring(_baseMapDecalsPath.Length + 1);
                var baseMessage = new KerbalKonstructsChangeMapDecalMessage
                {
                    Name = Path.GetFileNameWithoutExtension(writtenFileName),
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
                    Name = Path.GetFileNameWithoutExtension(writtenFileName),
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
                    Name = Path.GetFileNameWithoutExtension(modelName),
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

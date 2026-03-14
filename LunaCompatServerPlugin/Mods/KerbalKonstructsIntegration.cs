using LmpCommon.Message.Data;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;

using LunaCompatServerPlugin.Utils;

using LunaConfigNode.CfgNode;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods;

internal class KerbalKonstructsIntegration : ServersideModIntegration
{
    #region Fields

    private ServerModMessageHandler _messageHandler;
    private string _baseInstancePath;
    private string _baseGroupsPath;

    #endregion

    #region Properties

    public override string ModPrefix => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void Setup(ServerModMessageHandler messageHandler)
    {
        _messageHandler = messageHandler;

        _messageHandler.OnCompatMessageReceived += OnCompatMessageReceived;

        _baseInstancePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "NewInstances");
        if (!FileHandler.FolderExists(_baseInstancePath))
            FileHandler.FolderCreate(_baseInstancePath);
        _baseGroupsPath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "KerbalKonstructs", "Groups");
        if (!FileHandler.FolderExists(_baseGroupsPath))
            FileHandler.FolderCreate(_baseGroupsPath);
    }

    #endregion

    #region Non-Public Methods

    private void OnCompatMessageReceived(object sender, (ClientStructure Client, ModMsgData Data) e)
    {
        if (SerializationUtil.IsMessageOfType<KerbalKonstructsRequestInstancesMessage>(e.Data.ModName))
            Task.Run(() => SendAllInstances(e));

        if (SerializationUtil.IsMessageOfType<KerbalKonstructsDeleteStaticInstanceMessage>(e.Data.ModName))
            Task.Run(() => DeleteStaticInstance(e));
        if (SerializationUtil.IsMessageOfType<KerbalKonstructsDeleteGroupCenterMessage>(e.Data.ModName))
            Task.Run(() => DeleteGroupCenter(e));

        if (SerializationUtil.IsMessageOfType<KerbalKonstructsChangeStaticInstanceMessage>(e.Data.ModName))
            Task.Run(() => UpdateStaticInstance(e));

        if (SerializationUtil.IsMessageOfType<KerbalKonstructsChangeGroupCenterMessage>(e.Data.ModName))
            Task.Run(() => UpdateGroupCenter(e));
    }

    private void UpdateGroupCenter((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructsChangeGroupCenterMessage>(data.Data.Data);

            Log.Debug($"Received group center update for {message.ModelName} ({message.Uuid})", ModPrefix);

            var uniquePath = Path.Combine(_baseGroupsPath, message.Uuid);

            if (!Directory.Exists(uniquePath))
                Directory.CreateDirectory(uniquePath);

            var groupPath = Path.Combine(uniquePath, $"{message.ModelName}.cfg");

            // for groups, we can just replace the whole thing
            FileHandler.WriteToFile(groupPath, message.Content);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    private void UpdateStaticInstance((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructsChangeStaticInstanceMessage>(data.Data.Data);

            Log.Debug($"Received static instance update for {message.ModelName}", ModPrefix);

            var instancePath = Path.Combine(_baseInstancePath, $"{message.ModelName}.cfg");

            if (File.Exists(instancePath))
            {
                var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));

                var existingInstances = existing.GetNode("root")?.Value?.GetNode("STATIC")?.Value;

                if (existingInstances == null)
                {
                    Log.Warning($"Received empty static instance definition from {data.Client.PlayerName} for {message.ModelName}: {message.Content}",
                                ModPrefix);
                    return;
                }

                var eN = existingInstances.GetNodes("Instances");
                var update = new ConfigNode(message.Content);
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
                FileHandler.WriteToFile(instancePath, message.Content);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    private void DeleteGroupCenter((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructsDeleteGroupCenterMessage>(data.Data.Data);

            Log.Debug($"Received group delete for {message.Uuid}", ModPrefix);

            var groupPath = Path.Combine(_baseGroupsPath, message.Uuid);

            if (!Directory.Exists(groupPath))
            {
                Log.Warning($"Trying to delete group center which does not exist on the server ({message.Uuid}).", ModPrefix);
                return;
            }

            Directory.Delete(groupPath, true);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    private void DeleteStaticInstance((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructsDeleteStaticInstanceMessage>(data.Data.Data);

            Log.Debug($"Received static instance delete for {message.ModelName} ({message.Uuid})", ModPrefix);

            var instancePath = Path.Combine(_baseInstancePath, $"{message.ModelName}.cfg");

            if (!File.Exists(instancePath))
            {
                Log.Warning($"Trying to delete static instance which does not exist on the server ({message.ModelName}).", ModPrefix);
                return;
            }

            var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));
            var root = existing.GetNode("root");
            var statics = root.Value.GetNode("STATIC").Value;
            var instances = statics.GetNodes("Instances");

            foreach (var instance in instances)
            {
                if (instance.Value.GetValue("UUID").Value != message.Uuid)
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
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    private void SendAllInstances((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            Log.Info($"Sending all KK instances to {data.Client.PlayerName}", ModPrefix);

            SendAllGroupCenters(data.Client);
            SendAllStaticInstances(data.Client);

            _messageHandler.SendCompatMessage(data.Client, new KerbalKonstructsRequestInstancesMessage());
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    private void SendAllStaticInstances(ClientStructure client)
    {
        var files = Directory.GetFiles(_baseInstancePath, "*.cfg", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                Log.Debug($"Sending {file}", ModPrefix);

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
                Log.Error($"Failed to send {file}: {ex}", ModPrefix);
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

                Log.Debug($"Sending {modelName} - {uuid}", ModPrefix);

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
                Log.Error($"Failed to send {group}: {ex}", ModPrefix);
            }
        }
    }

    #endregion
}

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
    }

    #endregion

    #region Non-Public Methods

    private void OnCompatMessageReceived(object sender, (ClientStructure Client, ModMsgData Data) e)
    {
        if (SerializationUtil.IsMessageOfType<KerbalKonstructRequestInstancesMessage>(e.Data.ModName))
            Task.Run(() => SendAllInstances(e));

        if (SerializationUtil.IsMessageOfType<KerbalKonstructDeleteStaticInstanceMessage>(e.Data.ModName))
            Task.Run(() => DeleteStaticInstance(e));

        if (SerializationUtil.IsMessageOfType<KerbalKonstructChangeStaticInstanceMessage>(e.Data.ModName))
            Task.Run(() => UpdateStaticInstance(e));
    }

    private void UpdateStaticInstance((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructChangeStaticInstanceMessage>(data.Data.Data);

            Log.Debug($"Received static instance update for {message.ModelName}", ModPrefix);

            var instancePath = Path.Combine(_baseInstancePath, $"{message.ModelName}.cfg");

            if (File.Exists(instancePath))
            {
                var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));

                var existingInstances = existing.GetNode("root").Value.GetNode("STATIC").Value;
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

    private void DeleteStaticInstance((ClientStructure Client, ModMsgData Data) data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<KerbalKonstructDeleteStaticInstanceMessage>(data.Data.Data);

            Log.Debug($"Received static instance delete for {message.ModelName} ({message.Uuid})", ModPrefix);

            var instancePath = Path.Combine(_baseInstancePath, $"{message.ModelName}.cfg");

            if (!File.Exists(instancePath))
            {
                Log.Warning($"Trying to delete static instance which does not exist on the server ({message.ModelName}).", ModPrefix);
                return;
            }

            var existing = new ConfigNode(FileHandler.ReadFileText(instancePath));
            var statics = existing.GetNode("root").Value.GetNode("STATIC").Value;
            var instances = statics.GetNodes("Instances");

            foreach (var instance in instances)
            {
                if (instance.Value.GetValue("UUID").Value == message.Uuid)
                {
                    instances.Remove(instance);
                    break;
                }
            }

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
            Log.Debug($"Sending all static instances to {data.Client.PlayerName}", ModPrefix);

            var files = Directory.GetFiles(_baseInstancePath);

            foreach (var file in files)
            {
                try
                {
                    var baseMessage = new KerbalKonstructChangeStaticInstanceMessage
                    {
                        ModelName = Path.GetFileNameWithoutExtension(file),
                        Content = FileHandler.ReadFileText(file)
                    };
                    _messageHandler.SendCompatMessage(data.Client, baseMessage);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to send {file}: {ex}", ModPrefix);
                }
            }

            _messageHandler.SendCompatMessage(data.Client, new KerbalKonstructRequestInstancesMessage());
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString(), ModPrefix);
        }
    }

    #endregion
}

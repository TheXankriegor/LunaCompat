using System.Reflection;

using LmpCommon.Message;
using LmpCommon.Message.Client;
using LmpCommon.Message.Data;
using LmpCommon.Message.Interface;
using LmpCommon.Message.Server;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

using LunaCompatServerPlugin.Utils;

using Server.Client;
using Server.Server;

namespace LunaCompatServerPlugin;

internal class ServerModMessageHandler
{
    #region Fields

    private readonly ServerMessageFactory _messageFactory;

    #endregion

    #region Constructors

    public ServerModMessageHandler()
    {
        _messageFactory = new ServerMessageFactory();
    }

    #endregion

    #region Events

    public event EventHandler<(ClientStructure Client, ModMsgData Data)> OnCompatMessageReceived;

    #endregion

    #region Public Methods

    public void SendCompatMessageToAll<T>(T message)
        where T : class, new()
    {
        SendCompatMessageInternal(null, message, true);
    }

    public void SendCompatMessage<T>(ClientStructure client, T message)
        where T : class, new()
    {
        SendCompatMessageInternal(client, message, false);
    }

    public void CompatMessageReceived(ClientStructure client, ModCliMsg clientMessage)
    {
        if (clientMessage.Data is not ModMsgData msgData || !msgData.ModName.StartsWith(Constants.Prefix))
            return;

        Log.NetworkDebug($"Received {clientMessage} from {client.PlayerName}: {msgData.ModName}");

        // Initialization check
        if (SerializationUtil.IsMessageOfType<InitializeMessage>(msgData.ModName))
        {
            Task.Run(() => SendLunaCompatVersionInfo(client, msgData));
            return;
        }

        OnCompatMessageReceived?.Invoke(this, (client, msgData));
    }

    #endregion

    #region Non-Public Methods

    private static void SendCompatMessageToAll(IMessageData data)
    {
        MessageQueuer.SendToAllClients<ModSrvMsg>(data);
    }

    private static void SendCompatMessage(ClientStructure client, IMessageData data)
    {
        MessageQueuer.SendToClient<ModSrvMsg>(client, data);
    }

    private void SendCompatMessageInternal<T>(ClientStructure client, T message, bool allClients)
        where T : class, new()
    {
        try
        {
            var msg = CreateModMsgData(message);

            if (msg.NumBytes < Constants.MaxMessageSize)
            {
                if (allClients)
                    SendCompatMessageToAll(msg);
                else
                    SendCompatMessage(client, msg);

                return;
            }

            // segmentation required
            var msgId = msg.GetHashCode();
            var segments = msg.NumBytes / Constants.MaxMessageSize + 1;
            var ptr = 0;
            var segmentSize = msg.NumBytes / segments + 1;

            var originalType = SerializationUtil.CreatePrefixedModMessageId<T>();

            for (var i = 0; i < segments; i++)
            {
                var endPtr = ptr + segmentSize;
                if (endPtr >= msg.NumBytes)
                    endPtr = msg.NumBytes;

                var dstArray = new byte[endPtr - ptr];
                Array.Copy(msg.Data, ptr, dstArray, 0, endPtr - ptr);

                var segmentData = new SegmentedMessage
                {
                    MessageId = msgId,
                    PartCount = segments,
                    PartId = i,
                    OriginalType = originalType,
                    PartData = dstArray
                };

                ptr = endPtr;
                var newMsg = CreateModMsgData(segmentData);

                if (allClients)
                    SendCompatMessageToAll(newMsg);
                else
                    SendCompatMessage(client, newMsg);
            }
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private void SendLunaCompatVersionInfo(ClientStructure client, ModMsgData msgData)
    {
        Log.Info($"Initializing LMP compatibility for player {client.PlayerName}.");

        var initMessage = SerializationUtil.Deserialize<InitializeMessage>(msgData.Data);
        var serverPluginVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        if (initMessage.Version != serverPluginVersion)
            Log.Warning($"Client {client.PlayerName} is using a different version of LunaCompat ({initMessage.Version}, should be {serverPluginVersion}).");

        var msg = CreateModMsgData(new InitializeMessage
        {
            Version = serverPluginVersion
        });

        SendCompatMessage(client, msg);
    }

    private ModMsgData CreateModMsgData<T>(T message)
        where T : class, new()
    {
        var msg = _messageFactory.CreateNewMessageData<ModMsgData>();
        msg.ModName = SerializationUtil.CreatePrefixedModMessageId<T>();
        msg.Data = SerializationUtil.Serialize(message);
        msg.NumBytes = msg.Data.Length;

        return msg;
    }

    #endregion
}

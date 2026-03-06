using System.Reflection;

using LmpCommon.Message;
using LmpCommon.Message.Client;
using LmpCommon.Message.Data;
using LmpCommon.Message.Interface;
using LmpCommon.Message.Server;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using Server.Client;
using Server.Log;
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

    public event EventHandler<(ClientStructure Client, IServerMessageBase Message)> OnCompatMessageSent;

    public event EventHandler<ClientStructure> OnClientConnected;

    public event EventHandler<ClientStructure> OnClientAuthenticated;

    #endregion

    #region Public Methods

    public void SendCompatMessage(ClientStructure client, IMessageData data)
    {
        MessageQueuer.SendToClient<ModSrvMsg>(client, data);
    }

    public void SendCompatMessageToAll(ClientStructure client, IMessageData data)
    {
        MessageQueuer.SendToAllClients<ModSrvMsg>(data);
    }

    public void CompatMessageReceived(ClientStructure client, ModCliMsg clientMessage)
    {
        LunaLog.Info($"Received {clientMessage}: {((ModMsgData)clientMessage.Data).ModName}");

        if (clientMessage.Data is not ModMsgData msgData || !msgData.ModName.StartsWith(Constants.Prefix))
            return;

        // Initialization check
        if (SerializationUtil.IsMessageOfType<InitializeMessage>(msgData.ModName))
        {
            LunaLog.Info($"Initializing LMP compatibility for player {client.PlayerName}.");

            var initMessage = SerializationUtil.Deserialize<InitializeMessage>(msgData.Data);

            var serverPluginVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

            if (initMessage.Version != serverPluginVersion)
            {
                LunaLog.Warning(
                    $"Client {client.PlayerName} is using a different version of LunaCompat ({initMessage.Version}, should be {serverPluginVersion}).");
            }

            var msg = CreateModMsgData(new InitializeMessage
            {
                Version = serverPluginVersion
            });

            SendCompatMessage(client, msg);
            return;
        }

        OnCompatMessageReceived?.Invoke(this, (client, msgData));
    }

    public ModMsgData CreateModMsgData<T>(T message)
        where T : class, new()
    {
        var msg = _messageFactory.CreateNewMessageData<ModMsgData>();
        msg.ModName = SerializationUtil.CreatePrefixedModMessageId<T>();
        msg.Data = SerializationUtil.Serialize(message);
        msg.NumBytes = msg.Data.Length;
        return msg;
    }

    public void CompatMessageSent(ClientStructure client, IServerMessageBase messageData)
    {
        OnCompatMessageSent?.Invoke(this, (client, messageData));
    }

    public void ClientConnected(ClientStructure client)
    {
        OnClientConnected?.Invoke(this, client);
    }

    public void ClientAuthenticated(ClientStructure client)
    {
        OnClientAuthenticated?.Invoke(this, client);
    }

    #endregion
}

using System.Reflection;

using LmpCommon.Message;
using LmpCommon.Message.Client;
using LmpCommon.Message.Data;
using LmpCommon.Message.Interface;
using LmpCommon.Message.Server;

using LunaCompatShared.Utils;

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

    public event EventHandler<(ClientStructure, ModCliMsg)>? OnCompatMessageReceived;

    public event EventHandler<(ClientStructure, IServerMessageBase)>? OnCompatMessageSent;

    public event EventHandler<ClientStructure>? OnClientConnected;

    public event EventHandler<ClientStructure>? OnClientAuthenticated;

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
        if (clientMessage.Data is ModMsgData msgData && msgData.ModName.StartsWith("LMPC_"))
        {
            // Initialization check
            if (msgData.ModName.Equals("LMPC_Init"))
            {
                

                LunaLog.Info($"Initializing LMP compatibility for player {client.PlayerName}. ({Environment.Version})");
                var msg = _messageFactory.CreateNewMessageData<ModMsgData>();
                msg.ModName = msgData.ModName;
                msg.Data = BinaryUtils.Serialize(Assembly.GetExecutingAssembly().GetName().Version?.ToString());
                msg.NumBytes = msg.Data.Length;

                SendCompatMessage(client, msg);
                return;
            }

            OnCompatMessageReceived?.Invoke(this, (client, clientMessage));
        }
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

    #region Nested Types

    internal interface IModMessage;

    [Serializable]
    internal class LunaCompatInit : IModMessage
    {
        #region Properties

        public string Version { get; set; }

        #endregion
    }

    #endregion
}

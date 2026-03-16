using LmpCommon.Message;
using LmpCommon.Message.Client;
using LmpCommon.Message.Data;
using LmpCommon.Message.Server;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

using Server.Client;
using Server.Server;

namespace LunaCompatServerPlugin;

internal interface IServerMessageSender
{
    void SendCompatMessageToAll<TMessageType>(TMessageType message)
        where TMessageType : class, IModMessage, new();

    void SendCompatMessage<TMessageType>(ClientStructure client, TMessageType message)
        where TMessageType : class, IModMessage, new();
}

internal interface IServerMessageHandler : IMessageHandler<IServerMessageListener>, IServerMessageSender
{
    void HandleReceivedMessage(ClientStructure client, ModCliMsg clientMessage);

    void HandleReceivedMessage(ClientStructure client, string id, byte[] data);

    void RegisterModMessageListener<TMessageType>(Action<ClientStructure, TMessageType> messageHandler)
        where TMessageType : class, IModMessage, new();

    void UnregisterModMessageListener<TMessageType>()
        where TMessageType : class, IModMessage, new();
}

internal class ServerMessageHandler : MessageHandler<IServerMessageListener>, IServerMessageHandler
{
    #region Constructors

    public ServerMessageHandler(ILogger logger)
        : base(logger, new ServerMessageFactory())
    {
        _modMessageListeners.Add(SerializationUtil.CreatePrefixedModMessageId<SegmentedMessage>(), new ServerSegmentedMessageListener(logger, this));
    }

    #endregion

    #region Public Methods

    public void HandleReceivedMessage(ClientStructure client, ModCliMsg clientMessage)
    {
        if (clientMessage.Data is not ModMsgData msgData)
            return;

        _logger.NetworkDebug($"Received {clientMessage} from {client.PlayerName}: {msgData.ModName}");

        HandleReceivedMessage(client, msgData.ModName, msgData.Data);
    }

    public void HandleReceivedMessage(ClientStructure client, string id, byte[] data)
    {
        if (!id.StartsWith(Constants.Prefix) || !TryGetMessageListener(id, out var messageListener))
            return;

        Task.Run(() => messageListener.Execute(client, data));
    }

    public void RegisterModMessageListener<TMessageType>(Action<ClientStructure, TMessageType> messageHandler)
        where TMessageType : class, IModMessage, new()
    {
        _modMessageListeners.TryAdd(SerializationUtil.CreatePrefixedModMessageId<TMessageType>(),
                                    new ServerMessageListener<TMessageType>(_logger, messageHandler));
    }

    public void UnregisterModMessageListener<TMessageType>()
        where TMessageType : class, IModMessage, new()
    {
        _modMessageListeners.Remove(SerializationUtil.CreatePrefixedModMessageId<TMessageType>());
    }

    public void SendCompatMessageToAll<TMessageType>(TMessageType message)
        where TMessageType : class, IModMessage, new()
    {
        SendMessageInternal(message, MessageQueuer.SendToAllClients<ModSrvMsg>);
    }

    public void SendCompatMessage<TMessageType>(ClientStructure client, TMessageType message)
        where TMessageType : class, IModMessage, new()
    {
        SendMessageInternal(message, msg => MessageQueuer.SendToClient<ModSrvMsg>(client, msg));
    }

    #endregion
}

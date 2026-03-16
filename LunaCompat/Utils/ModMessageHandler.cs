using System;

using LmpClient.Base;
using LmpClient.Network;
using LmpClient.Systems.ModApi;

using LmpCommon.Message;
using LmpCommon.Message.Client;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

namespace LunaCompat.Utils;

internal interface IClientMessageListener : IMessageListener
{
    void Execute(byte[] data);
}

internal class ClientMessageListener<TMessageType> : MessageListener<TMessageType>, IClientMessageListener
    where TMessageType : class, IModMessage, new()
{
    #region Fields

    private readonly Action<TMessageType> _action;

    #endregion

    #region Constructors

    public ClientMessageListener(ILogger logger, Action<TMessageType> action)
        : base(logger)
    {
        _action = action;
    }

    #endregion

    #region Public Methods

    public void Execute(byte[] data)
    {
        try
        {
            if (!TryDeserializeMessage(data, out var message))
            {
                _logger.Error($"Failed to deserialize '{typeof(TMessageType).Name}' message ({data.Length} bytes).");
                return;
            }

            _action.Invoke(message);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    #endregion
}

internal class ClientSegmentedMessageListener : SegmentedMessageListener, IClientMessageListener
{
    #region Fields

    private readonly ClientMessageHandler _messageHandler;

    #endregion

    #region Constructors

    public ClientSegmentedMessageListener(ILogger logger, ClientMessageHandler messageHandler)
        : base(logger)
    {
        _messageHandler = messageHandler;
    }

    #endregion

    #region Public Methods

    public void Execute(byte[] data)
    {
        if (TryHandleSegment(data, out var id, out var combinedBytes))
            _messageHandler.HandleReceivedMessage(id, combinedBytes);
    }

    #endregion
}

internal interface IClientMessageSender
{
    void SendUnreliableMessage<TMessageType>(TMessageType message, bool relay = true)
        where TMessageType : class, IModMessage, new();

    void SendReliableMessage<TMessageType>(TMessageType message, bool relay = true)
        where TMessageType : class, IModMessage, new();
}

internal interface IClientMessageHandler : IMessageHandler<IClientMessageListener>, IClientMessageSender
{
    event EventHandler<bool> HasServerIntegrationChanged;

    bool HasServerIntegration { get; }

    void SetServerIntegrationDetermined(bool hasServerIntegration);

    void HandleReceivedMessage(string id, byte[] data);

    void RegisterModMessageListener<TMessageType>(Action<TMessageType> messageHandler)
        where TMessageType : class, IModMessage, new();

    void UnregisterModMessageListener<TMessageType>()
        where TMessageType : class, IModMessage, new();
}

internal class ClientMessageHandler : MessageHandler<IClientMessageListener>, IClientMessageHandler, IDisposable
{
    #region Fields

    private readonly EventData<string, byte[]> _onModMessageReceivedEvent;

    #endregion

    #region Constructors

    public ClientMessageHandler(ILogger logger)
        : base(logger, new ClientMessageFactory())
    {
        Instance = this;

        _modMessageListeners.Add(SerializationUtil.CreatePrefixedModMessageId<SegmentedMessage>(), new ClientSegmentedMessageListener(logger, this));

        _onModMessageReceivedEvent = GameEvents.FindEvent<EventData<string, byte[]>>("onModMessageReceived");
        _onModMessageReceivedEvent?.Add(HandleReceivedMessage);
    }

    #endregion

    #region Events

    public event EventHandler<bool> HasServerIntegrationChanged;

    #endregion

    #region Properties

    public static ClientMessageHandler Instance { get; private set; }

    public bool HasServerIntegration { get; private set; }

    #endregion

    #region Public Methods

    public void SetServerIntegrationDetermined(bool hasServerIntegration)
    {
        HasServerIntegration = hasServerIntegration;
        HasServerIntegrationChanged?.Invoke(this, hasServerIntegration);
    }

    public void HandleReceivedMessage(string id, byte[] data)
    {
        if (!id.StartsWith(Constants.Prefix) || !TryGetMessageListener(id, out var messageListener))
            return;

        messageListener.Execute(data);
    }

    public void RegisterModMessageListener<TMessageType>(Action<TMessageType> messageHandler)
        where TMessageType : class, IModMessage, new()
    {
        _modMessageListeners.TryAdd(SerializationUtil.CreatePrefixedModMessageId<TMessageType>(),
                                    new ClientMessageListener<TMessageType>(_logger, messageHandler));
    }

    public void UnregisterModMessageListener<TMessageType>()
        where TMessageType : class, IModMessage, new()
    {
        _modMessageListeners.Remove(SerializationUtil.CreatePrefixedModMessageId<TMessageType>());
    }

    public void SendUnreliableMessage<TMessageType>(TMessageType message, bool relay = true)
        where TMessageType : class, IModMessage, new()
    {
        SendMessageInternal(message, msgData =>
        {
            ModApiSystem.Singleton.SendModMessage(SerializationUtil.CreatePrefixedModMessageId<TMessageType>(), msgData.Data, relay);
        });
    }

    public void SendReliableMessage<TMessageType>(TMessageType message, bool relay = true)
        where TMessageType : class, IModMessage, new()
    {
        SendMessageInternal(message, msgData =>
        {
            msgData.Relay = relay;
            var msg = SystemBase.MessageFactory.CreateNew<ModCliMsg>(msgData);
            SystemBase.TaskFactory.StartNew(() => NetworkSender.QueueOutgoingMessage(msg));
        });
    }

    public void Dispose()
    {
        _onModMessageReceivedEvent?.Remove(HandleReceivedMessage);
    }

    #endregion
}

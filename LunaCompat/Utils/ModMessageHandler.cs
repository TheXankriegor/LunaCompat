using System;
using System.Collections.Generic;

using KSPBuildTools;

using LmpClient.Base;
using LmpClient.Network;
using LmpClient.Systems.ModApi;

using LmpCommon.Message.Client;
using LmpCommon.Message.Data;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;

namespace LunaCompat.Utils;

internal interface IMessageListener
{
    void Execute(byte[] data);
}

internal class ModMessageHandler
{
    #region Fields

    public static ModMessageHandler Instance;

    private readonly EventData<string, byte[]> _onModMessageReceivedEvent;
    private readonly Dictionary<string, IMessageListener> _modMessageListeners;
    private readonly SegmentedMessageHandler _segmentedMessageHandler;

    #endregion

    #region Constructors

    public ModMessageHandler()
    {
        Instance = this;
        _modMessageListeners = [];
        _segmentedMessageHandler = new SegmentedMessageHandler(_modMessageListeners);
        _onModMessageReceivedEvent = GameEvents.FindEvent<EventData<string, byte[]>>("onModMessageReceived");
        _onModMessageReceivedEvent?.Add(HandleModMessage);
    }

    #endregion

    #region Events

    public event EventHandler<bool> HasServerIntegrationChanged;

    #endregion

    #region Properties

    public bool HasServerIntegration { get; private set; }

    #endregion

    #region Public Methods

    public void SetServerIntegrationDetermined(bool hasServerIntegration)
    {
        HasServerIntegration = hasServerIntegration;
        HasServerIntegrationChanged?.Invoke(this, hasServerIntegration);
    }

    public void RegisterModMessageListener<TMessageType>(Action<TMessageType> messageHandler)
        where TMessageType : class, IModMessage, new()
    {
        _modMessageListeners.TryAdd(SerializationUtil.CreatePrefixedModMessageId<TMessageType>(), new MessageListener<TMessageType>(messageHandler));
    }

    public void UnregisterModMessageListener<TMessageType>()
    {
        _modMessageListeners.Remove(SerializationUtil.CreatePrefixedModMessageId<TMessageType>());
    }

    public void Destroy()
    {
        _onModMessageReceivedEvent?.Remove(HandleModMessage);
    }

    public void SendUnreliableMessage<T>(T messageToSend, bool relay = true)
        where T : class, IModMessage, new()
    {
        var messageToBeSend = SerializationUtil.Serialize(messageToSend);
        ModApiSystem.Singleton.SendModMessage(SerializationUtil.CreatePrefixedModMessageId<T>(), messageToBeSend, messageToBeSend.Length, relay);
    }

    public void SendReliableMessage<T>(T messageToSend, bool relay = true)
        where T : class, IModMessage, new()
    {
        var messageToBeSend = SerializationUtil.Serialize(messageToSend);

        var msgData = NetworkMain.CliMsgFactory.CreateNewMessageData<ModMsgData>();
        if (msgData.Data.Length < messageToBeSend.Length)
            msgData.Data = new byte[messageToBeSend.Length];

        Array.Copy(messageToBeSend, msgData.Data, messageToBeSend.Length);

        msgData.NumBytes = messageToBeSend.Length;
        msgData.Relay = relay;
        msgData.ModName = SerializationUtil.CreatePrefixedModMessageId<T>();

        // set message to reliable so that it gets split
        msgData.Reliable = true;

        var msg = SystemBase.MessageFactory.CreateNew<ModCliMsg>(msgData);
        SystemBase.TaskFactory.StartNew(() => NetworkSender.QueueOutgoingMessage(msg));
    }

    #endregion

    #region Non-Public Methods

    private void HandleModMessage(string id, byte[] data)
    {
        if (id == SerializationUtil.CreatePrefixedModMessageId<SegmentedMessage>())
            _segmentedMessageHandler.HandleSegment(data);

        if (!_modMessageListeners.TryGetValue(id, out var mod))
            return;

        mod.Execute(data);
    }

    #endregion

    #region Nested Types

    private class MessageListener<T> : IMessageListener
        where T : class, IModMessage, new()
    {
        #region Fields

        private readonly Action<T> _messageHandler;

        #endregion

        #region Constructors

        public MessageListener(Action<T> messageHandler)
        {
            _messageHandler = messageHandler;
        }

        #endregion

        #region Public Methods

        public void Execute(byte[] data)
        {
            if (data.Length <= 0)
                return;

            T syncMessage = null;

            try
            {
                syncMessage = SerializationUtil.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to deserialize {typeof(T).Name} message: {data.Length} size");
                Log.Exception(ex);
            }

            try
            {
                _messageHandler.Invoke(syncMessage);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        #endregion
    }

    #endregion
}

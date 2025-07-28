using System;
using System.Collections.Generic;

using LmpClient.Base;
using LmpClient.Network;
using LmpClient.Systems.ModApi;

using LmpCommon.Message.Client;
using LmpCommon.Message.Data;

namespace LunaCompat.Utils;

internal class ModMessageHandler
{
    #region Fields

    public static ModMessageHandler Instance;

    private readonly EventData<string, byte[]> _onModMessageReceivedEvent;
    private readonly Dictionary<string, IMessageListener> _modMessageListeners;

    #endregion

    #region Constructors

    public ModMessageHandler()
    {
        Instance = this;
        _modMessageListeners = [];
        _onModMessageReceivedEvent = GameEvents.FindEvent<EventData<string, byte[]>>("onModMessageReceived");
        _onModMessageReceivedEvent?.Add(HandleModMessage);
    }

    #endregion

    #region Public Methods

    public void RegisterModMessageListener<TMessageType>(string id, Action<TMessageType> messageHandler)
    {
        _modMessageListeners.TryAdd(id, new MessageListener<TMessageType>(messageHandler));
    }

    public void Destroy()
    {
        _onModMessageReceivedEvent?.Remove(HandleModMessage);
    }

    public void SendUnreliableMessage(string packageName, object messageToSend, bool relay = true)
    {
        var messageToBeSend = BinaryUtils.Serialize(messageToSend);
        ModApiSystem.Singleton.SendModMessage(packageName, messageToBeSend, messageToBeSend.Length, relay);
    }

    public void SendReliableMessage(string packageName, object messageToSend, bool relay = true)
    {
        var messageToBeSend = BinaryUtils.Serialize(messageToSend);

        var msgData = NetworkMain.CliMsgFactory.CreateNewMessageData<ModMsgData>();
        if (msgData.Data.Length < messageToBeSend.Length)
            msgData.Data = new byte[messageToBeSend.Length];

        Array.Copy(messageToBeSend, msgData.Data, messageToBeSend.Length);

        msgData.NumBytes = messageToBeSend.Length;
        msgData.Relay = relay;
        msgData.ModName = packageName;

        // set message to reliable so that it gets split
        msgData.Reliable = true;

        var msg = SystemBase.MessageFactory.CreateNew<ModCliMsg>(msgData);
        SystemBase.TaskFactory.StartNew(() => NetworkSender.QueueOutgoingMessage(msg));
    }

    #endregion

    #region Non-Public Methods

    private void HandleModMessage(string id, byte[] data)
    {
        if (!_modMessageListeners.TryGetValue(id, out var mod))
            return;

        mod.Execute(data);
    }

    #endregion

    #region Nested Types

    private interface IMessageListener
    {
        void Execute(byte[] data);
    }

    private class MessageListener<T> : IMessageListener
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

            var syncMessage = BinaryUtils.Deserialize<T>(data);
            _messageHandler.Invoke(syncMessage);
        }

        #endregion
    }

    #endregion
}

using System;
using System.Collections.Generic;

using LmpClient.Base;
using LmpClient.Network;
using LmpClient.Systems.ModApi;

using LmpCommon.Message.Client;
using LmpCommon.Message.Data;

namespace LunaFixes.Utils;

internal class ModMessageHandler
{
    #region Fields

    private readonly EventData<string, byte[]> _onModMessageReceivedEvent;
    private readonly Dictionary<string, Action<byte[]>> _modMessageListeners;

    #endregion

    #region Constructors

    public ModMessageHandler()
    {
        _modMessageListeners = [];
        _onModMessageReceivedEvent = GameEvents.FindEvent<EventData<string, byte[]>>("onModMessageReceived");
        _onModMessageReceivedEvent?.Add(HandleModMessage);
    }

    #endregion

    #region Public Methods

    public void RegisterModMessageListener(string id, Action<byte[]> messageHandler)
    {
        _modMessageListeners.TryAdd(id, messageHandler);
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
        if (_modMessageListeners.TryGetValue(id, out var mod))
            mod.Invoke(data);
    }

    #endregion
}

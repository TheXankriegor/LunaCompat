// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;

using LmpCommon.Message.Base;
using LmpCommon.Message.Data;

using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

namespace LunaCompatCommon.Messages
{
    internal interface IMessageHandler<TMessageListener>
    {
        bool TryGetMessageListener(string messageName, out TMessageListener messageListener);
    }

    internal abstract class MessageHandler<TMessageListener> : IMessageHandler<TMessageListener>
        where TMessageListener : IMessageListener
    {
        #region Fields

        protected readonly Dictionary<string, TMessageListener> _modMessageListeners;
        protected readonly ILogger _logger;
        private readonly FactoryBase _messageFactory;

        #endregion

        #region Constructors

        protected MessageHandler(ILogger logger, FactoryBase messageFactory)
        {
            _logger = logger;
            _messageFactory = messageFactory;
            _modMessageListeners = new Dictionary<string, TMessageListener>();
        }

        #endregion

        #region Public Methods

        public bool TryGetMessageListener(string messageName, out TMessageListener messageListener)
        {
            return _modMessageListeners.TryGetValue(messageName, out messageListener);
        }

        #endregion

        #region Non-Public Methods

        protected void SendMessageInternal<T>(T message, Action<ModMsgData> sendAction)
            where T : class, IModMessage, new()
        {
            try
            {
                var msg = CreateModMsgData(message);

                if (msg.NumBytes < Constants.MaxMessageSize)
                {
                    sendAction(msg);
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

                    sendAction(newMsg);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }
        }

        protected ModMsgData CreateModMsgData<T>(T message)
            where T : class, IModMessage, new()
        {
            var msg = _messageFactory.CreateNewMessageData<ModMsgData>();
            msg.ModName = SerializationUtil.CreatePrefixedModMessageId<T>();
            msg.Data = SerializationUtil.Serialize(message);
            msg.NumBytes = msg.Data.Length;

            return msg;
        }

        #endregion
    }
}

// ReSharper disable RedundantUsingDirective

using System;

using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

namespace LunaCompatCommon.Messages
{
    internal interface IMessageListener
    {
    }

    internal abstract class MessageListener<TMessageType> : IMessageListener
        where TMessageType : class, IModMessage, new()
    {
        #region Fields

        protected readonly ILogger _logger;

        #endregion

        #region Constructors

        protected MessageListener(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Non-Public Methods

        protected bool TryDeserializeMessage(byte[] data, out TMessageType message)
        {
            message = null;

            if (data.Length <= 0)
                return false;

            try
            {
                message = SerializationUtil.Deserialize<TMessageType>(data);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to deserialize {typeof(TMessageType).Name} message ({data.Length} bytes): {ex}");
            }

            return false;
        }

        #endregion
    }
}

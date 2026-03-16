// ReSharper disable RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Linq;

using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

namespace LunaCompatCommon.Messages
{
    internal abstract class SegmentedMessageListener : IMessageListener
    {
        #region Fields

        private readonly Dictionary<int, List<SegmentedMessage>> _messageParts;
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        protected SegmentedMessageListener(ILogger logger)
        {
            _logger = logger;
            _messageParts = new Dictionary<int, List<SegmentedMessage>>();
        }

        #endregion

        #region Non-Public Methods

        protected bool TryHandleSegment(byte[] data, out string messageType, out byte[] combinedBytes)
        {
            combinedBytes = Array.Empty<byte>();
            messageType = string.Empty;

            try
            {
                var message = SerializationUtil.Deserialize<SegmentedMessage>(data);

                if (_messageParts.TryGetValue(message.MessageId, out var parts))
                {
                    parts.Add(message);

                    if (parts.Count != message.PartCount)
                        return false;

                    _messageParts.Remove(message.MessageId);

                    var totalLength = parts.Sum(p => p.PartData.Length);
                    combinedBytes = new byte[totalLength];
                    var offset = 0;

                    foreach (var part in parts.OrderBy(x => x.PartId))
                    {
                        Array.Copy(part.PartData, 0, combinedBytes, offset, part.PartData.Length);
                        offset += part.PartData.Length;
                    }

                    messageType = message.OriginalType;

                    return true;
                }

                _messageParts.Add(message.MessageId, new List<SegmentedMessage>
                {
                    message
                });

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to deserialize message segment ({data.Length} bytes): {ex}");
            }

            return false;
        }

        #endregion
    }
}

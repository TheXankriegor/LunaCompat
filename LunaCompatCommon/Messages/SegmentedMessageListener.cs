// ReSharper disable RedundantUsingDirective

using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LunaCompatCommon.Messages
{
    internal abstract class SegmentedMessageListener : IMessageListener
    {
        #region Fields

        protected readonly ILogger _logger;

        private readonly ConcurrentDictionary<int, (List<SegmentedMessage> Segments, DateTime ReceivedTime )> _messageParts;

        #endregion

        #region Constructors

        protected SegmentedMessageListener(ILogger logger)
        {
            _logger = logger;
            _messageParts = new ConcurrentDictionary<int, (List<SegmentedMessage> Segments, DateTime ReceivedTime)>();
        }

        #endregion

        #region Non-Public Methods

        protected bool TryHandleSegment(byte[] data, out string messageType, out byte[] combinedBytes)
        {
            combinedBytes = Array.Empty<byte>();
            messageType = string.Empty;

            var message = SerializationUtil.Deserialize<SegmentedMessage>(data);

            if (_messageParts.TryGetValue(message.MessageId, out var partTpl))
            {
                var parts = partTpl.Segments;

                parts.Add(message);

                if (parts.Count != message.PartCount)
                    return false;

                _messageParts.TryRemove(message.MessageId, out _);

                var totalLength = 0;

                // Serverside has no defined LINQ Sum
                for (var i = 0; i < parts.Count; i++)
                {
                    var p = parts[i];
                    totalLength += p.PartData.Length;
                }

                combinedBytes = new byte[totalLength];
                var offset = 0;

                foreach (var part in parts.OrderBy(x => x.PartId))
                {
                    Array.Copy(part.PartData, 0, combinedBytes, offset, part.PartData.Length);
                    offset += part.PartData.Length;
                }

                if (_messageParts.Count > 100)
                {
                    var toRem = new List<int>();
                    var cutoff = DateTime.Now.AddMinutes(-1d);

                    foreach (var msg in _messageParts)
                    {
                        if (msg.Value.ReceivedTime < cutoff)
                            toRem.Add(msg.Key);
                    }

                    foreach (var idx in toRem)
                        _messageParts.TryRemove(idx, out _);
                }

                messageType = message.OriginalType;

                return true;
            }

            _messageParts.TryAdd(message.MessageId, (new List<SegmentedMessage>
            {
                message
            }, DateTime.Now));

            return false;
        }

        #endregion
    }
}

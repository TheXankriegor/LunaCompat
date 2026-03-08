using System;
using System.Collections.Generic;
using System.Linq;

using KSPBuildTools;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Serializer;

namespace LunaCompat.Utils;

internal class SegmentedMessageHandler
{
    #region Fields

    private readonly Dictionary<string, IMessageListener> _modMessageListeners;
    private readonly Dictionary<int, List<SegmentedMessage>> _messageParts;

    #endregion

    #region Constructors

    public SegmentedMessageHandler(Dictionary<string, IMessageListener> modMessageListeners)
    {
        _modMessageListeners = modMessageListeners;
        _messageParts = new Dictionary<int, List<SegmentedMessage>>();
    }

    #endregion

    #region Public Methods

    public void HandleSegment(byte[] data)
    {
        try
        {
            var message = SerializationUtil.Deserialize<SegmentedMessage>(data);

            if (_messageParts.TryGetValue(message.MessageId, out var parts))
            {
                parts.Add(message);

                if (parts.Count != message.PartCount)
                    return;

                _messageParts.Remove(message.MessageId);

                var combined = Array.Empty<byte>();
                foreach (var part in parts.OrderBy(x => x.PartId))
                    combined = combined.Concat(part.PartData).ToArray();
                if (!_modMessageListeners.TryGetValue(message.OriginalType, out var mod))
                    return;

                mod.Execute(combined);
            }
            else
                _messageParts.Add(message.MessageId, [message]);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to deserialize message segment: {data.Length} size");
            Log.Exception(ex);
        }
    }

    #endregion
}

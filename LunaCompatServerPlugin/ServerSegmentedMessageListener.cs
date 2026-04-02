using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using Server.Client;

namespace LunaCompatServerPlugin;

internal class ServerSegmentedMessageListener : SegmentedMessageListener, IServerMessageListener
{
    #region Fields

    private readonly ServerMessageHandler _messageHandler;

    #endregion

    #region Constructors

    public ServerSegmentedMessageListener(ILogger logger, ServerMessageHandler messageHandler)
        : base(logger)
    {
        _messageHandler = messageHandler;
    }

    #endregion

    #region Public Methods

    public void Execute(ClientStructure client, byte[] data)
    {
        try
        {
            if (TryHandleSegment(data, out var id, out var combinedBytes))
                _messageHandler.HandleReceivedMessage(client, id, combinedBytes);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to deserialize message segment ({data.Length} bytes): {ex}");
        }
    }

    #endregion
}

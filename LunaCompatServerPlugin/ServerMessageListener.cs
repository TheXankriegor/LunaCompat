using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using Server.Client;

namespace LunaCompatServerPlugin;

internal interface IServerMessageListener : IMessageListener
{
    void Execute(ClientStructure client, byte[] data);
}

internal class ServerMessageListener<TMessageType> : MessageListener<TMessageType>, IServerMessageListener
    where TMessageType : class, IModMessage, new()
{
    #region Fields

    private readonly Action<ClientStructure, TMessageType> _action;

    #endregion

    #region Constructors

    public ServerMessageListener(ILogger logger, Action<ClientStructure, TMessageType> action)
        : base(logger)
    {
        _action = action;
    }

    #endregion

    #region Public Methods

    public void Execute(ClientStructure client, byte[] data)
    {
        try
        {
            if (!TryDeserializeMessage(data, out var message))
            {
                _logger.Error($"Received invalid message of type '{typeof(TMessageType).Name}'.");
                return;
            }

            _action.Invoke(client, message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle received '{typeof(TMessageType).Name}' message: {ex}");
        }
    }

    #endregion
}

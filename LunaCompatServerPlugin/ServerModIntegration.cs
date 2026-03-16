using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompatServerPlugin;

internal abstract class ServerModIntegration : ModIntegration
{
    #region Fields

    protected readonly ServerMessageHandler _messageHandler;

    #endregion

    #region Constructors

    protected ServerModIntegration(ILogger logger, ServerMessageHandler messageHandler)
        : base(logger)
    {
        _messageHandler = messageHandler;
    }

    #endregion

    #region Public Methods

    public abstract void Setup();

    #endregion
}

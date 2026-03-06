namespace LunaCompatServerPlugin;

internal abstract class ServersideModIntegration
{
    #region Properties

    public abstract string ModPrefix { get; }

    #endregion

    #region Public Methods

    // replace server with some interface
    public abstract void Setup(ServerModMessageHandler messageHandler);

    public virtual void Destroy()
    {
        // nothing to do usually
    }

    #endregion
}

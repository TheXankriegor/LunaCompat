using LunaCompatCommon.Messages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

using LunaCompatServerPlugin.ModSettings;

using Server.Client;

namespace LunaCompatServerPlugin;

internal abstract class ServerModIntegration : ModIntegration
{
    #region Fields

    protected readonly ServerMessageHandler _messageHandler;

    #endregion

    #region Constructors

    protected ServerModIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider)
    {
        _messageHandler = messageHandler;
    }

    #endregion

    #region Public Methods

    public abstract void Setup();

    public virtual void InitializeSettings(ModSettingsProvider settingsProvider)
    {
        settingsProvider.SetValue(PackageName, IsIntegrationEnabledKey, true);
    }

    #endregion

    #region Non-Public Methods

    protected void SendSettingsValue<TMessage>(ClientStructure client, string key, object defaultValue)
        where TMessage : SettingsValueMessage, new()
    {
        var value = _settingsProvider.GetValue(PackageName, key, defaultValue);
        var msg = new TMessage
        {
            Key = key,
            Value = value.ToString()
        };
        _messageHandler.SendCompatMessage(client, msg);
    }

    #endregion
}

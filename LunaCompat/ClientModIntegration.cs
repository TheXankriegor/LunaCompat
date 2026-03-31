using System;

using LunaCompat.Utils;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat;

internal abstract class ClientModIntegration : ModIntegration
{
    #region Constructors

    protected ClientModIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Public Methods

    public abstract void Setup();

    #endregion

    #region Non-Public Methods

    protected void UpdateSetting<T>(string key, T defaultValue, ReflectedType type)
    {
        try
        {
            var value = _settingsProvider.GetValue(PackageName, key, defaultValue);
            var paramNode = HighLogic.CurrentGame.Parameters.CustomParams(type.Type);
            var converted = Convert.ChangeType(value, typeof(T));
            type.SetField(key, paramNode, converted);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load local setting for '{key}': {ex}");
        }
    }

    #endregion
}

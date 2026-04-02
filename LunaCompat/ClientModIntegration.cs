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

    protected void SaveSetting(string key, object instance, ReflectedType type)
    {
        try
        {
            var value = type.GetField(key, instance);
            _settingsProvider.SetValue(PackageName, key, value);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load local setting for '{key}': {ex}");
        }
    }

    protected void UpdateSetting<T>(string key, T defaultValue, object instance, ReflectedType type)
    {
        try
        {
            var value = _settingsProvider.GetValue(PackageName, key, defaultValue);
            var converted = Convert.ChangeType(value, typeof(T));
            type.SetField(key, instance, converted);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load local setting for '{key}': {ex}");
        }
    }

    #endregion
}

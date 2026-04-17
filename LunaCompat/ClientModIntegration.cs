using System;
using System.Linq;

using LmpClient.Systems.Status;

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

    /// <summary>
    /// Determine if the current player is the player with the alphabetically first name. This should be reliable enough for
    /// only processing background entities on one client.
    /// At worst, this misses a couple seconds when one person drops from the game.
    /// </summary>
    protected static bool IsPrimaryPlayer()
    {
        try
        {
            if (StatusSystem.Singleton == null || StatusSystem.Singleton.MyPlayerStatus == null || StatusSystem.Singleton.PlayerStatusList == null)
                return false;

            var localPlayer = StatusSystem.Singleton.MyPlayerStatus.PlayerName;

            // this could try to get a player in the most advanced subspace...
            var players = StatusSystem.Singleton.PlayerStatusList.Values.Select(x => x.PlayerName).Concat([localPlayer]).Distinct().OrderBy(x => x).ToArray();

            return players.Length <= 1 || players[0] == localPlayer;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to determine primary player: {ex}");
            return false;
        }
    }

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

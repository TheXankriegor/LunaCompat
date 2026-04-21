using System;
using System.Linq;

using LmpClient.Systems.Status;
using LmpClient.Systems.Warp;

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
    /// Determine if the current player is the player with the alphabetically first name in the latest subspace. This should be
    /// reliable enough for only processing background entities on one client.
    /// </summary>
    protected static bool IsPrimaryPlayer()
    {
        try
        {
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Status == Game.GameStatus.UNSTARTED || StatusSystem.Singleton == null ||
                WarpSystem.Singleton == null)
                return false;

            var localPlayer = StatusSystem.Singleton.MyPlayerStatus.PlayerName;

            // Never process if warping. At worst, this will "revert" processing to the state of the 2nd subspace - but otherwise we would have to compare potentially multiple warping players.
            if (WarpSystem.Singleton.CurrentlyWarping)
                return false;

            // if the first player is warping, they are not in a subspace, and instead the 2nd last player is selected...
            foreach (var subSpace in WarpSystem.Singleton.Subspaces.OrderByDescending(x => x.Value))
            {
                var subSpacePlayers = WarpSystem.Singleton.ClientSubspaceList.Where(x => x.Value == subSpace.Key).Select(x => x.Key).ToArray();

                if (!subSpacePlayers.Any())
                    continue;

                return localPlayer == subSpacePlayers.OrderBy(x => x).First();
            }

            // all players fallback
            Logger.Instance.Warning("No players found in subspaces. Using fallback primary player selection.");
            return localPlayer == StatusSystem.Singleton.PlayerStatusList.Values.Select(x => x.PlayerName)
                                              .Concat([localPlayer])
                                              .Distinct()
                                              .OrderBy(x => x)
                                              .FirstOrDefault();
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

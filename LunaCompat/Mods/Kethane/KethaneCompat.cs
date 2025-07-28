using HarmonyLib;

using JetBrains.Annotations;

using KSPBuildTools;

using LmpClient.Systems.TimeSync;

using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.Kethane;

[LunaFix]
[UsedImplicitly]
internal class KethaneCompat : ModCompat
{
    #region Properties

    public override string PackageName => "Kethane";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        var legacyResourceGenerator = AccessTools.TypeByName("Kethane.Generators.LegacyResourceGenerator");

        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(legacyResourceGenerator, "Load", [
            typeof(CelestialBody), typeof(ConfigNode)
        ]), new HarmonyMethod(typeof(KethaneCompat), nameof(PrefixLoad)));
    }

    #endregion

    #region Non-Public Methods

    /// <summary>
    /// Patch Kethane.Generators.LegacyResourceGenerator to not use random but instead always generate same values by
    /// pregenerating a seed node.
    /// </summary>
    // ReSharper disable once UnusedParameter.Local
    private static void PrefixLoad(CelestialBody body, ConfigNode node)
    {
        if (node != null)
            return;

        node = new ConfigNode();
        var seed = (int)(TimeSyncSystem.ServerStartTime % int.MaxValue);
        node.SetValue("Seed", seed);

        Log.Message($"[FIX {nameof(PackageName)}]: Use server-wide Kethane seed ({seed}).");
    }

    #endregion
}

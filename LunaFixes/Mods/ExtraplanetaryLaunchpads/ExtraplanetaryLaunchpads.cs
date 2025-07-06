using System;

using HarmonyLib;

using JetBrains.Annotations;

using LmpClient.Systems.TimeSync;

using LunaFixes.Attributes;
using LunaFixes.Utils;

namespace LunaFixes.Mods.ExtraplanetaryLaunchpads;

[LunaFix]
[UsedImplicitly]
internal class ExtraplanetaryLaunchpadsCompat : ModCompat
{
    #region Fields

    private static int serverSeed;

    #endregion

    #region Properties

    public override string PackageName => "Launchpad";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler)
    {
        // TODO add Extraplanetary Launchpads fixes here
        // building progress and basic launch + decouple works
        // can we patch the send/retrieve for docking vessel info to split larger vessels and reassemble them?

        // patch recycler random order
        serverSeed = (int)(TimeSyncSystem.ServerStartTime % int.MaxValue);

        var recyclerFsm = AccessTools.TypeByName("RecyclerFSM");

        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(recyclerFsm, "random", [
            typeof(float), typeof(float)
        ]), new HarmonyMethod(typeof(ExtraplanetaryLaunchpadsCompat), nameof(PrefixPatchedFloatRandom)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(recyclerFsm, "random", [
            typeof(int), typeof(int)
        ]), new HarmonyMethod(typeof(ExtraplanetaryLaunchpadsCompat), nameof(PrefixPatchedIntRandom)));
    }

    #endregion

    #region Non-Public Methods

    private static bool PrefixPatchedFloatRandom(ref float __result, float min, float max)
    {
        // Reinitialize Random every time to not break for different starting points
        var seed = serverSeed + (int)max;
        var setRandom = new Random(seed);

        __result = (float)(setRandom.NextDouble() * (max - min) + min);
        return false;
    }

    private static bool PrefixPatchedIntRandom(ref int __result, int min, int max)
    {
        // Reinitialize Random every time to not break for different starting points
        var seed = serverSeed + max;
        var setRandom = new Random(seed);

        __result = setRandom.Next() * (max - min) + min;
        return false;
    }

    #endregion
}

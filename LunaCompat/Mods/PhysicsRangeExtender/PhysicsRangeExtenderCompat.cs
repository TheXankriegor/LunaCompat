using System;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.PhysicsRangeExtender;

[LunaFix]
[UsedImplicitly]
internal class PhysicsRangeExtenderCompat : ModCompat
{
    #region Fields

    private static Type preSettings;

    #endregion

    #region Properties

    public override string PackageName => "PhysicsRangeExtender";

    #endregion

    #region Public Methods

    /// <summary>
    /// PRE will never work with LMP. Disable it on load if enabled.
    /// </summary>
    [HarmonyPatch]
    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        preSettings = AccessTools.TypeByName("PhysicsRangeExtender.PreSettings");
        var modEnabledSetter = AccessTools.PropertySetter(preSettings, "ModEnabled");
        modEnabledSetter.Invoke(null, [false]);
        LunaCompat.HarmonyInstance.Patch(modEnabledSetter, prefix: new HarmonyMethod(typeof(PhysicsRangeExtenderCompat), nameof(PrefixEnabledSet)));
    }

    #endregion

    #region Non-Public Methods

    private static bool PrefixEnabledSet()
    {
        return false;
    }

    #endregion
}

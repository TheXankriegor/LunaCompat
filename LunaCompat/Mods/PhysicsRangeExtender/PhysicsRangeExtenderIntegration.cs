using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Utils;

using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.PhysicsRangeExtender;

[UsedImplicitly]
internal class PhysicsRangeExtenderIntegration : ClientModIntegration
{
    #region Constructors

    public PhysicsRangeExtenderIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "PhysicsRangeExtender";

    #endregion

    #region Public Methods

    /// <summary>
    /// PRE will never work with LMP. Disable it on load if enabled.
    /// </summary>
    [HarmonyPatch]
    public override void Setup(ModSettingsProvider node)
    {
        var preSettings = AccessTools.TypeByName("PhysicsRangeExtender.PreSettings");
        var modEnabledSetter = AccessTools.PropertySetter(preSettings, "ModEnabled");
        modEnabledSetter.Invoke(null, [false]);
        LunaCompat.HarmonyInstance.Patch(modEnabledSetter, prefix: new HarmonyMethod(typeof(PhysicsRangeExtenderIntegration), nameof(PrefixEnabledSet)));
    }

    #endregion

    #region Non-Public Methods

    private static bool PrefixEnabledSet()
    {
        return false;
    }

    #endregion
}

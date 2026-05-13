using HarmonyLib;

using JetBrains.Annotations;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.PhysicsRangeExtender;

[UsedImplicitly]
internal class PhysicsRangeExtenderIntegration : ClientModIntegration
{
    #region Constructors

    public PhysicsRangeExtenderIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "PhysicsRangeExtender";

    #endregion

    #region Public Methods

    /// <summary>
    /// PRE terrain extender will cause vessel switching when easing positions. Disable it on load if enabled.
    /// </summary>
    [HarmonyPatch]
    public override void Setup()
    {
        var preSettings = AccessTools.TypeByName("PhysicsRangeExtender.PreSettings");
        var terrainExtenderEnabledSetter = AccessTools.PropertySetter(preSettings, "TerrainExtenderEnabled");
        terrainExtenderEnabledSetter.Invoke(null, [false]);
        LunaCompat.HarmonyInstance.Patch(terrainExtenderEnabledSetter,
                                         prefix: new HarmonyMethod(typeof(PhysicsRangeExtenderIntegration), nameof(PrefixTerrainExtenderEnabledSet)));
    }

    #endregion

    #region Non-Public Methods

    private static bool PrefixTerrainExtenderEnabledSet()
    {
        return false;
    }

    #endregion
}

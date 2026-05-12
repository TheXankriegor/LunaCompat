using System.IO;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Utils;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.SpeedUnitAnnex;

[UsedImplicitly]
internal class SpeedUnitAnnexIntegration : ClientModIntegration
{
    #region Fields

    private static ReflectedType suaSettingsSurfaceType;
    private static ReflectedType suaSettingsOrbitType;
    private static ReflectedType suaSettingsTargetType;

    private static string suaSettingsFilePath;

    #endregion

    #region Constructors

    public SpeedUnitAnnexIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "SpeedUnitAnnex";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        suaSettingsFilePath = Path.Combine(GetSharedModIntegrationFolder(), "SUASettings.cfg");

        suaSettingsSurfaceType = new ReflectedType("SpeedUnitAnnex.SUASettingsSurface");
        suaSettingsOrbitType = new ReflectedType("SpeedUnitAnnex.SUASettingsOrbit");
        suaSettingsTargetType = new ReflectedType("SpeedUnitAnnex.SUASettingsTarget");
        var speedUnitAnnexType = new ReflectedType("SpeedUnitAnnex.SpeedUnitAnnex");

        LunaCompat.HarmonyInstance.Patch(speedUnitAnnexType.Method("Start"), prefix: new HarmonyMethod(typeof(SpeedUnitAnnexIntegration), nameof(PrefixStart)));

        GameEvents.OnDifficultySettingsDismiss.Add(OnCloseDifficultySettings);
    }

    public override void Destroy()
    {
        GameEvents.OnDifficultySettingsDismiss.Remove(OnCloseDifficultySettings);

        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private static void PrefixStart()
    {
        if (!File.Exists(suaSettingsFilePath))
            return;

        var node = ConfigNode.Load(suaSettingsFilePath);

        if (node.HasNode("SUASettingsSurface"))
            HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsSurfaceType.Type)?.Load(node.GetNode("SUASettingsSurface"));
        if (node.HasNode("SUASettingsOrbit"))
            HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsOrbitType.Type)?.Load(node.GetNode("SUASettingsOrbit"));
        if (node.HasNode("SUASettingsTarget"))
            HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsTargetType.Type)?.Load(node.GetNode("SUASettingsTarget"));
    }

    private void OnCloseDifficultySettings(DifficultyOptionsMenu menu, bool commitChanges)
    {
        if (!commitChanges)
            return;

        var node = new ConfigNode();

        HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsSurfaceType.Type).Save(node.AddNode("SUASettingsSurface"));
        HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsOrbitType.Type).Save(node.AddNode("SUASettingsOrbit"));
        HighLogic.CurrentGame?.Parameters?.CustomParams(suaSettingsTargetType.Type).Save(node.AddNode("SUASettingsTarget"));

        node.Save(suaSettingsFilePath);
    }

    #endregion
}

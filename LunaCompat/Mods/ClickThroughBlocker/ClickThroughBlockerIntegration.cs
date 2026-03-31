using System.IO;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Utils;

using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.ClickThroughBlocker;

[UsedImplicitly]
internal class ClickThroughBlockerIntegration : ClientModIntegration
{
    #region Fields

    private static ReflectedType ctbType;

    private static string ctbSettingsFilePath;

    #endregion

    #region Constructors

    public ClickThroughBlockerIntegration(ILogger logger, ModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "ClickThroughBlocker";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        var oneTimePopupType = new ReflectedType("ClickThroughFix.OneTimePopup");
        ctbType = new ReflectedType("ClickThroughFix.CTB");

        var saveGamePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder);
        if (!Directory.Exists(saveGamePath))
            Directory.CreateDirectory(saveGamePath);

        ctbSettingsFilePath = Path.Combine(saveGamePath, "CTBSettings.cfg");

        LunaCompat.HarmonyInstance.Patch(oneTimePopupType.Method("Awake"),
                                         prefix: new HarmonyMethod(typeof(ClickThroughBlockerIntegration), nameof(PrefixAwake)));
        LunaCompat.HarmonyInstance.Patch(oneTimePopupType.Method("CreatePopUpFlagFile"),
                                         prefix: new HarmonyMethod(typeof(ClickThroughBlockerIntegration), nameof(PrefixPopUpFile)));
    }

    #endregion

    #region Non-Public Methods

    private static void PrefixAwake()
    {
        if (!File.Exists(ctbSettingsFilePath))
            return;

        var node = ConfigNode.Load(ctbSettingsFilePath);

        HighLogic.CurrentGame?.Parameters?.CustomParams(ctbType.Type).Load(node);
    }

    private static void PrefixPopUpFile()
    {
        var settings = HighLogic.CurrentGame?.Parameters?.CustomParams(ctbType.Type);
        if (settings == null)
            return;

        var node = new ConfigNode();

        settings.Save(node);
        node.Save(ctbSettingsFilePath);
    }

    #endregion
}

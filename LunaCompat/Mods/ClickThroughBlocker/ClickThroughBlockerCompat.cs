using System;
using System.IO;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.ClickThroughBlocker;

[LunaFix]
[UsedImplicitly]
internal class ClickThroughBlockerCompat : ModCompat
{
    #region Fields

    private static Type ctbParameters;
    private static string ctbSettingsFilePath;

    #endregion

    #region Properties

    public override string PackageName => "ClickThroughBlocker";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        var oneTimePopup = AccessTools.TypeByName("ClickThroughFix.OneTimePopup");
        ctbParameters = AccessTools.TypeByName("ClickThroughFix.CTB");

        var saveGamePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder);
        if (!Directory.Exists(saveGamePath))
            Directory.CreateDirectory(saveGamePath);

        ctbSettingsFilePath = Path.Combine(saveGamePath, "CTBSettings.cfg");

        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(oneTimePopup, "Awake", []),
                                         prefix: new HarmonyMethod(typeof(ClickThroughBlockerCompat), nameof(PrefixAwake)));
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(oneTimePopup, "CreatePopUpFlagFile", []),
                                         prefix: new HarmonyMethod(typeof(ClickThroughBlockerCompat), nameof(PrefixPopUpFile)));
    }

    #endregion

    #region Non-Public Methods

    private static void PrefixAwake()
    {
        if (!File.Exists(ctbSettingsFilePath))
            return;

        var node = ConfigNode.Load(ctbSettingsFilePath);

        HighLogic.CurrentGame?.Parameters?.CustomParams(ctbParameters).Load(node);
    }

    private static void PrefixPopUpFile()
    {
        var settings = HighLogic.CurrentGame?.Parameters?.CustomParams(ctbParameters);
        if (settings == null)
            return;

        var node = new ConfigNode();

        settings.Save(node);
        node.Save(ctbSettingsFilePath);
    }

    #endregion
}

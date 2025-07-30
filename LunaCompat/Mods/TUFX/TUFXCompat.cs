using System;
using System.IO;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.TUFX;

[LunaFix]
[UsedImplicitly]
internal class TufxCompat : ModCompat
{
    #region Fields

    private static Type tufxGameSettings;
    private static string tufxSettingsFilePath;

    #endregion

    #region Properties

    public override string PackageName => "TUFX";

    #endregion

    #region Public Methods

    /// <summary>
    /// Patch TUFX to save settings not in the sfs but in a separate file that does not reset on join.
    /// </summary>
    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        var texturesUnlimitedFXLoader = AccessTools.TypeByName("TUFX.TexturesUnlimitedFXLoader");
        var tufxScene = AccessTools.TypeByName("TUFX.TUFXScene");
        tufxGameSettings = AccessTools.TypeByName("TUFX.TUFXGameSettings");
        tufxSettingsFilePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder, "TUFXSettings.cfg");

        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(texturesUnlimitedFXLoader, "GetProfileNameForScene", [
            tufxScene
        ]), prefix: new HarmonyMethod(typeof(TufxCompat), nameof(PrefixLoad)));
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(texturesUnlimitedFXLoader, "ChangeProfileForScene", [
            typeof(string), tufxScene
        ]), postfix: new HarmonyMethod(typeof(TufxCompat), nameof(PostfixSave)));
    }

    #endregion

    #region Non-Public Methods

    private static void PrefixLoad(object[] __args)
    {
        if (!File.Exists(tufxSettingsFilePath))
            return;

        var node = ConfigNode.Load(tufxSettingsFilePath);

        HighLogic.CurrentGame?.Parameters?.CustomParams(tufxGameSettings).Load(node);
    }

    private static void PostfixSave(object[] __args)
    {
        var settings = HighLogic.CurrentGame?.Parameters?.CustomParams(tufxGameSettings);
        if (settings == null)
            return;

        var node = new ConfigNode();

        settings.Save(node);
        node.Save(tufxSettingsFilePath);
    }

    #endregion
}

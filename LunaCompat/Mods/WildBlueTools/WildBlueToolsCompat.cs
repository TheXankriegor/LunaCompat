using JetBrains.Annotations;
using LunaCompat.Attributes;
using LunaCompat.Utils;

namespace LunaCompat.Mods.WildBlueTools;

[LunaFix]
[UsedImplicitly]
internal class WildBlueToolsCompat : ModCompat
{
    #region Properties

    public override string PackageName => "WildBlueTools";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        // TODO: WildBlueTools seems to have some background processing for OmniConverters. This will probably need a similar handling as SCANsat
    }

    #endregion
}
using JetBrains.Annotations;

using LunaFixes.Attributes;
using LunaFixes.Utils;

namespace LunaFixes.Mods.SpaceDust;

[LunaFix]
[UsedImplicitly]
internal class SpaceDustCompat : ModCompat
{
    #region Properties

    public override string PackageName => "SpaceDust";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        // TODO: Will have to actually check and see if SpaceDust is maybe already compatible. At worst will need similar sync as SCANsat...
    }

    #endregion
}

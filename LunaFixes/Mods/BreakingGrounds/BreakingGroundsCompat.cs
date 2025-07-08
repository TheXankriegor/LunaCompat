using JetBrains.Annotations;

using LunaFixes.Attributes;
using LunaFixes.Utils;

namespace LunaFixes.Mods.BreakingGrounds;

[LunaFix]
[UsedImplicitly]
internal class BreakingGroundsCompat : ModCompat
{
    #region Properties

    public override string PackageName => "Breaking Grounds";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        // TODO this will require hooking into the creation event via harmony and fire an actual event the server can see
        // Currently LMP is missing a handler for all ground science GameEvents (GameEvents.onGroundSciencePartDeployed etc.)
        // Postfix to ModuleGroundExpControl.OnGroundSciencePartDeployed should suffice for changing vessel type if needed.
        // --> And should probably be done in base LMP
    }

    #endregion
}

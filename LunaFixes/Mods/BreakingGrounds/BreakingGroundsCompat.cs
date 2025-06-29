using JetBrains.Annotations;

using LunaFixes.Attributes;

namespace LunaFixes.Mods.BreakingGrounds;

[LunaFixFor(PackageName)]
[UsedImplicitly]
internal class BreakingGroundsCompat
{
    #region Constants

    private const string PackageName = "Breaking Grounds";

    #endregion

    #region Constructors

    public BreakingGroundsCompat(LunaFixForAttribute _)
    {
        // TODO this will require hooking into the creation event via harmony and fire an actual event the server can see
        // Currently LMP is missing a handler for all ground science GameEvents (GameEvents.onGroundSciencePartDeployed etc.)
        // Postfix to ModuleGroundExpControl.OnGroundSciencePartDeployed should suffice for changing vessel type if needed.
        // --> And should probably be done in base LMP
    }

    #endregion
}

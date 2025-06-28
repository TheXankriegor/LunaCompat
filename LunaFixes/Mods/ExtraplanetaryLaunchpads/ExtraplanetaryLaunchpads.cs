using JetBrains.Annotations;

using LunaFixes.Attributes;

namespace LunaFixes.Mods.ExtraplanetaryLaunchpads
{
    [LunaFixFor(PackageName)]
    [UsedImplicitly]
    internal class ExtraplanetaryLaunchpadsCompat
    {
        #region Constants

        private const string PackageName = "Launchpad";

        #endregion

        #region Constructors

        public ExtraplanetaryLaunchpadsCompat(LunaFixForAttribute _)
        {
            // TODO add Extraplanetary Launchpads fixes here
            // can we patch the send/retrieve for docking vessel info to split larger vessels and reassemble them?
        }

        #endregion
    }
}

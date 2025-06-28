using JetBrains.Annotations;

using LunaFixes.Attributes;

namespace LunaFixes.Mods.Kethane
{
    [LunaFixFor(PackageName)]
    [UsedImplicitly]
    internal class KethaneCompat
    {
        #region Constants

        private const string PackageName = "Kethane";

        #endregion

        #region Constructors

        public KethaneCompat(LunaFixForAttribute _)
        {
            // TODO add Kethane fixes here
        }

        #endregion
    }
}

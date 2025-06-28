using JetBrains.Annotations;

using LunaFixes.Attributes;

namespace LunaFixes.Mods.KerbalPlanetaryBaseSystems
{
    [LunaFixFor(PackageName)]
    [UsedImplicitly]
    internal class PlanetaryBaseSystemsCompat
    {
        #region Constants

        private const string PackageName = "Launchpad";

        #endregion

        #region Constructors

        public PlanetaryBaseSystemsCompat(LunaFixForAttribute _)
        {
            // TODO add Kerbal Planetary Base Systems fixes here
        }

        #endregion
    }
}

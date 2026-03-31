using JetBrains.Annotations;

using LunaCompat.Utils;

using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.KerbalPlanetaryBaseSystems;

[UsedImplicitly]
internal class PlanetaryBaseSystemsIntegration : ClientModIntegration
{
    #region Constructors

    public PlanetaryBaseSystemsIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "PlanetarySurfaceStructures";

    #endregion

    #region Public Methods

    public override void Setup(ModSettingsProvider node)
    {
        // TODO add Kerbal Planetary Base Systems fixes here
    }

    #endregion
}

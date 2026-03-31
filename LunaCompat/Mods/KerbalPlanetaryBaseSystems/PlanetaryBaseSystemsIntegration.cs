using JetBrains.Annotations;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.KerbalPlanetaryBaseSystems;

[UsedImplicitly]
internal class PlanetaryBaseSystemsIntegration : ClientModIntegration
{
    #region Constructors

    public PlanetaryBaseSystemsIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "PlanetarySurfaceStructures";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        // TODO add Kerbal Planetary Base Systems fixes here
    }

    #endregion
}

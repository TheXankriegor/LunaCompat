using JetBrains.Annotations;

using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.SpaceDust;

[UsedImplicitly]
internal class SpaceDustIntegration : ClientModIntegration
{
    #region Constructors

    public SpaceDustIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "SpaceDust";

    #endregion

    #region Public Methods

    public override void Setup(ConfigNode node)
    {
        // TODO: Will have to actually check and see if SpaceDust is maybe already compatible. At worst will need similar sync as SCANsat...
    }

    #endregion
}

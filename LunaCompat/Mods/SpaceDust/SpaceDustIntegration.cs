using JetBrains.Annotations;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.SpaceDust;

[UsedImplicitly]
internal class SpaceDustIntegration : ClientModIntegration
{
    #region Constructors

    public SpaceDustIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "SpaceDust";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        // TODO: Will have to actually check and see if SpaceDust is maybe already compatible. At worst will need similar sync as SCANsat...
    }

    #endregion
}

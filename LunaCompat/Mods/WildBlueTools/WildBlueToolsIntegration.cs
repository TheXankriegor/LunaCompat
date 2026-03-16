using JetBrains.Annotations;

using LunaCompatCommon.Utils;

namespace LunaCompat.Mods.WildBlueTools;

[UsedImplicitly]
internal class WildBlueToolsIntegration : ClientModIntegration
{
    #region Constructors

    public WildBlueToolsIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "WildBlueTools";

    #endregion

    #region Public Methods

    public override void Setup(ConfigNode node)
    {
        // TODO: WildBlueTools seems to have some background processing for OmniConverters. This will probably need a similar handling as SCANsat
    }

    #endregion
}

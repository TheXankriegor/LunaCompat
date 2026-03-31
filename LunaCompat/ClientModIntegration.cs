using LunaCompat.Utils;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat;

internal abstract class ClientModIntegration : ModIntegration
{
    #region Constructors

    protected ClientModIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Public Methods

    public abstract void Setup(ModSettingsProvider node);

    #endregion
}

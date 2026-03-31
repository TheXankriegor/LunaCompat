using LunaCompatCommon.Utils;

namespace LunaCompatCommon.ModIntegration
{
    public interface IModIntegration
    {
        string PackageName { get; }

        public void Destroy();
    }

    internal abstract class ModIntegration : IModIntegration
    {
        #region Fields

        protected readonly ILogger _logger;

        #endregion

        #region Constructors

        protected ModIntegration(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Properties

        public abstract string PackageName { get; }

        public string IsIntegrationEnabledKey => $"{PackageName}_Enabled";

        #endregion

        #region Public Methods

        public virtual void Destroy()
        {
            // nothing to do usually
        }

        #endregion
    }
}

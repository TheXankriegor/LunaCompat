using LunaCompat.Utils;

namespace LunaCompat;

internal abstract class ModCompat
{
    #region Properties

    public abstract string PackageName { get; }

    #endregion

    #region Public Methods

    public abstract void Patch(ModMessageHandler modMessageHandler, ConfigNode node);

    public virtual void Destroy()
    {
        // nothing to do usually
    }

    #endregion
}

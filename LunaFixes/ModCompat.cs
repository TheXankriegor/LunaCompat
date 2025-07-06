using LunaFixes.Utils;

namespace LunaFixes;

internal abstract class ModCompat
{
    #region Properties

    public abstract string PackageName { get; }

    #endregion

    #region Public Methods

    public abstract void Patch(ModMessageHandler modMessageHandler);

    public virtual void Destroy()
    {
        // nothing to do usually
    }

    #endregion
}

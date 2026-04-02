using LunaCompatCommon.Utils;

using Server.Log;

namespace LunaCompatServerPlugin.Utils;

internal class Logger : BaseLogger
{
    #region Public Methods

    public override void NetworkDebug(string msg, string integration = null)
    {
        LunaLog.NetworkDebug(FormatMessage(msg, integration));
    }

    public override void Debug(string msg, string integration = null)
    {
        LunaLog.Debug(FormatMessage(msg, integration));
    }

    public override void Info(string msg, string integration = null)
    {
        LunaLog.Info(FormatMessage(msg, integration));
    }

    public override void Warning(string msg, string integration = null)
    {
        LunaLog.Warning(FormatMessage(msg, integration));
    }

    public override void Error(string msg, string integration = null)
    {
        LunaLog.Error(FormatMessage(msg, integration));
    }

    public override void Error(Exception ex, string integration = null)
    {
        LunaLog.Fatal(FormatMessage(ex.ToString(), integration));
    }

    #endregion
}

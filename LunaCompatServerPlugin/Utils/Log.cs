using Server.Log;

namespace LunaCompatServerPlugin.Utils;

internal static class Log
{
    #region Public Methods

    public static void NetworkDebug(string msg, string integration = null)
    {
        LunaLog.NetworkDebug(FormatMessage(msg, integration));
    }

    public static void Debug(string msg, string integration = null)
    {
        LunaLog.Debug(FormatMessage(msg, integration));
    }

    public static void Info(string msg, string integration = null)
    {
        LunaLog.Info(FormatMessage(msg, integration));
    }

    public static void Warning(string msg, string integration = null)
    {
        LunaLog.Warning(FormatMessage(msg, integration));
    }

    public static void Error(string msg, string integration = null)
    {
        LunaLog.Error(FormatMessage(msg, integration));
    }

    public static void Fatal(string msg, string integration = null)
    {
        LunaLog.Fatal(FormatMessage(msg, integration));
    }

    #endregion

    #region Non-Public Methods

    private static string FormatMessage(string msg, string integration)
    {
        return string.IsNullOrEmpty(integration) ? $"[LunaCompat] - {msg}" : $"[LunaCompat|{integration}] - {msg}";
    }

    #endregion
}

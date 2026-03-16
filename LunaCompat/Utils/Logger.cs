using System;

using KSPBuildTools;

using LunaCompatCommon.Utils;

namespace LunaCompat.Utils
{
    internal class Logger : BaseLogger
    {
        #region Fields

        public static Logger Instance;

        #endregion

        #region Constructors

        public Logger()
        {
            Instance = new Logger();
        }

        #endregion

        #region Public Methods

        public override void NetworkDebug(string msg, string integration = null)
        {
            Log.Debug(FormatMessage(msg, integration));
        }

        public override void Debug(string msg, string integration = null)
        {
            Log.Debug(FormatMessage(msg, integration));
        }

        public override void Info(string msg, string integration = null)
        {
            Log.Message(FormatMessage(msg, integration));
        }

        public override void Warning(string msg, string integration = null)
        {
            Log.Warning(FormatMessage(msg, integration));
        }

        public override void Error(string msg, string integration = null)
        {
            Log.Error(FormatMessage(msg, integration));
        }

        public override void Error(Exception ex, string integration = null)
        {
            Log.Exception(ex);
        }

        #endregion
    }
}

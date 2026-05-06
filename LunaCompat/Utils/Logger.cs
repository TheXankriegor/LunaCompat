using KSPBuildTools;
using LunaCompatCommon.Utils;
using System;

using LmpClient;

using UnityEngine;

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
            Instance = this;
        }

        #endregion

        #region Public Methods

        public static void LogServerPluginMissing(string integration)
        {
            LogScreenMessage("Sync will not function without the server plugin!", Color.red, integration);
        }
        public static void LogScreenMessage(string msg, Color color, string integration = null)
        {
            LunaScreenMsg.PostScreenMessage(FormatMessage(msg, integration), 5f, ScreenMessageStyle.UPPER_CENTER, color);
        }

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

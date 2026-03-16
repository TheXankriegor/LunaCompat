// ReSharper disable RedundantUsingDirective

using System;

namespace LunaCompatCommon.Utils
{
    internal interface ILogger
    {
        void NetworkDebug(string msg, string integration = null);

        void Debug(string msg, string integration = null);

        void Info(string msg, string integration = null);

        void Warning(string msg, string integration = null);

        void Error(string msg, string integration = null);

        void Error(Exception ex, string integration = null);
    }

    internal abstract class BaseLogger : ILogger
    {
        #region Public Methods

        public abstract void NetworkDebug(string msg, string integration = null);

        public abstract void Debug(string msg, string integration = null);

        public abstract void Info(string msg, string integration = null);

        public abstract void Warning(string msg, string integration = null);

        public abstract void Error(string msg, string integration = null);

        public abstract void Error(Exception ex, string integration = null);

        #endregion

        #region Non-Public Methods

        protected static string FormatMessage(string msg, string integration)
        {
            return string.IsNullOrEmpty(integration) ? $"[LunaCompat] - {msg}" : $"[LunaCompat|{integration}] - {msg}";
        }

        #endregion
    }
}

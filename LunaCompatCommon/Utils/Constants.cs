namespace LunaCompatCommon.Utils
{
    internal static class Constants
    {
        #region Constants

        public const string Prefix = "LMPC_";
        /// <summary>
        /// Technically anything under 8192 could work, but depending on the message we need a buffer
        /// </summary>
        public const int MaxMessageSize = 8000;

        #endregion
    }
}

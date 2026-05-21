using System;
using System.Runtime.CompilerServices;

using LmpClient;

using LunaCompatCommon.Utils;

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

        public static void PostPopupDialog(string id, string text)
        {
            PopupDialog.SpawnPopupDialog(new MultiOptionDialog(id, text, nameof(LunaCompat), HighLogic.UISkin, new DialogGUIButton("OK", () =>
            {
            })), false, HighLogic.UISkin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void NetworkDebug(string msg, string integration = null)
        {
#if DEBUG
            UnityEngine.Debug.Log($"[NETWORK_DEBUG] {FormatMessage(msg, integration)}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Debug(string msg, string integration = null)
        {
#if DEBUG
            UnityEngine.Debug.Log($"[DEBUG] {FormatMessage(msg, integration)}");
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Info(string msg, string integration = null)
        {
            UnityEngine.Debug.Log(FormatMessage(msg, integration));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Warning(string msg, string integration = null)
        {
            UnityEngine.Debug.LogWarning(FormatMessage(msg, integration));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Error(string msg, string integration = null)
        {
            UnityEngine.Debug.LogError(FormatMessage(msg, integration));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Error(Exception ex, string integration = null)
        {
            UnityEngine.Debug.LogException(ex);
        }

        #endregion
    }
}

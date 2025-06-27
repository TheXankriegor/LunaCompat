using HarmonyLib;

using JetBrains.Annotations;

using KSPBuildTools;

using LmpClient.Systems.VesselProtoSys;

using LunaFixes.Attributes;

using UnityEngine;

namespace LunaFixes.Mods.KerbalInventorySystem
{
    [LunaFixFor(PackageName)]
    [UsedImplicitly]
    public class KisCompat
    {
        #region Constants

        private const string PackageName = "KIS_Shared";

        #endregion

        #region Constructors

        public KisCompat(LunaFixForAttribute _)
        {
            var kisShared = AccessTools.TypeByName("KIS.KIS_Shared");
            var onPartReady = kisShared.GetNestedType("OnPartReady");

            LunaFixes.HarmonyInstance.Patch(AccessTools.Method(kisShared, "CreatePart", [
                typeof(ConfigNode), typeof(Vector3), typeof(Quaternion), typeof(Part), typeof(Part), typeof(string), typeof(AttachNode), onPartReady,
                typeof(bool)
            ]), postfix: new HarmonyMethod(typeof(KisCompat), nameof(PostfixCreatePart)));
        }

        #endregion

        #region Non-Public Methods

        /// <summary>
        /// When dropping a KIS inventory item don't set its type as "debris" as the server will remove it with the dekessler.
        /// </summary>
        private static void PostfixCreatePart(Part __result)
        {
            if (__result.vessel.vesselType == VesselType.Debris)
                __result.vessel.vesselType = VesselType.Unknown;

            Log.Message($"[FIX {nameof(PackageName)}]: Set vessel type to {VesselType.Unknown}");

            VesselProtoSystem.Singleton.MessageSender.SendVesselMessage(__result.vessel);
        }

        #endregion
    }
}

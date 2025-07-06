using HarmonyLib;

using JetBrains.Annotations;

using KSPBuildTools;

using LmpClient.Systems.VesselProtoSys;

using LunaFixes.Attributes;
using LunaFixes.Utils;

using UnityEngine;

namespace LunaFixes.Mods.KerbalInventorySystem;

[LunaFix]
[UsedImplicitly]
internal class KisCompat : ModCompat
{
    #region Properties

    public override string PackageName => "KIS";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler)
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

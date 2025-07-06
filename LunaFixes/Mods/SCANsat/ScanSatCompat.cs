using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using JetBrains.Annotations;

using KSPBuildTools;

using LmpClient.Systems.Status;
using LmpClient.Systems.VesselProtoSys;

using LunaFixes.Attributes;
using LunaFixes.Utils;

using UnityEngine;

namespace LunaFixes.Mods.SCANsat;

[LunaFix]
[UsedImplicitly]
internal class ScanSatCompat : ModCompat
{
    #region Fields

    private static MethodInfo finishRegistrationMethod;

    private bool _keepAlive;
    private ModMessageHandler _modMessageHandler;
    private MethodInfo _getDataMethod;
    private PropertyInfo _getAllDataMethod;
    private MethodInfo _serializeMethod;
    private PropertyInfo _coverageProp;
    private PropertyInfo _bodyProp;
    private MethodInfo _deserializeMethod;

    #endregion

    #region Properties

    public override string PackageName => "SCANsat";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler)
    {
        _modMessageHandler = modMessageHandler;
        var scanControllerType = AccessTools.TypeByName("SCANsat.SCANcontroller");
        finishRegistrationMethod = scanControllerType.GetMethod("finishRegistration", BindingFlags.NonPublic | BindingFlags.Instance);
        _getDataMethod = scanControllerType.GetMethod("getData", [typeof(string)]);
        _getAllDataMethod = scanControllerType.GetProperty("GetAllData");

        var scanDataType = AccessTools.TypeByName("SCANsat.SCAN_Data.SCANdata");
        _serializeMethod = scanDataType.GetMethod("shortSerialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        _coverageProp = scanDataType.GetProperty("Coverage");
        _bodyProp = scanDataType.GetProperty("Body");
        _deserializeMethod = scanDataType.GetMethod("shortDeserialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "scanFromAllVessels"),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixScan)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "OnSave", [typeof(ConfigNode)]),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixOnSave)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "Update"),
                                        postfix: new HarmonyMethod(typeof(ScanSatCompat), nameof(PostfixUpdate)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "finishRegistration", [typeof(Guid)]),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixFinishRegistration)));

        // add a custom scenario handler for map progress
        _keepAlive = true;
        LunaFixes.Singleton.StartCoroutine(ScanSatSync());
        modMessageHandler.RegisterModMessageListener(PackageName, OnModMessageReceived);
    }

    public override void Destroy()
    {
        _keepAlive = false;
        LunaFixes.Singleton.StopCoroutine(ScanSatSync());
        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private static void PostfixUpdate(object __instance, List<Guid> ___tempIDs)
    {
        // all vessels loaded
        if (!VesselProtoSystem.Singleton.VesselProtos.All(x => x.Value.IsEmpty))
            return;

        var toRem = new List<Guid>();

        for (var i = ___tempIDs.Count - 1; i >= 0; i--)
        {
            if (FlightGlobals.Vessels.Any(a => a.id == ___tempIDs[i]))
            {
                finishRegistrationMethod.Invoke(__instance, [___tempIDs[i]]);
                toRem.Add(___tempIDs[i]);
            }
        }

        foreach (var id in toRem)
            ___tempIDs.Remove(id);
    }

    private static bool PrefixOnSave(ConfigNode __0)
    {
        if (!__0.HasData)
            return false;

        if (VesselProtoSystem.Singleton.VesselProtos.All(x => x.Value.IsEmpty))
            return true;

        Log.Message(
            $"Blocking SCANsat save - vessels are still loading ({VesselProtoSystem.Singleton.VesselProtos.Values.Count}, {VesselProtoSystem.Singleton.VesselProtos.Count(x => !x.Value.IsEmpty)}).");
        return false;
    }

    private static bool PrefixFinishRegistration(Guid __0)
    {
        return FlightGlobals.Vessels.Any(a => a.id == __0);
    }

    private static bool PrefixScan()
    {
        return IsPrimaryPlayer();
    }

    private static bool IsPrimaryPlayer()
    {
        var localPlayer = StatusSystem.Singleton.MyPlayerStatus.PlayerName;
        var players = StatusSystem.Singleton.PlayerStatusList.Values.Select(x => x.PlayerName).Concat([localPlayer]).OrderBy(x => x).ToArray();

        return players.Length <= 1 || players[0] == StatusSystem.Singleton.MyPlayerStatus.PlayerName;
    }

    private IEnumerator ScanSatSync()
    {
        var previousValues = new Dictionary<string, string>();

        while (_keepAlive)
        {
            yield return new WaitForSeconds(5);

            try
            {
                var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == "SCANcontroller");

                if (scanController == null || !IsPrimaryPlayer())
                    continue;

                // For the server, the scenario data is updated as should be. However, without an additional system other clients won't know about the scan updates

                var scanDataList = _getAllDataMethod.GetValue(scanController) as IEnumerable;

                foreach (var scanData in scanDataList)
                {
                    var coverage = _coverageProp.GetValue(scanData) as short[,];
                    var body = _bodyProp.GetValue(scanData) as CelestialBody;

                    if (!body || coverage == null)
                        continue;

                    var serializedData = _serializeMethod.Invoke(scanData, []) as string;

                    // no change
                    if (previousValues.TryGetValue(body.bodyName, out var previousValue) && previousValue.Equals(serializedData))
                        continue;

                    previousValues[body.bodyName] = serializedData;

                    var messageToSend = new ScanSatSyncMessage
                    {
                        Body = body.bodyName,
                        Map = serializedData
                    };

                    _modMessageHandler.SendReliableMessage(PackageName, messageToSend);

                    //var messageToBeSend = BinaryUtils.Serialize(messageToSend);

                    //var msgData = NetworkMain.CliMsgFactory.CreateNewMessageData<ModMsgData>();
                    //if (msgData.Data.Length < messageToBeSend.Length)
                    //    msgData.Data = new byte[messageToBeSend.Length];

                    //Array.Copy(messageToBeSend, msgData.Data, messageToBeSend.Length);

                    //msgData.NumBytes = messageToBeSend.Length;
                    //msgData.Relay = true;
                    //msgData.ModName = PackageName;
                    //// set message to reliable so that it gets split
                    //msgData.Reliable = true;

                    //var msg = ModApiSystem.MessageFactory.CreateNew<ModCliMsg>(msgData);
                    //ModApiMessageSender.TaskFactory.StartNew(() => NetworkSender.QueueOutgoingMessage(msg));
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }

    private void ProcessReceivedMessage(ScanSatSyncMessage message)
    {
        // Refresh scan controller every time to account for scene changes
        var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == "SCANcontroller");

        if (scanController == null || IsPrimaryPlayer())
            return;

        var scanData = _getDataMethod.Invoke(scanController, [message.Body]);
        _deserializeMethod.Invoke(scanData, [message.Map]);
    }

    private void OnModMessageReceived(byte[] data)
    {
        if (data.Length <= 0)
            return;

        var syncMessage = BinaryUtils.Deserialize<ScanSatSyncMessage>(data);
        ProcessReceivedMessage(syncMessage);
    }

    #endregion

    #region Nested Types

    [Serializable]
    public class ScanSatSyncMessage
    {
        public string Body { get; set; }

        public string Map { get; set; }
    }

    #endregion
}

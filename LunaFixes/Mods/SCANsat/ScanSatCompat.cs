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
    #region Constants

    private const string SyncMessageId = "SCANsatSync";
    private const string ChangeMessageId = "SCANsatChange";

    #endregion

    #region Fields

    private static MethodInfo finishRegistrationMethod;
    private static Type scanSatType;
    private static Type scanVesselType;
    private static Type scanTypeType;

    private bool _keepAlive;
    private ModMessageHandler _modMessageHandler;
    private MethodInfo _getDataMethod;
    private PropertyInfo _getAllDataMethod;
    private MethodInfo _serializeMethod;
    private PropertyInfo _coverageProp;
    private PropertyInfo _bodyProp;
    private MethodInfo _deserializeMethod;
    private int _syncInterval;
    private MethodInfo registerSensorTempMethod;
    private MethodInfo unregisterSensorMethod;
    private FieldInfo tempIdsField;
    private FieldInfo knownVesselsField;
    private FieldInfo vesselField;
    private MethodInfo tryGetValueField;

    #endregion

    #region Properties

    public override string PackageName => "SCANsat";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        _modMessageHandler = modMessageHandler;

        var scanControllerType = AccessTools.TypeByName("SCANsat.SCANcontroller");
        ReflectScanSatTypes(scanControllerType);

        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "scanFromAllVessels"),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixScan)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "OnSave", [typeof(ConfigNode)]),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixOnSave)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "Update"),
                                        postfix: new HarmonyMethod(typeof(ScanSatCompat), nameof(PostfixUpdate)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanControllerType, "finishRegistration", [typeof(Guid)]),
                                        new HarmonyMethod(typeof(ScanSatCompat), nameof(PrefixFinishRegistration)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanSatType, "startScan"),
                                        postfix: new HarmonyMethod(typeof(ScanSatCompat), nameof(PostfixStartScan)));
        LunaFixes.HarmonyInstance.Patch(AccessTools.Method(scanSatType, "stopScan"),
                                        postfix: new HarmonyMethod(typeof(ScanSatCompat), nameof(PostfixStopScan)));

        // add a custom scenario handler for map progress
        var intervalString = node.GetValue("SCANsatSyncInterval");

        if (!int.TryParse(intervalString, out _syncInterval))
            _syncInterval = 5;

        _keepAlive = true;
        LunaFixes.Singleton.StartCoroutine(ScanSatSync());
        modMessageHandler.RegisterModMessageListener<ScanSatSyncMessage>(SyncMessageId, OnSyncMessageReceived);
        modMessageHandler.RegisterModMessageListener<ScanSatScannerChangeMessage>(ChangeMessageId, OnChangeMessageReceived);
    }

    public override void Destroy()
    {
        _keepAlive = false;
        LunaFixes.Singleton.StopCoroutine(ScanSatSync());
        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private static void PostfixStartScan(ref object __instance)
    {
        if (IsPrimaryPlayer())
            return;

        var message = CreateFromScanSatModule(ref __instance);
        message.Loaded = true;

        ModMessageHandler.Instance.SendReliableMessage(ChangeMessageId, message);
    }

    private static void PostfixStopScan(ref object __instance)
    {
        if (IsPrimaryPlayer())
            return;

        var message = CreateFromScanSatModule(ref __instance);
        message.Loaded = false;

        ModMessageHandler.Instance.SendReliableMessage(ChangeMessageId, message);
    }

    private static ScanSatScannerChangeMessage CreateFromScanSatModule(ref object __instance)
    {
        var partModule = __instance as PartModule;

        return new ScanSatScannerChangeMessage
        {
            Vessel = partModule?.vessel.id ?? Guid.Empty,
            Sensor = (int)scanSatType.GetField("sensorType").GetValue(__instance),
            Fov = (float)scanSatType.GetField("fov").GetValue(__instance),
            MinAlt = (float)scanSatType.GetField("min_alt").GetValue(__instance),
            MaxAlt = (float)scanSatType.GetField("max_alt").GetValue(__instance),
            BestAlt = (float)scanSatType.GetField("best_alt").GetValue(__instance),
            RequireLight = (bool)scanSatType.GetField("requireLight").GetValue(__instance)
        };
    }

    private static void PostfixUpdate(object __instance, List<Guid> ___tempIDs)
    {
        // not all vessels loaded
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

    private void ReflectScanSatTypes(Type scanControllerType)
    {
        finishRegistrationMethod = scanControllerType.GetMethod("finishRegistration", BindingFlags.NonPublic | BindingFlags.Instance);
        _getDataMethod = scanControllerType.GetMethod("getData", [typeof(string)]);
        _getAllDataMethod = scanControllerType.GetProperty("GetAllData");

        scanSatType = AccessTools.TypeByName("SCANsat.SCAN_PartModules.SCANsat");
        scanVesselType = AccessTools.TypeByName("SCANvessel");
        scanTypeType = AccessTools.TypeByName("SCANtype");
        registerSensorTempMethod = scanControllerType.GetMethod("registerSensorTemp", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        unregisterSensorMethod = scanControllerType.GetMethod("unregisterSensor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        tempIdsField = scanControllerType.GetField("tempIDs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        knownVesselsField = scanControllerType.GetField("knownVessels", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        vesselField = scanVesselType.GetField("vessel");

        var dicType = typeof(DictionaryValueList<,>).MakeGenericType(typeof(Guid), scanVesselType);
        tryGetValueField = dicType.GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public);

        var scanDataType = AccessTools.TypeByName("SCANsat.SCAN_Data.SCANdata");
        _serializeMethod = scanDataType.GetMethod("shortSerialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        _coverageProp = scanDataType.GetProperty("Coverage");
        _bodyProp = scanDataType.GetProperty("Body");
        _deserializeMethod = scanDataType.GetMethod("shortDeserialize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    private IEnumerator ScanSatSync()
    {
        var previousValues = new Dictionary<string, string>();

        while (_keepAlive)
        {
            yield return new WaitForSeconds(_syncInterval);

            try
            {
                var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == "SCANcontroller");

                if (!scanController || !IsPrimaryPlayer())
                    continue;

                if (_getAllDataMethod.GetValue(scanController) is not IEnumerable scanDataList)
                    continue;

                // For the server, the scenario data is updated as should be. However, without an additional system other clients won't know about the scan updates
                foreach (var scanData in scanDataList)
                {
                    var coverage = _coverageProp.GetValue(scanData) as short[,];
                    var body = _bodyProp.GetValue(scanData) as CelestialBody;

                    if (!body || coverage == null)
                        continue;

                    var serializedData = _serializeMethod.Invoke(scanData, []) as string;

                    if (previousValues.TryGetValue(body.bodyName, out var previousValue) && previousValue.Equals(serializedData))
                        continue;

                    previousValues[body.bodyName] = serializedData;

                    var messageToSend = new ScanSatSyncMessage
                    {
                        Body = body.bodyName,
                        Map = serializedData
                    };

                    _modMessageHandler.SendReliableMessage(SyncMessageId, messageToSend);
                }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }

    private void OnChangeMessageReceived(ScanSatScannerChangeMessage message)
    {
        // Refresh scan controller every time to account for scene changes
        var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == "SCANcontroller");

        if (scanController == null || !IsPrimaryPlayer())
            return;

        var sensorShort = Enum.ToObject(scanTypeType, message.Sensor);

        if (message.Loaded && tempIdsField.GetValue(scanController) is IList tempIds)
        {
            registerSensorTempMethod?.Invoke(scanController, [
                message.Vessel, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                message.RequireLight
            ]);
            tempIds.Add(message.Vessel);
        }
        else
        {
            var args = new[]
            {
                message.Vessel, Activator.CreateInstance(scanVesselType)
            };
            var knownVessels = knownVesselsField.GetValue(scanController);
            tryGetValueField?.Invoke(knownVessels, args);
            var vesselObj = vesselField.GetValue(args[1]);

            unregisterSensorMethod?.Invoke(scanController, [
                vesselObj, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                message.RequireLight
            ]);
        }
    }

    private void OnSyncMessageReceived(ScanSatSyncMessage message)
    {
        // Refresh scan controller every time to account for scene changes
        var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == "SCANcontroller");

        if (scanController == null || IsPrimaryPlayer())
            return;

        var scanData = _getDataMethod.Invoke(scanController, [message.Body]);
        _deserializeMethod.Invoke(scanData, [message.Map]);
    }

    #endregion

    #region Nested Types

    [Serializable]
    public class ScanSatSyncMessage
    {
        public string Body { get; set; }

        public string Map { get; set; }
    }

    [Serializable]
    public class ScanSatScannerChangeMessage
    {
        public bool Loaded { get; set; }

        public Guid Vessel { get; set; }

        public int Sensor { get; set; }

        public float Fov { get; set; }

        public float MinAlt { get; set; }

        public float MaxAlt { get; set; }

        public float BestAlt { get; set; }

        public bool RequireLight { get; set; }
    }

    #endregion
}

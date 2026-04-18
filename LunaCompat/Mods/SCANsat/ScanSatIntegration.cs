using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using JetBrains.Annotations;

using LmpClient.Systems.VesselPositionSys;
using LmpClient.Systems.VesselProtoSys;

using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using UnityEngine;

using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;

namespace LunaCompat.Mods.SCANsat;

[UsedImplicitly]
internal class ScanSatIntegration : ClientModIntegration
{
    #region Fields

    private static ReflectedType scanTypeType;
    private static ReflectedType scanDataType;
    private static ReflectedType scanControllerType;
    private static ReflectedType scanSatType;
    private static ReflectedType scanVesselType;
    private static ReflectedType scanUtilType;

    private bool _keepAlive;
    private int _syncInterval;
    private MethodInfo _tryGetValueField;

    #endregion

    #region Constructors

    public ScanSatIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => ScanSatPackageName;

    private static string ScanSatPackageName => "SCANsat";

    #endregion

    #region Public Methods

    /// <summary>
    /// TODO: This whole system can break when conflicting with the normal scenario sync. Rewrite to use server plugin.
    /// SCANsat compatibility covers multiple parts:
    /// - Only allow background scanning for one client, see <see cref="PrefixScan" />
    /// - Block saving and vessel registration while LMP is still loading, see <see cref="PrefixOnSave" />
    /// - Sync scan data regularly from the primary client to all others, see <see cref="ScanSatSync" />
    /// - Sync changes in loaded vessels of other players, see <see cref="PostfixStartScan" /> and
    /// <see cref="PostfixStopScan" />
    /// </summary>
    public override void Setup()
    {
        ReflectScanSatTypes();

        LunaCompat.HarmonyInstance.Patch(scanControllerType.Method("scanFromAllVessels"), new HarmonyMethod(typeof(ScanSatIntegration), nameof(PrefixScan)));
        LunaCompat.HarmonyInstance.Patch(scanControllerType.Method("OnSave"), new HarmonyMethod(typeof(ScanSatIntegration), nameof(PrefixOnSave)));
        LunaCompat.HarmonyInstance.Patch(scanControllerType.Method("Update"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixUpdate)));
        LunaCompat.HarmonyInstance.Patch(scanControllerType.Method("finishRegistration"),
                                         new HarmonyMethod(typeof(ScanSatIntegration), nameof(PrefixFinishRegistration)));

        LunaCompat.HarmonyInstance.Patch(scanSatType.Method("startScan"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixStartScan)));
        LunaCompat.HarmonyInstance.Patch(scanSatType.Method("stopScan"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixStopScan)));

        // add unknown bodies for vessels
        LunaCompat.HarmonyInstance.Patch(scanUtilType.Method("getData", [typeof(CelestialBody)]),
                                         prefix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PrefixGetData)));

        var intervalString = _settingsProvider.GetValue(PackageName, "SyncInterval", 5);

        if (!int.TryParse((string)intervalString, out _syncInterval))
            _syncInterval = 5;

        _keepAlive = true;
        LunaCompat.Singleton.StartCoroutine(ScanSatSync());
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatSyncMessage>(OnSyncMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatScannerChangeMessage>(OnChangeMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();

        _keepAlive = false;
        LunaCompat.Singleton.StopCoroutine(ScanSatSync());

        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatSyncMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatScannerChangeMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void PostfixStartScan(ref object __instance)
    {
        try
        {
            var message = CreateFromScanSatModule(ref __instance);
            message.Loaded = true;

            VesselPositionSystem.Singleton.ForceUpdateVesselPosition(message.Vessel);

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }
    }

    private static void PostfixStopScan(ref object __instance)
    {
        try
        {
            var message = CreateFromScanSatModule(ref __instance);
            message.Loaded = false;

            VesselPositionSystem.Singleton.ForceUpdateVesselPosition(message.Vessel);

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }
    }

    private static ScanSatScannerChangeMessage CreateFromScanSatModule(ref object __instance)
    {
        var partModule = __instance as PartModule;

        return new ScanSatScannerChangeMessage
        {
            Vessel = partModule?.vessel.id ?? Guid.Empty,
            Sensor = (int)scanSatType.GetField("sensorType", __instance),
            Fov = (float)scanSatType.GetField("fov", __instance),
            MinAlt = (float)scanSatType.GetField("min_alt", __instance),
            MaxAlt = (float)scanSatType.GetField("max_alt", __instance),
            BestAlt = (float)scanSatType.GetField("best_alt", __instance),
            RequireLight = (bool)scanSatType.GetField("requireLight", __instance)
        };
    }

    private static void PostfixUpdate(object __instance, List<Guid> ___tempIDs)
    {
        try
        {
            // not all vessels loaded
            if (!VesselProtoSystem.Singleton.VesselProtos.All(x => x.Value.IsEmpty))
                return;

            var toRem = new List<Guid>();

            for (var i = ___tempIDs.Count - 1; i >= 0; i--)
            {
                if (FlightGlobals.Vessels.Any(a => a.id == ___tempIDs[i]))
                {
                    scanControllerType.Invoke("finishRegistration", __instance, [___tempIDs[i]]);
                    toRem.Add(___tempIDs[i]);
                }
            }

            foreach (var id in toRem)
                ___tempIDs.Remove(id);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }
    }

    private static bool PrefixGetData(CelestialBody body)
    {
        try
        {
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            if (!scanController || scanControllerType.GetProperty("GetAllData", scanController) is not IEnumerable scanDataList)
                return true;

            var createNewData = true;

            foreach (var scanData in scanDataList)
            {
                if (ReferenceEquals(scanDataType.GetField("body", scanData), body))
                    createNewData = false;
            }

            if (createNewData)
            {
                Logger.Instance.Warning($"Creating missing data for {body.name}", ScanSatPackageName);
                CreateScanDataForBody(scanController, body.name);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }

        return true;
    }

    private static bool PrefixOnSave(ConfigNode __0)
    {
        try
        {
            if (!__0.HasData)
                return false;

            if (VesselProtoSystem.Singleton.VesselProtos.All(x => x.Value.IsEmpty))
                return true;

            Logger.Instance.Info(
                $"Blocking SCANsat save - vessels are still loading ({VesselProtoSystem.Singleton.VesselProtos.Values.Count}, {VesselProtoSystem.Singleton.VesselProtos.Count(x => !x.Value.IsEmpty)}).",
                ScanSatPackageName);

            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
            return true;
        }
    }

    private static bool PrefixFinishRegistration(Guid __0)
    {
        try
        {
            return FlightGlobals.Vessels.Any(a => a.id == __0);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
            return true;
        }
    }

    private static bool PrefixScan()
    {
        try
        {
            return IsPrimaryPlayer();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
            return true;
        }
    }

    private static void CreateScanDataForBody(ScenarioModule scanController, string body)
    {
        var celestialBody = FlightGlobals.Bodies.FirstOrDefault(x => x.name == body);

        if (!celestialBody)
        {
            Logger.Instance.Warning($"Failed to find matching celestial body ({body}).", ScanSatPackageName);
            return;
        }

        var newScanData = Activator.CreateInstance(scanDataType.Type, BindingFlags.Instance | BindingFlags.NonPublic, null, [celestialBody], null);
        scanControllerType.Invoke("addToBodyData", scanController, [celestialBody, newScanData]);
    }

    private void ReflectScanSatTypes()
    {
        scanControllerType = new ReflectedType("SCANsat.SCANcontroller");
        scanSatType = new ReflectedType("SCANsat.SCAN_PartModules.SCANsat");
        scanVesselType = new ReflectedType("SCANvessel");
        scanTypeType = new ReflectedType("SCANtype");
        scanDataType = new ReflectedType("SCANsat.SCAN_Data.SCANdata");
        scanUtilType = new ReflectedType("SCANsat.SCANUtil");

        var dictType = typeof(DictionaryValueList<,>).MakeGenericType(typeof(Guid), scanVesselType.Type);
        _tryGetValueField = AccessTools.Method(dictType, "TryGetValue");
    }

    private IEnumerator ScanSatSync()
    {
        var previousValues = new Dictionary<string, string>();

        while (_keepAlive)
        {
            yield return new WaitForSeconds(_syncInterval);

            try
            {
                if (!IsPrimaryPlayer())
                    continue;

                var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

                if (!scanController || scanControllerType.GetProperty("GetAllData", scanController) is not IList scanDataList)
                    continue;

                // For the server, the scenario data is updated as should be. However, without an additional system other clients won't know about the scan updates
                foreach (var scanData in scanDataList)
                {
                    var body = scanDataType.GetProperty("Body", scanData) as CelestialBody;

                    if (!body || scanDataType.GetProperty("Coverage", scanData) is not short[,])
                        continue;

                    var serializedData = scanDataType.Invoke("shortSerialize", scanData, []) as string;

                    if (previousValues.TryGetValue(body.bodyName, out var previousValue) && previousValue.Equals(serializedData))
                        continue;

                    _logger.Info($"Sending scan coverage for {body.name}", PackageName);

                    previousValues[body.bodyName] = serializedData;

                    var messageToSend = new ScanSatSyncMessage
                    {
                        Body = body.bodyName,
                        Map = serializedData
                    };

                    ClientMessageHandler.Instance.SendReliableMessage(messageToSend);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, PackageName);
            }
        }
    }

    private void OnChangeMessageReceived(ScanSatScannerChangeMessage message)
    {
        try
        {
            _logger.Info($"Received sensor update {message.Loaded}", PackageName);

            // Refresh scan controller every time to account for scene changes
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            if (scanController == null)
                return;

            var sensorShort = Enum.ToObject(scanTypeType.Type, message.Sensor);

            if (message.Loaded && scanControllerType.GetField("tempIDs", scanController) is IList tempIds)
            {
                scanControllerType.Invoke("registerSensorTemp", scanController, [
                    message.Vessel, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                    message.RequireLight
                ]);
                tempIds.Add(message.Vessel);
            }
            else
            {
                var args = new[]
                {
                    message.Vessel, Activator.CreateInstance(scanVesselType.Type)
                };
                var knownVessels = scanControllerType.GetField("knownVessels", scanController);

                if (knownVessels == null)
                {
                    _logger.Warning($"{nameof(scanControllerType.Type.Name)} known vessel field is null.", PackageName);
                    return;
                }

                if (_tryGetValueField?.Invoke(knownVessels, args) is true)
                {
                    var vesselObj = scanVesselType.GetField("vessel", args[1]);

                    scanControllerType.Invoke("unregisterSensor", scanController, [
                        vesselObj, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                        message.RequireLight
                    ]);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, PackageName);
        }
    }

    private void OnSyncMessageReceived(ScanSatSyncMessage message)
    {
        try
        {
            // Refresh scan controller every time to account for scene changes
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            var scanData = scanControllerType.Invoke("getData", [typeof(string)], scanController, [message.Body]);

            // body does not exist yet
            if (scanData == null)
                CreateScanDataForBody(scanController, message.Body);

            scanDataType.Invoke("shortDeserialize", scanData, [message.Map]);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    #endregion
}

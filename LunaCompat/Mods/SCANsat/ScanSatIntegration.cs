using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using JetBrains.Annotations;

using LmpClient.Systems.Status;
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

    public override string PackageName => "SCANsat";

    #endregion

    #region Public Methods

    /// <summary>
    /// SCANsat compatibility covers multiple parts:
    /// - Only allow background scanning for one client, see <see cref="PrefixScan" />
    /// - Block saving and vessel registration while LMP is still loading, see <see cref="PrefixOnSave" />
    /// - Sync scan data regularly from the primary client to all others, see <see cref="ScanSatSync" />
    /// - Sync changes in loaded vessels of other players to the primary client, see <see cref="PostfixStartScan" /> and
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
            if (IsPrimaryPlayer())
                return;

            var message = CreateFromScanSatModule(ref __instance);
            message.Loaded = true;

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
        }
    }

    private static void PostfixStopScan(ref object __instance)
    {
        try
        {
            if (IsPrimaryPlayer())
                return;

            var message = CreateFromScanSatModule(ref __instance);
            message.Loaded = false;

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
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
            Logger.Instance.Error(ex);
        }
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
                $"Blocking SCANsat save - vessels are still loading ({VesselProtoSystem.Singleton.VesselProtos.Values.Count}, {VesselProtoSystem.Singleton.VesselProtos.Count(x => !x.Value.IsEmpty)}).");

            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
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
            Logger.Instance.Error(ex);
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
            Logger.Instance.Error(ex);
            return true;
        }
    }

    private static bool IsPrimaryPlayer()
    {
        var localPlayer = StatusSystem.Singleton.MyPlayerStatus.PlayerName;
        var players = StatusSystem.Singleton.PlayerStatusList.Values.Select(x => x.PlayerName).Concat([localPlayer]).OrderBy(x => x).ToArray();

        return players.Length <= 1 || players[0] == StatusSystem.Singleton.MyPlayerStatus.PlayerName;
    }

    private void ReflectScanSatTypes()
    {
        scanControllerType = new ReflectedType("SCANsat.SCANcontroller");
        scanSatType = new ReflectedType("SCANsat.SCAN_PartModules.SCANsat");
        scanVesselType = new ReflectedType("SCANvessel");
        scanTypeType = new ReflectedType("SCANtype");
        scanDataType = new ReflectedType("SCANsat.SCAN_Data.SCANdata");

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
                var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

                if (!scanController || !IsPrimaryPlayer())
                    continue;

                if (scanControllerType.GetProperty("GetAllData", scanController) is not IEnumerable scanDataList)
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

                    previousValues[body.bodyName] = serializedData;

                    var messageToSend = new ScanSatSyncMessage
                    {
                        Body = body.bodyName,
                        Map = serializedData
                    };

                    ClientMessageHandler.Instance.SendReliableMessage(messageToSend);
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Error(e);
            }
        }
    }

    private void OnChangeMessageReceived(ScanSatScannerChangeMessage message)
    {
        try
        {
            if (!IsPrimaryPlayer() || FlightGlobals.VesselsLoaded.Any(x => x.id == message.Vessel))
                return;

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
                _tryGetValueField?.Invoke(knownVessels, args);
                var vesselObj = scanVesselType.GetField("vessel", args[1]);

                scanControllerType.Invoke("unregisterSensor", scanController, [
                    vesselObj, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                    message.RequireLight
                ]);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
        }
    }

    private void OnSyncMessageReceived(ScanSatSyncMessage message)
    {
        try
        {
            // Refresh scan controller every time to account for scene changes
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            if (scanController == null || IsPrimaryPlayer())
                return;

            var scanData = scanControllerType.Invoke("getData", scanController, [message.Body]);
            scanDataType.Invoke("shortDeserialize", scanData, [message.Map]);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
        }
    }

    #endregion
}

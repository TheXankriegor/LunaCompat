using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using JetBrains.Annotations;

using LmpClient.Systems.VesselPositionSys;
using LmpClient.Systems.VesselProtoSys;

using LmpCommon;

using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Serializer;

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
    private static ReflectedType scanUiSettingsType;
    private static MethodInfo tryGetValueField;

    private Dictionary<string, string> _previousValues;

    private bool _keepAlive;
    private int _syncInterval;

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
    /// SCANsat compatibility covers multiple parts:
    /// - Sync scan data regularly from the primary client to all others, see <see cref="ScanSatSync" />
    /// - Sync changes in loaded vessels of other players, see <see cref="PostfixStartScan" /> and
    /// <see cref="PostfixStopScan" />
    /// </summary>
    public override void Setup()
    {
        _previousValues = new Dictionary<string, string>();

        ReflectScanSatTypes();

        IgnoredScenarios.IgnoreReceive.Add("SCANcontroller");
        IgnoredScenarios.IgnoreSend.Add("SCANcontroller");

        // scanner changes
        LunaCompat.HarmonyInstance.Patch(scanSatType.Method("startScan"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixStartScan)));
        LunaCompat.HarmonyInstance.Patch(scanSatType.Method("stopScan"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixStopScan)));

        // coverage resets
        LunaCompat.HarmonyInstance.Patch(scanUiSettingsType.Method("ResetCurrent"),
                                         postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixResetCurrent)));
        LunaCompat.HarmonyInstance.Patch(scanUiSettingsType.Method("ResetAll"),
                                         postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixResetAll)));

        // add unknown bodies from other players
        LunaCompat.HarmonyInstance.Patch(scanUtilType.Method("getData", [typeof(CelestialBody)]),
                                         prefix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PrefixGetData)));
        LunaCompat.HarmonyInstance.Patch(scanControllerType.Method("Update"), postfix: new HarmonyMethod(typeof(ScanSatIntegration), nameof(PostfixUpdate)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatRequestDataMessage>(OnAllDataAvailableMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatSyncDataMessage>(OnSyncMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatScannerChangeMessage>(OnChangeScannerMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<ScanSatResetDataMessage>(OnResetScanSatDataMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();

        _previousValues.Clear();
        _keepAlive = false;
        LunaCompat.Singleton.StopCoroutine(ScanSatSync());
        LunaCompat.Singleton.StopCoroutine(UpdateStoredSettings());

        ClientMessageHandler.Instance.HasServerIntegrationChanged -= OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatRequestDataMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatSyncDataMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatScannerChangeMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<ScanSatResetDataMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectScanSatTypes()
    {
        scanControllerType = new ReflectedType("SCANsat.SCANcontroller");
        scanSatType = new ReflectedType("SCANsat.SCAN_PartModules.SCANsat");
        scanVesselType = new ReflectedType("SCANvessel");
        scanTypeType = new ReflectedType("SCANtype");
        scanDataType = new ReflectedType("SCANsat.SCAN_Data.SCANdata");
        scanUtilType = new ReflectedType("SCANsat.SCANUtil");
        scanUiSettingsType = new ReflectedType("SCANsat.SCAN_Unity.SCAN_UI_Settings");

        var dictType = typeof(DictionaryValueList<,>).MakeGenericType(typeof(Guid), scanVesselType.Type);
        tryGetValueField = AccessTools.Method(dictType, "TryGetValue");
    }

    private static void PostfixResetCurrent(ref object __instance)
    {
        try
        {
            var body = scanUiSettingsType.Invoke("getTargetBody", __instance, []) as CelestialBody;
            var dataType = scanUiSettingsType.GetField("_currentDataType", __instance);

            if (!body || dataType == null)
                return;

            Logger.Instance.Info($"Deleting coverage data for body {body.name} and datatype {dataType}", ScanSatPackageName);

            var message = new ScanSatResetDataMessage
            {
                Body = body.name,
                Type = (short)dataType
            };
            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }
    }

    private static void PostfixResetAll(ref object __instance)
    {
        try
        {
            var dataType = scanUiSettingsType.GetField("_currentDataType", __instance);

            if (dataType == null)
                return;

            Logger.Instance.Info($"Deleting all coverage data for datatype {dataType}", ScanSatPackageName);

            var message = new ScanSatResetDataMessage
            {
                Body = ScanSatConstants.AllCelestialBodiesIdentifier,
                Type = (short)dataType
            };
            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, ScanSatPackageName);
        }
    }

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

    private static object CreateScanDataForBody(ScenarioModule scanController, string body)
    {
        var celestialBody = FlightGlobals.Bodies.FirstOrDefault(x => x.name == body);

        if (!celestialBody)
        {
            Logger.Instance.Warning($"Failed to find matching celestial body ({body}).", ScanSatPackageName);
            return null;
        }

        var newScanData = Activator.CreateInstance(scanDataType.Type, BindingFlags.Instance | BindingFlags.NonPublic, null, [celestialBody], null);
        scanControllerType.Invoke("addToBodyData", scanController, [celestialBody, newScanData]);
        return newScanData;
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        UnloadAllData();

        if (!hasServerIntegration)
            return;

        _logger.Info("Requesting all scanner data.", PackageName);

        ClientMessageHandler.Instance.SendReliableMessage(new ScanSatRequestDataMessage(), false);
    }

    private void UnloadAllData()
    {
        var controller = scanControllerType.GetProperty("controller", null);

        if (scanControllerType.GetProperty("GetAllData", controller) is IList allData)
        {
            foreach (var data in allData)
                scanDataType.Invoke("reset", [scanTypeType.Type], data, [short.MaxValue]);
        }
    }

    private void LoadLocalSettings()
    {
        var scanController = scanControllerType.GetProperty("controller", null);

        UpdateSetting("mainMapVisible", false, scanController, scanControllerType);
        UpdateSetting("mainMapColor", true, scanController, scanControllerType);
        UpdateSetting("mainMapTerminator", false, scanController, scanControllerType);
        UpdateSetting("mainMapBiome", false, scanController, scanControllerType);
        UpdateSetting("mainMapMinimized", false, scanController, scanControllerType);

        UpdateSetting("bigMapVisible", false, scanController, scanControllerType);
        UpdateSetting("bigMapColor", true, scanController, scanControllerType);
        UpdateSetting("bigMapTerminator", false, scanController, scanControllerType);
        UpdateSetting("bigMapGrid", true, scanController, scanControllerType);
        UpdateSetting("bigMapOrbit", true, scanController, scanControllerType);
        UpdateSetting("bigMapWaypoint", true, scanController, scanControllerType);
        UpdateSetting("bigMapAnomaly", true, scanController, scanControllerType);
        UpdateSetting("bigMapFlag", true, scanController, scanControllerType);
        UpdateSetting("bigMapLegend", true, scanController, scanControllerType);
        UpdateSetting("bigMapResourceOn", false, scanController, scanControllerType);
        UpdateSetting("bigMapProjection", "Rectangular", scanController, scanControllerType);
        UpdateSetting("bigMapType", "Altimetry", scanController, scanControllerType);
        UpdateSetting("bigMapResource", "Ore", scanController, scanControllerType);
        UpdateSetting("bigMapBody", "Kerbin", scanController, scanControllerType);

        UpdateSetting("zoomMapVesselLock", false, scanController, scanControllerType);
        UpdateSetting("zoomMapColor", true, scanController, scanControllerType);
        UpdateSetting("zoomMapTerminator", false, scanController, scanControllerType);
        UpdateSetting("zoomMapOrbit", true, scanController, scanControllerType);
        UpdateSetting("zoomMapIcons", true, scanController, scanControllerType);
        UpdateSetting("zoomMapLegend", true, scanController, scanControllerType);
        UpdateSetting("zoomMapResourceOn", false, scanController, scanControllerType);
        UpdateSetting("zoomMapType", "Altimetry", scanController, scanControllerType);
        UpdateSetting("zoomMapResource", "Ore", scanController, scanControllerType);
        UpdateSetting("zoomMapState", 0, scanController, scanControllerType);
        UpdateSetting("zoomMapRefresh", 0, scanController, scanControllerType);
        UpdateSetting("zoomMapZoomPersist", false, scanController, scanControllerType);
        UpdateSetting("zoomMapZoom", 10f, scanController, scanControllerType);
        UpdateSetting("overlaySelection", 0, scanController, scanControllerType);
        UpdateSetting("overlayResource", "Ore", scanController, scanControllerType);
    }

    private IEnumerator ScanSatSync()
    {
        while (_keepAlive)
        {
            yield return new WaitForSeconds(_syncInterval);

            try
            {
                var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

                if (!scanController || scanControllerType.GetProperty("GetAllData", scanController) is not IList scanDataList)
                    continue;

                foreach (var scanData in scanDataList)
                {
                    var body = scanDataType.GetProperty("Body", scanData) as CelestialBody;

                    if (!body || scanDataType.GetProperty("Coverage", scanData) is not short[,])
                        continue;

                    var coverageRaw = scanDataType.GetProperty("Coverage", scanData);
                    if (coverageRaw is not short[,] coverage)
                        continue;

                    var asBytes = SerializationUtil.Serialize(coverage, false);
                    var newHash = Common.CalculateSha256Hash(asBytes);

                    if (_previousValues.TryGetValue(body.bodyName, out var previousValue) && previousValue.Equals(newHash))
                        continue;

                    _logger.Info($"Sending scan coverage for {body.name}", PackageName);

                    _previousValues[body.bodyName] = newHash;

                    var messageToSend = new ScanSatSyncDataMessage
                    {
                        Body = body.bodyName,
                        Map = coverage
                    };

                    ClientMessageHandler.Instance.SendReliableMessage(messageToSend);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, PackageName);
            }
        }
    }

    private IEnumerator UpdateStoredSettings()
    {
        while (_keepAlive)
        {
            yield return new WaitForSeconds(_syncInterval);

            try
            {
                if (HighLogic.CurrentGame == null)
                    continue;

                var scanController = scanControllerType.GetProperty("controller", null);

                SaveSetting("mainMapVisible", scanController, scanControllerType);
                SaveSetting("mainMapColor", scanController, scanControllerType);
                SaveSetting("mainMapTerminator", scanController, scanControllerType);
                SaveSetting("mainMapBiome", scanController, scanControllerType);
                SaveSetting("mainMapMinimized", scanController, scanControllerType);

                SaveSetting("bigMapVisible", scanController, scanControllerType);
                SaveSetting("bigMapColor", scanController, scanControllerType);
                SaveSetting("bigMapTerminator", scanController, scanControllerType);
                SaveSetting("bigMapGrid", scanController, scanControllerType);
                SaveSetting("bigMapOrbit", scanController, scanControllerType);
                SaveSetting("bigMapWaypoint", scanController, scanControllerType);
                SaveSetting("bigMapAnomaly", scanController, scanControllerType);
                SaveSetting("bigMapFlag", scanController, scanControllerType);
                SaveSetting("bigMapLegend", scanController, scanControllerType);
                SaveSetting("bigMapResourceOn", scanController, scanControllerType);
                SaveSetting("bigMapProjection", scanController, scanControllerType);
                SaveSetting("bigMapType", scanController, scanControllerType);
                SaveSetting("bigMapResource", scanController, scanControllerType);
                SaveSetting("bigMapBody", scanController, scanControllerType);

                SaveSetting("zoomMapVesselLock", scanController, scanControllerType);
                SaveSetting("zoomMapColor", scanController, scanControllerType);
                SaveSetting("zoomMapTerminator", scanController, scanControllerType);
                SaveSetting("zoomMapOrbit", scanController, scanControllerType);
                SaveSetting("zoomMapIcons", scanController, scanControllerType);
                SaveSetting("zoomMapLegend", scanController, scanControllerType);
                SaveSetting("zoomMapResourceOn", scanController, scanControllerType);
                SaveSetting("zoomMapType", scanController, scanControllerType);
                SaveSetting("zoomMapResource", scanController, scanControllerType);
                SaveSetting("zoomMapState", scanController, scanControllerType);
                SaveSetting("zoomMapRefresh", scanController, scanControllerType);
                SaveSetting("zoomMapZoomPersist", scanController, scanControllerType);
                SaveSetting("zoomMapZoom", scanController, scanControllerType);
                SaveSetting("overlaySelection", scanController, scanControllerType);
                SaveSetting("overlayResource", scanController, scanControllerType);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save settings: {ex}", PackageName);
            }
        }
    }

    private void OnAllDataAvailableMessageReceived(ScanSatRequestDataMessage msg)
    {
        try
        {
            var intervalString = _settingsProvider.GetValue(PackageName, "SyncInterval", 15);

            if (!int.TryParse((string)intervalString, out _syncInterval))
                _syncInterval = 15;
            _keepAlive = true;

            LoadLocalSettings();

            LunaCompat.Singleton.StartCoroutine(ScanSatSync());
            LunaCompat.Singleton.StartCoroutine(UpdateStoredSettings());
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize SCANsat integration: {ex}", PackageName);
        }
    }

    private void OnResetScanSatDataMessageReceived(ScanSatResetDataMessage message)
    {
        try
        {
            if (message.Body == ScanSatConstants.AllCelestialBodiesIdentifier)
            {
                var controller = scanControllerType.GetProperty("controller", null);

                if (scanControllerType.GetProperty("GetAllData", controller) is IList allData)
                {
                    foreach (var data in allData)
                        scanDataType.Invoke("reset", [scanTypeType.Type], data, [message.Type]);
                }
            }
            else
            {
                var body = FlightGlobals.Bodies.SingleOrDefault(x => x.name == message.Body);

                if (body)
                {
                    var data = scanUtilType.Invoke("getData", [typeof(CelestialBody)], null, [body]);
                    scanDataType.Invoke("reset", [scanTypeType.Type], data, [message.Type]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnChangeScannerMessageReceived(ScanSatScannerChangeMessage message)
    {
        try
        {
            _logger.Info($"Received sensor update for {message.Vessel} (Scanner active: {message.Loaded})", PackageName);

            // Refresh scan controller every time to account for scene changes
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            if (scanController == null)
                return;

            var sensorShort = Enum.ToObject(scanTypeType.Type, message.Sensor);

            if (message.Loaded)
            {
                scanControllerType.Invoke("registerSensorTemp", scanController, [
                    message.Vessel, sensorShort, message.Fov, message.MinAlt, message.MaxAlt, message.BestAlt,
                    message.RequireLight
                ]);

                scanControllerType.Invoke("finishRegistration", scanController, [message.Vessel]);
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

                if (tryGetValueField?.Invoke(knownVessels, args) is true)
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

    private void OnSyncMessageReceived(ScanSatSyncDataMessage message)
    {
        try
        {
            _logger.Debug($"Received scan coverage update for {message.Body}", PackageName);

            // Refresh scan controller every time to account for scene changes
            var scanController = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == scanControllerType.Type.Name);

            // body might not exist yet
            var scanData = scanControllerType.Invoke("getData", [typeof(string)], scanController, [message.Body]) ??
                           CreateScanDataForBody(scanController, message.Body);

            var coverageRaw = scanDataType.GetProperty("Coverage", scanData);

            if (coverageRaw is not short[,] coverage)
            {
                _logger.Error("Failed to read SCANsat coverage data", PackageName);
                return;
            }

            var mergedCoverage = ScanSatCommon.MergeCoverageData(coverage, message.Map);

            var asBytes = SerializationUtil.Serialize(mergedCoverage, false);
            var newHash = Common.CalculateSha256Hash(asBytes);

            _previousValues[message.Body] = newHash;
            scanDataType.SetProperty("Coverage", scanData, mergedCoverage);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    #endregion
}

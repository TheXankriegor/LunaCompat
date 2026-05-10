using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using HarmonyLib;

using JetBrains.Annotations;

using LmpCommon;

using LunaCompat.Mods.KerbalKonstructs;
using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using UnityEngine;

using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;

namespace LunaCompat.Mods.KerbalColonies;

[UsedImplicitly]
internal class KerbalColoniesIntegration : ClientModIntegration
{
    #region Constants

    public const string KerbalColoniesPackageName = "KerbalColonies";

    #endregion

    #region Fields

    private static ReflectedType kCCabWindowType;
    private static ReflectedType kerbalKonstructsApiType;
    private static ReflectedType colonyBuildQueueType;
    private static ReflectedType kCProductionFacilityType;
    private static ReflectedType kCGameParametersType;
    private static ReflectedType colonyClassType;
    private static ReflectedType kCFacilityBaseType;
    private static ReflectedType kCFacilityWindowBaseType;
    private static ReflectedType kCWindowManagerType;
    private static ReflectedType colonyBuildingType;
    private static ReflectedType configurationType;

    private static IReadOnlyDictionary<string, HashSet<int>> lastOpenColonies;
    private static Dictionary<string, string[]> colonyLineCache;

    private static bool initialized;

    private int _syncInterval;
    private bool _keepAlive;

    #endregion

    #region Constructors

    public KerbalColoniesIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
        IgnoredScenarios.IgnoreReceive.Add("Configuration");
        IgnoredScenarios.IgnoreSend.Add("Configuration");
    }

    #endregion

    #region Properties

    public override string PackageName => KerbalColoniesPackageName;

    #endregion

    #region Public Methods

    public override void Setup()
    {
        colonyLineCache = new Dictionary<string, string[]>();

        ReflectKerbalColoniesTypes();

        LunaCompat.HarmonyInstance.Patch(configurationType.Method("OnSave"),
                                         postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixSaveConfiguration)));
        LunaCompat.HarmonyInstance.Patch(kCWindowManagerType.Method("CloseWindow"),
                                         postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixCloseWindow)));
        LunaCompat.HarmonyInstance.Patch(colonyBuildingType.Method("PlaceNewGroupSave"),
                                         postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixPlaceNewGroupSave)));

        // prevent error states from updates during loads
        LunaCompat.HarmonyInstance.Patch(kCProductionFacilityType.Method("UpdateSharedNode"),
                                         prefix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PrefixUpdateSharedNode)));
        LunaCompat.HarmonyInstance.Patch(colonyClassType.Method("UpdateColony"),
                                         prefix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PrefixUpdateColony)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalColoniesRequestColoniesMessage>(OnAllColoniesReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalColoniesChangeColonyMessage>(OnChangeColonyMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalColoniesSettingsValueMessage>(OnSettingsValueMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();
        initialized = false;
        _keepAlive = false;
        colonyLineCache.Clear();
        lastOpenColonies = null;

        ClientMessageHandler.Instance.HasServerIntegrationChanged -= OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalColoniesRequestColoniesMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalColoniesChangeColonyMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalColoniesSettingsValueMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectKerbalColoniesTypes()
    {
        configurationType = new ReflectedType("KerbalColonies.Settings.Configuration");
        colonyBuildingType = new ReflectedType("KerbalColonies.ColonyBuilding");
        kCWindowManagerType = new ReflectedType("KerbalColonies.UI.KCWindowManager");
        kCFacilityWindowBaseType = new ReflectedType("KerbalColonies.UI.KCFacilityWindowBase");
        kCCabWindowType = new ReflectedType("KerbalColonies.colonyFacilities.CabFacility.KC_CAB_Window");
        kCFacilityBaseType = new ReflectedType("KerbalColonies.colonyFacilities.KCFacilityBase");
        colonyClassType = new ReflectedType("KerbalColonies.colonyClass");
        kCGameParametersType = new ReflectedType("KerbalColonies.Settings.KCGameParameters");
        kCProductionFacilityType = new ReflectedType("KerbalColonies.colonyFacilities.ProductionFacility.KCProductionFacility");

        kerbalKonstructsApiType = new ReflectedType("KerbalKonstructs.API");

        var queueInformationType = AccessTools.TypeByName("KerbalColonies.ColonyBuilding+QueueInformation");
        colonyBuildQueueType = new ReflectedType(typeof(Queue<>).MakeGenericType(queueInformationType));
    }

    private static bool PrefixUpdateSharedNode(ref object colony)
    {
        try
        {
            if (colony == null)
            {
                Logger.Instance.Warning("Preventing update for invalid colony", KerbalColoniesPackageName);
                return false;
            }

            var colonyObj = colony;
            var allowSave = true;

            if (kCProductionFacilityType.GetProperty("ConstructingFacilities", null) is IDictionary constructingFacilities)
                CheckDictionary(constructingFacilities, "ConstructingFacilities");
            if (kCProductionFacilityType.GetProperty("ConstructedFacilities", null) is IDictionary constructedFacilities)
                CheckDictionary(constructedFacilities, "ConstructedFacilities");
            if (kCProductionFacilityType.GetProperty("UpgradingFacilities", null) is IDictionary upgradingFacilities)
                CheckDictionary(upgradingFacilities, "UpgradingFacilities");
            if (kCProductionFacilityType.GetProperty("UpgradedFacilities", null) is IDictionary upgradedFacilities)
                CheckDictionary(upgradedFacilities, "UpgradedFacilities");

            return allowSave;

            void CheckDictionary(IDictionary dict, string dictName)
            {
                if (dict.Contains(colonyObj))
                    return;

                Logger.Instance.Warning($"Colony {colonyObj} missing in facility dictionary {dictName}.", KerbalColoniesPackageName);
                allowSave = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Error in {nameof(PrefixUpdateSharedNode)}: {ex}", KerbalColoniesPackageName);
            return false;
        }
    }

    private static bool PrefixUpdateColony()
    {
        try
        {
            if (!initialized)
                Logger.Instance.Debug("Preventing colony update during load.", KerbalColoniesPackageName);

            return initialized;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Error in {nameof(PrefixUpdateColony)}: {ex}", KerbalColoniesPackageName);
            return false;
        }
    }

    private static void PostfixCloseWindow(ref object drawfunct)
    {
        try
        {
            if (!initialized)
                return;

            if (!ClientMessageHandler.Instance.HasServerIntegration)
            {
                Logger.LogServerPluginMissing(KerbalColoniesPackageName);
                return;
            }

            if (!TryGetColonyFromWindow(drawfunct, out var colony, out var facilityId))
                return;

            Logger.Instance.Debug($"Closing window: {drawfunct}.", KerbalColoniesPackageName);

            lastOpenColonies = new Dictionary<string, HashSet<int>>
            {
                {
                    colony, [facilityId]
                }
            };
            SaveColonyScenario();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to handle closed KC window: {ex}", KerbalColoniesPackageName);
        }
    }

    private static void PostfixPlaceNewGroupSave()
    {
        try
        {
            SaveColonyScenario();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to invoke colony update: {ex}", KerbalColoniesPackageName);
        }
    }

    private static void PostfixSaveConfiguration(ref object node)
    {
        try
        {
            if (!initialized)
                return;

            if (!ClientMessageHandler.Instance.HasServerIntegration)
            {
                Logger.LogServerPluginMissing(KerbalColoniesPackageName);
                return;
            }

            if (node is not ConfigNode configNode)
            {
                Logger.Instance.Error("Saved object was not a config node.", KerbalColoniesPackageName);
                return;
            }

            var colonyNode = configNode.GetNode("colonyNode");

            foreach (var body in colonyNode.GetNodes())
            {
                foreach (var colony in body.GetNodes())
                {
                    var name = colony.GetValue("name");
                    var colonyId = $"{body.name}_{name}";
                    var colonyStr = colony.ToString();

                    // created and not open
                    if (colonyLineCache.ContainsKey(colonyId) && (lastOpenColonies == null || !lastOpenColonies.ContainsKey(colonyId)))
                    {
                        Logger.Instance.Debug($"Skipping update for {colonyId}.", KerbalColoniesPackageName);
                        continue;
                    }

                    var lines = colonyStr.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
                    var differs = CheckColonyForDifferences(colonyId, lines);

                    if (!differs)
                        return;

                    colonyLineCache[colonyId] = lines;

                    Logger.Instance.Info($"Sending update for {colonyId}.", KerbalColoniesPackageName);

                    var msg = new KerbalColoniesChangeColonyMessage
                    {
                        Body = body.name,
                        ColonyName = name,
                        Content = colonyStr
                    };

                    ClientMessageHandler.Instance.SendReliableMessage(msg);
                }
            }

            lastOpenColonies = null;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to send colony update: {ex}", KerbalColoniesPackageName);
        }
    }

    private static bool CheckColonyForDifferences(string colonyId, string[] lines)
    {
        if (!colonyLineCache.TryGetValue(colonyId, out var cachedLines))
            return true;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var cachedLine = cachedLines[i];

            var segments = line.Split(['='], 2);
            var cachedSegments = cachedLine.Split(['='], 2);

            if (segments[0].Trim() != cachedSegments[0].Trim())
            {
                Logger.Instance.Debug($"Cached colony difference in: {line} (previous: {cachedLine})", KerbalColoniesPackageName);
                return true;
            }

            if (segments.Length != 2 || cachedSegments.Length != 2)
                continue;

            if (float.TryParse(segments[1].Trim(), out var value) && float.TryParse(cachedSegments[1].Trim(), out var cachedValue))
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value == cachedValue)
                    continue;

                // ignore skip if value differs only a little, e.g. time, charge
                var diff = Math.Abs(value - cachedValue);
                if (value == 0 || cachedValue == 0 || diff > value / 100)
                    return true;
            }
            else
            {
                if (segments[1].Trim() != cachedSegments[1].Trim())
                {
                    Logger.Instance.Debug($"Cached colony difference in: {line} (previous: {cachedLine})", KerbalColoniesPackageName);
                    return true;
                }
            }
        }

        return true;
    }

    private static void SaveColonyScenario()
    {
        if (!initialized)
            return;

        if (!ClientMessageHandler.Instance.HasServerIntegration)
        {
            Logger.LogServerPluginMissing(KerbalColoniesPackageName);
            return;
        }

        var colonyScenario = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == configurationType.Type.Name);

        if (!colonyScenario)
        {
            Logger.Instance.Warning("No scenario module found to save.", KerbalColoniesPackageName);
            return;
        }

        var configNode = new ConfigNode();
        colonyScenario.Save(configNode);
    }

    private static bool TryGetColonyFromWindow(object window, out string colony, out int facilityId)
    {
        colony = null;
        facilityId = 0;

        object facility = null;
        if (kCCabWindowType.Type.IsAssignableFrom(window.GetType()))
            facility = kCCabWindowType.GetField("CABFacility", window);

        if (kCFacilityWindowBaseType.Type.IsAssignableFrom(window.GetType()))
            facility = kCFacilityWindowBaseType.GetField("facility", window);

        if (facility == null)
            return false;

        var colonyObj = kCFacilityBaseType.GetProperty("Colony", facility);

        if (colonyObj == null)
            return false;

        facilityId = (int)kCFacilityBaseType.GetField("id", facility);
        var name = colonyClassType.GetProperty("Name", colonyObj) as string;
        var body = colonyClassType.GetProperty("BodyName", colonyObj) as string;
        colony = $"{body}_{name}";

        return true;
    }

    private static bool CheckFacilityLoadedState(ConfigNode facilityNode, out string uuid)
    {
        uuid = string.Empty;

        var launchpadNode = facilityNode.GetNode("facilityNode")?.GetNode("LaunchpadFacility");

        if (launchpadNode != null)
        {
            foreach (var launchSide in launchpadNode.GetNodes())
            {
                uuid = launchSide.GetValue("launchSiteUUID");

                if (uuid != null)
                {
                    if (kerbalKonstructsApiType.Invoke("getStaticInstanceByUUID", null, [uuid]) == null)
                        return false;
                }
            }
        }

        return true;
    }

    private static bool CheckCanLoadColony(ConfigNode node, out string uuid)
    {
        uuid = string.Empty;
        var cabNode = node.GetNode("CAB");

        if (cabNode != null)
        {
            if (!CheckFacilityLoadedState(cabNode, out uuid))
                return false;
        }

        foreach (var facilityNode in node.GetNodes("facility"))
        {
            if (!CheckFacilityLoadedState(facilityNode, out uuid))
                return false;
        }

        return true;
    }

    private static bool HasColonyWindowOpen()
    {
        var coloniesToUpdate = new Dictionary<string, HashSet<int>>();

        var singleton = kCWindowManagerType.GetField("instance", null);
        var windows = kCWindowManagerType.GetField("openWindows", singleton) as IList;

        if (windows == null)
            return false;

        // check current windows for a colony window
        foreach (var window in windows)
        {
            if (TryGetColonyFromWindow(window, out var colony, out var facility))
            {
                if (coloniesToUpdate.TryGetValue(colony, out var facilities))
                    facilities.Add(facility);
                else
                {
                    coloniesToUpdate.Add(colony, new HashSet<int>
                    {
                        facility
                    });
                }
            }
        }

        lastOpenColonies = coloniesToUpdate;

        return coloniesToUpdate.Count != 0;
    }

    private static void CloseUiIfOpen(string colonyName, string body)
    {
        var singleton = kCWindowManagerType.GetField("instance", null);

        if (kCWindowManagerType.GetField("openWindows", singleton) is not IList windows)
            return;

        var colonyId = $"{body}_{colonyName}";

        var toUnload = new List<object>();

        // check current windows for a colony window
        foreach (var window in windows)
        {
            if (TryGetColonyFromWindow(window, out var colony, out _) && colony == colonyId)
                toUnload.Add(window);
        }

        foreach (var window in toUnload)
            kCFacilityWindowBaseType.Invoke("Close", window, []);
    }

    private static void LoadColony(string colonyName, string body, string colonyStr)
    {
        // check if all uuids are loaded. Otherwise, delay and retry.
        var node = ConfigNode.Parse(colonyStr);

        if (!CheckCanLoadColony(node, out var uuid))
        {
            Logger.Instance.Warning($"Colony {colonyName} ({body}) cannot be loaded because it is missing static instance '{uuid}'", KerbalColoniesPackageName);
            LunaCompat.Singleton.StartCoroutine(RetryLoadColony());
            return;
        }

        UnloadColony(colonyName, body);

        Logger.Instance.Debug($"Loading colony {colonyName} ({body})", KerbalColoniesPackageName);

        var colonyId = $"{body}_{colonyName}";
        colonyLineCache[colonyId] = colonyStr.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        if (configurationType.GetField("colonyDictionary", null) is not IDictionary colonyDict)
        {
            Logger.Instance.Warning($"Colony {colonyName} ({body}) cannot be loaded because KC is not initialized.", KerbalColoniesPackageName);
            LunaCompat.Singleton.StartCoroutine(RetryLoadColony());
            return;
        }

        if (!colonyDict.Contains(body))
        {
            var listType = typeof(List<>).MakeGenericType(colonyClassType.Type);
            colonyDict.Add(body, Activator.CreateInstance(listType));
        }

        var newColony = Activator.CreateInstance(colonyClassType.Type, node.GetNode("colonyClass"));

        if (configurationType.GetField("GroupFacilities", null) is IDictionary groupFacilities)
        {
            var cab = colonyClassType.GetProperty("CAB", newColony);
            UpdateKkGroups(groupFacilities, cab);

            if (colonyClassType.GetProperty("Facilities", newColony) is IList facilities)
            {
                foreach (var facility in facilities)
                    UpdateKkGroups(groupFacilities, facility);
            }
        }

        if (colonyDict[body] is IList colonies)
            colonies.Add(newColony);

        // reapply global dictionaries
        colonyClassType.Invoke("UpdateColony", newColony, []);
        return;

        static void UpdateKkGroups(IDictionary groupFacilitiesDict, object facility)
        {
            var groups = kCFacilityBaseType.GetField("KKgroups", facility);

            if (groups is not IList groupList)
                return;

            foreach (var group in groupList)
                groupFacilitiesDict.Add(group, facility);
        }

        IEnumerator RetryLoadColony()
        {
            yield return new WaitForSeconds(1);

            CloseUiIfOpen(colonyName, body);
            LoadColony(colonyName, body, colonyStr);
        }
    }

    private static void UnloadColony(string name, string body)
    {
        var toRemove = new List<object>();

        if (kCProductionFacilityType.GetProperty("ConstructingFacilities", null) is IDictionary constructingFacilities)
            RemoveFacilities(constructingFacilities);
        if (kCProductionFacilityType.GetProperty("ConstructedFacilities", null) is IDictionary constructedFacilities)
            RemoveFacilities(constructedFacilities);
        if (kCProductionFacilityType.GetProperty("UpgradingFacilities", null) is IDictionary upgradingFacilities)
            RemoveFacilities(upgradingFacilities);
        if (kCProductionFacilityType.GetProperty("UpgradedFacilities", null) is IDictionary upgradedFacilities)
            RemoveFacilities(upgradedFacilities);

        // KCgroups should not matter for v4 KC systems
        if (configurationType.GetField("colonyDictionary", null) is IDictionary colonyDictionary && colonyDictionary.Contains(body) &&
            colonyDictionary[body] is IList colonies)
        {
            foreach (var colonyObj in colonies)
            {
                if (colonyObj == null || IsMatchingColony(colonyObj))
                    toRemove.Add(colonyObj);
            }

            foreach (var colonyObj in toRemove)
                colonies.Remove(colonyObj);

            toRemove.Clear();
        }

        if (configurationType.GetField("GroupFacilities", null) is IDictionary groupFacilities)
        {
            foreach (var key in groupFacilities.Keys)
            {
                var colonyObj = kCFacilityBaseType.GetProperty("Colony", groupFacilities[key]);

                if (colonyObj == null || IsMatchingColony(colonyObj))
                    toRemove.Add(key);
            }

            foreach (var key in toRemove)
                groupFacilities.Remove(key);

            toRemove.Clear();
        }

        return;

        void RemoveFacilities(IDictionary dictionary)
        {
            foreach (var colony in dictionary.Keys)
            {
                if (colony == null || IsMatchingColony(colony))
                    toRemove.Add(colony);
            }

            foreach (var facility in toRemove)
                dictionary.Remove(facility);

            toRemove.Clear();
        }

        bool IsMatchingColony(object colonyObj)
        {
            var colonyName = colonyClassType.GetProperty("Name", colonyObj) as string;

            if (name != colonyName)
                return false;

            var colonyBody = colonyClassType.GetProperty("BodyName", colonyObj) as string;
            if (body != colonyBody)
                return false;

            return true;
        }
    }

    private static void UnloadAllColonies()
    {
        Logger.Instance.Info("Unloading all colonies.", KerbalColoniesPackageName);

        if (kCProductionFacilityType.GetProperty("ConstructingFacilities", null) is IDictionary constructingFacilities)
            constructingFacilities.Clear();
        if (kCProductionFacilityType.GetProperty("ConstructedFacilities", null) is IDictionary constructedFacilities)
            constructedFacilities.Clear();
        if (kCProductionFacilityType.GetProperty("UpgradingFacilities", null) is IDictionary upgradingFacilities)
            upgradingFacilities.Clear();
        if (kCProductionFacilityType.GetProperty("UpgradedFacilities", null) is IDictionary upgradedFacilities)
            upgradedFacilities.Clear();

        if (configurationType.GetField("KCgroups", null) is IDictionary kCgroups)
            kCgroups.Clear();
        if (configurationType.GetField("colonyDictionary", null) is IDictionary colonyDictionary)
            colonyDictionary.Clear();
        if (configurationType.GetField("GroupFacilities", null) is IDictionary groupFacilities)
            groupFacilities.Clear();

        colonyBuildQueueType.Invoke("Clear", colonyBuildingType.GetField("buildQueue", null), []);
    }

    private static void OnSettingsValueMessageReceived(KerbalColoniesSettingsValueMessage msg)
    {
        var kCGameParameters = HighLogic.CurrentGame.Parameters.CustomParams(kCGameParametersType.Type);

        switch (msg.Key)
        {
            case KerbalColoniesConstants.FacilityCostMultiplier:
            case KerbalColoniesConstants.FacilityTimeMultiplier:
            case KerbalColoniesConstants.FacilityRangeMultiplier:
            case KerbalColoniesConstants.EditorRangeMultiplier:
            case KerbalColoniesConstants.VesselCostMultiplier:
            case KerbalColoniesConstants.VesselTimeMultiplier:
                if (!float.TryParse(msg.Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var asFloat))
                    asFloat = 1f;
                kCGameParametersType.SetProperty(msg.Key, kCGameParameters, asFloat);
                break;

            case KerbalColoniesConstants.MaxColoniesPerBody:
                if (!int.TryParse(msg.Value, out var asInt))
                    asInt = 10;
                kCGameParametersType.SetProperty(msg.Key, kCGameParameters, asInt);
                break;

            default:
                Logger.Instance.Warning($"Received settings update for unknown key '{msg.Key}'.", KerbalColoniesPackageName);
                return;
        }

        Logger.Instance.Info($"Updating settings to server value: '{msg.Key}': {msg.Value}", KerbalColoniesPackageName);
    }

    private void OnChangeColonyMessageReceived(KerbalColoniesChangeColonyMessage msg)
    {
        try
        {
            _logger.Info($"Colony data received: {msg.ColonyName} ({msg.Body})", PackageName);

            CloseUiIfOpen(msg.ColonyName, msg.Body);

            LoadColony(msg.ColonyName, msg.Body, msg.Content);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle colony update: {ex}", PackageName);
        }
    }

    private void OnAllColoniesReceived(KerbalColoniesRequestColoniesMessage msg)
    {
        try
        {
            _logger.Info("KC received all colonies.", PackageName);

            var intervalString = _settingsProvider.GetValue(PackageName, "SyncInterval", 5);

            if (!int.TryParse((string)intervalString, out _syncInterval))
                _syncInterval = 5;

            _keepAlive = true;
            LunaCompat.Singleton.StartCoroutine(CheckForColonyUpdate());
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize KC compat: {ex}", PackageName);
        }
        finally
        {
            initialized = true;
        }
    }

    private IEnumerator CheckForColonyUpdate()
    {
        while (_keepAlive)
        {
            yield return new WaitForSeconds(_syncInterval);

            try
            {
                if (!HasColonyWindowOpen())
                    continue;

                SaveColonyScenario();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to check for colony update: {ex}", PackageName);
            }
        }
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        initialized = false;

        UnloadAllColonies();

        if (!hasServerIntegration)
            return;

        Task.Run(async () =>
        {
            // only sync colonies once all KK instances are synced
            while (!KerbalKonstructsIntegration.Initialized)
                await Task.Delay(100);

            _logger.Info("KC requesting all colonies.", PackageName);

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalColoniesRequestColoniesMessage(), false);
        });
    }

    #endregion
}

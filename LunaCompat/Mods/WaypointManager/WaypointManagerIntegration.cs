using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using FinePrint;

using HarmonyLib;

using JetBrains.Annotations;

using LmpCommon;

using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using UnityEngine;

using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;

namespace LunaCompat.Mods.WaypointManager;

[UsedImplicitly]
internal class WaypointManagerIntegration : ClientModIntegration
{
    #region Constants

    private const string WaypointManagerPackageName = "Waypoint Manager";

    #endregion

    #region Fields

    private static ReflectedType waypointDataType;
    private static ReflectedType customWaypointsGuiType;
    private static ReflectedType customWaypointsType;
    private static MethodInfo removeWaypointMethod;
    private static MethodInfo syncWaypointMethod;

    private static List<Guid> waypointLoadList;
    private static List<Guid> waypointRemoveList;
    private static Dictionary<Guid, Waypoint> waypointsCache;
    private static bool isDeleting;

    private bool _keepAlive;

    #endregion

    #region Constructors

    public WaypointManagerIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override bool RequiresServerPlugin => true;

    public override string PackageName => WaypointManagerPackageName;

    #endregion

    #region Public Methods

    public override void Setup()
    {
        _keepAlive = true;
        waypointsCache = new Dictionary<Guid, Waypoint>();
        waypointLoadList = [];
        waypointRemoveList = [];

        ReflectWaypointManagerTypes();

        IgnoredScenarios.IgnoreReceive.Add("ScenarioCustomWaypoints");
        IgnoredScenarios.IgnoreSend.Add("ScenarioCustomWaypoints");

        // add / update custom waypoints
        LunaCompat.HarmonyInstance.Patch(customWaypointsGuiType.Method("WindowGUI"),
                                         transpiler: new HarmonyMethod(typeof(WaypointManagerIntegration), nameof(TranspileWindowGui)));
        // remove custom waypoints
        LunaCompat.HarmonyInstance.Patch(removeWaypointMethod, prefix: new HarmonyMethod(typeof(WaypointManagerIntegration), nameof(PrefixRemoveWaypoint)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<WaypointManagerChangeMessage>(OnSyncWaypointsMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<WaypointManagerDeleteMessage>(OnRemoveWaypointMessageReceived);

        LunaCompat.Singleton.StartCoroutine(AddReceivedWaypointsCoroutine());
    }

    public override void Destroy()
    {
        base.Destroy();
        _keepAlive = false;
        waypointsCache.Clear();
        waypointLoadList.Clear();
        waypointRemoveList.Clear();

        LunaCompat.Singleton.StopCoroutine(AddReceivedWaypointsCoroutine());

        ClientMessageHandler.Instance.HasServerIntegrationChanged -= OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.UnregisterModMessageListener<WaypointManagerChangeMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<WaypointManagerDeleteMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectWaypointManagerTypes()
    {
        customWaypointsType = new ReflectedType("WaypointManager.CustomWaypoints");
        customWaypointsGuiType = new ReflectedType("WaypointManager.CustomWaypointGUI");
        waypointDataType = new ReflectedType("WaypointManager.WaypointData");
        syncWaypointMethod = AccessTools.Method(typeof(WaypointManagerIntegration), nameof(SyncWaypoint));
        removeWaypointMethod = AccessTools.Method(typeof(ScenarioCustomWaypoints), nameof(ScenarioCustomWaypoints.RemoveWaypoint));
    }

    private static IEnumerable<CodeInstruction> TranspileWindowGui(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instr in instructions)
        {
            if (instr.opcode == OpCodes.Ret)
            {
                yield return new CodeInstruction(OpCodes.Ldloc_3); // save
                yield return new CodeInstruction(OpCodes.Ldloc_S, (byte)4); // apply
                yield return new CodeInstruction(OpCodes.Call, syncWaypointMethod);
            }

            yield return instr;
        }
    }

    private static void SyncWaypoint(bool save, bool apply)
    {
        try
        {
            if (!save && !apply)
                return;

            if (customWaypointsGuiType.GetField("selectedWaypoint", null) is not Waypoint waypoint)
                return;

            Logger.Instance.Info($"Synchronizing waypoint {waypoint.navigationId}", WaypointManagerPackageName);

            var message = new WaypointManagerChangeMessage
            {
                ConfigNodeString = WriteWaypointToNode(waypoint),
                NavigationId = waypoint.navigationId
            };

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, WaypointManagerPackageName);
        }
    }

    private static bool PrefixRemoveWaypoint(Waypoint waypoint)
    {
        try
        {
            if (isDeleting)
                return true;

            if (waypointsCache.ContainsKey(waypoint.navigationId))
            {
                Logger.Instance.Warning($"Attempting to remove waypoint which does not exist: {waypoint.navigationId}", WaypointManagerPackageName);
                return true;
            }

            var message = new WaypointManagerDeleteMessage
            {
                NavigationId = waypoint.navigationId
            };

            ClientMessageHandler.Instance.SendReliableMessage(message);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, WaypointManagerPackageName);
        }

        return true;
    }

    private static string WriteWaypointToNode(Waypoint waypoint)
    {
        try
        {
            var node = new ConfigNode("WAYPOINT");

                node.AddValue("name", waypoint.name);
                node.AddValue("celestialName", waypoint.celestialName);
            node.AddValue("latitude", waypoint.latitude);
            node.AddValue("longitude", waypoint.longitude);
            node.AddValue("navigationId", waypoint.navigationId);

                node.AddValue("icon", waypoint.id);

            node.AddValue("altitude", waypoint.altitude);
            node.AddValue("index", waypoint.index);
            node.AddValue("seed", waypoint.seed);

            return node.ToString();
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, WaypointManagerPackageName);
            return null;
        }
    }

    private IEnumerator AddReceivedWaypointsCoroutine()
    {
        while (_keepAlive)
        {
            yield return new WaitForSeconds(1);

            if ((waypointLoadList.Count == 0 && waypointRemoveList.Count == 0) || !FinePrint.WaypointManager.Instance())
                continue;

            foreach (var waypointId in waypointLoadList)
            {
                try
                {
                    if (!waypointsCache.TryGetValue(waypointId, out var waypoint))
                    {
                        _logger.Warning($"Attempting to load non-existent waypoint: {waypointId}", PackageName);
                        continue;
                    }

                    customWaypointsType.Invoke("AddWaypoint", null, [waypoint]);
                    _logger.Info($"Added new waypoint: {waypointId}", PackageName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, PackageName);
                }
            }

            waypointLoadList.Clear();

            foreach (var waypointId in waypointRemoveList)
            {
                try
                {
                    var match = FinePrint.WaypointManager.Instance().Waypoints.Find(x => x.navigationId == waypointId);

                    if (match == null)
                    {
                        _logger.Warning($"Attempting to remove non-existent waypoint: {waypointId}", PackageName);
                        continue;
                    }

                    isDeleting = true;
                    waypointsCache.Remove(waypointId);
                    customWaypointsType.Invoke("RemoveWaypoint", null, [match]);
                    isDeleting = false;
                    _logger.Info($"Removed waypoint: {waypointId}", PackageName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, PackageName);
                }
                
            }

            waypointRemoveList.Clear();

            waypointDataType.Invoke("CacheWaypointData", null, []);
        }
    }

    private void ApplyConfigNodeToWaypoint(ConfigNode node, Waypoint waypoint)
    {
        if (node.HasValue("name"))
            waypoint.name = node.GetValue("name");
        if (node.HasValue("celestialName"))
            waypoint.celestialName = node.GetValue("celestialName");
        if (node.HasValue("latitude"))
            waypoint.latitude = float.Parse(node.GetValue("latitude"));
        if (node.HasValue("longitude"))
            waypoint.longitude = float.Parse(node.GetValue("longitude"));
        if (node.HasValue("navigationId"))
            waypoint.navigationId = Guid.Parse(node.GetValue("navigationId"));
        if (node.HasValue("icon"))
            waypoint.id = node.GetValue("icon");
        if (node.HasValue("altitude"))
            waypoint.altitude = float.Parse(node.GetValue("altitude"));
        if (node.HasValue("index"))
            waypoint.index = int.Parse(node.GetValue("index"));
        if (node.HasValue("seed"))
            waypoint.seed = int.Parse(node.GetValue("seed"));

        waypoint.isCustom = true;
        waypoint.isNavigatable = true;
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        try
        {
            if (!hasServerIntegration)
            {
                Logger.LogServerPluginMissing(PackageName);
                return;
            }

            _logger.Info("Requesting all waypoint data", PackageName);
            ClientMessageHandler.Instance.SendReliableMessage(new WaypointManagerRequestMessage());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnSyncWaypointsMessageReceived(WaypointManagerChangeMessage msg)
    {
        try
        {
            _logger.Info($"Received synchronization message for waypoint {msg.NavigationId}: {msg.ConfigNodeString}", PackageName);

            if (msg.NavigationId == Guid.Empty)
            {
                _logger.Error("Received synchronization message for waypoint with empty navigation ID", PackageName);
                return;
            }

            var node = ConfigNode.Parse(msg.ConfigNodeString).GetNode("WAYPOINT");

            if (waypointsCache.TryGetValue(msg.NavigationId, out var existing))
            {
                // update
                _logger.Info($"Updating existing waypoint ({msg.NavigationId}).", PackageName);
                ApplyConfigNodeToWaypoint(node, existing);
            }
            else
            {
                // add
                _logger.Info($"Adding new waypoint ({msg.NavigationId}).", PackageName);
                var waypoint = new Waypoint();
                ApplyConfigNodeToWaypoint(node, waypoint);
                waypointLoadList.Add(waypoint.navigationId);
                waypointsCache.Add(waypoint.navigationId, waypoint);
            }

            waypointDataType.Invoke("CacheWaypointData", null, []);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnRemoveWaypointMessageReceived(WaypointManagerDeleteMessage msg)
    {
        try
        {
            _logger.Info($"Deleting waypoint: {msg.NavigationId}", PackageName);

            if (msg.NavigationId == Guid.Empty)
            {
                _logger.Error("Attempting to remove waypoint with empty navigation ID", PackageName);
                return;
            }

            if (waypointsCache.ContainsKey(msg.NavigationId))
            {
                waypointRemoveList.Add(msg.NavigationId);
                waypointLoadList.Remove(msg.NavigationId);
            }
            else
                _logger.Warning($"Attempting to remove non-existent waypoint: {msg.NavigationId}", PackageName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    #endregion
}

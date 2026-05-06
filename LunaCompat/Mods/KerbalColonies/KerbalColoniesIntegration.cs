using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using HarmonyLib;

using JetBrains.Annotations;

using LmpCommon;

using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using UnityEngine;

using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;
using Version = System.Version;

namespace LunaCompat.Mods.KerbalColonies;

[UsedImplicitly]
internal class KerbalColoniesIntegration : ClientModIntegration
{
    #region Constants

    public const string KerbalColoniesPackageName = "KerbalColonies";

    #endregion

    #region Fields

    private int _syncInterval;
    private bool _keepAlive;
    private static IReadOnlyDictionary<string, HashSet<int>> lastOpenColonies;
    private static ReflectedType colonyClassType;
    private static ReflectedType kCFacilityBaseType;
    private static ReflectedType kCFacilityWindowBaseType;
    private static ReflectedType kCWindowManagerType;
    private static ReflectedType colonyBuildingType;
    private static bool isDeleting;

    private static bool initialized;

    private static ReflectedType configurationType;

    private static Dictionary<string, string> colonyStateCache;

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
        colonyStateCache = new Dictionary<string, string>();

        ReflectKerbalColoniesTypes();

        // prefix KerbalColonies.Update to only run for the main player

        // postfix Configuration.OnSave(ConfigNode node) to send to server (if main)

        LunaCompat.HarmonyInstance.Patch(configurationType.Method("OnSave"),
                                         postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixSaveConfiguration)));

        // react to windows closing KCWindowManager.CloseWindow(this);
        LunaCompat.HarmonyInstance.Patch(kCWindowManagerType.Method("CloseWindow"),
                                         postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixCloseWindow)));
        //LunaCompat.HarmonyInstance.Patch(kCFacilityWindowBaseType.Method("Draw"),
        //                                 postfix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PostfixFacilityWindowDraw)));

        // react to new colonies being placed
        LunaCompat.HarmonyInstance.Patch(colonyBuildingType.Method("QueuePlacer"),
                                         prefix: new HarmonyMethod(typeof(KerbalColoniesIntegration), nameof(PrefixQueuePlacer)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalColoniesRequestColoniesMessage>(OnAllColoniesReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalColoniesChangeColonyMessage>(OnChangeColonyMessageReceived);
        // Invoke Configuration.OnLoad(ConfigNode node) (or load modular per colony?) on received message
    }

    public override void Destroy()
    {
        base.Destroy();
        initialized = false;
        _keepAlive = false;
        colonyStateCache.Clear();

        ClientMessageHandler.Instance.HasServerIntegrationChanged -= OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalColoniesChangeColonyMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void PrefixQueuePlacer()
    {
        // set a flag which allows postfix to run for all users

        //  KerbalColonies.colonyClass.CreateConfigNode() // save only one specific node 
    }

    private static bool TryGetColonyFromWindow(object window, out string colony, out int facilityId)
    {
        colony = null;
        facilityId = 0;

        if (kCFacilityWindowBaseType.Type.IsAssignableFrom(window.GetType()))
        {
            var facility = kCFacilityWindowBaseType.GetField("facility", window);

            if (facility != null)
            {
                var colonyObj = kCFacilityBaseType.GetField("Colony", facility);

                if (colonyObj == null)
                    return false;

                facilityId = (int)kCFacilityBaseType.GetField("id", facility);

                var name = colonyClassType.GetField("Name", colonyObj) as string;
                var body = colonyClassType.GetField("Body", colonyObj) as string;
                colony = $"{body}_{name}";

                return true;
            }
        }

        return false;
    }

    private static void PostfixCloseWindow(ref object drawfunct)
    {
        // drawfunct is a KCWindow
        Logger.Instance.Info($"{nameof(PostfixCloseWindow)}: {drawfunct}.", KerbalColoniesPackageName);

        if (TryGetColonyFromWindow(drawfunct, out var colony, out var facilityId))
        {
            lastOpenColonies = new Dictionary<string, HashSet<int>>
            {
                {
                    colony, new HashSet<int>
                    {
                        facilityId
                    }
                }
            };
            SaveColonyScenario();
        }
    }

    private static void PostfixSaveConfiguration(ref object node)
    {
        try
        {
            if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
                return;

            if (node is not ConfigNode configNode)
            {
                Logger.Instance.Error("Saved object was not a config node.", KerbalColoniesPackageName);
                return;
            }

            Logger.Instance.Info($"SAVING: {configNode}", KerbalColoniesPackageName);
            var colonyNode = configNode.GetNode("colonyNode");
            Logger.Instance.Info($"colonyNode: {colonyNode.CountNodes}", KerbalColoniesPackageName);

            foreach (var body in colonyNode.GetNodes())
            {
                Logger.Instance.Info($"colony body: {body.name}", KerbalColoniesPackageName);

                foreach (var colony in body.GetNodes())
                {
                    var name = colony.GetValue("name");
                    Logger.Instance.Info($"colony class: {name}", KerbalColoniesPackageName);

                    var colonyId = $"{body.name}_{name}";
                    var colonyStr = colony.ToString();

                    ConfigNode nodeTarget = new ConfigNode(); 

                    foreach (var iN in colony.GetNodes())
                    {
                        var innerNode = iN.CreateCopy();

                        switch (innerNode.name)
                        {
                            case "sharedColonyNodes":
                                break;
                            case "CAB":
                                break;
                            case "facility":
                                break;
                        }
                    }

                    var colonyHash = Common.CalculateSha256Hash(Encoding.UTF8.GetBytes(colonyStr));

                    // one issue here is that it is hard to differentiate between primary player save, new colony addition or save "to discard"

                    // TODO do we need a primary player even? Processing can be done on update. probably not!

                    // if this is false it should be the creation of a new colony or a newly connecting player
                    if (colonyStateCache.TryGetValue(colonyId, out var existingHash))
                    {
                        // should be safe to ignore on non primary players
                        if (colonyHash == existingHash || (lastOpenColonies != null && !lastOpenColonies.ContainsKey(colonyId)))
                            continue;
                    }

                    colonyStateCache[colonyId] = colonyHash;

                    var msg = new KerbalColoniesChangeColonyMessage
                    {
                        Body = body.name,
                        ColonyName = name,
                        Content = colonyStr
                    };

                    ClientMessageHandler.Instance.SendReliableMessage(msg, false);
                }
            }

            lastOpenColonies = null;
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to send colony update: {ex}", KerbalColoniesPackageName);
        }
    }

    private static void ReflectKerbalColoniesTypes()
    {
        configurationType = new ReflectedType("KerbalColonies.Settings.Configuration");
        colonyBuildingType = new ReflectedType("KerbalColonies.ColonyBuilding");
        kCWindowManagerType = new ReflectedType("KerbalColonies.UI.KCWindowManager");
        kCFacilityWindowBaseType = new ReflectedType("KerbalColonies.UI.KCFacilityWindowBase");
        kCFacilityBaseType = new ReflectedType("KerbalColonies.colonyFacilities.KCFacilityBase");
        colonyClassType = new ReflectedType("KerbalColonies.colonyClass");
    }

    private static void SaveColonyScenario()
    {
        var colonyScenario = ScenarioRunner.GetLoadedModules()?.Find(x => x.ClassName == configurationType.Type.Name);

        if (!colonyScenario)
        {
            Logger.Instance.Warning("No scenario module found to save.", KerbalColoniesPackageName);
            return;
        }

        var configNode = new ConfigNode();
        // trigger save postfix
        colonyScenario.Save(configNode);
    }

    private bool HasColonyWindowOpen()
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

    private void OnChangeColonyMessageReceived(KerbalColoniesChangeColonyMessage msg)
    {
        _logger.Info($"Colony data received: {msg.ColonyName} ({msg.Body})", KerbalColoniesPackageName);

        CloseUiIfOpen();

        LoadColony(msg.ColonyName, msg.Body, msg.Content);
    }

    private void CloseUiIfOpen()
    {
    }

    private void LoadColony(string colonyName, string body, string colonyStr)
    {
        // create new colonyNode 

        // unload relevant entries?
        // KCProductionFacility.ConstructedFacilities.Clear();
        // KCProductionFacility.ConstructingFacilities.Clear();
        // KCProductionFacility.UpgradingFacilities.Clear();
        // KCProductionFacility.UpgradedFacilities.Clear();

        // KCgroups.Clear();
        // colonyDictionary.Clear();
        // GroupFacilities.Clear();
        // ColonyBuilding.buildQueue.Clear();
        var node = ConfigNode.Parse(colonyStr);
        var colonyId = $"{body}_{colonyName}";
        var colonyHash = Common.CalculateSha256Hash(Encoding.UTF8.GetBytes(colonyStr));
        colonyStateCache[colonyId] = colonyHash;

        var rootNode = new ConfigNode("SCENARIO");
        // use version from 
        var ver = configurationType.GetField("saveVersion", null) as Version;
        rootNode.AddValue("version", ver.ToString());
        var colonyNode = rootNode.AddNode("colonyNode");
        var bodyNode = colonyNode.AddNode(body);
        bodyNode.AddNode(node.GetNode("colonyClass"));

        _logger.Warning($"Loading colony node:  {rootNode} - {rootNode.HasNode("colonyNode")}");

        // LoadColoniesV4(node);
        configurationType.Invoke("LoadColoniesV4", null, [rootNode]);

        var dict = configurationType.GetField("colonyDictionary", null) as IDictionary;

        _logger.Warning($"Loaded colonies:  {dict.Count}");
    }

    private void OnAllColoniesReceived(KerbalColoniesRequestColoniesMessage msg)
    {
        _logger.Info("KC received all colonies.", KerbalColoniesPackageName);

        var intervalString = _settingsProvider.GetValue(PackageName, "SyncInterval", 5);

        if (!int.TryParse((string)intervalString, out _syncInterval))
            _syncInterval = 15;

        _keepAlive = true;
        LunaCompat.Singleton.StartCoroutine(CheckForColonyUpdate());

        initialized = true;
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
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        initialized = false;

        UnloadAllColonies();

        if (!hasServerIntegration)
            return;

        _logger.Info("KC requesting all colonies.", KerbalColoniesPackageName);

        ClientMessageHandler.Instance.SendReliableMessage(new KerbalColoniesRequestColoniesMessage(), false);
    }

    private void UnloadAllColonies()
    {
    }

    #endregion
}

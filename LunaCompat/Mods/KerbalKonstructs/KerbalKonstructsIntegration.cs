using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using JetBrains.Annotations;

using LunaCompat.Utils;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;

using UnityEngine;

using static UrlDir;

using File = System.IO.File;
using FileInfo = System.IO.FileInfo;
using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;

namespace LunaCompat.Mods.KerbalKonstructs;

[UsedImplicitly]
internal class KerbalKonstructsIntegration : ClientModIntegration
{
    #region Constants

    public const string KerbalKonstructsPackageName = "KerbalKonstructs";

    #endregion

    #region Fields

    private static ReflectedType decalsDatabaseType;
    private static ReflectedType mapDecalInstanceType;
    private static MethodInfo groupCenterUpdateInvokeActionMethod;
    private static ReflectedType configParserType;
    private static ReflectedType groupCenterType;
    private static ReflectedType staticsEditorGuiType;
    private static ReflectedType kkCustomParameters0Type;
    private static ReflectedType kkCustomParameters1Type;
    private static ReflectedType apiType;
    private static ReflectedType staticModelType;
    private static ReflectedType staticInstanceType;
    private static ReflectedType staticDatabaseType;
    private static ReflectedType kerbalKonstructsType;

    private static Dictionary<string, object> groupDictionary;
    private static List<CelestialBody> queuedCelestialsToRebuild;

    private static bool initialized;
    private static bool isDeleting;

    #endregion

    #region Constructors

    public KerbalKonstructsIntegration(ILogger logger, IModSettingsProvider settingsProvider)
        : base(logger, settingsProvider)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => KerbalKonstructsPackageName;

    #endregion

    #region Public Methods

    public override void Setup()
    {
        groupDictionary = new Dictionary<string, object>();
        queuedCelestialsToRebuild = new List<CelestialBody>();

        ReflectKerbalKonstructsTypes();

        LunaCompat.HarmonyInstance.Patch(kkCustomParameters1Type.Method("Interactible"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixKKCustomParameter1Interactible)));

        // KerbalKonstructs.Core.StaticInstance.SaveConfig() is inlined and cannot be patched by Harmony.
        // Postfix KerbalKonstructs.Core.ConfigParser.SaveInstanceByCfg(string pathname) instead.
        LunaCompat.HarmonyInstance.Patch(configParserType.Method("SaveInstanceByCfg"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixSaveInstanceByCfg)));

        LunaCompat.HarmonyInstance.Patch(kerbalKonstructsType.Method("DeleteInstance"),
                                         new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PrefixStaticInstanceDelete)));

        LunaCompat.HarmonyInstance.Patch(groupCenterType.Method("Save"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixSaveGroupCenter)));
        LunaCompat.HarmonyInstance.Patch(groupCenterType.Method("DeleteGroupCenter"),
                                         prefix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PrefixDeleteGroupCenter)));

        LunaCompat.HarmonyInstance.Patch(configParserType.Method("SaveMapDecalInstance"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixSaveMapDecalInstance)));
        LunaCompat.HarmonyInstance.Patch(decalsDatabaseType.Method("DeleteMapDecalInstance"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixDeleteMapDecalInstance)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>(OnChangeGroupCenterMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>(OnDeleteGroupCenterMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsChangeMapDecalMessage>(OnChangeMapDecalMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsDeleteMapDecalMessage>(OnDeleteMapDecalMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsRequestInstancesMessage>(OnAllInstancesAvailableMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsSettingsValueMessage>(OnSettingsValueMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();
        initialized = false;
        groupDictionary.Clear();

        ClientMessageHandler.Instance.HasServerIntegrationChanged -= OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>();
        ClientMessageHandler.Instance.UnregisterModMessageListener<KerbalKonstructsRequestInstancesMessage>();
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectKerbalKonstructsTypes()
    {
        kerbalKonstructsType = new ReflectedType("KerbalKonstructs.KerbalKonstructs");
        staticDatabaseType = new ReflectedType("KerbalKonstructs.Core.StaticDatabase");
        staticInstanceType = new ReflectedType("KerbalKonstructs.Core.StaticInstance");
        staticModelType = new ReflectedType("KerbalKonstructs.Core.StaticModel");
        apiType = new ReflectedType("KerbalKonstructs.API");
        configParserType = new ReflectedType("KerbalKonstructs.Core.ConfigParser");
        kkCustomParameters0Type = new ReflectedType("KerbalKonstructs.Core.KKCustomParameters0");
        kkCustomParameters1Type = new ReflectedType("KerbalKonstructs.Core.KKCustomParameters1");
        staticsEditorGuiType = new ReflectedType("KerbalKonstructs.UI.StaticsEditorGUI");
        groupCenterType = new ReflectedType("KerbalKonstructs.Core.GroupCenter");
        mapDecalInstanceType = new ReflectedType("KerbalKonstructs.Core.MapDecalInstance");
        decalsDatabaseType = new ReflectedType("KerbalKonstructs.Core.DecalsDatabase");
        groupCenterUpdateInvokeActionMethod = AccessTools.Method(typeof(Action<>).MakeGenericType(groupCenterType.Type), "Invoke");
    }

    private static void PrefixStaticInstanceDelete(ref object __0)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        var uuid = staticInstanceType.GetField("UUID", __0) as string;
        var model = staticInstanceType.GetField("model", __0);
        var name = staticModelType.GetField("name", model) as string;

        Logger.Instance.Info($"Delete: {name} ({uuid})", KerbalKonstructsPackageName);

        ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteStaticInstanceMessage
        {
            ModelName = name,
            Identifier = uuid
        });
    }

    private static void PostfixKKCustomParameter1Interactible()
    {
        FixSaveLocations();
    }

    private static void PrefixDeleteGroupCenter(ref object __instance)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var groupCenter = __instance;
            var existing = groupDictionary.SingleOrDefault(x => x.Value == groupCenter);

            var pathName = groupCenterType.GetField("configPath", __instance) as string;
            var basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            var nodePath = Path.Combine(basePath, pathName);

            Logger.Instance.Info($"Group deleted: ({nodePath})", KerbalKonstructsPackageName);

            if (File.Exists(nodePath))
                File.Delete(nodePath);

            if (existing.Key == null)
            {
                // did not exist in dict
                Logger.Instance.Warning("Deleted group did not exist in LunaCompat dictionary.", KerbalKonstructsPackageName);
                return;
            }

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteGroupCenterMessage
            {
                Identifier = existing.Key,
            });
            groupDictionary.Remove(existing.Key);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, KerbalKonstructsPackageName);
        }
    }

    private static void PostfixSaveGroupCenter(ref object __instance)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var pathName = groupCenterType.GetField("configPath", __instance) as string;
            var isInSavegame = groupCenterType.GetField("isInSavegame", __instance);

            // set isInSavegame to false to avoid KC scenario saves
            if (isInSavegame is true)
            {
                Logger.Instance.Info($"Fixing savegame setting for {__instance}", KerbalKonstructsPackageName);
                groupCenterType.SetField("isInSavegame", __instance, false);
                var childInstances = (IEnumerable)groupCenterType.GetField("childInstances", __instance);

                foreach (var instance in childInstances)
                    staticInstanceType.SetField("isInSavegame", instance, false);

                // just recall?
                groupCenterType.Invoke("Save", __instance, []);
                return;
            }

            // check buildin/savegame
            if (string.IsNullOrEmpty(pathName))
                return;

            var basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            var nodePath = Path.Combine(basePath, pathName);

            if (!nodePath.Contains("KerbalKonstructs/NewInstances"))
            {
                Logger.Instance.Info($"Ignoring save for local group center: ({nodePath})", KerbalKonstructsPackageName);
                return;
            }

            Logger.Instance.Info($"Group saved: ({nodePath})", KerbalKonstructsPackageName);

            var groupCenter = __instance;
            var existing = groupDictionary.SingleOrDefault(x => x.Value == groupCenter);

            string uuid;

            if (existing.Key != null)
                uuid = existing.Key;
            else
            {
                uuid = Guid.NewGuid().ToString();
                groupDictionary.Add(uuid, __instance);
            }

            var node = ConfigNode.Load(nodePath);
            var writtenFileName = nodePath.Remove(0, basePath.Length + 1);

            Logger.Instance.Debug($"Group sending: ({nodePath}, {uuid})", KerbalKonstructsPackageName);

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeGroupCenterMessage
            {
                Uuid = uuid,
                Name = Path.GetFileNameWithoutExtension(writtenFileName),
                Content = node.ToString()
            });

            // update attached static instances to account for name changes
            var instances = (IEnumerable)groupCenterType.GetField("childInstances", __instance);

            foreach (var instance in instances)
                staticInstanceType.Invoke("SaveConfig", instance, []);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, KerbalKonstructsPackageName);
        }
    }

    private static void PostfixDeleteMapDecalInstance(ref object instance)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var decalSavePath = mapDecalInstanceType.GetField("configPath", instance) as string;

            Logger.Instance.Info($"Map decal deleted: {decalSavePath}.", KerbalKonstructsPackageName);

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteMapDecalMessage
            {
                Identifier = Path.GetFileNameWithoutExtension(decalSavePath),
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, KerbalKonstructsPackageName);
        }
    }

    private static void PostfixSaveMapDecalInstance(ref object instance)
    {
        try
        {
            if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
                return;

            var isInSavegame = mapDecalInstanceType.GetField("isInSavegame", instance);

            // set isInSavegame to false to avoid KC scenario saves
            if (isInSavegame is true)
            {
                Logger.Instance.Info($"Fixing savegame setting for {instance}", KerbalKonstructsPackageName);
                mapDecalInstanceType.SetField("isInSavegame", instance, false);
                configParserType.Invoke("SaveMapDecalInstance", null, [instance]);
                return;
            }

            var decalSavePath = mapDecalInstanceType.GetField("configPath", instance) as string;

            if (!decalSavePath.Contains("KerbalKonstructs/NewInstances"))
            {
                Logger.Instance.Info($"Ignoring save for local map decal: ({decalSavePath})", KerbalKonstructsPackageName);
                return;
            }

            var basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            var nodePath = Path.Combine(basePath, decalSavePath);

            var node = ConfigNode.Load(nodePath);
            var name = node.GetNode("KK_MapDecal")?.GetValue("Name");

            Logger.Instance.Info($"Map decal saved: {name} ({nodePath}).", KerbalKonstructsPackageName);

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeMapDecalMessage
            {
                Name = Path.GetFileNameWithoutExtension(nodePath),
                Content = node.ToString()
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, KerbalKonstructsPackageName);
        }
    }

    private static void PostfixSaveInstanceByCfg(string pathname)
    {
        var nodePath = KSPUtil.ApplicationRootPath + "GameData/" + pathname;

        try
        {
            if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
                return;

            if (!nodePath.Contains("KerbalKonstructs/NewInstances"))
            {
                Logger.Instance.Info($"Ignoring save for local instance: ({nodePath})", KerbalKonstructsPackageName);
                return;
            }

            var node = ConfigNode.Load(nodePath);
            var name = node.GetNode("STATIC")?.GetValue("pointername");

            Logger.Instance.Info($"Static instance saved: ({nodePath})", KerbalKonstructsPackageName);

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeStaticInstanceMessage
            {
                Name = name,
                Content = node.ToString()
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex, KerbalKonstructsPackageName);
        }
    }

    private static void FixSaveLocations()
    {
        if (!ClientMessageHandler.Instance.HasServerIntegration)
            return;

        var kkParameters = HighLogic.CurrentGame.Parameters.CustomParams(kkCustomParameters1Type.Type);
        kkCustomParameters1Type.SetField("newInstancePath", kkParameters, "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances");
    }

    private static void OnDeleteStaticInstanceMessageReceived(KerbalKonstructsDeleteStaticInstanceMessage msg)
    {
        Logger.Instance.Info($"Static instance unload received: {msg.ModelName}", KerbalKonstructsPackageName);

        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.ModelName}.cfg");

        Task.Run(() =>
        {
            try
            {
                if (!File.Exists(targetPath))
                    return;

                var node = ConfigNode.Load(targetPath);
                var existingInstances = node.GetNode("root").GetNode("STATIC");

                foreach (var instance in existingInstances.GetNodes("Instances"))
                {
                    if (instance.GetValue("UUID") == msg.Identifier)
                    {
                        existingInstances.RemoveNode(instance);
                        break;
                    }
                }

                File.WriteAllText(targetPath, node.ToString());
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, KerbalKonstructsPackageName);
            }
        });

        CloseUiIfOpen();
        apiType.Invoke("RemoveStatic", null, [msg.Identifier]);
    }

    private static void OnChangeStaticInstanceMessageReceived(KerbalKonstructsChangeStaticInstanceMessage msg)
    {
        Logger.Instance.Info($"Static instance received: {msg.Name}", KerbalKonstructsPackageName);

        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.Name}.cfg");

        Task.Run(() =>
        {
            try
            {
                File.WriteAllText(targetPath, msg.Content);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, KerbalKonstructsPackageName);
            }
        });

        CloseUiIfOpen();
        var node = ConfigNode.Parse(msg.Content);
        LoadInstance(targetPath, node);
    }

    private static void LoadGroup(string uuid, string targetPath, ConfigNode node, string nodePath)
    {
        var urlDir = new UrlDir([new ConfigDirectory("", "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances", DirectoryType.GameData)],
                                [new ConfigFileType(FileType.Config, ["cfg"])]);
        var collectionFile = new UrlFile(urlDir, new FileInfo(targetPath));

        var groupNode = node.GetNode("root")?.GetNode("KK_GroupCenter");

        if (groupNode == null)
        {
            Logger.Instance.Warning($"Group {targetPath} has no KK_GroupCenter component.", KerbalKonstructsPackageName);
            return;
        }

        // we need to replace groups whenever something changed. However without uuids, a renamed or changed group is annoying to detect. Instead, map all received groups via a custom uuid
        if (groupDictionary.TryGetValue(uuid, out var existing))
        {
            Logger.Instance.Warning($"Updating group {targetPath}.", KerbalKonstructsPackageName);

            var newName = groupNode.GetValue("Group");
            var currentName = groupCenterType.GetField("Group", existing) as string;

            if (newName != currentName)
            {
                Logger.Instance.Info($"Name changed, updating ({currentName} > {newName})", KerbalKonstructsPackageName);

                groupCenterType.Invoke("RenameGroup", existing, [newName]);
            }

            groupCenterType.Invoke("ParseCFGNode", existing, [groupNode]);

            // update RadialPosition
            var refLat = (double)groupCenterType.GetField("RefLatitude", existing);
            var refLng = (double)groupCenterType.GetField("RefLongitude", existing);
            var body = groupCenterType.GetField("CelestialBody", existing) as CelestialBody;
            groupCenterType.SetField("RadialPosition", existing, (Vector3)(body?.GetRelSurfaceNVector(refLat, refLng).normalized * body.pqsController.radius));

            // update RotationAngle
            var groupCenterGameObject = groupCenterType.GetField("gameObject", existing) as GameObject;
            var rotation = Quaternion.AngleAxis((float)groupCenterType.GetField("Heading", existing), (Vector3)groupCenterType.GetField("upVector", existing));
            if (groupCenterGameObject)
                groupCenterGameObject.transform.forward = rotation * (Vector3)groupCenterType.GetField("northVector", existing);
            groupCenterType.SetField("RotationAngle", existing, groupCenterType.GetField("Heading", existing));

            groupCenterType.Invoke("Update", existing, []);
            var pqs = groupCenterType.GetField("pqsCity", existing) as PQSCity;
            if (pqs)
                pqs.Orientate();

            // Apparently not needed:
            //groupCenterType.Invoke("UpdateRotation2Heading", existing, []);

            var onSaveAction = apiType.GetField("OnGroupSaved", null);
            groupCenterUpdateInvokeActionMethod.Invoke(onSaveAction, [existing]);
        }
        else
        {
            Logger.Instance.Warning($"Adding new group {targetPath}.", KerbalKonstructsPackageName);

            var config = new UrlConfig(collectionFile, groupNode);
            collectionFile.configs.Add(config);
            GameDatabase.Instance.root.children.First().files.Add(collectionFile);

            var groupCenter = Activator.CreateInstance(groupCenterType.Type);

            groupCenterType.Invoke("ParseCFGNode", groupCenter, [groupNode]);
            groupCenterType.SetField("configPath", groupCenter, nodePath);
            groupCenterType.SetField("configUrl", groupCenter, config);
            groupCenterType.Invoke("Spawn", groupCenter, []);

            groupDictionary.Add(uuid, groupCenter);
        }

        var allCenters = (IDictionary)staticDatabaseType.GetField("allCenters", null);
        Logger.Instance.Info($"Loaded centers: {allCenters.Count}", KerbalKonstructsPackageName);
    }

    private static void CloseUiIfOpen()
    {
        var instance = staticsEditorGuiType.GetField("_instance", null);

        if (instance != null && staticsEditorGuiType.Invoke("IsOpen", instance, []) is bool and true)
            staticsEditorGuiType.Invoke("ToggleEditor", instance, []);
    }

    private static void LoadInstance(string targetPath, ConfigNode node)
    {
        // uri for LunaCompat to ensure valid save location on update
        // GameDatabase.Instance.root.AllDirectories.Single(x => x.url == nameof(LunaCompat))
        var urlDir = new UrlDir([new ConfigDirectory("", "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances", DirectoryType.GameData)],
                                [new ConfigFileType(FileType.Config, ["cfg"])]);
        var collectionFile = new UrlFile(urlDir, new FileInfo(targetPath));
        var staticNode = node.GetNode("root")?.GetNode("STATIC");

        if (staticNode == null)
        {
            Logger.Instance.Warning($"Static instance {targetPath} has no STATIC component.", KerbalKonstructsPackageName);
            return;
        }

        isDeleting = true;

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            apiType.Invoke("RemoveStatic", null, [uuid]);
        }

        isDeleting = false;

        var config = new UrlConfig(collectionFile, staticNode);
        collectionFile.configs.Add(config);
        GameDatabase.Instance.root.children.First().files.Add(collectionFile);

        var modelName = staticNode.GetValue("pointername");
        var model = staticDatabaseType.Invoke("GetModelByName", null, [modelName]);

        var kkInstance = kerbalKonstructsType.GetField("instance", null);

        if (model != null)
            kerbalKonstructsType.Invoke("LoadInstances", kkInstance, [config, model]);

        var subPath = $"../saves/LunaMultiplayer/KerbalKonstructs/NewInstances/{Path.GetFileName(targetPath)}";

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            var instance = apiType.Invoke("getStaticInstanceByUUID", null, [uuid]);

            if (instance == null)
                Logger.Instance.Info($"No UUID {uuid} for '{targetPath}'", KerbalKonstructsPackageName);
            else
                staticInstanceType.SetField("configPath", instance, subPath);
        }

        var allStaticInstances = (Array)staticDatabaseType.GetField("allStaticInstances", null);
        Logger.Instance.Info($"Loaded: {allStaticInstances.Length} instances", KerbalKonstructsPackageName);

        if (!initialized)
            return;

        kerbalKonstructsType.Invoke("OnLevelWasLoad", kkInstance, [HighLogic.LoadedScene]);
    }

    private static void DeleteMapDecal(object instance)
    {
        isDeleting = true;
        var decalObject = mapDecalInstanceType.GetField("gameObject", instance) as GameObject;

        if (decalObject)
        {
            decalObject.transform.parent = null;
            decalObject.DestroyGameObject();
        }

        var mapDecal = mapDecalInstanceType.GetField("mapDecal", instance) as PQSMod_MapDecal;
        if (mapDecal)
            mapDecal.transform.parent = null;
        var body = mapDecalInstanceType.GetField("CelestialBody", instance) as CelestialBody;

        if (body)
        {
            if (initialized)
                body.pqsController.RebuildSphere();
            else
                queuedCelestialsToRebuild.Add(body);
        }

        decalsDatabaseType.Invoke("DeleteMapDecalInstance", null, [instance]);
        isDeleting = false;
    }

    private void LoadLocalSettings()
    {
        UpdateSetting("toggleIconsWithBB", false, kkCustomParameters0Type);
        UpdateSetting("soundMasterVolume", 1f, kkCustomParameters0Type);
        UpdateSetting("focusLastLaunchSite", false, kkCustomParameters0Type);
    }

    private void OnAllInstancesAvailableMessageReceived(KerbalKonstructsRequestInstancesMessage msg)
    {
        LoadLocalSettings();

        var kkInstance = kerbalKonstructsType.GetField("instance", null);
        kerbalKonstructsType.Invoke("OnLevelWasLoad", kkInstance, [HighLogic.LoadedScene]);
        initialized = true;

        // update spheres once after initialization
        foreach (var sphere in queuedCelestialsToRebuild.Distinct())
            sphere.pqsController.RebuildSphere();
        queuedCelestialsToRebuild.Clear();
    }

    private void OnSettingsValueMessageReceived(KerbalKonstructsSettingsValueMessage msg)
    {
        var kkParameters0 = HighLogic.CurrentGame.Parameters.CustomParams(kkCustomParameters0Type.Type);

        switch (msg.Key)
        {
            case KerbalKonstructsConstants.EnableRT:
            case KerbalKonstructsConstants.EnableCommNet:
            case KerbalKonstructsConstants.DisableRemoteBaseOpening:
            case KerbalKonstructsConstants.DisableRemoteRecovery:
                if (!bool.TryParse(msg.Value, out var asBool))
                    asBool = false;
                kkCustomParameters0Type.SetField(msg.Key, kkParameters0, asBool);
                break;

            case KerbalKonstructsConstants.FacilityUseRange:
                if (!float.TryParse(msg.Value, out var asFloat))
                    asFloat = 300f;
                kkCustomParameters0Type.SetField(msg.Key, kkParameters0, asFloat);
                break;

            default:
                _logger.Warning($"Received settings update for unknown key '{msg.Key}'.", PackageName);
                return;
        }

        _logger.Info($"Updating settings to server value: '{msg.Key}': {msg.Value}", PackageName);
    }

    private void OnDeleteMapDecalMessageReceived(KerbalKonstructsDeleteMapDecalMessage msg)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var configPath = Path.Combine("../saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.Identifier}.cfg").Replace('\\', '/');

            object existingDecal = null;

            foreach (var decal in (Array)decalsDatabaseType.GetField("allMapDecalInstances", null))
            {
                var existingPath = mapDecalInstanceType.GetField("configPath", decal) as string;

                if (configPath.Equals(existingPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    existingDecal = decal;
                    break;
                }
            }

            if (existingDecal == null)
                _logger.Warning($"Map decal '{configPath}' was deleted on server but does not exist.", PackageName);
            else
            {
                CloseUiIfOpen();

                DeleteMapDecal(existingDecal);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnChangeMapDecalMessageReceived(KerbalKonstructsChangeMapDecalMessage msg)
    {
        Logger.Instance.Info($"Map decal received: {msg.Name}", KerbalKonstructsPackageName);

        var relativePath = Path.Combine("../saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.Name}.cfg").Replace('\\', '/');
        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", relativePath);

        Task.Run(() =>
        {
            try
            {
                File.WriteAllText(targetPath, msg.Content);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex, KerbalKonstructsPackageName);
            }
        });

        CloseUiIfOpen();
        var node = ConfigNode.Parse(msg.Content);
        LoadMapDecal(targetPath, node, relativePath);
    }

    private void LoadMapDecal(string targetPath, ConfigNode node, string relativePath)
    {
        var urlDir = new UrlDir([new ConfigDirectory("", "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances", DirectoryType.GameData)],
                                [new ConfigFileType(FileType.Config, ["cfg"])]);
        var collectionFile = new UrlFile(urlDir, new FileInfo(targetPath));
        var decalNode = node.GetNode("root")?.GetNode("KK_MapDecal");

        object existingDecal = null;

        foreach (var decal in (Array)decalsDatabaseType.GetField("allMapDecalInstances", null))
        {
            var existingPath = mapDecalInstanceType.GetField("configPath", decal) as string;
            _logger.Info($"Comparing decal {relativePath} with {existingPath}", PackageName);

            if (relativePath.Equals(existingPath, StringComparison.InvariantCultureIgnoreCase))
            {
                existingDecal = decal;
                _logger.Info($"Deleting {existingPath}", PackageName);
                break;
            }
        }

        if (existingDecal != null)
            DeleteMapDecal(existingDecal);

        var config = new UrlConfig(collectionFile, decalNode);
        collectionFile.configs.Add(config);
        GameDatabase.Instance.root.children.First().files.Add(collectionFile);

        var newInstance = Activator.CreateInstance(mapDecalInstanceType.Type, true);
        configParserType.Invoke("ParseMapDecalConfig", null, [newInstance, decalNode]);
        mapDecalInstanceType.SetField("configPath", newInstance, relativePath);

        if (mapDecalInstanceType.GetField("CelestialBody", newInstance) == null)
        {
            isDeleting = true;
            decalsDatabaseType.Invoke("DeleteMapDecalInstance", null, [newInstance]);
            isDeleting = false;
        }

        else
        {
            var mapDecal = mapDecalInstanceType.GetField("mapDecal", newInstance) as PQSMod_MapDecal;
            var body = mapDecalInstanceType.GetField("CelestialBody", newInstance) as CelestialBody;

            if (mapDecal && body)
            {
                var lat = (double)mapDecalInstanceType.GetField("Latitude", newInstance);
                var lng = (double)mapDecalInstanceType.GetField("Longitude", newInstance);
                var offset = (float)mapDecalInstanceType.GetField("AbsolutOffset", newInstance);

                mapDecal.transform.position = body.GetWorldSurfacePosition(lat, lng, offset);
                mapDecal.transform.up = body.GetSurfaceNVector(lat, lng);
            }

            mapDecalInstanceType.Invoke("Update", newInstance, [false]);

            if (body)
            {
                if (initialized)
                    body.pqsController.RebuildSphere();
                else
                    queuedCelestialsToRebuild.Add(body);
            }
        }
    }

    private void OnDeleteGroupCenterMessageReceived(KerbalKonstructsDeleteGroupCenterMessage msg)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            if (!groupDictionary.TryGetValue(msg.Identifier, out var group))
            {
                _logger.Warning("Deleted group on server did not exist locally.", PackageName);
                return;
            }

            _logger.Info($"Received group center deletion: {msg.Identifier}", PackageName);

            var groupName = groupCenterType.GetField("Group", group);
            var body = groupCenterType.GetField("CelestialBody", group) as CelestialBody;

            isDeleting = true;

            CloseUiIfOpen();

            apiType.Invoke("RemoveGroup", null, [groupName, body?.name]);
            groupDictionary.Remove(msg.Identifier);

            isDeleting = false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, PackageName);
        }
    }

    private void OnChangeGroupCenterMessageReceived(KerbalKonstructsChangeGroupCenterMessage msg)
    {
        _logger.Info($"Received group center update: {msg.Uuid} - {msg.Name}", PackageName);

        var subPath = Path.Combine("saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.Name}.cfg");
        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, subPath);

        Task.Run(() =>
        {
            try
            {
                File.WriteAllText(targetPath, msg.Content);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, PackageName);
            }
        });

        CloseUiIfOpen();
        var node = ConfigNode.Parse(msg.Content);
        LoadGroup(msg.Uuid, targetPath, node, $"../{subPath}");
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        FixSaveLocations();

        UnloadAllGroupCenters();
        UnloadAllMapDecals();
        UnloadAllStaticInstances();

        if (!hasServerIntegration)
            return;

        var instancePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances");

        if (Directory.Exists(instancePath) && Directory.EnumerateFiles(instancePath).Any())
            Directory.Delete(instancePath, true);

        Directory.CreateDirectory(instancePath);

        ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsRequestInstancesMessage(), false);
    }

    private void UnloadAllStaticInstances()
    {
        var allStaticInstances = (Array)staticDatabaseType.GetField("allStaticInstances", null);

        foreach (var instance in allStaticInstances)
        {
            var path = staticInstanceType.GetField("configPath", instance) as string;

            if (string.IsNullOrEmpty(path) || !path.Contains("KerbalKonstructs/NewInstances") ||
                staticInstanceType.GetField("configUrl", instance) is not UrlConfig url)
                continue;

            _logger.Debug($"Unloading {path} instance", PackageName);
            url.config.RemoveNodes("Instances");
            staticInstanceType.Invoke("Deactivate", instance, []);
            var uuid = staticInstanceType.GetField("UUID", instance) as string;
            apiType.Invoke("RemoveStatic", null, [uuid]);
        }

        allStaticInstances = (Array)staticDatabaseType.GetField("allStaticInstances", null);
        _logger.Info($"Loaded: {allStaticInstances.Length} instances", PackageName);

        foreach (var instance in allStaticInstances)
        {
            var path = staticInstanceType.GetField("configPath", instance) as string;
            var uuid = staticInstanceType.GetField("UUID", instance) as string;
            _logger.Info($"Still loaded: {path} [{uuid}]", PackageName);
        }
    }

    private void UnloadAllMapDecals()
    {
        var decalsToUnload = new List<object>();

        foreach (var decal in (Array)decalsDatabaseType.GetField("allMapDecalInstances", null))
        {
            var path = mapDecalInstanceType.GetField("configPath", decal) as string;

            if (string.IsNullOrEmpty(path) || !path.Contains("KerbalKonstructs/NewInstances") ||
                mapDecalInstanceType.GetField("configUrl", decal) is not UrlConfig url)
                continue;

            url.config.RemoveNodes("KK_MapDecal");
            decalsToUnload.Add(decal);
        }

        foreach (var decal in decalsToUnload)
            DeleteMapDecal(decal);
    }

    private void UnloadAllGroupCenters()
    {
        var allCenters = (IDictionary)staticDatabaseType.GetField("allCenters", null);
        var groupsToUnload = new List<string>();

        foreach (var fgn in allCenters.Keys)
        {
            if (fgn is not string groupName)
                continue;

            var groupObject = allCenters[fgn];
            var path = groupCenterType.GetField("configPath", groupObject) as string;

            if (string.IsNullOrEmpty(path) || !path.Contains("KerbalKonstructs/NewInstances") ||
                groupCenterType.GetField("configUrl", groupObject) is not UrlConfig url)
                continue;

            url.config.RemoveNodes("Instances");
            groupsToUnload.Add(groupName);
        }

        foreach (var group in groupsToUnload)
        {
            var nameParts = group.Split(['_'], 2);
            if (nameParts.Length != 2)
                continue;

            _logger.Info($"Unloading group '{group}'.", PackageName);
            apiType.Invoke("RemoveGroup", null, [nameParts[1], nameParts[0]]);
        }
    }

    #endregion
}

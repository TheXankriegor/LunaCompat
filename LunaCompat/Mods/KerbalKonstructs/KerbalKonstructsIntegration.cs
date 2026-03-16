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
    #region Fields

    private static MethodInfo groupCenterUpdateInvokeActionMethod;
    private static ReflectedType configParserType;
    private static ReflectedType groupCenterType;
    private static ReflectedType staticsEditorGuiType;
    private static ReflectedType kkCustomParameters1Type;
    private static ReflectedType apiType;
    private static ReflectedType staticModelType;
    private static ReflectedType staticInstanceType;
    private static ReflectedType staticDatabaseType;
    private static ReflectedType kerbalKonstructsType;

    private static Dictionary<string, object> groupDictionary;

    private static bool initialized;
    private static bool isDeleting;

    #endregion

    #region Constructors

    public KerbalKonstructsIntegration(ILogger logger)
        : base(logger)
    {
    }

    #endregion

    #region Properties

    public override string PackageName => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void Setup(ConfigNode node)
    {
        groupDictionary = new Dictionary<string, object>();

        ReflectKerbalKonstructsTypes();

        LunaCompat.HarmonyInstance.Patch(kkCustomParameters1Type.Method("Interactible"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixKKCustomParameter1Interactible)));

        // KerbalKonstructs.Core.StaticInstance.SaveConfig() is inlined and cannot be patched by Harmony.
        // Postfix KerbalKonstructs.Core.ConfigParser.SaveInstanceByCfg(string pathname) instead.
        LunaCompat.HarmonyInstance.Patch(configParserType.Method("SaveInstanceByCfg"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixSaveInstanceByCfg)));

        LunaCompat.HarmonyInstance.Patch(kerbalKonstructsType.Method("DeleteInstance"),
                                         new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PrefixStaticInstanceDelete)));

        // groups KerbalKonstructs.Core.GroupCenter.Save DeleteGroupCenter
        //var groupCenterType = AccessTools.TypeByName("KerbalKonstructs.Core.GroupCenter");
        LunaCompat.HarmonyInstance.Patch(groupCenterType.Method("Save"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PostfixSaveGroupCenter)));
        LunaCompat.HarmonyInstance.Patch(groupCenterType.Method("DeleteGroupCenter"),
                                         prefix: new HarmonyMethod(typeof(KerbalKonstructsIntegration), nameof(PrefixDeleteGroupCenter)));

        ClientMessageHandler.Instance.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>(OnChangeGroupCenterMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>(OnDeleteGroupCenterMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);
        ClientMessageHandler.Instance.RegisterModMessageListener<KerbalKonstructsRequestInstancesMessage>(OnAllInstancesAvailableMessageReceived);
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
        kkCustomParameters1Type = new ReflectedType("KerbalKonstructs.Core.KKCustomParameters1");
        staticsEditorGuiType = new ReflectedType("KerbalKonstructs.UI.StaticsEditorGUI");
        groupCenterType = new ReflectedType("KerbalKonstructs.Core.GroupCenter");
        groupCenterUpdateInvokeActionMethod = AccessTools.Method(typeof(Action<>).MakeGenericType(groupCenterType.Type), "Invoke");
    }

    private static void PrefixStaticInstanceDelete(ref object __0)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        var uuid = staticInstanceType.Field("UUID").GetValue(__0) as string;
        var model = staticInstanceType.Field("model").GetValue(__0);
        var name = staticModelType.GetField("name", model) as string;

        Logger.Instance.Info($"KerbalKonstructs delete: {name} ({uuid})");

        ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteStaticInstanceMessage
        {
            ModelName = name,
            Uuid = uuid
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

            Logger.Instance.Info($"KerbalKonstructs group deleted: ({nodePath})");

            if (File.Exists(nodePath))
                File.Delete(nodePath);

            if (existing.Key == null)
            {
                // did not exist in dict
                Logger.Instance.Warning("Deleted group did not exist in LunaCompat dictionary.");
                return;
            }

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteGroupCenterMessage
            {
                Uuid = existing.Key,
            });
            groupDictionary.Remove(existing.Key);
        }

        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
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
                Logger.Instance.Info($"KerbalKonstructs - fixing savegame setting for {__instance}");
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
                Logger.Instance.Info($"Ignoring save for local group center: ({nodePath})");
                return;
            }

            Logger.Instance.Info($"KerbalKonstructs group saved: ({nodePath})");

            var groupCenter = __instance;
            var existing = groupDictionary.SingleOrDefault(x => x.Value == groupCenter);

            // update attached static instances to account for name changes

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

            Logger.Instance.Debug($"KerbalKonstructs group sending: ({nodePath}, {uuid})");

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeGroupCenterMessage
            {
                Uuid = uuid,
                ModelName = Path.GetFileNameWithoutExtension(writtenFileName),
                Content = node.ToString()
            });

            var instances = (IEnumerable)groupCenterType.GetField("childInstances", __instance);

            foreach (var instance in instances)
                staticInstanceType.Invoke("SaveConfig", instance, []);
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
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
                Logger.Instance.Info($"Ignoring save for local instance: ({nodePath})");
                return;
            }

            var node = ConfigNode.Load(nodePath);
            var name = node.GetNode("STATIC")?.GetValue("pointername");

            Logger.Instance.Info($"KerbalKonstructs saved: ({nodePath})");

            ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeStaticInstanceMessage
            {
                ModelName = name,
                Content = node.ToString()
            });
        }
        catch (Exception ex)
        {
            Logger.Instance.Error(ex);
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
        Logger.Instance.Info($"KerbalKonstructs unload received: {msg.ModelName}");

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
                    if (instance.GetValue("UUID") == msg.Uuid)
                    {
                        existingInstances.RemoveNode(instance);
                        break;
                    }
                }

                File.WriteAllText(targetPath, node.ToString());
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        });

        CloseUiIfOpen();
        apiType.Invoke("RemoveStatic", null, [msg.Uuid]);
    }

    private static void OnChangeStaticInstanceMessageReceived(KerbalKonstructsChangeStaticInstanceMessage msg)
    {
        Logger.Instance.Info($"KerbalKonstructs received: {msg.ModelName}");

        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.ModelName}.cfg");

        // move to matching uuid folder?

        Task.Run(() =>
        {
            try
            {
                File.WriteAllText(targetPath, msg.Content);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
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
            Logger.Instance.Warning($"KK group {targetPath} has no KK_GroupCenter component.");
            return;
        }

        // we need to replace groups whenever something changed. However without uuids, a renamed or changed group is annoying to detect. Instead, map all received groups via a custom uuid
        if (groupDictionary.TryGetValue(uuid, out var existing))
        {
            Logger.Instance.Warning($"Updating KK group {targetPath}.");

            var newName = groupNode.GetValue("Group");
            var currentName = groupCenterType.GetField("Group", existing) as string;

            if (newName != currentName)
            {
                Logger.Instance.Info($"Name changed, updating ({currentName} > {newName})");

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
            Logger.Instance.Warning($"Adding new KK group {targetPath}.");

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

        Logger.Instance.Warning($"Loaded KK groups: {groupDictionary.Count}.");

        var allCenters = (IDictionary)staticDatabaseType.Field("allCenters").GetValue(null);
        Logger.Instance.Info($"Loaded in KK: {allCenters.Count} instances");
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
            Logger.Instance.Warning($"KK instance {targetPath} has no STATIC component.");
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

        var kkInstance = kerbalKonstructsType.Field("instance").GetValue(null);

        if (model != null)
            kerbalKonstructsType.Invoke("LoadInstances", kkInstance, [config, model]);

        var subPath = $"../saves/LunaMultiplayer/KerbalKonstructs/NewInstances/{Path.GetFileName(targetPath)}";

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            var instance = apiType.Invoke("getStaticInstanceByUUID", null, [uuid]);

            if (instance == null)
                Logger.Instance.Info($"No UUID {uuid} for '{targetPath}'");
            else
                staticInstanceType.SetField("configPath", instance, subPath);
        }

        var allStaticInstances = (Array)staticDatabaseType.Field("allStaticInstances").GetValue(null);
        Logger.Instance.Info($"Loaded: {allStaticInstances.Length} instances");

        if (!initialized)
            return;

        kerbalKonstructsType.Invoke("OnLevelWasLoad", kkInstance, [HighLogic.LoadedScene]);
    }

    private static void OnAllInstancesAvailableMessageReceived(KerbalKonstructsRequestInstancesMessage msg)
    {
        var kkInstance = kerbalKonstructsType.Field("instance").GetValue(null);
        kerbalKonstructsType.Invoke("OnLevelWasLoad", kkInstance, [HighLogic.LoadedScene]);
        initialized = true;
    }

    private void OnDeleteGroupCenterMessageReceived(KerbalKonstructsDeleteGroupCenterMessage msg)
    {
        if (!initialized || isDeleting || !ClientMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            if (!groupDictionary.TryGetValue(msg.Uuid, out var group))
            {
                _logger.Warning("Deleted group on server did not exist locally.");
                return;
            }

            _logger.Info($"KerbalKonstructs received group center deletion: {msg.Uuid}");

            var groupName = groupCenterType.GetField("Group", group);
            var body = groupCenterType.GetField("CelestialBody", group) as CelestialBody;

            isDeleting = true;

            CloseUiIfOpen();

            apiType.Invoke("RemoveGroup", null, [groupName, body?.name]);
            groupDictionary.Remove(msg.Uuid);

            isDeleting = false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    private void OnChangeGroupCenterMessageReceived(KerbalKonstructsChangeGroupCenterMessage msg)
    {
        _logger.Info($"KerbalKonstructs received group center update: {msg.Uuid} - {msg.ModelName}");

        var subPath = Path.Combine("saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.ModelName}.cfg");
        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, subPath);

        Task.Run(() =>
        {
            try
            {
                File.WriteAllText(targetPath, msg.Content);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        });

        CloseUiIfOpen();
        var node = ConfigNode.Parse(msg.Content);
        LoadGroup(msg.Uuid, targetPath, node, $"../{subPath}");
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        FixSaveLocations();

        // also unload groups

        var allStaticInstances = (Array)staticDatabaseType.Field("allStaticInstances").GetValue(null);

        foreach (var instance in allStaticInstances)
        {
            var path = staticInstanceType.GetField("configPath", instance) as string;

            if (string.IsNullOrEmpty(path) || !path.Contains("KerbalKonstructs/NewInstances") ||
                staticInstanceType.GetField("configUrl", instance) is not UrlConfig url)
                continue;

            _logger.Debug($"Unloading {path} instance");
            url.config.RemoveNodes("Instances");
            staticInstanceType.Invoke("Deactivate", instance, []);
            var uuid = staticInstanceType.GetField("UUID", instance) as string;
            apiType.Invoke("RemoveStatic", null, [uuid]);
        }

        allStaticInstances = (Array)staticDatabaseType.Field("allStaticInstances").GetValue(null);
        _logger.Info($"Loaded: {allStaticInstances.Length} instances");

        foreach (var instance in allStaticInstances)
        {
            var path = staticInstanceType.GetField("configPath", instance) as string;
            var uuid = staticInstanceType.GetField("UUID", instance) as string;
            _logger.Info($"Still loaded: {path} [{uuid}]");
        }

        if (!hasServerIntegration)
            return;

        var instancePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances");

        if (Directory.Exists(instancePath) && Directory.EnumerateFiles(instancePath).Any())
            Directory.Delete(instancePath, true);

        Directory.CreateDirectory(instancePath);

        ClientMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsRequestInstancesMessage(), false);
    }

    #endregion
}

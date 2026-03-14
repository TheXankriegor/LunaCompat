using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using JetBrains.Annotations;

using KSPBuildTools;

using LunaCompat.Attributes;
using LunaCompat.Utils;

using LunaCompatCommon.Messages;

using UnityEngine;

using static UrlDir;

using File = System.IO.File;
using FileInfo = System.IO.FileInfo;

namespace LunaCompat.Mods.KerbalKonstructs;

[LunaFix]
[UsedImplicitly]
internal class KerbalKonstructsCompat : ModCompat
{
    #region Fields

    private ModMessageHandler _modMessageHandler;
    private static MethodInfo saveConfigMethod;
    private static MethodInfo deleteInstanceMethod;
    private static FieldInfo groupCelestialBodyField;
    private static FieldInfo groupNameField;
    private static MethodInfo spawnGroupMethod;
    private static MethodInfo groupCenterUpdateInvokeMethod;
    private static FieldInfo onSavedField;
    private static MethodInfo updateGroupMethod;
    private static MethodInfo updateRotation2HeadingMethod;
    private static MethodInfo parseCFGNodeMethod;
    private static Type groupCenterType;
    private static FieldInfo staticInstanceIsInSavegameField;
    private static FieldInfo childInstancesField;
    private static FieldInfo groupConfigUrlField;
    private static FieldInfo isInSavegameField;
    private static FieldInfo groupConfigPathField;
    private static MethodInfo getStaticInstanceByUuidMethod;
    private static MethodInfo removeGroupMethod;
    private static FieldInfo allCentersField;
    private static Dictionary<string, object> groupDictionary;
    private static MethodInfo deactivateMethod;
    private static FieldInfo configUrlField;
    private static FieldInfo configPathField;
    private static FieldInfo allStaticInstancesField;
    private static FieldInfo guiInstanceField;
    private static MethodInfo toggleEditorMethod;
    private static MethodInfo isOpenMethod;
    private static FieldInfo instancePathField;
    private static Type kkCustomParametersType;
    private static MethodInfo removeStaticMethod;
    private static FieldInfo nameField;
    private static FieldInfo modelField;
    private static FieldInfo uuidField;
    private static Type staticInstanceType;
    private static MethodInfo getModelByNameMethod;
    private static MethodInfo onLevelWasLoadMethod;
    private static FieldInfo kkInstanceField;
    private static MethodInfo loadInstancesMethod;
    private static bool initialized;
    private static bool isDeleting;

    #endregion

    #region Properties

    public override string PackageName => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        _modMessageHandler = modMessageHandler;
        groupDictionary = new Dictionary<string, object>();

        ReflectKerbalKonstructsTypes();

        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(kkCustomParametersType, "Interactible"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixKKCustomParameter1Interactible)));

        // KerbalKonstructs.Core.StaticInstance.SaveConfig() is inlined and cannot be patched by Harmony.
        // Postfix KerbalKonstructs.Core.ConfigParser.SaveInstanceByCfg(string pathname) instead.
        var configParserType = AccessTools.TypeByName("KerbalKonstructs.Core.ConfigParser");
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(configParserType, "SaveInstanceByCfg"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixSaveInstanceByCfg)));

        LunaCompat.HarmonyInstance.Patch(deleteInstanceMethod, new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PrefixStaticInstanceDelete)));

        // groups KerbalKonstructs.Core.GroupCenter.Save DeleteGroupCenter
        var groupCenterType = AccessTools.TypeByName("KerbalKonstructs.Core.GroupCenter");
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(groupCenterType, "Save"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixSaveGroupCenter)));
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(groupCenterType, "DeleteGroupCenter"),
                                         prefix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PrefixDeleteGroupCenter)));

        _modMessageHandler.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructsChangeGroupCenterMessage>(OnChangeGroupCenterMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructsDeleteGroupCenterMessage>(OnDeleteGroupCenterMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructsChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructsDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructsRequestInstancesMessage>(OnAllInstancesAvailableMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();
        initialized = false;
        groupDictionary.Clear();
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectKerbalKonstructsTypes()
    {
        var kerbalKonstructsType = AccessTools.TypeByName("KerbalKonstructs.KerbalKonstructs");
        loadInstancesMethod = AccessTools.Method(kerbalKonstructsType, "LoadInstances");
        deleteInstanceMethod = AccessTools.Method(kerbalKonstructsType, "DeleteInstance");
        kkInstanceField = AccessTools.Field(kerbalKonstructsType, "instance");
        onLevelWasLoadMethod = AccessTools.Method(kerbalKonstructsType, "OnLevelWasLoad");

        var staticDatabaseType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticDatabase");
        getModelByNameMethod = AccessTools.Method(staticDatabaseType, "GetModelByName");
        allStaticInstancesField = AccessTools.Field(staticDatabaseType, "allStaticInstances");
        allCentersField = AccessTools.Field(staticDatabaseType, "allCenters");

        staticInstanceType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticInstance");
        uuidField = AccessTools.Field(staticInstanceType, "UUID");
        modelField = AccessTools.Field(staticInstanceType, "model");
        deactivateMethod = AccessTools.Method(staticInstanceType, "Deactivate");
        saveConfigMethod = AccessTools.Method(staticInstanceType, "SaveConfig");
        configPathField = AccessTools.Field(staticInstanceType, "configPath");
        configUrlField = AccessTools.Field(staticInstanceType, "configUrl");
        staticInstanceIsInSavegameField = AccessTools.Field(staticInstanceType, "isInSavegame");

        var staticModelType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticModel");
        nameField = AccessTools.Field(staticModelType, "name");

        var apiType = AccessTools.TypeByName("KerbalKonstructs.API");
        removeStaticMethod = AccessTools.Method(apiType, "RemoveStatic");
        getStaticInstanceByUuidMethod = AccessTools.Method(apiType, "getStaticInstanceByUUID");
        removeGroupMethod = AccessTools.Method(apiType, "RemoveGroup");
        onSavedField = AccessTools.Field(apiType, "OnGroupSaved");

        kkCustomParametersType = AccessTools.TypeByName("KerbalKonstructs.Core.KKCustomParameters1");
        instancePathField = kkCustomParametersType.GetField("newInstancePath");

        var staticsEditorGuiType = AccessTools.TypeByName("KerbalKonstructs.UI.StaticsEditorGUI");
        isOpenMethod = AccessTools.Method(staticsEditorGuiType, "IsOpen");
        toggleEditorMethod = AccessTools.Method(staticsEditorGuiType, "ToggleEditor");
        guiInstanceField = AccessTools.Field(staticsEditorGuiType, "_instance");

        groupCenterType = AccessTools.TypeByName("KerbalKonstructs.Core.GroupCenter");
        groupConfigPathField = AccessTools.Field(groupCenterType, "configPath");
        isInSavegameField = AccessTools.Field(groupCenterType, "isInSavegame");
        childInstancesField = AccessTools.Field(groupCenterType, "childInstances");
        groupConfigUrlField = AccessTools.Field(groupCenterType, "configUrl");
        parseCFGNodeMethod = AccessTools.Method(groupCenterType, "ParseCFGNode");
        updateRotation2HeadingMethod = AccessTools.Method(groupCenterType, "UpdateRotation2Heading");
        updateGroupMethod = AccessTools.Method(groupCenterType, "Update");
        groupCenterUpdateInvokeMethod = AccessTools.Method(typeof(Action<>).MakeGenericType(groupCenterType), "Invoke");
        spawnGroupMethod = AccessTools.Method(groupCenterType, "Spawn");

        groupNameField = AccessTools.Field(groupCenterType, "Group");
        groupCelestialBodyField = AccessTools.Field(groupCenterType, "CelestialBody");
    }

    private static void PrefixStaticInstanceDelete(ref object __0)
    {
        if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
            return;

        var uuid = uuidField.GetValue(__0) as string;
        var model = modelField.GetValue(__0);
        var name = nameField.GetValue(model) as string;

        Log.Message($"KerbalKonstructs delete: {name} ({uuid})");

        ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteStaticInstanceMessage
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
        if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var groupCenter = __instance;
            var existing = groupDictionary.SingleOrDefault(x => x.Value == groupCenter);

            var pathName = groupConfigPathField.GetValue(__instance) as string;
            var basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            var nodePath = Path.Combine(basePath, pathName);

            Log.Message($"KerbalKonstructs group deleted: ({nodePath})");

            if (File.Exists(nodePath))
                File.Delete(nodePath);

            if (existing.Key == null)
            {
                // did not exist in dict
                Log.Warning("Deleted group did not exist in LunaCompat dictionary.");
                return;
            }

            ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsDeleteGroupCenterMessage
            {
                Uuid = existing.Key,
            });
            groupDictionary.Remove(existing.Key);
        }

        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private static void PostfixSaveGroupCenter(ref object __instance)
    {
        if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            var pathName = groupConfigPathField.GetValue(__instance) as string;
            var isInSavegame = isInSavegameField.GetValue(__instance);

            // set isInSavegame to false to avoid KC scenario saves
            if (isInSavegame is true)
            {
                Log.Message($"KerbalKonstructs - fixing savegame setting for {__instance}");
                isInSavegameField.SetValue(__instance, false);
                var childInstances = (IEnumerable)childInstancesField.GetValue(__instance);

                foreach (var instance in childInstances)
                    staticInstanceIsInSavegameField.SetValue(instance, false);

                // just recall?
                var saveMethod = AccessTools.Method(groupCenterType, "Save");
                saveMethod.Invoke(__instance, []);
                return;
            }

            // check buildin/savegame
            if (string.IsNullOrEmpty(pathName))
                return;

            var basePath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData");
            var nodePath = Path.Combine(basePath, pathName);

            if (!nodePath.Contains("KerbalKonstructs/NewInstances"))
            {
                Log.Message($"Ignoring save for local group center: ({nodePath})");
                return;
            }

            Log.Message($"KerbalKonstructs group saved: ({nodePath})");

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

            Log.Debug($"KerbalKonstructs group sending: ({nodePath}, {uuid})");

            ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeGroupCenterMessage
            {
                Uuid = uuid,
                ModelName = Path.GetFileNameWithoutExtension(writtenFileName),
                Content = node.ToString()
            });

            var instances = (IEnumerable)childInstancesField.GetValue(__instance);

            foreach (var instance in instances)
                saveConfigMethod.Invoke(instance, []);
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private static void PostfixSaveInstanceByCfg(string pathname)
    {
        var nodePath = KSPUtil.ApplicationRootPath + "GameData/" + pathname;

        try
        {
            if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
                return;

            if (!nodePath.Contains("KerbalKonstructs/NewInstances"))
            {
                Log.Message($"Ignoring save for local instance: ({nodePath})");
                return;
            }

            var node = ConfigNode.Load(nodePath);
            var name = node.GetNode("STATIC")?.GetValue("pointername");

            Log.Message($"KerbalKonstructs saved: ({nodePath})");

            ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructsChangeStaticInstanceMessage
            {
                ModelName = name,
                Content = node.ToString()
            });
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private static void FixSaveLocations()
    {
        if (!ModMessageHandler.Instance.HasServerIntegration)
            return;

        var kkParameters = HighLogic.CurrentGame.Parameters.CustomParams(kkCustomParametersType);
        instancePathField.SetValue(kkParameters, "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances");
    }

    private static void OnDeleteStaticInstanceMessageReceived(KerbalKonstructsDeleteStaticInstanceMessage msg)
    {
        Log.Message($"KerbalKonstructs unload received: {msg.ModelName}");

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
                Log.Exception(ex);
            }
        });

        CloseUiIfOpen();
        RemoveInstanceByUuid(msg.Uuid);
    }

    private static void OnChangeStaticInstanceMessageReceived(KerbalKonstructsChangeStaticInstanceMessage msg)
    {
        Log.Message($"KerbalKonstructs received: {msg.ModelName}");

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
                Log.Exception(ex);
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
            Log.Warning($"KK group {targetPath} has no KK_GroupCenter component.");
            return;
        }

        // we need to replace groups whenever something changed. However without uuids, a renamed or changed group is annoying to detect. Instead, map all received groups via a custom uuid
        if (groupDictionary.TryGetValue(uuid, out var existing))
        {
            Log.Warning($"Updating KK group {targetPath}.");

            var refLatField = AccessTools.Field(groupCenterType, "RefLatitude");
            var refLngField = AccessTools.Field(groupCenterType, "RefLongitude");
            var bodyField = AccessTools.Field(groupCenterType, "CelestialBody");
            var groupField = AccessTools.Field(groupCenterType, "Group");
            var radialPosField = AccessTools.Field(groupCenterType, "RadialPosition");
            var rotationAngleField = AccessTools.Field(groupCenterType, "RotationAngle");
            var headingField = AccessTools.Field(groupCenterType, "Heading");
            var dynHeadingProp = AccessTools.Property(groupCenterType, "heading");

            var newName = groupNode.GetValue("Group");
            var currentName = groupField.GetValue(existing) as string;

            if (newName != currentName)
            {
                Log.Message($"Name changed, updating ({currentName} > {newName})");

                var renameGroupMethod = AccessTools.Method(groupCenterType, "RenameGroup");
                renameGroupMethod.Invoke(existing, [newName]);
            }

            // original 24.1468945

            // shown 15.147
            Log.Message($"KK Before: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");

            parseCFGNodeMethod.Invoke(existing, [groupNode]);

            // update RadialPosition
            var refLat = (double)refLatField.GetValue(existing);
            var refLng = (double)refLngField.GetValue(existing);
            var body = bodyField.GetValue(existing) as CelestialBody;
            radialPosField.SetValue(existing, (Vector3)(body?.GetRelSurfaceNVector(refLat, refLng).normalized * body.pqsController.radius));

            // update RotationAngle

            var upVectorProp = AccessTools.Property(groupCenterType, "upVector");
            var northVectorProp = AccessTools.Property(groupCenterType, "northVector");
            var gOField = AccessTools.Field(groupCenterType, "gameObject");
            var pqsCityField = AccessTools.Field(groupCenterType, "pqsCity");
            var gO = gOField.GetValue(existing) as GameObject;
            var rotation = Quaternion.AngleAxis((float)headingField.GetValue(existing), (Vector3)upVectorProp.GetValue(existing));
            gO.transform.forward = rotation * (Vector3)northVectorProp.GetValue(existing);

            Log.Message($"KK After0: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");

            // update RotationAngle
            rotationAngleField.SetValue(existing, headingField.GetValue(existing));

            Log.Message($"KK After1: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");
            updateGroupMethod.Invoke(existing, []);

            Log.Message($"KK After2: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");
            // apparnetly not enough
            var pqs = pqsCityField.GetValue(existing) as PQSCity;
            pqs.Orientate();
            // updateRotation2HeadingMethod.Invoke(existing, []);

            Log.Message($"KK After3: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");

            var onSaveAction = onSavedField.GetValue(null);
            groupCenterUpdateInvokeMethod.Invoke(onSaveAction, [existing]);

            Log.Message($"KK After4: {headingField.GetValue(existing)} | {dynHeadingProp.GetValue(existing)}");
        }
        else
        {
            Log.Warning($"Adding new KK group {targetPath}.");

            var config = new UrlConfig(collectionFile, groupNode);
            collectionFile.configs.Add(config);
            GameDatabase.Instance.root.children.First().files.Add(collectionFile);

            var groupCenter = Activator.CreateInstance(groupCenterType);
            parseCFGNodeMethod.Invoke(groupCenter, [groupNode]);
            groupConfigPathField.SetValue(groupCenter, nodePath);
            groupConfigUrlField.SetValue(groupCenter, config);
            spawnGroupMethod.Invoke(groupCenter, []);
            groupDictionary.Add(uuid, groupCenter);
        }

        Log.Warning($"Loaded KK groups: {groupDictionary.Count}.");

        var allCenters = (IDictionary)allCentersField.GetValue(null);
        Log.Message($"Loaded in KK: {allCenters.Count} instances");
    }

    private static void CloseUiIfOpen()
    {
        var instance = guiInstanceField.GetValue(null);

        if (instance != null && isOpenMethod.Invoke(instance, []) is bool and true)
            toggleEditorMethod.Invoke(instance, []);
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
            Log.Warning($"KK instance {targetPath} has no STATIC component.");
            return;
        }

        isDeleting = true;

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            RemoveInstanceByUuid(uuid);
        }

        isDeleting = false;

        var config = new UrlConfig(collectionFile, staticNode);
        collectionFile.configs.Add(config);
        GameDatabase.Instance.root.children.First().files.Add(collectionFile);

        var modelName = staticNode.GetValue("pointername");
        var model = getModelByNameMethod.Invoke(null, [modelName]);

        if (model != null)
            loadInstancesMethod.Invoke(kkInstanceField.GetValue(null), [config, model]);

        var subPath = $"../saves/LunaMultiplayer/KerbalKonstructs/NewInstances/{Path.GetFileName(targetPath)}";

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            var instance = getStaticInstanceByUuidMethod.Invoke(null, [uuid]);

            if (instance == null)
                Log.Message($"No UUID {uuid} for '{targetPath}'");
            else
                configPathField.SetValue(instance, subPath);
        }
        // get by uuid 
        // update configPath to targetPath

        var allStaticInstances = (Array)allStaticInstancesField.GetValue(null);
        Log.Message($"Loaded: {allStaticInstances.Length} instances");

        if (!initialized)
            return;

        onLevelWasLoadMethod.Invoke(kkInstanceField.GetValue(null), [HighLogic.LoadedScene]);
    }

    private static void RemoveInstanceByUuid(string uuid)
    {
        removeStaticMethod.Invoke(null, [uuid]);
    }

    private static void OnAllInstancesAvailableMessageReceived(KerbalKonstructsRequestInstancesMessage msg)
    {
        onLevelWasLoadMethod.Invoke(kkInstanceField.GetValue(null), [HighLogic.LoadedScene]);
        initialized = true;
    }

    private void OnDeleteGroupCenterMessageReceived(KerbalKonstructsDeleteGroupCenterMessage msg)
    {
        if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
            return;

        try
        {
            if (!groupDictionary.TryGetValue(msg.Uuid, out var group))
            {
                Log.Warning("Deleted group on server did not exist locally.");
                return;
            }

            Log.Message($"KerbalKonstructs received group center deletion: {msg.Uuid}");

            var groupName = groupNameField.GetValue(group);
            var body = groupCelestialBodyField.GetValue(group) as CelestialBody;

            isDeleting = true;

            CloseUiIfOpen();

            removeGroupMethod.Invoke(null, [groupName, body?.name]);
            groupDictionary.Remove(msg.Uuid);

            isDeleting = false;
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
    }

    private void OnChangeGroupCenterMessageReceived(KerbalKonstructsChangeGroupCenterMessage msg)
    {
        Log.Message($"KerbalKonstructs received group center update: {msg.Uuid} - {msg.ModelName}");

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
                Log.Exception(ex);
            }
        });

        CloseUiIfOpen();
        var node = ConfigNode.Parse(msg.Content);
        LoadGroup(msg.Uuid, targetPath, node, $"../{subPath}");
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        _modMessageHandler.HasServerIntegrationChanged -= OnServerIntegrationDetermined;

        FixSaveLocations();

        // also unload groups

        var allStaticInstances = (Array)allStaticInstancesField.GetValue(null);

        foreach (var instance in allStaticInstances)
        {
            var path = configPathField.GetValue(instance) as string;

            if (string.IsNullOrEmpty(path) || !path.Contains("KerbalKonstructs/NewInstances") || configUrlField.GetValue(instance) is not UrlConfig url)
                continue;

            Log.Debug($"Unloading {path} instance");
            url.config.RemoveNodes("Instances");
            deactivateMethod.Invoke(instance, []);
            var uuid = uuidField.GetValue(instance) as string;
            RemoveInstanceByUuid(uuid);
        }

        allStaticInstances = (Array)allStaticInstancesField.GetValue(null);
        Log.Message($"Loaded: {allStaticInstances.Length} instances");

        foreach (var instance in allStaticInstances)
        {
            var path = configPathField.GetValue(instance) as string;
            var uuid = uuidField.GetValue(instance) as string;
            Log.Message($"Still loaded: {path} [{uuid}]");
        }

        if (!hasServerIntegration)
            return;

        var instancePath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances");

        if (Directory.Exists(instancePath) && Directory.EnumerateFiles(instancePath).Any())
            Directory.Delete(instancePath, true);

        Directory.CreateDirectory(instancePath);

        _modMessageHandler.SendReliableMessage(new KerbalKonstructsRequestInstancesMessage(), false);
    }

    #endregion
}

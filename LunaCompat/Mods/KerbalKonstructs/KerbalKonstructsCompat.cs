using System;
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

using static UrlDir;

namespace LunaCompat.Mods.KerbalKonstructs;

[LunaFix]
[UsedImplicitly]
internal class KerbalKonstructsCompat : ModCompat
{
    #region Fields

    private ModMessageHandler _modMessageHandler;
    private static FieldInfo selectedInstanceField;
    private static Type editorGuiType;
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

        ReflectKerbalKonstructsTypes();

        // KerbalKonstructs.Core.StaticInstance.SaveConfig() is inlined and cannot be patched by Harmony.
        // Postfix KerbalKonstructs.Core.ConfigParser.SaveInstanceByCfg(string pathname) instead.
        var configParserType = AccessTools.TypeByName("KerbalKonstructs.Core.ConfigParser");
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(configParserType, "SaveInstanceByCfg"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixSaveInstanceByCfg)));

        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(kkCustomParametersType, "Interactible"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixKKCustomParameter1Interactible)));

        var deleteInstanceMethod = AccessTools.Method(editorGuiType, "DeleteInstance");
        LunaCompat.HarmonyInstance.Patch(deleteInstanceMethod, new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PrefixStaticInstanceDelete)));

        _modMessageHandler.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructDeleteStaticInstanceMessage>(OnDeleteStaticInstanceMessageReceived);
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructRequestInstancesMessage>(OnAllInstancesAvailableMessageReceived);
    }

    public override void Destroy()
    {
        base.Destroy();
        initialized = false;
    }

    #endregion

    #region Non-Public Methods

    private static void ReflectKerbalKonstructsTypes()
    {
        var kerbalKonstructsType = AccessTools.TypeByName("KerbalKonstructs.KerbalKonstructs");
        loadInstancesMethod = AccessTools.Method(kerbalKonstructsType, "LoadInstances");
        kkInstanceField = AccessTools.Field(kerbalKonstructsType, "instance");
        onLevelWasLoadMethod = AccessTools.Method(kerbalKonstructsType, "OnLevelWasLoad");

        var staticDatabaseType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticDatabase");
        getModelByNameMethod = AccessTools.Method(staticDatabaseType, "GetModelByName");
        allStaticInstancesField = AccessTools.Field(staticDatabaseType, "allStaticInstances");

        staticInstanceType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticInstance");
        uuidField = AccessTools.Field(staticInstanceType, "UUID");
        modelField = AccessTools.Field(staticInstanceType, "model");
        deactivateMethod = AccessTools.Method(staticInstanceType, "Deactivate");
        configPathField = AccessTools.Field(staticInstanceType, "configPath");
        configUrlField = AccessTools.Field(staticInstanceType, "configUrl");

        var staticModelType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticModel");
        nameField = AccessTools.Field(staticModelType, "name");

        var apiType = AccessTools.TypeByName("KerbalKonstructs.API");
        removeStaticMethod = AccessTools.Method(apiType, "RemoveStatic");

        kkCustomParametersType = AccessTools.TypeByName("KerbalKonstructs.Core.KKCustomParameters1");
        instancePathField = kkCustomParametersType.GetField("newInstancePath");

        var staticsEditorGuiType = AccessTools.TypeByName("KerbalKonstructs.UI.StaticsEditorGUI");
        isOpenMethod = AccessTools.Method(staticsEditorGuiType, "IsOpen");
        toggleEditorMethod = AccessTools.Method(staticsEditorGuiType, "ToggleEditor");
        guiInstanceField = AccessTools.Field(staticsEditorGuiType, "_instance");

        editorGuiType = AccessTools.TypeByName("KerbalKonstructs.UI.EditorGUI");
        selectedInstanceField = AccessTools.Field(editorGuiType, "selectedInstance");
    }

    private static void PrefixStaticInstanceDelete()
    {
        if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
            return;

        // prefix so that we can access selectedInstance
        var selected = selectedInstanceField.GetValue(null);

        var uuid = uuidField.GetValue(selected) as string;
        var model = modelField.GetValue(selected);
        var name = nameField.GetValue(model) as string;

        Log.Message($"KerbalKonstructs delete: {name} ({uuid})");

        ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructDeleteStaticInstanceMessage
        {
            ModelName = name,
            Uuid = uuid
        });
    }

    private static void PostfixKKCustomParameter1Interactible()
    {
        FixSaveLocations();
    }

    private static void PostfixSaveInstanceByCfg(string pathname)
    {
        var nodePath = KSPUtil.ApplicationRootPath + "GameData/" + pathname;

        try
        {
            if (!initialized || isDeleting || !ModMessageHandler.Instance.HasServerIntegration)
                return;

            var node = ConfigNode.Load(nodePath);
            var name = node.GetNode("STATIC")?.GetValue("pointername");

            Log.Message($"KerbalKonstructs saved: ({nodePath})");

            ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructChangeStaticInstanceMessage
            {
                ModelName = name,
                Content = node.ToString()
            });
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
        }
        finally
        {
            File.Delete(nodePath);
        }
    }

    private static void FixSaveLocations()
    {
        if (!ModMessageHandler.Instance.HasServerIntegration)
            return;

        var kkParameters = HighLogic.CurrentGame.Parameters.CustomParams(kkCustomParametersType);
        instancePathField.SetValue(kkParameters, "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances");
    }

    private static void OnDeleteStaticInstanceMessageReceived(KerbalKonstructDeleteStaticInstanceMessage msg)
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

    private static void OnChangeStaticInstanceMessageReceived(KerbalKonstructChangeStaticInstanceMessage msg)
    {
        Log.Message($"KerbalKonstructs received: {msg.ModelName}");

        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "saves/LunaMultiplayer/KerbalKonstructs/NewInstances", $"{msg.ModelName}.cfg");

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

    private static void CloseUiIfOpen()
    {
        var instance = guiInstanceField.GetValue(null);

        if (instance != null && isOpenMethod.Invoke(instance, []) is bool and true)
            toggleEditorMethod.Invoke(instance, []);
    }

    private static void LoadInstance(string targetPath, ConfigNode node)
    {
        // uri for LunaCompat to ensure valid save location on update
        var collectionFile = new UrlFile(GameDatabase.Instance.root.AllDirectories.Single(x => x.url == nameof(LunaCompat)), new FileInfo(targetPath));
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

    private static void OnAllInstancesAvailableMessageReceived(KerbalKonstructRequestInstancesMessage msg)
    {
        onLevelWasLoadMethod.Invoke(kkInstanceField.GetValue(null), [HighLogic.LoadedScene]);
        initialized = true;
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        _modMessageHandler.HasServerIntegrationChanged -= OnServerIntegrationDetermined;

        FixSaveLocations();

        var allStaticInstances = (Array)allStaticInstancesField.GetValue(null);

        foreach (var instance in allStaticInstances)
        {
            var path = configPathField.GetValue(instance) as string;

            if (string.IsNullOrEmpty(path) || configUrlField.GetValue(instance) is not UrlConfig url)
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

        var saveInstancePath = "saves/LunaMultiplayer/KerbalKonstructs/NewInstances";
        var instancePath = Path.Combine(KSPUtil.ApplicationRootPath, saveInstancePath);

        if (Directory.Exists(instancePath) && Directory.EnumerateFiles(instancePath).Any())
            Directory.Delete(instancePath, true);

        Directory.CreateDirectory(instancePath);

        _modMessageHandler.SendReliableMessage(new KerbalKonstructRequestInstancesMessage(), false);
    }

    #endregion
}

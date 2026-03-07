using System;
using System.IO;
using System.Linq;

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

    #endregion

    #region Properties

    public override string PackageName => "KerbalKonstructs";

    #endregion

    #region Public Methods

    public override void Patch(ModMessageHandler modMessageHandler, ConfigNode node)
    {
        _modMessageHandler = modMessageHandler;

        // KerbalKonstructs.Core.StaticInstance.SaveConfig() is inlined and cannot be patched by Harmony.
        // Postfix KerbalKonstructs.Core.ConfigParser.SaveInstanceByCfg(string pathname) instead.
        var configParserType = AccessTools.TypeByName("KerbalKonstructs.Core.ConfigParser");
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(configParserType, "SaveInstanceByCfg"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixSaveInstanceByCfg)));

        var kkCustomParametersType = AccessTools.TypeByName("KerbalKonstructs.Core.KKCustomParameters1");
        LunaCompat.HarmonyInstance.Patch(AccessTools.Method(kkCustomParametersType, "Interactible"),
                                         postfix: new HarmonyMethod(typeof(KerbalKonstructsCompat), nameof(PostfixKKCustomParameter1Interactible)));

        _modMessageHandler.HasServerIntegrationChanged += OnServerIntegrationDetermined;
        _modMessageHandler.RegisterModMessageListener<KerbalKonstructChangeStaticInstanceMessage>(OnChangeStaticInstanceMessageReceived);
    }

    #endregion

    #region Non-Public Methods

    private static void PostfixKKCustomParameter1Interactible()
    {
        FixSaveLocations();
    }

    private static void PostfixSaveInstanceByCfg(string pathname)
    {
        var nodePath = KSPUtil.ApplicationRootPath + "GameData/" + pathname;

        try
        {
            if (!ModMessageHandler.Instance.HasServerIntegration)
                return;

            var node = ConfigNode.Load(nodePath);

            Log.Message($"KerbalKonstructs saved: {node} ({nodePath})");

            ModMessageHandler.Instance.SendReliableMessage(new KerbalKonstructChangeStaticInstanceMessage
            {
                PathName = Path.GetFileName(pathname),
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

        var kkCustomParametersType = AccessTools.TypeByName("KerbalKonstructs.Core.KKCustomParameters1");
        var kkParameters = HighLogic.CurrentGame.Parameters.CustomParams(kkCustomParametersType);
        var instancePathField = kkCustomParametersType.GetField("newInstancePath");

        instancePathField.SetValue(kkParameters, "../saves/LunaMultiplayer/KerbalKonstructs/NewInstances");
    }

    private static void OnChangeStaticInstanceMessageReceived(KerbalKonstructChangeStaticInstanceMessage msg)
    {
        Log.Message($"KerbalKonstructs received: {msg.PathName}");

        var targetPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", msg.PathName);
        File.WriteAllText(targetPath, msg.Content);

        LoadInstance(targetPath);
    }

    private static void CloseUiIfOpen()
    {
        var staticsEditorGuiType = AccessTools.TypeByName("KerbalKonstructs.UI.StaticsEditorGUI");
        var isOpenMethod = AccessTools.Method(staticsEditorGuiType, "IsOpen");
        var closeMethod = AccessTools.Method(staticsEditorGuiType, "Close");
        var instanceField = AccessTools.Field(staticsEditorGuiType, "_instance");
        var instance = instanceField.GetValue(null);

        if (instance != null && isOpenMethod.Invoke(instance, []) is bool and true)
            closeMethod.Invoke(instance, []);
    }

    private static void LoadInstance(string targetPath)
    {
        CloseUiIfOpen();

        // uri for LunaCompat to ensure valid save location on update
        var collectionFile = new UrlFile(GameDatabase.Instance.root.AllDirectories.Single(x => x.url == nameof(LunaCompat)), new FileInfo(targetPath));
        var node = ConfigNode.Load(targetPath);
        var staticNode = node.GetNode("root")?.GetNode("STATIC");

        if (staticNode == null)
        {
            Log.Warning($"KK instance {targetPath} has no STATIC component.");
            return;
        }

        foreach (var n in staticNode.GetNodes("Instances"))
        {
            var uuid = n.GetValue("UUID");
            RemoveInstanceByUuid(uuid);
        }

        var config = new UrlConfig(collectionFile, staticNode);
        collectionFile.configs.Add(config);
        GameDatabase.Instance.root.children.First().files.Add(collectionFile);

        var modelname = staticNode.GetValue("pointername");

        var staticDatabaseType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticDatabase");
        var allStaticInstancesMethod = AccessTools.Method(staticDatabaseType, "GetModelByName");
        var model = allStaticInstancesMethod.Invoke(null, [modelname]);

        var kerbalKonstructsType = AccessTools.TypeByName("KerbalKonstructs.KerbalKonstructs");
        var loadInstancesMethod = AccessTools.Method(kerbalKonstructsType, "LoadInstances");
        var kkInstanceField = AccessTools.Field(kerbalKonstructsType, "instance");

        if (model != null)
            loadInstancesMethod.Invoke(kkInstanceField.GetValue(null), [config, model]);

        var onLevelWasLoadMethod = AccessTools.Method(kerbalKonstructsType, "OnLevelWasLoad");
        onLevelWasLoadMethod.Invoke(kkInstanceField.GetValue(null), [GameScenes.SPACECENTER]);

        var allStaticInstancesField = AccessTools.Field(staticDatabaseType, "allStaticInstances");
        var allStaticInstances = (Array)allStaticInstancesField.GetValue(null);
        Log.Message($"Total instances loaded: {allStaticInstances.Length}s");
    }

    private static void RemoveInstanceByUuid(string uuid)
    {
        var apiType = AccessTools.TypeByName("KerbalKonstructs.API");
        var removeStaticMethod = AccessTools.Method(apiType, "RemoveStatic");
        removeStaticMethod.Invoke(null, [uuid]);
    }

    private void OnServerIntegrationDetermined(object sender, bool hasServerIntegration)
    {
        _modMessageHandler.HasServerIntegrationChanged -= OnServerIntegrationDetermined;

        FixSaveLocations();

        var staticDatabaseType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticDatabase");
        var allStaticInstancesField = AccessTools.Field(staticDatabaseType, "allStaticInstances");
        var allStaticInstances = (Array)allStaticInstancesField.GetValue(null);

        var staticInstanceType = AccessTools.TypeByName("KerbalKonstructs.Core.StaticInstance");
        var deactivateMethod = AccessTools.Method(staticInstanceType, "Deactivate");
        var configPathField = AccessTools.Field(staticInstanceType, "configPath");
        var configUrlField = AccessTools.Field(staticInstanceType, "configUrl");
        var uuidField = AccessTools.Field(staticInstanceType, "UUID");

        foreach (var instance in allStaticInstances)
        {
            var path = configPathField.GetValue(instance) as string;
            var url = configUrlField.GetValue(instance) as UrlConfig;

            if (!string.IsNullOrEmpty(path) && path.Contains("NewInstances"))
            {
                Log.Message($"Unloading {path} instance");
                url.config.RemoveNodes("Instances");
                deactivateMethod.Invoke(instance, []);
                var uuid = uuidField.GetValue(instance) as string;
                RemoveInstanceByUuid(uuid);
            }
        }

        if (!hasServerIntegration)
            return;

        var saveInstancePath = "saves/LunaMultiplayer/KerbalKonstructs/NewInstances";
        var instancePath = Path.Combine(KSPUtil.ApplicationRootPath, saveInstancePath);

        if (!Directory.Exists(instancePath))
            Directory.CreateDirectory(instancePath);

        if (Directory.EnumerateFiles(instancePath).Any())
            Directory.Delete(instancePath, true);

        _modMessageHandler.SendReliableMessage(new KerbalKonstructRequestInstancesMessage(), false);
    }

    #endregion
}

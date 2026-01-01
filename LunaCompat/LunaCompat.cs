using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using KSPBuildTools;

using LmpClient;
using LmpClient.Events;

using LmpCommon.Enums;

using LunaCompat.Attributes;
using LunaCompat.Utils;

using UnityEngine;

namespace LunaCompat;

[KSPAddon(KSPAddon.Startup.MainMenu, true)]
public class LunaCompat : MonoBehaviour
{
    private const string ConfigFilePath = $"GameData/{nameof(LunaCompat)}/{nameof(LunaCompat)}.cfg";

    public static Harmony HarmonyInstance = new("LunaCompat");

    private readonly HashSet<ModCompat> _activePatches = [];
    private ModMessageHandler _modMessageHandler;
    private ConfigNode _node;

    public static LunaCompat Singleton { get; set; }

    private void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(this);

        if (!MainSystem.Singleton || !MainSystem.Singleton.Enabled)
        {
            Log.Error("Luna Multiplayer does not appear to be running.");
            return;
        }

        _modMessageHandler = new ModMessageHandler();

        NetworkEvent.onNetworkStatusChanged.Add(OnLmpNetworkStatusChanged);

        _node = ConfigNode.Load(KSPUtil.ApplicationRootPath + ConfigFilePath);

        if (_node == null)
        {
            Log.Error($"Failed to locate config file '{ConfigFilePath}'.");
            return;
        }

        // We could load external fixes here as well - but will that ever be needed?
        var queue = Assembly.GetAssembly(typeof(LunaCompat)).GetTypes().Where(IsLunaFix);

        foreach (var type in queue)
            SetupModCompat(type, _node);

        Log.Message("Xan's Luna Compat Plugin started.");
    }

    private void OnDestroy()
    {
        foreach (var patch in _activePatches)
            patch.Destroy();

        _modMessageHandler.Destroy();
        NetworkEvent.onNetworkStatusChanged?.Remove(OnLmpNetworkStatusChanged);
    }

    private void SetupModCompat(Type type, ConfigNode node)
    {
        try
        {
            var compatInstance = (ModCompat)Activator.CreateInstance(type);

            if (!AssemblyLoader.loadedAssemblies.Contains(compatInstance.PackageName))
                return;

            _activePatches.Add(compatInstance);

            compatInstance.Patch(_modMessageHandler, node);

            Log.Message($"Initialized compatibility for {compatInstance.PackageName}");
        }
        catch (Exception e)
        {
            Log.Error($"Exception loading {type.Name}: {e}");
        }
    }

    private void OnLmpNetworkStatusChanged(ClientState data)
    {
        // Test for Compat plugin
        if (data == ClientState.Running)
        {
            const string PACKAGE_NAME = "Init";

            var serverModConfirmed = false;
            Log.Message("Testing for Luna Compat Server Plugin...");

            _modMessageHandler.RegisterModMessageListener<LunaCompatInit>(PACKAGE_NAME, message =>
            {
                Log.Message($"Received Luna Compat Server Plugin: {message.Version}");
                serverModConfirmed = true;
                _node.SetValue("HasServerCompatPlugin", serverModConfirmed, true);
            });

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _modMessageHandler.SendReliableMessage(PACKAGE_NAME, new LunaCompatInit
            {
                Version = version
            }, false);

            // If no reply within 5 seconds
            Task.Run(async () =>
            {
                await Task.Delay(5000);

                if (!serverModConfirmed)
                {
                    Log.Message("Luna Compat server mod missing");
                    _node.SetValue("HasServerCompatPlugin", serverModConfirmed, true);
                }

                _modMessageHandler.UnregisterModMessageListener(PACKAGE_NAME);
            });
        }
    }

    private static bool IsLunaFix(Type type)
    {
        var attributes = type.GetCustomAttributes<LunaFixAttribute>(false);
        return attributes.Any();
    }
}

[Serializable]
internal class LunaCompatInit : IModMessage
{
    #region Properties

    public string Version { get; set; }

    #endregion
}

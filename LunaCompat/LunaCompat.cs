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

using LunaCompatCommon.Messages;

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
        if (data != ClientState.Running)
            return;

        var serverModConfirmed = false;
        Log.Message("Testing for Luna Compat Server Plugin...");

        var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        _modMessageHandler.RegisterModMessageListener<InitializeMessage>(message =>
        {
            Log.Message($"Received Luna Compat Server Plugin: {message.Version}");

            if (message.Version != version)
            {
                Log.Warning(
                    $"The Luna Compat Server Plugin does not match the installed version: Client: {version}, Server: {message.Version} - Contact the server owner for assistance.");
            }

            serverModConfirmed = true;
            _node.SetValue("HasServerCompatPlugin", serverModConfirmed, true);
        });

        _modMessageHandler.SendReliableMessage(new InitializeMessage
        {
            Version = version
        }, false);

        // If no reply within 5 seconds
        Task.Run(async () =>
        {
            await Task.Delay(5000);

            if (!serverModConfirmed)
            {
                Log.Warning("Luna Compat Server Plugin is missing. Contact the server owner for assistance.");
                _node.SetValue("HasServerCompatPlugin", serverModConfirmed, true);
            }

            _modMessageHandler.UnregisterModMessageListener<InitializeMessage>();
        });
    }

    private static bool IsLunaFix(Type type)
    {
        var attributes = type.GetCustomAttributes<LunaFixAttribute>(false);
        return attributes.Any();
    }
}

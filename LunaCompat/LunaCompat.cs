using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using KSPBuildTools;

using LmpClient;

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

        var node = ConfigNode.Load(KSPUtil.ApplicationRootPath + ConfigFilePath);

        if (node == null)
        {
            Log.Error($"Failed to locate config file '{ConfigFilePath}'.");
            return;
        }

        _modMessageHandler = new ModMessageHandler();

        // We could load external fixes here as well - but will that ever be needed?
        var queue = Assembly.GetAssembly(typeof(LunaCompat)).GetTypes().Where(IsLunaFix);

        foreach (var type in queue)
        {
            try
            {
                var compatInstance = (ModCompat)Activator.CreateInstance(type);

                if (!AssemblyLoader.loadedAssemblies.Contains(compatInstance.PackageName))
                    continue;

                _activePatches.Add(compatInstance);

                compatInstance.Patch(_modMessageHandler, node);

                Log.Message($"Initialized compatibility for {compatInstance.PackageName}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception loading {type.Name}: {e}");
            }
        }

        Log.Message("Xan's Luna Compat Plugin started.");
    }

    private void OnDestroy()
    {
        foreach (var patch in _activePatches)
            patch.Destroy();

        _modMessageHandler.Destroy();
    }

    private static bool IsLunaFix(Type type)
    {
        var attributes = type.GetCustomAttributes<LunaFixAttribute>(false);
        return attributes.Any();
    }
}

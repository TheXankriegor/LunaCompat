using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using KSPBuildTools;

using LmpClient;

using LunaFixes.Attributes;

using UnityEngine;

namespace LunaFixes;

[KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
public class LunaFixes : MonoBehaviour
{
    #region Public Methods

    public static LunaFixes Singleton { get; set; }

    public static Harmony HarmonyInstance = new("LunaFixes");

    public void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(this);

        if (!MainSystem.Singleton || !MainSystem.Singleton.Enabled)
            return;

        // does KSP use AssemblyLoader?
        var queue = Assembly.GetAssembly(typeof(LunaFixes))
                            .GetTypes()
                            .Where(IsLunaFix)
                            .SelectMany(t => t.GetCustomAttributes<LunaFixForAttribute>(false), (type, attr) => (type, attr));

        foreach (var (type, attr) in queue)
        {
            try
            {
                Activator.CreateInstance(type, attr);

                Log.Message($"Initialized compatibility for {attr.PackageId}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception loading {attr.PackageId}: {e}");
            }
        }

        Log.Message("Xan's Luna Fixes Plugin started.");

        HarmonyInstance.PatchAll();
    }

    private static bool IsLunaFix(Type type)
    {
        var attributes = type.GetCustomAttributes<LunaFixForAttribute>(false);
        return attributes.Any();
    }

    #endregion
}

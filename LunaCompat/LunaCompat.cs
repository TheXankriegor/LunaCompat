using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HarmonyLib;

using LmpClient;
using LmpClient.Events;

using LmpCommon.Enums;

using LunaCompat.Utils;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using UnityEngine;

using ILogger = LunaCompatCommon.Utils.ILogger;
using Logger = LunaCompat.Utils.Logger;

namespace LunaCompat;

[KSPAddon(KSPAddon.Startup.MainMenu, true)]
public class LunaCompat : MonoBehaviour
{
    private const string ConfigFilePath = $"GameData/{nameof(LunaCompat)}/{nameof(LunaCompat)}.cfg";

    public static Harmony HarmonyInstance = new("LunaCompat");

    private readonly HashSet<ClientModIntegration> _activePatches = [];
    private FileInteractionHandler _fileInteractionHandler;
    private ILogger _logger;
    private ClientMessageHandler _messageHandler;
    private ModSettingsProvider _settingsProvider;

    public static LunaCompat Singleton { get; set; }

    private void Awake()
    {
        Singleton = this;
        _logger = new Logger();

        DontDestroyOnLoad(this);

        if (!MainSystem.Singleton || !MainSystem.Singleton.Enabled)
        {
            _logger.Error("Luna Multiplayer does not appear to be running.");
            return;
        }

        _fileInteractionHandler = new FileInteractionHandler(_logger);
        _messageHandler = new ClientMessageHandler(_logger);

        NetworkEvent.onNetworkStatusChanged.Add(OnLmpNetworkStatusChanged);

        _settingsProvider = new ModSettingsProvider(KSPUtil.ApplicationRootPath + ConfigFilePath, _logger);
        _settingsProvider.TryLoadSettings();

        // We could load external fixes here as well - but will that ever be needed?
        var modIntegrations = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(ClientModIntegration).IsAssignableFrom(x) && !x.IsAbstract).ToList();

        foreach (var type in modIntegrations)
            SetupModCompat(type);

        _logger.Info("Xan's Luna Compat Plugin started.");
    }

    private void FixedUpdate()
    {
        // check if anything needs to run in unity context
        _fileInteractionHandler.Update();
    }

    private void OnDestroy()
    {
        foreach (var patch in _activePatches)
            patch.Destroy();

        _messageHandler.Dispose();

        NetworkEvent.onNetworkStatusChanged?.Remove(OnLmpNetworkStatusChanged);
    }

    private void SetupModCompat(Type type)
    {
        try
        {
            var compatInstance = (ClientModIntegration)Activator.CreateInstance(type, _logger, _settingsProvider);

            if (bool.TryParse(_settingsProvider.GetValue(compatInstance.PackageName, compatInstance.IsIntegrationEnabledKey, true) as string,
                              out var integrationEnabled))
            {
                if (!integrationEnabled)
                {
                    _logger.Info($"{compatInstance.PackageName} is disabled.");
                    return;
                }
            }
            else
                _logger.Error($"Failed to read {compatInstance.PackageName} switch - are the settings corrupt?.");

            if (!AssemblyLoader.loadedAssemblies.Contains(compatInstance.PackageName))
                return;

            compatInstance.Setup();
            _activePatches.Add(compatInstance);

            _logger.Info($"Initialized compatibility for {compatInstance.PackageName}");
        }
        catch (Exception e)
        {
            _logger.Error($"Exception loading {type.Name}: {e}");
        }
    }

    private void OnLmpNetworkStatusChanged(ClientState data)
    {
        // Test for Compat plugin
        if (data != ClientState.Running)
            return;

        var serverModConfirmed = false;
        _logger.Info("Testing for Luna Compat Server Plugin...");

        var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        _messageHandler.RegisterModMessageListener<InitializeMessage>(message =>
        {
            _logger.Info($"Received Luna Compat Server Plugin: {message.Version}");

            if (message.Version != version)
            {
                _logger.Warning(
                    $"The Luna Compat Server Plugin does not match the installed version: Client: {version}, Server: {message.Version} - Contact the server owner for assistance.");
            }

            serverModConfirmed = true;
            _messageHandler.SetServerIntegrationDetermined(true);
        });

        _messageHandler.SendReliableMessage(new InitializeMessage
        {
            Version = version
        }, false);

        // If no reply within 15 seconds - due to this happening on load the communication can be VERY delayed
        Task.Run(async () =>
        {
            await Task.Delay(15000);

            if (!serverModConfirmed)
            {
                _logger.Warning("Luna Compat Server Plugin is missing. Contact the server owner for assistance.");
                _messageHandler.SetServerIntegrationDetermined(false);
            }

            _messageHandler.UnregisterModMessageListener<InitializeMessage>();
        });
    }
}

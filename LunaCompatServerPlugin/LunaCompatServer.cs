using System.Reflection;

using LmpCommon.Enums;
using LmpCommon.Message.Client;
using LmpCommon.Message.Interface;

using LunaCompatCommon.Messages;
using LunaCompatCommon.Utils;

using LunaCompatServerPlugin.ModSettings;
using LunaCompatServerPlugin.Utils;

using Server.Client;
using Server.Plugin;

namespace LunaCompatServerPlugin;

public class LunaCompatServer : LmpPlugin
{
    #region Fields

    private readonly Dictionary<string, ServerModIntegration> _modIntegrations;
    private readonly ModSettingsProvider _settingsProvider;
    private readonly ServerMessageHandler _messageHandler;
    private readonly ILogger _logger;

    #endregion

    #region Constructors

    public LunaCompatServer()
    {
        _logger = new Logger();

        var modSettingsPath = Path.Combine(GetLunaCompatBaseDirectory(), "ModSettingsStructure.xml");
        _modIntegrations = new Dictionary<string, ServerModIntegration>();
        _settingsProvider = new ModSettingsProvider(modSettingsPath, _logger);

        _messageHandler = new ServerMessageHandler(_logger);
        _messageHandler.RegisterModMessageListener<InitializeMessage>(OnInitializeMessageReceived);
    }

    #endregion

    #region Public Methods

    public static string GetLunaCompatBaseDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Universe", "LunaCompat");
    }

    public override void OnServerStart()
    {
        _logger.Info("Luna Compat: Loading mod settings storage");

        // We could load external fixes here as well - but will that ever be needed?
        var modIntegrations = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo(typeof(ServerModIntegration)) && !x.IsAbstract).ToList();

        if (!modIntegrations.Any())
        {
            _logger.Error("No Luna Compat integrations found.");
            return;
        }

        var settingsValid = _settingsProvider.TryLoadSettings();

        foreach (var integration in modIntegrations)
        {
            try
            {
                var instance = (ServerModIntegration)Activator.CreateInstance(integration, _logger, _messageHandler)!;

                if (!settingsValid)
                    instance.InitializeSettings(_settingsProvider);

                var isEnabled = _settingsProvider.GetValue(instance.PackageName, instance.IsIntegrationEnabledKey, true);

                if (isEnabled is bool and false)
                {
                    _logger.Info($"{instance.PackageName} is disabled.");
                    return;
                }

                instance.Setup();
                _modIntegrations.Add(integration.Name, instance);
                _logger.Info($"Setup mod integration '{integration.Name}' ({instance.PackageName})");
            }
            catch (Exception e)
            {
                _logger.Error($"Exception loading {integration.Name}: {e}");
            }
        }
    }

    public override void OnMessageReceived(ClientStructure client, IClientMessageBase messageData)
    {
        if (messageData.MessageType != ClientMessageType.Mod || messageData is not ModCliMsg clientMessage)
            return;

        _messageHandler.HandleReceivedMessage(client, clientMessage);
    }

    public override void OnServerStop()
    {
        base.OnServerStop();

        _messageHandler.UnregisterModMessageListener<InitializeMessage>();

        foreach (var integration in _modIntegrations)
            integration.Value.Destroy();

        _modIntegrations.Clear();
    }

    #endregion

    #region Non-Public Methods

    private void OnInitializeMessageReceived(ClientStructure client, InitializeMessage msg)
    {
        _logger.Info($"Initializing LMP compatibility for player {client.PlayerName}.");

        var serverPluginVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        if (msg.Version != serverPluginVersion)
            _logger.Warning($"Client {client.PlayerName} is using a different version of LunaCompat ({msg.Version}, should be {serverPluginVersion}).");

        _messageHandler.SendCompatMessage(client, new InitializeMessage
        {
            Version = serverPluginVersion
        });
    }

    #endregion
}

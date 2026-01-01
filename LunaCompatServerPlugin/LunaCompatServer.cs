using System.Reflection;

using LmpCommon.Enums;
using LmpCommon.Message.Client;
using LmpCommon.Message.Interface;
using LmpCommon.Xml;

using LunaCompatServerPlugin.ModSettings;

using Server.Client;
using Server.Log;
using Server.Plugin;

namespace LunaCompatServerPlugin;

public class LunaCompatServer : LmpPlugin
{
    #region Fields

    private readonly string _modSettingsPath;
    private readonly Dictionary<string, ServersideModIntegration> _modIntegrations;
    private ModSettingsStructure _settingsStructure;
    private readonly ServerModMessageHandler _modMessageHandler;

    #endregion

    #region Constructors

    public LunaCompatServer()
    {
        _modSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "ModSettingsStructure.xml");
        _modIntegrations = new Dictionary<string, ServersideModIntegration>();
        _modMessageHandler = new ServerModMessageHandler();
        _settingsStructure = new ModSettingsStructure();
    }

    #endregion

    #region Public Methods

    public override void OnServerStart()
    {
        LunaLog.Info("Luna Compat: Loading mod settings storage");

        if (!File.Exists(_modSettingsPath))
            LunaXmlSerializer.WriteToXmlFile(new ModSettingsStructure(), _modSettingsPath);

        _settingsStructure = LunaXmlSerializer.ReadXmlFromPath<ModSettingsStructure>(_modSettingsPath);

        // We could load external fixes here as well - but will that ever be needed?
        var modIntegrations = Assembly.GetExecutingAssembly()?.GetTypes().Where(x => x.IsAssignableTo(typeof(ServersideModIntegration)) && !x.IsAbstract);

        if (modIntegrations == null)
        {
            LunaLog.Error("No Luna Compat integrations found.");
            return;
        }

        foreach (var integration in modIntegrations)
        {
            try
            {
                var instance = (ServersideModIntegration)Activator.CreateInstance(integration)!;
                instance.Setup(this);
                _modIntegrations.Add(integration.Name, instance);
                LunaLog.Info($"Setup mod integration '{integration.Name}' ({instance.ModPrefix})");
            }
            catch (Exception e)
            {
                LunaLog.Error($"Exception loading {integration.Name}: {e}");
            }
        }
    }

    public override void OnClientConnect(ClientStructure client)
    {
        LunaLog.Info("Luna: OnClientConnect");

        _modMessageHandler.ClientConnected(client);
    }

    public override void OnClientAuthenticated(ClientStructure client)
    {
        LunaLog.Info("Luna: OnClientAuthenticated");

        _modMessageHandler.ClientAuthenticated(client);
    }

    public override void OnMessageReceived(ClientStructure client, IClientMessageBase messageData)
    {
        if (messageData.MessageType != ClientMessageType.Mod || messageData is not ModCliMsg clientMessage)
            return;

        _modMessageHandler.CompatMessageReceived(client, clientMessage);
    }

    public override void OnMessageSent(ClientStructure client, IServerMessageBase messageData)
    {
        if (messageData.MessageType == ServerMessageType.Vessel)
        {
        }

        _modMessageHandler.CompatMessageSent(client, messageData);
    }

    #endregion
}

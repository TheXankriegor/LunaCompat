using System.Reflection;

using LmpCommon.Enums;
using LmpCommon.Message.Client;
using LmpCommon.Message.Interface;
using LmpCommon.Xml;

using LunaCompatServerPlugin.ModSettings;
using LunaCompatServerPlugin.Utils;

using Server.Client;
using Server.Plugin;
using Server.System;

namespace LunaCompatServerPlugin;

public class LunaCompatServer : LmpPlugin
{
    #region Fields

    private readonly string _modSettingsPath;
    private readonly Dictionary<string, ServersideModIntegration> _modIntegrations;
    private readonly ServerModMessageHandler _modMessageHandler;
    private ModSettingsStructure _settingsStructure;

    #endregion

    #region Constructors

    public LunaCompatServer()
    {
        _modSettingsPath = Path.Combine(GetLunaCompatBaseDirectory(), "ModSettingsStructure.xml");
        _modIntegrations = new Dictionary<string, ServersideModIntegration>();
        _modMessageHandler = new ServerModMessageHandler();
        _settingsStructure = new ModSettingsStructure();
    }

    #endregion

    #region Public Methods

    public static string GetLunaCompatBaseDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Universe", "LunaCompat");
    }

    public override void OnServerStart()
    {
        Log.Info("Luna Compat: Loading mod settings storage");

        if (!FileHandler.FileExists(_modSettingsPath))
        {
            FileHandler.FolderCreate(GetLunaCompatBaseDirectory());
            LunaXmlSerializer.WriteToXmlFile(new ModSettingsStructure(), _modSettingsPath);
        }

        _settingsStructure = LunaXmlSerializer.ReadXmlFromPath<ModSettingsStructure>(_modSettingsPath);

        // We could load external fixes here as well - but will that ever be needed?
        var modIntegrations = Assembly.GetExecutingAssembly()
                                      .GetTypes()
                                      .Where(x => x.IsAssignableTo(typeof(ServersideModIntegration)) && !x.IsAbstract)
                                      .ToList();

        if (!modIntegrations.Any())
        {
            Log.Error("No Luna Compat integrations found.");
            return;
        }

        foreach (var integration in modIntegrations)
        {
            try
            {
                var instance = (ServersideModIntegration)Activator.CreateInstance(integration)!;
                instance.Setup(_modMessageHandler);
                _modIntegrations.Add(integration.Name, instance);
                Log.Info($"Setup mod integration '{integration.Name}' ({instance.ModPrefix})");
            }
            catch (Exception e)
            {
                Log.Error($"Exception loading {integration.Name}: {e}");
            }
        }
    }

    public override void OnMessageReceived(ClientStructure client, IClientMessageBase messageData)
    {
        if (messageData.MessageType != ClientMessageType.Mod || messageData is not ModCliMsg clientMessage)
            return;

        _modMessageHandler.CompatMessageReceived(client, clientMessage);
    }

    public override void OnServerStop()
    {
        base.OnServerStop();

        foreach (var integration in _modIntegrations)
            integration.Value.Destroy();

        _modIntegrations.Clear();
    }

    #endregion
}

using LmpCommon.Xml;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

using Server.System;

namespace LunaCompatServerPlugin.ModSettings;

internal class ModSettingsProvider : IModSettingsProvider
{
    #region Fields

    private readonly string _settingsPath;
    private readonly ILogger _logger;
    private ModSettingsStructure _settingsStructure;

    #endregion

    #region Constructors

    public ModSettingsProvider(string settingsPath, ILogger logger)
    {
        _settingsPath = settingsPath;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    public bool TryLoadSettings()
    {
        if (!FileHandler.FileExists(_settingsPath))
        {
            FileHandler.FolderCreate(LunaCompatServer.GetLunaCompatBaseDirectory());
            _logger.Warning($"Settings file '{_settingsPath}' does not exist and will be recreated.");
            _settingsStructure = new ModSettingsStructure();
        }
        else
        {
            try
            {
                _settingsStructure = LunaXmlSerializer.ReadXmlFromPath<ModSettingsStructure>(_settingsPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load settings: {ex}");
            }
        }

        return false;
    }

    public void SetValue(string modName, string key, object value)
    {
        var mod = _settingsStructure.GetEntry(modName);

        if (mod == null)
        {
            mod = new ModSettingsEntry(modName);
            _settingsStructure.Mods.Add(mod);
        }

        var currentValue = mod.GetValue(key);

        if (value != currentValue)
        {
            mod.SetValue(key, value);
            LunaXmlSerializer.WriteToXmlFile(_settingsStructure, _settingsPath);
        }
    }

    public object GetValue(string modName, string key, object defaultValue = null)
    {
        var mod = _settingsStructure.GetEntry(modName);

        if (mod == null)
        {
            mod = new ModSettingsEntry(modName);
            _settingsStructure.Mods.Add(mod);
        }

        var value = mod.GetValue(key);

        if (value == null && defaultValue != null)
        {
            value = defaultValue;
            mod.SetValue(key, defaultValue);
            LunaXmlSerializer.WriteToXmlFile(_settingsStructure, _settingsPath);
        }

        return value;
    }

    #endregion
}

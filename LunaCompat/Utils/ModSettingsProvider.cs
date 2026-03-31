using System;
using System.IO;

using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Utils;

namespace LunaCompat.Utils
{
    internal class ModSettingsProvider : IModSettingsProvider
    {
        #region Fields

        private readonly string _settingsPath;
        private readonly ILogger _logger;
        private ConfigNode _settingsStructure;
        private ConfigNode _settingsRoot;

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
            if (!File.Exists(_settingsPath))
            {
                Directory.CreateDirectory(Directory.GetParent(_settingsPath)?.FullName);
                _logger.Warning($"Settings file '{_settingsPath}' does not exist and will be recreated.");
            }
            else
            {
                try
                {
                    _settingsRoot = ConfigNode.Load(_settingsPath);
                    _settingsStructure = _settingsRoot.GetNode(nameof(LunaCompat));
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load settings: {ex}");
                }
            }

            _settingsRoot = new ConfigNode(nameof(LunaCompat));
            _settingsStructure = _settingsRoot.AddNode(nameof(LunaCompat));

            return false;
        }

        public void SetValue(string modName, string key, object value)
        {
            var mod = _settingsStructure.GetNode(modName) ?? _settingsStructure.AddNode(modName);

            mod.SetValue(key, value.ToString(), true);
            _settingsRoot.Save(_settingsPath);
        }

        public object GetValue(string modName, string key, object defaultValue = null)
        {
            var mod = _settingsStructure.GetNode(modName) ?? _settingsStructure.AddNode(modName);

            var value = mod.GetValue(key);

            if (value == null && defaultValue != null)
            {
                value = defaultValue.ToString();
                mod.SetValue(key, value, true);
                _settingsRoot.Save(_settingsPath);
            }

            return value;
        }

        #endregion
    }
}

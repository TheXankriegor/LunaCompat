namespace LunaCompatServerPlugin.ModSettings;

[Serializable]
public class ModSettingsStructure
{
    #region Constructors

    public ModSettingsStructure()
    {
        Mods = new List<ModSettingsEntry>();
    }

    #endregion

    #region Properties

    public List<ModSettingsEntry> Mods { get; set; }

    #endregion

    #region Public Methods

    public ModSettingsEntry GetEntry(string modName)
    {
        return Mods.SingleOrDefault(x => x.ModName == modName);
    }

    #endregion
}

[Serializable]
public class ModSettingsEntry
{
    #region Constructors

    public ModSettingsEntry(string modName)
        : this()
    {
        ModName = modName;
    }

    public ModSettingsEntry()
    {
        Settings = new List<ModSetting>();
    }

    #endregion

    #region Properties

    public string ModName { get; set; }

    public List<ModSetting> Settings { get; set; }

    #endregion

    #region Public Methods

    public void SetValue(string key, object value)
    {
        var match = Settings.FirstOrDefault(x => x.Key == key);

        if (match != null)
            match.Value = value;
        else
        {
            Settings.Add(new ModSetting
            {
                Key = key,
                Value = value
            });
        }
    }

    public object GetValue(string key)
    {
        return Settings.FirstOrDefault(x => x.Key == key)?.Value;
    }

    #endregion
}

[Serializable]
public class ModSetting
{
    #region Properties

    public string Key { get; set; }

    public object Value { get; set; }

    #endregion
}

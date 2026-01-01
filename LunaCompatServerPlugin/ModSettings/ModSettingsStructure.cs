namespace LunaCompatServerPlugin.ModSettings;

public class ModSettingsStructure
{
    #region Constructors

    public ModSettingsStructure()
    {
        Mods = [];
    }

    #endregion

    #region Properties

    public List<ModSettingsEntry> Mods { get; set; }

    #endregion
}

public class ModSettingsEntry
{
    #region Constructors

    public ModSettingsEntry()
    {
        Settings = [];
    }

    #endregion

    #region Properties

    public List<ModSetting> Settings { get; set; }

    #endregion
}

public class ModSetting
{
    #region Properties

    public string? Key { get; set; }

    public object? Value { get; set; }

    #endregion
}

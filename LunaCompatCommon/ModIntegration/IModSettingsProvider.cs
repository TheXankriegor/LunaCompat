namespace LunaCompatCommon.ModIntegration
{
    public interface IModSettingsProvider
    {
        bool TryLoadSettings();

        void SetValue(string modName, string key, object value);

        object GetValue(string modName, string key, object defaultValue = null);
    }
}

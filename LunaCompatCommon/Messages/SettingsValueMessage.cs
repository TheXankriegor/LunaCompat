namespace LunaCompatCommon.Messages
{
    public abstract class SettingsValueMessage : IModMessage
    {
        #region Properties

        public string Key { get; set; }

        public string Value { get; set; }

        #endregion
    }
}

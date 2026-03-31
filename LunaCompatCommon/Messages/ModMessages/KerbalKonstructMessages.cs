namespace LunaCompatCommon.Messages.ModMessages
{
    public static class KerbalKonstructsConstants
    {
        #region Constants

        public const string DisableRemoteRecovery = "disableRemoteRecovery";
        public const string FacilityUseRange = "facilityUseRange";
        public const string EnableRT = "enableRT";
        public const string EnableCommNet = "enableCommNet";
        public const string DisableRemoteBaseOpening = "disableRemoteBaseOpening";

        #endregion
    }

    public class KerbalKonstructsRequestInstancesMessage : IModMessage
    {
    }

    public class KerbalKonstructsDeleteMessage : IModMessage
    {
        #region Properties

        public string Identifier { get; set; }

        #endregion
    }

    public class KerbalKonstructsChangeMessage : IModMessage
    {
        #region Properties

        public string Content { get; set; }

        public string Name { get; set; }

        #endregion
    }

    public class KerbalKonstructsChangeStaticInstanceMessage : KerbalKonstructsChangeMessage
    {
    }

    public class KerbalKonstructsDeleteStaticInstanceMessage : KerbalKonstructsDeleteMessage
    {
        #region Properties

        public string ModelName { get; set; }

        #endregion
    }

    public class KerbalKonstructsChangeGroupCenterMessage : KerbalKonstructsChangeMessage
    {
        #region Properties

        public string Uuid { get; set; }

        #endregion
    }

    public class KerbalKonstructsChangeMapDecalMessage : KerbalKonstructsChangeMessage
    {
    }

    public class KerbalKonstructsDeleteMapDecalMessage : KerbalKonstructsDeleteMessage
    {
    }

    public class KerbalKonstructsDeleteGroupCenterMessage : KerbalKonstructsDeleteMessage
    {
    }

    public class KerbalKonstructsSettingsValueMessage : SettingsValueMessage
    {
    }
}

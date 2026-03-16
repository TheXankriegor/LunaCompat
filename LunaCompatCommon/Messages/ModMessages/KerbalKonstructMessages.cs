namespace LunaCompatCommon.Messages.ModMessages
{
    public class KerbalKonstructsRequestInstancesMessage : IModMessage
    {
    }

    public class KerbalKonstructsDeleteMessage : IModMessage
    {
        #region Properties

        public string Uuid { get; set; }

        #endregion
    }

    public class KerbalKonstructsChangeMessage : IModMessage
    {
        #region Properties

        public string Content { get; set; }

        public string ModelName { get; set; }

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

    public class KerbalKonstructsDeleteGroupCenterMessage : KerbalKonstructsDeleteMessage
    {
    }
}

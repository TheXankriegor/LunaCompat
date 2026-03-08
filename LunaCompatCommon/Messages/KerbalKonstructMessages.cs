namespace LunaCompatCommon.Messages
{
    public class KerbalKonstructRequestInstancesMessage : IModMessage
    {
    }

    public class KerbalKonstructDeleteStaticInstanceMessage : IModMessage
    {
        #region Properties

        public string Uuid { get; set; }

        public string ModelName { get; set; }

        #endregion
    }

    public class KerbalKonstructChangeStaticInstanceMessage : IModMessage
    {
        #region Properties

        public string Content { get; set; }

        public string ModelName { get; set; }

        #endregion
    }
}

namespace LunaCompatCommon.Messages
{
    public class KerbalKonstructRequestInstancesMessage : IModMessage
    {
    }

    public class KerbalKonstructChangeStaticInstanceMessage : IModMessage
    {
        #region Properties

        public string Content { get; set; }

        public string PathName { get; set; }

        #endregion
    }
}

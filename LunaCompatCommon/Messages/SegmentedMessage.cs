namespace LunaCompatCommon.Messages
{
    public class SegmentedMessage : IModMessage
    {
        #region Properties

        public string OriginalType { get; set; }

        public int PartCount { get; set; }

        public int MessageId { get; set; }

        public int PartId { get; set; }

        public byte[] PartData { get; set; }

        #endregion
    }
}

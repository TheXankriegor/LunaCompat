namespace LunaCompatCommon.Messages.ModMessages
{
    public class KerbalColoniesRequestColoniesMessage : IModMessage
    {
    }

    public abstract class KerbalColoniesColonyMessage : IModMessage
    {
        #region Properties

        public string Body { get; set; }

        public string ColonyName { get; set; }

        #endregion
    }

    public class KerbalColoniesDeleteColonyMessage : KerbalColoniesColonyMessage
    {
    }

    public class KerbalColoniesChangeColonyMessage : KerbalColoniesColonyMessage
    {
        #region Properties

        public string Content { get; set; }

        #endregion
    }
}

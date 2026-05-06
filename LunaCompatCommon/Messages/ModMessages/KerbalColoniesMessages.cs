namespace LunaCompatCommon.Messages.ModMessages
{
    public static class KerbalColoniesConstants
    {
        #region Constants

        public const string FacilityCostMultiplier = nameof(FacilityCostMultiplier);
        public const string FacilityTimeMultiplier = nameof(FacilityTimeMultiplier);
        public const string FacilityRangeMultiplier = nameof(FacilityRangeMultiplier);
        public const string EditorRangeMultiplier = nameof(EditorRangeMultiplier);
        public const string VesselCostMultiplier = nameof(VesselCostMultiplier);
        public const string VesselTimeMultiplier = nameof(VesselTimeMultiplier);
        public const string MaxColoniesPerBody = "maxColoniesPerBody";

        #endregion
    }

    public class KerbalColoniesRequestColoniesMessage : IModMessage
    {
    }

    public class KerbalColoniesChangeColonyMessage : IModMessage
    {
        #region Properties

        public string Body { get; set; }

        public string Content { get; set; }

        public string ColonyName { get; set; }

        #endregion
    }

    public class KerbalColoniesSettingsValueMessage : SettingsValueMessage
    {
    }
}

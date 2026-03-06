// ReSharper disable once RedundantUsingDirective
using System;

namespace LunaCompatCommon.Messages
{
    public class ScanSatSyncMessage : IModMessage
    {
        #region Properties

        public string Body { get; set; }

        public string Map { get; set; }

        #endregion
    }

    public class ScanSatScannerChangeMessage : IModMessage
    {
        #region Properties

        public bool Loaded { get; set; }

        public Guid Vessel { get; set; }

        public int Sensor { get; set; }

        public float Fov { get; set; }

        public float MinAlt { get; set; }

        public float MaxAlt { get; set; }

        public float BestAlt { get; set; }

        public bool RequireLight { get; set; }

        #endregion
    }
}

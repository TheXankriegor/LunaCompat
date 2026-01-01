using System;

namespace LunaCompatCommon.Messages
{
    [Serializable]
    public class ScanSatSyncMessage : IModMessage
    {
        public string Body { get; set; }

        public string Map { get; set; }
    }

    [Serializable]
    public class ScanSatScannerChangeMessage : IModMessage
    {
        public bool Loaded { get; set; }

        public Guid Vessel { get; set; }

        public int Sensor { get; set; }

        public float Fov { get; set; }

        public float MinAlt { get; set; }

        public float MaxAlt { get; set; }

        public float BestAlt { get; set; }

        public bool RequireLight { get; set; }
    }
}

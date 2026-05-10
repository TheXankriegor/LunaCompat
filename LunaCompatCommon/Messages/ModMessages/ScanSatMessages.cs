// ReSharper disable once RedundantUsingDirective

using System;

namespace LunaCompatCommon.Messages.ModMessages
{
    public static class ScanSatConstants
    {
        #region Constants

        public const int CoverageSizeX = 360;
        public const int CoverageSizeY = 180;
        public const string AllCelestialBodiesIdentifier = "LunaCompat_Everything";

        #endregion
    }

    public static class ScanSatCommon
    {
        #region Public Methods

        public static short[,] MergeCoverageData(short[,] a, short[,] b)
        {
            if (a.Length != b.Length)
                throw new InvalidOperationException("Cannot merge coverage data of differing dimensions.");

            var result = new short[a.GetLength(0), a.GetLength(1)];

            for (var i = 0; i < a.GetLength(0); i++)
            {
                for (var j = 0; j < a.GetLength(1); j++)
                    result[i, j] = (short)(a[i, j] | b[i, j]);
            }

            return result;
        }

        #endregion
    }

    public class ScanSatRequestDataMessage : IModMessage
    {
    }

    public class ScanSatDataMessage : IModMessage
    {
        #region Properties

        public string Body { get; set; }

        #endregion
    }

    public class ScanSatSyncDataMessage : ScanSatDataMessage
    {
        #region Properties

        public short[,] Map { get; set; }

        #endregion
    }

    public class ScanSatResetDataMessage : ScanSatDataMessage
    {
        #region Properties

        public short Type { get; set; }

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

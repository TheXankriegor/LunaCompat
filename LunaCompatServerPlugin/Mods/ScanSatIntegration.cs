using LmpCommon.Xml;

using LunaCompatCommon.Messages.ModMessages;
using LunaCompatCommon.ModIntegration;
using LunaCompatCommon.Serializer;
using LunaCompatCommon.Utils;

using Server.Client;
using Server.System;

namespace LunaCompatServerPlugin.Mods;

internal class ScanSatIntegration : ServerModIntegration
{
    #region Fields

    private readonly string _basePath;
    private Dictionary<string, short[,]> _coverage;

    #endregion

    #region Constructors

    public ScanSatIntegration(ILogger logger, IModSettingsProvider settingsProvider, ServerMessageHandler messageHandler)
        : base(logger, settingsProvider, messageHandler)
    {
        _basePath = Path.Combine(LunaCompatServer.GetLunaCompatBaseDirectory(), "SCANsat");
    }

    #endregion

    #region Properties

    public override string PackageName => "SCANsat";

    #endregion

    #region Public Methods

    public override void Setup()
    {
        _messageHandler.RegisterModMessageListener<ScanSatScannerChangeMessage>(OnChangeScanSatScannerMessageReceived);
        _messageHandler.RegisterModMessageListener<ScanSatSyncDataMessage>(OnSyncScanSatMessageReceived);
        _messageHandler.RegisterModMessageListener<ScanSatRequestDataMessage>(OnRequestScanSatDataMessageReceived);
        _messageHandler.RegisterModMessageListener<ScanSatResetDataMessage>(OnResetScanSatDataMessageReceived);

        if (!FileHandler.FolderExists(_basePath))
            FileHandler.FolderCreate(_basePath);

        _coverage = new Dictionary<string, short[,]>();

        var coverageEntries = Directory.GetFiles(_basePath, "*.bin", SearchOption.AllDirectories);

        foreach (var coverageEntry in coverageEntries)
        {
            try
            {
                var coverageRaw = FileHandler.ReadFile(coverageEntry);
                var coverage = SerializationUtil.Deserialize<short[,]>(coverageRaw, false);

                _coverage.Add(Path.GetFileNameWithoutExtension(coverageEntry), coverage);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load coverage data '{coverageEntry}'. If the problem persists try deleting the file: {ex}", PackageName);
            }
        }
    }

    public override void Destroy()
    {
        _coverage.Clear();

        _messageHandler.UnregisterModMessageListener<ScanSatScannerChangeMessage>();
        _messageHandler.UnregisterModMessageListener<ScanSatSyncDataMessage>();
        _messageHandler.UnregisterModMessageListener<ScanSatRequestDataMessage>();
        _messageHandler.UnregisterModMessageListener<ScanSatResetDataMessage>();

        base.Destroy();
    }

    #endregion

    #region Non-Public Methods

    private void OnResetScanSatDataMessageReceived(ClientStructure client, ScanSatResetDataMessage msg)
    {
        try
        {
            if (msg.Body == ScanSatConstants.AllCelestialBodiesIdentifier)
            {
                _logger.Info($"Resetting all coverage data (type {msg.Type})", PackageName);

                foreach (var entry in _coverage.ToList())
                    ResetEntry(entry.Key, entry.Value, msg.Type);
            }
            else
            {
                _logger.Info($"Resetting coverage data for {msg.Body} (type {msg.Type})", PackageName);

                if (_coverage.TryGetValue(msg.Body, out var coverage))
                    ResetEntry(msg.Body, coverage, msg.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle coverage reset for {msg.Body} (type {msg.Type}): {ex}", PackageName);
        }

        return;

        void ResetEntry(string body, short[,] coverage, short type)
        {
            var mask = type;
            mask ^= short.MaxValue;

            var m = mask;

            for (var x = 0; x < ScanSatConstants.CoverageSizeX; x++)
            {
                for (var y = 0; y < ScanSatConstants.CoverageSizeY; y++)
                    coverage[x, y] &= m;
            }

            _coverage[msg.Body] = coverage;

            var targetPath = Path.Combine(_basePath, $"{body}.bin");
            var serialized = SerializationUtil.Serialize(coverage, false);
            FileHandler.WriteToFile(targetPath, serialized, serialized.Length);
        }
    }

    private void OnRequestScanSatDataMessageReceived(ClientStructure client, ScanSatRequestDataMessage msg)
    {
        _logger.Info($"Sending SCANsat data to {client.PlayerName} ({_coverage.Count} entries)", PackageName);

        // send all coverage data
        foreach (var coverageEntry in _coverage)
        {
            _messageHandler.SendCompatMessage(client, new ScanSatSyncDataMessage
            {
                Body = coverageEntry.Key,
                Map = coverageEntry.Value
            });
        }

        // send registered scanners
        var vessels = Directory.GetFiles(_basePath, "*.xml", SearchOption.AllDirectories);

        foreach (var vesselEntry in vessels)
        {
            try
            {
                var vessel = LunaXmlSerializer.ReadXmlFromPath<ScanSatVesselState>(vesselEntry);

                foreach (var scanner in vessel.Scanners)
                {
                    _messageHandler.SendCompatMessage(client, new ScanSatScannerChangeMessage
                    {
                        Vessel = vessel.Vessel,
                        Loaded = true,
                        Sensor = scanner.Sensor,
                        RequireLight = scanner.RequireLight,
                        MinAlt = scanner.MinAlt,
                        MaxAlt = scanner.MaxAlt,
                        BestAlt = scanner.BestAlt,
                        Fov = scanner.Fov
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to send active scanner data for vessel '{vesselEntry}'. If the problem persists try deleting the file: {ex}",
                              PackageName);
            }
        }

        _messageHandler.SendCompatMessage(client, new ScanSatRequestDataMessage());
    }

    private void OnSyncScanSatMessageReceived(ClientStructure client, ScanSatSyncDataMessage msg)
    {
        try
        {
            _logger.Debug($"Received coverage update for {msg.Body} from {client.PlayerName}", PackageName);

            if (!_coverage.TryGetValue(msg.Body, out var coverageEntry))
            {
                coverageEntry = msg.Map;
                _coverage.Add(msg.Body, coverageEntry);
            }
            else
            {
                coverageEntry = ScanSatCommon.MergeCoverageData(coverageEntry, msg.Map);
                _coverage[msg.Body] = coverageEntry;
            }

            var targetPath = Path.Combine(_basePath, $"{msg.Body}.bin");
            var serialized = SerializationUtil.Serialize(coverageEntry, false);
            FileHandler.WriteToFile(targetPath, serialized, serialized.Length);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle coverage data for {msg.Body} from {client.PlayerName}: {ex}", PackageName);
        }
    }

    private void OnChangeScanSatScannerMessageReceived(ClientStructure client, ScanSatScannerChangeMessage msg)
    {
        try
        {
            _logger.Debug($"Received scanner update for {msg.Vessel} ({msg.Sensor}) from {client.PlayerName}");

            var targetPath = Path.Combine(_basePath, $"{msg.Vessel}.xml");

            if (File.Exists(targetPath))
            {
                var existing = LunaXmlSerializer.ReadXmlFromPath<ScanSatVesselState>(targetPath);

                var match = -1;

                for (var i = 0; i < existing.Scanners.Length; i++)
                {
                    var scanner = existing.Scanners[i];
                    if (scanner.Sensor == msg.Sensor && scanner.RequireLight == msg.RequireLight && scanner.MinAlt == msg.MinAlt &&
                        scanner.MaxAlt == msg.MaxAlt && scanner.BestAlt == msg.BestAlt && scanner.Fov == msg.Fov)
                        match = i;
                }

                if (msg.Loaded && match == -1)
                {
                    var newEntry = new ScanSatScannerState
                    {
                        Sensor = msg.Sensor,
                        RequireLight = msg.RequireLight,
                        MinAlt = msg.MinAlt,
                        MaxAlt = msg.MaxAlt,
                        BestAlt = msg.BestAlt,
                        Fov = msg.Fov
                    };

                    existing.Scanners = existing.Scanners.Append(newEntry).ToArray();
                }

                if (!msg.Loaded && match != -1)
                {
                    existing.Scanners = existing.Scanners.Except(new[]
                                                {
                                                    existing.Scanners[match]
                                                })
                                                .ToArray();
                }

                if (existing.Scanners.Length == 0)
                {
                    FileHandler.FileDelete(targetPath);
                    return;
                }

                FileHandler.WriteToFile(targetPath, LunaXmlSerializer.SerializeToXml(existing));
            }
            else
            {
                var newEntry = new ScanSatVesselState
                {
                    Vessel = msg.Vessel,
                    Scanners = new[]
                    {
                        new ScanSatScannerState
                        {
                            Sensor = msg.Sensor,
                            RequireLight = msg.RequireLight,
                            MinAlt = msg.MinAlt,
                            MaxAlt = msg.MaxAlt,
                            BestAlt = msg.BestAlt,
                            Fov = msg.Fov
                        }
                    }
                };

                FileHandler.WriteToFile(targetPath, LunaXmlSerializer.SerializeToXml(newEntry));
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle scanner update for {msg.Vessel} from {client.PlayerName}: {ex}", PackageName);
        }
    }

    #endregion
}

public class ScanSatVesselState
{
    #region Properties

    public Guid Vessel { get; set; }

    public ScanSatScannerState[] Scanners { get; set; }

    #endregion
}

public class ScanSatScannerState
{
    #region Properties

    public int Sensor { get; set; }

    public float Fov { get; set; }

    public float MinAlt { get; set; }

    public float MaxAlt { get; set; }

    public float BestAlt { get; set; }

    public bool RequireLight { get; set; }

    #endregion
}

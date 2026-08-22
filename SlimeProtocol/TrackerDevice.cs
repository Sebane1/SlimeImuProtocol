using SlimeImuProtocol.SlimeVR;
using System.Text;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

namespace SlimeImuProtocol.SlimeProtocol {
    public class TrackerDevice {
        public int Id;
        public string Name;
        public string Manufacturer;
        public string HardwareIdentifier;
        public Dictionary<int, Tracker> Trackers = new();

        /// <summary>
        /// Sensor ID for each standalone device's primary sensor (0).
        /// Since SlimeVR Server indexes connections by IP/Port (127.0.0.1/port),
        /// each UDPHandler is registered as a separate device with Sensor 0.
        /// </summary>
        public int SensorId { get; } = 0;

        private UDPHandler? _udpHandler;
        private readonly object _lock = new object();

        public TrackerDevice(int id) {
            Id = id;
            SensorId = 0;
        }

        public BoardType BoardType { get; set; }
        public McuType McuType { get; set; }
        public MagnetometerStatus MagnetometerStatus { get; set; }
        public string FirmwareVersion { get; set; }

        public UDPHandler GetUDPHandler() {
            lock (_lock) {
                if (_udpHandler == null) {
                    string fw = !string.IsNullOrEmpty(FirmwareVersion) ? FirmwareVersion : "1.0.0_ESB";
                    byte[] mac = Encoding.GetEncoding("UTF-8").GetBytes(HardwareIdentifier);
                    _udpHandler = new UDPHandler(
                        fw + "_EsbToLan",
                        mac,
                        BoardType,
                        ImuType.UNKNOWN,
                        McuType,
                        MagnetometerStatus,
                        1,
                        Id
                    );
                }
                return _udpHandler;
            }
        }

        public Tracker? GetTracker(int id) {
            if (Trackers.TryGetValue(id, out var tracker)) {
                return tracker;
            }
            return null;
        }
    }
}
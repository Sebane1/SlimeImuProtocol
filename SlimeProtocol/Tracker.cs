using SlimeImuProtocol.SlimeVR;
using SlimeImuProtocol.Utility;
using System.Numerics;
using System.Text;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

namespace SlimeImuProtocol.SlimeProtocol {
    public class Tracker : IDisposable {
        public int TrackerNum { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool HasRotation { get; set; }
        public bool HasAcceleration { get; set; }
        public bool UserEditable { get; set; }
        public ImuType ImuType { get; set; }
        public bool AllowFiltering { get; set; }
        public bool NeedsReset { get; set; }
        public bool NeedsMounting { get; set; }
        public bool UsesTimeout { get; set; }
        public MagnetometerStatus MagStatus { get; set; }

        private FunctionSequenceManager _functionSequenceManager;

        public float BatteryLevel {
            get => _batteryLevel;
            set
            {
                _batteryLevel = value;
                if (_ready)
                {
                    _udpHandler.SetSensorBattery(_batteryLevel, BatteryVoltage);
                }
            }
        }
        public float BatteryVoltage { get; set; }
        public float? Temperature { get; set; }
        
        private int _signalStrength;
        public int SignalStrength {
            get => _signalStrength;
            set
            {
                _signalStrength = value;
                if (_ready)
                {
                    UDPHandler?.SetSensorSignalStrength(_signalStrength, 0);
                }
            }
        }

        public TrackerStatus Status = TrackerStatus.Disconnected;
        private TrackerDevice? _device;
        private UDPHandler? _udpHandler;
        private bool _ready;
        private float _batteryLevel;
        private Quaternion _currentRotation;
        private Vector3 _currentAcceleration;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public UDPHandler? UDPHandler => _udpHandler ?? _device?.GetUDPHandler();

        // These can be set after construction when device data is parsed
        public string FirmwareVersion { get; set; }
        public string HardwareIdentifier { get; set; }
        public BoardType BoardType { get; set; }
        public McuType McuType { get; set; }
        public Quaternion CurrentRotation { get => _currentRotation; set => _currentRotation = value; }
        public Vector3 CurrentAcceleration { get => _currentAcceleration; set => _currentAcceleration = value; }

        public Tracker(TrackerDevice device, int trackerNum, string name, string displayName, bool hasRotation, bool hasAcceleration,
            bool userEditable, ImuType imuType, bool allowFiltering, bool needsReset,
            bool needsMounting, bool usesTimeout, MagnetometerStatus magStatus) {
            _device = device;
            TrackerNum = trackerNum;
            Name = name;
            DisplayName = displayName;
            HasRotation = hasRotation;
            HasAcceleration = hasAcceleration;
            UserEditable = userEditable;
            ImuType = imuType;
            AllowFiltering = allowFiltering;
            NeedsReset = needsReset;
            NeedsMounting = needsMounting;
            UsesTimeout = usesTimeout;
            MagStatus = magStatus;
            _ready = true;
            _ = UDPHandler?.RegisterSensor((byte)TrackerNum, ImuType);
        }

        public void TryInitialize() {
            if (FirmwareVersion != null && HardwareIdentifier != null && _device != null) {
                _udpHandler = _device.GetUDPHandler();
                _ready = true;
                _ = _udpHandler.RegisterSensor((byte)TrackerNum, ImuType);
            }
        }

        public static byte[] GenerateUniqueMac(string? hwId, int trackerIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(hwId)) hwId = "DefaultESB";

            string clean = hwId.Replace(":", "").Replace("-", "");
            if (clean.Length == 12 && System.Text.RegularExpressions.Regex.IsMatch(clean, "^[0-9a-fA-F]{12}$"))
            {
                try
                {
                    byte[] mac = new byte[6];
                    for (int i = 0; i < 6; i++)
                    {
                        mac[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                    }
                    mac[0] = (byte)((mac[0] & 0xFE) | 0x02); // Set locally administered bit
                    mac[5] = (byte)((mac[5] + trackerIndex) & 0xFF);
                    return mac;
                }
                catch { }
            }

            // Generate deterministic 6-byte MAC from text identifier using SHA256
            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{hwId}:{trackerIndex}"));
                byte[] mac = new byte[6];
                mac[0] = (byte)((bytes[0] & 0xFE) | 0x02); // Unicast, locally administered MAC
                mac[1] = bytes[1];
                mac[2] = bytes[2];
                mac[3] = bytes[3];
                mac[4] = bytes[4];
                mac[5] = bytes[5];
                return mac;
            }
        }

        public void SetRotation(Quaternion q) {
            if (_ready)
            {
                _currentRotation = q;
                _udpHandler?.SetSensorRotation(q, (byte)TrackerNum);
            }
        }

        public void SetAcceleration(Vector3 a) {
            if (_ready)
            {
                _currentAcceleration = a;
                _udpHandler?.SetSensorAcceleration(a, (byte)TrackerNum);
            }
        }

        public void SetMagVector(Vector3 m) {
            if (_ready)
            {
                _udpHandler?.SetSensorMagnetometer(m, (byte)TrackerNum);
            }
        }

        public async Task DataTick() {
            // What do?
        }

        public void Dispose() {
             _ready = false;
             _cts.Cancel();
        }
    }

    public enum TrackerStatus {
        OK,
        Disconnected
    }
}

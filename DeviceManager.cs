namespace SlimeImuProtocol {
    public class DeviceManager {
        private static DeviceManager _instance = new DeviceManager();
        Dictionary<string, TrackerDevice> _devices = new Dictionary<string, TrackerDevice>();
        private int _nextLocalTrackerId;

        public static DeviceManager Instance {
            get {
                if (_instance == null) {
                    _instance = new DeviceManager();
                }
                return _instance;
            }
        }
        public DeviceManager() {
            _instance = this;
        }

        public void AddDevice(TrackerDevice newDevice) {
            _devices[newDevice.HardwareIdentifier] = newDevice;
        }
        public int GetNextLocalTrackerId() {
            return ++_nextLocalTrackerId;
        }
    }
}

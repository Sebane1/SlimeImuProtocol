using SlimeImuProtocol;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

public class TrackerDevice {
    public int Id;
    public string Name;
    public string Manufacturer;
    public string HardwareIdentifier;
    public Dictionary<int, Tracker> Trackers = new();
    public TrackerDevice(int id) { Id = id; }

    public BoardType BoardType { get; set; }
    public McuType McuType { get; set; }
    public MagnetometerStatus MagnetometerStatus { get; set; }
    public string FirmwareVersion { get; set; }

    public Tracker? GetTracker(int id) {
        if (Trackers.TryGetValue(id, out var tracker)) {
            return tracker;
        }
        return null;
    }
}
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

namespace SlimeImuProtocol.SlimeVR {
    public class UDPHandler {
        private static string _endpoint = "255.255.255.255";
        private static bool _handshakeOngoing = false;
        public static event EventHandler OnForceHandshake;
        private byte[] _hardwareAddress;
        private int _supportedSensorCount;
        private PacketBuilder packetBuilder;
        private int slimevrPort = 6969;
        UdpClient udpClient;
        int handshakeCount = 1000;
        bool _active = true;

        public bool Active { get => _active; set => _active = value; }
        public static string Endpoint { get => _endpoint; set => _endpoint = value; }
        public static bool HandshakeOngoing { get => _handshakeOngoing; }

        public UDPHandler(string firmware, byte[] hardwareAddress, BoardType boardType, ImuType imuType, McuType mcuType, int supportedSensorCount) {
            _hardwareAddress = hardwareAddress;
            _supportedSensorCount = supportedSensorCount;
            packetBuilder = new PacketBuilder(firmware);
            ConfigureUdp();
            Task.Run(() => {
                DoHandshake(hardwareAddress, boardType, imuType, mcuType, supportedSensorCount);
            });

            OnForceHandshake += delegate {
                DoHandshake(hardwareAddress, boardType, imuType, mcuType, supportedSensorCount);
            };
        }
        public static void ForceUDPClientsToDoHandshake() {
            OnForceHandshake?.Invoke(new object(), EventArgs.Empty);
        }
        public void DoHandshake(byte[] hardwareAddress, BoardType boardType, ImuType imuType, McuType mcuType, int supportedSensorCount) {
            while (_handshakeOngoing) {
                Thread.Sleep(5000);
            }
            while (true) {
                if (_active) {
                    Initialize(boardType, imuType, mcuType, hardwareAddress);
                    break;
                } else {
                    Thread.Sleep(5000);
                }
            }
        }

        public void ConfigureUdp() {
            if (udpClient != null) {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            packetBuilder.PacketId = 0;
            udpClient = new UdpClient();
            udpClient.Connect(_endpoint, 6969);
        }

        public void Initialize(BoardType boardType, ImuType imuType, McuType mcuType, byte[] macAddress) {
            bool listeningForHandShake = false;
            _handshakeOngoing = true;
            if (!listeningForHandShake) {
                Task.Run(() => {
                    bool listeningForHandShake = true;
                    ListenForHandshake(boardType, imuType, mcuType, macAddress);
                    listeningForHandShake = false;
                });
            }
            while (_handshakeOngoing) {
                Handshake(boardType, imuType, mcuType, _hardwareAddress);
                Thread.Sleep(1000);
            }
            for (int i = 0; i < _supportedSensorCount; i++) {
                AddImu(imuType, TrackerPosition.NONE, TrackerDataType.ROTATION, (byte)i);
            }
        }
        public void Heartbeat() {
            Task.Run(async () => {
                while (true) {
                    if (_active) {
                        await udpClient.SendAsync(packetBuilder.HeartBeat);
                    }
                    await Task.Delay(900); // At least 1 time per second (<1000ms)
                }
            });
        }

        public async void AddImu(ImuType imuType, TrackerPosition trackerPosition, TrackerDataType trackerDataType, byte trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildSensorInfoPacket(imuType, trackerPosition, trackerDataType, trackerId));
        }

        public async void Handshake(BoardType boardType, ImuType imuType, McuType mcuType, byte[] macAddress) {
            await udpClient.SendAsync(packetBuilder.BuildHandshakePacket(boardType, imuType, mcuType, macAddress));
            await Task.Delay(500);
            Heartbeat();
        }

        public async Task<bool> SetSensorRotation(Quaternion rotation, byte trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildRotationPacket(rotation, trackerId));
            return true;
        }
        public async Task<bool> SetSensorAcceleration(Vector3 acceleration, byte trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildAccelerationPacket(acceleration, trackerId));
            return true;
        }
        public async Task<bool> SetSensorGyro(Vector3 gyro, byte trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildGyroPacket(gyro, trackerId));
            return true;
        }
        public async Task<bool> SetSensorFlexData(float flexResistance, byte trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildFlexDataPacket(flexResistance, trackerId));
            return true;
        }
        public async Task<bool> SendButton(UserActionType userActionType) {
            await udpClient.SendAsync(packetBuilder.BuildButtonPushedPacket(userActionType));
            return true;
        }

        public async Task<bool> SendPacket(byte[] packet) {
            await udpClient.SendAsync(packet);
            return true;
        }

        public async void ListenForHandshake(BoardType boardType, ImuType imuType, McuType mcuType, byte[] macAddress) {
            try {
                var data = await udpClient.ReceiveAsync();
                string value = Encoding.UTF8.GetString(data.Buffer);
                if (value.Contains("Hey OVR =D 5")) {
                    udpClient.Connect(data.RemoteEndPoint.Address.ToString(), 6969);
                    _handshakeOngoing = false;
                    Handshake(boardType, imuType, mcuType, _hardwareAddress);
                }
            } catch {

            }
        }

        public async Task<bool> SetSensorBattery(float battery, float voltage) {
            await udpClient.SendAsync(packetBuilder.BuildBatteryLevelPacket(battery, voltage));
            return true;
        }

        public async Task<bool> SetSensorMagnetometer(Vector3 magnetometer, int trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildMagnetometerPacket(magnetometer, trackerId));
            return true;
        }
    }
}

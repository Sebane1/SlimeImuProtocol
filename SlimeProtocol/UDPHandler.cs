using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using static Everything_To_IMU_SlimeVR.SlimeVR.FirmwareConstants;

namespace Everything_To_IMU_SlimeVR.SlimeVR {
    public class UDPHandler {
        private byte[] _hardwareAddress;
        private int _supportedSensorCount;
        private PacketBuilder packetBuilder;
        private int slimevrPort = 6969;
        UdpClient udpClient;
        int handshakeCount = 1000;
        bool _active = true;

        public bool Active { get => _active; set => _active = value; }

        public UDPHandler(string firmware, byte[] hardwareAddress, BoardType boardType, ImuType imuType, McuType mcuType, int supportedSensorCount) {
            _hardwareAddress = hardwareAddress;
            _supportedSensorCount = supportedSensorCount;
            packetBuilder = new PacketBuilder(firmware);
            ResetUdp();
            Task.Run(() => {
                while (true) {
                    if (_active) {
                        Initialize(boardType, imuType, mcuType, hardwareAddress);
                        Thread.Sleep(1000);
                        handshakeCount += 1;
                        if (handshakeCount > 10) {
                            break;
                        }
                    } else {
                        Thread.Sleep(5000);
                    }
                }
                //while (true) {
                //    Thread.Sleep(1800000);
                //    ResetUdp();
                //    Initialize();
                //}
            });
        }
        public void ResetUdp() {
            if (udpClient != null) {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            packetBuilder.PacketId = 0;
            udpClient = new UdpClient();
            udpClient.Connect("localhost", 6969);
        }
        public void Initialize(BoardType boardType, ImuType imuType, McuType mcuType, byte[] macAddress) {
            Handshake(boardType, imuType, mcuType, _hardwareAddress);
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

        public async void ListenForHandshake() {
            try {
                var data = await udpClient.ReceiveAsync();
                string value = Encoding.UTF8.GetString(data.Buffer);
                if (value.Contains("Hey OVR =D 5")) {
                    udpClient.Connect(data.RemoteEndPoint.Address.ToString(), 6969);
                }
            } catch {

            }
        }

        public async Task<bool> SetSensorBattery(float battery) {
            await udpClient.SendAsync(packetBuilder.BuildBatteryLevelPacket(battery));
            return true;
        }

        public async Task<bool> SetSensorMagnetometer(Vector3 magnetometer, int trackerId) {
            await udpClient.SendAsync(packetBuilder.BuildMagnetometerPacket(magnetometer, trackerId));
            return true;
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

namespace SlimeImuProtocol.SlimeVR
{
    public class UDPHandler : IDisposable
    {
        private static string _endpoint = "255.255.255.255";
        private static bool _handshakeOngoing = false;
        public static event EventHandler OnForceHandshake;
        public static event EventHandler OnForceDestroy;
        private Stopwatch _timeSinceLastQuaternionDataPacket = new Stopwatch();
        private Stopwatch _timeSinceLastAccelerationDataPacket = new Stopwatch();
        private string _id;
        private byte[] _hardwareAddress;
        private int _supportedSensorCount;
        private PacketBuilder packetBuilder;
        private int slimevrPort = 6969;
        UdpClient udpClient;
        int handshakeCount = 1000;
        bool _active = true;
        private bool disposed;
        private EventHandler forceHandShakeDelegate;
        private Vector3 _lastAccelerationPacket;
        private Quaternion _lastQuaternion;

        public bool Active { get => _active; set => _active = value; }
        public static string Endpoint { get => _endpoint; set => _endpoint = value; }
        public static bool HandshakeOngoing { get => _handshakeOngoing; }

        public UDPHandler(string firmware, byte[] hardwareAddress, BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, int supportedSensorCount)
        {
            _id = Guid.NewGuid().ToString();
            _hardwareAddress = hardwareAddress;
            _supportedSensorCount = supportedSensorCount;
            packetBuilder = new PacketBuilder(firmware);
            ConfigureUdp();
            Task.Run(() =>
            {
                DoHandshake(hardwareAddress, boardType, imuType, mcuType, magnetometerStatus, supportedSensorCount);
            });

            forceHandShakeDelegate = delegate (object o, EventArgs e)
            {
                DoHandshake(hardwareAddress, boardType, imuType, mcuType, magnetometerStatus, supportedSensorCount);
            };
            OnForceHandshake += forceHandShakeDelegate;
            OnForceDestroy += UDPHandler_OnForceDestroy;
        }

        private void UDPHandler_OnForceDestroy(object? sender, EventArgs e)
        {
            OnForceHandshake -= forceHandShakeDelegate;
            OnForceDestroy -= UDPHandler_OnForceDestroy;
            this?.Dispose();
        }

        public static void ForceUDPClientsToDoHandshake()
        {
            OnForceHandshake?.Invoke(new object(), EventArgs.Empty);
        }
        public static void ForceDestroy()
        {
            OnForceHandshake -= OnForceHandshake;
            OnForceDestroy?.Invoke(new object(), EventArgs.Empty);
        }
        public void DoHandshake(byte[] hardwareAddress, BoardType boardType, ImuType imuType, McuType mcuType,
            MagnetometerStatus magnetometerStatus, int supportedSensorCount)
        {
            if (!disposed)
            {
                while (_handshakeOngoing)
                {
                    Thread.Sleep(5000);
                }
                while (true)
                {
                    if (_active)
                    {
                        Initialize(boardType, imuType, mcuType, magnetometerStatus, hardwareAddress);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
        }

        public void ConfigureUdp()
        {
            if (udpClient != null)
            {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            udpClient = new UdpClient();
            udpClient.Connect(_endpoint, 6969);
        }

        public void Initialize(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, byte[] macAddress)
        {
            bool listeningForHandShake = false;
            _handshakeOngoing = true;
            if (!listeningForHandShake)
            {
                Task.Run(() =>
                {
                    bool listeningForHandShake = true;
                    ListenForHandshake(boardType, imuType, mcuType, magnetometerStatus, macAddress);
                    listeningForHandShake = false;
                });
            }
            while (_handshakeOngoing)
            {
                Handshake(boardType, imuType, mcuType, magnetometerStatus, _hardwareAddress);
                Thread.Sleep(1000);
            }
            for (int i = 0; i < _supportedSensorCount; i++)
            {
                AddImu(imuType, TrackerPosition.NONE, TrackerDataType.ROTATION, (byte)i);
            }
        }
        public void Heartbeat()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_active)
                    {
                        if (udpClient != null)
                        {
                            await udpClient.SendAsync(packetBuilder.HeartBeat);
                        }
                    }
                    await Task.Delay(900); // At least 1 time per second (<1000ms)
                }
            });
        }

        public async void AddImu(ImuType imuType, TrackerPosition trackerPosition, TrackerDataType trackerDataType, byte trackerId)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildSensorInfoPacket(imuType, trackerPosition, trackerDataType, trackerId));
            }
        }

        public async void Handshake(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, byte[] macAddress)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildHandshakePacket(boardType, imuType, mcuType, magnetometerStatus, macAddress));
            }
            await Task.Delay(500);
            Heartbeat();
        }

        public async Task<bool> SetSensorRotation(Quaternion rotation, byte trackerId)
        {
            if (udpClient != null)
            {

                await udpClient.SendAsync(packetBuilder.BuildRotationPacket(rotation, trackerId));
                _lastQuaternion = rotation;
            }
            return true;
        }

        public static bool QuatEqualsWithEpsilon(Quaternion a, Quaternion b)
        {
            const float epsilon = 0.0001f;
            return MathF.Abs(a.X - b.X) < epsilon
                && MathF.Abs(a.Y - b.Y) < epsilon
                && MathF.Abs(a.Z - b.Z) < epsilon
                && MathF.Abs(a.W - b.W) < epsilon;
        }

        public async Task<bool> SetSensorAcceleration(Vector3 acceleration, byte trackerId)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildAccelerationPacket(acceleration, trackerId));
                _timeSinceLastAccelerationDataPacket.Restart();
                _lastAccelerationPacket = acceleration;
            }
            return true;
        }
        public async Task<bool> SetSensorGyro(Vector3 gyro, byte trackerId)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildGyroPacket(gyro, trackerId));
            }
            return true;
        }
        public async Task<bool> SetSensorFlexData(float flexResistance, byte trackerId)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildFlexDataPacket(flexResistance, trackerId));
            }
            return true;
        }
        public async Task<bool> SendButton(UserActionType userActionType)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildButtonPushedPacket(userActionType));
            }
            return true;
        }

        public async Task<bool> SendPacket(byte[] packet)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packet);
            }
            return true;
        }

        public async void ListenForHandshake(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, byte[] macAddress)
        {
            try
            {
                var data = await udpClient.ReceiveAsync();
                string value = Encoding.UTF8.GetString(data.Buffer);
                if (value.Contains("Hey OVR =D 5"))
                {
                    udpClient.Connect(data.RemoteEndPoint.Address.ToString(), 6969);
                    _handshakeOngoing = false;
                    Handshake(boardType, imuType, mcuType, magnetometerStatus, _hardwareAddress);
                }
            }
            catch
            {
                _handshakeOngoing = false;
            }
        }
        public async void ListenForHeartbeat(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, byte[] macAddress)
        {
            try
            {
                var data = await udpClient.ReceiveAsync();
                string value = Encoding.UTF8.GetString(data.Buffer);
            }
            catch
            {
                Handshake(boardType, imuType, mcuType, magnetometerStatus, macAddress);
            }
        }

        public async Task<bool> SetSensorBattery(float battery, float voltage)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildBatteryLevelPacket(battery, voltage));
            }
            return true;
        }

        public async Task<bool> SetSensorMagnetometer(Vector3 magnetometer, byte trackerId)
        {
            if (udpClient != null)
            {
                await udpClient.SendAsync(packetBuilder.BuildMagnetometerPacket(magnetometer, trackerId));
            }
            return true;
        }

        public void Dispose()
        {
            try
            {
                if (udpClient != null)
                {
                    if (!disposed)
                    {
                        disposed = true;
                        udpClient?.Close();
                        udpClient = null;
                        _handshakeOngoing = false;
                    }
                }
            }
            catch
            {

            }
        }
    }
}

using SlimeImuProtocol.SlimeVR;
using System;
using System.Buffers.Binary;
using System.Numerics;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;

namespace SlimeImuProtocol.SlimeVR
{
    public class PacketBuilder
    {
        private string _identifierString = "Bootleg Tracker";
        private int _protocolVersion = 19;
        private long _packetId;

        // Pre-allocated buffers for reuse
        private readonly byte[] _rotationBuffer = new byte[4 + 8 + 1 + 1 + 16 + 1];
        private readonly byte[] _accelerationBuffer = new byte[4 + 8 + 12 + 1];
        private readonly byte[] _analogueStickBuffer = new byte[4 + 8 + 8 + 1];
        private readonly byte[] _gyroBuffer = new byte[4 + 8 + 1 + 1 + 12 + 1];
        private readonly byte[] _magnetometerBuffer = new byte[4 + 8 + 1 + 1 + 12 + 1];
        private readonly byte[] _flexDataBuffer = new byte[4 + 8 + 1 + 4];
        private readonly byte[] _buttonBuffer = new byte[4 + 8 + 1];
        private readonly byte[] _batteryBuffer = new byte[4 + 8 + 4 + 4];
        private readonly byte[] _hapticBuffer = new byte[4 + 3 + 4 + 4 + 1];

        public byte[] HeartBeat;

        public PacketBuilder(string fwString)
        {
            _identifierString = fwString;
            HeartBeat = CreateHeartBeat();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long NextPacketId()
        {
            if (_packetId >= long.MaxValue) _packetId = 0;
            return _packetId++;
        }

        private byte[] CreateHeartBeat()
        {
            var w = new BigEndianWriter(HeartBeat = new byte[4 + 8 + 1]);
            w.WriteInt32((int)UDPPackets.HEARTBEAT); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(0); // Tracker Id
            return HeartBeat;
        }

        public ReadOnlyMemory<byte> BuildRotationPacket(Quaternion rotation, byte trackerId)
        {
            var w = new BigEndianWriter(_rotationBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.ROTATION_DATA); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(trackerId); // Tracker id
            w.WriteByte(1); // Data type
            w.WriteSingle(rotation.X); // Quaternion X
            w.WriteSingle(rotation.Y); // Quaternion Y
            w.WriteSingle(rotation.Z); // Quaternion Z
            w.WriteSingle(rotation.W); // Quaternion W
            w.WriteByte(0); // Calibration Info
            return _rotationBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildAccelerationPacket(Vector3 acceleration, byte trackerId)
        {
            var w = new BigEndianWriter(_accelerationBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.ACCELERATION); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteSingle(acceleration.X); // Euler X
            w.WriteSingle(acceleration.Y); // Euler Y
            w.WriteSingle(acceleration.Z); // Euler Z
            w.WriteByte(trackerId); // Tracker id
            return _accelerationBuffer.AsMemory(0, w.Position);
        }
        public ReadOnlyMemory<byte> BuildThumbstickPacket(Vector2 _analogueStick, byte trackerId)
        {
            var w = new BigEndianWriter(_analogueStickBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.THUMBSTICK); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteSingle(_analogueStick.X); // Analogue X
            w.WriteSingle(_analogueStick.Y); // Analogue Y
            w.WriteByte(trackerId); // Tracker id
            return _accelerationBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildGyroPacket(Vector3 gyro, byte trackerId)
        {
            var w = new BigEndianWriter(_gyroBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.GYRO); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(trackerId); // Tracker id
            w.WriteByte(1); // Data type 
            w.WriteSingle(gyro.X); // Euler X
            w.WriteSingle(gyro.Y); // Euler Y
            w.WriteSingle(gyro.Z); // Euler Z
            w.WriteByte(0); // Calibration Info
            return _gyroBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildMagnetometerPacket(Vector3 m, byte trackerId)
        {
            var w = new BigEndianWriter(_magnetometerBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.MAG); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(trackerId); // Tracker id
            w.WriteByte(1); // Data type 
            w.WriteSingle(m.X); // Euler X
            w.WriteSingle(m.Y); // Euler Y
            w.WriteSingle(m.Z); // Euler Z
            w.WriteByte(0); // Calibration Info
            return _magnetometerBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildFlexDataPacket(float flex, byte trackerId)
        {
            var w = new BigEndianWriter(_flexDataBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.FLEX_DATA_PACKET); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(trackerId); // Tracker id
            w.WriteSingle(flex); // Flex data
            return _flexDataBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildButtonPushedPacket(UserActionType action)
        {
            var w = new BigEndianWriter(_buttonBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.BUTTON); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte((byte)action); // Action type
            return _buttonBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildBatteryLevelPacket(float battery, float voltage)
        {
            var w = new BigEndianWriter(_batteryBuffer);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.BATTERY_LEVEL); // Header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteSingle(voltage); // Battery data
            w.WriteSingle(battery / 100f); // Battery data
            return _batteryBuffer.AsMemory(0, w.Position);
        }

        public ReadOnlyMemory<byte> BuildHapticPacket(float intensity, int duration)
        {
            var w = new BigEndianWriter(_hapticBuffer);
            w.SetPosition(0);
            w.WriteByte(0); // Padding
            w.WriteByte(0); // Padding
            w.WriteByte(0); // Padding
            w.WriteByte((byte)UDPPackets.HAPTICS); // Header
            w.WriteSingle(intensity); // Vibration Intensity
            w.WriteInt32(duration); // Haptic Duration
            w.WriteByte(1); // active
            return _hapticBuffer.AsMemory(0, w.Position);
        }

        public byte[] BuildHandshakePacket(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magStatus, byte[] mac)
        {
            var idBytes = System.Text.Encoding.UTF8.GetBytes(_identifierString);
            int totalSize = 4 + 8 + 4 * 7 + 1 + idBytes.Length + mac.Length;
            var span = new byte[totalSize];

            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.HANDSHAKE); // Header 
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteInt32((int)boardType); // Board type
            w.WriteInt32((int)imuType); // IMU type
            w.WriteInt32((int)mcuType); // MCU Type
            w.WriteInt32((int)magStatus); // IMU Info
            w.WriteInt32((int)magStatus); // IMU Info
            w.WriteInt32((int)magStatus); // IMU Info
            w.WriteInt32(_protocolVersion); // Protocol Version

            // Identifier string
            w.WriteByte((byte)idBytes.Length);  // Identifier Length
            idBytes.CopyTo(span.AsSpan(w.Position)); // Identifier String
            w.Skip(idBytes.Length);

            // MAC address
            mac.CopyTo(span.AsSpan(w.Position)); // MAC Address
            w.Skip(mac.Length);

            return span;
        }

        public byte[] BuildSensorInfoPacket(ImuType imuType, TrackerPosition pos, TrackerDataType dataType, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 1 + 1 + 2 + 1 + 1];
            var w = new BigEndianWriter(span);
            w.SetPosition(0);
            w.WriteInt32((int)UDPPackets.SENSOR_INFO); // Packet header
            w.WriteInt64(NextPacketId()); // Packet counter
            w.WriteByte(trackerId); // Tracker Id
            w.WriteByte(0); // Sensor status
            w.WriteByte((byte)imuType);  // IMU type
            w.WriteInt16(1); // Calibration state
            w.WriteByte((byte)pos);  // Tracker Position
            w.WriteByte((byte)dataType);  // Tracker Data Type
            return span;
        }
    }
}

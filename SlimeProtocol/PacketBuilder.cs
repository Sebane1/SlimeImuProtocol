using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static SlimeImuProtocol.SlimeVR.FirmwareConstants;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SlimeImuProtocol.Utility.BigEndianExtensions;
using SlimeImuProtocol.Utility;
namespace SlimeImuProtocol.SlimeVR
{
    public class PacketBuilder
    {
        private string _identifierString = "Dualsense-IMU-Tracker";
        private int _protocolVersion = 19;
        private long _packetId = 0;

        private byte[] _heartBeat = new byte[0];
        private MemoryStream _heartbeatStream;
        private MemoryStream _handshakeStream;
        private MemoryStream _sensorInfoStream;
        private MemoryStream _rotationPacketStream;
        private MemoryStream _accelerationPacketStream;
        private MemoryStream _gyroPacketStream;
        private MemoryStream _flexdataPacketStream;
        private MemoryStream _buttonPushPacketStream;
        private MemoryStream _batteryLevelPacketStream;
        private MemoryStream _magnetometerPacketStream;
        private MemoryStream _rotationAndAccelerationPacketStream;
        private BigEndianBinaryWriter _handshakeWriter;
        private BigEndianBinaryWriter _sensorInfoWriter;
        private BigEndianBinaryWriter _sensorRotationPacketWriter;
        private BigEndianBinaryWriter _rotationPacketWriter;
        private BigEndianBinaryWriter _accelerationPacketWriter;
        private BigEndianBinaryWriter _rotationAndAccelerationPacketWriter;
        private BigEndianBinaryWriter _gyroPacketWriter;
        private BigEndianBinaryWriter _flexDataPacketWriter;
        private BigEndianBinaryWriter _buttonPushPacketWriter;
        private BigEndianBinaryWriter _batteryLevelPacketWriter;
        private BigEndianBinaryWriter _magnetometerPacketWriter;
        private BigEndianBinaryWriter _hapticPacketWriter;

        public byte[] HeartBeat { get => _heartBeat; set => _heartBeat = value; }
        public long PacketId { get => _packetId; set => _packetId = value; }

        public PacketBuilder(string fwString)
        {
            _identifierString = fwString;
            _heartbeatStream = new MemoryStream();
            _handshakeStream = new MemoryStream();
            _sensorInfoStream = new MemoryStream();
            _rotationPacketStream = new MemoryStream();
            _accelerationPacketStream = new MemoryStream();
            _gyroPacketStream = new MemoryStream();
            _flexdataPacketStream = new MemoryStream();
            _buttonPushPacketStream = new MemoryStream();
            _batteryLevelPacketStream = new MemoryStream();
            _magnetometerPacketStream = new MemoryStream();
            _rotationAndAccelerationPacketStream = new MemoryStream();

            _handshakeWriter = new BigEndianBinaryWriter(_handshakeStream);
            _sensorInfoWriter = new BigEndianBinaryWriter(_sensorInfoStream);
            _rotationPacketWriter = new BigEndianBinaryWriter(_rotationPacketStream);
            _accelerationPacketWriter = new BigEndianBinaryWriter(_accelerationPacketStream);
            _gyroPacketWriter = new BigEndianBinaryWriter(_gyroPacketStream);
            _flexDataPacketWriter = new BigEndianBinaryWriter(_flexdataPacketStream);
            _buttonPushPacketWriter = new BigEndianBinaryWriter(_buttonPushPacketStream);
            _batteryLevelPacketWriter = new BigEndianBinaryWriter(_batteryLevelPacketStream);
            _magnetometerPacketWriter = new BigEndianBinaryWriter(_magnetometerPacketStream);
            _rotationAndAccelerationPacketWriter = new BigEndianBinaryWriter(_rotationAndAccelerationPacketStream);

            _heartBeat = CreateHeartBeat();
        }

        private byte[] CreateHeartBeat()
        {
            BigEndianBinaryWriter writer = new BigEndianBinaryWriter(_heartbeatStream);
            _heartbeatStream.Position = 0;
            writer.Write(UDPPackets.HEARTBEAT); // header
            writer.Write(_packetId++); // packet counter
            writer.Write((byte)0); // Tracker Id
            _heartbeatStream.Position = 0;
            var data = _heartbeatStream.ToArray();
            _heartbeatStream?.Dispose();
            return data;
        }

        public byte[] BuildHandshakePacket(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magnetometerStatus, byte[] macAddress)
        {
            BigEndianBinaryWriter writer = _handshakeWriter;
            _handshakeStream.Position = 0;
            writer.Write(UDPPackets.HANDSHAKE); // header
            writer.Write((long)_packetId++); // packet counter
            writer.Write((int)boardType); // Board type
            writer.Write((int)imuType); //IMU type
            writer.Write((int)mcuType); // MCU Type

            writer.Write((int)magnetometerStatus); // IMU Info
            writer.Write((int)magnetometerStatus); // IMU Info
            writer.Write((int)magnetometerStatus); // IMU Info

            writer.Write(_protocolVersion); // Protocol Version
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(_identifierString);
            writer.Write((byte)utf8Bytes.Length); // identifier string
            writer.Write(utf8Bytes); // identifier string
            writer.Write(macAddress); // MAC Address
            _handshakeStream.Position = 0;
            var data = _handshakeStream.ToArray();
            return data;
        }


        public byte[] BuildSensorInfoPacket(ImuType imuType, TrackerPosition trackerPosition, TrackerDataType trackerDataType, byte trackerId)
        {
            BigEndianBinaryWriter writer = _sensorInfoWriter;
            _sensorInfoStream.Position = 0;
            writer.Write((int)UDPPackets.SENSOR_INFO); // Packet header
            writer.Write((long)_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker Id
            writer.Write((byte)0); // Sensor status
            writer.Write((byte)imuType); // imu type
            writer.Write((short)0); // Magnetometer support
            writer.Write((byte)trackerPosition); // Tracker Position
            writer.Write((byte)trackerDataType); // Tracker Data Type
            _sensorInfoStream.Position = 0;
            var data = _sensorInfoStream.ToArray();
            return data;
        }

        public byte[] BuildRotationAndAccelerationBundle(Quaternion rotation, Vector3 acceleration, byte trackerId)
        {
            BigEndianBinaryWriter writer = _rotationAndAccelerationPacketWriter;
            _rotationAndAccelerationPacketStream.Position = 0;
            return _rotationAndAccelerationPacketStream.ToArray();
        }

        public byte[] BuildRotationPacket(Quaternion rotation, byte trackerId)
        {
            BigEndianBinaryWriter writer = _rotationPacketWriter;
            _rotationPacketStream.Position = 0;
            writer.Write(UDPPackets.ROTATION_DATA); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker id
            writer.Write((byte)1); // Data type
            writer.Write(rotation.X); // Quaternion X
            writer.Write(rotation.Y); // Quaternion Y
            writer.Write(rotation.Z); // Quaternion Z
            writer.Write(rotation.W); // Quaternion W
            writer.Write((byte)0); // Calibration Info
            _rotationPacketStream.Position = 0;
            var data = _rotationPacketStream.ToArray();
            return data;
        }
        public byte[] BuildAccelerationPacket(Vector3 acceleration, byte trackerId)
        {
            BigEndianBinaryWriter writer = _accelerationPacketWriter;
            _accelerationPacketStream.Position = 0;
            writer.Write(UDPPackets.ACCELERATION); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker id
            writer.Write(acceleration.X); // Euler X
            writer.Write(acceleration.Y); // Euler Y
            writer.Write(acceleration.Z); // Euler Z
            writer.Write((byte)0); // Calibration Info
            _accelerationPacketStream.Position = 0;
            var data = _accelerationPacketStream.ToArray();
            return data;
        }
        public byte[] BuildGyroPacket(Vector3 gyro, byte trackerId)
        {
            BigEndianBinaryWriter writer = _gyroPacketWriter;
            _gyroPacketStream.Position = 0;
            writer.Write(UDPPackets.GYRO); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker id
            writer.Write((byte)1); // Data type 
            writer.Write(gyro.X); // Euler X
            writer.Write(gyro.Y); // Euler Y
            writer.Write(gyro.Z); // Euler Z
            writer.Write((byte)0); // Calibration Info
            _gyroPacketStream.Position = 0;
            var data = _gyroPacketStream.ToArray();
            return data;
        }
        public byte[] BuildFlexDataPacket(float flexData, byte trackerId)
        {
            BigEndianBinaryWriter writer = _flexDataPacketWriter;
            _flexdataPacketStream.Position = 0;
            writer.Write(UDPPackets.FLEX_DATA_PACKET); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker id
            writer.Write(flexData); // Flex data
            _flexdataPacketStream.Position = 0;
            var data = _flexdataPacketStream.ToArray();
            return data;
        }
        public byte[] BuildButtonPushedPacket(UserActionType userActionType)
        {
            BigEndianBinaryWriter writer = _buttonPushPacketWriter;
            _buttonPushPacketStream.Position = 0;
            writer.Write(UDPPackets.CALIBRATION_RESET); // Header 21
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)userActionType); // Action
            _buttonPushPacketStream.Position = 0;
            var data = _buttonPushPacketStream.ToArray();
            return data;
        }
        public byte[] BuildBatteryLevelPacket(float battery, float voltage)
        {
            BigEndianBinaryWriter writer = _batteryLevelPacketWriter;
            _batteryLevelPacketStream.Position = 0;
            writer.Write(UDPPackets.BATTERY_LEVEL); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write(voltage); // Battery data
            writer.Write(battery / 100); // Battery data
            _batteryLevelPacketStream.Position = 0;
            var data = _batteryLevelPacketStream.ToArray();
            return data;
        }

        public byte[] BuildMagnetometerPacket(Vector3 magnetometer, int trackerId)
        {
            BigEndianBinaryWriter writer = _magnetometerPacketWriter;
            _magnetometerPacketStream.Position = 0;
            writer.Write(UDPPackets.MAG); // Header
            writer.Write(_packetId++); // Packet counter
            writer.Write((byte)trackerId); // Tracker id
            writer.Write((byte)1); // Data type 
            writer.Write(magnetometer.X); // Euler X
            writer.Write(magnetometer.Y); // Euler Y
            writer.Write(magnetometer.Z); // Euler Z
            writer.Write((byte)0); // Calibration Info
            _magnetometerPacketStream.Position = 0;
            var data = _magnetometerPacketStream.ToArray();
            return data;
        }
        public byte[] BuildHapticPacket(float intensity, int duration) {
            BigEndianBinaryWriter writer = _hapticPacketWriter;
            hapticPacketStream.Position = 0;
            writer.Write(new byte[3]); // Padding
            writer.Write((byte)UDPPackets.HAPTICS); // Header
            writer.Write(intensity); // Vibration Intensity
            writer.Write(duration); // Haptic Duration
            writer.Write(true); // Haptics active
            hapticPacketStream.Position = 0;
            var data = hapticPacketStream.ToArray();
            return data;
        }
    }
}

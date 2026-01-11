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

        // Pre-allocated buffers for high-frequency packets
        private readonly byte[] _rotationBuffer = new byte[1 + 8 + 1 + 1 + 16 + 1];
        private readonly byte[] _accelerationBuffer = new byte[1 + 8 + 12 + 1];
        private readonly byte[] _gyroBuffer = new byte[1 + 8 + 1 + 1 + 12 + 1];
        private readonly byte[] _magnetometerBuffer = new byte[1 + 8 + 1 + 1 + 12 + 1];
        private readonly byte[] _flexDataBuffer = new byte[1 + 8 + 1 + 4];
        private readonly byte[] _buttonBuffer = new byte[1 + 8 + 1];
        private readonly byte[] _batteryBuffer = new byte[1 + 8 + 4 + 4];
        private readonly byte[] _hapticBuffer = new byte[3 + 1 + 4 + 4 + 1];

        public readonly byte[] HeartBeat;

        public PacketBuilder(string fwString)
        {
            _identifierString = fwString;
            HeartBeat = CreateHeartBeat();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long NextPacketId()
        {
            if (_packetId >= long.MaxValue)
                _packetId = 0;
            return _packetId++;
        }

        ref struct BigEndianSpanWriter
        {
            private Span<byte> _span;
            private int _offset;

            public BigEndianSpanWriter(Span<byte> span)
            {
                _span = span;
                _offset = 0;
            }

            public void WriteByte(byte value) => _span[_offset++] = value;
            public void WriteInt16(short value)
            {
                BinaryPrimitives.WriteInt16BigEndian(_span.Slice(_offset, 2), value);
                _offset += 2;
            }
            public void WriteInt32(int value)
            {
                BinaryPrimitives.WriteInt32BigEndian(_span.Slice(_offset, 4), value);
                _offset += 4;
            }
            public void WriteInt64(long value)
            {
                BinaryPrimitives.WriteInt64BigEndian(_span.Slice(_offset, 8), value);
                _offset += 8;
            }
            public void WriteSingle(float value)
            {
                BinaryPrimitives.WriteSingleBigEndian(_span.Slice(_offset, 4), value);
                _offset += 4;
            }
            public int Position => _offset;
        }

        private byte[] CreateHeartBeat()
        {
            var span = new Span<byte>(new byte[1 + 8 + 1]);
            var writer = new BigEndianSpanWriter(span);
            writer.WriteByte((byte)UDPPackets.HEARTBEAT);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(0); // TrackerId
            return span.ToArray(); // only called once
        }

        public ReadOnlyMemory<byte> BuildRotationPacket(Quaternion r, byte trackerId)
        {
            var writer = new BigEndianSpanWriter(_rotationBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.ROTATION_DATA);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(trackerId);
            writer.WriteByte(1); // Data type
            writer.WriteSingle(r.X);
            writer.WriteSingle(r.Y);
            writer.WriteSingle(r.Z);
            writer.WriteSingle(r.W);
            writer.WriteByte(0); // Calibration
            return _rotationBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildAccelerationPacket(Vector3 a, byte trackerId)
        {
            var writer = new BigEndianSpanWriter(_accelerationBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.ACCELERATION);
            writer.WriteInt64(NextPacketId());
            writer.WriteSingle(a.X);
            writer.WriteSingle(a.Y);
            writer.WriteSingle(a.Z);
            writer.WriteByte(trackerId);
            return _accelerationBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildGyroPacket(Vector3 g, byte trackerId)
        {
            var writer = new BigEndianSpanWriter(_gyroBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.GYRO);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(trackerId);
            writer.WriteByte(1);
            writer.WriteSingle(g.X);
            writer.WriteSingle(g.Y);
            writer.WriteSingle(g.Z);
            writer.WriteByte(0);
            return _gyroBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildMagnetometerPacket(Vector3 m, byte trackerId)
        {
            var writer = new BigEndianSpanWriter(_magnetometerBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.MAG);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(trackerId);
            writer.WriteByte(1);
            writer.WriteSingle(m.X);
            writer.WriteSingle(m.Y);
            writer.WriteSingle(m.Z);
            writer.WriteByte(0);
            return _magnetometerBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildFlexDataPacket(float flex, byte trackerId)
        {
            var writer = new BigEndianSpanWriter(_flexDataBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.FLEX_DATA_PACKET);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(trackerId);
            writer.WriteSingle(flex);
            return _flexDataBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildButtonPushedPacket(UserActionType action)
        {
            var writer = new BigEndianSpanWriter(_buttonBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.CALIBRATION_RESET);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte((byte)action);
            return _buttonBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildBatteryLevelPacket(float battery, float voltage)
        {
            var writer = new BigEndianSpanWriter(_batteryBuffer.AsSpan());
            writer.WriteByte((byte)UDPPackets.BATTERY_LEVEL);
            writer.WriteInt64(NextPacketId());
            writer.WriteSingle(voltage);
            writer.WriteSingle(battery / 100);
            return _batteryBuffer.AsMemory(0, writer.Position);
        }

        public ReadOnlyMemory<byte> BuildHapticPacket(float intensity, int duration)
        {
            var writer = new BigEndianSpanWriter(_hapticBuffer.AsSpan());
            writer.WriteByte(0); writer.WriteByte(0); writer.WriteByte(0); // padding
            writer.WriteByte((byte)UDPPackets.HAPTICS);
            writer.WriteSingle(intensity);
            writer.WriteInt32(duration);
            writer.WriteByte(1); // haptics active
            return _hapticBuffer.AsMemory(0, writer.Position);
        }

        public byte[] BuildHandshakePacket(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magStatus, byte[] mac)
        {
            var span = new Span<byte>(new byte[512]);
            var writer = new BigEndianSpanWriter(span);
            writer.WriteByte((byte)UDPPackets.HANDSHAKE);
            writer.WriteInt64(NextPacketId());
            writer.WriteInt32((int)boardType);
            writer.WriteInt32((int)imuType);
            writer.WriteInt32((int)mcuType);
            writer.WriteInt32((int)magStatus);
            writer.WriteInt32((int)magStatus);
            writer.WriteInt32((int)magStatus);
            writer.WriteInt32(_protocolVersion);

            var idBytes = System.Text.Encoding.UTF8.GetBytes(_identifierString);
            idBytes.CopyTo(span.Slice(writer.Position, idBytes.Length));
            writer = new BigEndianSpanWriter(span.Slice(writer.Position + idBytes.Length));
            mac.CopyTo(span.Slice(writer.Position, mac.Length));
            return span.Slice(0, writer.Position + mac.Length).ToArray();
        }

        public byte[] BuildSensorInfoPacket(ImuType imuType, TrackerPosition pos, TrackerDataType dataType, byte trackerId)
        {
            var span = new Span<byte>(new byte[16]);
            var writer = new BigEndianSpanWriter(span);
            writer.WriteInt32((int)UDPPackets.SENSOR_INFO);
            writer.WriteInt64(NextPacketId());
            writer.WriteByte(trackerId);
            writer.WriteByte(0); // sensor status
            writer.WriteByte((byte)imuType);
            writer.WriteInt16(1); // calibration state
            writer.WriteByte((byte)pos);
            writer.WriteByte((byte)dataType);
            return span.Slice(0, writer.Position).ToArray();
        }
    }
}

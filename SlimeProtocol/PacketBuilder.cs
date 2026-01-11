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

        public PacketBuilder(string fwString)
        {
            _identifierString = fwString;
            HeartBeat = CreateHeartBeat();
        }

        public readonly byte[] HeartBeat;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long NextPacketId()
        {
            if (_packetId >= long.MaxValue) _packetId = 0;
            return _packetId++;
        }

        ref struct BigEndianWriter
        {
            private Span<byte> _span;
            private int _pos;

            public BigEndianWriter(Span<byte> span)
            {
                _span = span;
                _pos = 0;
            }

            public void WriteByte(byte value) => _span[_pos++] = value;

            public void WriteInt16(short value)
            {
                BinaryPrimitives.WriteInt16BigEndian(_span.Slice(_pos, 2), value);
                _pos += 2;
            }

            public void WriteInt32(int value)
            {
                BinaryPrimitives.WriteInt32BigEndian(_span.Slice(_pos, 4), value);
                _pos += 4;
            }

            public void WriteInt64(long value)
            {
                BinaryPrimitives.WriteInt64BigEndian(_span.Slice(_pos, 8), value);
                _pos += 8;
            }

            public void WriteSingle(float value)
            {
                BinaryPrimitives.WriteSingleBigEndian(_span.Slice(_pos, 4), value);
                _pos += 4;
            }

            public void Skip(int count) => _pos += count;

            public int Position => _pos;
        }

        private byte[] CreateHeartBeat()
        {
            var span = new byte[4 + 8 + 1]; // header + packetId + trackerId
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.HEARTBEAT);
            w.WriteInt64(NextPacketId());
            w.WriteByte(0); // trackerId
            return span;
        }

        public byte[] BuildRotationPacket(Quaternion r, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 1 + 16 + 1];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.ROTATION_DATA);
            w.WriteInt64(NextPacketId());
            w.WriteByte(trackerId);
            w.WriteByte(1); // data type
            w.WriteSingle(r.X);
            w.WriteSingle(r.Y);
            w.WriteSingle(r.Z);
            w.WriteSingle(r.W);
            w.WriteByte(0); // calibration
            return span;
        }

        public byte[] BuildAccelerationPacket(Vector3 a, byte trackerId)
        {
            var span = new byte[4 + 8 + 12 + 1]; // header + packetId + 3 floats + trackerId
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.ACCELERATION);
            w.WriteInt64(NextPacketId());
            w.WriteSingle(a.X);
            w.WriteSingle(a.Y);
            w.WriteSingle(a.Z);
            w.WriteByte(trackerId);
            return span;
        }

        public byte[] BuildGyroPacket(Vector3 g, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 1 + 12 + 1];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.GYRO);
            w.WriteInt64(NextPacketId());
            w.WriteByte(trackerId);
            w.WriteByte(1); // data type
            w.WriteSingle(g.X);
            w.WriteSingle(g.Y);
            w.WriteSingle(g.Z);
            w.WriteByte(0);
            return span;
        }

        public byte[] BuildMagnetometerPacket(Vector3 m, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 1 + 12 + 1];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.MAG);
            w.WriteInt64(NextPacketId());
            w.WriteByte(trackerId);
            w.WriteByte(1);
            w.WriteSingle(m.X);
            w.WriteSingle(m.Y);
            w.WriteSingle(m.Z);
            w.WriteByte(0);
            return span;
        }

        // ------------------- Flex Packet -------------------
        public byte[] BuildFlexDataPacket(float flex, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 4];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.FLEX_DATA_PACKET);
            w.WriteInt64(NextPacketId());
            w.WriteByte(trackerId);
            w.WriteSingle(flex);
            return span;
        }

        // ------------------- Button Packet -------------------
        public byte[] BuildButtonPushedPacket(UserActionType action)
        {
            var span = new byte[4 + 8 + 1];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.CALIBRATION_RESET);
            w.WriteInt64(NextPacketId());
            w.WriteByte((byte)action);
            return span;
        }

        // ------------------- Battery Packet -------------------
        public byte[] BuildBatteryLevelPacket(float battery, float voltage)
        {
            var span = new byte[4 + 8 + 4 + 4];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.BATTERY_LEVEL);
            w.WriteInt64(NextPacketId());
            w.WriteSingle(voltage);
            w.WriteSingle(battery / 100f);
            return span;
        }

        // ------------------- Haptics Packet -------------------
        public byte[] BuildHapticPacket(float intensity, int duration)
        {
            var span = new byte[4 + 3 + 4 + 4 + 1]; // header + padding + intensity + duration + active
            var w = new BigEndianWriter(span);
            w.WriteInt32(0); // padding header?
            w.WriteByte(0); w.WriteByte(0); w.WriteByte(0);
            w.WriteByte((byte)UDPPackets.HAPTICS);
            w.WriteSingle(intensity);
            w.WriteInt32(duration);
            w.WriteByte(1); // active
            return span;
        }

        public byte[] BuildHandshakePacket(BoardType boardType, ImuType imuType, McuType mcuType, MagnetometerStatus magStatus, byte[] mac)
        {
            var idBytes = System.Text.Encoding.UTF8.GetBytes(_identifierString);
            int totalSize = 4 + 8 + 4 * 7 + 1 + idBytes.Length + mac.Length;
            var span = new byte[totalSize];

            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.HANDSHAKE);
            w.WriteInt64(NextPacketId());
            w.WriteInt32((int)boardType);
            w.WriteInt32((int)imuType);
            w.WriteInt32((int)mcuType);
            w.WriteInt32((int)magStatus);
            w.WriteInt32((int)magStatus);
            w.WriteInt32((int)magStatus);
            w.WriteInt32(_protocolVersion);

            // Identifier string
            w.WriteByte((byte)idBytes.Length);
            idBytes.CopyTo(span.AsSpan(w.Position));
            w.Skip(idBytes.Length);

            // MAC address
            mac.CopyTo(span.AsSpan(w.Position));
            w.Skip(mac.Length);

            return span;
        }

        public byte[] BuildSensorInfoPacket(ImuType imuType, TrackerPosition pos, TrackerDataType dataType, byte trackerId)
        {
            var span = new byte[4 + 8 + 1 + 1 + 1 + 2 + 1 + 1];
            var w = new BigEndianWriter(span);
            w.WriteInt32((int)UDPPackets.SENSOR_INFO);
            w.WriteInt64(NextPacketId());
            w.WriteByte(trackerId);
            w.WriteByte(0); // sensor status
            w.WriteByte((byte)imuType);
            w.WriteInt16(1); // calibration
            w.WriteByte((byte)pos);
            w.WriteByte((byte)dataType);
            return span;
        }
    }
}

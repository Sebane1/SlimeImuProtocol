﻿namespace SlimeImuProtocol.Utility {
    public class BigEndianBinaryWriter : BinaryWriter {
        public BigEndianBinaryWriter(Stream stream) : base(stream) { }
        public override void Write(float value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(int value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(uint value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(long value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(short value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(double value) {
            base.Write(value.CorrectEndian());
        }
        public override void Write(byte[] value) {
            base.Write(value);
        }
        public override void Write(ushort value) {
            base.Write(value.CorrectEndian());
        }
    }
}

namespace Everything_To_IMU_SlimeVR.Utility {
    public static class BigEndianExtensions {
        static bool _skipCorrection = false;

        public static bool SkipCorrection { get => _skipCorrection; set => _skipCorrection = value; }

        public static int CorrectEndian(this int val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] intAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(intAsBytes);
                return BitConverter.ToInt32(intAsBytes, 0);
            }
            return val;
        }
        public static uint CorrectEndian(this uint val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] intAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(intAsBytes);
                return BitConverter.ToUInt32(intAsBytes, 0);
            }
            return val;
        }
        public static long CorrectEndian(this long val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] longAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(longAsBytes);
                return BitConverter.ToInt64(longAsBytes, 0);
            }
            return val;
        }
        public static float CorrectEndian(this float val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] floatAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(floatAsBytes);
                return BitConverter.ToSingle(floatAsBytes, 0);
            }
            return val;
        }
        public static short CorrectEndian(this short val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] shortAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(shortAsBytes);
                return BitConverter.ToInt16(shortAsBytes, 0);
            }
            return val;
        }
        public static double CorrectEndian(this double val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] doubleAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(doubleAsBytes);
                return BitConverter.ToDouble(doubleAsBytes, 0);
            }
            return val;
        }

        public static ushort CorrectEndian(this ushort val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                byte[] doubleAsBytes = BitConverter.GetBytes(val);
                Array.Reverse(doubleAsBytes);
                return BitConverter.ToUInt16(doubleAsBytes, 0);
            }
            return val;
        }

        public static byte[] CorrectEndian(this byte[] val) {
            if (BitConverter.IsLittleEndian && !_skipCorrection) {
                Array.Reverse(val);
            }
            return val;
        }
    }
}

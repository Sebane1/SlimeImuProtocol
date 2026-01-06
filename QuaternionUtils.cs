using System;
using System.Numerics;

public static class QuaternionUtils {
    private static Vector3 NormalizeVector(float x, float y, float z) {
        float length = MathF.Sqrt(x * x + y * y + z * z);
        if (length == 0f) return new Vector3(0f, 0f, 0f);
        return new Vector3(x / length, y / length, z / length);
    }
    public static double[] ToVQFDoubleArray(this Vector3 vector3) {
        return new double[] { (double)vector3.X, (double)-vector3.Z, (double)vector3.Y };
    }
    public static void ToAxisAngle(this Quaternion q, out Vector3 axis, out float angle)
    {
        if (q.W > 1f) q = Quaternion.Normalize(q); // normalize if needed

        angle = 2f * MathF.Acos(q.W);
        float s = MathF.Sqrt(1f - q.W * q.W);

        if (s < 0.0001f)
        {
            // If s is close to zero, direction of axis is not important
            axis = new Vector3(1, 0, 0);
        } else
        {
            axis = new Vector3(q.X / s, q.Y / s, q.Z / s);
        }
    }
    /// <summary>
    /// Returns the local rotation of a child relative to a parent rotation
    /// </summary>
    public static Quaternion LocalRotation(this Quaternion child, Quaternion parent)
    {
        // Equivalent to Unity: child.localRotation = Quaternion.Inverse(parent.rotation) * child.rotation
        return Quaternion.Normalize(Quaternion.Inverse(parent) * child);
    }
    public static Vector3 TransformAxis(Quaternion rotation, Vector3 localAxis)
    {
        return Vector3.Transform(localAxis, rotation); // rotation applied to vector
    }

    public static Quaternion Normalize(this Quaternion q)
    {
        float mag = (float)Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
        if (mag < 1e-6f)
            return Quaternion.Identity; // fallback
        return new Quaternion(q.X / mag, q.Y / mag, q.Z / mag, q.W / mag);
    }

    public static Quaternion QuatFromGravity(
        float x, float y, float z,
        float cx, float cy, float cz,
        float scale
    ) {
        // 1. Scale raw values
        float sx = (x - cx) / scale;
        float sy = (y - cy) / scale;
        float sz = (z - cz) / scale;

        // 2. Clamp values to [-1.5, 1.5]
        const float clamp = 1f;
        sx = Math.Clamp(sx, -clamp, clamp);
        sy = Math.Clamp(sy, -clamp, clamp);
        sz = Math.Clamp(sz, -clamp, clamp);

        // 3. Use custom normalize function
        Vector3 gravity = NormalizeVector(sx, sy, sz);
        Vector3 reference = new Vector3(0f, 0f, -1f);

        // 4. Cross product to find axis
        Vector3 axis = new Vector3(
            gravity.Y * reference.Z - gravity.Z * reference.Y,
            gravity.Z * reference.X - gravity.X * reference.Z,
            gravity.X * reference.Y - gravity.Y * reference.X
        );

        // 5. Dot product and clamp
        float dot = gravity.X * reference.X + gravity.Y * reference.Y + gravity.Z * reference.Z;
        dot = Math.Clamp(dot, -1.0f, 1.0f); // ensure acos is valid

        // 6. Edge cases
        if (MathF.Abs(dot - 1.0f) < 1e-5f) {
            return new Quaternion(0f, 0f, 0f, 1f); // identity
        }
        if (MathF.Abs(dot + 1.0f) < 1e-5f) {
            return new Quaternion(1f, 0f, 0f, 0f); // 180 degrees around X
        }

        // 7. Normalize axis
        axis = NormalizeVector(axis.X, axis.Y, axis.Z);

        // 8. Convert axis-angle to quaternion
        float angle = MathF.Acos(dot);
        float halfAngle = angle * 0.5f;
        float sinHalf = MathF.Sin(halfAngle);

        return new Quaternion(
            axis.X * sinHalf,
            axis.Y * sinHalf,
            axis.Z * sinHalf,
            MathF.Cos(halfAngle)
        );
    }
}

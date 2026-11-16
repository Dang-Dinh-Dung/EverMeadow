using System;

namespace Evermeadow.Foundation
{
    /// <summary>
    /// Engine-free 3D vector used by pure-C# cores (ADR-0001: cores never reference UnityEngine). The game is 2D side-view:
    /// X and Y are the play plane, Z is depth / layering. Adapters convert to and from <c>UnityEngine.Vector3</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// var offset = new Vec3(0f, 0f, 0.5f);
    /// Vec3 p = owner + offset;
    /// </code>
    /// </example>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        /// <summary>Horizontal component.</summary>
        public readonly float X;

        /// <summary>Vertical component (positive is up).</summary>
        public readonly float Y;

        /// <summary>Depth component.</summary>
        public readonly float Z;

        /// <summary>The zero vector.</summary>
        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        /// <summary>Creates a vector from its components.</summary>
        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Euclidean length.</summary>
        public float Magnitude => (float)Math.Sqrt((double)X * X + (double)Y * Y + (double)Z * Z);

        /// <summary>Component-wise scale.</summary>
        public static Vec3 operator *(Vec3 v, float s) => new Vec3(v.X * s, v.Y * s, v.Z * s);

        /// <summary>Component-wise sum.</summary>
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        /// <inheritdoc />
        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (((X.GetHashCode() * 397) ^ Y.GetHashCode()) * 397) ^ Z.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}

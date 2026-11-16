using System;

namespace Evermeadow.Foundation
{
    /// <summary>
    /// Engine-free 2D vector used by pure-C# cores (ADR-0001: cores never reference UnityEngine).
    /// Adapters convert to and from <c>UnityEngine.Vector2</c> at the boundary.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        /// <summary>Horizontal component.</summary>
        public readonly float X;

        /// <summary>Vertical component (positive is up).</summary>
        public readonly float Y;

        /// <summary>The zero vector.</summary>
        public static readonly Vec2 Zero = new Vec2(0f, 0f);

        /// <summary>Creates a vector from its components.</summary>
        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Euclidean length.</summary>
        public float Magnitude => (float)Math.Sqrt((double)X * X + (double)Y * Y);

        /// <summary>Component-wise scale.</summary>
        public static Vec2 operator *(Vec2 v, float s) => new Vec2(v.X * s, v.Y * s);

        /// <summary>Component-wise sum.</summary>
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

        /// <inheritdoc />
        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}

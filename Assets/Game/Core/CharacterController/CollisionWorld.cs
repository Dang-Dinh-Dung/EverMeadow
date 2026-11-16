using Evermeadow.Foundation;

namespace Evermeadow.Core.CharacterController
{
    /// <summary>One hit of a box cast (hook H-3: a fake world builds these from analytic geometry).</summary>
    public struct CastHit
    {
        /// <summary>Distance travelled before contact.</summary>
        public float Distance;
        /// <summary>Surface normal at the contact (unit length).</summary>
        public Vec2 Normal;
        /// <summary>The collider is a one-way platform (supports only from above, Rule 22).</summary>
        public bool OneWay;
        /// <summary>Highest y of the collider (one-way: feet must be above it).</summary>
        public float Top;
        /// <summary>Stable id of the collider for the duration of a session (DropThrough ignore list).</summary>
        public int ColliderId;
    }

    /// <summary>
    /// Collision queries the Controller needs (GDD Character Controller Rule 30). Positions are box centres; the box is never
    /// rotated. Casts that start overlapping a collider ignore that collider (Physics2D <c>queriesStartInColliders = false</c>),
    /// and never report the character's own collider.
    /// </summary>
    public interface ICollisionWorld
    {
        /// <summary>
        /// Casts a box of <paramref name="size"/> from <paramref name="center"/> along unit <paramref name="direction"/> up to
        /// <paramref name="distance"/>. Writes hits into <paramref name="results"/> and returns the count (order unspecified).
        /// </summary>
        int Cast(Vec2 center, Vec2 size, Vec2 direction, float distance, CastHit[] results);

        /// <summary>
        /// Smallest push that separates a box at <paramref name="center"/> from overlapping solid colliders (one-way platforms
        /// excluded). False when the box overlaps nothing.
        /// </summary>
        bool ComputePenetration(Vec2 center, Vec2 size, out Vec2 push);
    }
}

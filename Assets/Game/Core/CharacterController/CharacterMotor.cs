using System;
using Evermeadow.Foundation;

namespace Evermeadow.Core.CharacterController
{
    /// <summary>Contacts met during one <see cref="CharacterMotor.Move"/>.</summary>
    public struct MoveContacts
    {
        /// <summary>A walkable surface stopped downward motion.</summary>
        public bool Ground;
        /// <summary>Normal of that surface.</summary>
        public Vec2 GroundNormal;
        /// <summary>A surface facing down stopped upward motion.</summary>
        public bool Ceiling;
        /// <summary>A wall or a too-steep slope stopped horizontal motion.</summary>
        public bool Wall;
    }

    /// <summary>What supports the character from below.</summary>
    public struct GroundInfo
    {
        /// <summary>Gap between the feet (minus skin) and the surface.</summary>
        public float Gap;
        /// <summary>Most upward-facing normal among the supporting surfaces.</summary>
        public Vec2 Normal;
        /// <summary>Every supporting surface is a one-way platform (DropThrough possible, Rule 18).</summary>
        public bool OneWayOnly;
        /// <summary>Number of supporting colliders recorded in <see cref="CharacterMotor.SupportIds"/>.</summary>
        public int Count;
    }

    /// <summary>
    /// Collide-and-slide solver of the Controller (GDD Character Controller Rule 21–23, F6). Keeps <c>skin</c> between the box and
    /// every surface, so casts never start touching. Filters one-way platforms itself (Rule 22) and keeps the DropThrough ignore
    /// list. Allocates only in the constructor.
    /// </summary>
    public sealed class CharacterMotor
    {
        /// <summary>Most slide iterations per move.</summary>
        public const int MaxIterations = 4;

        private const int MaxIgnored = 4;
        private readonly ICollisionWorld _world;
        private readonly CastHit[] _hits = new CastHit[16];
        private readonly int[] _ignoredIds = new int[MaxIgnored];
        private readonly float[] _ignoredTops = new float[MaxIgnored];
        private int _ignoredCount;

        /// <summary>Supporting collider ids from the last <see cref="ProbeGround"/>.</summary>
        public readonly int[] SupportIds = new int[MaxIgnored];

        /// <summary>Tops of the supporting colliders from the last <see cref="ProbeGround"/>.</summary>
        public readonly float[] SupportTops = new float[MaxIgnored];

        /// <summary>Creates a motor for a box of <paramref name="size"/>.</summary>
        public CharacterMotor(ICollisionWorld world, Vec2 size, float skin, float maxWalkAngleDegrees)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            Size = size;
            Skin = skin;
            WalkCos = (float)Math.Cos(maxWalkAngleDegrees * Math.PI / 180.0) - 1e-4f;
        }

        /// <summary>Box size.</summary>
        public Vec2 Size { get; }

        /// <summary>Gap kept to every surface.</summary>
        public float Skin { get; }

        /// <summary>Lowest normal.y of a walkable surface (F6, with ε).</summary>
        public float WalkCos { get; }

        /// <summary>Number of colliders currently ignored after a DropThrough.</summary>
        public int IgnoredCount => _ignoredCount;

        /// <summary>Feet height of a box centred at <paramref name="center"/>.</summary>
        public float Feet(Vec2 center) => center.Y - Size.Y * 0.5f;

        /// <summary>
        /// Moves the box from <paramref name="pos"/> by <paramref name="delta"/>, sliding along what it hits. Returns the new position.
        /// Steep slopes block like walls unless the motion is downward (then the box slides down them).
        /// </summary>
        public Vec2 Move(Vec2 pos, Vec2 delta, ref MoveContacts contacts)
        {
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                float len = delta.Magnitude;
                if (len < 1e-6f) break;
                var dir = new Vec2(delta.X / len, delta.Y / len);

                int best = Nearest(pos, dir, len + Skin);
                if (best < 0)
                {
                    pos += delta;
                    break;
                }

                CastHit h = _hits[best];
                float travel = Math.Min(len, Math.Max(0f, h.Distance - Skin));
                pos += dir * travel;
                var remaining = new Vec2(delta.X - dir.X * travel, delta.Y - dir.Y * travel);

                Vec2 n = h.Normal;
                bool walkable = n.Y >= WalkCos;
                if (walkable)
                {
                    contacts.Ground = true;
                    contacts.GroundNormal = n;
                }
                else if (n.Y < -0.01f) contacts.Ceiling = true;
                else contacts.Wall = true;

                if (!walkable && n.Y > 0f && remaining.Y >= 0f)
                    n = new Vec2(n.X >= 0f ? 1f : -1f, 0f); // too steep to walk up: a wall

                float into = remaining.X * n.X + remaining.Y * n.Y;
                if (into < 0f) remaining = new Vec2(remaining.X - n.X * into, remaining.Y - n.Y * into);
                delta = remaining;
            }
            return pos;
        }

        /// <summary>Casts down up to <paramref name="distance"/> for walkable support. Fills <see cref="SupportIds"/>.</summary>
        public bool ProbeGround(Vec2 pos, float distance, out GroundInfo ground)
        {
            ground = default;
            var down = new Vec2(0f, -1f);
            int count = _world.Cast(pos, Size, down, distance + Skin, _hits);
            float min = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (IsWalkableSupport(_hits[i], pos) && _hits[i].Distance < min) min = _hits[i].Distance;
            }
            if (min == float.MaxValue) return false;

            ground.Gap = Math.Max(0f, min - Skin);
            ground.OneWayOnly = true;
            ground.Normal = new Vec2(0f, -1f);
            for (int i = 0; i < count; i++)
            {
                CastHit h = _hits[i];
                if (!IsWalkableSupport(h, pos) || h.Distance > min + 0.002f) continue;
                if (!h.OneWay) ground.OneWayOnly = false;
                if (h.Normal.Y > ground.Normal.Y) ground.Normal = h.Normal;
                if (ground.Count < SupportIds.Length)
                {
                    SupportIds[ground.Count] = h.ColliderId;
                    SupportTops[ground.Count] = h.Top;
                    ground.Count++;
                }
            }
            return true;
        }

        /// <summary>
        /// Ceiling corner correction (Rule 23): when only the edge of the head clips a ceiling, shifts the box sideways by up to
        /// <paramref name="maxShift"/> if the shifted box is clear sideways and for <paramref name="upDistance"/> above.
        /// </summary>
        public bool TryCornerCorrect(Vec2 pos, float maxShift, float upDistance, out Vec2 corrected)
        {
            corrected = pos;
            var up = new Vec2(0f, 1f);
            for (int side = -1; side <= 1; side += 2)
            {
                var dir = new Vec2(side, 0f);
                if (Nearest(pos, dir, maxShift + Skin) >= 0) continue;
                var shifted = new Vec2(pos.X + side * maxShift, pos.Y);
                if (Nearest(shifted, up, upDistance + Skin) >= 0) continue;
                // Prefer the smallest shift that clears: halve while the smaller shift still clears.
                float shift = maxShift;
                for (int i = 0; i < 4; i++)
                {
                    var half = new Vec2(pos.X + side * shift * 0.5f, pos.Y);
                    if (Nearest(half, up, upDistance + Skin) >= 0) break;
                    shift *= 0.5f;
                }
                corrected = new Vec2(pos.X + side * shift, pos.Y);
                return true;
            }
            return false;
        }

        /// <summary>Pushes the box out of solid geometry by at most <paramref name="maxPush"/>. False when it stays stuck.</summary>
        public bool Depenetrate(ref Vec2 pos, float maxPush)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!_world.ComputePenetration(pos, Size, out Vec2 push)) return true;
                float m = push.Magnitude;
                if (m > maxPush) return false;
                var dir = m > 1e-6f ? new Vec2(push.X / m, push.Y / m) : new Vec2(0f, 1f);
                pos += push + dir * Skin;
            }
            return !_world.ComputePenetration(pos, Size, out _);
        }

        /// <summary>Starts ignoring the supporting colliders of the last probe (DropThrough, Rule 18).</summary>
        public void IgnoreSupports(in GroundInfo ground)
        {
            _ignoredCount = 0;
            for (int i = 0; i < ground.Count && i < MaxIgnored; i++)
            {
                _ignoredIds[_ignoredCount] = SupportIds[i];
                _ignoredTops[_ignoredCount] = SupportTops[i];
                _ignoredCount++;
            }
        }

        /// <summary>Stops ignoring colliders whose top is above the feet of a box at <paramref name="pos"/>.</summary>
        public void ReleaseIgnoredBelowFeet(Vec2 pos)
        {
            float feet = Feet(pos);
            int w = 0;
            for (int i = 0; i < _ignoredCount; i++)
            {
                if (feet < _ignoredTops[i] - Skin) continue; // fully passed: forget it
                _ignoredIds[w] = _ignoredIds[i];
                _ignoredTops[w] = _ignoredTops[i];
                w++;
            }
            _ignoredCount = w;
        }

        /// <summary>Forgets the DropThrough ignore list.</summary>
        public void ClearIgnored() => _ignoredCount = 0;

        private int Nearest(Vec2 pos, Vec2 dir, float distance)
        {
            int count = _world.Cast(pos, Size, dir, distance, _hits);
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!Accepts(_hits[i], pos, dir) || _hits[i].Distance >= bestDist) continue;
                best = i;
                bestDist = _hits[i].Distance;
            }
            return best;
        }

        private bool IsWalkableSupport(in CastHit h, Vec2 pos) => h.Normal.Y >= WalkCos && Accepts(h, pos, new Vec2(0f, -1f));

        // One-way platforms support only from above, while moving down, and not while ignored (Rule 22).
        private bool Accepts(in CastHit h, Vec2 pos, Vec2 dir)
        {
            if (h.Distance < 0f) return false;
            if (!h.OneWay) return true;
            if (dir.Y >= 0f || h.Normal.Y < WalkCos) return false;
            for (int i = 0; i < _ignoredCount; i++)
            {
                if (_ignoredIds[i] == h.ColliderId) return false;
            }
            return Feet(pos) >= h.Top - Skin * 2f;
        }
    }
}

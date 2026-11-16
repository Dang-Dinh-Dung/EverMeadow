using Evermeadow.Foundation;
using UnityEngine;

namespace Evermeadow.Core.CharacterController.Unity
{
    /// <summary>
    /// <see cref="ICollisionWorld"/> over Physics2D. Box casts use an explicit origin (<c>Physics2D.BoxCast</c> with a
    /// <c>ContactFilter2D</c>), so the core keeps the authoritative position and never depends on when a body pose becomes
    /// visible to queries. Triggers and the character's own colliders are skipped. Result buffers are allocated once.
    /// </summary>
    public sealed class Physics2DCollisionWorld : ICollisionWorld
    {
        private readonly Collider2D _self;
        private readonly Rigidbody2D _selfBody;
        private readonly RaycastHit2D[] _raw = new RaycastHit2D[32];
        private readonly Collider2D[] _overlaps = new Collider2D[16];
        private ContactFilter2D _filter;

        /// <summary>Creates a world casting against <paramref name="terrainMask"/>, ignoring <paramref name="self"/>.</summary>
        public Physics2DCollisionWorld(Collider2D self, LayerMask terrainMask)
        {
            _self = self;
            _selfBody = self.attachedRigidbody;
            _filter = new ContactFilter2D { useTriggers = false };
            _filter.SetLayerMask(terrainMask);
        }

        /// <summary>
        /// Back-off distance. Physics2D inflates colliders by their contact offset (0.01 u by default, as large as the core's skin);
        /// a cast that starts inside that margin counts as starting inside the collider and, with queriesStartInColliders off,
        /// misses it. Casting from slightly behind keeps resting contacts visible.
        /// </summary>
        public const float BackOff = 0.05f;

        /// <inheritdoc />
        public int Cast(Vec2 center, Vec2 size, Vec2 direction, float distance, CastHit[] results)
        {
            var dir = new Vector2(direction.X, direction.Y);
            var origin = new Vector2(center.X, center.Y) - dir * BackOff;
            int n = Physics2D.BoxCast(origin, new Vector2(size.X, size.Y), 0f, dir, _filter, _raw, distance + BackOff);
            int w = 0;
            for (int i = 0; i < n && w < results.Length; i++)
            {
                RaycastHit2D h = _raw[i];
                Collider2D c = h.collider;
                if (c == null || IsSelf(c)) continue;
                if (Vector2.Dot(h.normal, dir) >= 0f) continue; // a surface behind the back-off start, not one we move into
                results[w++] = new CastHit
                {
                    Distance = Mathf.Max(0f, h.distance - BackOff),
                    Normal = new Vec2(h.normal.x, h.normal.y),
                    OneWay = c.TryGetComponent(out OneWayPlatform _),
                    Top = c.bounds.max.y,
                    ColliderId = c.GetInstanceID()
                };
            }
            return w;
        }

        /// <inheritdoc />
        public bool ComputePenetration(Vec2 center, Vec2 size, out Vec2 push)
        {
            push = Vec2.Zero;
            var c = new Vector2(center.X, center.Y);
            int n = Physics2D.OverlapBox(c, new Vector2(size.X, size.Y), 0f, _filter, _overlaps);
            if (n == 0) return false;

            // Distance at an explicit pose needs a Rigidbody2D on both colliders, which static terrain lacks. Place our body at the
            // queried centre for the measurement instead, then put it back (spawn / teleport only, not a per-frame path).
            Vector2 saved = _selfBody.position;
            _selfBody.position = c;
            float best = 0f;
            for (int i = 0; i < n; i++)
            {
                Collider2D other = _overlaps[i];
                if (other == null || IsSelf(other) || other.TryGetComponent(out OneWayPlatform _)) continue;
                ColliderDistance2D d = _self.Distance(other);
                if (!d.isValid || !d.isOverlapped || -d.distance <= best) continue;
                best = -d.distance;
                // Overlapped: the normal points from the point on the other collider to ours, i.e. inward, and distance is
                // negative, so normal × distance is the separating push (includes both colliders' contact offsets).
                push = new Vec2(d.normal.x * d.distance, d.normal.y * d.distance);
            }
            _selfBody.position = saved;
            return best > 0f;
        }

        private bool IsSelf(Collider2D c) => c == _self || (_selfBody != null && c.attachedRigidbody == _selfBody);
    }
}

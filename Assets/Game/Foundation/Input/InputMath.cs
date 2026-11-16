using System;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Foundation.Input
{
    /// <summary>Pure formulas of GDD Input: circular dead zone (F4), "Down" qualifier (F3), device-switch stick threshold (F4).</summary>
    public static class InputMath
    {
        /// <summary>Minimum gap between the device-switch stick threshold and the dead zone (F4).</summary>
        public const float SwitchGapOverDeadZone = 0.15f;

        /// <summary>
        /// Circular dead zone with rescale (F4): 0 below <paramref name="deadZone"/>, otherwise the direction scaled to
        /// (r − dz)/(1 − dz) with r clamped to 1. Continuous at r = dz.
        /// </summary>
        public static Vec2 ApplyDeadZone(Vec2 raw, float deadZone)
        {
            float mag = raw.Magnitude;
            float r = Math.Min(mag, 1f);
            if (r < deadZone || mag <= 0f) return Vec2.Zero;
            float scale = (r - deadZone) / (1f - deadZone);
            return new Vec2(raw.X / mag * scale, raw.Y / mag * scale);
        }

        /// <summary>
        /// "Down" qualifier (F3) on Move after dead zone: y ≤ −threshold and the angle to straight down ≤ <paramref name="angleDegrees"/>.
        /// </summary>
        public static bool IsDown(Vec2 move, float threshold, float angleDegrees)
        {
            float mag = move.Magnitude;
            if (mag <= 0f) return false;
            if (move.Y > -threshold) return false;
            double cosLimit = Math.Cos(angleDegrees * Math.PI / 180.0);
            return -move.Y / mag >= cosLimit - 1e-6;
        }

        /// <summary>Effective stick magnitude that switches the active device (F4): max(threshold, deadZone + 0.15).</summary>
        public static float StickSwitchEffective(float threshold, float deadZone) => Math.Max(threshold, deadZone + SwitchGapOverDeadZone);
    }

    /// <summary>
    /// Navigate auto-repeat (GDD Input Rule 16, F5) on the real clock. Emits at most one step per frame and drops the excess,
    /// so a long frame never moves the cursor several cells. Changing quadrant or releasing restarts the count.
    /// </summary>
    public sealed class NavRepeater
    {
        private readonly long _delay;
        private readonly long _interval;
        private readonly float _threshold;
        private int _quadrant = -1;
        private long _t0;
        private long _emitted;

        /// <summary>Creates a repeater from config values.</summary>
        public NavRepeater(double delaySeconds, double intervalSeconds, float threshold)
        {
            _delay = Ticks.FromSeconds(delaySeconds);
            _interval = Math.Max(1L, Ticks.FromSeconds(intervalSeconds));
            _threshold = threshold;
        }

        /// <summary>Direction of the step emitted by the last <see cref="Update"/>, or zero.</summary>
        public Vec2 Step { get; private set; }

        /// <summary>Forgets the held direction (mode barrier, focus loss).</summary>
        public void Reset()
        {
            _quadrant = -1;
            _emitted = 0;
            Step = Vec2.Zero;
        }

        /// <summary>Updates with Navigate after dead zone at real time <paramref name="realNow"/>; sets <see cref="Step"/>.</summary>
        public void Update(Vec2 navigate, long realNow)
        {
            int q = Quadrant(navigate);
            if (q < 0)
            {
                Reset();
                return;
            }

            if (q != _quadrant)
            {
                _quadrant = q;
                _t0 = realNow;
                _emitted = 1;
                Step = Direction(q);
                return;
            }

            long elapsed = realNow - _t0;
            long steps = elapsed < _delay ? 1 : 1 + ((elapsed - _delay) / _interval + 1);
            bool emit = steps > _emitted;
            _emitted = steps;
            Step = emit ? Direction(q) : Vec2.Zero;
        }

        private int Quadrant(Vec2 v)
        {
            if (v.Magnitude < _threshold) return -1;
            if (Math.Abs(v.X) >= Math.Abs(v.Y)) return v.X >= 0 ? 0 : 2; // right, left
            return v.Y >= 0 ? 1 : 3; // up, down
        }

        private static Vec2 Direction(int q)
        {
            switch (q)
            {
                case 0: return new Vec2(1f, 0f);
                case 1: return new Vec2(0f, 1f);
                case 2: return new Vec2(-1f, 0f);
                default: return new Vec2(0f, -1f);
            }
        }
    }
}

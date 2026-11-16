using System;
using System.Collections.Generic;

namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Tuning knobs of the Input system (GDD Input › Tuning Knobs). Loaded from data; never hardcoded at call sites.
    /// Use <see cref="InputConfigValidator.Validate"/> before handing a config to <see cref="InputService"/>.
    /// </summary>
    [Serializable]
    public sealed class InputConfig
    {
        /// <summary>Ring buffer size (power of two, 32–128).</summary>
        public int BufferCapacity = 32;
        /// <summary>Hard ceiling on a buffered press, real seconds (0.5–2.0).</summary>
        public double BufferRetentionSeconds = 1.0;
        /// <summary>Assumed total real hit-stop time inside one window, seconds (0–0.5). Warning only.</summary>
        public double HitstopBudgetSeconds = 0.21;
        /// <summary>Smallest hit-stop time scale (Combat-owned constant, referenced for F2).</summary>
        public double HitstopMinScale = 0.05;
        /// <summary>Move Y magnitude needed for "Down" (0.3–0.7).</summary>
        public float DownThreshold = 0.5f;
        /// <summary>Half-angle around straight down for DropThrough, degrees (25–44).</summary>
        public float DownAngleDropDegrees = 35f;
        /// <summary>Half-angle around straight down for Plunge, degrees (45–60).</summary>
        public float DownAnglePlungeDegrees = 50f;
        /// <summary>Circular stick dead zone (0.05–0.4).</summary>
        public float DeadZone = 0.2f;
        /// <summary>Trigger press point and device-switch threshold (0.15–0.5).</summary>
        public float TriggerThreshold = 0.3f;
        /// <summary>Raw stick magnitude that switches the active device (0.4–0.8; floored at DeadZone + 0.15).</summary>
        public float StickSwitchThreshold = 0.5f;
        /// <summary>Mouse travel (1080p-normalised pixels) that switches the active device in Menu/Map (4–24).</summary>
        public float MouseSwitchPixels = 8f;
        /// <summary>Window for accumulating mouse travel, real seconds (0.1–0.5).</summary>
        public double MouseSwitchSeconds = 0.25;
        /// <summary>Navigate magnitude that counts as holding a direction (0.3–0.7).</summary>
        public float NavThreshold = 0.5f;
        /// <summary>Delay before Navigate starts repeating, real seconds (0.25–0.6).</summary>
        public double NavRepeatDelaySeconds = 0.35;
        /// <summary>Navigate repeat interval, real seconds (0.05–0.15).</summary>
        public double NavRepeatIntervalSeconds = 0.09;

        /// <summary>Returns a copy.</summary>
        public InputConfig Clone() => (InputConfig)MemberwiseClone();
    }

    /// <summary>
    /// Pure validator for <see cref="InputConfig"/> (GDD Input › Formulas › validator; test hook H-10).
    /// Out-of-range knobs fall back to their default; a violated cross-constraint resets every knob that takes part.
    /// </summary>
    public static class InputConfigValidator
    {
        /// <summary>Peak human press rate used to size the buffer (F2), presses per second.</summary>
        public const double PeakPressRate = 15.0;

        /// <summary>Validates <paramref name="raw"/> and returns the effective config; <paramref name="violations"/> lists what was reset.</summary>
        public static InputConfig Validate(InputConfig raw, out List<string> violations)
        {
            var d = new InputConfig();
            var c = raw?.Clone() ?? new InputConfig();
            violations = new List<string>();

            c.BufferRetentionSeconds = Range(c.BufferRetentionSeconds, 0.5, 2.0, d.BufferRetentionSeconds, "BufferRetentionSeconds", violations);
            c.HitstopBudgetSeconds = Range(c.HitstopBudgetSeconds, 0.0, 0.5, d.HitstopBudgetSeconds, "HitstopBudgetSeconds", violations);
            c.DownThreshold = RangeF(c.DownThreshold, 0.3f, 0.7f, d.DownThreshold, "DownThreshold", violations);
            c.DownAngleDropDegrees = RangeF(c.DownAngleDropDegrees, 25f, 44f, d.DownAngleDropDegrees, "DownAngleDropDegrees", violations);
            c.DownAnglePlungeDegrees = RangeF(c.DownAnglePlungeDegrees, 45f, 60f, d.DownAnglePlungeDegrees, "DownAnglePlungeDegrees", violations);
            c.DeadZone = RangeF(c.DeadZone, 0.05f, 0.4f, d.DeadZone, "DeadZone", violations);
            c.TriggerThreshold = RangeF(c.TriggerThreshold, 0.15f, 0.5f, d.TriggerThreshold, "TriggerThreshold", violations);
            c.StickSwitchThreshold = RangeF(c.StickSwitchThreshold, 0.4f, 0.8f, d.StickSwitchThreshold, "StickSwitchThreshold", violations);
            c.MouseSwitchPixels = RangeF(c.MouseSwitchPixels, 4f, 24f, d.MouseSwitchPixels, "MouseSwitchPixels", violations);
            c.MouseSwitchSeconds = Range(c.MouseSwitchSeconds, 0.1, 0.5, d.MouseSwitchSeconds, "MouseSwitchSeconds", violations);
            c.NavThreshold = RangeF(c.NavThreshold, 0.3f, 0.7f, d.NavThreshold, "NavThreshold", violations);
            c.NavRepeatDelaySeconds = Range(c.NavRepeatDelaySeconds, 0.25, 0.6, d.NavRepeatDelaySeconds, "NavRepeatDelaySeconds", violations);
            c.NavRepeatIntervalSeconds = Range(c.NavRepeatIntervalSeconds, 0.05, 0.15, d.NavRepeatIntervalSeconds, "NavRepeatIntervalSeconds", violations);

            if (c.BufferCapacity < 32 || c.BufferCapacity > 128 || (c.BufferCapacity & (c.BufferCapacity - 1)) != 0)
            {
                violations.Add($"BufferCapacity {c.BufferCapacity} must be a power of two in 32–128; using {d.BufferCapacity}.");
                c.BufferCapacity = d.BufferCapacity;
            }

            if (c.BufferCapacity < RequiredCapacity(c.BufferRetentionSeconds))
            {
                violations.Add($"BufferCapacity {c.BufferCapacity} < ceil(2 x {PeakPressRate} x {c.BufferRetentionSeconds}); both reset.");
                c.BufferCapacity = d.BufferCapacity;
                c.BufferRetentionSeconds = d.BufferRetentionSeconds;
            }

            if (!(c.DownAngleDropDegrees < 45f && c.DownAnglePlungeDegrees >= 45f))
            {
                violations.Add("Down angles must satisfy drop < 45 <= plunge; both reset.");
                c.DownAngleDropDegrees = d.DownAngleDropDegrees;
                c.DownAnglePlungeDegrees = d.DownAnglePlungeDegrees;
            }

            return c;
        }

        /// <summary>Minimum buffer capacity for a retention (F2), before rounding to a power of two.</summary>
        public static int RequiredCapacity(double retentionSeconds) => (int)Math.Ceiling(2.0 * PeakPressRate * retentionSeconds);

        // Float knobs compare in float so a boundary such as 0.4f (slightly above the double 0.4) is accepted.
        private static float RangeF(float value, float min, float max, float fallback, string name, List<string> violations)
        {
            if (!float.IsNaN(value) && value >= min && value <= max) return value;
            violations.Add($"{name} {value} outside {min}–{max}; using {fallback}.");
            return fallback;
        }

        private static double Range(double value, double min, double max, double fallback, string name, List<string> violations)
        {
            if (!double.IsNaN(value) && value >= min && value <= max) return value;
            violations.Add($"{name} {value} outside {min}–{max}; using {fallback}.");
            return fallback;
        }
    }
}

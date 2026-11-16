using System;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// The one place skill timestamps are snapped to the nominal 1/60 s grid (ADR-0002 Decision 3, GDD Skill &amp; Moveset Rule 23).
    /// 1/60 s is not a whole number of microsecond ticks, and <see cref="Ticks.FromSeconds"/> rounds to the nearest tick, so skill
    /// code must never use it: build-time bounds and run-time bounds have to agree. The grid is nominal; the Controller's variable
    /// sub-steps (≤ 1/30 s) fire an event on the first simulation step whose scaled time reaches the snapped tick.
    /// </summary>
    /// <example>
    /// <code>
    /// int steps = SkillTime.StepsOf(0.10);            // 6
    /// long tick = SkillTime.TickOfStep(steps);        // 100000
    /// </code>
    /// </example>
    public static class SkillTime
    {
        /// <summary>Nominal steps per second.</summary>
        public const int StepsPerSecond = 60;

        // Absorbs binary error such as 0.1 × 60 = 6.000000000000001 so that 0.10 s is exactly 6 steps, not 7.
        private const double Epsilon = 1e-6;

        /// <summary>Whole steps needed to reach <paramref name="seconds"/>: ceil(seconds × 60 − ε), never negative.</summary>
        public static int StepsOf(double seconds)
        {
            if (double.IsNaN(seconds) || seconds <= 0.0) return 0;
            double steps = Math.Ceiling(seconds * StepsPerSecond - Epsilon);
            return steps <= 0.0 ? 0 : steps >= int.MaxValue ? int.MaxValue : (int)steps;
        }

        /// <summary>Tick at which step <paramref name="steps"/> is reached, rounded up so an event never fires early.</summary>
        public static long TickOfStep(int steps) => ((long)steps * Ticks.PerSecond + StepsPerSecond - 1) / StepsPerSecond;

        /// <summary>Snaps <paramref name="seconds"/> to the grid and returns its tick (<c>TickOfStep(StepsOf(seconds))</c>).</summary>
        public static long TickOf(double seconds) => TickOfStep(StepsOf(seconds));
    }
}

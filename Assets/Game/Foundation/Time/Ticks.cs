using System;

namespace Evermeadow.Foundation.Time
{
    /// <summary>
    /// Project-wide gameplay time unit (ADR-0001 Decision 2): <c>long</c> ticks, 1 tick = 1 microsecond.
    /// Integer ticks keep boundary compares (Input F1) and floors (Input F5) exact.
    /// </summary>
    public static class Ticks
    {
        /// <summary>Ticks in one second.</summary>
        public const long PerSecond = 1_000_000L;

        /// <summary>Converts seconds to ticks, rounding to the nearest tick.</summary>
        public static long FromSeconds(double seconds) => (long)Math.Round(seconds * PerSecond);

        /// <summary>Converts ticks to seconds (display and tests only; never accumulate the result).</summary>
        public static double ToSeconds(long ticks) => ticks / (double)PerSecond;
    }
}

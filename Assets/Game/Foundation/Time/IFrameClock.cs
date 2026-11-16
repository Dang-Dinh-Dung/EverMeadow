using System;

namespace Evermeadow.Foundation.Time
{
    /// <summary>
    /// Read side of the frame clock (ADR-0001 Decision 2). Input, Character Controller and Combat read time
    /// only through this interface. All values are ticks (<see cref="Ticks"/>).
    /// </summary>
    public interface IFrameClock
    {
        /// <summary>Unscaled time since the clock started. Monotonic.</summary>
        long RealNow { get; }

        /// <summary>Scaled time (hit-stop, pause applied). Monotonic. GDDs call this the "Game" clock.</summary>
        long ScaledNow { get; }

        /// <summary>Unscaled time elapsed during the last <c>Advance</c>.</summary>
        long RealDelta { get; }

        /// <summary>Scaled time elapsed during the last <c>Advance</c>.</summary>
        long ScaledDelta { get; }

        /// <summary>Current effective scale: the minimum of active scale handles, 1.0 when none.</summary>
        double Scale { get; }
    }

    /// <summary>
    /// Write side of the frame clock. The composition root injects it only into Combat (hit-stop) and the pause owner.
    /// No other system may change time speed, and nothing but the frame driver writes <c>Time.timeScale</c>.
    /// </summary>
    public interface ITimeScaleControl
    {
        /// <summary>
        /// Starts a scale. Effective scale is the minimum of all active scales.
        /// With <paramref name="durationRealTicks"/> the handle expires by itself inside <c>Advance</c>
        /// (hit-stop); without it the owner must <see cref="PopScale"/> (pause). A handle whose
        /// <paramref name="isAlive"/> returns false is removed on the next <c>Advance</c> and logged as an error.
        /// </summary>
        ScaleHandle PushScale(double scale, object owner, Func<bool> isAlive, long? durationRealTicks = null);

        /// <summary>Ends a scale. Returns false (and warns) for an invalid, stale or foreign handle.</summary>
        bool PopScale(ScaleHandle handle);
    }

    /// <summary>Token returned by <see cref="ITimeScaleControl.PushScale"/>. Carries a generation so a stale handle never pops a reused slot.</summary>
    public readonly struct ScaleHandle
    {
        internal readonly FrameClock Clock;
        internal readonly int Slot;
        internal readonly int Generation;

        internal ScaleHandle(FrameClock clock, int slot, int generation)
        {
            Clock = clock;
            Slot = slot;
            Generation = generation;
        }

        /// <summary>False for <c>default</c> or a handle a full clock refused.</summary>
        public bool IsValid => Clock != null;
    }
}

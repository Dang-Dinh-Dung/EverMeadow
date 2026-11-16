using System;

namespace Evermeadow.Foundation.Time
{
    /// <summary>
    /// The single frame clock of the project (ADR-0001 Decision 2).
    /// The frame driver calls <see cref="Advance"/> first thing every frame with the real time source in ticks.
    /// Scaled time integrates the real interval piecewise: a hit-stop handle that expires mid-frame splits the
    /// interval at its expiry, so the first frame after a hit-stop already runs at the restored speed
    /// (Character Controller OQ-C3). Scales combine by minimum. Rounding keeps the sub-tick remainder so
    /// <see cref="ScaledNow"/> never drifts.
    /// </summary>
    public sealed class FrameClock : IFrameClock, ITimeScaleControl
    {
        /// <summary>Maximum number of simultaneous scale handles.</summary>
        public const int Capacity = 16;

        /// <summary>Largest accepted scale value.</summary>
        public const double MaxScale = 10.0;

        private struct Entry
        {
            public bool Active;
            public int Generation;
            public double Scale;
            public object Owner;
            public Func<bool> IsAlive;
            public long ExpireAt; // long.MaxValue = no expiry
        }

        private readonly Entry[] _entries = new Entry[Capacity];
        private readonly ILogSink _log;
        private bool _started;
        private bool _loggedBackwards;
        private double _remainder;

        /// <summary>Creates a clock. Nothing runs until the first <see cref="Advance"/>.</summary>
        public FrameClock(ILogSink log = null)
        {
            _log = log ?? new NullLogSink();
        }

        /// <inheritdoc />
        public long RealNow { get; private set; }

        /// <inheritdoc />
        public long ScaledNow { get; private set; }

        /// <inheritdoc />
        public long RealDelta { get; private set; }

        /// <inheritdoc />
        public long ScaledDelta { get; private set; }

        /// <inheritdoc />
        public double Scale => ScaleAt(RealNow);

        /// <summary>
        /// Advances the clock to <paramref name="realSourceTicks"/>. The first call only anchors the clock.
        /// A source that runs backwards is clamped (time never decreases) and logged once per session.
        /// </summary>
        public void Advance(long realSourceTicks)
        {
            if (!_started)
            {
                _started = true;
                RealNow = realSourceTicks;
                RealDelta = 0;
                ScaledDelta = 0;
                return;
            }

            if (realSourceTicks < RealNow)
            {
                if (!_loggedBackwards)
                {
                    _loggedBackwards = true;
                    _log.Error("FrameClock.Backwards", $"Real time source went backwards ({realSourceTicks} < {RealNow}); clamped.");
                }
                realSourceTicks = RealNow;
            }

            RemoveDeadOwners();

            long start = RealNow;
            long end = realSourceTicks;
            double scaled = 0.0;
            long t = start;
            while (t < end)
            {
                long segmentEnd = Math.Min(end, NextExpiryAfter(t));
                scaled += (segmentEnd - t) * ScaleAt(t);
                t = segmentEnd;
            }
            ExpireUpTo(end);

            double total = scaled + _remainder;
            long whole = (long)Math.Floor(total);
            _remainder = total - whole;

            RealDelta = end - start;
            ScaledDelta = whole;
            RealNow = end;
            ScaledNow += whole;
        }

        /// <inheritdoc />
        public ScaleHandle PushScale(double scale, object owner, Func<bool> isAlive, long? durationRealTicks = null)
        {
            if (double.IsNaN(scale) || scale < 0.0 || scale > MaxScale)
            {
                _log.Error("FrameClock.BadScale", $"Scale {scale} outside [0, {MaxScale}]; refused.");
                return default;
            }

            for (int i = 0; i < Capacity; i++)
            {
                if (_entries[i].Active) continue;
                ref Entry e = ref _entries[i];
                e.Active = true;
                e.Generation++;
                e.Scale = scale;
                e.Owner = owner;
                e.IsAlive = isAlive;
                e.ExpireAt = durationRealTicks.HasValue ? RealNow + Math.Max(0L, durationRealTicks.Value) : long.MaxValue;
                return new ScaleHandle(this, i, e.Generation);
            }

            _log.Error("FrameClock.Full", $"No free scale slot (capacity {Capacity}); refused.");
            return default;
        }

        /// <inheritdoc />
        public bool PopScale(ScaleHandle handle)
        {
            if (handle.Clock != this || handle.Slot < 0 || handle.Slot >= Capacity)
            {
                _log.Warn("FrameClock.ForeignHandle", "PopScale with an invalid or foreign handle ignored.");
                return false;
            }

            ref Entry e = ref _entries[handle.Slot];
            if (!e.Active || e.Generation != handle.Generation)
            {
                _log.Warn("FrameClock.StaleHandle", "PopScale with a stale handle ignored.");
                return false;
            }

            Release(ref e);
            return true;
        }

        private double ScaleAt(long t)
        {
            double min = 1.0;
            bool any = false;
            for (int i = 0; i < Capacity; i++)
            {
                ref Entry e = ref _entries[i];
                if (!e.Active || e.ExpireAt <= t) continue;
                if (!any || e.Scale < min)
                {
                    min = e.Scale;
                    any = true;
                }
            }
            return any ? min : 1.0;
        }

        private long NextExpiryAfter(long t)
        {
            long next = long.MaxValue;
            for (int i = 0; i < Capacity; i++)
            {
                ref Entry e = ref _entries[i];
                if (e.Active && e.ExpireAt > t && e.ExpireAt < next) next = e.ExpireAt;
            }
            return next;
        }

        private void ExpireUpTo(long t)
        {
            for (int i = 0; i < Capacity; i++)
            {
                ref Entry e = ref _entries[i];
                if (e.Active && e.ExpireAt <= t) Release(ref e);
            }
        }

        private void RemoveDeadOwners()
        {
            for (int i = 0; i < Capacity; i++)
            {
                ref Entry e = ref _entries[i];
                if (!e.Active || e.IsAlive == null || e.IsAlive()) continue;
                _log.Error("FrameClock.DeadOwner", $"Scale handle owned by a dead owner ({e.Owner}) removed.");
                Release(ref e);
            }
        }

        private static void Release(ref Entry e)
        {
            e.Active = false;
            e.Owner = null;
            e.IsAlive = null;
        }
    }
}

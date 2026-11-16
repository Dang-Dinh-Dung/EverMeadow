using System;

namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Fixed-capacity ring of button events (GDD Input Rule 5–6, F1). Allocates once; zero garbage per frame.
    /// Selection returns the oldest eligible Pressed record (one press, one effect — Rule 8).
    /// </summary>
    public sealed class InputBuffer
    {
        private readonly InputRecord[] _ring;
        private readonly long[] _lastReleaseSeq = new long[(int)InputActionId.Count];
        private readonly long _retention;
        private int _head;   // index of the oldest record
        private int _count;
        private long _nextSeq = 1;

        /// <summary>Creates a buffer of <paramref name="capacity"/> records with real-time retention <paramref name="retentionTicks"/>.</summary>
        public InputBuffer(int capacity, long retentionTicks)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _ring = new InputRecord[capacity];
            _retention = retentionTicks;
            for (int i = 0; i < _lastReleaseSeq.Length; i++) _lastReleaseSeq[i] = -1;
        }

        /// <summary>Capacity of the ring.</summary>
        public int Capacity => _ring.Length;

        /// <summary>Records currently stored (including consumed and expired ones not yet overwritten).</summary>
        public int Count => _count;

        /// <summary>Sequence number the next record will get.</summary>
        public long NextSeq => _nextSeq;

        /// <summary>Records (either phase) overwritten while still unconsumed and inside retention (telemetry; non-zero means capacity is too small).</summary>
        public long OverwrittenBeforeExpiry { get; private set; }

        /// <summary>Appends a record, assigning its sequence number. Returns that number.</summary>
        public long Add(InputRecord record, long realNow)
        {
            record.Seq = _nextSeq++;
            if (_count == _ring.Length)
            {
                ref InputRecord oldest = ref _ring[_head];
                if (!oldest.Consumed && realNow - oldest.TReal <= _retention)
                    OverwrittenBeforeExpiry++;
                _ring[_head] = record;
                _head = (_head + 1) % _ring.Length;
            }
            else
            {
                _ring[(_head + _count) % _ring.Length] = record;
                _count++;
            }

            if (record.Phase == InputPhase.Released) _lastReleaseSeq[(int)record.Action] = record.Seq;
            return record.Seq;
        }

        /// <summary>
        /// Marks <paramref name="action"/> released now without storing a record (device lost, focus lost, latch),
        /// so <see cref="WasReleasedAfter"/> stays correct. Consumes a sequence number.
        /// </summary>
        public void MarkReleased(InputActionId action) => _lastReleaseSeq[(int)action] = _nextSeq++;

        /// <summary>True when <paramref name="action"/> was released after the record numbered <paramref name="seq"/>.</summary>
        public bool WasReleasedAfter(InputActionId action, long seq) => _lastReleaseSeq[(int)action] > seq;

        /// <summary>
        /// Finds the oldest Pressed record that satisfies F1 for <paramref name="action"/> (matching Action or BaseAction),
        /// optionally consuming it.
        /// </summary>
        public bool Find(InputActionId action, long windowTicks, ClockKind clock, long realNow, long scaledNow,
                         long epoch, bool consume, out InputRecord record)
        {
            if (windowTicks < 0) windowTicks = 0;
            for (int i = 0; i < _count; i++)
            {
                ref InputRecord r = ref _ring[(_head + i) % _ring.Length];
                if (r.Phase != InputPhase.Pressed || r.Consumed || r.Epoch != epoch) continue;
                if (r.Action != action && r.BaseAction != action) continue;
                if (realNow - r.TReal > _retention) continue;
                long age = clock == ClockKind.Real ? realNow - r.TReal : scaledNow - r.TScaled;
                if (age > windowTicks) continue;
                if (consume) r.Consumed = true;
                record = r;
                return true;
            }
            record = default;
            return false;
        }

        /// <summary>
        /// Converts the unconsumed Pressed record <paramref name="seq"/> into a chord (Rule 4 "same step"): Action becomes
        /// <paramref name="chord"/>, BaseAction the previous action, and Move is replaced. Returns false if not found.
        /// </summary>
        public bool ConvertToChord(long seq, InputActionId chord, Vec2 move)
        {
            for (int i = 0; i < _count; i++)
            {
                ref InputRecord r = ref _ring[(_head + i) % _ring.Length];
                if (r.Seq != seq) continue;
                if (r.Consumed || r.Phase != InputPhase.Pressed) return false;
                r.BaseAction = r.Action;
                r.Action = chord;
                r.Move = move;
                return true;
            }
            return false;
        }

        /// <summary>Copies stored records oldest-first into <paramref name="destination"/> (test hook H-3). Returns the number copied.</summary>
        public int Snapshot(InputRecord[] destination)
        {
            int n = Math.Min(destination.Length, _count);
            for (int i = 0; i < n; i++) destination[i] = _ring[(_head + i) % _ring.Length];
            return n;
        }
    }
}

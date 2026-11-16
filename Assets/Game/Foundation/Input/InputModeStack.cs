using System;

namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Token stack of input modes (GDD Input Rule 11–12). Gameplay is the permanent, implicit bottom.
    /// Every change of the top handle invokes <c>onTopChanged</c> once (no coalescing). While deferring
    /// (inside Input's own event dispatch) pushes and pops are queued and applied in order on <see cref="EndDefer"/>.
    /// </summary>
    public sealed class InputModeStack
    {
        /// <summary>Maximum simultaneous pushed modes.</summary>
        public const int Capacity = 16;

        private struct Entry
        {
            public bool Used;      // slot holds a live or pending entry
            public bool Active;    // entry is on the stack
            public int Generation;
            public InputMode Mode;
            public object Owner;
            public Func<bool> IsAlive;
        }

        private struct PendingOp
        {
            public bool IsPush;
            public int Slot;
            public int Generation;
        }

        private readonly Entry[] _entries = new Entry[Capacity];
        private readonly int[] _order = new int[Capacity]; // active slots, bottom → top
        private readonly PendingOp[] _pending = new PendingOp[Capacity * 2];
        private readonly Action<ModeHandle, ModeHandle> _onTopChanged; // (oldTop, newTop); default = Gameplay base
        private readonly ILogSink _log;
        private int _orderCount;
        private int _pendingCount;
        private bool _deferring;

        /// <summary>Creates a stack. <paramref name="onTopChanged"/> receives (oldTop, newTop); an invalid handle means the Gameplay base.</summary>
        public InputModeStack(Action<ModeHandle, ModeHandle> onTopChanged, ILogSink log)
        {
            _onTopChanged = onTopChanged;
            _log = log ?? new NullLogSink();
        }

        /// <summary>Mode currently on top.</summary>
        public InputMode TopMode => _orderCount == 0 ? InputMode.Gameplay : _entries[_order[_orderCount - 1]].Mode;

        /// <summary>Slot currently on top, −1 for the Gameplay base.</summary>
        public int TopSlot => _orderCount == 0 ? -1 : _order[_orderCount - 1];

        /// <summary>Handle of the top entry; invalid for the Gameplay base.</summary>
        public ModeHandle TopHandle => HandleOf(TopSlot);

        /// <summary>Number of pushed (active) modes above Gameplay.</summary>
        public int Depth => _orderCount;

        /// <summary>Pushes <paramref name="mode"/>. Gameplay cannot be pushed. Returns an invalid handle when refused.</summary>
        public ModeHandle Push(InputMode mode, object owner, Func<bool> isAlive)
        {
            if (mode == InputMode.Gameplay)
            {
                _log.Warn("Input.PushGameplay", "Gameplay is the permanent bottom and cannot be pushed.");
                return default;
            }

            for (int i = 0; i < Capacity; i++)
            {
                if (_entries[i].Used) continue;
                ref Entry e = ref _entries[i];
                e.Used = true;
                e.Active = false;
                e.Generation++;
                e.Mode = mode;
                e.Owner = owner;
                e.IsAlive = isAlive;
                var handle = new ModeHandle(this, i, e.Generation);
                if (_deferring) Enqueue(true, i, e.Generation);
                else Activate(i);
                return handle;
            }

            _log.Error("Input.StackFull", $"Mode stack full (capacity {Capacity}); push of {mode} refused.");
            return default;
        }

        /// <summary>Pops the entry of <paramref name="handle"/>. Returns false (and warns) for an invalid, stale or foreign handle.</summary>
        public bool Pop(ModeHandle handle)
        {
            if (!IsLive(handle))
            {
                _log.Warn("Input.BadModeHandle", "Pop with an invalid, stale or foreign mode handle ignored.");
                return false;
            }
            if (_deferring) Enqueue(false, handle.Slot, handle.Generation);
            else Remove(handle.Slot);
            return true;
        }

        /// <summary>True when <paramref name="handle"/>'s entry is the top of the stack.</summary>
        public bool IsTop(ModeHandle handle) => IsLive(handle) && _entries[handle.Slot].Active && TopSlot == handle.Slot;

        /// <summary>Handle for the entry in <paramref name="slot"/>, or default.</summary>
        public ModeHandle HandleOf(int slot) =>
            slot >= 0 && slot < Capacity && _entries[slot].Used ? new ModeHandle(this, slot, _entries[slot].Generation) : default;

        /// <summary>Removes entries whose owner is dead (Rule 11) and logs an error for each.</summary>
        public void SweepDead()
        {
            for (int i = 0; i < Capacity; i++)
            {
                ref Entry e = ref _entries[i];
                if (!e.Used || e.IsAlive == null || e.IsAlive()) continue;
                _log.Error("Input.DeadModeOwner", $"Mode {e.Mode} owned by a dead owner ({e.Owner}) removed.");
                if (e.Active) Remove(i);
                else Free(ref e);
            }
        }

        /// <summary>Starts queueing pushes and pops (Input's own event dispatch).</summary>
        public void BeginDefer() => _deferring = true;

        /// <summary>Stops queueing and applies queued operations in order; each top change is its own barrier.</summary>
        public void EndDefer()
        {
            _deferring = false;
            for (int i = 0; i < _pendingCount; i++)
            {
                PendingOp op = _pending[i];
                ref Entry e = ref _entries[op.Slot];
                if (!e.Used || e.Generation != op.Generation) continue;
                if (op.IsPush)
                {
                    if (!e.Active) Activate(op.Slot);
                }
                else if (e.Active) Remove(op.Slot);
                else Free(ref e); // pushed and popped inside the same dispatch
            }
            _pendingCount = 0;
        }

        private bool IsLive(ModeHandle h) =>
            h.Stack == this && h.Slot >= 0 && h.Slot < Capacity && _entries[h.Slot].Used && _entries[h.Slot].Generation == h.Generation;

        private void Enqueue(bool push, int slot, int generation)
        {
            if (_pendingCount >= _pending.Length)
            {
                _log.Error("Input.PendingFull", "Too many deferred mode operations; dropped.");
                return;
            }
            _pending[_pendingCount++] = new PendingOp { IsPush = push, Slot = slot, Generation = generation };
        }

        private void Activate(int slot)
        {
            ModeHandle oldTop = TopHandle;
            _entries[slot].Active = true;
            _order[_orderCount++] = slot;
            _onTopChanged?.Invoke(oldTop, HandleOf(slot));
        }

        private void Remove(int slot)
        {
            int oldTopSlot = TopSlot;
            ModeHandle oldTop = TopHandle;
            int index = -1;
            for (int i = 0; i < _orderCount; i++)
            {
                if (_order[i] == slot) { index = i; break; }
            }
            if (index >= 0)
            {
                for (int i = index; i < _orderCount - 1; i++) _order[i] = _order[i + 1];
                _orderCount--;
            }
            Free(ref _entries[slot]);
            if (TopSlot != oldTopSlot) _onTopChanged?.Invoke(oldTop, TopHandle);
        }

        private static void Free(ref Entry e)
        {
            e.Used = false;
            e.Active = false;
            e.Owner = null;
            e.IsAlive = null;
        }
    }
}

using System;
using System.Collections.Generic;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Engine-free core of the Input system (GDD Input; ADR-0001). One instance per session, created by the composition root.
    /// The frame driver calls <see cref="BeginStep"/>, feeds adapter events through <see cref="IInputEventSink"/>, then calls
    /// <see cref="EndStep"/> before any gameplay <c>Update</c>.
    /// </summary>
    public sealed class InputService : IInputQueries, IInputModes, IInputDevices, IInputEventSink
    {
        /// <summary>Simultaneous physical sources tracked per action (keyboard, mouse, two gamepads).</summary>
        public const int MaxSourcesPerAction = 4;

        private const int MaxPendingChords = 8;

        private struct HeldSource
        {
            public bool Used;
            public bool Latched;
            public int DeviceId;
        }

        private struct PendingChord
        {
            public long Seq;
            public InputActionId Chord;
        }

        private readonly IFrameClock _clock;
        private readonly ILogSink _log;
        private readonly bool _devWarnings;
        private readonly InputConfig _config;
        private readonly long _retention;
        private readonly long _hitstopExtra;
        private readonly InputBuffer _buffer;
        private readonly InputModeStack _stack;
        private readonly ActiveDeviceTracker _tracker;
        private readonly NavRepeater _nav;
        private readonly HeldSource[] _held = new HeldSource[(int)InputActionId.Count * MaxSourcesPerAction];
        private readonly bool[] _chordEnabled = new bool[(int)InputActionId.Count];
        private readonly HoldMode[] _holdMode = new HoldMode[(int)InputActionId.Count];
        private readonly long[] _registeredWindow = new long[(int)InputActionId.Count];
        private readonly bool[] _warnedWindow = new bool[(int)InputActionId.Count];
        private readonly bool[] _toggleOn = new bool[InputModeStack.Capacity];
        private readonly int[] _toggleGen = new int[InputModeStack.Capacity];
        private readonly PendingChord[] _pendingChords = new PendingChord[MaxPendingChords];

        private int _pendingChordCount;
        private Vec2 _move;
        private Vec2 _navigate;
        private int _lastGamepadId = -1;
        private bool _ignorePointerThisStep;
        private bool _stallInit;
        private long _lastScaledNow;
        private long _stallStartReal;
        private bool _stallWarned;

        /// <summary>
        /// Creates the core. <paramref name="config"/> is validated here; each violation is logged as an error.
        /// <paramref name="gamepadConnected"/> picks the start device (Rule 13). <paramref name="devWarnings"/> enables
        /// development-build warnings (Rule 7, 10).
        /// </summary>
        public InputService(IFrameClock clock, InputConfig config, ILogSink log, bool gamepadConnected, bool devWarnings = true)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _log = log ?? new NullLogSink();
            _devWarnings = devWarnings;
            _config = InputConfigValidator.Validate(config, out List<string> violations);
            foreach (string v in violations) _log.Error("Input.Config", v);

            _retention = Ticks.FromSeconds(_config.BufferRetentionSeconds);
            _hitstopExtra = Ticks.FromSeconds(_config.HitstopBudgetSeconds * (1.0 - _config.HitstopMinScale));
            _buffer = new InputBuffer(_config.BufferCapacity, _retention);
            _stack = new InputModeStack(OnTopChanged, _log);
            _tracker = new ActiveDeviceTracker(gamepadConnected ? DeviceKind.Gamepad : DeviceKind.KeyboardMouse, _config);
            _nav = new NavRepeater(_config.NavRepeatDelaySeconds, _config.NavRepeatIntervalSeconds, _config.NavThreshold);
            _chordEnabled[(int)InputActionId.Plunge] = true;
            _chordEnabled[(int)InputActionId.DropThrough] = true;
        }

        /// <inheritdoc />
        public event Action<ModeHandle, bool> TopChanged;

        /// <inheritdoc />
        public event Action<DeviceKind> DeviceChanged;

        /// <inheritdoc />
        public event Action DeviceLost;

        /// <summary>Barrier counter (Rule 12, 17). Starts at 0.</summary>
        public long Epoch { get; private set; }

        /// <summary>Sequence number the next record will get.</summary>
        public long NextSeq => _buffer.NextSeq;

        /// <summary>Mode on top of the stack.</summary>
        public InputMode TopMode => _stack.TopMode;

        /// <summary>Effective config after validation and Settings pushes.</summary>
        public InputConfig Config => _config;

        /// <summary>Records written (telemetry).</summary>
        public long RecordedCount { get; private set; }

        /// <summary>Records consumed (telemetry).</summary>
        public long ConsumedCount { get; private set; }

        /// <summary>Records overwritten before expiry (telemetry; F2).</summary>
        public long OverwrittenBeforeExpiry => _buffer.OverwrittenBeforeExpiry;

        /// <inheritdoc />
        public DeviceKind Active => _tracker.Active;

        // ---------------------------------------------------------------- step

        /// <summary>Starts an input step. Push/Pop from Input event handlers are deferred until <see cref="EndStep"/>.</summary>
        public void BeginStep()
        {
            _tracker.BeginStep();
            _stack.BeginDefer();
            _pendingChordCount = 0;
        }

        /// <summary>
        /// Ends the step: resolves end-of-step chords (Rule 4), updates Navigate repeat, raises <see cref="DeviceChanged"/>,
        /// removes modes of dead owners, applies deferred Push/Pop, checks the frozen-clock warning (Rule 10).
        /// </summary>
        public void EndStep()
        {
            ResolvePendingChords();

            if (ActionCatalog.IsAxisInMode(AxisId.Navigate, _stack.TopMode)) _nav.Update(_navigate, _clock.RealNow);
            else _nav.Reset();

            if (_tracker.EndStep()) DeviceChanged?.Invoke(_tracker.Active);

            _stack.SweepDead();
            _stack.EndDefer();
            CheckFrozenClock();
            _ignorePointerThisStep = false;
        }

        // ---------------------------------------------------------------- queries

        /// <inheritdoc />
        public bool TryConsume(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record)
        {
            if (!Find(action, windowTicks, clock, true, out record)) return false;
            ConsumedCount++;
            return true;
        }

        /// <inheritdoc />
        public bool Peek(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record) =>
            Find(action, windowTicks, clock, false, out record);

        /// <inheritdoc />
        public bool WasReleasedAfter(InputActionId action, long seq)
        {
            if (!ValidAction(action)) return false;
            InputActionId baseAction = BaseOf(action);
            if (baseAction != InputActionId.None && _chordEnabled[(int)action] && _buffer.WasReleasedAfter(baseAction, seq)) return true;
            return _buffer.WasReleasedAfter(action, seq);
        }

        /// <inheritdoc />
        public bool IsHeld(InputActionId action)
        {
            if (!ValidAction(action) || !ActionCatalog.IsInMode(action, _stack.TopMode)) return false;
            if (ActionCatalog.IsHoldAction(action) && _holdMode[(int)action] == HoldMode.Toggle) return ToggleState();
            int b = (int)action * MaxSourcesPerAction;
            for (int i = 0; i < MaxSourcesPerAction; i++)
            {
                if (_held[b + i].Used && !_held[b + i].Latched) return true;
            }
            return false;
        }

        /// <inheritdoc />
        public Vec2 ReadMove() => ActionCatalog.IsAxisInMode(AxisId.Move, _stack.TopMode) ? _move : Vec2.Zero;

        /// <inheritdoc />
        public Vec2 ReadNavigate() => ActionCatalog.IsAxisInMode(AxisId.Navigate, _stack.TopMode) ? _navigate : Vec2.Zero;

        /// <inheritdoc />
        public bool TryGetNavigateStep(out Vec2 direction)
        {
            direction = ActionCatalog.IsAxisInMode(AxisId.Navigate, _stack.TopMode) ? _nav.Step : Vec2.Zero;
            return direction.X != 0f || direction.Y != 0f;
        }

        /// <inheritdoc />
        public void RegisterConsumer(object consumer, InputActionId action, long maxWindowTicks, ClockKind clock)
        {
            if (!ValidAction(action)) return;
            long worst = clock == ClockKind.Real ? maxWindowTicks : maxWindowTicks + _hitstopExtra;
            if (worst > _retention)
            {
                _log.Warn("Input.WindowExceedsRetention",
                    $"{consumer} registered {action} window {Ticks.ToSeconds(maxWindowTicks)} s ({clock}); worst real {Ticks.ToSeconds(worst)} s > retention {_config.BufferRetentionSeconds} s.");
            }
            if (maxWindowTicks > _registeredWindow[(int)action]) _registeredWindow[(int)action] = maxWindowTicks;
        }

        /// <summary>Copies stored records oldest-first (test hook H-3).</summary>
        public int SnapshotBuffer(InputRecord[] destination) => _buffer.Snapshot(destination);

        // ---------------------------------------------------------------- modes

        /// <inheritdoc />
        public ModeHandle Push(InputMode mode, object owner, Func<bool> isAlive) => _stack.Push(mode, owner, isAlive);

        /// <inheritdoc />
        public bool Pop(ModeHandle handle) => _stack.Pop(handle);

        /// <inheritdoc />
        public bool IsTop(ModeHandle handle) => _stack.IsTop(handle);

        /// <summary>
        /// Barrier without a mode change (Rule 17: binding changes). Increments the epoch, latches held buttons, clears Navigate repeat.
        /// </summary>
        public void ApplyBarrier()
        {
            Epoch++;
            for (int i = 0; i < _held.Length; i++)
            {
                if (_held[i].Used) _held[i].Latched = true;
            }
            _nav.Reset();
            _pendingChordCount = 0;
        }

        // ---------------------------------------------------------------- settings

        /// <summary>Settings push (Rule 17): dead zone clamped to the safe range 0.05–0.4.</summary>
        public void SetDeadZone(float value)
        {
            _config.DeadZone = float.IsNaN(value) ? 0.2f : Math.Max(0.05f, Math.Min(0.4f, value));
            _tracker.Configure(_config);
        }

        /// <summary>Settings push (Rule 9). Ignored for non-hold actions. Clears toggle states.</summary>
        public void SetHoldMode(InputActionId action, HoldMode mode)
        {
            if (!ValidAction(action) || !ActionCatalog.IsHoldAction(action)) return;
            _holdMode[(int)action] = mode;
            Array.Clear(_toggleOn, 0, _toggleOn.Length);
        }

        /// <summary>
        /// Chord (true, default) or single-button (false) binding for Plunge / DropThrough (Rule 4). A binding change, so it
        /// raises a barrier (Rule 17).
        /// </summary>
        public void SetChordMode(InputActionId chordAction, bool useChord)
        {
            if (BaseOf(chordAction) == InputActionId.None) return;
            _chordEnabled[(int)chordAction] = useChord;
            ApplyBarrier();
        }

        /// <summary>Registers a button already held when Input starts; it stays latched until released (Rule 15, first frame).</summary>
        public void AddLatchedHold(InputActionId action, int deviceId)
        {
            if (!ValidAction(action)) return;
            int s = FindOrAllocSource(action, deviceId);
            if (s >= 0) _held[s].Latched = true;
        }

        // ---------------------------------------------------------------- adapter events

        /// <inheritdoc />
        public void OnAction(InputActionId action, InputPhase phase, DeviceKind device, int deviceId, bool isPointer)
        {
            if (!ValidAction(action)) return;
            if (phase == InputPhase.Pressed) OnPressed(action, device, deviceId, isPointer);
            else OnReleased(action, device, deviceId);
        }

        /// <inheritdoc />
        public void OnAxisSample(AxisId axis, Vec2 raw, DeviceKind device)
        {
            Vec2 v = InputMath.ApplyDeadZone(raw, _config.DeadZone);
            if (axis == AxisId.Move) _move = v;
            else _navigate = v;
        }

        /// <inheritdoc />
        public void OnDeviceSignal(DeviceKind device, int deviceId, SignalKind kind, float magnitude)
        {
            if (kind == SignalKind.MouseClick && _ignorePointerThisStep) return;
            InputMode top = _stack.TopMode;
            bool mouseCounts = top == InputMode.Menu || top == InputMode.Map;
            if (_tracker.Signal(device, kind, magnitude, mouseCounts, _clock.RealNow) && device == DeviceKind.Gamepad)
                _lastGamepadId = deviceId;
        }

        /// <inheritdoc />
        public void OnDeviceRemoved(DeviceKind device, int deviceId)
        {
            for (int a = 1; a < (int)InputActionId.Count; a++)
            {
                int b = a * MaxSourcesPerAction;
                for (int i = 0; i < MaxSourcesPerAction; i++)
                {
                    ref HeldSource h = ref _held[b + i];
                    if (!h.Used || h.DeviceId != deviceId) continue;
                    h = default;
                    _buffer.MarkReleased((InputActionId)a);
                }
            }

            if (device != DeviceKind.Gamepad) return;
            bool mostRecent = deviceId == _lastGamepadId || (_lastGamepadId < 0 && _tracker.Active == DeviceKind.Gamepad);
            if (!mostRecent) return;
            _lastGamepadId = -1;
            if (_tracker.Active == DeviceKind.Gamepad) _tracker.Force(DeviceKind.KeyboardMouse);
            DeviceLost?.Invoke();
        }

        /// <inheritdoc />
        public void OnFocusChanged(bool focused)
        {
            if (focused)
            {
                _ignorePointerThisStep = true;
                return;
            }

            for (int a = 1; a < (int)InputActionId.Count; a++)
            {
                int b = a * MaxSourcesPerAction;
                bool any = false;
                for (int i = 0; i < MaxSourcesPerAction; i++)
                {
                    ref HeldSource h = ref _held[b + i];
                    if (!h.Used || h.Latched) continue;
                    h.Latched = true;
                    any = true;
                }
                if (any) _buffer.MarkReleased((InputActionId)a);
            }
            _nav.Reset();
        }

        // ---------------------------------------------------------------- internals

        private void OnPressed(InputActionId action, DeviceKind device, int deviceId, bool isPointer)
        {
            int s = FindOrAllocSource(action, deviceId);
            if (s >= 0) _held[s].Latched = false;

            if (isPointer && _ignorePointerThisStep)
            {
                if (s >= 0) _held[s].Latched = true; // the focus-regain click is swallowed, release included
                return;
            }

            if (!ActionCatalog.IsInMode(action, _stack.TopMode)) return; // tracked as held, not recorded

            var record = new InputRecord
            {
                Action = action,
                Phase = InputPhase.Pressed,
                TReal = _clock.RealNow,
                TScaled = _clock.ScaledNow,
                Device = device,
                Epoch = Epoch,
                Move = _move
            };

            InputActionId chord = ActionCatalog.ChordFor(action);
            bool chordActive = chord != InputActionId.None && _chordEnabled[(int)chord] && ActionCatalog.IsInMode(chord, _stack.TopMode);
            if (chordActive && IsDownFor(chord, _move))
            {
                record.BaseAction = action;
                record.Action = chord;
            }

            long seq = _buffer.Add(record, _clock.RealNow);
            RecordedCount++;

            if (chordActive && record.Action == action && _pendingChordCount < MaxPendingChords)
                _pendingChords[_pendingChordCount++] = new PendingChord { Seq = seq, Chord = chord };

            if (ActionCatalog.IsHoldAction(action) && _holdMode[(int)action] == HoldMode.Toggle) FlipToggle();
        }

        private void OnReleased(InputActionId action, DeviceKind device, int deviceId)
        {
            bool latched;
            int s = FindSource(action, deviceId);
            if (s >= 0)
            {
                latched = _held[s].Latched;
                _held[s] = default;
            }
            else if (!ReleaseAllSources(action, out latched))
            {
                return; // never seen pressed (e.g. pressed before this instance and not registered)
            }

            if (latched || !ActionCatalog.IsInMode(action, _stack.TopMode))
            {
                _buffer.MarkReleased(action);
                return;
            }

            _buffer.Add(new InputRecord
            {
                Action = action,
                Phase = InputPhase.Released,
                TReal = _clock.RealNow,
                TScaled = _clock.ScaledNow,
                Device = device,
                Epoch = Epoch,
                Move = _move
            }, _clock.RealNow);
            RecordedCount++;
        }

        private bool Find(InputActionId action, long windowTicks, ClockKind clock, bool consume, out InputRecord record)
        {
            record = default;
            if (!ValidAction(action) || !ActionCatalog.IsInMode(action, _stack.TopMode)) return false;
            if (windowTicks < 0) windowTicks = 0;
            WarnWindowOnce(action, windowTicks);
            return _buffer.Find(action, windowTicks, clock, _clock.RealNow, _clock.ScaledNow, Epoch, consume, out record);
        }

        private void WarnWindowOnce(InputActionId action, long windowTicks)
        {
            if (!_devWarnings || _warnedWindow[(int)action]) return;
            long registered = _registeredWindow[(int)action];
            if (windowTicks <= _retention && (registered == 0 || windowTicks <= registered)) return;
            _warnedWindow[(int)action] = true;
            _log.Warn("Input.WindowOverRegistered",
                $"{action} queried with window {Ticks.ToSeconds(windowTicks)} s above its registered window or retention; capped by retention.");
        }

        private void ResolvePendingChords()
        {
            for (int i = 0; i < _pendingChordCount; i++)
            {
                PendingChord p = _pendingChords[i];
                if (IsDownFor(p.Chord, _move)) _buffer.ConvertToChord(p.Seq, p.Chord, _move);
            }
            _pendingChordCount = 0;
        }

        private bool IsDownFor(InputActionId chord, Vec2 move)
        {
            float angle = chord == InputActionId.Plunge ? _config.DownAnglePlungeDegrees : _config.DownAngleDropDegrees;
            return InputMath.IsDown(move, _config.DownThreshold, angle);
        }

        private void OnTopChanged(ModeHandle oldTop, ModeHandle newTop)
        {
            ApplyBarrier();
            if (oldTop.IsValid) TopChanged?.Invoke(oldTop, false);
            if (newTop.IsValid) TopChanged?.Invoke(newTop, true);
        }

        private bool ToggleState()
        {
            int slot = _stack.TopSlot;
            if (slot < 0) return false;
            return _toggleOn[slot] && _toggleGen[slot] == _stack.TopHandle.Generation;
        }

        private void FlipToggle()
        {
            int slot = _stack.TopSlot;
            if (slot < 0) return;
            int gen = _stack.TopHandle.Generation;
            if (_toggleGen[slot] != gen)
            {
                _toggleGen[slot] = gen;
                _toggleOn[slot] = false;
            }
            _toggleOn[slot] = !_toggleOn[slot];
        }

        private void CheckFrozenClock()
        {
            long realNow = _clock.RealNow;
            long scaledNow = _clock.ScaledNow;
            if (!_stallInit || scaledNow != _lastScaledNow || _stack.TopMode != InputMode.Gameplay)
            {
                _stallInit = true;
                _lastScaledNow = scaledNow;
                _stallStartReal = realNow;
                _stallWarned = false;
                return;
            }
            if (_devWarnings && !_stallWarned && realNow - _stallStartReal > _retention)
            {
                _stallWarned = true;
                _log.Warn("Input.FrozenClock",
                    "Scaled clock frozen longer than buffer retention while Gameplay is on top; a pause must push Menu or Locked (Rule 10).");
            }
        }

        private int FindSource(InputActionId action, int deviceId)
        {
            int b = (int)action * MaxSourcesPerAction;
            for (int i = 0; i < MaxSourcesPerAction; i++)
            {
                if (_held[b + i].Used && _held[b + i].DeviceId == deviceId) return b + i;
            }
            return -1;
        }

        // A Button action is canceled only when none of its controls is actuated, and it reports the last control, which can
        // belong to another device than the one that performed it. Such a release frees every source of the action.
        private bool ReleaseAllSources(InputActionId action, out bool allLatched)
        {
            int b = (int)action * MaxSourcesPerAction;
            bool any = false;
            allLatched = true;
            for (int i = 0; i < MaxSourcesPerAction; i++)
            {
                if (!_held[b + i].Used) continue;
                any = true;
                if (!_held[b + i].Latched) allLatched = false;
                _held[b + i] = default;
            }
            return any;
        }

        private int FindOrAllocSource(InputActionId action, int deviceId)
        {
            int s = FindSource(action, deviceId);
            if (s >= 0) return s;
            int b = (int)action * MaxSourcesPerAction;
            for (int i = 0; i < MaxSourcesPerAction; i++)
            {
                if (_held[b + i].Used) continue;
                _held[b + i] = new HeldSource { Used = true, DeviceId = deviceId };
                return b + i;
            }
            _log.Warn("Input.TooManySources", $"More than {MaxSourcesPerAction} devices hold {action}; extra source not tracked.");
            return -1;
        }

        private static InputActionId BaseOf(InputActionId chord)
        {
            switch (chord)
            {
                case InputActionId.Plunge: return InputActionId.Attack;
                case InputActionId.DropThrough: return InputActionId.Jump;
                default: return InputActionId.None;
            }
        }

        private static bool ValidAction(InputActionId action) => action > InputActionId.None && action < InputActionId.Count;
    }
}

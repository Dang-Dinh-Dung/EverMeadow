using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>Final state of one rebind request (GDD Input Rule 17).</summary>
    public enum RebindStatus
    {
        /// <summary>The new key is applied.</summary>
        Applied,
        /// <summary>Another action uses the key; call <see cref="RebindController.CommitRebind"/>.</summary>
        Conflict,
        /// <summary>Refused by the rules; see <see cref="RebindResult.Outcome"/>.</summary>
        Rejected,
        /// <summary>Cancelled; see <see cref="RebindResult.CancelReason"/>.</summary>
        Cancelled
    }

    /// <summary>Why a rebind was cancelled.</summary>
    public enum RebindCancelReason
    {
        /// <summary>Not cancelled.</summary>
        None,
        /// <summary>Esc during the rebind, or Settings cancelled.</summary>
        User,
        /// <summary>The top of the mode stack changed.</summary>
        ModeChanged,
        /// <summary>The gamepad of the slot was removed.</summary>
        DeviceLost
    }

    /// <summary>Result reported to Settings.</summary>
    public readonly struct RebindResult
    {
        /// <summary>Status.</summary>
        public readonly RebindStatus Status;
        /// <summary>Rule outcome (Apply, Conflict or a rejection).</summary>
        public readonly RebindOutcome Outcome;
        /// <summary>Cancel reason.</summary>
        public readonly RebindCancelReason CancelReason;
        /// <summary>Actions in conflict (Status = Conflict).</summary>
        public readonly IReadOnlyList<InputActionId> Conflicts;

        /// <summary>Creates a result.</summary>
        public RebindResult(RebindStatus status, RebindOutcome outcome, RebindCancelReason reason, IReadOnlyList<InputActionId> conflicts)
        {
            Status = status;
            Outcome = outcome;
            CancelReason = reason;
            Conflicts = conflicts ?? Array.Empty<InputActionId>();
        }
    }

    /// <summary>A binding slot as Settings shows it.</summary>
    public readonly struct RebindSlotView
    {
        /// <summary>Slot index for <see cref="RebindController.StartRebindSlot"/> (stable while the asset is unchanged).</summary>
        public readonly int Slot;
        /// <summary>Effective control path; empty when unbound.</summary>
        public readonly string Path;
        /// <summary>Key name to show ("Space", "A", "—" when unbound).</summary>
        public readonly string Label;
        /// <summary>Fixed Pause/Cancel slot (Esc, Menu/Options): shown locked, never rebound.</summary>
        public readonly bool Fixed;

        /// <summary>Creates a view.</summary>
        public RebindSlotView(int slot, string path, string label, bool isFixed)
        {
            Slot = slot;
            Path = path;
            Label = label;
            Fixed = isFixed;
        }
    }

    /// <summary>
    /// Rebinding (GDD Input Rule 17; ADR-0001 Decision 6). The action being rebound is disabled for the interactive operation —
    /// the only exception to "maps stay enabled" — and every finish re-enables it and raises an Input barrier, which also latches
    /// the key used to choose. Overrides are the Input System's JSON keyed by binding GUID (stable ids in the asset);
    /// <see cref="ApplyBindingOverrides"/> runs the Rule 17 load checks. Plunge and DropThrough use their chord until a key of
    /// their own is bound (Rule 4).
    /// </summary>
    public sealed class RebindController : IDisposable
    {
        private const string KeyboardGroup = "Keyboard&Mouse";
        private const string GamepadGroup = "Gamepad";

        private struct SlotRef
        {
            public InputAction Action;
            public int BindingIndex;
        }

        private readonly UnityInputAdapter _adapter;
        private readonly InputService _input;
        private readonly ILogSink _log;
        private readonly List<BindingSlot> _slots = new List<BindingSlot>();
        private readonly List<SlotRef> _refs = new List<SlotRef>();
        private readonly List<int> _conflicts = new List<int>();

        private InputActionRebindingExtensions.RebindingOperation _op;
        private InputAction _action;
        private int _bindingIndex;
        private int _slotIndex;
        private string _pendingPath;
        private bool _awaitingCommit;
        private Action<RebindResult> _onResult;

        /// <summary>Creates the controller; it follows the top of the mode stack and device loss to cancel rebinds.</summary>
        public RebindController(UnityInputAdapter adapter, InputService input, ILogSink log)
        {
            _adapter = adapter;
            _input = input;
            _log = log ?? new NullLogSink();
            _input.TopChanged += OnTopChanged;
            _input.DeviceLost += OnDeviceLost;
        }

        /// <summary>True while a rebind is listening or waiting for <see cref="CommitRebind"/>.</summary>
        public bool IsRebinding => _action != null;

        /// <summary>The key chosen while waiting for <see cref="CommitRebind"/> (shown in the conflict question), else null.</summary>
        public string PendingPath => _awaitingCommit ? _pendingPath : null;

        /// <summary>Current slots of the runtime asset, in the rules' model (test hook).</summary>
        public IReadOnlyList<BindingSlot> BuildSlots()
        {
            RebuildSlots();
            return _slots;
        }

        /// <summary>Slots of button action <paramref name="action"/> on <paramref name="device"/>, in asset order (primary first).</summary>
        public void GetSlots(InputActionId action, DeviceKind device, List<RebindSlotView> into) =>
            CollectSlots(_adapter.ActionFor(action), null, device, into);

        /// <summary>Slots of one direction (<paramref name="part"/>: up, down, left, right) of <paramref name="axis"/> on <paramref name="device"/>.</summary>
        public void GetAxisSlots(AxisId axis, string part, DeviceKind device, List<RebindSlotView> into) =>
            CollectSlots(_adapter.Asset.FindAction(axis == AxisId.Move ? ActionIds.Move : ActionIds.Navigate), part, device, into);

        /// <summary>Name to show for <paramref name="path"/> ("—" when unbound); gamepad buttons use Xbox names.</summary>
        public static string DisplayName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "—";
            if (path.StartsWith("<Gamepad>")) return GlyphNames.Gamepad(path.Substring(path.LastIndexOf('/') + 1), GlyphFamily.Xbox);
            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>Like <see cref="StartRebind"/> for slot <paramref name="slot"/> (from <see cref="GetSlots"/> / <see cref="GetAxisSlots"/>).</summary>
        public void StartRebindSlot(int slot, Action<RebindResult> onResult)
        {
            RebuildSlots();
            if (slot < 0 || slot >= _refs.Count)
            {
                _log.Warn("Input.RebindBadSlot", $"No binding slot {slot}.");
                onResult?.Invoke(new RebindResult(RebindStatus.Rejected, RebindOutcome.RejectedFixedSlot, RebindCancelReason.None, null));
                return;
            }
            StartRebindAt(_refs[slot].Action, _refs[slot].BindingIndex, onResult);
        }

        /// <summary>
        /// Listens for a new key for binding <paramref name="bindingIndex"/> of <paramref name="action"/>. Esc cancels.
        /// <paramref name="onResult"/> receives Applied, Conflict, Rejected or Cancelled.
        /// </summary>
        public void StartRebind(InputActionId action, int bindingIndex, Action<RebindResult> onResult) =>
            StartRebindAt(_adapter.ActionFor(action), bindingIndex, onResult);

        private void StartRebindAt(InputAction a, int bindingIndex, Action<RebindResult> onResult)
        {
            if (IsRebinding) Finish(RebindStatus.Cancelled, RebindOutcome.Apply, RebindCancelReason.User, null);

            if (a == null || bindingIndex < 0 || bindingIndex >= a.bindings.Count || a.bindings[bindingIndex].isComposite)
            {
                _log.Warn("Input.RebindBadSlot", $"No rebindable binding {bindingIndex} on {a?.name}.");
                onResult?.Invoke(new RebindResult(RebindStatus.Rejected, RebindOutcome.RejectedFixedSlot, RebindCancelReason.None, null));
                return;
            }

            RebuildSlots();
            _slotIndex = IndexOfSlot(a, bindingIndex);
            if (_slots[_slotIndex].Fixed)
            {
                onResult?.Invoke(new RebindResult(RebindStatus.Rejected, RebindOutcome.RejectedFixedSlot, RebindCancelReason.None, null));
                return;
            }

            _action = a;
            _bindingIndex = bindingIndex;
            _onResult = onResult;
            _pendingPath = null;
            _awaitingCommit = false;

            bool gamepadSlot = _slots[_slotIndex].Device == DeviceKind.Gamepad;
            a.Disable();
            // Esc cancels a keyboard rebind, Menu/Options a gamepad one: both are fixed Pause/Cancel keys no action can take.
            _op = a.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough(gamepadSlot ? "<Gamepad>/start" : "<Keyboard>/escape")
                .WithMatchingEventsBeingSuppressed()
                .OnApplyBinding((op, path) => _pendingPath = path)
                .OnComplete(_ => OnListenComplete())
                .OnCancel(_ => Finish(RebindStatus.Cancelled, RebindOutcome.Apply, RebindCancelReason.User, null));
            if (gamepadSlot) _op.WithControlsHavingToMatchPath("<Gamepad>");
            else
            {
                _op.WithControlsHavingToMatchPath("<Keyboard>");
                _op.WithControlsHavingToMatchPath("<Mouse>");
                _op.WithControlsExcluding("<Mouse>/position").WithControlsExcluding("<Mouse>/delta");
            }
            _op.Start();
        }

        /// <summary>Answers a Conflict: Swap (true) gives the conflicting slots the old key; Cancel (false) changes nothing.</summary>
        public void CommitRebind(bool swap)
        {
            if (!_awaitingCommit) return;
            if (!swap)
            {
                Finish(RebindStatus.Cancelled, RebindOutcome.Conflict, RebindCancelReason.User, null);
                return;
            }

            string oldPath = _slots[_slotIndex].Path;
            RebindOutcome outcome = RebindRules.EvaluateSwap(_slots, _slotIndex, _pendingPath, _conflicts);
            if (outcome != RebindOutcome.Apply)
            {
                Finish(RebindStatus.Rejected, outcome, RebindCancelReason.None, null);
                return;
            }
            _action.ApplyBindingOverride(_bindingIndex, _pendingPath);
            foreach (int c in _conflicts)
            {
                SlotRef r = _refs[c];
                r.Action.ApplyBindingOverride(r.BindingIndex, oldPath);
            }
            Finish(RebindStatus.Applied, RebindOutcome.Apply, RebindCancelReason.None, null);
        }

        /// <summary>Cancels a running rebind.</summary>
        public void CancelRebind()
        {
            if (!IsRebinding) return;
            Finish(RebindStatus.Cancelled, RebindOutcome.Apply, RebindCancelReason.User, null);
        }

        /// <summary>Opaque override data for Settings to store (format owned by the Input System; keyed by binding GUID).</summary>
        public string ExportBindingOverrides() => _adapter.Asset.SaveBindingOverridesAsJson();

        /// <summary>
        /// Applies stored overrides and raises a barrier, with the Rule 17 load checks: unreadable data → every default and an
        /// error; a key taken from the fixed Pause/Cancel slots or shared by two actions of a mode → those actions back to
        /// defaults. Returns false when anything was reset (Settings should save again).
        /// </summary>
        public bool ApplyBindingOverrides(string data)
        {
            bool clean = true;
            _adapter.Asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    _adapter.Asset.LoadBindingOverridesFromJson(data);
                }
                catch (Exception e)
                {
                    _log.Error("Input.BindingsUnreadable", $"Saved key bindings could not be read; using defaults. {e.Message}");
                    _adapter.Asset.RemoveAllBindingOverrides();
                    clean = false;
                }
            }

            RebuildSlots();
            var reset = new HashSet<InputAction>();
            for (int i = 0; i < _slots.Count; i++)
            {
                BindingSlot s = _slots[i];
                bool stolenFixedKey = RebindRules.IsFixedKey(s.Path) && s.Action != InputActionId.Pause && s.Action != InputActionId.Cancel;
                if (stolenFixedKey) reset.Add(_refs[i].Action);
            }
            var pairs = new List<(int a, int b)>();
            RebindRules.FindConflicts(_slots, pairs);
            foreach ((int a, int b) in pairs)
            {
                reset.Add(_refs[a].Action);
                reset.Add(_refs[b].Action);
            }
            foreach (InputAction a in reset)
            {
                _log.Error("Input.BindingsInvalid", $"Saved keys of {a.name} conflict or use a reserved key; {a.name} back to defaults.");
                a.RemoveAllBindingOverrides();
                clean = false;
            }

            SyncChordModes();
            _input.ApplyBarrier();
            return clean;
        }

        /// <summary>Restores defaults for <paramref name="action"/> (None = every action) and raises a barrier.</summary>
        public void ResetBindings(InputActionId action = InputActionId.None)
        {
            if (action == InputActionId.None) _adapter.Asset.RemoveAllBindingOverrides();
            else _adapter.ActionFor(action)?.RemoveAllBindingOverrides();
            SyncChordModes();
            _input.ApplyBarrier();
        }

        /// <summary>Cancels a running rebind and unsubscribes.</summary>
        public void Dispose()
        {
            CancelRebind();
            _input.TopChanged -= OnTopChanged;
            _input.DeviceLost -= OnDeviceLost;
        }

        private void OnListenComplete()
        {
            string path = _pendingPath ?? string.Empty;
            RebindOutcome outcome = RebindRules.Evaluate(_slots, _slotIndex, path, _conflicts);
            switch (outcome)
            {
                case RebindOutcome.Apply:
                    _action.ApplyBindingOverride(_bindingIndex, path);
                    Finish(RebindStatus.Applied, outcome, RebindCancelReason.None, null);
                    break;
                case RebindOutcome.Conflict:
                    DisposeOperation();
                    _awaitingCommit = true;
                    var actions = new List<InputActionId>(_conflicts.Count);
                    foreach (int c in _conflicts) actions.Add(_adapter.IdOf(_refs[c].Action));
                    _onResult?.Invoke(new RebindResult(RebindStatus.Conflict, outcome, RebindCancelReason.None, actions));
                    break;
                default:
                    Finish(RebindStatus.Rejected, outcome, RebindCancelReason.None, null);
                    break;
            }
        }

        private void Finish(RebindStatus status, RebindOutcome outcome, RebindCancelReason reason, IReadOnlyList<InputActionId> conflicts)
        {
            InputAction action = _action;
            Action<RebindResult> callback = _onResult;
            DisposeOperation();
            _action = null;
            _onResult = null;
            _awaitingCommit = false;
            if (action != null && !action.enabled) action.Enable();
            SyncChordModes();
            _input.ApplyBarrier(); // Rule 17: binding changes are barriers; also latches the key used to choose
            callback?.Invoke(new RebindResult(status, outcome, reason, conflicts));
        }

        private void DisposeOperation()
        {
            _op?.Dispose();
            _op = null;
        }

        private void OnTopChanged(ModeHandle handle, bool top)
        {
            if (IsRebinding) Finish(RebindStatus.Cancelled, RebindOutcome.Apply, RebindCancelReason.ModeChanged, null);
        }

        private void OnDeviceLost()
        {
            if (IsRebinding && _slots[_slotIndex].Device == DeviceKind.Gamepad)
                Finish(RebindStatus.Cancelled, RebindOutcome.Apply, RebindCancelReason.DeviceLost, null);
        }

        // Rule 4: Plunge / DropThrough are chords ("Down + Attack / Jump") until the player binds a key of their own.
        private void SyncChordModes()
        {
            foreach (InputActionId chord in new[] { InputActionId.Plunge, InputActionId.DropThrough })
            {
                InputAction a = _adapter.ActionFor(chord);
                if (a == null) continue;
                bool bound = false;
                foreach (InputBinding b in a.bindings) bound |= !b.isComposite && !string.IsNullOrEmpty(b.effectivePath);
                _input.SetChordMode(chord, !bound);
            }
        }

        private void CollectSlots(InputAction action, string part, DeviceKind device, List<RebindSlotView> into)
        {
            into.Clear();
            if (action == null) return;
            RebuildSlots();
            for (int i = 0; i < _refs.Count; i++)
            {
                if (_refs[i].Action != action || _slots[i].Device != device) continue;
                InputBinding b = action.bindings[_refs[i].BindingIndex];
                if (part != null && !(b.isPartOfComposite && string.Equals(b.name, part, StringComparison.OrdinalIgnoreCase))) continue;
                if (part == null && b.isPartOfComposite) continue;
                into.Add(new RebindSlotView(i, _slots[i].Path, DisplayName(_slots[i].Path), _slots[i].Fixed));
            }
        }

        private int IndexOfSlot(InputAction action, int bindingIndex)
        {
            for (int i = 0; i < _refs.Count; i++)
            {
                if (_refs[i].Action == action && _refs[i].BindingIndex == bindingIndex) return i;
            }
            return -1;
        }

        private void RebuildSlots()
        {
            _slots.Clear();
            _refs.Clear();
            for (var id = InputActionId.Jump; id < InputActionId.Count; id++)
                AddSlots(_adapter.ActionFor(id), id, ActionCatalog.ModeMask(id), id == InputActionId.Confirm || id == InputActionId.Cancel);
            AddSlots(_adapter.Asset.FindAction(ActionIds.Move), InputActionId.None, ActionCatalog.AxisModeMask(AxisId.Move), false);
            AddSlots(_adapter.Asset.FindAction(ActionIds.Navigate), InputActionId.None, ActionCatalog.AxisModeMask(AxisId.Navigate), true);
        }

        private void AddSlots(InputAction action, InputActionId id, int modes, bool menuEssential)
        {
            if (action == null) return;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding b = action.bindings[i];
                if (b.isComposite) continue;
                bool fixedSlot = (id == InputActionId.Pause || id == InputActionId.Cancel) && RebindRules.IsFixedKey(b.path);
                _slots.Add(new BindingSlot
                {
                    Key = b.isPartOfComposite ? action.name + "/" + b.name : action.name,
                    Action = id,
                    Modes = modes,
                    Device = b.groups != null && b.groups.Contains(GamepadGroup) ? DeviceKind.Gamepad : DeviceKind.KeyboardMouse,
                    Path = b.effectivePath ?? string.Empty,
                    Fixed = fixedSlot,
                    MenuEssential = menuEssential
                });
                _refs.Add(new SlotRef { Action = action, BindingIndex = i });
            }
        }
    }
}

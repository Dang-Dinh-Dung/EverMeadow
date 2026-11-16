using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Object = UnityEngine.Object;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>
    /// Translates Input System events into core events (ADR-0001 Decision 1, 3, 4). Owns a runtime clone of the action asset so
    /// overrides never touch the source asset. Buttons forward <c>performed</c> (press) and <c>canceled</c> (release); <c>started</c>
    /// is ignored. Axes are read once per frame by <see cref="SampleFrame"/>. No gameplay rules live here.
    /// </summary>
    public sealed class UnityInputAdapter : IDisposable
    {
        private readonly InputActionAsset _asset;
        private readonly IInputEventSink _sink;
        private readonly ILogSink _log;
        private readonly InputAction[] _actions = new InputAction[(int)InputActionId.Count];
        private readonly Dictionary<InputAction, InputActionId> _ids = new Dictionary<InputAction, InputActionId>();
        private readonly InputAction _move;
        private readonly InputAction _navigate;
        private readonly Action<InputAction.CallbackContext> _onPerformed;
        private readonly Action<InputAction.CallbackContext> _onCanceled;
        private readonly IDisposable _anyButton;
        private bool _disposed;

        /// <summary>Clones <paramref name="source"/>, subscribes to engine events and enables every action map (maps stay enabled).</summary>
        public UnityInputAdapter(InputActionAsset source, IInputEventSink sink, ILogSink log)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _log = log ?? new NullLogSink();
            _asset = Object.Instantiate(source);
            _asset.name = source.name + " (runtime)";

            _onPerformed = ctx => Forward(ctx, InputPhase.Pressed);
            _onCanceled = ctx => Forward(ctx, InputPhase.Released);

            for (var id = InputActionId.Jump; id < InputActionId.Count; id++)
            {
                InputAction action = _asset.FindAction(ActionIds.For(id));
                if (action == null)
                {
                    _log.Error("Input.AssetMismatch", $"Action {id} ({ActionIds.For(id)}) missing from {source.name}.");
                    continue;
                }
                _actions[(int)id] = action;
                _ids[action] = id;
                action.performed += _onPerformed;
                action.canceled += _onCanceled;
            }

            _move = _asset.FindAction(ActionIds.Move);
            _navigate = _asset.FindAction(ActionIds.Navigate);
            if (_move == null || _navigate == null) _log.Error("Input.AssetMismatch", $"Move or Navigate missing from {source.name}.");

            InputSystem.onDeviceChange += OnDeviceChange;
            Application.focusChanged += OnFocusChanged;
            _anyButton = InputSystem.onAnyButtonPress.Call(OnAnyButton);
            _asset.Enable();
        }

        /// <summary>The runtime clone (rebinding and glyphs read it).</summary>
        public InputActionAsset Asset => _asset;

        /// <summary>True when at least one gamepad is connected (start device, Rule 13).</summary>
        public static bool IsGamepadConnected => Gamepad.all.Count > 0;

        /// <summary>Input action for <paramref name="id"/>, or null.</summary>
        public InputAction ActionFor(InputActionId id) =>
            id > InputActionId.None && id < InputActionId.Count ? _actions[(int)id] : null;

        /// <summary>Core id of <paramref name="action"/>, or None (Move, Navigate, Point).</summary>
        public InputActionId IdOf(InputAction action) =>
            action != null && _ids.TryGetValue(action, out InputActionId id) ? id : InputActionId.None;

        /// <summary>Reports buttons already held when Input starts, so they stay latched until released (Rule 15).</summary>
        public void ReportHeldButtons(Action<InputActionId, int> addLatchedHold)
        {
            for (var id = InputActionId.Jump; id < InputActionId.Count; id++)
            {
                InputAction a = _actions[(int)id];
                if (a == null || !a.IsPressed()) continue;
                addLatchedHold(id, a.activeControl?.device.deviceId ?? -1);
            }
        }

        /// <summary>
        /// Frame step 3 (ADR-0001 Decision 3): samples Move and Navigate and the analog device-switch signals. Called by
        /// <see cref="FrameDriver"/> after <c>InputSystem.Update()</c>.
        /// </summary>
        public void SampleFrame()
        {
            if (_disposed) return;
            if (_move != null) SampleAxis(AxisId.Move, _move);
            if (_navigate != null) SampleAxis(AxisId.Navigate, _navigate);

            ReadOnlyArray<Gamepad> pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                Gamepad pad = pads[i];
                float stick = Mathf.Max(pad.leftStick.ReadValue().magnitude, pad.rightStick.ReadValue().magnitude);
                if (stick > 0f) _sink.OnDeviceSignal(DeviceKind.Gamepad, pad.deviceId, SignalKind.Stick, stick);
                float trigger = Mathf.Max(pad.leftTrigger.ReadValue(), pad.rightTrigger.ReadValue());
                if (trigger > 0f) _sink.OnDeviceSignal(DeviceKind.Gamepad, pad.deviceId, SignalKind.Trigger, trigger);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float pixels = mouse.delta.ReadValue().magnitude * 1080f / Mathf.Max(1, Screen.height);
                if (pixels > 0f) _sink.OnDeviceSignal(DeviceKind.KeyboardMouse, mouse.deviceId, SignalKind.MouseMove, pixels);
            }
        }

        /// <summary>Unsubscribes everything, disables the actions and destroys the clone (Rule 18).</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            InputSystem.onDeviceChange -= OnDeviceChange;
            Application.focusChanged -= OnFocusChanged;
            _anyButton?.Dispose();
            foreach (KeyValuePair<InputAction, InputActionId> pair in _ids)
            {
                pair.Key.performed -= _onPerformed;
                pair.Key.canceled -= _onCanceled;
            }
            _asset.Disable();
            Object.DestroyImmediate(_asset); // a runtime clone, not a scene object: free it now so nothing outlives Dispose
        }

        private void SampleAxis(AxisId axis, InputAction action)
        {
            Vector2 v = action.ReadValue<Vector2>();
            _sink.OnAxisSample(axis, new Vec2(v.x, v.y), KindOf(action.activeControl?.device));
        }

        private void Forward(InputAction.CallbackContext ctx, InputPhase phase)
        {
            if (!_ids.TryGetValue(ctx.action, out InputActionId id)) return;
            InputDevice device = ctx.control?.device;
            // Removing a device cancels its actions before onDeviceChange fires (V3). That release is handled as a device loss
            // (latch, no Released record — GDD Input Rule 15), so the cancel itself is not forwarded.
            if (phase == InputPhase.Released && device != null && !device.added) return;
            _sink.OnAction(id, phase, KindOf(device), device?.deviceId ?? -1, device is Pointer);
        }

        private void OnAnyButton(InputControl control)
        {
            InputDevice device = control.device;
            if (device is Gamepad) _sink.OnDeviceSignal(DeviceKind.Gamepad, device.deviceId, SignalKind.Button, 1f);
            else if (device is Mouse) _sink.OnDeviceSignal(DeviceKind.KeyboardMouse, device.deviceId, SignalKind.MouseClick, 1f);
            else _sink.OnDeviceSignal(DeviceKind.KeyboardMouse, device.deviceId, SignalKind.Button, 1f);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
                _sink.OnDeviceRemoved(KindOf(device), device.deviceId);
        }

        private void OnFocusChanged(bool focused) => _sink.OnFocusChanged(focused);

        private static DeviceKind KindOf(InputDevice device) => device is Gamepad ? DeviceKind.Gamepad : DeviceKind.KeyboardMouse;
    }
}

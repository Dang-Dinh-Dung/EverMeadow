using System;

namespace Evermeadow.Foundation.Input
{
    /// <summary>What gameplay systems use to read buffered input (ADR-0001 Key Interfaces; GDD Input Rule 8).</summary>
    public interface IInputQueries
    {
        /// <summary>Takes the oldest eligible Pressed record of <paramref name="action"/> (F1) and marks it consumed.</summary>
        bool TryConsume(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record);

        /// <summary>Like <see cref="TryConsume"/> without consuming.</summary>
        bool Peek(InputActionId action, long windowTicks, ClockKind clock, out InputRecord record);

        /// <summary>True when <paramref name="action"/> was released after the record numbered <paramref name="seq"/>.</summary>
        bool WasReleasedAfter(InputActionId action, long seq);

        /// <summary>Held state after hold/toggle mode (Rule 9) and latching (Rule 12). False outside the top mode.</summary>
        bool IsHeld(InputActionId action);

        /// <summary>Move after dead zone; zero unless Gameplay is on top.</summary>
        Vec2 ReadMove();

        /// <summary>Navigate after dead zone; zero unless the top mode has Navigate.</summary>
        Vec2 ReadNavigate();

        /// <summary>Navigate step emitted this frame by auto-repeat (Rule 16, F5); false when none.</summary>
        bool TryGetNavigateStep(out Vec2 direction);

        /// <summary>Declares the largest window <paramref name="consumer"/> will use for <paramref name="action"/> (Rule 7, F2).</summary>
        void RegisterConsumer(object consumer, InputActionId action, long maxWindowTicks, ClockKind clock);
    }

    /// <summary>Token stack of input modes (Rule 11–12).</summary>
    public interface IInputModes
    {
        /// <summary>Pushes <paramref name="mode"/>; only the returned handle can pop it.</summary>
        ModeHandle Push(InputMode mode, object owner, Func<bool> isAlive);

        /// <summary>Pops the entry of <paramref name="handle"/>. False for an invalid, stale or foreign handle.</summary>
        bool Pop(ModeHandle handle);

        /// <summary>True when <paramref name="handle"/>'s entry is on top.</summary>
        bool IsTop(ModeHandle handle);

        /// <summary>Raised for the handle that left the top (false) and the one that reached it (true). The Gameplay base raises nothing.</summary>
        event Action<ModeHandle, bool> TopChanged;
    }

    /// <summary>Active device for glyphs (Rule 13–15).</summary>
    public interface IInputDevices
    {
        /// <summary>Device whose glyphs are shown.</summary>
        DeviceKind Active { get; }

        /// <summary>Raised at most once per input step, at its end, when the active device changed.</summary>
        event Action<DeviceKind> DeviceChanged;

        /// <summary>Raised when the most recently used gamepad is removed.</summary>
        event Action DeviceLost;
    }

    /// <summary>
    /// Adapter → core events. <c>deviceId</c> identifies the physical device (needed to latch only the buttons of a removed
    /// gamepad, Rule 15); <c>isPointer</c> marks mouse-button sources (ignored on the focus-regain step, Rule 15).
    /// </summary>
    public interface IInputEventSink
    {
        /// <summary>A button action was pressed (performed) or released (canceled).</summary>
        void OnAction(InputActionId action, InputPhase phase, DeviceKind device, int deviceId, bool isPointer);

        /// <summary>Raw axis value sampled once per frame.</summary>
        void OnAxisSample(AxisId axis, Vec2 raw, DeviceKind device);

        /// <summary>Raw signal used only for active-device tracking (F4).</summary>
        void OnDeviceSignal(DeviceKind device, int deviceId, SignalKind kind, float magnitude);

        /// <summary>A physical device was removed.</summary>
        void OnDeviceRemoved(DeviceKind device, int deviceId);

        /// <summary>Application focus changed.</summary>
        void OnFocusChanged(bool focused);
    }
}

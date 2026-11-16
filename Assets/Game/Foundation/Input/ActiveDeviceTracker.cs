using System;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Tracks the active device for glyphs only (GDD Input Rule 13, F4). It never blocks input.
    /// A change is reported at most once per input step, at the end, and only if the value differs from the start of the step.
    /// </summary>
    public sealed class ActiveDeviceTracker
    {
        private float _triggerThreshold;
        private float _stickSwitchEffective;
        private float _mousePixels;
        private long _mouseWindow;
        private DeviceKind _stepStart;
        private long _mouseWindowStart = long.MinValue;
        private float _mouseAccumulated;

        /// <summary>Creates a tracker starting on <paramref name="initial"/>.</summary>
        public ActiveDeviceTracker(DeviceKind initial, InputConfig config)
        {
            Active = initial;
            _stepStart = initial;
            Configure(config);
        }

        /// <summary>Device whose glyphs are shown.</summary>
        public DeviceKind Active { get; private set; }

        /// <summary>Applies thresholds from <paramref name="config"/> (dead zone changes move the stick floor).</summary>
        public void Configure(InputConfig config)
        {
            _triggerThreshold = config.TriggerThreshold;
            _stickSwitchEffective = InputMath.StickSwitchEffective(config.StickSwitchThreshold, config.DeadZone);
            _mousePixels = config.MouseSwitchPixels;
            _mouseWindow = Ticks.FromSeconds(config.MouseSwitchSeconds);
        }

        /// <summary>Marks the start of an input step.</summary>
        public void BeginStep() => _stepStart = Active;

        /// <summary>
        /// Feeds a raw signal. <paramref name="mouseMoveCounts"/> is true only in Menu and Map (Rule 13).
        /// Returns true when the signal was meaningful (F4).
        /// </summary>
        public bool Signal(DeviceKind device, SignalKind kind, float magnitude, bool mouseMoveCounts, long realNow)
        {
            bool meaningful;
            switch (kind)
            {
                case SignalKind.Button:
                case SignalKind.MouseClick:
                    meaningful = true;
                    break;
                case SignalKind.Trigger:
                    meaningful = magnitude >= _triggerThreshold;
                    break;
                case SignalKind.Stick:
                    meaningful = magnitude >= _stickSwitchEffective;
                    break;
                case SignalKind.MouseMove:
                    meaningful = mouseMoveCounts && AccumulateMouse(magnitude, realNow);
                    break;
                default:
                    meaningful = false;
                    break;
            }
            if (meaningful) Active = device;
            return meaningful;
        }

        /// <summary>Forces the active device (gamepad removed → keyboard and mouse, Rule 15).</summary>
        public void Force(DeviceKind device) => Active = device;

        /// <summary>Ends the step; returns true when <see cref="Active"/> differs from the start of the step.</summary>
        public bool EndStep() => Active != _stepStart;

        private bool AccumulateMouse(float pixels, long realNow)
        {
            if (_mouseWindowStart == long.MinValue || realNow - _mouseWindowStart > _mouseWindow)
            {
                _mouseWindowStart = realNow;
                _mouseAccumulated = 0f;
            }
            _mouseAccumulated += Math.Abs(pixels);
            return _mouseAccumulated > _mousePixels;
        }
    }
}

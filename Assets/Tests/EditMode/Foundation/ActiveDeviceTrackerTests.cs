using Evermeadow.Foundation.Input;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class ActiveDeviceTrackerTests
    {
        private static ActiveDeviceTracker Create(DeviceKind initial, InputConfig cfg = null) =>
            new ActiveDeviceTracker(initial, cfg ?? InputFixture.DefaultConfig());

        [Test, Description("AC-F12")]
        public void Signal_DeadZone04_StickSwitchesOnlyAtOrAbove055()
        {
            var cfg = InputFixture.DefaultConfig();
            cfg.DeadZone = 0.4f;
            var tracker = Create(DeviceKind.KeyboardMouse, cfg);

            bool drift = tracker.Signal(DeviceKind.Gamepad, SignalKind.Stick, 0.35f, false, 0);
            bool atEff = tracker.Signal(DeviceKind.Gamepad, SignalKind.Stick, 0.55f, false, 0);

            Assert.IsFalse(drift);
            Assert.IsTrue(atEff);
            Assert.AreEqual(DeviceKind.Gamepad, tracker.Active);
            Assert.IsTrue(Create(DeviceKind.KeyboardMouse, cfg).Signal(DeviceKind.Gamepad, SignalKind.Stick, 0.6f, false, 0));
        }

        [Test, Description("AC-F13 (device part)")]
        public void Signal_Trigger029Then030_SwitchesOnlyAtThreshold()
        {
            var tracker = Create(DeviceKind.KeyboardMouse);
            float threshold = InputFixture.DefaultConfig().TriggerThreshold;

            tracker.Signal(DeviceKind.Gamepad, SignalKind.Trigger, threshold - 0.01f, false, 0);
            DeviceKind below = tracker.Active;
            tracker.Signal(DeviceKind.Gamepad, SignalKind.Trigger, threshold, false, 0);

            Assert.AreEqual(DeviceKind.KeyboardMouse, below);
            Assert.AreEqual(DeviceKind.Gamepad, tracker.Active);
        }

        [TestCase(8f, false, Description = "AC-F14: 8 px at 1080p")]
        [TestCase(9f, true, Description = "AC-F14: 9 px at 1080p")]
        [TestCase(16f * 1080f / 2160f, false, Description = "AC-F14: 16 px at 2160p")]
        [TestCase(18f * 1080f / 2160f, true, Description = "AC-F14: 18 px at 2160p")]
        public void Signal_MouseMoveInMenu_SwitchesAboveThreshold(float normalisedPixels, bool expected)
        {
            var tracker = Create(DeviceKind.Gamepad);

            bool switched = tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.MouseMove, normalisedPixels, true, 0);

            Assert.AreEqual(expected, switched);
        }

        [Test, Description("AC-F14")]
        public void Signal_MouseMoveInGameplay_NeverSwitches()
        {
            var tracker = Create(DeviceKind.Gamepad);

            bool switched = tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.MouseMove, 100f, false, 0);

            Assert.IsFalse(switched);
            Assert.AreEqual(DeviceKind.Gamepad, tracker.Active);
        }

        [Test]
        public void Signal_MouseMoveOutsideWindow_AccumulationRestarts()
        {
            var tracker = Create(DeviceKind.Gamepad);
            long window = Evermeadow.Foundation.Time.Ticks.FromSeconds(InputFixture.DefaultConfig().MouseSwitchSeconds);

            tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.MouseMove, 5f, true, 0);
            bool late = tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.MouseMove, 5f, true, window + 1);

            Assert.IsFalse(late);
        }

        [Test, Description("AC-R29 (tracker part)")]
        public void EndStep_BothDevicesSignalInOneStep_LastWinsAndReportsOnce()
        {
            var tracker = Create(DeviceKind.KeyboardMouse);
            tracker.BeginStep();

            tracker.Signal(DeviceKind.Gamepad, SignalKind.Button, 1f, false, 0);
            tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.Button, 1f, false, 0);
            bool changed = tracker.EndStep();

            Assert.IsFalse(changed, "ended on the device it started on");
            tracker.BeginStep();
            tracker.Signal(DeviceKind.KeyboardMouse, SignalKind.Button, 1f, false, 0);
            tracker.Signal(DeviceKind.Gamepad, SignalKind.Button, 1f, false, 0);
            Assert.IsTrue(tracker.EndStep());
            Assert.AreEqual(DeviceKind.Gamepad, tracker.Active);
        }
    }
}

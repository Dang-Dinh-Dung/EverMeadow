using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Input.Unity;
using Evermeadow.Foundation.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;
using UnityEngine.TestTools;

namespace Evermeadow.Tests.PlayMode.Foundation
{
    /// <summary>
    /// Engine-assumption checks of ADR-0001 (V1–V10) and adapter AC of GDD Input, on the real action asset with simulated devices.
    /// Every test switches the Input System to manual updates, as the project's settings asset does.
    /// </summary>
    public class InputAdapterTests : InputTestFixture
    {
        private const string AssetPath = "Assets/Game/Input/EvermeadowInput.inputactions";

        private InputActionAsset _source;
        private FrameClock _clock;
        private InputService _service;
        private UnityInputAdapter _adapter;
        private Keyboard _keyboard;
        private long _now;

        private sealed class NullLog : ILogSink
        {
            public void Warn(string key, string message) { }
            public void Error(string key, string message) { }
        }

        public override void Setup()
        {
            base.Setup();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
#if UNITY_EDITOR
            _source = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
#endif
            Assert.IsNotNull(_source, AssetPath);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _clock = new FrameClock();
            _now = Ticks.FromSeconds(10);
            _clock.Advance(_now);
            _service = new InputService(_clock, new InputConfig(), new NullLog(), false);
            _adapter = new UnityInputAdapter(_source, _service, new NullLog());
        }

        public override void TearDown()
        {
            _adapter?.Dispose();
            UnityEngine.Time.timeScale = 1f;
            base.TearDown();
        }

        /// <summary>Frame prologue as FrameDriver runs it, without touching Time.timeScale.</summary>
        private void Step(long realDelta = 16_667)
        {
            _now += realDelta;
            _clock.Advance(_now);
            _service.BeginStep();
            InputSystem.Update();
            _adapter.SampleFrame();
            _service.EndStep();
        }

        private List<InputRecord> Records()
        {
            var buffer = new InputRecord[64];
            int n = _service.SnapshotBuffer(buffer);
            return buffer.Take(n).ToList();
        }

        [Test, Description("V6: InputTestFixture usable from the test asmdef; V10 part 1: clone keeps action ids")]
        public void Adapter_ClonesAssetWithSameActionIds()
        {
            Assert.AreNotSame(_source, _adapter.Asset);
            Assert.AreEqual(_source.FindAction(ActionIds.For(InputActionId.Jump)).id, _adapter.ActionFor(InputActionId.Jump).id);
            Assert.IsFalse(_source.enabled, "source asset is never enabled");
            Assert.IsTrue(_adapter.Asset.enabled);
        }

        [Test, Description("AC-R10 / V1: press-release-press inside one update gives three records")]
        public void PressReleasePress_OneUpdate_ThreeRecords()
        {
            Press(_keyboard.jKey, queueEventOnly: true);
            Release(_keyboard.jKey, queueEventOnly: true);
            Press(_keyboard.jKey, queueEventOnly: true);

            Step();

            List<InputRecord> r = Records();
            CollectionAssert.AreEqual(new[] { InputPhase.Pressed, InputPhase.Released, InputPhase.Pressed }, r.Select(x => x.Phase).ToArray());
            Assert.That(r.Select(x => x.Action), Is.All.EqualTo(InputActionId.Attack));
        }

        [Test, Description("AC-R19 / V2: manual mode processes only inside InputSystem.Update, stamped with real time under hit-stop")]
        public void QueuedPress_DuringHitstop_RecordedAtUpdateWithRealTime()
        {
            _clock.PushScale(0.05, this, () => true);
            UnityEngine.Time.timeScale = 0.05f;
            Press(_keyboard.leftShiftKey, queueEventOnly: true);
            _adapter.SampleFrame();
            int beforeUpdate = Records().Count;

            Step();

            Assert.AreEqual(0, beforeUpdate);
            InputRecord dodge = Records().Single();
            Assert.AreEqual(InputActionId.Dodge, dodge.Action);
            Assert.AreEqual(_clock.RealNow, dodge.TReal);
            Assert.AreEqual(_clock.ScaledNow, dodge.TScaled);
            Assert.Less(_clock.ScaledDelta, _clock.RealDelta, "scaled clock slowed by the hit-stop");
        }

        [Test, Description("V7: started and performed fire in the same update; only performed is recorded")]
        public void ButtonPress_StartedAndPerformedSameUpdate_OneRecord()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            int started = 0, performed = 0;
            jump.started += _ => started++;
            jump.performed += _ => performed++;
            Press(_keyboard.spaceKey, queueEventOnly: true);

            Step();

            Assert.AreEqual(1, started);
            Assert.AreEqual(1, performed);
            Assert.AreEqual(1, Records().Count(r => r.Action == InputActionId.Jump));
        }

        [Test, Description("V8: WASD diagonal has length 1 (DigitalNormalized)")]
        public void WasdDiagonal_ReadMove_LengthOne()
        {
            Press(_keyboard.wKey, queueEventOnly: true);
            Press(_keyboard.dKey, queueEventOnly: true);

            Step();

            Vec2 move = _service.ReadMove();
            Assert.AreEqual(1f, move.Magnitude, 1e-5f);
            Assert.AreEqual(0.70710678f, move.X, 1e-5f);
        }

        [Test, Description("AC-M06 (I-8) end to end: Esc and Menu/Options are Pause in Gameplay, Cancel when a menu is open")]
        public void EscAndMenuButton_ByMode_OneActionPerPress()
        {
            var pad = InputSystem.AddDevice<Gamepad>();

            PressAndRelease(_keyboard.escapeKey, queueEventOnly: true);
            Step();
            var gameplay = Records().Where(r => r.Phase == InputPhase.Pressed).Select(r => r.Action).ToArray();

            ModeHandle map = _service.Push(InputMode.Map, this, () => true);
            PressAndRelease(_keyboard.escapeKey, queueEventOnly: true);
            Step();
            var inMap = Records().Where(r => r.Phase == InputPhase.Pressed && r.Epoch == _service.Epoch).Select(r => r.Action).ToArray();
            _service.Pop(map);

            _service.Push(InputMode.Menu, this, () => true);
            PressAndRelease(pad.startButton, queueEventOnly: true);
            Step();
            var inMenu = Records().Where(r => r.Phase == InputPhase.Pressed && r.Epoch == _service.Epoch).Select(r => r.Action).ToArray();

            CollectionAssert.AreEqual(new[] { InputActionId.Pause }, gameplay);
            CollectionAssert.AreEqual(new[] { InputActionId.Cancel }, inMap);
            CollectionAssert.AreEqual(new[] { InputActionId.Cancel }, inMenu);
        }

        [Test, Description("V9: interactive rebinding throws while the action is enabled (already when the operation is created)")]
        public void PerformInteractiveRebinding_ActionEnabled_Throws()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);

            Assert.Throws<InvalidOperationException>(() => jump.PerformInteractiveRebinding(0).Start());
        }

        [Test, Description("V10: override JSON saved on the clone is keyed by the source asset's binding ids")]
        public void SaveBindingOverridesAsJson_OnClone_UsesSourceBindingIds()
        {
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            int index = jump.bindings.IndexOf(b => b.path == "<Keyboard>/space");
            Guid sourceBindingId = _source.FindAction(ActionIds.For(InputActionId.Jump)).bindings[index].id;

            jump.ApplyBindingOverride(index, "<Keyboard>/j");
            string json = _adapter.Asset.SaveBindingOverridesAsJson();

            Assert.AreEqual(sourceBindingId, jump.bindings[index].id);
            StringAssert.Contains(sourceBindingId.ToString(), json);
            Assert.AreEqual("<Keyboard>/space", _source.FindAction(ActionIds.For(InputActionId.Jump)).bindings[index].effectivePath, "source untouched");
        }

        [Test, Description("V3 / AC-R33 integration: removing a gamepad while holding Jump leaves it released and latched")]
        public void RemoveGamepad_WhileHoldingJump_NotHeldAndReleasedAfter()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Press(pad.buttonSouth, queueEventOnly: true);
            Step();
            InputRecord press = Records().Single(r => r.Action == InputActionId.Jump);

            _service.BeginStep();
            InputSystem.RemoveDevice(pad);
            InputSystem.Update();
            _service.EndStep();

            Assert.IsFalse(_service.IsHeld(InputActionId.Jump));
            Assert.IsTrue(_service.WasReleasedAfter(InputActionId.Jump, press.Seq));
            Assert.AreEqual(DeviceKind.KeyboardMouse, _service.Active);
            Assert.IsFalse(Records().Any(r => r.Phase == InputPhase.Released), "V3: removal cancels the action; no Released record (Rule 15)");
        }

        [Test, Description("AC-R31: glyphs per layout and with a family override")]
        public void GetGlyph_Jump_PerLayoutAndOverride()
        {
            var glyphs = new GlyphProvider(_adapter, _service);
            var xbox = InputSystem.AddDevice<XInputController>();
            var dualShock = InputSystem.AddDevice<DualShock4GamepadHID>();
            var generic = InputSystem.AddDevice<Gamepad>();

            string kb = glyphs.GetGlyph(InputActionId.Jump, DeviceKind.KeyboardMouse, null).Label;
            string onXbox = glyphs.GetGlyph(InputActionId.Jump, DeviceKind.Gamepad, xbox).Label;
            string onDualShock = glyphs.GetGlyph(InputActionId.Jump, DeviceKind.Gamepad, dualShock).Label;
            string onGeneric = glyphs.GetGlyph(InputActionId.Jump, DeviceKind.Gamepad, generic).Label;
            glyphs.SetGlyphFamily(GlyphFamily.PlayStation);
            string overridden = glyphs.GetGlyph(InputActionId.Jump, DeviceKind.Gamepad, xbox).Label;

            CollectionAssert.AreEqual(new[] { "Space", "A", "Cross", "A", "Cross" }, new[] { kb, onXbox, onDualShock, onGeneric, overridden });
        }

        private sealed class CountingSink : IInputEventSink
        {
            public int Removed;
            public void OnAction(InputActionId action, InputPhase phase, DeviceKind device, int deviceId, bool isPointer) { }
            public void OnAxisSample(AxisId axis, Vec2 raw, DeviceKind device) { }
            public void OnDeviceSignal(DeviceKind device, int deviceId, SignalKind kind, float magnitude) { }
            public void OnDeviceRemoved(DeviceKind device, int deviceId) => Removed++;
            public void OnFocusChanged(bool focused) { }
        }

        [Test, Description("AC-R41 / V5: a disposed adapter leaves no subscription and no enabled clone")]
        public void DisposeThenRecreate_DeviceChangeHandledOnce()
        {
            var first = new CountingSink();
            var a1 = new UnityInputAdapter(_source, first, new NullLog());
            InputActionAsset clone1 = a1.Asset;
            a1.Dispose();
            var second = new CountingSink();
            var a2 = new UnityInputAdapter(_source, second, new NullLog());
            var pad = InputSystem.AddDevice<Gamepad>();

            InputSystem.RemoveDevice(pad);
            a2.Dispose();

            Assert.AreEqual(0, first.Removed);
            Assert.AreEqual(1, second.Removed);
            Assert.IsTrue(clone1 == null, "clone destroyed");
            Assert.IsFalse(_source.enabled);
        }

        [Test, Description("V2: rebinding works in manual mode; AC-R36 end to end")]
        public void Rebind_JumpToJ_ConflictThenSwap()
        {
            var rebind = new RebindController(_adapter, _service, new NullLog());
            InputAction jump = _adapter.ActionFor(InputActionId.Jump);
            InputAction attack = _adapter.ActionFor(InputActionId.Attack);
            int jumpIndex = jump.bindings.IndexOf(b => b.path == "<Keyboard>/space");
            int attackIndex = attack.bindings.IndexOf(b => b.path == "<Keyboard>/j");
            var results = new List<RebindResult>();
            long epoch = _service.Epoch;

            rebind.StartRebind(InputActionId.Jump, jumpIndex, results.Add);
            Press(_keyboard.jKey, queueEventOnly: true);
            for (int i = 0; i < 5 && results.Count == 0; i++)
            {
                currentTime += 0.1; // the operation waits 0.05 s of input time for a better match (OnMatchWaitForAnother)
                Step();
            }
            Release(_keyboard.jKey, queueEventOnly: true);
            Step();

            Assert.AreEqual(1, results.Count, "rebind finished listening");
            Assert.AreEqual(RebindStatus.Conflict, results[0].Status);
            CollectionAssert.AreEqual(new[] { InputActionId.Attack }, results[0].Conflicts);
            rebind.CommitRebind(true);
            Assert.AreEqual("<Keyboard>/j", jump.bindings[jumpIndex].effectivePath);
            Assert.AreEqual("<Keyboard>/space", attack.bindings[attackIndex].effectivePath);
            Assert.IsTrue(jump.enabled);
            Assert.Greater(_service.Epoch, epoch, "rebind is a barrier");
            Assert.IsFalse(Records().Any(r => r.Action == InputActionId.Attack), "the key pressed to rebind did not leak as Attack");
            rebind.Dispose();
        }

        private sealed class OrderProbe : MonoBehaviour
        {
            public IInputDevices Devices;
            public int FirstFrameSeenGamepad = -1;

            private void Update()
            {
                if (FirstFrameSeenGamepad < 0 && Devices.Active == DeviceKind.Gamepad) FirstFrameSeenGamepad = UnityEngine.Time.frameCount;
            }
        }

        [UnityTest, Description("AC-R32: DeviceChanged is raised in frame N before any default-order Update")]
        public IEnumerator FrameDriver_GamepadPress_DeviceChangedBeforeGameplayUpdate()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var go = new GameObject("FrameDriver");
            var driver = go.AddComponent<FrameDriver>();
            driver.Init(_clock, _service, _adapter, new UnityRealTimeSource());
            var probe = go.AddComponent<OrderProbe>();
            probe.Devices = _service;
            int changedFrame = -1;
            _service.DeviceChanged += d => { if (d == DeviceKind.Gamepad) changedFrame = UnityEngine.Time.frameCount; };
            yield return null;

            Press(pad.buttonEast, queueEventOnly: true);
            yield return null;
            yield return null;
            UnityEngine.Object.Destroy(go);

            Assert.Greater(changedFrame, 0, "DeviceChanged raised");
            Assert.AreEqual(changedFrame, probe.FirstFrameSeenGamepad, "gameplay saw the new device in the same frame");
        }
    }
}

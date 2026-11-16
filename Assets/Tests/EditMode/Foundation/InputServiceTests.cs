using System.Collections.Generic;
using System.Linq;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Foundation.InputFixture;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class InputServiceTests
    {
        private FakeClock _clock;
        private RecordingLogSink _log;
        private InputService _input;
        private long _window;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock { RealNow = Ticks.FromSeconds(10), ScaledNow = Ticks.FromSeconds(10) };
            _log = new RecordingLogSink();
            _input = Create(_clock, _log);
            _window = Ticks.FromSeconds(0.12);
        }

        private ModeHandle PushNow(InputMode mode) => _input.Push(mode, Owner(), Alive);

        // ------------------------------------------------------------ H.1 core rules

        [Test, Description("AC-R04")]
        public void Press_KeyboardWhileGamepadActive_RecordedWithKeyboardAndMoveRead()
        {
            var input = Create(_clock, _log, gamepadConnected: true);

            input.Step(() =>
            {
                input.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse);
                input.Press(InputActionId.Attack);
            });

            Assert.AreEqual(DeviceKind.Gamepad, input.Active);
            Assert.IsTrue(input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out InputRecord r));
            Assert.AreEqual(DeviceKind.KeyboardMouse, r.Device);
            Assert.Greater(input.ReadMove().X, 0f);
        }

        [Test, Description("AC-R05")]
        public void Press_AttackWhileDown_OnePlungeRecordConsumableOnce()
        {
            _input.Step(() => _input.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse));

            _input.Step(() => _input.Press(InputActionId.Attack));

            List<InputRecord> records = _input.Records();
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual(InputActionId.Plunge, records[0].Action);
            Assert.AreEqual(InputActionId.Attack, records[0].BaseAction);
            Assert.IsTrue(_input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out InputRecord r));
            Assert.AreEqual(records[0].Seq, r.Seq);
            Assert.IsFalse(_input.TryConsume(InputActionId.Plunge, _window, ClockKind.Real, out _));
        }

        [Test, Description("AC-R06")]
        public void Press_DownAndAttackInEitherOrderInOneStep_SamePlungeWithEndOfStepMove()
        {
            var endMove = new Vec2(0.1f, -0.9f);
            var axisFirst = Create(new FakeClock());
            var attackFirst = Create(new FakeClock());

            axisFirst.Step(() =>
            {
                axisFirst.OnAxisSample(AxisId.Move, endMove, DeviceKind.KeyboardMouse);
                axisFirst.Press(InputActionId.Attack);
            });
            attackFirst.Step(() =>
            {
                attackFirst.Press(InputActionId.Attack);
                attackFirst.OnAxisSample(AxisId.Move, endMove, DeviceKind.KeyboardMouse);
            });

            InputRecord a = axisFirst.Records().Single();
            InputRecord b = attackFirst.Records().Single();
            Assert.AreEqual(InputActionId.Plunge, a.Action);
            Assert.AreEqual(InputActionId.Plunge, b.Action);
            Assert.AreEqual(InputActionId.Attack, b.BaseAction);
            Assert.AreEqual(a.Move, b.Move);
            Assert.AreEqual(axisFirst.ReadMove(), b.Move);
        }

        [Test, Description("AC-R07 (core part; rebinding Attack is adapter work)")]
        public void Press_AttackWhileDownWithSingleButtonPlunge_PlainAttack()
        {
            _input.SetChordMode(InputActionId.Plunge, false);
            _input.Step(() => _input.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse));

            _input.Step(() => _input.Press(InputActionId.Attack));

            InputRecord r = _input.Records().Single();
            Assert.AreEqual(InputActionId.Attack, r.Action);
            Assert.AreEqual(InputActionId.None, r.BaseAction);
        }

        [Test, Description("AC-R08")]
        public void TryConsume_DownReleasedBeforeConsume_PlungeKeepsPressMove()
        {
            _input.Step(() =>
            {
                _input.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse);
                _input.Press(InputActionId.Attack);
            });

            _input.Step(() => _input.OnAxisSample(AxisId.Move, Vec2.Zero, DeviceKind.KeyboardMouse));

            Assert.IsTrue(_input.TryConsume(InputActionId.Plunge, _window, ClockKind.Real, out InputRecord r));
            Assert.LessOrEqual(r.Move.Y, -_input.Config.DownThreshold);
        }

        [Test, Description("AC-R09")]
        public void PressRelease_InjectedClock_RecordsMatchExactly()
        {
            _clock.RealNow = 1_234_567;
            _clock.ScaledNow = 765_432;

            _input.Step(() =>
            {
                _input.OnAxisSample(AxisId.Move, Right, DeviceKind.Gamepad);
                _input.Press(InputActionId.Jump, DeviceKind.Gamepad, PadA);
                _input.Release(InputActionId.Jump, DeviceKind.Gamepad, PadA);
            });

            List<InputRecord> r = _input.Records();
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(r[0].Seq + 1, r[1].Seq);
            Assert.AreEqual(InputPhase.Pressed, r[0].Phase);
            Assert.AreEqual(InputPhase.Released, r[1].Phase);
            foreach (InputRecord rec in r)
            {
                Assert.AreEqual(1_234_567, rec.TReal);
                Assert.AreEqual(765_432, rec.TScaled);
                Assert.AreEqual(0, rec.Epoch);
                Assert.AreEqual(DeviceKind.Gamepad, rec.Device);
                Assert.AreEqual(Right, rec.Move);
            }
        }

        [Test, Description("AC-R13")]
        public void RegisterConsumer_OverRetentionAndOverRegisteredQueries_WarnOncePerPair()
        {
            object consumer = Owner();
            long w = Ticks.FromSeconds(1.5);

            _input.RegisterConsumer(consumer, InputActionId.Jump, w, ClockKind.Real);
            int afterRegister = _log.Count("Input.WindowExceedsRetention");
            _input.TryConsume(InputActionId.Jump, w + 1, ClockKind.Real, out _);
            _input.TryConsume(InputActionId.Jump, w + 1, ClockKind.Real, out _);

            Assert.AreEqual(1, afterRegister);
            Assert.AreEqual(1, _log.Count("Input.WindowOverRegistered"));
        }

        [Test, Description("AC-R13: negative window acts as zero")]
        public void Peek_NegativeWindow_ActsAsZero()
        {
            _input.Step(() => _input.Press(InputActionId.Jump));
            long negative = -Ticks.FromSeconds(0.1);

            bool sameInstant = _input.Peek(InputActionId.Jump, negative, ClockKind.Real, out _);
            _clock.Advance(1);
            bool oneTickLater = _input.Peek(InputActionId.Jump, negative, ClockKind.Real, out _);

            Assert.IsTrue(sameInstant);
            Assert.IsFalse(oneTickLater);
        }

        [Test, Description("AC-R14")]
        public void TryConsume_ThreePressesFourCalls_ThreeInSeqOrderThenNone()
        {
            _input.Step(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    _input.Press(InputActionId.Attack);
                    _input.Release(InputActionId.Attack);
                }
            });

            var seqs = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(_input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out InputRecord r));
                seqs.Add(r.Seq);
            }

            Assert.IsFalse(_input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out _));
            CollectionAssert.IsOrdered(seqs);
            CollectionAssert.AllItemsAreUnique(seqs);
        }

        [Test, Description("AC-R15")]
        public void PeekTwiceThenConsume_SameRecordAndPeekDoesNotConsume()
        {
            _input.Step(() => _input.Press(InputActionId.Dodge));

            _input.Peek(InputActionId.Dodge, _window, ClockKind.Real, out InputRecord p1);
            _input.Peek(InputActionId.Dodge, _window, ClockKind.Real, out InputRecord p2);
            bool consumed = _input.TryConsume(InputActionId.Dodge, _window, ClockKind.Real, out InputRecord c);

            Assert.IsTrue(consumed);
            Assert.AreEqual(p1.Seq, p2.Seq);
            Assert.AreEqual(p1.Seq, c.Seq);
            Assert.IsFalse(p2.Consumed);
        }

        [Test, Description("AC-R16")]
        public void WasReleasedAfter_ReleaseDuringHitstopThenOverwritten_True()
        {
            _input.Step(() => _input.Press(InputActionId.Jump));
            _input.Peek(InputActionId.Jump, _window, ClockKind.Real, out InputRecord press);
            _clock.RealNow += 10_000;
            _clock.ScaledNow += 500; // hit-stop
            _input.Step(() => _input.Release(InputActionId.Jump));

            _input.Step(() =>
            {
                for (int i = 0; i < 40; i++)
                {
                    _input.Press(InputActionId.Attack);
                    _input.Release(InputActionId.Attack);
                }
            });

            Assert.IsFalse(_input.Records().Any(r => r.Action == InputActionId.Jump));
            Assert.IsTrue(_input.WasReleasedAfter(InputActionId.Jump, press.Seq));
        }

        [TestCase(InputMode.Menu)]
        [TestCase(InputMode.Fishing)]
        [Description("AC-R17")]
        public void Queries_GameplayNotOnTop_ZeroAxisNoneAndNoLog(InputMode mode)
        {
            _input.Step(() => _input.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse));
            _input.Step(() => _input.OnAxisSample(AxisId.Navigate, Right, DeviceKind.KeyboardMouse));
            PushNow(mode);

            Vec2 move = _input.ReadMove();
            Vec2 nav = _input.ReadNavigate();
            bool jump = _input.TryConsume(InputActionId.Jump, _window, ClockKind.Real, out _);

            Assert.AreEqual(Vec2.Zero, move);
            Assert.AreEqual(mode == InputMode.Menu ? Right : Vec2.Zero, nav);
            Assert.IsFalse(jump);
            Assert.AreEqual(0, _log.Total);
        }

        [Test, Description("AC-R18")]
        public void IsHeld_ReelToggle_PressReleasePressGivesTrueTrueFalse()
        {
            _input.SetHoldMode(InputActionId.Reel, HoldMode.Toggle);
            PushNow(InputMode.Fishing);

            _input.Step(() => _input.Press(InputActionId.Reel));
            bool afterPress = _input.IsHeld(InputActionId.Reel);
            _input.Step(() => _input.Release(InputActionId.Reel));
            bool afterRelease = _input.IsHeld(InputActionId.Reel);
            _input.Step(() => _input.Press(InputActionId.Reel));
            bool afterSecondPress = _input.IsHeld(InputActionId.Reel);

            Assert.AreEqual(new[] { true, true, false }, new[] { afterPress, afterRelease, afterSecondPress });
        }

        [Test, Description("AC-R18")]
        public void IsHeld_ReelHold_FollowsButton()
        {
            PushNow(InputMode.Fishing);

            _input.Step(() => _input.Press(InputActionId.Reel));
            bool down = _input.IsHeld(InputActionId.Reel);
            _input.Step(() => _input.Release(InputActionId.Reel));

            Assert.IsTrue(down);
            Assert.IsFalse(_input.IsHeld(InputActionId.Reel));
        }

        [Test, Description("AC-R20")]
        public void EndStep_ScaledClockFrozenOverRetentionInGameplay_WarnsOnce()
        {
            _input.Step();

            _clock.RealNow += Retention + 1;
            _input.Step();
            _clock.RealNow += Retention + 1;
            _input.Step();

            Assert.AreEqual(1, _log.Count("Input.FrozenClock"));
        }

        [Test, Description("AC-R20")]
        public void EndStep_ScaledClockFrozenWithMenuPushed_NoWarning()
        {
            PushNow(InputMode.Menu);
            _input.Step();

            _clock.RealNow += Retention + 1;
            _input.Step();

            Assert.AreEqual(0, _log.Count("Input.FrozenClock"));
        }

        [Test, Description("AC-R21")]
        public void Dialogue_AttackAndForeignPops_NoRecordAndStackUnchanged()
        {
            PushNow(InputMode.Dialogue);
            ModeHandle foreign = Create(new FakeClock()).Push(InputMode.Dialogue, Owner(), Alive);

            _input.Step(() => _input.Press(InputActionId.Attack));
            bool foreignPop = _input.Pop(foreign);
            bool gameplayPop = _input.Pop(default);

            Assert.AreEqual(0, _input.Records().Count);
            Assert.IsFalse(foreignPop);
            Assert.IsFalse(gameplayPop);
            Assert.AreEqual(InputMode.Dialogue, _input.TopMode);
        }

        [Test, Description("AC-R22")]
        public void Pop_StaleHandle_EpochUnchanged()
        {
            ModeHandle old = PushNow(InputMode.Menu);
            _input.Pop(old);
            PushNow(InputMode.Menu);
            long epoch = _input.Epoch;

            bool popped = _input.Pop(old);

            Assert.IsFalse(popped);
            Assert.AreEqual(epoch, _input.Epoch);
            Assert.AreEqual(1, _log.Count("Input.BadModeHandle"));
        }

        [Test, Description("AC-R23")]
        public void EndStep_LockedOwnerDead_RemovedWithBarrierAndPauseRecordedAgain()
        {
            bool alive = true;
            _input.Push(InputMode.Locked, Owner(), () => alive);
            long epoch = _input.Epoch;
            alive = false;

            _input.Step();
            _input.Step(() => _input.Press(InputActionId.Pause));

            Assert.AreEqual(InputMode.Gameplay, _input.TopMode);
            Assert.AreEqual(1, _log.Count("Input.DeadModeOwner"));
            Assert.AreEqual(epoch + 1, _input.Epoch);
            Assert.IsTrue(_input.TryConsume(InputActionId.Pause, _window, ClockKind.Real, out _));
        }

        [Test, Description("AC-R24")]
        public void TopChanged_MenuOverFishingPushedAndPopped_FishingGetsFalseThenTrue()
        {
            ModeHandle fishing = PushNow(InputMode.Fishing);
            var seen = new List<(bool value, bool isTop)>();
            _input.TopChanged += (h, top) =>
            {
                if (h.SameAs(fishing)) seen.Add((top, _input.IsTop(fishing)));
            };

            ModeHandle menu = PushNow(InputMode.Menu);
            _input.Pop(menu);

            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual((false, false), seen[0]);
            Assert.AreEqual((true, true), seen[1]);
        }

        [Test, Description("AC-R25")]
        public void PushMenu_UnconsumedAttack_ClearedEvenAfterPop()
        {
            _input.Step(() => _input.Press(InputActionId.Attack));
            long epoch = _input.Epoch;

            ModeHandle menu = PushNow(InputMode.Menu);
            long afterPush = _input.Epoch;
            _input.Pop(menu);

            Assert.AreEqual(epoch + 1, afterPush);
            Assert.IsFalse(_input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out _));
        }

        [Test, Description("AC-R26")]
        public void PopMenu_SpaceHeld_ReleaseNotRecordedAndNextPressNewEpoch()
        {
            ModeHandle menu = PushNow(InputMode.Menu);
            _input.Step(() =>
            {
                _input.Press(InputActionId.Confirm);
                _input.Press(InputActionId.Jump); // same key, Gameplay map is always enabled
            });

            _input.Pop(menu);
            int before = _input.Records().Count;
            _input.Step(() =>
            {
                _input.Release(InputActionId.Confirm);
                _input.Release(InputActionId.Jump);
            });

            Assert.AreEqual(before, _input.Records().Count, "no Released record");
            Assert.IsFalse(_input.IsHeld(InputActionId.Jump));
            Assert.IsFalse(_input.Records().Any(r => r.Action == InputActionId.Jump));
            _input.Step(() => _input.Press(InputActionId.Jump));
            InputRecord jump = _input.Records().Last();
            Assert.AreEqual(InputActionId.Jump, jump.Action);
            Assert.AreEqual(_input.Epoch, jump.Epoch);
        }

        [Test, Description("AC-R27")]
        public void PopMenu_DHeld_MoveReadImmediately()
        {
            ModeHandle menu = PushNow(InputMode.Menu);
            _input.Step(() => _input.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse));

            _input.Pop(menu);

            Assert.Greater(_input.ReadMove().X, 0f);
        }

        [Test, Description("AC-R28")]
        public void DeviceLostHandlerPushes_AttackSameStep_PushDeferredAndAttackCleared()
        {
            _input.DeviceLost += () => PushNow(InputMode.Menu);
            _input.Step(() => _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Button, 1f));
            InputMode topBeforeEnd = InputMode.Locked;
            long epoch = _input.Epoch;

            _input.BeginStep();
            _input.OnDeviceRemoved(DeviceKind.Gamepad, PadA);
            _input.Press(InputActionId.Attack);
            topBeforeEnd = _input.TopMode;
            _input.EndStep();

            Assert.AreEqual(InputMode.Gameplay, topBeforeEnd);
            Assert.AreEqual(InputMode.Menu, _input.TopMode);
            Assert.AreEqual(epoch, _input.Records().Single().Epoch);
            Assert.AreEqual(epoch + 1, _input.Epoch);
        }

        [Test, Description("AC-R29")]
        public void DeviceChanged_SubThresholdSignals_NeverRaised()
        {
            int changes = 0;
            _input.DeviceChanged += _ => changes++;
            float belowEff = InputMath.StickSwitchEffective(_input.Config.StickSwitchThreshold, _input.Config.DeadZone) - 0.01f;

            _input.Step(() =>
            {
                _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Stick, belowEff);
                _input.OnDeviceSignal(DeviceKind.KeyboardMouse, MouseId, SignalKind.MouseMove, 100f); // Gameplay
            });

            Assert.AreEqual(0, changes);
        }

        [Test, Description("AC-R29")]
        public void DeviceChanged_KeyboardAndGamepadSameStep_OnceAndLastWins()
        {
            var changes = new List<DeviceKind>();
            _input.DeviceChanged += d => changes.Add(d);

            _input.Step(() =>
            {
                _input.OnDeviceSignal(DeviceKind.KeyboardMouse, KeyboardId, SignalKind.Button, 1f);
                _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Button, 1f);
            });

            CollectionAssert.AreEqual(new[] { DeviceKind.Gamepad }, changes);
        }

        [Test, Description("AC-R30")]
        public void Active_AtStartup_DependsOnConnectedGamepad()
        {
            Assert.AreEqual(DeviceKind.Gamepad, Create(_clock, null, gamepadConnected: true).Active);
            Assert.AreEqual(DeviceKind.KeyboardMouse, Create(_clock, null, gamepadConnected: false).Active);
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        [Description("AC-R33")]
        public void RemoveRecentGamepad_JumpHeld_LatchedReleasedLostAndStackUnchanged(bool spaceAlsoHeld, bool expectHeld)
        {
            int lost = 0;
            _input.DeviceLost += () => lost++;
            _input.Step(() =>
            {
                _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Button, 1f);
                _input.Press(InputActionId.Jump, DeviceKind.Gamepad, PadA);
                if (spaceAlsoHeld) _input.Press(InputActionId.Jump);
            });
            InputRecord press = _input.Records().First();
            int before = _input.Records().Count;

            _input.Step(() => _input.OnDeviceRemoved(DeviceKind.Gamepad, PadA));

            Assert.AreEqual(expectHeld, _input.IsHeld(InputActionId.Jump));
            Assert.IsTrue(_input.WasReleasedAfter(InputActionId.Jump, press.Seq));
            Assert.AreEqual(before, _input.Records().Count, "no Released record");
            Assert.AreEqual(DeviceKind.KeyboardMouse, _input.Active);
            Assert.AreEqual(1, lost);
            Assert.AreEqual(InputMode.Gameplay, _input.TopMode);
        }

        [Test, Description("AC-R34")]
        public void FocusLostThenRegainedWithClick_LatchedRepeatClearedClickIgnored()
        {
            var input = Create(_clock, _log, gamepadConnected: true);
            ModeHandle menu = input.Push(InputMode.Menu, Owner(), Alive);
            input.Step(() =>
            {
                input.Press(InputActionId.Confirm);
                input.OnAxisSample(AxisId.Navigate, Right, DeviceKind.Gamepad);
            });
            bool repeatingBefore = input.TryGetNavigateStep(out _);

            input.OnFocusChanged(false);
            bool held = input.IsHeld(InputActionId.Confirm);
            bool repeatingAfter = input.TryGetNavigateStep(out _);
            input.Pop(menu);
            int before = input.Records().Count;
            input.Step(() =>
            {
                input.OnFocusChanged(true);
                input.OnDeviceSignal(DeviceKind.KeyboardMouse, MouseId, SignalKind.MouseClick, 1f);
                input.Press(InputActionId.Attack, DeviceKind.KeyboardMouse, MouseId, pointer: true);
            });

            Assert.IsTrue(repeatingBefore);
            Assert.IsFalse(held);
            Assert.IsFalse(repeatingAfter);
            Assert.AreEqual(before, input.Records().Count, "click produced no Attack");
            Assert.AreEqual(DeviceKind.Gamepad, input.Active);
        }

        [Test, Description("AC-R35")]
        public void FirstFrame_SpaceHeldAtStartup_EmptyEpochZeroAndLatchedUntilReleased()
        {
            _input.AddLatchedHold(InputActionId.Jump, KeyboardId);

            _input.Step();
            bool heldFirstFrame = _input.IsHeld(InputActionId.Jump);
            _input.Step(() => _input.Release(InputActionId.Jump));
            int afterRelease = _input.Records().Count;
            _input.Step(() => _input.Press(InputActionId.Jump));

            Assert.AreEqual(0, _input.Epoch);
            Assert.IsFalse(heldFirstFrame);
            Assert.AreEqual(0, afterRelease);
            Assert.AreEqual(1, _input.Records().Count);
        }

        // ------------------------------------------------------------ H.2 modes

        private static IEnumerable<InputMode> Modes() =>
            new[] { InputMode.Gameplay, InputMode.Menu, InputMode.Dialogue, InputMode.Fishing, InputMode.Map, InputMode.Locked };

        [TestCaseSource(nameof(Modes)), Description("AC-M01, AC-M02")]
        public void Press_EveryAction_OnlyTopModeActionsRecorded(InputMode mode)
        {
            if (mode != InputMode.Gameplay) PushNow(mode);

            _input.Step(() =>
            {
                for (var a = InputActionId.Jump; a < InputActionId.Count; a++) _input.Press(a);
            });

            var recorded = new HashSet<InputActionId>(_input.Records().Select(r => r.Action));
            for (var a = InputActionId.Jump; a < InputActionId.Count; a++)
                Assert.AreEqual(ActionCatalog.IsInMode(a, mode), recorded.Contains(a), $"{a} in {mode}");
        }

        [Test, Description("AC-M03")]
        public void Dialogue_ConfirmWithEThenPop_NoInteractOrAttackForTenFrames()
        {
            ModeHandle dialogue = PushNow(InputMode.Dialogue);
            _input.Step(() =>
            {
                _input.Press(InputActionId.Confirm);
                _input.Press(InputActionId.Interact); // E is bound in both maps
            });
            Assert.IsTrue(_input.TryConsume(InputActionId.Confirm, _window, ClockKind.Real, out _));
            _input.Pop(dialogue);

            for (int frame = 0; frame < 10; frame++)
            {
                _clock.Advance(16_667);
                _input.Step(() =>
                {
                    if (frame != 3) return;
                    _input.Release(InputActionId.Confirm);
                    _input.Release(InputActionId.Interact);
                });
            }

            Assert.IsFalse(_input.Records().Any(r => r.Action == InputActionId.Interact || r.Action == InputActionId.Attack));
        }

        [Test, Description("AC-M04")]
        public void Fishing_EscThenMenuOnTop_CancelOnlyAndFishingGetsNothingUntilPop()
        {
            PushNow(InputMode.Fishing);
            _input.Step(() =>
            {
                _input.Press(InputActionId.Pause);  // Esc fires both actions
                _input.Press(InputActionId.Cancel);
            });
            bool pause = _input.TryConsume(InputActionId.Pause, _window, ClockKind.Real, out _);
            bool cancel = _input.TryConsume(InputActionId.Cancel, _window, ClockKind.Real, out _);
            ModeHandle menu = PushNow(InputMode.Menu); // e.g. Menu & Navigation reacting to DeviceLost

            _input.Step(() =>
            {
                _input.Press(InputActionId.Reel);
                _input.Press(InputActionId.Cancel, DeviceKind.Gamepad, PadA);
            });
            _input.Pop(menu);

            Assert.IsFalse(pause);
            Assert.IsTrue(cancel);
            Assert.IsFalse(_input.TryConsume(InputActionId.Reel, _window, ClockKind.Real, out _));
            Assert.IsFalse(_input.TryConsume(InputActionId.Cancel, _window, ClockKind.Real, out _));
        }

        [TestCaseSource(nameof(Modes)), Description("AC-M06 (I-8)")]
        public void EscPressed_EachMode_PauseOnlyInGameplayCancelInMenus(InputMode mode)
        {
            if (mode != InputMode.Gameplay) PushNow(mode);

            _input.Step(() =>
            {
                _input.Press(InputActionId.Pause);  // the fixed key is bound to both
                _input.Press(InputActionId.Cancel);
            });

            List<InputActionId> recorded = _input.Records().Select(r => r.Action).ToList();
            InputActionId[] expected = mode == InputMode.Gameplay ? new[] { InputActionId.Pause }
                : mode == InputMode.Locked ? new InputActionId[0]
                : new[] { InputActionId.Cancel };
            CollectionAssert.AreEqual(expected, recorded);
        }

        [Test, Description("AC-M05")]
        public void PopNonTop_HeldButton_EpochUnchangedAndNotLatched()
        {
            ModeHandle fishing = PushNow(InputMode.Fishing);
            PushNow(InputMode.Menu);
            _input.Step(() => _input.Press(InputActionId.Confirm));
            long epoch = _input.Epoch;

            _input.Pop(fishing);

            Assert.AreEqual(epoch, _input.Epoch);
            Assert.IsTrue(_input.IsHeld(InputActionId.Confirm));
        }

        // ------------------------------------------------------------ H.3 formulas

        [Test, Description("AC-F02")]
        public void Peek_RealClockHitstopAtPress_WindowEndsAtRealWindow()
        {
            var clock = new FrameClock();
            clock.Advance(0);
            var input = Create(clock);
            input.Step(() => input.Press(InputActionId.Jump));
            clock.PushScale(0.05, Owner(), Alive, Ticks.FromSeconds(0.07));

            clock.Advance(_window);
            bool atEdge = input.Peek(InputActionId.Jump, _window, ClockKind.Real, out _);
            clock.Advance(_window + 1);
            bool past = input.Peek(InputActionId.Jump, _window, ClockKind.Real, out _);

            Assert.IsTrue(atEdge);
            Assert.IsFalse(past);
        }

        [Test, Description("AC-F03")]
        public void Peek_ScaledClockHitstopAtPress_WindowEndsAtReal01865()
        {
            var clock = new FrameClock();
            clock.Advance(0);
            var input = Create(clock);
            input.Step(() => input.Press(InputActionId.Jump));
            clock.PushScale(0.05, Owner(), Alive, Ticks.FromSeconds(0.07));
            long edge = Ticks.FromSeconds(0.1865);

            clock.Advance(edge);
            bool atEdge = input.Peek(InputActionId.Jump, _window, ClockKind.Scaled, out _);
            clock.Advance(edge + 1);
            bool past = input.Peek(InputActionId.Jump, _window, ClockKind.Scaled, out _);

            Assert.AreEqual(_window, clock.ScaledNow - 1);
            Assert.IsTrue(atEdge);
            Assert.IsFalse(past);
        }

        [Test, Description("AC-F04")]
        public void Peek_ScaledWindowLongHitstop_CutByRetention()
        {
            long window = Ticks.FromSeconds(0.15);
            long t0 = _clock.RealNow;
            long s0 = _clock.ScaledNow;
            _input.Step(() => _input.Press(InputActionId.Dodge));

            _clock.RealNow = t0 + Retention;
            _clock.ScaledNow = s0 + Ticks.FromSeconds(0.05);
            bool atRetention = _input.Peek(InputActionId.Dodge, window, ClockKind.Scaled, out _);
            _clock.RealNow = t0 + Retention + 1;
            bool past = _input.Peek(InputActionId.Dodge, window, ClockKind.Scaled, out _);

            Assert.IsTrue(atRetention);
            Assert.IsFalse(past);
        }

        [Test, Description("AC-F05")]
        public void TryConsume_OldestBelongsToOldEpoch_ReturnsOldestOfCurrentEpoch()
        {
            _input.Step(() => _input.Press(InputActionId.Attack));
            _input.ApplyBarrier();
            _input.Step(() =>
            {
                _input.Release(InputActionId.Attack);
                _input.Press(InputActionId.Attack);
                _input.Release(InputActionId.Attack);
                _input.Press(InputActionId.Attack);
            });
            long expected = _input.Records().First(r => r.Phase == InputPhase.Pressed && r.Epoch == _input.Epoch).Seq;

            _input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out InputRecord r1);

            Assert.AreEqual(expected, r1.Seq);
        }

        [TestCase(0.15, ClockKind.Scaled, 0L, false)]
        [TestCase(1.0, ClockKind.Real, 0L, false)]
        [TestCase(1.0, ClockKind.Real, 1L, true)]
        [TestCase(0.81, ClockKind.Scaled, 0L, true)]
        [Description("AC-F06")]
        public void RegisterConsumer_F2Cases_WarnAsExpected(double seconds, ClockKind clock, long extraTicks, bool warn)
        {
            _input.RegisterConsumer(Owner(), InputActionId.Dodge, Ticks.FromSeconds(seconds) + extraTicks, clock);

            Assert.AreEqual(warn ? 1 : 0, _log.Count("Input.WindowExceedsRetention"));
        }

        [Test, Description("AC-F08")]
        public void Press_KeyboardDiagonalDown_JumpStaysJumpAttackBecomesPlunge()
        {
            _input.Step(() => _input.OnAxisSample(AxisId.Move, new Vec2(0.70710678f, -0.70710678f), DeviceKind.KeyboardMouse));

            _input.Step(() =>
            {
                _input.Press(InputActionId.Jump);
                _input.Press(InputActionId.Attack);
            });

            List<InputRecord> r = _input.Records();
            Assert.AreEqual(InputActionId.Jump, r[0].Action);
            Assert.AreEqual(InputActionId.Plunge, r[1].Action);
        }

        [Test, Description("AC-F12 (service part)")]
        public void SetDeadZone_AboveMax_ClampedAndStickThresholdFollows()
        {
            _input.SetDeadZone(0.5f);

            _input.Step(() => _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Stick, 0.35f));
            DeviceKind afterDrift = _input.Active;
            _input.Step(() => _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Stick, 0.55f));

            Assert.AreEqual(0.4f, _input.Config.DeadZone, 1e-6f);
            Assert.AreEqual(DeviceKind.KeyboardMouse, afterDrift);
            Assert.AreEqual(DeviceKind.Gamepad, _input.Active);
        }

        // ------------------------------------------------------------ H.5 edge cases

        [Test, Description("AC-E01")]
        public void LongStall_ThreePressesDuringStall_EligibleInOrderAndOldExpired()
        {
            _input.Step(() => _input.Press(InputActionId.Attack));
            _input.Step(() => _input.Release(InputActionId.Attack));
            _clock.Advance(Ticks.FromSeconds(1.5));

            _input.Step(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    _input.Press(InputActionId.Attack);
                    _input.Release(InputActionId.Attack);
                }
            });

            var seqs = new List<long>();
            while (_input.TryConsume(InputActionId.Attack, Retention, ClockKind.Real, out InputRecord r))
            {
                Assert.AreEqual(_clock.RealNow, r.TReal);
                seqs.Add(r.Seq);
            }
            Assert.AreEqual(3, seqs.Count);
            CollectionAssert.IsOrdered(seqs);
        }

        [Test, Description("AC-E02")]
        public void PressAndReleaseSameFrame_ConsumeReleasedAfterNotHeld()
        {
            _input.Step(() =>
            {
                _input.Press(InputActionId.Jump);
                _input.Release(InputActionId.Jump);
            });

            bool consumed = _input.TryConsume(InputActionId.Jump, _window, ClockKind.Real, out InputRecord r);

            Assert.IsTrue(consumed);
            Assert.AreEqual(InputPhase.Pressed, r.Phase);
            Assert.IsTrue(_input.WasReleasedAfter(InputActionId.Jump, r.Seq));
            Assert.IsFalse(_input.IsHeld(InputActionId.Jump));
        }

        [Test, Description("AC-E03")]
        public void PushAndPopInsideStep_EpochPlusTwo()
        {
            long epoch = _input.Epoch;

            _input.Step(() =>
            {
                ModeHandle h = PushNow(InputMode.Menu);
                _input.Pop(h);
            });

            Assert.AreEqual(epoch + 2, _input.Epoch);
            Assert.AreEqual(InputMode.Gameplay, _input.TopMode);
        }

        [Test, Description("AC-E04")]
        public void LockedOverHeldReel_NotHeldAtPushAndStillLatchedAfterPop()
        {
            PushNow(InputMode.Fishing);
            _input.Step(() => _input.Press(InputActionId.Reel));
            int before = _input.Records().Count;

            ModeHandle locked = PushNow(InputMode.Locked);
            bool heldDuringLock = _input.IsHeld(InputActionId.Reel);
            _input.Pop(locked);
            bool heldAfterPop = _input.IsHeld(InputActionId.Reel);
            _input.Step(() => _input.Release(InputActionId.Reel));

            Assert.IsFalse(heldDuringLock);
            Assert.IsFalse(heldAfterPop);
            Assert.AreEqual(before, _input.Records().Count, "no Released record");
        }

        [Test, Description("AC-X06 (core part) / Rule 9 I-6")]
        public void ToggleReel_MenuCoversFishing_HiddenThenRestored()
        {
            _input.SetHoldMode(InputActionId.Reel, HoldMode.Toggle);
            PushNow(InputMode.Fishing);
            _input.Step(() => _input.Press(InputActionId.Reel));

            ModeHandle menu = PushNow(InputMode.Menu);
            bool duringPause = _input.IsHeld(InputActionId.Reel);
            _input.Pop(menu);

            Assert.IsFalse(duringPause);
            Assert.IsTrue(_input.IsHeld(InputActionId.Reel));
        }

        [Test, Description("AC-E05")]
        public void SetChordModeSingle_PlungeChordBuffered_NoLongerConsumable()
        {
            _input.Step(() =>
            {
                _input.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse);
                _input.Press(InputActionId.Attack);
            });

            _input.SetChordMode(InputActionId.Plunge, false);

            Assert.IsFalse(_input.TryConsume(InputActionId.Plunge, _window, ClockKind.Real, out _));
            Assert.IsFalse(_input.TryConsume(InputActionId.Attack, _window, ClockKind.Real, out _));
        }

        [Test, Description("AC-E07")]
        public void RemoveOtherGamepad_TwoPads_OnlyItsButtonsLatchedNoDeviceLost()
        {
            int lost = 0;
            _input.DeviceLost += () => lost++;
            _input.Step(() =>
            {
                _input.OnDeviceSignal(DeviceKind.Gamepad, PadA, SignalKind.Button, 1f);
                _input.Press(InputActionId.Jump, DeviceKind.Gamepad, PadA);
                _input.OnDeviceSignal(DeviceKind.Gamepad, PadB, SignalKind.Button, 1f);
                _input.Press(InputActionId.Attack, DeviceKind.Gamepad, PadB);
            });

            _input.Step(() => _input.OnDeviceRemoved(DeviceKind.Gamepad, PadA));

            Assert.AreEqual(0, lost);
            Assert.IsFalse(_input.IsHeld(InputActionId.Jump));
            Assert.IsTrue(_input.IsHeld(InputActionId.Attack));
            Assert.AreEqual(DeviceKind.Gamepad, _input.Active);
        }

        [Test, Description("Rule 12: released then pressed again in one frame")]
        public void LatchedButtonReleasedAndPressedSameFrame_PressRecordedInNewEpoch()
        {
            _input.Step(() => _input.Press(InputActionId.Jump));
            ModeHandle menu = PushNow(InputMode.Menu);
            _input.Pop(menu);

            _input.Step(() =>
            {
                _input.Release(InputActionId.Jump);
                _input.Press(InputActionId.Jump);
            });

            Assert.IsTrue(_input.TryConsume(InputActionId.Jump, _window, ClockKind.Real, out InputRecord r));
            Assert.AreEqual(_input.Epoch, r.Epoch);
        }

        [Test, Description("Button disambiguation: canceled reports the last control, possibly from another device")]
        public void Release_FromDeviceThatNeverPressed_ReleasesTheAction()
        {
            _input.Step(() => _input.Press(InputActionId.Jump, DeviceKind.Gamepad, PadA));

            _input.Step(() => _input.Release(InputActionId.Jump)); // keyboard Space was pressed second, so no performed for it

            Assert.IsFalse(_input.IsHeld(InputActionId.Jump));
            Assert.AreEqual(InputPhase.Released, _input.Records().Last().Phase);
        }

        [Test, Description("AC-R40 (core part)")]
        public void NewInstance_AfterOld_SeqAndEpochResetAndOldHandlesRejected()
        {
            ModeHandle oldHandle = PushNow(InputMode.Menu);
            _input.Step(() => _input.Press(InputActionId.Confirm));

            var fresh = Create(_clock, _log);

            Assert.AreEqual(0, fresh.Epoch);
            Assert.AreEqual(1, fresh.NextSeq);
            Assert.IsFalse(fresh.Pop(oldHandle));
        }
    }
}

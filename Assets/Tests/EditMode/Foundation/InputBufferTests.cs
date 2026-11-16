using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class InputBufferTests
    {
        private static InputBuffer Create() =>
            new InputBuffer(InputFixture.DefaultConfig().BufferCapacity, InputFixture.Retention);

        private static InputRecord Pressed(InputActionId a, long t) =>
            new InputRecord { Action = a, Phase = InputPhase.Pressed, TReal = t, TScaled = t };

        private static InputRecord Released(InputActionId a, long t) =>
            new InputRecord { Action = a, Phase = InputPhase.Released, TReal = t, TScaled = t };

        [Test, Description("AC-F01")]
        public void Find_WindowBoundary_InclusiveThenExcluded()
        {
            var buffer = Create();
            long window = Ticks.FromSeconds(0.125);
            buffer.Add(Pressed(InputActionId.Jump, 0), 0);

            bool atEdge = buffer.Find(InputActionId.Jump, window, ClockKind.Real, window, window, 0, false, out _);
            bool past = buffer.Find(InputActionId.Jump, window, ClockKind.Real, window + 1, window + 1, 0, false, out _);

            Assert.IsTrue(atEdge);
            Assert.IsFalse(past);
        }

        [Test, Description("AC-E06")]
        public void Find_After18Hours_BoundaryUnchanged()
        {
            var buffer = Create();
            long t0 = Ticks.FromSeconds(64_800);
            long window = Ticks.FromSeconds(0.125);
            buffer.Add(Pressed(InputActionId.Jump, t0), t0);

            bool atEdge = buffer.Find(InputActionId.Jump, window, ClockKind.Real, t0 + window, t0 + window, 0, false, out _);
            bool past = buffer.Find(InputActionId.Jump, window, ClockKind.Real, t0 + window + 1, t0 + window + 1, 0, false, out _);

            Assert.IsTrue(atEdge);
            Assert.IsFalse(past);
        }

        [Test, Description("AC-F07")]
        public void Add_MashingWithinRetention_CountsOverwritesBeforeExpiry()
        {
            long gap = Ticks.FromSeconds(1.0 / 17.0);

            var fifteen = Create();
            for (int i = 0; i < 15; i++)
            {
                fifteen.Add(Pressed(InputActionId.Attack, i * gap), i * gap);
                fifteen.Add(Released(InputActionId.Attack, i * gap), i * gap);
            }
            var seventeen = Create();
            for (int i = 0; i < 17; i++)
            {
                seventeen.Add(Pressed(InputActionId.Attack, i * gap), i * gap);
                seventeen.Add(Released(InputActionId.Attack, i * gap), i * gap);
            }

            Assert.AreEqual(0, fifteen.OverwrittenBeforeExpiry);
            Assert.AreEqual(2, seventeen.OverwrittenBeforeExpiry);
        }

        [Test, Description("AC-R16")]
        public void WasReleasedAfter_ReleaseRecordOverwritten_StillTrue()
        {
            var buffer = Create();
            long pressSeq = buffer.Add(Pressed(InputActionId.Jump, 0), 0);
            buffer.Add(Released(InputActionId.Jump, 10), 10);

            for (int i = 0; i < 40; i++) buffer.Add(Pressed(InputActionId.Attack, 20), 20);

            Assert.IsTrue(buffer.WasReleasedAfter(InputActionId.Jump, pressSeq));
            Assert.IsFalse(buffer.WasReleasedAfter(InputActionId.Attack, 0));
        }

        [Test, Description("AC-R12")]
        public void Snapshot_After100Events_NeverExceedsCapacityAndExpiredUnreachable()
        {
            var buffer = Create();
            long step = InputFixture.Retention / 20;
            for (int i = 0; i < 100; i++) buffer.Add(Pressed(InputActionId.Attack, i * step), i * step);
            long now = 99 * step;
            var snapshot = new InputRecord[128];
            int n = buffer.Snapshot(snapshot);
            int fresh = 0;
            for (int i = 0; i < n; i++)
            {
                if (now - snapshot[i].TReal <= InputFixture.Retention) fresh++;
            }

            int returned = 0;
            while (buffer.Find(InputActionId.Attack, long.MaxValue, ClockKind.Real, now, now, 0, true, out InputRecord r))
            {
                Assert.LessOrEqual(now - r.TReal, InputFixture.Retention);
                returned++;
            }

            Assert.LessOrEqual(n, buffer.Capacity);
            Assert.Less(fresh, n, "fixture must leave expired records in the ring");
            Assert.AreEqual(fresh, returned);
        }

        [Test]
        public void ConvertToChord_ConsumedRecord_Refused()
        {
            var buffer = Create();
            long seq = buffer.Add(Pressed(InputActionId.Attack, 0), 0);
            buffer.Find(InputActionId.Attack, 0, ClockKind.Real, 0, 0, 0, true, out _);

            Assert.IsFalse(buffer.ConvertToChord(seq, InputActionId.Plunge, InputFixture.Down));
        }
    }
}

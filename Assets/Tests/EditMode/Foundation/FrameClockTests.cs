using Evermeadow.Foundation.Time;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class FrameClockTests
    {
        private static readonly object Owner = new object();
        private static bool AliveTrue() => true;

        [Test]
        public void Advance_FirstCall_AnchorsWithZeroDelta()
        {
            var clock = new FrameClock();

            clock.Advance(5_000_000);

            Assert.AreEqual(5_000_000, clock.RealNow);
            Assert.AreEqual(0, clock.ScaledNow);
            Assert.AreEqual(0, clock.RealDelta);
            Assert.AreEqual(0, clock.ScaledDelta);
        }

        [Test]
        public void Advance_NoScale_ScaledMatchesReal()
        {
            var clock = new FrameClock();
            clock.Advance(0);

            clock.Advance(16_667);

            Assert.AreEqual(16_667, clock.RealDelta);
            Assert.AreEqual(16_667, clock.ScaledDelta);
            Assert.AreEqual(1.0, clock.Scale);
        }

        [Test, Description("AC-R11")]
        public void Advance_SourceRunsBackwards_ClampsAndLogsOncePerSession()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);
            clock.Advance(0);
            clock.Advance(100_000);

            clock.Advance(95_000);
            long afterFirst = clock.RealNow;
            clock.Advance(90_000);

            Assert.AreEqual(100_000, afterFirst);
            Assert.AreEqual(100_000, clock.RealNow);
            Assert.AreEqual(0, clock.RealDelta);
            Assert.AreEqual(1, log.Count("FrameClock.Backwards"));
        }

        [Test, Description("CC OQ-C3: a hit-stop expiring mid-frame splits the interval")]
        public void Advance_HitstopExpiresMidFrame_IntegratesBothSegments()
        {
            var clock = new FrameClock();
            clock.Advance(0);
            clock.PushScale(0.05, Owner, AliveTrue, Ticks.FromSeconds(0.07));

            clock.Advance(Ticks.FromSeconds(0.1));

            // 0.07 s at 0.05 + 0.03 s at 1.0 = 3 500 + 30 000 ticks
            Assert.AreEqual(33_500, clock.ScaledDelta);
            Assert.AreEqual(1.0, clock.Scale);
        }

        [Test]
        public void PushScale_TwoOverlapping_CombineByMinimum()
        {
            var clock = new FrameClock();
            clock.Advance(0);
            clock.PushScale(0.5, Owner, AliveTrue);
            clock.PushScale(0.05, Owner, AliveTrue);

            clock.Advance(100_000);

            Assert.AreEqual(0.05, clock.Scale, 1e-12);
            Assert.AreEqual(5_000, clock.ScaledDelta);
        }

        [Test]
        public void Advance_FractionalScale_KeepsRemainderWithoutDrift()
        {
            var clock = new FrameClock();
            clock.Advance(0);
            clock.PushScale(0.5, Owner, AliveTrue);

            for (int i = 1; i <= 10; i++) clock.Advance(i);

            Assert.AreEqual(5, clock.ScaledNow);
        }

        [Test]
        public void Advance_OwnerDead_RemovesScaleAndLogsError()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);
            bool alive = true;
            clock.Advance(0);
            clock.PushScale(0.0, Owner, () => alive);
            alive = false;

            clock.Advance(10_000);

            Assert.AreEqual(10_000, clock.ScaledDelta);
            Assert.AreEqual(1, log.Count("FrameClock.DeadOwner"));
        }

        [Test]
        public void PopScale_StaleHandle_ReturnsFalseAndWarns()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);
            ScaleHandle h = clock.PushScale(0.0, Owner, AliveTrue);
            clock.PopScale(h);
            clock.PushScale(0.5, Owner, AliveTrue); // reuses the slot

            bool popped = clock.PopScale(h);

            Assert.IsFalse(popped);
            Assert.AreEqual(1, log.Count("FrameClock.StaleHandle"));
            Assert.AreEqual(0.5, clock.Scale);
        }

        [Test]
        public void PopScale_HandleOfOtherClock_ReturnsFalseAndWarns()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);
            ScaleHandle foreign = new FrameClock().PushScale(0.0, Owner, AliveTrue);

            Assert.IsFalse(clock.PopScale(foreign));
            Assert.IsFalse(clock.PopScale(default));
            Assert.AreEqual(2, log.Count("FrameClock.ForeignHandle"));
        }

        [Test]
        public void PushScale_OutOfRange_RefusedWithError()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);

            ScaleHandle negative = clock.PushScale(-0.1, Owner, AliveTrue);
            ScaleHandle nan = clock.PushScale(double.NaN, Owner, AliveTrue);

            Assert.IsFalse(negative.IsValid);
            Assert.IsFalse(nan.IsValid);
            Assert.AreEqual(2, log.Count("FrameClock.BadScale"));
        }

        [Test]
        public void PushScale_Full_RefusedWithError()
        {
            var log = new RecordingLogSink();
            var clock = new FrameClock(log);
            for (int i = 0; i < FrameClock.Capacity; i++) clock.PushScale(1.0, Owner, AliveTrue);

            ScaleHandle extra = clock.PushScale(1.0, Owner, AliveTrue);

            Assert.IsFalse(extra.IsValid);
            Assert.AreEqual(1, log.Count("FrameClock.Full"));
        }
    }
}

using System;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class InputMathTests
    {
        private const float Eps = 1e-6f;
        private static readonly InputConfig Cfg = InputFixture.DefaultConfig();

        private static Vec2 Polar(float magnitude, float degreesFromDown)
        {
            double r = degreesFromDown * Math.PI / 180.0;
            return new Vec2((float)(Math.Sin(r) * magnitude), (float)(-Math.Cos(r) * magnitude));
        }

        [Test, Description("AC-F11")]
        public void ApplyDeadZone_BoundaryAndExampleValues_MatchF4()
        {
            const float dz = 0.2f;
            Assert.AreEqual(0f, InputMath.ApplyDeadZone(new Vec2(0.15f, 0f), dz).Magnitude, Eps);
            Assert.AreEqual(0f, InputMath.ApplyDeadZone(new Vec2(0.2f, 0f), dz).Magnitude, Eps);
            Assert.AreEqual(0.00125f, InputMath.ApplyDeadZone(new Vec2(0.201f, 0f), dz).Magnitude, Eps);

            Vec2 example = InputMath.ApplyDeadZone(new Vec2(0.3f, -0.4f), dz);
            Assert.AreEqual(0.225f, example.X, Eps);
            Assert.AreEqual(-0.3f, example.Y, Eps);

            Assert.AreEqual(1f, InputMath.ApplyDeadZone(new Vec2(1f, 1f) * (1.41f / 1.41421356f), dz).Magnitude, Eps);
        }

        [Test, Description("AC-F08")]
        public void IsDown_KeyboardDiagonal_DropFalsePlungeTrue()
        {
            var m = new Vec2(0.70710678f, -0.70710678f);

            Assert.IsFalse(InputMath.IsDown(m, Cfg.DownThreshold, Cfg.DownAngleDropDegrees));
            Assert.IsTrue(InputMath.IsDown(m, Cfg.DownThreshold, Cfg.DownAnglePlungeDegrees));
        }

        [Test, Description("AC-F09")]
        public void IsDown_DropThroughBoundaries_MatchF3()
        {
            float t = Cfg.DownThreshold;
            float a = Cfg.DownAngleDropDegrees;

            Assert.IsTrue(InputMath.IsDown(new Vec2(0f, -0.5f), t, a));
            Assert.IsFalse(InputMath.IsDown(new Vec2(0f, -0.49f), t, a));
            Assert.IsFalse(InputMath.IsDown(Vec2.Zero, t, a));
            Assert.IsFalse(InputMath.IsDown(Polar(0.60f, 35f), t, a));
        }

        [Test, Description("AC-F10")]
        public void DeadZoneThenDown_Raw30DegreesMagnitude08_IsDownForBoth()
        {
            Vec2 m = InputMath.ApplyDeadZone(Polar(0.8f, 30f), 0.2f);

            Assert.AreEqual(0.75f, m.Magnitude, Eps);
            Assert.IsTrue(InputMath.IsDown(m, Cfg.DownThreshold, Cfg.DownAngleDropDegrees));
            Assert.IsTrue(InputMath.IsDown(m, Cfg.DownThreshold, Cfg.DownAnglePlungeDegrees));
        }

        [Test, Description("AC-F12 (formula part)")]
        public void StickSwitchEffective_DeadZone04_Is055()
        {
            Assert.AreEqual(0.55f, InputMath.StickSwitchEffective(0.5f, 0.4f), Eps);
            Assert.AreEqual(0.5f, InputMath.StickSwitchEffective(0.5f, 0.2f), Eps);
        }

        private static NavRepeater DefaultRepeater() =>
            new NavRepeater(Cfg.NavRepeatDelaySeconds, Cfg.NavRepeatIntervalSeconds, Cfg.NavThreshold);

        [Test, Description("AC-F15")]
        public void NavRepeater_HeldOneSecondAt60Fps_EmitsNineSteps()
        {
            var nav = DefaultRepeater();
            int steps = 0;

            for (int frame = 0; frame <= 60; frame++)
            {
                nav.Update(InputFixture.Right, Ticks.FromSeconds(frame / 60.0));
                if (nav.Step.X != 0f) steps++;
            }

            Assert.AreEqual(9, steps);
        }

        [Test, Description("AC-F15 boundary")]
        public void NavRepeater_DelayBoundary_SecondStepExactlyAtDelay()
        {
            var nav = DefaultRepeater();
            long delay = Ticks.FromSeconds(Cfg.NavRepeatDelaySeconds);
            nav.Update(InputFixture.Right, 0);

            nav.Update(InputFixture.Right, delay - 1);
            bool beforeDelay = nav.Step.X != 0f;
            nav.Update(InputFixture.Right, delay);
            bool atDelay = nav.Step.X != 0f;

            Assert.IsFalse(beforeDelay);
            Assert.IsTrue(atDelay);
        }

        [Test, Description("AC-F16")]
        public void NavRepeater_LongFrame_EmitsOneStepAndDropsExcess()
        {
            var nav = DefaultRepeater();
            nav.Update(InputFixture.Right, 0);

            nav.Update(InputFixture.Right, Ticks.FromSeconds(1.0));
            bool longFrame = nav.Step.X != 0f;
            nav.Update(InputFixture.Right, Ticks.FromSeconds(1.0 + 1.0 / 60.0));
            bool nextFrame = nav.Step.X != 0f;

            Assert.IsTrue(longFrame);
            Assert.IsFalse(nextFrame);
        }

        [Test, Description("AC-F16")]
        public void NavRepeater_QuadrantChange_EmitsImmediatelyAndRestarts()
        {
            var nav = DefaultRepeater();
            nav.Update(InputFixture.Right, 0);
            nav.Update(InputFixture.Right, 100_000);

            nav.Update(InputFixture.Down, 116_667);
            Vec2 changed = nav.Step;
            nav.Update(InputFixture.Down, 116_667 + Ticks.FromSeconds(Cfg.NavRepeatDelaySeconds) - 1);

            Assert.AreEqual(-1f, changed.Y);
            Assert.AreEqual(Vec2.Zero, nav.Step);
        }

        [Test]
        public void NavRepeater_BelowThreshold_NoStep()
        {
            var nav = DefaultRepeater();

            nav.Update(new Vec2(Cfg.NavThreshold - 0.01f, 0f), 0);

            Assert.AreEqual(Vec2.Zero, nav.Step);
        }
    }
}

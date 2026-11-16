using System;
using System.Collections.Generic;
using Evermeadow.Foundation.Input;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class InputConfigTests
    {
        [Test, Description("AC-V01")]
        public void Validate_Retention2WithCapacity32_ResetsBothWithOneViolation()
        {
            var raw = InputFixture.DefaultConfig();
            raw.BufferRetentionSeconds = 2.0;
            raw.BufferCapacity = 32;
            var d = InputFixture.DefaultConfig();

            InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);

            Assert.AreEqual(1, violations.Count);
            Assert.AreEqual(d.BufferRetentionSeconds, c.BufferRetentionSeconds);
            Assert.AreEqual(d.BufferCapacity, c.BufferCapacity);
        }

        [Test, Description("AC-V01")]
        public void Validate_Retention2WithCapacity64_Accepted()
        {
            var raw = InputFixture.DefaultConfig();
            raw.BufferRetentionSeconds = 2.0;
            raw.BufferCapacity = 64;

            InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);

            Assert.AreEqual(0, violations.Count);
            Assert.AreEqual(2.0, c.BufferRetentionSeconds);
            Assert.AreEqual(64, c.BufferCapacity);
        }

        [Test, Description("AC-V02")]
        public void Validate_Capacity48_UsesDefaultWithOneViolation()
        {
            var raw = InputFixture.DefaultConfig();
            raw.BufferCapacity = 48;

            InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);

            Assert.AreEqual(1, violations.Count);
            Assert.AreEqual(InputFixture.DefaultConfig().BufferCapacity, c.BufferCapacity);
        }

        [Test, Description("AC-V03")]
        public void Validate_DropAngle45_UsesDefaultAndKeepsOrdering()
        {
            var raw = InputFixture.DefaultConfig();
            raw.DownAngleDropDegrees = 45f;

            InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);

            Assert.AreEqual(1, violations.Count);
            Assert.AreEqual(InputFixture.DefaultConfig().DownAngleDropDegrees, c.DownAngleDropDegrees);
            Assert.That(c.DownAngleDropDegrees < 45f && c.DownAnglePlungeDegrees >= 45f);
        }

        public sealed class Knob
        {
            public string Name;
            public double Min;
            public double Max;
            public double Epsilon;
            public Action<InputConfig, double> Set;
            public Func<InputConfig, double> Get;
            public Action<InputConfig> Prepare = c => { };
            public override string ToString() => Name;
        }

        private static Knob F(string name, double min, double max, Action<InputConfig, float> set, Func<InputConfig, float> get) =>
            new Knob { Name = name, Min = min, Max = max, Epsilon = 1e-3, Set = (c, v) => set(c, (float)v), Get = c => get(c) };

        private static Knob D(string name, double min, double max, Action<InputConfig, double> set, Func<InputConfig, double> get) =>
            new Knob { Name = name, Min = min, Max = max, Epsilon = 1e-6, Set = set, Get = get };

        // Safe ranges from GDD Input › Tuning Knobs (fixture table).
        private static IEnumerable<Knob> Knobs()
        {
            var retention = D("BufferRetentionSeconds", 0.5, 2.0, (c, v) => c.BufferRetentionSeconds = v, c => c.BufferRetentionSeconds);
            retention.Prepare = c => c.BufferCapacity = 128; // keep the F2 cross-constraint out of a range test
            yield return retention;
            yield return D("HitstopBudgetSeconds", 0.0, 0.5, (c, v) => c.HitstopBudgetSeconds = v, c => c.HitstopBudgetSeconds);
            yield return F("DownThreshold", 0.3, 0.7, (c, v) => c.DownThreshold = v, c => c.DownThreshold);
            yield return F("DownAngleDropDegrees", 25, 44, (c, v) => c.DownAngleDropDegrees = v, c => c.DownAngleDropDegrees);
            yield return F("DownAnglePlungeDegrees", 45, 60, (c, v) => c.DownAnglePlungeDegrees = v, c => c.DownAnglePlungeDegrees);
            yield return F("DeadZone", 0.05, 0.4, (c, v) => c.DeadZone = v, c => c.DeadZone);
            yield return F("TriggerThreshold", 0.15, 0.5, (c, v) => c.TriggerThreshold = v, c => c.TriggerThreshold);
            yield return F("StickSwitchThreshold", 0.4, 0.8, (c, v) => c.StickSwitchThreshold = v, c => c.StickSwitchThreshold);
            yield return F("MouseSwitchPixels", 4, 24, (c, v) => c.MouseSwitchPixels = v, c => c.MouseSwitchPixels);
            yield return D("MouseSwitchSeconds", 0.1, 0.5, (c, v) => c.MouseSwitchSeconds = v, c => c.MouseSwitchSeconds);
            yield return F("NavThreshold", 0.3, 0.7, (c, v) => c.NavThreshold = v, c => c.NavThreshold);
            yield return D("NavRepeatDelaySeconds", 0.25, 0.6, (c, v) => c.NavRepeatDelaySeconds = v, c => c.NavRepeatDelaySeconds);
            yield return D("NavRepeatIntervalSeconds", 0.05, 0.15, (c, v) => c.NavRepeatIntervalSeconds = v, c => c.NavRepeatIntervalSeconds);
        }

        [TestCaseSource(nameof(Knobs)), Description("AC-V04")]
        public void Validate_KnobAtBounds_AcceptedAndOutsideResetWithOneViolation(Knob k)
        {
            double fallback = k.Get(InputFixture.DefaultConfig());
            double[] accepted = { k.Min, k.Max };
            double[] rejected = { k.Min - k.Epsilon, k.Max + k.Epsilon };

            foreach (double v in accepted)
            {
                var raw = InputFixture.DefaultConfig();
                k.Prepare(raw);
                k.Set(raw, v);
                InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);
                Assert.AreEqual(0, violations.Count, $"{k.Name}={v} should be accepted: {string.Join("; ", violations)}");
                Assert.AreEqual(v, k.Get(c), k.Epsilon / 10);
            }

            foreach (double v in rejected)
            {
                var raw = InputFixture.DefaultConfig();
                k.Prepare(raw);
                k.Set(raw, v);
                InputConfig c = InputConfigValidator.Validate(raw, out List<string> violations);
                Assert.AreEqual(1, violations.Count, $"{k.Name}={v} should give exactly one violation");
                Assert.AreEqual(fallback, k.Get(c), 1e-9);
            }
        }

        [Test]
        public void Validate_Null_ReturnsDefaults()
        {
            InputConfig c = InputConfigValidator.Validate(null, out List<string> violations);

            Assert.AreEqual(0, violations.Count);
            Assert.AreEqual(InputFixture.DefaultConfig().BufferCapacity, c.BufferCapacity);
        }
    }
}

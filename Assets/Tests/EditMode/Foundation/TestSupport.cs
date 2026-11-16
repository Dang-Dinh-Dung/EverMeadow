using System.Collections.Generic;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Tests.EditMode.Foundation
{
    /// <summary>Injected clock (test hook H-1): tests set real and scaled time directly.</summary>
    public sealed class FakeClock : IFrameClock
    {
        public long RealNow { get; set; }
        public long ScaledNow { get; set; }
        public long RealDelta => 0;
        public long ScaledDelta => 0;
        public double Scale => 1.0;

        /// <summary>Advances both clocks by the same amount (no hit-stop).</summary>
        public void Advance(long ticks)
        {
            RealNow += ticks;
            ScaledNow += ticks;
        }
    }

    /// <summary>Log sink that counts messages by key (test hook H-6).</summary>
    public sealed class RecordingLogSink : ILogSink
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public int Warnings { get; private set; }
        public int Errors { get; private set; }
        public int Total => Warnings + Errors;

        public void Warn(string key, string message)
        {
            Warnings++;
            Bump(key);
        }

        public void Error(string key, string message)
        {
            Errors++;
            Bump(key);
        }

        public int Count(string key) => _counts.TryGetValue(key, out int n) ? n : 0;

        private void Bump(string key) => _counts[key] = Count(key) + 1;
    }

    /// <summary>Fixture values and helpers shared by Input tests. Defaults come from <see cref="InputConfig"/>, never retyped.</summary>
    public static class InputFixture
    {
        public const int KeyboardId = 1;
        public const int MouseId = 2;
        public const int PadA = 10;
        public const int PadB = 11;

        public static readonly Vec2 Down = new Vec2(0f, -1f);
        public static readonly Vec2 Right = new Vec2(1f, 0f);

        public static InputConfig DefaultConfig() => new InputConfig();

        public static long Retention => Ticks.FromSeconds(DefaultConfig().BufferRetentionSeconds);

        public static InputService Create(IFrameClock clock, ILogSink log = null, bool gamepadConnected = false) =>
            new InputService(clock, DefaultConfig(), log ?? new RecordingLogSink(), gamepadConnected);

        public static void Press(this InputService s, InputActionId a, DeviceKind d = DeviceKind.KeyboardMouse, int id = KeyboardId, bool pointer = false) =>
            s.OnAction(a, InputPhase.Pressed, d, id, pointer);

        public static void Release(this InputService s, InputActionId a, DeviceKind d = DeviceKind.KeyboardMouse, int id = KeyboardId, bool pointer = false) =>
            s.OnAction(a, InputPhase.Released, d, id, pointer);

        /// <summary>Runs one input step with <paramref name="body"/> as the adapter events.</summary>
        public static void Step(this InputService s, System.Action body = null)
        {
            s.BeginStep();
            body?.Invoke();
            s.EndStep();
        }

        public static List<InputRecord> Records(this InputService s)
        {
            var buffer = new InputRecord[256];
            int n = s.SnapshotBuffer(buffer);
            var list = new List<InputRecord>(n);
            for (int i = 0; i < n; i++) list.Add(buffer[i]);
            return list;
        }

        public static object Owner() => new object();

        public static System.Func<bool> Alive => () => true;
    }
}

using Evermeadow.Foundation.Time;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>
    /// Runs the frame prologue before any gameplay <c>Update</c> (ADR-0001 Decision 3):
    /// 1. advance the frame clock and mirror its scale to <c>Time.timeScale</c>; 2. <c>InputSystem.Update()</c> (button callbacks);
    /// 3. sample axes; 4. end the input step. The only writer of <c>Time.timeScale</c> and the only caller of <c>InputSystem.Update()</c>.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class FrameDriver : MonoBehaviour
    {
        private FrameClock _clock;
        private InputService _input;
        private UnityInputAdapter _adapter;
        private UnityRealTimeSource _time;

        /// <summary>Injected by the composition root.</summary>
        public void Init(FrameClock clock, InputService input, UnityInputAdapter adapter, UnityRealTimeSource time)
        {
            _clock = clock;
            _input = input;
            _adapter = adapter;
            _time = time;
        }

        private void Update()
        {
            if (_input == null) return;
            Step(_time.NowTicks);
        }

        /// <summary>One frame prologue at real time <paramref name="realTicks"/> (public for PlayMode tests).</summary>
        public void Step(long realTicks)
        {
            _clock.Advance(realTicks);
            UnityEngine.Time.timeScale = (float)_clock.Scale;
            _input.BeginStep();
            InputSystem.Update();
            _adapter.SampleFrame();
            _input.EndStep();
        }
    }
}

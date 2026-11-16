using Evermeadow.Foundation.Time;
using UnityEngine;

namespace Evermeadow.Foundation.Input.Unity
{
    /// <summary>The project's only real time source (ADR-0001 Decision 2): <c>Time.unscaledTimeAsDouble</c> in ticks.</summary>
    public sealed class UnityRealTimeSource
    {
        /// <summary>Unscaled time of the current frame, in ticks. Constant within a frame.</summary>
        public long NowTicks => Ticks.FromSeconds(UnityEngine.Time.unscaledTimeAsDouble);
    }
}

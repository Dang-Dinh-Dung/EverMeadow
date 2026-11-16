using NUnit.Framework;
using UnityEngine;

namespace Evermeadow.Tests.EditMode.Core
{
    /// <summary>Project Physics2D settings the Character Controller relies on (GDD Character Controller Rule 30).</summary>
    public class Physics2DSettingsTests
    {
        [Test, Description("Physics2D steps right after Update, with the scaled frame delta the FrameDriver sets")]
        public void SimulationMode_IsUpdate()
        {
            Assert.AreEqual(SimulationMode2D.Update, Physics2D.simulationMode);
        }

        [Test, Description("Casts that start inside a collider ignore it (one-way platforms from below, own collider)")]
        public void QueriesStartInColliders_IsOff()
        {
            Assert.IsFalse(Physics2D.queriesStartInColliders);
        }
    }
}

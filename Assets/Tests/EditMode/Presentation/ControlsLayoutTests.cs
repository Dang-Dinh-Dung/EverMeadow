using System.Linq;
using Evermeadow.Foundation.Input;
using Evermeadow.Presentation.Menus;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>The Controls screen lists every rebindable action once (GDD Input Rule 17).</summary>
    public class ControlsLayoutTests
    {
        [Test, Description("Rule 17: every button action except the pointer click has exactly one row")]
        public void Rows_EveryButtonActionOnce()
        {
            var listed = ControlsLayout.Rows.Where(r => !r.IsHeader && r.Part == null).Select(r => r.Action).ToList();

            for (var a = InputActionId.Jump; a < InputActionId.Count; a++)
            {
                int expected = a == InputActionId.Click ? 0 : 1;
                Assert.AreEqual(expected, listed.Count(x => x == a), a.ToString());
            }
        }

        [Test, Description("Move has one row per direction; their gamepad side (stick, D-pad) is fixed")]
        public void Rows_MoveDirections()
        {
            var directions = ControlsLayout.Rows.Where(r => r.Part != null).ToList();

            CollectionAssert.AreEquivalent(new[] { "up", "down", "left", "right" }, directions.Select(r => r.Part));
            Assert.IsTrue(directions.All(r => r.Axis == AxisId.Move && r.FixedGamepadText != null));
        }

        [Test]
        public void Rows_HaveNamesAndStartWithAHeader()
        {
            Assert.IsTrue(ControlsLayout.Rows[0].IsHeader);
            Assert.IsTrue(ControlsLayout.Rows.All(r => !string.IsNullOrWhiteSpace(r.Label)));
        }
    }
}

using Evermeadow.Foundation.Input;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class GlyphNamesTests
    {
        [TestCase("buttonSouth", GlyphFamily.Xbox, "A")]
        [TestCase("buttonSouth", GlyphFamily.PlayStation, "Cross")]
        [TestCase("buttonSouth", GlyphFamily.Auto, "A")]
        [TestCase("rightShoulder", GlyphFamily.Xbox, "RB")]
        [TestCase("rightShoulder", GlyphFamily.PlayStation, "R1")]
        [TestCase("start", GlyphFamily.PlayStation, "Options")]
        [TestCase("select", GlyphFamily.Xbox, "View")]
        [Description("AC-R31 (label table)")]
        public void Gamepad_ControlAndFamily_ReturnsLabel(string control, GlyphFamily family, string expected)
        {
            Assert.AreEqual(expected, GlyphNames.Gamepad(control, family));
        }

        [Test]
        public void Gamepad_UnknownControl_ReturnsControlName()
        {
            Assert.AreEqual("paddle1", GlyphNames.Gamepad("paddle1", GlyphFamily.Xbox));
        }
    }
}

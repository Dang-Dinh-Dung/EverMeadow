using System.Collections.Generic;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>
    /// Skill Tool advice (2026-11-14, from a real skill whose attack never showed): an End reached while an Animation may
    /// still play cuts it back to idle.
    /// </summary>
    public class SkillLintTests
    {
        private static SkillGraphData G(string inner)
        {
            Assert.IsTrue(SkillXmlReader.TryParseData(Body(inner), "100000", out SkillGraphData g, new List<SkillError>()));
            return g;
        }

        private const string Start = "<node id=\"0\" type=\"Start\"/><node id=\"1\" type=\"Animation\"><f n=\"AnimationType\" v=\"attack1\"/></node>";

        [Test]
        public void EndAfterInstantHit_CutsAnimation_Warned()
        {
            // The user's skill: Start → Animation → Effect → Hit (instant) → (end) End: End at 0 s.
            List<string> w = SkillLint.Warnings(G(Start +
                "<node id=\"2\" type=\"Effect\"/><node id=\"3\" type=\"Hit\"/><node id=\"4\" type=\"End\"/>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"2\" when=\"start\"/><link from=\"2\" to=\"3\" when=\"start\"/><link from=\"3\" to=\"4\" when=\"end\"/>"));
            Assert.AreEqual(1, w.Count);
            StringAssert.Contains("End 4", w[0]);
            StringAssert.Contains("0.00 s", w[0]);
            StringAssert.Contains("Animation 1", w[0]);
        }

        [Test]
        public void EndAfterAnimationEnds_NoWarning()
        {
            CollectionAssert.IsEmpty(SkillLint.Warnings(G(Start + "<node id=\"4\" type=\"End\"/>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"4\" when=\"end\"/>")));
        }

        [Test]
        public void EndBeforeAnimationStarts_NoWarning()
        {
            CollectionAssert.IsEmpty(SkillLint.Warnings(G(
                "<node id=\"0\" type=\"Start\"/><node id=\"1\" type=\"Animation\"><f n=\"ActionDelay\" v=\"1\"/></node><node id=\"4\" type=\"End\"><f n=\"ActionDelay\" v=\"0.5\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"4\" when=\"start\"/>")));
        }

        [Test]
        public void EndWithDelay_StillWarnedBecauseClipLengthUnknown()
        {
            List<string> w = SkillLint.Warnings(G(Start + "<node id=\"4\" type=\"End\"><f n=\"ActionDelay\" v=\"0.4\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"4\" when=\"start\"/>"));
            Assert.AreEqual(1, w.Count, "the tool cannot know the clip is shorter than 0.4 s: it reminds, never blocks");
            StringAssert.Contains("0.40 s", w[0]);
        }

        [Test]
        public void EndOnHitOrSearchBranch_TimeUnknown_NoWarning()
        {
            CollectionAssert.IsEmpty(SkillLint.Warnings(G(Start +
                "<node id=\"3\" type=\"Hit\"/><node id=\"4\" type=\"End\"/>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"3\" when=\"start\"/><link from=\"3\" to=\"4\" when=\"hit\"/>")));
        }

        [TestCase("Hit", "180,0,0", null, true)]      // the user's skill: radius 0
        [TestCase("Hit", "0,1.5,0", null, true)]      // angle 0
        [TestCase("Hit", "180,1.5,0", null, false)]
        [TestCase("Search", "360,0,0", null, true)]
        [TestCase("Hit", "1,0,1", "Box", true)]        // length 0
        [TestCase("Hit", "1,2,0", "Box", false)]       // width as height
        [TestCase("Hit", "0,2,0", "Box", true)]        // no width, no height
        public void ZeroSizeArea_Warned(string kind, string sub, string area, bool warned)
        {
            string areaField = area != null ? $"<f n=\"AreaType\" v=\"{area}\"/>" : "";
            List<string> w = SkillLint.Warnings(G($"<node id=\"0\" type=\"Start\"/><node id=\"3\" type=\"{kind}\"><f n=\"SubValue\" v=\"{sub}\"/>{areaField}</node>" +
                                                  "<link from=\"0\" to=\"3\" when=\"start\"/>"));
            Assert.AreEqual(warned ? 1 : 0, w.Count, string.Join("\n", w));
            if (warned) StringAssert.Contains("kích thước 0", w[0]);
        }

        [Test]
        public void ZeroAreaScale_Warned() =>
            Assert.AreEqual(1, SkillLint.Warnings(G("<node id=\"0\" type=\"Start\"/><node id=\"3\" type=\"Hit\"><f n=\"AreaScale\" v=\"0\"/></node><link from=\"0\" to=\"3\" when=\"start\"/>")).Count);

        [Test]
        public void NoEnd_NoWarning() =>
            CollectionAssert.IsEmpty(SkillLint.Warnings(G(Start + "<link from=\"0\" to=\"1\" when=\"start\"/>")));

        [Test]
        public void SteadyHitLongerThanNothing_EndAfterIt_StillWarned()
        {
            List<string> w = SkillLint.Warnings(G(Start +
                "<node id=\"3\" type=\"Hit\"><f n=\"IsSteady\" v=\"true\"/><f n=\"SteadyDuration\" v=\"0.3\"/></node><node id=\"4\" type=\"End\"/>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"3\" when=\"start\"/><link from=\"3\" to=\"4\" when=\"end\"/>"));
            StringAssert.Contains("0.30 s", w[0]);
        }
    }
}

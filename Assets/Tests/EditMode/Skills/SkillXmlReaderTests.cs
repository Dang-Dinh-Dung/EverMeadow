using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>Skill files as event graphs (user decision 2026-11-09): reading, ids, time grid, structural errors.</summary>
    public class SkillXmlReaderTests
    {
        // ------------------------------------------------------------ SkillTime

        [TestCase(0.10, 6), TestCase(0.04, 3), TestCase(0.06, 4), TestCase(0.05, 3), TestCase(0.0, 0), TestCase(0.30, 18)]
        [Description("Rule 23: whole steps round up")]
        public void StepsOf_RoundsUpWithEpsilon(double seconds, int steps) => Assert.AreEqual(steps, SkillTime.StepsOf(seconds));

        [Test, Description("Rule 34: a scaled time that is a whole step must not gain a step from binary error (0.1 × 1.5 × 60 = 9.000000000000002)")]
        public void StepsOf_ScaledWholeStep_DoesNotOvershoot()
        {
            double scale = 1.5, scale2 = 1.75;

            Assert.AreEqual(9, SkillTime.StepsOf(0.1 * scale));
            Assert.AreEqual(21, SkillTime.StepsOf(0.2 * scale2));
        }

        [Test, Description("1/60 s is not a whole number of microseconds: the tick rounds up, so an event never fires early")]
        public void TickOfStep_RoundsUp()
        {
            Assert.AreEqual(0L, SkillTime.TickOfStep(0));
            Assert.AreEqual(16_667L, SkillTime.TickOfStep(1));
            Assert.AreEqual(100_000L, SkillTime.TickOfStep(6));
        }

        // ------------------------------------------------------------ ids (file names)

        [TestCase("100000", true), TestCase("999999", true), TestCase("099999", false), TestCase("10000", false), TestCase("12a456", false), TestCase(null, false)]
        [Description("A skill id is a 6-digit number from 100000 (the file name)")]
        public void IsSkillId_SixDigitsFrom100000(string id, bool valid) => Assert.AreEqual(valid, SkillXmlReader.IsSkillId(id));

        [TestCase("100123.xml", "100123"), TestCase("spear/100123.xml", "100123"), TestCase("a\\b\\100123.XML", "100123")]
        public void IdFromFileName_StripsFolderAndExtension(string name, string id) => Assert.AreEqual(id, SkillXmlReader.IdFromFileName(name));

        [Test, Description("A file whose name is not a skill id is rejected; the id is never an attribute")]
        public void TryParse_BadIdOrIdAttribute_Rejected()
        {
            Assert.IsTrue(Errors(GuardRush, "skl.spear").Any(e => e.Code == "IdFormat"));
            Assert.IsTrue(Errors(GuardRush.Replace("schema_version=\"2\"", "schema_version=\"2\" id=\"100001\"")).Any(e => e.Code == "UnknownAttribute"));
        }

        // ------------------------------------------------------------ reading a graph

        [Test, Description("Nodes, fields, wires, links with their conditions, positions and memos are read")]
        public void TryParse_GuardRush_ReadsWholeGraph()
        {
            SkillDef s = Parse(GuardRush, "100010");

            Assert.IsNotNull(s, string.Join("; ", Errors(GuardRush, "100010")));
            Assert.AreEqual("100010", s.Id);
            Assert.AreEqual(SkillOwner.Player, s.Owner);
            Assert.AreEqual(8, s.Graph.Nodes.Count);
            Assert.AreEqual(SkillNodeKind.Start, s.Start.Kind);
            Assert.AreEqual("Run 02", s.Node(11).Get("AnimationType"));
            Assert.AreEqual("0.1", s.Node(12).Get("ActionDelay"));
            Assert.AreEqual("0", s.Node(11).Get("ActionDelay"), "absent fields take the schema default");
            Assert.AreEqual("6.LastHitCreatureGID", s.Node(15).Refs["TargetCreatureGID"]);
            Assert.AreEqual(new[] { 13, 15 }, s.Children(6).Select(l => l.To).ToArray());
            Assert.IsTrue(s.Children(6).All(l => l.When == SkillTrigger.Hit));
            Assert.AreEqual((900f, 600f), (s.Node(15).X, s.Node(15).Y));
            Assert.AreEqual("Guard Rush", s.Graph.Memos[0].Title);
            StringAssert.Contains("\n", s.Graph.Memos[0].Text);
        }

        [Test, Description("Values are normalised to their canonical text when read")]
        public void TryParse_NormalisesValues()
        {
            string xml = Body(@"<node id=""0"" type=""Start""/><node id=""1"" type=""Search"">
                <f n=""ActionDelay"" v=""0.10""/><f n=""SubValue"" v="" 90, 2.50 ,0""/><f n=""SearchObjectFlag"" v=""Allies|Enemies""/></node>
                <link from=""0"" to=""1""/>");

            SkillNodeData n = Parse(xml).Node(1);

            Assert.AreEqual(("0.1", "90,2.5,0", "Enemies|Allies"), (n.Get("ActionDelay"), n.Get("SubValue"), n.Get("SearchObjectFlag")));
        }

        [Test, Description("A link without when defaults to the parent's start")]
        public void TryParse_LinkDefaultsToStart() => Assert.AreEqual(SkillTrigger.Start, Parse(Skill("100001").Xml).Children(0)[0].When);

        [Test, Description("The Skill Tool can open a broken file: unknown nodes and fields are reported and skipped")]
        public void TryParseData_IsLenient_TryParseIsStrict()
        {
            string xml = Body(@"<node id=""0"" type=""Start""/><node id=""1"" type=""Teleport""/>
                <node id=""2"" type=""Animation""><f n=""Speed"" v=""2""/></node><link from=""0"" to=""2""/>");
            var errors = new List<SkillError>();

            Assert.IsTrue(SkillXmlReader.TryParseData(xml, "100000", out SkillGraphData g, errors));
            Assert.AreEqual(2, g.Nodes.Count);
            CollectionAssert.IsSubsetOf(new[] { "UnknownNode", "UnknownField" }, errors.Select(e => e.Code).ToList());
            Assert.IsNull(Parse(xml));
        }

        // ------------------------------------------------------------ schema and errors

        [TestCase("1", "SchemaTooOld"), TestCase("3", "SchemaTooNew")]
        [Description("Only the graph schema (2) is read")]
        public void TryParse_OtherSchema_Rejected(string version, string code) =>
            Assert.IsTrue(Errors(GuardRush.Replace("schema_version=\"2\"", $"schema_version=\"{version}\"")).Any(e => e.Code == code));

        [Test, Description("Unknown elements and attributes are rejected")]
        public void TryParse_UnknownElementOrAttribute_Rejected()
        {
            Assert.IsTrue(Errors(GuardRush.Replace("<memo ", "<script/><memo ")).Any(e => e.Code == "UnknownNode"));
            Assert.IsTrue(Errors(GuardRush.Replace("owner=\"player\"", "owner=\"player\" kind=\"normal\"")).Any(e => e.Code == "UnknownAttribute"));
        }

        [Test, Description("Parsers never throw on bad data")]
        public void TryParse_GarbageDoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillXmlReader.TryParse("{ not xml", "100000", out _, new List<SkillError>()));
            Assert.IsFalse(SkillXmlReader.TryParse("<skill", "100000", out _, new List<SkillError>()));
            Assert.IsFalse(SkillXmlReader.TryParse(null, "100000", out _, new List<SkillError>()));
            Assert.IsTrue(Errors("<skill").Any(e => e.Code == "XmlUnreadable"));
        }
    }
}

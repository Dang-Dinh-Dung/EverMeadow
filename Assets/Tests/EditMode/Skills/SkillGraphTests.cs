using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>Node schema, graph validation rules and the writer (user decision 2026-11-09: event graph like the reference tool).</summary>
    public class SkillGraphTests
    {
        private const string StartNode = @"<node id=""0"" type=""Start""/>";

        private static List<string> Codes(string inner) => Errors(Body(inner)).Select(e => e.Code).ToList();

        // ------------------------------------------------------------ schema

        [Test, Description("Every node except Start has ActionDelay / CanCancel / UseAttackSpeed and outputs NodeID")]
        public void Schema_CommonBlock()
        {
            foreach (SkillNodeKind k in SkillSchema.Kinds)
            {
                SkillNodeDef d = SkillSchema.Get(k);
                CollectionAssert.AreEqual(new[] { "ActionDelay", "CanCancel", "UseAttackSpeed" }, d.Fields.Take(3).Select(f => f.Name), k.ToString());
                Assert.AreEqual("NodeID", d.Outputs[0].Name, k.ToString());
                if (k == SkillNodeKind.End) CollectionAssert.IsEmpty(d.Triggers, "End has no children (user decision 2026-11-14)");
                else Assert.IsTrue(d.Allows(SkillTrigger.Start) && d.Allows(SkillTrigger.End), k.ToString());
            }
        }

        [Test, Description("Start outputs the cast values of the reference tool")]
        public void Schema_StartOutputs()
        {
            CollectionAssert.AreEqual(new[]
            {
                "SkillID", "SkillLevel", "CastRatio", "SkillInputType", "IsChanneling", "OwnerGID", "OwnerTID", "OwnerPosition",
                "OwnerDirection", "OwnerLevel", "ExternalTargetGID", "ExternalPosition"
            }, SkillSchema.Get(SkillNodeKind.Start).Outputs.Select(o => o.Name));
        }

        [Test, Description("Only Hit offers 'each hit'; only Search offers found / not found")]
        public void Schema_SpecialTriggers()
        {
            Assert.IsTrue(SkillSchema.Get(SkillNodeKind.Hit).Allows(SkillTrigger.Hit));
            Assert.IsTrue(SkillSchema.Get(SkillNodeKind.Search).Allows(SkillTrigger.Found));
            Assert.IsTrue(SkillSchema.Get(SkillNodeKind.Search).Allows(SkillTrigger.NotFound));
            Assert.IsFalse(SkillSchema.Get(SkillNodeKind.Animation).Allows(SkillTrigger.Hit));
            Assert.IsFalse(SkillSchema.Get(SkillNodeKind.Start).Allows(SkillTrigger.End));
        }

        [Test, Description("Every default value is valid for its own field type")]
        public void Schema_DefaultsAreValid()
        {
            foreach (SkillNodeKind k in SkillSchema.Kinds)
            foreach (SkillFieldDef f in SkillSchema.Get(k).Fields)
                Assert.IsTrue(SkillSchema.TryNormalize(f.Type, f.EnumType, f.Default, out string c) && c == f.Default, $"{k}.{f.Name} = {f.Default}");
        }

        [TestCase(SkillFieldType.Int, SkillFieldType.Float, true), TestCase(SkillFieldType.Int, SkillFieldType.Long, true)]
        [TestCase(SkillFieldType.Long, SkillFieldType.Int, false), TestCase(SkillFieldType.Vec3, SkillFieldType.Long, false)]
        [TestCase(SkillFieldType.LongArray, SkillFieldType.Long, false), TestCase(SkillFieldType.Long, SkillFieldType.Long, true)]
        public void Schema_CanAssign(SkillFieldType from, SkillFieldType to, bool ok) => Assert.AreEqual(ok, SkillSchema.CanAssign(from, null, to, null));

        // ------------------------------------------------------------ validation

        [Test, Description("The Guard Rush graph is valid")]
        public void Validate_GuardRush_Clean() => Assert.IsEmpty(Errors(GuardRush));

        [Test]
        public void Validate_StartRules()
        {
            CollectionAssert.Contains(Codes(@"<node id=""1"" type=""Animation""/>"), "MissingStart");
            CollectionAssert.Contains(Codes(@"<node id=""3"" type=""Start""/>"), "StartNotZero");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""0"" type=""Hit""/>"), "DuplicateNodeId");
        }

        [Test, Description("Links: existing nodes, never into Start, one parent, no loops, conditions the parent offers")]
        public void Validate_LinkRules()
        {
            CollectionAssert.Contains(Codes(StartNode + @"<link from=""0"" to=""9""/>"), "BadLink");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""/><link from=""1"" to=""0""/>"), "LinkIntoStart");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""/><node id=""2"" type=""Buff""/>
                <link from=""0"" to=""2""/><link from=""1"" to=""2""/>"), "MultipleParents");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""/><node id=""2"" type=""Buff""/>
                <link from=""1"" to=""2""/><link from=""2"" to=""1""/>"), "Cycle");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Animation""/><node id=""2"" type=""Buff""/>
                <link from=""0"" to=""1""/><link from=""1"" to=""2"" when=""hit""/>"), "BadTrigger");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""/><link from=""0"" to=""1"" when=""end""/>"), "BadTrigger");
        }

        [Test, Description("Values must parse as their field type")]
        public void Validate_BadValues()
        {
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""><f n=""ActionDelay"" v=""soon""/></node>"), "BadValue");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""><f n=""AreaType"" v=""Cone""/></node>"), "BadValue");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""><f n=""SubValue"" v=""1,2""/></node>"), "BadValue");
            CollectionAssert.Contains(Codes(StartNode + @"<node id=""1"" type=""Hit""><f n=""IsSteady"" v=""yes""/></node>"), "BadValue");
        }

        [Test, Description("Wires: an output of Start or an ancestor, of a compatible type")]
        public void Validate_WireRules()
        {
            const string hit = @"<node id=""1"" type=""Hit""/><link from=""0"" to=""1""/>";
            CollectionAssert.Contains(Codes(StartNode + hit + @"<node id=""2"" type=""Buff""><f n=""TargetCreatureGID"" ref=""1-Last""/></node><link from=""1"" to=""2"" when=""hit""/>"), "BadRef");
            CollectionAssert.Contains(Codes(StartNode + hit + @"<node id=""2"" type=""Buff""><f n=""TargetCreatureGID"" ref=""1.Nothing""/></node><link from=""1"" to=""2"" when=""hit""/>"), "BadRef");
            CollectionAssert.Contains(Codes(StartNode + hit + @"<node id=""2"" type=""Buff""><f n=""TargetCreatureGID"" ref=""1.LastHitCreaturePosition""/></node><link from=""1"" to=""2"" when=""hit""/>"), "BadRefType");
            CollectionAssert.Contains(Codes(StartNode + hit + @"<node id=""2"" type=""Buff""><f n=""TargetCreatureGID"" ref=""1.LastHitCreatureGID""/></node><link from=""0"" to=""2""/>"), "RefNotAncestor");

            Assert.IsEmpty(Codes(StartNode + hit + @"<node id=""2"" type=""Buff""><f n=""TargetCreatureGID"" ref=""1.LastHitCreatureGID""/></node><link from=""1"" to=""2"" when=""hit""/>"));
            Assert.IsEmpty(Codes(StartNode + @"<node id=""1"" type=""Buff""><f n=""TargetCreatureGID"" ref=""0.ExternalTargetGID""/></node><link from=""0"" to=""1""/>"), "Start values feed any node");
            Assert.IsEmpty(Codes(StartNode + @"<node id=""1"" type=""Buff""><f n=""StackCount"" ref=""0.SkillLevel""/><f n=""Duration"" ref=""0.SkillLevel""/></node><link from=""0"" to=""1""/>"), "Int feeds Int and Float");
        }

        [Test, Description("Loose nodes (not linked from Start) are allowed and ignored at run time")]
        public void Validate_LooseNodeAllowed() => Assert.IsEmpty(Codes(StartNode + @"<node id=""1"" type=""Effect""/>"));

        // ------------------------------------------------------------ writer

        [Test, Description("Write → read keeps the whole graph")]
        public void Write_RoundTrips()
        {
            SkillDef original = Parse(GuardRush, "100010");

            string xml = SkillXmlWriter.Write(original.Graph);
            SkillDef again = Parse(xml, "100010");

            Assert.IsNotNull(again, string.Join("; ", Errors(xml, "100010")) + "\n" + xml);
            Assert.AreEqual(original.Graph.Nodes.Count, again.Graph.Nodes.Count);
            foreach (SkillNodeData n in original.Graph.Nodes)
            {
                SkillNodeData m = again.Node(n.Id);
                Assert.AreEqual((n.Kind, n.X, n.Y), (m.Kind, m.X, m.Y));
                foreach (SkillFieldDef f in n.Def.Fields) Assert.AreEqual(n.Get(f.Name), m.Get(f.Name), $"{n.Id}.{f.Name}");
                CollectionAssert.AreEquivalent(n.Refs, m.Refs);
            }
            CollectionAssert.AreEqual(original.Graph.Links.Select(l => (l.From, l.To, l.When)).OrderBy(x => x),
                again.Graph.Links.Select(l => (l.From, l.To, l.When)).OrderBy(x => x));
            Assert.AreEqual(original.Graph.Memos[0].Text, again.Graph.Memos[0].Text);
        }

        [Test, Description("Same graph → same bytes; only non-default fields are written; LF only")]
        public void Write_IsDeterministicAndCompact()
        {
            SkillGraphData g = Parse(GuardRush).Graph;
            g.Node(11).Values["CanCancel"] = "true";   // explicitly set to its default: still not written

            string a = SkillXmlWriter.Write(g), b = SkillXmlWriter.Write(g);

            Assert.AreEqual(a, b);
            Assert.IsFalse(a.Contains("\r"));
            StringAssert.DoesNotContain("n=\"CanCancel\"", a, "defaults are not written");
            StringAssert.Contains("<f n=\"Scale\" v=\"1.5\"/>", a);
        }

        [Test, Description("ADR-0002 Decision 6: the stamped payload_hash verifies, and any edit breaks it")]
        public void Write_StampsValidPayloadHash()
        {
            string xml = SkillXmlWriter.Write(SkillGraphData.CreateDefault("100020"));
            SkillDef s = Parse(xml, "100020");

            Assert.IsTrue(SkillHash.Matches(xml, s.PayloadHash));
            Assert.IsFalse(SkillHash.Matches(xml.Replace("v=\"attack\"", "v=\"attack2\""), s.PayloadHash));
        }

        [Test, Description("Text with quotes, < and new lines survives the round trip")]
        public void Write_EscapesText()
        {
            SkillGraphData g = SkillGraphData.CreateDefault("100021");
            g.Node(1).Values["AnimationType"] = "a\"b<c&d";
            g.Memos.Add(new SkillMemo { Title = "t\"1", Text = "line 1\nline <2>" });

            SkillDef s = Parse(SkillXmlWriter.Write(g), "100021");

            Assert.AreEqual("a\"b<c&d", s.Node(1).Get("AnimationType"));
            Assert.AreEqual(("t\"1", "line 1\nline <2>"), (s.Graph.Memos[0].Title, s.Graph.Memos[0].Text));
        }

        [Test, Description("A new skill from the tool is valid as created; creature owner round-trips")]
        public void CreateDefault_IsValid_OwnerRoundTrips()
        {
            SkillGraphData g = SkillGraphData.CreateDefault("100060");
            g.Owner = SkillOwner.Creature;

            SkillDef s = Parse(SkillXmlWriter.Write(g), "100060");

            Assert.IsNotNull(s);
            Assert.AreEqual(SkillOwner.Creature, s.Owner);
            Assert.AreEqual(3, s.Graph.Nodes.Count);
        }

        [Test]
        public void NextNodeId_IsMaxPlusOne() => Assert.AreEqual(16, Parse(GuardRush).Graph.NextNodeId());
    }
}

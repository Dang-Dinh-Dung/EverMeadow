using System.Collections.Generic;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>GDD Skill &amp; Moveset Rule 11–15, 22 (AC-C, AC-D20): the command table picks a skill, and a press is never swallowed.</summary>
    public class SkillSelectorTests
    {
        private const string Spear = "spear";

        private static string Row(string skill, string context = "any", string moving = "any", string dir = "any", string after = null,
            double link = 0.30, int priority = 0) =>
            $"<row skill=\"{skill}\" context=\"{context}\" moving=\"{moving}\" dir=\"{dir}\"" +
            (after != null ? $" after=\"{after}\"" : "") + $" link_window=\"{link}\" priority=\"{priority}\"/>";

        private static SkillLibrary SpearLibrary(params string[] extraRows)
        {
            SkillLibrary lib = Library(
                Skill("100001"), Skill("100002"), Skill("100003"), Skill("100004"), Skill("100005"),
                Skill("100006"), Skill("100007"), Skill("100008"), Skill("100009"));
            var rows = new List<string>
            {
                Row("100001", "ground"), Row("100002", "ground", after: "100001"), Row("100003", "ground", after: "100002"),
                Row("100004", "ground", dir: "up"), Row("100005", "ground", dir: "forward", after: "100001"),
                Row("100006", "ground", moving: "moving"), Row("100007", "ground", moving: "moving", dir: "back"),
                Row("100008", "air"), Row("100009", "dash")
            };
            rows.AddRange(extraRows);
            var errors = new List<SkillError>();
            Assert.IsTrue(SkillXmlReader.TryParseTable(Table(Spear, rows.ToArray()), out CommandTable table, errors), string.Join("; ", errors));
            lib.AddTable(table);
            return lib;
        }

        private static SelectionQuery Q(SkillContext ctx = SkillContext.Ground, bool moving = false, SkillDir dir = SkillDir.Neutral,
            SkillDef chain = null, long chainEnd = 0, long now = 0) => new SelectionQuery(ctx, moving, dir, chain, chainEnd, now);

        private static string Pick(SkillLibrary lib, SelectionQuery q) => lib.Select(Spear, in q, null).Id;

        [Test, Description("AC-C01: each kind of press lands on the intended skill")]
        public void Select_BasicPresses()
        {
            SkillLibrary lib = SpearLibrary();

            Assert.AreEqual("100001", Pick(lib, Q()));
            Assert.AreEqual("100006", Pick(lib, Q(moving: true)));
            Assert.AreEqual("100004", Pick(lib, Q(dir: SkillDir.Up)));
            Assert.AreEqual("100007", Pick(lib, Q(moving: true, dir: SkillDir.Back)));
            Assert.AreEqual("100001", Pick(lib, Q(dir: SkillDir.Down)), "Down + Attack on the ground falls to the plain row (Controller Rule 17)");
            Assert.AreEqual("100008", Pick(lib, Q(SkillContext.Air)));
            Assert.AreEqual("100009", Pick(lib, Q(SkillContext.Dash)));
        }

        [Test, Description("AC-C04: the follow-up link chains g1 → g2 → g3, and after the last the chain is over")]
        public void Select_ChainFollowsAfterLinks()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);
            lib.TryGet("100002", out SkillDef g2);
            lib.TryGet("100003", out SkillDef g3);

            Assert.AreEqual("100002", Pick(lib, Q(chain: g1, chainEnd: 0, now: 1000)));
            Assert.AreEqual("100003", Pick(lib, Q(chain: g2, chainEnd: 0, now: 1000)));
            Assert.AreEqual("100001", Pick(lib, Q(chain: g3, chainEnd: 0, now: 1000)), "nothing follows g3");
        }

        [Test, Description("AC-C07: a link is valid up to and including link_window, measured at consumption")]
        public void Select_LinkWindowBoundary()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);
            long window = SkillTime.TickOf(0.30);

            Assert.AreEqual("100002", Pick(lib, Q(chain: g1, chainEnd: 5_000, now: 5_000 + window)));
            Assert.AreEqual("100001", Pick(lib, Q(chain: g1, chainEnd: 5_000, now: 5_000 + window + 1)));
        }

        [Test, Description("AC-C05: a matching follow-up beats an opener, even while a direction is held")]
        public void Select_FollowUpBeatsOpener()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);

            Assert.AreEqual("100002", Pick(lib, Q(chain: g1, now: 1000, dir: SkillDir.Up)));
        }

        [Test, Description("AC-C05: the follow-up wins even over a more specific opener (back row: 3 fields vs g2: 2)")]
        public void Select_FollowUpBeatsMoreSpecificOpener()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);

            Assert.AreEqual("100002", Pick(lib, Q(moving: true, dir: SkillDir.Back, chain: g1, now: 1000)));
        }

        [Test, Description("AC-C05: priority outranks everything else")]
        public void Select_PriorityWins()
        {
            SkillLibrary lib = SpearLibrary(Row("100004", "ground", dir: "up", priority: 1));
            lib.TryGet("100001", out SkillDef g1);

            Assert.AreEqual("100004", Pick(lib, Q(chain: g1, now: 1000, dir: SkillDir.Up)));
        }

        [Test, Description("AC-C06: of two follow-ups, the more specific row wins")]
        public void Select_MoreSpecificFollowUpWins()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);

            Assert.AreEqual("100005", Pick(lib, Q(chain: g1, now: 1000, dir: SkillDir.Forward)));
        }

        [Test, Description("AC-C12: with only the mandatory rows, every query returns a skill")]
        public void Select_NeverReturnsNull_OverEveryCombination()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);
            foreach (SkillContext ctx in new[] { SkillContext.Ground, SkillContext.Air, SkillContext.Dash })
            foreach (bool moving in new[] { false, true })
            foreach (SkillDir dir in new[] { SkillDir.Neutral, SkillDir.Up, SkillDir.Down, SkillDir.Forward, SkillDir.Back })
            foreach (SkillDef chain in new[] { null, g1 })
            foreach (long now in new[] { 0L, 10_000L, 10_000_000L })
            {
                SkillDef a = lib.Select(Spear, Q(ctx, moving, dir, chain, 0, now), null);
                SkillDef b = lib.Select(Spear, Q(ctx, moving, dir, chain, 0, now), null);
                Assert.IsNotNull(a);
                Assert.AreSame(a, b);
            }
        }

        [Test, Description("Rule 13 / AC-D20: no table, or an unknown family, gives the built-in fallback for the context")]
        public void Select_NoTable_GivesFallback()
        {
            var lib = new SkillLibrary();

            SelectionQuery ground = Q();
            SelectionQuery air = Q(SkillContext.Air);
            SelectionQuery dash = Q(SkillContext.Dash);

            Assert.AreSame(lib.Fallback(SkillContext.Ground), lib.Select("nope", in ground, null));
            Assert.AreSame(lib.Fallback(SkillContext.Air), lib.Select("nope", in air, null));
            Assert.AreSame(lib.Fallback(SkillContext.Dash), lib.Select("nope", in dash, null));
        }

        [Test, Description("Rule 22: fallback skills are valid graphs that keep the prototype hit timing (0.04 / 0.03 / 0.02 s)")]
        public void Fallback_KeepsPrototypeRhythm()
        {
            var lib = new SkillLibrary();

            Assert.AreEqual("0.04", lib.Fallback(SkillContext.Ground).Node(2).Get("ActionDelay"));
            Assert.AreEqual("0.03", lib.Fallback(SkillContext.Air).Node(2).Get("ActionDelay"));
            Assert.AreEqual("0.02", lib.Fallback(SkillContext.Dash).Node(2).Get("ActionDelay"));
            Assert.IsTrue(SkillGraphValidator.Validate(lib.Fallback(SkillContext.Ground).Graph, new List<SkillError>()));
        }

        [Test, Description("A row whose skill is missing is skipped; the next row is tried before the fallback (Rule 22)")]
        public void Select_RowWithMissingSkill_IsSkipped()
        {
            SkillLibrary lib = Library(Skill("100001"));
            var errors = new List<SkillError>();
            SkillXmlReader.TryParseTable(Table(Spear, Row("100090", "ground", priority: 5), Row("100001", "ground")), out CommandTable table, errors);
            lib.AddTable(table);

            Assert.AreEqual("100001", Pick(lib, Q()));
        }

        [Test, Description("AC-C11: a normal skill still cooling down is skipped; the press falls to the next row")]
        public void Select_SkillOnCooldown_IsSkipped()
        {
            SkillLibrary lib = SpearLibrary();
            lib.TryGet("100001", out SkillDef g1);
            lib.TryGet("100002", out SkillDef g2);
            var cooling = new CoolingDown(g2);

            SelectionQuery q = Q(chain: g1, now: 1000);
            SkillDef picked = lib.Select(Spear, in q, cooling);

            Assert.AreEqual("100001", picked.Id);
        }

        private sealed class CoolingDown : ISkillCooldowns
        {
            private readonly SkillDef _skill;
            public CoolingDown(SkillDef skill) => _skill = skill;
            public bool IsCoolingDown(SkillDef skill) => skill == _skill;
        }

        // ------------------------------------------------------------ table file errors

        [Test, Description("A table row with an unknown attribute or a bad enum is rejected")]
        public void TryParseTable_BadRows_Rejected()
        {
            var errors = new List<SkillError>();

            bool ok1 = SkillXmlReader.TryParseTable(Table(Spear, "<row skill=\"100001\" context=\"sky\"/>"), out _, errors);
            bool ok2 = SkillXmlReader.TryParseTable(Table(Spear, "<row skill=\"100001\" sequence=\"down,right\"/>"), out _, errors);

            Assert.IsFalse(ok1);
            Assert.IsFalse(ok2);
            CollectionAssert.Contains(errors.ConvertAll(e => e.Code), "BadEnum");
            CollectionAssert.Contains(errors.ConvertAll(e => e.Code), "UnknownAttribute");
        }
    }
}

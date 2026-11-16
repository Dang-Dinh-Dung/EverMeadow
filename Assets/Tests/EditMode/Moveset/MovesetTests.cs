using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Moveset;
using Evermeadow.Core.Skills;
using Evermeadow.Foundation.Time;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Moveset
{
    /// <summary>
    /// Moveset + socket stones (user decision 2026-11-13): stones give movesets of rows (input + conditions + skill id),
    /// weapons have 2–4 sockets, every move comes from a stone, overlapping rows of two stones are refused.
    /// </summary>
    public class MovesetTests
    {
        private const string S1 = "100001", S2 = "100002", S3 = "100003", S9 = "100009";
        private static long Sec(double s) => SkillTime.TickOf(s);

        private static string Xml(string rows, string weapons = "any") =>
            $"<moveset schema_version=\"1\" weapons=\"{weapons}\">{rows}</moveset>";

        private static MovesetDef Def(string id, string rows, string weapons = "any")
        {
            var errors = new List<SkillError>();
            Assert.IsTrue(MovesetXml.TryParse(Xml(rows, weapons), id, out MovesetDef def, errors), string.Join("\n", errors));
            return def;
        }

        private static List<string> Codes(string rows, string weapons = "any", string id = "200000")
        {
            var errors = new List<SkillError>();
            MovesetXml.TryParse(Xml(rows, weapons), id, out _, errors);
            return errors.Select(e => e.Code).ToList();
        }

        private static MovesetLibrary Lib(params MovesetDef[] defs)
        {
            var lib = new MovesetLibrary();
            foreach (MovesetDef d in defs) lib.Replace(d);
            return lib;
        }

        private static MovesetLoadout Socketed(string family, MovesetLibrary lib, params string[] ids)
        {
            var l = new MovesetLoadout(family, MovesetLoadout.MaxSockets);
            for (int i = 0; i < ids.Length; i++)
            {
                lib.TryGet(ids[i], out MovesetDef d);
                Assert.AreEqual(SocketResult.Ok, l.TrySocket(i, d, lib, null), ids[i]);
            }
            return l;
        }

        private static MoveQuery Q(MoveInput input, SkillContext ctx = SkillContext.Ground, bool moving = false, SkillDir dir = SkillDir.Neutral,
            string chain = "", long chainEnd = 0, long now = 0) => new MoveQuery(input, ctx, moving, dir, chain, chainEnd, now);

        private static readonly MoveInput L = MoveInput.Press(MoveButton.Button1);
        private static readonly MoveInput R = MoveInput.Press(MoveButton.Button2);

        // ------------------------------------------------------------ input

        [TestCase("Button1", "Button1")]
        [TestCase(" Key3 ", "Key3")]
        [TestCase("Button1:press", "Button1")]
        [TestCase("Button1:double", "Button1:double")]
        [TestCase("Key2:hold", "Key2:hold")]
        [TestCase("Button2+Button1", "Button1+Button2")]
        [TestCase("Button1+Key4", "Button1+Key4")]
        public void MoveInput_ParsesAndFormatsCanonically(string text, string expected)
        {
            Assert.IsTrue(MoveInput.TryParse(text, out MoveInput i));
            Assert.AreEqual(expected, i.ToString());
            Assert.IsTrue(MoveInput.TryParse(i.ToString(), out MoveInput again));
            Assert.AreEqual(i, again);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("Mouse1")]
        [TestCase("button1")]
        [TestCase("Button1+Button1")]
        [TestCase("Button1:triple")]
        [TestCase("0")]
        [TestCase("3")]
        [TestCase("Button1+")]
        public void MoveInput_BadText_Rejected(string text) => Assert.IsFalse(MoveInput.TryParse(text, out _));

        [Test]
        public void Chord_OrderDoesNotMatter_AndUsesBothButtons()
        {
            MoveInput a = MoveInput.Chord(MoveButton.Button2, MoveButton.Button1);
            Assert.AreEqual(MoveInput.Chord(MoveButton.Button1, MoveButton.Button2), a);
            Assert.IsTrue(a.Uses(MoveButton.Button1) && a.Uses(MoveButton.Button2));
            Assert.IsFalse(a.Uses(MoveButton.Button3));
            Assert.AreNotEqual(MoveInput.Press(MoveButton.Button1), MoveInput.Hold(MoveButton.Button1));
            Assert.Throws<System.ArgumentException>(() => MoveInput.Chord(MoveButton.Key1, MoveButton.Key1));
        }

        // ------------------------------------------------------------ xml + validator

        [Test]
        public void Xml_RoundTrip_SameBytes()
        {
            var m = new MovesetData { Id = "200000", Name = "Đánh thường <cơ bản>", AnyWeapon = false, Weapons = { "spear", "sword" }, Note = "chuỗi 3 nhát & đòn trên không" };
            m.Rows.Add(new MovesetRowData { Input = L, Skill = S1, Context = SkillContext.Ground });
            m.Rows.Add(new MovesetRowData { Input = L, Skill = S2, Context = SkillContext.Ground, After = { S1 }, LinkWindow = 0.4 });
            m.Rows.Add(new MovesetRowData { Input = MoveInput.Chord(MoveButton.Button1, MoveButton.Button2), Skill = S9, Context = SkillContext.Ground | SkillContext.Air, Cooldown = 6, Priority = 2 });
            m.Rows.Add(new MovesetRowData { Input = MoveInput.Hold(MoveButton.Key1), Skill = S3, Moving = MovingFilter.Moving, Context = SkillContext.Ground, AnyDir = false, Dir = SkillDir.Up });

            string xml = MovesetXml.Write(m);
            Assert.IsTrue(MovesetXml.TryRead(xml, "200000", out MovesetData back, null));

            Assert.AreEqual(xml, MovesetXml.Write(back));
            Assert.AreEqual("Đánh thường <cơ bản>", back.Name);
            CollectionAssert.AreEqual(new[] { "spear", "sword" }, back.Weapons);
            Assert.AreEqual(0.4, back.Rows[1].LinkWindow, 1e-9);
            Assert.AreEqual(SkillDir.Up, back.Rows[3].Dir);
            StringAssert.Contains("context=\"ground|air\"", xml);
            StringAssert.Contains("input=\"Button1+Button2\"", xml);
        }

        [Test]
        public void Xml_DefaultsAreNotWritten()
        {
            var m = MovesetData.CreateDefault("200000");
            string xml = MovesetXml.Write(m);
            StringAssert.Contains("<row input=\"Button1\" skill=\"100000\" />", xml);
            StringAssert.Contains("weapons=\"any\"", xml);
            StringAssert.DoesNotContain("note", xml);
        }

        [Test]
        public void Xml_ValidMoveset_CompilesRows()
        {
            MovesetDef d = Def("200000", $"<row input=\"Button1\" skill=\"{S1}\" context=\"ground\" priority=\"3\"/><row input=\"Button1\" skill=\"{S2}\" after=\"{S1}\" link_window=\"0.5\" cooldown=\"2\"/>", "spear");

            Assert.AreEqual("200000", d.Id);
            Assert.IsTrue(d.AllowsWeapon("spear"));
            Assert.IsFalse(d.AllowsWeapon("sword"));
            Assert.AreEqual(2, d.Rows.Length);
            Assert.AreEqual(3, d.Rows[0].Priority);
            Assert.AreEqual(1, d.Rows[0].Specificity, "context constrained");
            Assert.AreEqual(1, d.Rows[1].Specificity, "after constrained");
            Assert.AreEqual(Sec(0.5), d.Rows[1].LinkWindowTicks);
            Assert.AreEqual(Sec(2), d.Rows[1].CooldownTicks);
            Assert.AreEqual(1, d.Rows[1].Order);
        }

        [TestCase("<row input=\"Mouse1\" skill=\"100001\"/>", "BadInput")]
        [TestCase("<row input=\"Button1\" skill=\"12\"/>", "IdFormat")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" context=\"water\"/>", "BadEnum")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" context=\"ground|\"/>", "BadEnum")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" moving=\"fast\"/>", "BadEnum")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" dir=\"left\"/>", "BadEnum")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" moving=\"still\"/>", "MovingOnlyGround")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" context=\"ground|air\" moving=\"still\"/>", "MovingOnlyGround")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" after=\"abc\"/>", "IdFormat")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" link_window=\"-1\"/>", "BadNumber")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" cooldown=\"x\"/>", "BadNumber")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" cooldown=\"-2\"/>", "BadNumber")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" priority=\"1.5\"/>", "BadNumber")]
        [TestCase("<row input=\"Button1\" skill=\"100001\" power=\"9\"/>", "UnknownAttribute")]
        [TestCase("<stone/>", "UnknownNode")]
        [TestCase("", "EmptyMoveset")]
        [TestCase("<row input=\"Button1\" skill=\"100001\"/><row input=\"Button1\" skill=\"100002\"/>", "DuplicateRow")]
        public void Validator_BadRow_Reported(string rows, string code) => CollectionAssert.Contains(Codes(rows), code);

        [Test]
        public void Validator_RowsThatDiffer_AreNotDuplicates()
        {
            string rows = $"<row input=\"Button1\" skill=\"{S1}\"/>" +
                          $"<row input=\"Button1\" skill=\"{S2}\" priority=\"1\"/>" +
                          $"<row input=\"Button1\" skill=\"{S2}\" context=\"air\"/>" +
                          $"<row input=\"Button1\" skill=\"{S2}\" dir=\"up\"/>" +
                          $"<row input=\"Button1\" skill=\"{S2}\" dir=\"down\"/>" +
                          $"<row input=\"Button1\" skill=\"{S3}\" after=\"{S1}\"/>" +
                          $"<row input=\"Button1\" skill=\"{S3}\" after=\"{S2}\"/>" +
                          $"<row input=\"Button1:double\" skill=\"{S3}\"/>";
            CollectionAssert.IsEmpty(Codes(rows));
            CollectionAssert.Contains(Codes($"<row input=\"Button1\" skill=\"{S3}\" after=\"{S1},{S2}\"/><row input=\"Button1\" skill=\"{S1}\" after=\"{S2},{S1}\"/>"), "DuplicateRow",
                "after lists compare as sets");
        }

        [TestCase("", "NoWeapons")]
        [TestCase("Spear", "IdFormat")]
        [TestCase("spear,spear", "DuplicateWeapon")]
        [TestCase("spear,any", "IdFormat")]
        public void Validator_BadWeapons_Reported(string weapons, string code) =>
            CollectionAssert.Contains(Codes($"<row input=\"Button1\" skill=\"{S1}\"/>", weapons), code);

        [Test]
        public void Validator_IdAndSchemaAndKnownSkills()
        {
            CollectionAssert.Contains(Codes($"<row input=\"Button1\" skill=\"{S1}\"/>", id: "abc"), "IdFormat");
            var errors = new List<SkillError>();
            MovesetXml.TryParse($"<moveset schema_version=\"2\" weapons=\"any\"><row input=\"Button1\" skill=\"{S1}\"/></moveset>", "200000", out _, errors);
            Assert.AreEqual("SchemaTooNew", errors.Single().Code);
            errors.Clear();
            Assert.IsFalse(MovesetXml.TryParse(Xml($"<row input=\"Button1\" skill=\"{S1}\"/>"), "200000", out _, errors, new HashSet<string> { S2 }));
            Assert.AreEqual("MissingSkill", errors.Single().Code);
            errors.Clear();
            Assert.IsFalse(MovesetXml.TryParse("not xml", "200000", out _, errors));
            Assert.AreEqual("XmlUnreadable", errors.Single().Code);
        }

        [Test]
        public void WeaponFamilies_ParseSkipsCommentsAndReportsBadOrRepeated()
        {
            var errors = new List<SkillError>();
            List<string> list = WeaponFamilies.Parse("# header\r\nslab\n  needle  # thin\n\nslab\nBig Sword\nany\nring", errors);
            CollectionAssert.AreEqual(new[] { "slab", "needle", "ring" }, list);
            CollectionAssert.AreEqual(new[] { "DuplicateWeapon", "IdFormat", "IdFormat" }, errors.Select(e => e.Code));
            StringAssert.Contains("line 5", errors[0].Message);
            CollectionAssert.IsEmpty(WeaponFamilies.Parse(null, null));
            CollectionAssert.AreEqual(WeaponFamilies.Defaults, WeaponFamilies.Parse(WeaponFamilies.DefaultFileText, null), "the default file lists the defaults");
        }

        [Test]
        public void Validator_WeaponNotInList_UnknownWeaponOnlyWhenListGiven()
        {
            MovesetXml.TryRead(Xml($"<row input=\"Button1\" skill=\"{S1}\"/>", "slab,spear"), "200000", out MovesetData m, null);
            var errors = new List<SkillError>();
            MovesetValidator.Validate(m, errors);
            CollectionAssert.IsEmpty(errors, "runtime does not need the list");
            MovesetValidator.Validate(m, errors, null, new HashSet<string>(WeaponFamilies.Defaults));
            Assert.AreEqual("UnknownWeapon", errors.Single().Code);
            StringAssert.Contains("spear", errors[0].Message);
        }

        // ------------------------------------------------------------ sockets

        [Test]
        public void Loadout_SocketCount_TwoToFour()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MovesetLoadout("spear", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MovesetLoadout("spear", 5));
            Assert.AreEqual(2, new MovesetLoadout("spear", 2).SocketCount);
            Assert.AreEqual(4, new MovesetLoadout("spear", 4).SocketCount);
        }

        [Test]
        public void Socket_WrongWeaponOrSlotOrTwice_Refused()
        {
            MovesetDef spearOnly = Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/>", "spear");
            MovesetLibrary lib = Lib(spearOnly);
            var sword = new MovesetLoadout("sword", 3);
            var spear = new MovesetLoadout("spear", 3);

            Assert.AreEqual(SocketResult.WeaponNotAllowed, sword.TrySocket(0, spearOnly, lib, null));
            Assert.IsNull(sword.Get(0));
            Assert.AreEqual(SocketResult.BadSlot, spear.TrySocket(3, spearOnly, lib, null));
            Assert.AreEqual(SocketResult.BadSlot, spear.TrySocket(-1, spearOnly, lib, null));
            Assert.AreEqual(SocketResult.Ok, spear.TrySocket(0, spearOnly, lib, null));
            Assert.AreEqual(SocketResult.AlreadySocketed, spear.TrySocket(1, spearOnly, lib, null));
            Assert.AreEqual(SocketResult.Ok, spear.TrySocket(0, spearOnly, lib, null), "re-socketing into its own slot is fine");
            Assert.IsTrue(spear.Unsocket(0));
            Assert.IsFalse(spear.Unsocket(0));
        }

        [Test]
        public void Socket_SameInputSameConditions_RefusedWithDetails()
        {
            MovesetDef a = Def("200000", $"<row input=\"Button1\" skill=\"{S1}\" context=\"ground\"/>");
            MovesetDef b = Def("200001", $"<row input=\"Button2\" skill=\"{S3}\"/><row input=\"Button1\" skill=\"{S2}\"/>");
            MovesetLibrary lib = Lib(a, b);
            var l = new MovesetLoadout("spear", 2);
            l.TrySocket(0, a, lib, null);
            var conflicts = new List<MoveConflict>();

            Assert.AreEqual(SocketResult.Conflict, l.TrySocket(1, b, lib, conflicts));

            Assert.IsNull(l.Get(1));
            MoveConflict c = conflicts.Single();
            Assert.AreEqual(1, c.Row);
            Assert.AreEqual(0, c.OtherSlot);
            Assert.AreEqual("200000", c.OtherMovesetId);
            Assert.AreEqual(0, c.OtherRow);
            Assert.AreEqual(L, c.Input);
            Assert.AreEqual(SocketResult.Ok, l.TrySocket(0, b, lib, null), "replacing the conflicting stone itself is fine");
        }

        // Each pair: (row A, row B, overlap?)
        [TestCase("input=\"Button1\" context=\"ground\"", "input=\"Button1\" context=\"air\"", false)]
        [TestCase("input=\"Button1\" context=\"ground|air\"", "input=\"Button1\" context=\"air|dash\"", true)]
        [TestCase("input=\"Button1\"", "input=\"Button2\"", false)]
        [TestCase("input=\"Button1\"", "input=\"Button1:double\"", false)]
        [TestCase("input=\"Button1\"", "input=\"Button1+Button2\"", false)]
        [TestCase("input=\"Button1+Button2\"", "input=\"Button2+Button1\"", true)]
        [TestCase("input=\"Button1\" context=\"ground\" moving=\"still\"", "input=\"Button1\" context=\"ground\" moving=\"moving\"", false)]
        [TestCase("input=\"Button1\" context=\"ground\" moving=\"still\"", "input=\"Button1\"", true)]
        [TestCase("input=\"Button1\" dir=\"up\"", "input=\"Button1\" dir=\"down\"", false)]
        [TestCase("input=\"Button1\" dir=\"up\"", "input=\"Button1\"", true)]
        [TestCase("input=\"Button1\" dir=\"up\"", "input=\"Button1\" dir=\"up\"", true)]
        [TestCase("input=\"Button1\" after=\"100001\"", "input=\"Button1\"", false)]
        [TestCase("input=\"Button1\" after=\"100001\"", "input=\"Button1\" after=\"100002\"", false)]
        [TestCase("input=\"Button1\" after=\"100001,100003\"", "input=\"Button1\" after=\"100003\"", true)]
        [TestCase("input=\"Button1\" priority=\"5\"", "input=\"Button1\"", true)]
        public void RowsOverlap_Cases(string a, string b, bool overlap)
        {
            MovesetRow ra = Def("200000", $"<row {a} skill=\"{S1}\"/>").Rows[0];
            MovesetRow rb = Def("200001", $"<row {b} skill=\"{S2}\"/>").Rows[0];
            Assert.AreEqual(overlap, MovesetLoadout.RowsOverlap(ra, rb));
            Assert.AreEqual(overlap, MovesetLoadout.RowsOverlap(rb, ra), "symmetric");
        }

        [Test]
        public void Validate_AfterHotReload_ReportsMissingWeaponAndConflicts()
        {
            MovesetDef a = Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/>");
            MovesetDef b = Def("200001", $"<row input=\"Button2\" skill=\"{S2}\"/>");
            MovesetLibrary lib = Lib(a, b);
            MovesetLoadout l = Socketed("spear", lib, "200000", "200001");
            var problems = new List<string>();
            l.Validate(lib, problems);
            CollectionAssert.IsEmpty(problems);

            lib.Replace(Def("200001", $"<row input=\"Button1\" skill=\"{S2}\"/>", "sword"));
            l.Validate(lib, problems);
            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
            lib.Remove("200000");
            problems.Clear();
            l.Validate(lib, problems);
            Assert.IsTrue(problems.Any(p => p.Contains("missing")));
        }

        // ------------------------------------------------------------ selection

        [Test]
        public void Select_NoStone_NoMove()
        {
            Assert.IsFalse(MoveSelector.Select(new MovesetLoadout("spear", 2), new MovesetLibrary(), Q(L)).Found);
            Assert.AreEqual("", MoveChoice.None.SkillId);
        }

        [Test]
        public void Select_BasicChain_OpenerThenFollowUpsInsideLinkWindow()
        {
            MovesetLibrary lib = Lib(Def("200000",
                $"<row input=\"Button1\" skill=\"{S1}\" context=\"ground\"/>" +
                $"<row input=\"Button1\" skill=\"{S2}\" context=\"ground\" after=\"{S1}\" link_window=\"0.5\"/>" +
                $"<row input=\"Button1\" skill=\"{S3}\" context=\"air\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");

            Assert.AreEqual(S1, MoveSelector.Select(l, lib, Q(L)).SkillId);
            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L, chain: S1, chainEnd: Sec(1), now: Sec(1.5))).SkillId, "inside the window (≤)");
            Assert.AreEqual(S1, MoveSelector.Select(l, lib, Q(L, chain: S1, chainEnd: Sec(1), now: Sec(1.5) + 1)).SkillId, "window over: opener again");
            Assert.AreEqual(S1, MoveSelector.Select(l, lib, Q(L, chain: S3, chainEnd: Sec(1), now: Sec(1))).SkillId, "wrong previous skill");
            Assert.AreEqual(S3, MoveSelector.Select(l, lib, Q(L, SkillContext.Air)).SkillId);
            Assert.IsFalse(MoveSelector.Select(l, lib, Q(L, SkillContext.Dash)).Found, "no dash row");
            Assert.IsFalse(MoveSelector.Select(l, lib, Q(R)).Found, "no Button2 row");
        }

        [Test]
        public void Select_FollowUpFromAnotherStone_WinsOverOpener()
        {
            MovesetLibrary lib = Lib(
                Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/>"),
                Def("200001", $"<row input=\"Button1\" skill=\"{S9}\" after=\"{S1}\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000", "200001");

            MoveChoice c = MoveSelector.Select(l, lib, Q(L, chain: S1, chainEnd: 0, now: 0));

            Assert.AreEqual(S9, c.SkillId);
            Assert.AreEqual(1, c.Slot);
            Assert.AreEqual("200001", c.Moveset.Id);
        }

        [Test]
        public void Select_MatchedFollowUp_BeatsMoreSpecificOpener()
        {
            MovesetLibrary lib = Lib(Def("200000",
                $"<row input=\"Button1\" skill=\"{S1}\" context=\"ground\" moving=\"still\" dir=\"up\"/>" +
                $"<row input=\"Button1\" skill=\"{S2}\" after=\"{S9}\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");
            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L, dir: SkillDir.Up, chain: S9)).SkillId);
            Assert.AreEqual(S1, MoveSelector.Select(l, lib, Q(L, dir: SkillDir.Up)).SkillId);
        }

        [Test]
        public void Select_PriorityThenSpecificity()
        {
            MovesetLibrary one = Lib(Def("200002",
                $"<row input=\"Button1\" skill=\"{S1}\"/>" +
                $"<row input=\"Button1\" skill=\"{S2}\" dir=\"up\"/>" +
                $"<row input=\"Button1\" skill=\"{S3}\" context=\"ground\" priority=\"1\"/>" +
                $"<row input=\"Button1\" skill=\"{S9}\" context=\"air\" moving=\"any\"/>"));
            MovesetLoadout m = Socketed("spear", one, "200002");
            Assert.AreEqual(S3, MoveSelector.Select(m, one, Q(L, dir: SkillDir.Up)).SkillId, "priority first");
            Assert.AreEqual(S2, MoveSelector.Select(m, one, Q(L, SkillContext.Air, dir: SkillDir.Up)).SkillId, "more specific (dir) beats less specific");
            Assert.AreEqual(S9, MoveSelector.Select(m, one, Q(L, SkillContext.Air)).SkillId, "context-specific beats any");
        }

        [Test]
        public void Select_TieBetweenRows_EarlierRowWins()
        {
            MovesetLibrary lib = Lib(Def("200000", $"<row input=\"Button1\" skill=\"{S1}\" dir=\"up\"/><row input=\"Button1\" skill=\"{S2}\" context=\"ground\"/>"));
            Assert.AreEqual(S1, MoveSelector.Select(Socketed("spear", lib, "200000"), lib, Q(L, dir: SkillDir.Up)).SkillId);
        }

        [Test]
        public void Select_StillMovingAndDirection()
        {
            MovesetLibrary lib = Lib(Def("200000",
                $"<row input=\"Button1\" skill=\"{S1}\" context=\"ground\" moving=\"still\"/>" +
                $"<row input=\"Button1\" skill=\"{S2}\" context=\"ground\" moving=\"moving\"/>" +
                $"<row input=\"Button1\" skill=\"{S3}\" dir=\"down\" context=\"air\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");
            Assert.AreEqual(S1, MoveSelector.Select(l, lib, Q(L, moving: false)).SkillId);
            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L, moving: true)).SkillId);
            Assert.AreEqual(S3, MoveSelector.Select(l, lib, Q(L, SkillContext.Air, dir: SkillDir.Down)).SkillId);
            Assert.IsFalse(MoveSelector.Select(l, lib, Q(L, SkillContext.Air, dir: SkillDir.Up)).Found);
        }

        private sealed class Cooling : IMoveCooldowns
        {
            public string Id;
            public int Row;
            public bool IsCoolingDown(string movesetId, int row) => movesetId == Id && row == Row;
        }

        [Test]
        public void Select_CoolingDownOrMissingSkillOrWeaponChanged_Skipped()
        {
            MovesetLibrary lib = Lib(Def("200000", $"<row input=\"Button1\" skill=\"{S1}\" priority=\"1\"/><row input=\"Button1\" skill=\"{S2}\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");

            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L), new Cooling { Id = "200000", Row = 0 }).SkillId);

            var skills = new SkillLibrary();
            Assert.IsTrue(SkillXmlReader.TryParse("<skill schema_version=\"2\"><node id=\"0\" type=\"Start\"/></skill>", S2, out SkillDef s2, null));
            skills.Add(s2);
            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L), null, skills).SkillId, "skill 100001 not loaded");

            lib.Replace(Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/>", "sword"));
            Assert.IsFalse(MoveSelector.Select(l, lib, Q(L)).Found, "moveset edited to another weapon while socketed");
        }

        [Test]
        public void Select_HotReloadedMoveset_UsedByNextLookup()
        {
            MovesetLibrary lib = Lib(Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");
            lib.Replace(Def("200000", $"<row input=\"Button1\" skill=\"{S2}\"/>"));
            Assert.AreEqual(S2, MoveSelector.Select(l, lib, Q(L)).SkillId);
        }

        // ------------------------------------------------------------ gestures

        private static MoveGestureRecognizer Gestures(params string[] inputs)
        {
            var g = new MoveGestureRecognizer(MoveGestureSettings.Default);
            g.Configure(inputs.Select(t =>
            {
                Assert.IsTrue(MoveInput.TryParse(t, out MoveInput i), t);
                return i;
            }));
            return g;
        }

        private static string Text(List<MoveInputEvent> e) => string.Join(" ", e.Select(x => x.Input.ToString()));

        [Test]
        public void Gesture_PressOnlyButton_FiresAtOnce()
        {
            MoveGestureRecognizer g = Gestures("Button1", "Button2");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button1, 100, e);
            Assert.AreEqual("Button1", Text(e));
            Assert.AreEqual(100, e[0].Tick);
            g.Press(MoveButton.Button1, 200, e);
            Assert.AreEqual(1, e.Count, "key repeat while held is ignored");
            g.Release(MoveButton.Button1, 300, e);
            g.Update(Sec(10), e);
            Assert.AreEqual(1, e.Count);
        }

        [Test]
        public void Gesture_DoubleTap_SecondPressInsideWindow()
        {
            MoveGestureRecognizer g = Gestures("Button1", "Button1:double");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button1, 0, e);
            g.Release(MoveButton.Button1, Sec(0.05), e);
            g.Press(MoveButton.Button1, Sec(0.1), e);
            Assert.AreEqual("Button1 Button1:double", Text(e));
            g.Release(MoveButton.Button1, Sec(0.15), e);
            g.Press(MoveButton.Button1, Sec(0.2), e);
            Assert.AreEqual("Button1 Button1:double Button1", Text(e), "a third press starts over (no triple tap), even inside the first press's window");
            g.Release(MoveButton.Button1, Sec(0.22), e);
            g.Press(MoveButton.Button1, Sec(0.2) + Sec(0.25) + 1, e);
            Assert.AreEqual("Button1 Button1:double Button1 Button1", Text(e), "just outside the window: two presses");
            g.Release(MoveButton.Button1, Sec(0.5), e);
            g.Press(MoveButton.Button1, Sec(0.2) + Sec(0.25) + 1 + Sec(0.25), e);
            Assert.AreEqual("Button1 Button1:double Button1 Button1 Button1:double", Text(e), "exactly at the window edge (≤) is a double tap");
            g.Release(MoveButton.Button1, Sec(2), e);
            e.Clear();
            g.Press(MoveButton.Button1, Sec(3), e);
            Assert.AreEqual("Button1", Text(e));
        }

        [Test]
        public void Gesture_DoubleTapNotUsed_TwoPresses()
        {
            MoveGestureRecognizer g = Gestures("Button1");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button1, 0, e);
            g.Release(MoveButton.Button1, 1, e);
            g.Press(MoveButton.Button1, 2, e);
            Assert.AreEqual("Button1 Button1", Text(e));
        }

        [Test]
        public void Gesture_HoldButton_TapOnReleaseOrHoldAtThreshold()
        {
            MoveGestureRecognizer g = Gestures("Key1", "Key1:hold");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Key1, 0, e);
            g.Update(Sec(0.30) - 1, e);
            Assert.AreEqual("", Text(e), "waiting");
            g.Release(MoveButton.Key1, Sec(0.2), e);
            Assert.AreEqual("Key1", Text(e));
            Assert.AreEqual(Sec(0.2), e[0].Tick);

            e.Clear();
            g.Press(MoveButton.Key1, Sec(1), e);
            g.Update(Sec(1) + Sec(0.30), e);
            Assert.AreEqual("Key1:hold", Text(e));
            Assert.AreEqual(Sec(1) + Sec(0.30), e[0].Tick);
            g.Update(Sec(5), e);
            g.Release(MoveButton.Key1, Sec(5), e);
            Assert.AreEqual(1, e.Count, "release after a hold fires nothing");
        }

        [Test]
        public void Gesture_Chord_BothWithinWindow_EitherOrder()
        {
            MoveGestureRecognizer g = Gestures("Button1", "Button2", "Button1+Button2");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button2, 0, e);
            g.Press(MoveButton.Button1, Sec(0.05), e);
            Assert.AreEqual("Button1+Button2", Text(e));
            g.Release(MoveButton.Button1, Sec(0.1), e);
            g.Release(MoveButton.Button2, Sec(0.1), e);
            Assert.AreEqual(1, e.Count, "neither button fires a press after the chord");
        }

        [Test]
        public void Gesture_ChordButtonAlone_PressAfterWindowOrOnRelease()
        {
            MoveGestureRecognizer g = Gestures("Button1", "Button2", "Button1+Button2");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button1, 0, e);
            g.Update(Sec(0.05), e);
            Assert.AreEqual("", Text(e), "window still open (≤)");
            g.Update(Sec(0.05) + 1, e);
            Assert.AreEqual("Button1", Text(e));
            Assert.AreEqual(Sec(0.05), e[0].Tick);
            g.Press(MoveButton.Button2, Sec(0.06), e);
            g.Update(Sec(0.07), e);
            Assert.AreEqual("Button1", Text(e), "partner too late: no chord, Button2 waits its own window");
            g.Release(MoveButton.Button2, Sec(0.08), e);
            Assert.AreEqual("Button1 Button2", Text(e), "quick tap released inside the window");
        }

        [Test]
        public void Gesture_ButtonOutsideChord_NotDelayed()
        {
            MoveGestureRecognizer g = Gestures("Button1+Button2", "Button3");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Button3, 7, e);
            Assert.AreEqual("Button3", Text(e));
        }

        [Test]
        public void Gesture_Reset_ForgetsPendingAndHeld()
        {
            MoveGestureRecognizer g = Gestures("Key1:hold");
            var e = new List<MoveInputEvent>();
            g.Press(MoveButton.Key1, 0, e);
            g.Reset();
            g.Update(Sec(1), e);
            g.Release(MoveButton.Key1, Sec(1), e);
            Assert.AreEqual(0, e.Count);
        }

        [Test]
        public void Gesture_InputsOf_CollectsSocketedRows()
        {
            MovesetLibrary lib = Lib(Def("200000", $"<row input=\"Button1\" skill=\"{S1}\"/><row input=\"Key1:hold\" skill=\"{S2}\"/>"));
            MovesetLoadout l = Socketed("spear", lib, "200000");
            CollectionAssert.AreEqual(new[] { "Button1", "Key1:hold" }, MoveGestureRecognizer.InputsOf(l, lib).Select(i => i.ToString()));
        }

        // ------------------------------------------------------------ hot reload

        private sealed class ListSource : ISkillTextSource
        {
            public readonly List<SkillSourceText> Files = new List<SkillSourceText>();
            public void ReadAll(List<SkillSourceText> into) => into.AddRange(Files);
        }

        [Test]
        public void Repository_ReloadRejectRemove()
        {
            var src = new ListSource();
            src.Files.Add(new SkillSourceText("200000.xml", Xml($"<row input=\"Button1\" skill=\"{S1}\"/>")));
            src.Files.Add(new SkillSourceText("x/200000.xml", Xml($"<row input=\"Button2\" skill=\"{S1}\"/>")));
            src.Files.Add(new SkillSourceText("200001.xml", "broken"));
            var repo = new MovesetRepository(src);
            var errors = new List<SkillError>();
            repo.LoadAll(errors);
            Assert.AreEqual(1, repo.Library.Count);
            CollectionAssert.AreEquivalent(new[] { "DuplicateId", "XmlUnreadable" }, errors.Select(x => x.Code));
            var events = new List<SkillReloadEvent>();
            repo.Changed += events.Add;

            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText("200000.xml", src.Files[0].Xml, null));
            Assert.AreEqual(SkillReloadResult.Updated, repo.ReloadText("200000.xml", Xml($"<row input=\"Button1\" skill=\"{S2}\"/>"), null));
            repo.Library.TryGet("200000", out MovesetDef d);
            Assert.AreEqual(S2, d.Rows[0].SkillId);
            Assert.AreEqual(SkillReloadResult.Rejected, repo.ReloadText("200000.xml", Xml(""), errors));
            repo.Library.TryGet("200000", out d);
            Assert.AreEqual(S2, d.Rows[0].SkillId, "bad save keeps the loaded version");
            Assert.AreEqual(SkillReloadResult.Added, repo.ReloadText("200001.xml", Xml($"<row input=\"Key1\" skill=\"{S3}\"/>"), null));
            Assert.AreEqual(SkillReloadResult.Removed, repo.RemoveFile(@"\200001.xml"));
            Assert.AreEqual(SkillReloadResult.Ignored, repo.RemoveFile("200001.xml"));
            Assert.AreEqual(3, events.Count);
            Assert.AreEqual(4, repo.Version);
        }
    }
}

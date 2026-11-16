using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>ADR-0002 Decision 4–6: loading a folder of .xml files (id = file name), library validation, payload hash.</summary>
    public class SkillLoaderTests
    {
        private sealed class ListSource : ISkillTextSource
        {
            private readonly SkillSourceText[] _texts;
            public ListSource(params SkillSourceText[] texts) => _texts = texts;
            public void ReadAll(List<SkillSourceText> into) => into.AddRange(_texts);
        }

        private const string Root = "C:/Game/StreamingAssets/Skills";
        private const string G1 = "100001", A1 = "100002", D1 = "100003", Plunge = "100004";

        private static FolderSkillTextSource FakeFolder(Dictionary<string, string> files, bool exists = true) =>
            new FolderSkillTextSource(Root, _ => exists, _ => files.Keys.ToArray(), p => files[p]);

        private static readonly Fx DiveSkill = Skill(Plunge);

        private static SkillSourceText FullTable => new SkillSourceText("Tables/spear.xml",
            Table("spear",
                $"<row skill=\"{G1}\" context=\"ground\"/>",
                $"<row skill=\"{A1}\" context=\"air\"/>",
                $"<row skill=\"{D1}\" context=\"dash\"/>",
                $"<row skill=\"{Plunge}\" context=\"air\" dir=\"down\"/>"));

        private static SkillLibrary LoadAll(List<SkillError> errors, params SkillSourceText[] texts) => SkillLoader.Load(new ListSource(texts), errors);

        // ------------------------------------------------------------ loader

        [Test, Description("The id of a skill is its file name, wherever the file sits")]
        public void Load_IdComesFromFileName()
        {
            SkillLibrary lib = LoadAll(new List<SkillError>(), new SkillSourceText("spear/100777.xml", Skill("100000").Xml));

            Assert.IsTrue(lib.TryGet("100777", out SkillDef s));
            Assert.AreEqual("100777", s.Id);
        }

        [Test, Description("Texts are routed by root element; a bad file is reported with its name and left out")]
        public void Load_RoutesByRootAndReportsBadFiles()
        {
            var errors = new List<SkillError>();

            SkillLibrary lib = LoadAll(errors, Skill(G1).Source, FullTable, new SkillSourceText("BAD.xml", Skill(G1).Xml),
                new SkillSourceText("100050.xml", "<weapon/>"), new SkillSourceText("100051.xml", "{"));

            Assert.IsTrue(lib.TryGet(G1, out _));
            Assert.IsTrue(lib.TryGetTable("spear", out _));
            Assert.AreEqual(1, lib.SkillCount);
            Assert.IsTrue(errors.Any(e => e.Code == "IdFormat" && e.Message.StartsWith("BAD.xml: ")));
            Assert.IsTrue(errors.Any(e => e.Code == "UnknownNode" && e.Message.StartsWith("100050.xml: ")));
            Assert.IsTrue(errors.Any(e => e.Code == "XmlUnreadable" && e.Message.StartsWith("100051.xml: ")));
        }

        [Test, Description("The same file name in two folders: the first is kept and the second is reported")]
        public void Load_DuplicateId_KeepsFirst()
        {
            var errors = new List<SkillError>();

            SkillLibrary lib = LoadAll(errors, new SkillSourceText("a/100001.xml", Skill(G1).Xml),
                new SkillSourceText("b/100001.xml", Skill(G1).Xml.Replace("anim=\"a\"", "anim=\"second\"")));

            lib.TryGet(G1, out SkillDef s);
            Assert.AreEqual("a", s.Node(1).Get("AnimationType"));
            Assert.IsTrue(errors.Any(e => e.Code == "DuplicateId"));
        }

        // ------------------------------------------------------------ folder source

        [Test, Description("The folder source reads every .xml in ordinal order of the relative path, sub-folders included")]
        public void Folder_ReadsXmlInOrdinalOrder()
        {
            var files = new Dictionary<string, string>
            {
                [Root + "/100002.xml"] = "B",
                [Root + "/Tables/spear.xml"] = "C",
                [Root + "/100001.xml"] = "A",
                [Root + "/notes.txt"] = "X"
            };
            var into = new List<SkillSourceText>();

            FakeFolder(files).ReadAll(into);

            CollectionAssert.AreEqual(new[] { "100001.xml", "100002.xml", "Tables/spear.xml" }, into.Select(t => t.Name));
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, into.Select(t => t.Xml));
        }

        [Test, Description("Rule 22: a missing folder reads as empty, and the library still answers with fallbacks")]
        public void Folder_Missing_GivesFallbackOnlyLibrary()
        {
            FolderSkillTextSource source = FakeFolder(new Dictionary<string, string>(), exists: false);

            SkillLibrary lib = SkillLoader.Load(source, new List<SkillError>());

            Assert.IsFalse(source.RootExists);
            Assert.AreEqual(0, lib.SkillCount);
            var q = new SelectionQuery(SkillContext.Air, false, SkillDir.Neutral, null, 0, 0);
            Assert.AreSame(lib.Fallback(SkillContext.Air), lib.Select("spear", in q, null));
        }

        [Test, Description("End to end: folder → loader → selection")]
        public void Folder_LoadedLibrarySelects()
        {
            var files = new Dictionary<string, string>
            {
                [Root + "/100001.xml"] = Skill(G1).Xml,
                [Root + "/Tables/spear.xml"] = Table("spear", $"<row skill=\"{G1}\" context=\"ground\"/>")
            };

            SkillLibrary lib = SkillLoader.Load(FakeFolder(files), new List<SkillError>());

            var q = new SelectionQuery(SkillContext.Ground, false, SkillDir.Neutral, null, 0, 0);
            Assert.AreEqual(G1, lib.Select("spear", in q, null).Id);
        }

        // ------------------------------------------------------------ library validator

        [Test, Description("Rule 15: a table with every mandatory row and valid references passes")]
        public void Validate_CompleteTable_Passes()
        {
            var errors = new List<SkillError>();
            SkillLibrary lib = LoadAll(errors, Skill(G1).Source, Skill(A1).Source, Skill(D1).Source, DiveSkill.Source, FullTable);

            Assert.IsEmpty(errors);
            Assert.IsTrue(SkillLibraryValidator.Validate(lib, errors), string.Join("; ", errors));
        }

        [Test, Description("Rule 15: missing context rows are reported")]
        public void Validate_MissingMandatoryRows_Reported()
        {
            var errors = new List<SkillError>();
            SkillLibrary lib = LoadAll(errors, Skill(G1).Source,
                new SkillSourceText("Tables/spear.xml", Table("spear", $"<row skill=\"{G1}\" context=\"ground\"/>")));

            Assert.IsFalse(SkillLibraryValidator.Validate(lib, errors));
            Assert.AreEqual(2, errors.Count(e => e.Code == "MissingMandatoryRow"), string.Join("; ", errors));
        }

        [Test, Description("Cross references: missing skill / after, creature skill in a command table")]
        public void Validate_BadReferences_Reported()
        {
            var errors = new List<SkillError>();
            Fx creature = Skill("100007", " owner=\"creature\"");
            SkillLibrary lib = LoadAll(errors, Skill(G1).Source, creature.Source,
                new SkillSourceText("Tables/spear.xml", Table("spear",
                    "<row skill=\"100090\" context=\"ground\"/>",
                    $"<row skill=\"{G1}\" context=\"ground\" after=\"100091\"/>",
                    "<row skill=\"100007\" context=\"ground\"/>")));
            Assert.IsEmpty(errors, string.Join("; ", errors));

            SkillLibraryValidator.Validate(lib, errors);

            var codes = errors.Select(e => e.Code).ToList();
            CollectionAssert.IsSubsetOf(new[] { "MissingSkill", "MissingAfter", "RowCreatureSkill" }, codes);
        }

        [Test, Description("Rows reference skills by 6-digit id; after entries are skill ids only (tag links removed 2026-11-08)")]
        public void TryParseTable_NonNumericRowIds_Rejected()
        {
            var errors = new List<SkillError>();

            Assert.IsFalse(SkillXmlReader.TryParseTable(Table("spear", "<row skill=\"skl.g1\" context=\"ground\"/>"), out _, errors));
            Assert.IsFalse(SkillXmlReader.TryParseTable(Table("spear", $"<row skill=\"{G1}\" after=\"skl.g0\"/>"), out _, errors));
            Assert.IsFalse(SkillXmlReader.TryParseTable(Table("spear", $"<row skill=\"{G1}\" after=\"tag:light\"/>"), out _, errors));
            Assert.IsTrue(SkillXmlReader.TryParseTable(Table("spear", $"<row skill=\"{G1}\" after=\"100000\"/>"), out _, errors));
            Assert.AreEqual(3, errors.Count(e => e.Code == "IdFormat"));
        }

        [Test, Description("Rule 15: moving is meaningful on ground rows only")]
        public void TryParseTable_MovingOnAirRow_Rejected()
        {
            var errors = new List<SkillError>();

            Assert.IsFalse(SkillXmlReader.TryParseTable(Table("spear", $"<row skill=\"{A1}\" context=\"air\" moving=\"moving\"/>"), out _, errors));
            Assert.IsTrue(errors.Any(e => e.Code == "MovingOnlyGround"));
        }

        // ------------------------------------------------------------ payload hash

        [Test, Description("ADR-0002 Decision 6: the hash ignores its own value and changes with any other byte")]
        public void PayloadHash_IgnoresOwnValueOnly()
        {
            string a = GuardRush.Replace("payload_hash=\"p\"", "payload_hash=\"\"");
            string hash = SkillHash.PayloadHash(a);
            string stamped = GuardRush.Replace("payload_hash=\"p\"", $"payload_hash=\"{hash}\"");
            string edited = stamped.Replace("v=\"0.1\"", "v=\"0.2\"");

            Assert.AreEqual(64, hash.Length);
            Assert.AreEqual(hash, SkillHash.PayloadHash(stamped));
            Assert.IsTrue(SkillHash.Matches(stamped, hash));
            Assert.IsFalse(SkillHash.Matches(edited, hash));
        }

        [Test, Description("With verification on, a hand-edited body is reported but the skill still loads")]
        public void Load_VerifyPayloadHash_ReportsMismatch()
        {
            var errors = new List<SkillError>();

            SkillLibrary lib = SkillLoader.Load(new ListSource(new SkillSourceText("100020.xml", GuardRush)), errors, verifyPayloadHash: true);

            Assert.IsTrue(lib.TryGet("100020", out _));
            Assert.IsTrue(errors.Any(e => e.Code == "PayloadHashMismatch"));
        }
    }
}

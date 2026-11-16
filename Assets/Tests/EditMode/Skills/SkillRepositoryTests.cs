using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>
    /// Hot reload of skill data (user request 2026-11-11: saving in the Skill Tool during Play Mode updates the skill in the
    /// running game, no restart): one file at a time, bad saves rejected with the old version kept, same text a no-op.
    /// </summary>
    public class SkillRepositoryTests
    {
        private const string A = "100001", B = "100002";

        private sealed class ListSource : ISkillTextSource
        {
            public readonly List<SkillSourceText> Files = new List<SkillSourceText>();

            public void ReadAll(List<SkillSourceText> into) => into.AddRange(Files);
        }

        private static string Anim(string id, string clip) => Skill(id).Xml.Replace("v=\"a\"", $"v=\"{clip}\"");

        private static string AnimOf(SkillRepository repo, string id)
        {
            Assert.IsTrue(repo.Library.TryGet(id, out SkillDef def), id);
            return def.Node(1).Get("AnimationType");
        }

        private static SkillRepository Loaded(out List<SkillReloadEvent> events, params SkillSourceText[] files)
        {
            var source = new ListSource();
            source.Files.AddRange(files);
            var repo = new SkillRepository(source);
            repo.LoadAll(null);
            var list = new List<SkillReloadEvent>();
            repo.Changed += list.Add;
            events = list;
            return repo;
        }

        [Test]
        public void LoadAll_EveryFileLoaded_VersionOne()
        {
            SkillRepository repo = Loaded(out _, Skill(A).Source, Skill(B).Source,
                new SkillSourceText("Tables/spear.xml", Table("spear", $"<row skill=\"{A}\" context=\"ground\"/>")));

            Assert.AreEqual(2, repo.Library.SkillCount);
            Assert.IsTrue(repo.Library.TryGetTable("spear", out _));
            Assert.AreEqual(1, repo.Version);
        }

        [Test]
        public void ReloadText_EditedSkill_NextLookupGetsNewVersion_RunningCastKeepsOld()
        {
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source);
            SkillLibrary library = repo.Library;
            repo.Library.TryGet(A, out SkillDef running);

            SkillReloadResult r = repo.ReloadText(A + ".xml", Anim(A, "slash_02"), null);

            Assert.AreEqual(SkillReloadResult.Updated, r);
            Assert.AreEqual("slash_02", AnimOf(repo, A));
            Assert.AreEqual("a", running.Node(1).Get("AnimationType"), "a SkillDef is immutable: the running cast finishes on it");
            Assert.AreSame(library, repo.Library, "systems holding the library see the change");
            Assert.AreEqual(2, repo.Version);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(A, events[0].Subject);
            Assert.AreEqual(A + ".xml", events[0].FileName);
            Assert.AreEqual(SkillReloadResult.Updated, events[0].Result);
            Assert.AreEqual(2, events[0].Version);
        }

        [Test]
        public void ReloadText_SameTextTwice_UnchangedWithoutEvent()
        {
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source);

            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText(A + ".xml", Skill(A).Xml, null), "same as loaded");
            Assert.AreEqual(SkillReloadResult.Updated, repo.ReloadText(A + ".xml", Anim(A, "b"), null));
            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText(A + ".xml", Anim(A, "b"), null), "Save + import report the same file twice");

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(2, repo.Version);
        }

        [Test]
        public void ReloadText_BrokenSave_RejectedOldVersionKeptAndErrorsReported()
        {
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source);
            var errors = new List<SkillError>();

            SkillReloadResult r = repo.ReloadText(A + ".xml", Body(@"<node id=""1"" type=""Animation""/>"), errors);

            Assert.AreEqual(SkillReloadResult.Rejected, r);
            Assert.AreEqual("a", AnimOf(repo, A), "the game never loses a skill to a bad save");
            Assert.IsNotEmpty(errors);
            StringAssert.StartsWith(A + ".xml: ", errors[0].Message);
            Assert.AreEqual(0, events.Count);
            Assert.AreEqual(1, repo.Version);
            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText(A + ".xml", Skill(A).Xml, null),
                "the rejected text was not remembered: the loaded text is still the reference");
            Assert.AreEqual(SkillReloadResult.Updated, repo.ReloadText(A + ".xml", Anim(A, "fixed"), null), "the fixed save applies");
            Assert.AreEqual("fixed", AnimOf(repo, A));
        }

        [Test]
        public void ReloadText_NotXml_Rejected()
        {
            SkillRepository repo = Loaded(out _, Skill(A).Source);
            var errors = new List<SkillError>();

            Assert.AreEqual(SkillReloadResult.Rejected, repo.ReloadText(A + ".xml", "not xml", errors));
            Assert.AreEqual("XmlUnreadable", errors.Single().Code);
            Assert.AreEqual(SkillReloadResult.Rejected, repo.ReloadText("x.xml", "<other/>", errors));
            Assert.AreEqual("UnknownNode", errors.Last().Code);
        }

        [Test]
        public void ReloadText_NewFile_Added()
        {
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source);

            Assert.AreEqual(SkillReloadResult.Added, repo.ReloadText(B + ".xml", Skill(B).Xml, null));

            Assert.IsTrue(repo.Library.TryGet(B, out _));
            Assert.AreEqual(SkillReloadResult.Added, events.Single().Result);
        }

        [Test]
        public void FileName_SlashesAndSubfolders_SameFile()
        {
            SkillRepository repo = Loaded(out _, new SkillSourceText("Boss/" + A + ".xml", Skill(A).Xml));

            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText(@"Boss\" + A + ".xml", Skill(A).Xml, null));
            Assert.AreEqual(SkillReloadResult.Updated, repo.ReloadText("/Boss/" + A + ".xml", Anim(A, "x"), null));
            Assert.AreEqual(SkillReloadResult.Unchanged, repo.ReloadText("Boss/" + A + ".xml", Anim(A, "x"), null), "leading slash is the same file");
            Assert.AreEqual(SkillReloadResult.Removed, repo.RemoveFile(@"Boss\" + A + ".xml"));
            Assert.AreEqual(SkillReloadResult.Ignored, repo.RemoveFile("/Boss/" + A + ".xml"), "one file, removed once");
        }

        [Test]
        public void RemoveFile_DeletedSkill_RemovedOnceThenIgnored()
        {
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source, Skill(B).Source);

            Assert.AreEqual(SkillReloadResult.Removed, repo.RemoveFile(A + ".xml"));
            Assert.AreEqual(SkillReloadResult.Ignored, repo.RemoveFile(A + ".xml"));
            Assert.AreEqual(SkillReloadResult.Ignored, repo.RemoveFile("unknown.xml"));

            Assert.IsFalse(repo.Library.TryGet(A, out _));
            Assert.IsTrue(repo.Library.TryGet(B, out _));
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(A, events[0].Subject);
            Assert.AreEqual(SkillReloadResult.Added, repo.ReloadText(A + ".xml", Skill(A).Xml, null), "restored file comes back");
        }

        [Test]
        public void ReloadText_TableChangesFamily_OldFamilyDropped()
        {
            const string file = "Tables/t.xml";
            SkillRepository repo = Loaded(out List<SkillReloadEvent> events, Skill(A).Source,
                new SkillSourceText(file, Table("spear", $"<row skill=\"{A}\" context=\"ground\"/>")));

            SkillReloadResult r = repo.ReloadText(file, Table("sword", $"<row skill=\"{A}\" context=\"ground\"/>"), null);

            Assert.AreEqual(SkillReloadResult.Added, r);
            Assert.IsFalse(repo.Library.TryGetTable("spear", out _));
            Assert.IsTrue(repo.Library.TryGetTable("sword", out _));
            Assert.AreEqual("table:sword", events.Single().Subject);
            Assert.AreEqual(SkillReloadResult.Removed, repo.RemoveFile(file));
            Assert.IsFalse(repo.Library.TryGetTable("sword", out _));
        }

        [Test]
        public void ReloadText_TableEdited_SelectUsesNewRows()
        {
            const string file = "Tables/spear.xml";
            SkillRepository repo = Loaded(out _, Skill(A).Source, Skill(B).Source,
                new SkillSourceText(file, Table("spear", $"<row skill=\"{A}\" context=\"ground\"/>")));
            var q = new SelectionQuery(SkillContext.Ground, false, SkillDir.Neutral, null, 0, 0);
            Assert.AreEqual(A, repo.Library.Select("spear", in q, null).Id);

            Assert.AreEqual(SkillReloadResult.Updated, repo.ReloadText(file, Table("spear", $"<row skill=\"{B}\" context=\"ground\"/>"), null));

            Assert.AreEqual(B, repo.Library.Select("spear", in q, null).Id);
        }

        [Test]
        public void ReloadText_HandEditedPayload_AcceptedWithWarningWhenVerifying()
        {
            var repo = new SkillRepository(new ListSource(), verifyPayloadHash: true);
            repo.LoadAll(null);
            var errors = new List<SkillError>();

            Assert.AreEqual(SkillReloadResult.Added, repo.ReloadText("100005.xml", GuardRush, errors));

            Assert.AreEqual("PayloadHashMismatch", errors.Single().Code);
            Assert.IsTrue(repo.Library.TryGet("100005", out _));
        }

        [Test]
        public void LoadAll_AfterReloads_ReadsSourceAgain()
        {
            var source = new ListSource();
            source.Files.Add(Skill(A).Source);
            var repo = new SkillRepository(source);
            repo.LoadAll(null);
            repo.ReloadText(B + ".xml", Skill(B).Xml, null);

            repo.LoadAll(null);

            Assert.IsFalse(repo.Library.TryGet(B, out _), "full reload = what the folder holds");
            Assert.AreEqual(3, repo.Version);
        }

        [Test]
        public void Library_ReplaceAndRemove_OverwriteAndDelete()
        {
            SkillLibrary lib = Library(Skill(A));
            SkillDef newer = Parse(Anim(A, "z"), A);

            lib.Replace(newer);
            Assert.IsTrue(lib.TryGet(A, out SkillDef got));
            Assert.AreSame(newer, got);
            Assert.AreEqual(1, lib.SkillCount);
            Assert.IsTrue(lib.Remove(A));
            Assert.IsFalse(lib.Remove(A));
            Assert.IsFalse(lib.Remove(null));
            Assert.AreEqual(0, lib.SkillCount);
        }
    }
}

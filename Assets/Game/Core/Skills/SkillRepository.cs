using System;
using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>What a hot reload did to one file.</summary>
    public enum SkillReloadResult
    {
        /// <summary>Same text as the loaded version: nothing to do.</summary>
        Unchanged,
        /// <summary>A skill or table that was loaded got its new version.</summary>
        Updated,
        /// <summary>A new skill or table was added.</summary>
        Added,
        /// <summary>The file was deleted and its skill or table removed.</summary>
        Removed,
        /// <summary>The new text has errors: the loaded version stays and the errors are reported.</summary>
        Rejected,
        /// <summary>The file was not known to the repository (deleting an unknown file).</summary>
        Ignored
    }

    /// <summary>One change applied by <see cref="SkillRepository"/>.</summary>
    public readonly struct SkillReloadEvent
    {
        /// <summary>File name relative to the skills folder (e.g. <c>100001.xml</c>).</summary>
        public readonly string FileName;
        /// <summary>Skill id, or <c>table:&lt;family&gt;</c> for a command table.</summary>
        public readonly string Subject;
        /// <summary>What happened.</summary>
        public readonly SkillReloadResult Result;
        /// <summary><see cref="SkillRepository.Version"/> after the change.</summary>
        public readonly int Version;

        /// <summary>Creates an event.</summary>
        public SkillReloadEvent(string fileName, string subject, SkillReloadResult result, int version)
        {
            FileName = fileName;
            Subject = subject;
            Result = result;
            Version = version;
        }

        /// <inheritdoc />
        public override string ToString() => $"{Result} {Subject} ({FileName}, v{Version})";
    }

    /// <summary>
    /// The game's live skill data: the <see cref="SkillLibrary"/> loaded from the skills folder, updated one file at a time
    /// while the game runs (hot reload: the Skill Tool saves a file in Play Mode → the next cast uses it, no restart).
    /// <para>Rules: callers look skills up by id on every cast (<see cref="SkillLibrary.TryGet"/> / <c>Select</c>) and never
    /// keep a <see cref="SkillDef"/> across casts; a cast already running finishes on the definition it started with. A file
    /// whose new text has errors is rejected and the loaded version stays (the game never loses a skill to a bad save).
    /// Same text twice is a no-op, so the editor may report one save through several paths. Main thread only.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// var repo = new SkillRepository(new FolderSkillTextSource(skillsDir));
    /// repo.LoadAll(errors);
    /// repo.Changed += e => log.Info("[Skills] " + e);
    /// repo.ReloadText("100001.xml", File.ReadAllText(path), errors);   // after the Skill Tool saved it
    /// repo.Library.TryGet("100001", out SkillDef skill);                 // the new version
    /// </code>
    /// </example>
    public sealed class SkillRepository
    {
        private const string TablePrefix = "table:";

        private readonly ISkillTextSource _source;
        private readonly bool _verifyPayloadHash;
        private readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _subjects = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<SkillError> _local = new List<SkillError>();

        /// <summary>Creates an empty repository reading from <paramref name="source"/> (call <see cref="LoadAll"/>).</summary>
        public SkillRepository(ISkillTextSource source, bool verifyPayloadHash = false)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _verifyPayloadHash = verifyPayloadHash;
            Library = new SkillLibrary();
        }

        /// <summary>The current library (the same object across single-file reloads; replaced by <see cref="LoadAll"/>).</summary>
        public SkillLibrary Library { get; private set; }

        /// <summary>Increases by one for every applied change (load, add, update, remove).</summary>
        public int Version { get; private set; }

        /// <summary>Raised after each applied change (not for <see cref="SkillReloadResult.Unchanged"/>, <c>Rejected</c>, <c>Ignored</c>).</summary>
        public event Action<SkillReloadEvent> Changed;

        /// <summary>(Re)loads every file of the source; problems go to <paramref name="errors"/> (nullable).</summary>
        public void LoadAll(List<SkillError> errors)
        {
            var texts = new List<SkillSourceText>();
            _source.ReadAll(texts);
            Library = SkillLoader.Load(texts, errors, _verifyPayloadHash);
            _texts.Clear();
            _subjects.Clear();
            foreach (SkillSourceText t in texts)
            {
                string subject = SubjectOf(t.Name, t.Xml);
                if (subject == null) continue;
                _texts[t.Name] = t.Xml;
                _subjects[t.Name] = subject;
            }
            Version++;
            Changed?.Invoke(new SkillReloadEvent("*", "*", SkillReloadResult.Updated, Version));
        }

        /// <summary>
        /// Applies the new text of one file (<paramref name="fileName"/> relative to the skills folder; the skill id is the
        /// file name). Returns what happened; on <see cref="SkillReloadResult.Rejected"/> the errors go to
        /// <paramref name="errors"/> (nullable) and the loaded version stays.
        /// </summary>
        public SkillReloadResult ReloadText(string fileName, string xml, List<SkillError> errors)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("file name required", nameof(fileName));
            string name = Normalize(fileName);
            xml = xml ?? "";
            if (_texts.TryGetValue(name, out string old) && old == xml) return SkillReloadResult.Unchanged;

            _local.Clear();
            string root = SkillXmlReader.RootName(xml);
            string subject;
            bool existed;
            if (root == "skill")
            {
                if (!SkillXmlReader.TryParse(xml, SkillXmlReader.IdFromFileName(name), out SkillDef skill, _local)) return Reject(name, errors);
                if (_verifyPayloadHash && skill.PayloadHash.Length > 0 && !SkillHash.Matches(xml, skill.PayloadHash))
                    _local.Add(new SkillError(skill.Id, "PayloadHashMismatch", "payload_hash does not match the body (edited by hand?)"));
                subject = skill.Id;
                existed = Library.TryGet(skill.Id, out _);
                Library.Replace(skill);
            }
            else if (root == "command_table")
            {
                if (!SkillXmlReader.TryParseTable(xml, out CommandTable table, _local)) return Reject(name, errors);
                subject = TablePrefix + table.WeaponFamily;
                // The file may have changed its family: drop the table it defined before.
                if (_subjects.TryGetValue(name, out string before) && before != subject && before.StartsWith(TablePrefix, StringComparison.Ordinal))
                    Library.RemoveTable(before.Substring(TablePrefix.Length));
                existed = Library.TryGetTable(table.WeaponFamily, out _);
                Library.ReplaceTable(table);
            }
            else
            {
                _local.Add(new SkillError("?", root == null ? "XmlUnreadable" : "UnknownNode",
                    root == null ? "not readable XML" : $"root <{root}> is neither <skill> nor <command_table>"));
                return Reject(name, errors);
            }

            Report(name, errors);   // warnings of an accepted file (payload hash)
            _texts[name] = xml;
            _subjects[name] = subject;
            Version++;
            SkillReloadResult result = existed ? SkillReloadResult.Updated : SkillReloadResult.Added;
            Changed?.Invoke(new SkillReloadEvent(name, subject, result, Version));
            return result;
        }

        /// <summary>Removes the skill or table that <paramref name="fileName"/> defined (the file was deleted or moved away).</summary>
        public SkillReloadResult RemoveFile(string fileName)
        {
            string name = Normalize(fileName ?? "");
            if (!_subjects.TryGetValue(name, out string subject)) return SkillReloadResult.Ignored;
            if (subject.StartsWith(TablePrefix, StringComparison.Ordinal)) Library.RemoveTable(subject.Substring(TablePrefix.Length));
            else Library.Remove(subject);
            _subjects.Remove(name);
            _texts.Remove(name);
            Version++;
            Changed?.Invoke(new SkillReloadEvent(name, subject, SkillReloadResult.Removed, Version));
            return SkillReloadResult.Removed;
        }

        private SkillReloadResult Reject(string name, List<SkillError> errors)
        {
            Report(name, errors);
            return SkillReloadResult.Rejected;
        }

        private void Report(string name, List<SkillError> errors)
        {
            if (errors == null) return;
            foreach (SkillError e in _local) errors.Add(new SkillError(e.Subject, e.Code, $"{name}: {e.Message}"));
        }

        private static string SubjectOf(string name, string xml)
        {
            string root = SkillXmlReader.RootName(xml);
            if (root == "skill") return SkillXmlReader.IdFromFileName(name);
            if (root == "command_table" && SkillXmlReader.TryParseTable(xml, out CommandTable table, new List<SkillError>()))
                return TablePrefix + table.WeaponFamily;
            return null;
        }

        private static string Normalize(string fileName) => fileName.Replace('\\', '/').TrimStart('/');
    }
}

using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>Every loaded moveset by id. Main thread only.</summary>
    /// <example>
    /// <code>
    /// if (library.TryGet("100000", out MovesetDef basic)) loadout.TrySocket(0, basic, conflicts);
    /// </code>
    /// </example>
    public sealed class MovesetLibrary
    {
        private readonly Dictionary<string, MovesetDef> _byId = new Dictionary<string, MovesetDef>(StringComparer.Ordinal);

        /// <summary>Number of movesets.</summary>
        public int Count => _byId.Count;

        /// <summary>Every moveset (no particular order).</summary>
        public IEnumerable<MovesetDef> All => _byId.Values;

        /// <summary>Adds or overwrites a moveset.</summary>
        public void Replace(MovesetDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            _byId[def.Id] = def;
        }

        /// <summary>Removes a moveset; false when unknown.</summary>
        public bool Remove(string id) => id != null && _byId.Remove(id);

        /// <summary>Looks a moveset up.</summary>
        public bool TryGet(string id, out MovesetDef def)
        {
            if (id != null) return _byId.TryGetValue(id, out def);
            def = null;
            return false;
        }
    }

    /// <summary>
    /// The game's live movesets: loads the folder and applies single-file changes while the game runs (hot reload from the
    /// Moveset Tool), with the same rules as <see cref="SkillRepository"/>: a file with errors is rejected and the loaded
    /// version stays; the same text twice is a no-op; callers resolve movesets by id when they need them. Main thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// var repo = new MovesetRepository(new FolderSkillTextSource(movesetsDir));
    /// repo.LoadAll(errors);
    /// repo.ReloadText("100000.xml", xml, errors);      // after the Moveset Tool saved it
    /// </code>
    /// </example>
    public sealed class MovesetRepository
    {
        private readonly ISkillTextSource _source;
        private readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _ids = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Creates an empty repository reading from <paramref name="source"/>.</summary>
        public MovesetRepository(ISkillTextSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>The live library (same object across single-file reloads; replaced by <see cref="LoadAll"/>).</summary>
        public MovesetLibrary Library { get; private set; } = new MovesetLibrary();

        /// <summary>Increases by one for every applied change.</summary>
        public int Version { get; private set; }

        /// <summary>Raised after each applied change.</summary>
        public event Action<SkillReloadEvent> Changed;

        /// <summary>(Re)loads every file; problems go to <paramref name="errors"/> (nullable). Later duplicates of an id are reported and skipped.</summary>
        public void LoadAll(List<SkillError> errors)
        {
            var texts = new List<SkillSourceText>();
            _source.ReadAll(texts);
            Library = new MovesetLibrary();
            _texts.Clear();
            _ids.Clear();
            var local = new List<SkillError>();
            foreach (SkillSourceText t in texts)
            {
                local.Clear();
                string id = MovesetXml.IdFromFileName(t.Name);
                if (!MovesetXml.TryParse(t.Xml, id, out MovesetDef def, local)) { }
                else if (Library.TryGet(id, out _)) local.Add(new SkillError("moveset:" + id, "DuplicateId", "another file already defines this id; the first one is kept"));
                else
                {
                    Library.Replace(def);
                    _texts[Normalize(t.Name)] = t.Xml;
                    _ids[Normalize(t.Name)] = id;
                }
                if (errors != null) foreach (SkillError e in local) errors.Add(new SkillError(e.Subject, e.Code, $"{t.Name}: {e.Message}"));
            }
            Version++;
            Changed?.Invoke(new SkillReloadEvent("*", "*", SkillReloadResult.Updated, Version));
        }

        /// <summary>Applies the new text of one file. See <see cref="SkillRepository.ReloadText"/> for the results.</summary>
        public SkillReloadResult ReloadText(string fileName, string xml, List<SkillError> errors)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("file name required", nameof(fileName));
            string name = Normalize(fileName);
            xml = xml ?? "";
            if (_texts.TryGetValue(name, out string old) && old == xml) return SkillReloadResult.Unchanged;
            string id = MovesetXml.IdFromFileName(name);
            var local = new List<SkillError>();
            bool ok = MovesetXml.TryParse(xml, id, out MovesetDef def, local);
            if (errors != null) foreach (SkillError e in local) errors.Add(new SkillError(e.Subject, e.Code, $"{name}: {e.Message}"));
            if (!ok) return SkillReloadResult.Rejected;
            bool existed = Library.TryGet(id, out _);
            Library.Replace(def);
            _texts[name] = xml;
            _ids[name] = id;
            Version++;
            SkillReloadResult result = existed ? SkillReloadResult.Updated : SkillReloadResult.Added;
            Changed?.Invoke(new SkillReloadEvent(name, id, result, Version));
            return result;
        }

        /// <summary>Removes the moveset a deleted file defined.</summary>
        public SkillReloadResult RemoveFile(string fileName)
        {
            string name = Normalize(fileName ?? "");
            if (!_ids.TryGetValue(name, out string id)) return SkillReloadResult.Ignored;
            Library.Remove(id);
            _ids.Remove(name);
            _texts.Remove(name);
            Version++;
            Changed?.Invoke(new SkillReloadEvent(name, id, SkillReloadResult.Removed, Version));
            return SkillReloadResult.Removed;
        }

        private static string Normalize(string fileName) => fileName.Replace('\\', '/').TrimStart('/');
    }
}

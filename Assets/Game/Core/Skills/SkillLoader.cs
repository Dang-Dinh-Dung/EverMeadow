using System;
using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Builds a <see cref="SkillLibrary"/> from an <see cref="ISkillTextSource"/> once at boot (ADR-0002 Decision 4). Each text
    /// is routed by its root element (<c>skill</c> or <c>command_table</c>); a skill's id is its file name (<c>100123.xml</c>). A rejected file is left out and reported; rows that
    /// point at it fall through to the next row or the fallback at selection (GDD Rule 22), so a bad file never stops the game.
    /// </summary>
    /// <example>
    /// <code>
    /// var errors = new List&lt;SkillError&gt;();
    /// SkillLibrary library = SkillLoader.Load(new FolderSkillTextSource(skillsFolder), errors);
    /// foreach (SkillError e in errors) Debug.LogWarning(e.ToString());   // log once per (id, reason)
    /// </code>
    /// </example>
    public static class SkillLoader
    {
        /// <summary>Reads every text from <paramref name="source"/> and parses it into a new library.</summary>
        /// <param name="source">Where the XML comes from.</param>
        /// <param name="errors">Receives every problem; the message starts with the file name.</param>
        /// <param name="verifyPayloadHash">Also report <c>PayloadHashMismatch</c> when a non-empty <c>payload_hash</c> does not match
        /// the body (the skill is still loaded: the file is well-formed, only hand-edited).</param>
        public static SkillLibrary Load(ISkillTextSource source, List<SkillError> errors, bool verifyPayloadHash = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var texts = new List<SkillSourceText>();
            source.ReadAll(texts);
            return Load(texts, errors, verifyPayloadHash);
        }

        /// <summary>Parses already-read texts into a new library.</summary>
        public static SkillLibrary Load(IReadOnlyList<SkillSourceText> texts, List<SkillError> errors, bool verifyPayloadHash = false)
        {
            var library = new SkillLibrary();
            var local = new List<SkillError>();
            for (int i = 0; i < texts.Count; i++)
            {
                SkillSourceText text = texts[i];
                local.Clear();
                string root = SkillXmlReader.RootName(text.Xml);
                if (root == "skill")
                {
                    if (SkillXmlReader.TryParse(text.Xml, SkillXmlReader.IdFromFileName(text.Name), out SkillDef skill, local))
                    {
                        if (!library.Add(skill))
                            local.Add(new SkillError(skill.Id, "DuplicateId", "another file already defines this id; the first one is kept"));
                        else if (verifyPayloadHash && skill.PayloadHash.Length > 0 && !SkillHash.Matches(text.Xml, skill.PayloadHash))
                            local.Add(new SkillError(skill.Id, "PayloadHashMismatch", "payload_hash does not match the body (edited by hand?)"));
                    }
                }
                else if (root == "command_table")
                {
                    if (SkillXmlReader.TryParseTable(text.Xml, out CommandTable table, local) && !library.AddTable(table))
                        local.Add(new SkillError("table:" + table.WeaponFamily, "DuplicateTable",
                            "another file already defines this family's table; the first one is kept"));
                }
                else
                {
                    local.Add(new SkillError("?", root == null ? "XmlUnreadable" : "UnknownNode",
                        root == null ? "not readable XML" : $"root <{root}> is neither <skill> nor <command_table>"));
                }

                if (errors == null) continue;
                foreach (SkillError e in local) errors.Add(new SkillError(e.Subject, e.Code, $"{text.Name}: {e.Message}"));
            }
            return library;
        }
    }
}

using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Library-level checks of ADR-0002 Decision 5 for command tables (combo system data): every row's skill exists and is a
    /// player skill, every follow-up link points at a loaded skill, and each table has a plain row for ground, air and dash
    /// (GDD Rule 15). Skill files only describe what a skill shows and touches (user decision 2026-11-08); rules about how a
    /// skill is used (cancel windows, charging, movement, armor) belong to the combo system. Runs in the editor, at build and
    /// in CI.
    /// </summary>
    /// <example>
    /// <code>
    /// var errors = new List&lt;SkillError&gt;();
    /// if (!SkillLibraryValidator.Validate(library, errors)) throw new BuildFailedException(string.Join("\n", errors));
    /// </code>
    /// </example>
    public static class SkillLibraryValidator
    {
        private static readonly SkillContext[] MandatoryContexts = { SkillContext.Ground, SkillContext.Air, SkillContext.Dash };

        /// <summary>Validates every command table against the loaded skills. Returns false when any error was added.</summary>
        public static bool Validate(SkillLibrary library, List<SkillError> errors)
        {
            int before = errors.Count;
            foreach (CommandTable table in library.Tables) ValidateTable(library, table, errors);
            return errors.Count == before;
        }

        private static void ValidateTable(SkillLibrary library, CommandTable table, List<SkillError> errors)
        {
            string subject = "table:" + table.WeaponFamily;
            foreach (CommandRow row in table.Rows)
            {
                string at = $"row {row.Order} ({row.SkillId})";
                if (!library.TryGet(row.SkillId, out SkillDef skill))
                {
                    errors.Add(new SkillError(subject, "MissingSkill", $"{at}: no loaded skill has this id"));
                    continue;
                }
                if (skill.Owner != SkillOwner.Player)
                    errors.Add(new SkillError(subject, "RowCreatureSkill", $"{at}: command tables hold player skills only"));
                foreach (string id in row.AfterIds)
                    if (!library.TryGet(id, out _))
                        errors.Add(new SkillError(subject, "MissingAfter", $"{at}: after '{id}' is not a loaded skill"));
            }

            foreach (SkillContext context in MandatoryContexts)
            {
                bool found = false;
                foreach (CommandRow row in table.Rows)
                    if ((row.Context & context) != 0 && row.AnyDir && !row.HasAfter && row.Moving == MovingFilter.Any && library.TryGet(row.SkillId, out _))
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    errors.Add(new SkillError(subject, "MissingMandatoryRow",
                        $"needs a dir=any row without after for context {context.ToString().ToLowerInvariant()} (GDD Rule 15)"));
            }
        }
    }
}

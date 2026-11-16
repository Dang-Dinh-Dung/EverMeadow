using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>
    /// The weapon family list the tools offer (Moveset Tool "Weapons", Socket test). Data, not code: a text file, one family
    /// per line, <c>#</c> starts a comment. Until the Weapon Tree GDD names the real weapons, the defaults are the five
    /// animation families of the art bible (shape families: crescent, slab, needle, ring, chain).
    /// </summary>
    /// <example>
    /// <code>
    /// List&lt;string&gt; families = WeaponFamilies.Parse(File.ReadAllText(path), errors);   // ["crescent", "slab", …]
    /// </code>
    /// </example>
    public static class WeaponFamilies
    {
        /// <summary>Families used when the list file does not exist (art bible §"Silhouette vũ khí").</summary>
        public static readonly string[] Defaults = { "crescent", "slab", "needle", "ring", "chain" };

        /// <summary>Content of a new list file: a header comment and <see cref="Defaults"/>.</summary>
        public static string DefaultFileText =>
            "# Weapon families offered by the Moveset Tool (one per line, [a-z0-9._], '#' = comment).\n" +
            "# Defaults = the five animation families of the art bible; the Weapon Tree GDD will set the real list.\n" +
            string.Join("\n", Defaults) + "\n";

        /// <summary>Parses the list: trims lines, skips blanks and comments, reports bad names and repeats (kept once).</summary>
        public static List<string> Parse(string text, List<SkillError> errors)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0) continue;
                if (!SkillXmlReader.IsName(line) || line == "any")
                    errors?.Add(new SkillError("weapons", "IdFormat", $"line {i + 1}: '{line}' must be [a-z0-9._]+ and not 'any'"));
                else if (!seen.Add(line))
                    errors?.Add(new SkillError("weapons", "DuplicateWeapon", $"line {i + 1}: '{line}' is listed twice"));
                else list.Add(line);
            }
            return list;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>
    /// Moveset XML (<c>StreamingAssets/Movesets/&lt;id&gt;.xml</c>):
    /// <code>
    /// &lt;moveset schema_version="1" name="Đánh thường" weapons="any"&gt;
    ///   &lt;note&gt;…&lt;/note&gt;
    ///   &lt;row input="Button1" skill="100000" context="ground"/&gt;
    ///   &lt;row input="Button1" skill="100001" context="ground" after="100000" link_window="0.4"/&gt;
    ///   &lt;row input="Button1+Button2" skill="100009" context="ground|air" cooldown="6"/&gt;
    /// &lt;/moveset&gt;
    /// </code>
    /// Row attributes: <c>input</c>, <c>skill</c> (required); <c>context</c> (<c>any</c> or <c>ground|air|dash</c> joined by
    /// <c>|</c>), <c>moving</c> (<c>any|still|moving</c>), <c>dir</c> (<c>any|neutral|up|down|forward|back</c>), <c>after</c>
    /// (skill ids, comma separated), <c>link_window</c>, <c>priority</c>, <c>cooldown</c> (seconds). The writer writes only
    /// non-default values in a fixed order, so the same moveset always gives the same bytes.
    /// </summary>
    /// <example>
    /// <code>
    /// if (MovesetXml.TryParse(xml, "100000", out MovesetDef def, errors)) library.Replace(def);
    /// string text = MovesetXml.Write(data);
    /// </code>
    /// </example>
    public static class MovesetXml
    {
        /// <summary>Format version this build reads and writes.</summary>
        public const int SchemaVersion = 1;
        /// <summary>File extension.</summary>
        public const string Extension = ".xml";
        /// <summary>Default link window (seconds) of a follow-up row (same as the old command table).</summary>
        public const double DefaultLinkWindow = SkillXmlReader.DefaultLinkWindow;
        /// <summary>Every context.</summary>
        public const SkillContext AllContexts = SkillContext.Ground | SkillContext.Air | SkillContext.Dash;

        private static readonly string[] RootAttributes = { "schema_version", "name", "weapons" };
        private static readonly string[] RowAttributes = { "input", "skill", "context", "moving", "dir", "after", "link_window", "priority", "cooldown" };

        /// <summary>Reads + validates + compiles. False (and errors) when anything is wrong.</summary>
        public static bool TryParse(string xml, string id, out MovesetDef def, List<SkillError> errors, ISet<string> knownSkills = null)
        {
            def = null;
            var local = new List<SkillError>();
            if (!TryRead(xml, id, out MovesetData data, local)) { Copy(local, errors); return false; }
            MovesetValidator.Validate(data, local, knownSkills);
            Copy(local, errors);
            if (local.Count > 0) return false;
            def = new MovesetDef(data);
            return true;
        }

        /// <summary>
        /// Reads the structure leniently (the tool can open a file with mistakes): returns the data whenever the XML is
        /// readable; values that do not parse are reported and left at their defaults.
        /// </summary>
        public static bool TryRead(string xml, string id, out MovesetData data, List<SkillError> errors)
        {
            data = null;
            string subject = "moveset:" + (id ?? "?");
            try
            {
                using (XmlReader r = Open(xml))
                {
                    if (r == null || r.MoveToContent() != XmlNodeType.Element)
                    {
                        Add(errors, subject, "XmlUnreadable", "empty document");
                        return false;
                    }
                    if (r.LocalName != "moveset")
                    {
                        Add(errors, subject, "UnknownNode", $"root <{r.LocalName}> is not <moveset>");
                        return false;
                    }
                    data = new MovesetData { Id = id ?? "" };
                    ReadRoot(r, data, errors, subject);
                }
            }
            catch (XmlException e)
            {
                Add(errors, subject, "XmlUnreadable", e.Message);
                data = null;
            }
            return data != null;
        }

        /// <summary>Canonical XML of <paramref name="m"/> (UTF-8 declaration, LF, two-space indent).</summary>
        public static string Write(MovesetData m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\n", OmitXmlDeclaration = true };
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            using (XmlWriter w = XmlWriter.Create(new StringWriter(sb, CultureInfo.InvariantCulture), settings))
            {
                w.WriteStartElement("moveset");
                w.WriteAttributeString("schema_version", SchemaVersion.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(m.Name)) w.WriteAttributeString("name", m.Name);
                w.WriteAttributeString("weapons", m.AnyWeapon ? "any" : string.Join(",", m.Weapons));
                if (!string.IsNullOrEmpty(m.Note)) w.WriteElementString("note", m.Note);
                foreach (MovesetRowData row in m.Rows)
                {
                    w.WriteStartElement("row");
                    w.WriteAttributeString("input", row.Input.ToString());
                    w.WriteAttributeString("skill", row.Skill ?? "");
                    if (row.Context != AllContexts) w.WriteAttributeString("context", ContextText(row.Context));
                    if (row.Moving != MovingFilter.Any) w.WriteAttributeString("moving", row.Moving == MovingFilter.Still ? "still" : "moving");
                    if (!row.AnyDir) w.WriteAttributeString("dir", DirText(row.Dir));
                    if (row.After.Count > 0) w.WriteAttributeString("after", string.Join(",", row.After));
                    if (row.After.Count > 0 && !SameNumber(row.LinkWindow, DefaultLinkWindow)) w.WriteAttributeString("link_window", Num(row.LinkWindow));
                    if (row.Priority != 0) w.WriteAttributeString("priority", row.Priority.ToString(CultureInfo.InvariantCulture));
                    if (row.Cooldown != 0) w.WriteAttributeString("cooldown", Num(row.Cooldown));
                    w.WriteEndElement();
                }
                w.WriteEndElement();
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>Moveset id of a file name or relative path (<c>"stones/100002.xml"</c> → <c>"100002"</c>).</summary>
        public static string IdFromFileName(string name) => SkillXmlReader.IdFromFileName(name);

        /// <summary>True for a moveset id: six digits, 100000–999999.</summary>
        public static bool IsMovesetId(string s) => SkillXmlReader.IsSkillId(s);

        /// <summary>Text of a context set: <c>any</c> or e.g. <c>ground|air</c>.</summary>
        public static string ContextText(SkillContext c)
        {
            if (c == AllContexts) return "any";
            var parts = new List<string>(3);
            if ((c & SkillContext.Ground) != 0) parts.Add("ground");
            if ((c & SkillContext.Air) != 0) parts.Add("air");
            if ((c & SkillContext.Dash) != 0) parts.Add("dash");
            return parts.Count == 0 ? "none" : string.Join("|", parts);
        }

        /// <summary>Parses <c>any</c> or <c>ground|air|dash</c> parts; false for unknown parts or an empty set.</summary>
        public static bool TryContext(string text, out SkillContext c)
        {
            c = SkillContext.None;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (text.Trim() == "any")
            {
                c = AllContexts;
                return true;
            }
            foreach (string raw in text.Split('|'))
            {
                switch (raw.Trim())
                {
                    case "ground": c |= SkillContext.Ground; break;
                    case "air": c |= SkillContext.Air; break;
                    case "dash": c |= SkillContext.Dash; break;
                    default: return false;
                }
            }
            return c != SkillContext.None;
        }

        /// <summary>Text of a direction.</summary>
        public static string DirText(SkillDir d) => d.ToString().ToLowerInvariant();

        private static void ReadRoot(XmlReader r, MovesetData data, List<SkillError> errors, string subject)
        {
            CheckAttributes(r, RootAttributes, errors, subject, "<moveset>");
            string version = r.GetAttribute("schema_version");
            if (!int.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out int v) || v < 1)
                Add(errors, subject, "SchemaMissing", $"schema_version '{version}' must be a positive integer");
            else if (v > SchemaVersion)
                Add(errors, subject, "SchemaTooNew", $"schema_version {v} is newer than {SchemaVersion}");
            data.Name = r.GetAttribute("name") ?? "";
            string weapons = (r.GetAttribute("weapons") ?? "any").Trim();
            data.AnyWeapon = weapons == "any";
            if (!data.AnyWeapon) data.Weapons.AddRange(SplitList(weapons));

            if (r.IsEmptyElement) return;
            int depth = r.Depth;
            r.Read();
            while (!(r.NodeType == XmlNodeType.EndElement && r.Depth == depth) && !r.EOF)
            {
                if (r.NodeType != XmlNodeType.Element)
                {
                    r.Read();
                    continue;
                }
                if (r.LocalName == "note")
                {
                    data.Note = r.ReadElementContentAsString();
                    continue;
                }
                if (r.LocalName == "row") ReadRow(r, data, errors, subject);
                else Add(errors, subject, "UnknownNode", $"unexpected <{r.LocalName}> in <moveset>");
                r.Skip();
            }
        }

        private static void ReadRow(XmlReader r, MovesetData data, List<SkillError> errors, string subject)
        {
            int n = data.Rows.Count;
            var row = new MovesetRowData();
            CheckAttributes(r, RowAttributes, errors, subject, $"row {n}");

            string input = r.GetAttribute("input");
            if (!MoveInput.TryParse(input, out row.Input))
                Add(errors, subject, "BadInput", $"row {n}: input '{input}' must be ButtonN / KeyN, ButtonN:double, ButtonN:hold or A+B");
            row.Skill = r.GetAttribute("skill") ?? "";

            string context = r.GetAttribute("context");
            if (context != null && !TryContext(context, out row.Context))
                Add(errors, subject, "BadEnum", $"row {n}: context '{context}' must be any or ground|air|dash");

            switch (r.GetAttribute("moving") ?? "any")
            {
                case "any": break;
                case "still": row.Moving = MovingFilter.Still; break;
                case "moving": row.Moving = MovingFilter.Moving; break;
                default: Add(errors, subject, "BadEnum", $"row {n}: moving '{r.GetAttribute("moving")}' must be still, moving or any"); break;
            }

            string dir = r.GetAttribute("dir") ?? "any";
            if (dir != "any")
            {
                row.AnyDir = false;
                switch (dir)
                {
                    case "neutral": row.Dir = SkillDir.Neutral; break;
                    case "up": row.Dir = SkillDir.Up; break;
                    case "down": row.Dir = SkillDir.Down; break;
                    case "forward": row.Dir = SkillDir.Forward; break;
                    case "back": row.Dir = SkillDir.Back; break;
                    default:
                        row.AnyDir = true;
                        Add(errors, subject, "BadEnum", $"row {n}: dir '{dir}' must be neutral, up, down, forward, back or any");
                        break;
                }
            }

            row.After.AddRange(SplitList(r.GetAttribute("after")));
            row.LinkWindow = Number(r, "link_window", DefaultLinkWindow, n, errors, subject);
            row.Cooldown = Number(r, "cooldown", 0, n, errors, subject);
            string priority = r.GetAttribute("priority");
            if (priority != null && !int.TryParse(priority, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out row.Priority))
                Add(errors, subject, "BadNumber", $"row {n}: priority '{priority}' must be an integer");
            data.Rows.Add(row);
        }

        private static double Number(XmlReader r, string attribute, double fallback, int n, List<SkillError> errors, string subject)
        {
            string text = r.GetAttribute(attribute);
            if (text == null) return fallback;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v) && !double.IsInfinity(v)) return v;
            Add(errors, subject, "BadNumber", $"row {n}: {attribute} '{text}' must be a number");
            return fallback;
        }

        private static XmlReader Open(string xml)
        {
            if (xml == null) return null;
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true,
                CloseInput = true
            };
            return XmlReader.Create(new StringReader(xml), settings);
        }

        private static void CheckAttributes(XmlReader r, string[] allowed, List<SkillError> errors, string subject, string where)
        {
            if (!r.MoveToFirstAttribute()) return;
            do
            {
                if (r.Prefix == "xmlns" || r.LocalName == "xmlns") continue;
                if (Array.IndexOf(allowed, r.LocalName) < 0) Add(errors, subject, "UnknownAttribute", $"{where}: attribute '{r.LocalName}' is not allowed");
            } while (r.MoveToNextAttribute());
            r.MoveToElement();
        }

        private static string[] SplitList(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            var list = new List<string>();
            foreach (string p in text.Split(','))
            {
                string t = p.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list.ToArray();
        }

        private static string Num(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

        private static bool SameNumber(double a, double b) => Math.Abs(a - b) < 1e-9;

        private static void Add(List<SkillError> errors, string subject, string code, string message) => errors?.Add(new SkillError(subject, code, message));

        private static void Copy(List<SkillError> from, List<SkillError> to)
        {
            if (to != null) to.AddRange(from);
        }
    }

    /// <summary>
    /// Rules of a moveset (also run by the Moveset Tool before saving): weapons <c>any</c> or valid family names without
    /// repeats, at least one row, six-digit skill ids (and known ones when <c>knownSkills</c> is given), at least one context,
    /// <c>moving</c> only on ground-only rows, valid <c>after</c> ids, non-negative times, no two rows that can never be told
    /// apart (same input, conditions, follow-up and priority).
    /// </summary>
    public static class MovesetValidator
    {
        /// <summary>
        /// Appends every problem of <paramref name="m"/> to <paramref name="errors"/>. With <paramref name="knownSkills"/> /
        /// <paramref name="knownWeapons"/> (the tool passes the skill folder and the weapon family list) unknown ids are errors too.
        /// </summary>
        public static void Validate(MovesetData m, List<SkillError> errors, ISet<string> knownSkills = null, ISet<string> knownWeapons = null)
        {
            string subject = "moveset:" + m.Id;
            void Error(string code, string message) => errors.Add(new SkillError(subject, code, message));

            if (!MovesetXml.IsMovesetId(m.Id)) Error("IdFormat", $"id '{m.Id}' must be six digits from 100000 (the file name)");
            if (!m.AnyWeapon)
            {
                if (m.Weapons.Count == 0) Error("NoWeapons", "weapons must be 'any' or at least one weapon family");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (string w in m.Weapons)
                {
                    if (!SkillXmlReader.IsName(w) || w == "any") Error("IdFormat", $"weapon family '{w}' must be [a-z0-9._]+");
                    else if (!seen.Add(w)) Error("DuplicateWeapon", $"weapon family '{w}' is listed twice");
                    else if (knownWeapons != null && !knownWeapons.Contains(w)) Error("UnknownWeapon", $"weapon family '{w}' is not in the weapon family list");
                }
            }
            if (m.Rows.Count == 0) Error("EmptyMoveset", "a moveset needs at least one row");

            for (int i = 0; i < m.Rows.Count; i++)
            {
                MovesetRowData r = m.Rows[i];
                if (!SkillXmlReader.IsSkillId(r.Skill)) Error("IdFormat", $"row {i}: skill '{r.Skill}' must be a six-digit skill id");
                else if (knownSkills != null && !knownSkills.Contains(r.Skill)) Error("MissingSkill", $"row {i}: skill {r.Skill} does not exist");
                if ((r.Context & MovesetXml.AllContexts) == SkillContext.None) Error("NoContext", $"row {i}: pick at least one of ground, air, dash");
                if (r.Moving != MovingFilter.Any && r.Context != SkillContext.Ground)
                    Error("MovingOnlyGround", $"row {i}: still / moving only applies to ground-only rows");
                foreach (string a in r.After)
                    if (!SkillXmlReader.IsSkillId(a)) Error("IdFormat", $"row {i}: after entry '{a}' must be a six-digit skill id");
                if (r.LinkWindow < 0) Error("BadNumber", $"row {i}: link_window must be ≥ 0");
                if (r.Cooldown < 0) Error("BadNumber", $"row {i}: cooldown must be ≥ 0");
                for (int j = 0; j < i; j++)
                    if (SameTrigger(m.Rows[j], r))
                        Error("DuplicateRow", $"row {i}: same input, conditions, follow-up and priority as row {j}; it can never be chosen");
            }
        }

        private static bool SameTrigger(MovesetRowData a, MovesetRowData b)
        {
            if (a.Input != b.Input || a.Context != b.Context || a.Moving != b.Moving || a.AnyDir != b.AnyDir || a.Priority != b.Priority) return false;
            if (!a.AnyDir && a.Dir != b.Dir) return false;
            if (a.After.Count != b.After.Count) return false;
            var set = new HashSet<string>(a.After, StringComparer.Ordinal);
            return set.SetEquals(b.After);
        }
    }
}

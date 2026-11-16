using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Parses skill graphs and command tables (ADR-0002; skill files are event graphs since 2026-11-09). Never throws on bad
    /// data: every problem becomes a <see cref="SkillError"/>. <see cref="TryParseData"/> reads as much as it can (the Skill
    /// Tool opens broken files to fix them); <see cref="TryParse"/> also runs <see cref="SkillGraphValidator"/> and only then
    /// returns a <see cref="SkillDef"/> the game may use. Runs once at load; allocation is fine here, never per frame.
    /// </summary>
    /// <example>
    /// <code>
    /// var errors = new List&lt;SkillError&gt;();
    /// if (SkillXmlReader.TryParse(xmlText, "100123", out SkillDef skill, errors)) library.Add(skill);
    /// else foreach (SkillError e in errors) log(e.ToString());
    /// </code>
    /// </example>
    public static class SkillXmlReader
    {
        /// <summary>Schema of skill files: 2 = event graph (2026-11-09).</summary>
        public const int SkillSchemaVersion = 2;

        /// <summary>Schema of command-table files.</summary>
        public const int SchemaVersion = 1;

        /// <summary>Smallest and largest skill id (six digits; user decision 2026-11-06).</summary>
        public const int MinSkillId = 100000, MaxSkillId = 999999;

        /// <summary>Family tag meaning "every family".</summary>
        public const string AnyFamily = "any";

        /// <summary>Row link window when the row omits it: the Controller's <c>combo_reset</c> (0.30 s, GDD Tuning Knobs).</summary>
        public const double DefaultLinkWindow = 0.30;

        /// <summary>Every context (a row's <c>any</c>).</summary>
        public const SkillContext AllContexts = SkillContext.Ground | SkillContext.Air | SkillContext.Dash;

        private static readonly string[] SkillAttributes = { "schema_version", "owner" };
        private static readonly string[] NodeAttributes = { "id", "type", "x", "y" };
        private static readonly string[] FieldAttributes = { "n", "v", "ref" };
        private static readonly string[] LinkAttributes = { "from", "to", "when" };
        private static readonly string[] MemoAttributes = { "x", "y", "w", "h", "title" };
        private static readonly string[] TableAttributes = { "schema_version", "weapon_family" };
        private static readonly string[] MetaAttributes = { "graph_hash", "payload_hash" };
        private static readonly string[] RowAttributes = { "skill", "context", "moving", "dir", "after", "link_window", "priority" };

        // ------------------------------------------------------------------ skills

        /// <summary>
        /// Parses and validates one skill file. <paramref name="id"/> is the file name without extension (a 6-digit number).
        /// Returns false and appends to <paramref name="errors"/> when the file is rejected.
        /// </summary>
        public static bool TryParse(string xml, string id, out SkillDef skill, List<SkillError> errors)
        {
            skill = null;
            List<SkillError> local = errors ?? new List<SkillError>();
            int before = local.Count;
            if (!TryParseData(xml, id, out SkillGraphData data, local)) return false;
            SkillGraphValidator.Validate(data, local);
            if (local.Count > before) return false;
            skill = new SkillDef(id, data);
            return true;
        }

        /// <summary>
        /// Reads a skill file into an editable graph, collecting problems instead of stopping (unknown nodes and fields are
        /// reported and skipped). Returns false only when the document cannot be read as a skill at all.
        /// Does not run <see cref="SkillGraphValidator"/>.
        /// </summary>
        public static bool TryParseData(string xml, string id, out SkillGraphData graph, List<SkillError> errors)
        {
            graph = null;
            var ctx = new Ctx(errors, string.IsNullOrEmpty(id) ? "?" : id);
            try
            {
                using (XmlReader r = Open(xml))
                {
                    if (r == null || r.MoveToContent() != XmlNodeType.Element)
                    {
                        ctx.Error("XmlUnreadable", "empty document");
                        return false;
                    }
                    if (r.LocalName != "skill")
                    {
                        ctx.Error("UnknownNode", $"root <{r.LocalName}> is not <skill>");
                        return false;
                    }
                    graph = ReadSkill(r, id ?? "", ctx);
                }
            }
            catch (XmlException e)
            {
                ctx.Error("XmlUnreadable", e.Message);
                graph = null;
            }
            return graph != null;
        }

        private static SkillGraphData ReadSkill(XmlReader r, string id, Ctx ctx)
        {
            CheckAttributes(r, SkillAttributes, ctx);
            string text = r.GetAttribute("schema_version");
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int version) || version < 1)
                ctx.Error("SchemaMissing", $"schema_version '{text}' must be a positive integer");
            else if (version > SkillSchemaVersion) ctx.Error("SchemaTooNew", $"schema_version {version} is newer than {SkillSchemaVersion}");
            else if (version < SkillSchemaVersion) ctx.Error("SchemaTooOld", $"schema_version {version} (timeline format) is no longer read; rebuild the skill in the Skill Tool");
            if (!IsSkillId(id)) ctx.Error("IdFormat", $"file name '{id}' must be a 6-digit skill id {MinSkillId}–{MaxSkillId}");

            var g = new SkillGraphData { Id = id };
            string ownerText = r.GetAttribute("owner");
            if (ownerText == "creature") g.Owner = SkillOwner.Creature;
            else if (ownerText != null && ownerText != "player") ctx.Error("BadEnum", $"owner '{ownerText}' must be player or creature");

            if (r.IsEmptyElement) return g;
            int depth = r.Depth;
            r.Read();
            while (!(r.NodeType == XmlNodeType.EndElement && r.Depth == depth) && !r.EOF)
            {
                if (r.NodeType != XmlNodeType.Element)
                {
                    r.Read();
                    continue;
                }
                switch (r.LocalName)
                {
                    case "meta":
                        CheckAttributes(r, MetaAttributes, ctx);
                        g.PayloadHash = r.GetAttribute("payload_hash") ?? "";
                        r.Skip();
                        break;
                    case "node":
                        ReadNode(r, g, ctx);
                        break;
                    case "link":
                        ReadLink(r, g, ctx);
                        r.Skip();
                        break;
                    case "memo":
                        ReadMemo(r, g, ctx);
                        break;
                    default:
                        ctx.Error("UnknownNode", $"unexpected <{r.LocalName}> in <skill>");
                        r.Skip();
                        break;
                }
            }
            return g;
        }

        private static void ReadNode(XmlReader r, SkillGraphData g, Ctx ctx)
        {
            CheckAttributes(r, NodeAttributes, ctx);
            string idText = r.GetAttribute("id"), typeText = r.GetAttribute("type");
            bool ok = true;
            if (!int.TryParse(idText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int nodeId))
            {
                ctx.Error("BadNodeId", $"node id '{idText}' must be an integer");
                ok = false;
            }
            if (!SkillSchema.TryParseKind(typeText, out SkillNodeKind kind))
            {
                ctx.Error("UnknownNode", $"node type '{typeText}' is not a skill node");
                ok = false;
            }
            var node = new SkillNodeData { Id = nodeId, Kind = kind, X = (float)Number(r, "x", ctx), Y = (float)Number(r, "y", ctx) };

            if (!r.IsEmptyElement)
            {
                int depth = r.Depth;
                r.Read();
                while (!(r.NodeType == XmlNodeType.EndElement && r.Depth == depth) && !r.EOF)
                {
                    if (r.NodeType != XmlNodeType.Element)
                    {
                        r.Read();
                        continue;
                    }
                    if (r.LocalName != "f") ctx.Error("UnknownNode", $"unexpected <{r.LocalName}> in <node>");
                    else if (ok) ReadField(r, node, ctx);
                    r.Skip();
                }
            }
            r.Read();
            if (ok) g.Nodes.Add(node);
        }

        private static void ReadField(XmlReader r, SkillNodeData node, Ctx ctx)
        {
            CheckAttributes(r, FieldAttributes, ctx);
            string name = r.GetAttribute("n"), value = r.GetAttribute("v"), reference = r.GetAttribute("ref");
            SkillFieldDef f = node.Def.Field(name);
            if (f == null)
            {
                ctx.Error("UnknownField", $"{node.Kind} {node.Id} has no field '{name}'");
                return;
            }
            if (value != null && reference != null) ctx.Error("BadValue", $"{node.Kind} {node.Id}.{name} has both v and ref");
            if (reference != null) node.Refs[name] = reference.Trim();
            else if (value != null)
                node.Values[name] = SkillSchema.TryNormalize(f.Type, f.EnumType, value, out string canonical) ? canonical : value;   // bad text kept; the validator reports BadValue
            else ctx.Error("BadValue", $"{node.Kind} {node.Id}.{name} needs v or ref");
        }

        private static void ReadLink(XmlReader r, SkillGraphData g, Ctx ctx)
        {
            CheckAttributes(r, LinkAttributes, ctx);
            string fromText = r.GetAttribute("from"), toText = r.GetAttribute("to"), whenText = r.GetAttribute("when") ?? "start";
            if (!int.TryParse(fromText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int from) ||
                !int.TryParse(toText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int to))
            {
                ctx.Error("BadLink", $"link from='{fromText}' to='{toText}' needs integer node ids");
                return;
            }
            if (!SkillSchema.TryParseTrigger(whenText, out SkillTrigger when))
            {
                ctx.Error("BadTrigger", $"link {from} → {to}: when='{whenText}' must be start, end, hit, found or not_found");
                return;
            }
            g.Links.Add(new SkillLink { From = from, To = to, When = when });
        }

        private static void ReadMemo(XmlReader r, SkillGraphData g, Ctx ctx)
        {
            CheckAttributes(r, MemoAttributes, ctx);
            var m = new SkillMemo
            {
                X = (float)Number(r, "x", ctx), Y = (float)Number(r, "y", ctx),
                W = (float)Number(r, "w", ctx, 300), H = (float)Number(r, "h", ctx, 120),
                Title = r.GetAttribute("title") ?? ""
            };
            if (r.IsEmptyElement)
            {
                m.Text = "";
                r.Read();
            }
            else
            {
                m.Text = r.ReadElementContentAsString();
            }
            g.Memos.Add(m);
        }

        private static double Number(XmlReader r, string name, Ctx ctx, double fallback = 0.0)
        {
            string text = r.GetAttribute(name);
            if (text == null) return fallback;
            if (TryNumber(text, out double v)) return v;
            ctx.Error("BadNumber", $"{name} '{text}' must be a number");
            return fallback;
        }

        // ------------------------------------------------------------------ command tables

        /// <summary>Parses one command table file. Returns false and appends to <paramref name="errors"/> when it is rejected.</summary>
        public static bool TryParseTable(string xml, out CommandTable table, List<SkillError> errors)
        {
            table = null;
            var ctx = new Ctx(errors, "?");
            try
            {
                using (XmlReader r = Open(xml))
                {
                    if (r == null || r.MoveToContent() != XmlNodeType.Element)
                    {
                        ctx.Error("XmlUnreadable", "empty document");
                        return false;
                    }
                    if (r.LocalName != "command_table")
                    {
                        ctx.Error("UnknownNode", $"root <{r.LocalName}> is not <command_table>");
                        return false;
                    }
                    table = ReadTable(r, ctx);
                }
            }
            catch (XmlException e)
            {
                ctx.Error("XmlUnreadable", e.Message);
                table = null;
            }
            if (ctx.Failed) table = null;
            return table != null;
        }

        private static CommandTable ReadTable(XmlReader r, Ctx ctx)
        {
            string family = r.GetAttribute("weapon_family") ?? "";
            ctx.Subject = family.Length > 0 ? "table:" + family : "table:?";
            CheckAttributes(r, TableAttributes, ctx);
            string text = r.GetAttribute("schema_version");
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int version) || version < 1)
                ctx.Error("SchemaMissing", $"schema_version '{text}' must be a positive integer");
            else if (version > SchemaVersion)
                ctx.Error("SchemaTooNew", $"schema_version {version} is newer than {SchemaVersion}");
            if (!IsName(family) || family == AnyFamily) ctx.Error("IdFormat", $"weapon_family '{family}' must be [a-z0-9._]+ and not 'any'");

            var rows = new List<CommandRow>();
            if (!r.IsEmptyElement)
            {
                int depth = r.Depth;
                r.Read();
                while (!(r.NodeType == XmlNodeType.EndElement && r.Depth == depth) && !r.EOF)
                {
                    if (r.NodeType != XmlNodeType.Element)
                    {
                        r.Read();
                        continue;
                    }
                    if (r.LocalName == "meta") CheckAttributes(r, MetaAttributes, ctx);
                    else if (r.LocalName == "row") ReadRow(r, rows, ctx);
                    else ctx.Error("UnknownNode", $"unexpected <{r.LocalName}> in <command_table>");
                    r.Skip();
                }
            }
            return ctx.Failed ? null : new CommandTable(family, rows.ToArray());
        }

        private static void ReadRow(XmlReader r, List<CommandRow> rows, Ctx ctx)
        {
            int order = rows.Count;
            CheckAttributes(r, RowAttributes, ctx);
            string skill = r.GetAttribute("skill") ?? "";
            if (!IsSkillId(skill)) ctx.Error("IdFormat", $"row {order}: skill '{skill}' must be a 6-digit skill id");

            SkillContext context = AllContexts;
            switch (r.GetAttribute("context") ?? "any")
            {
                case "any": break;
                case "ground": context = SkillContext.Ground; break;
                case "air": context = SkillContext.Air; break;
                case "dash": context = SkillContext.Dash; break;
                default: ctx.Error("BadEnum", $"row {order}: context '{r.GetAttribute("context")}' must be ground, air, dash or any"); break;
            }

            MovingFilter moving = MovingFilter.Any;
            switch (r.GetAttribute("moving") ?? "any")
            {
                case "any": break;
                case "still": moving = MovingFilter.Still; break;
                case "moving": moving = MovingFilter.Moving; break;
                default: ctx.Error("BadEnum", $"row {order}: moving '{r.GetAttribute("moving")}' must be still, moving or any"); break;
            }
            if (moving != MovingFilter.Any && context != SkillContext.Ground)
                ctx.Error("MovingOnlyGround", $"row {order}: moving applies to ground rows only (GDD Rule 15)");

            bool anyDir = false;
            SkillDir dir = SkillDir.Neutral;
            switch (r.GetAttribute("dir") ?? "any")
            {
                case "any": anyDir = true; break;
                case "neutral": dir = SkillDir.Neutral; break;
                case "up": dir = SkillDir.Up; break;
                case "down": dir = SkillDir.Down; break;
                case "forward": dir = SkillDir.Forward; break;
                case "back": dir = SkillDir.Back; break;
                default: ctx.Error("BadEnum", $"row {order}: dir '{r.GetAttribute("dir")}' must be neutral, up, down, forward, back or any"); break;
            }

            var ids = new List<string>();
            foreach (string entry in SplitList(r.GetAttribute("after")))
            {
                if (!IsSkillId(entry)) ctx.Error("IdFormat", $"row {order}: after entry '{entry}' must be a 6-digit skill id");
                ids.Add(entry);
            }

            double link = DefaultLinkWindow;
            string linkText = r.GetAttribute("link_window");
            if (linkText != null && (!TryNumber(linkText, out link) || link < 0.0))
                ctx.Error("BadNumber", $"row {order}: link_window '{linkText}' must be a number ≥ 0");

            int priority = 0;
            string priorityText = r.GetAttribute("priority");
            if (priorityText != null && !int.TryParse(priorityText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out priority))
                ctx.Error("BadNumber", $"row {order}: priority '{priorityText}' must be an integer");

            rows.Add(new CommandRow(skill, context, moving, anyDir, dir, ids.ToArray(), link, priority, order));
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Root element name of <paramref name="xml"/> (<c>skill</c>, <c>command_table</c>), or null when unreadable.</summary>
        public static string RootName(string xml)
        {
            try
            {
                using (XmlReader r = Open(xml))
                    return r != null && r.MoveToContent() == XmlNodeType.Element ? r.LocalName : null;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>True for a skill id: exactly six digits, 100000–999999 (the XML file name).</summary>
        public static bool IsSkillId(string s)
        {
            if (s == null || s.Length != 6 || s[0] == '0') return false;
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>Skill id of a file name or relative path: <c>"spear/100123.xml"</c> → <c>"100123"</c> (not validated).</summary>
        public static string IdFromFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string file = name.Substring(Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\')) + 1);
            return file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ? file.Substring(0, file.Length - 4) : file;
        }

        /// <summary>True for a non-empty <c>[a-z0-9._]+</c> name (weapon families).</summary>
        public static bool IsName(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '.' || c == '_')) return false;
            return true;
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

        private static void CheckAttributes(XmlReader r, string[] allowed, Ctx ctx)
        {
            if (!r.MoveToFirstAttribute()) return;
            do
            {
                if (r.Prefix == "xmlns" || r.LocalName == "xmlns") continue;
                if (Array.IndexOf(allowed, r.LocalName) < 0)
                    ctx.Error("UnknownAttribute", $"attribute '{r.LocalName}' is not allowed here");
            } while (r.MoveToNextAttribute());
            r.MoveToElement();
        }

        private static bool TryNumber(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);

        private static string[] SplitList(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            string[] parts = text.Split(',');
            var list = new List<string>(parts.Length);
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t.Length > 0) list.Add(t);
            }
            return list.ToArray();
        }

        private sealed class Ctx
        {
            private readonly List<SkillError> _errors;
            public string Subject;
            public bool Failed;

            public Ctx(List<SkillError> errors, string subject)
            {
                _errors = errors;
                Subject = subject;
            }

            public void Error(string code, string message)
            {
                Failed = true;
                _errors?.Add(new SkillError(Subject, code, message));
            }
        }
    }
}

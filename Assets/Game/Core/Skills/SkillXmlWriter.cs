using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Writes skill graphs and command tables deterministically (ADR-0002, ADR-0003): nodes by id, fields in schema order
    /// (only non-default literals and wires), links sorted by (from, to), memos in order, invariant numbers, LF line endings,
    /// and a <c>payload_hash</c> stamped last. Same graph → same string, byte for byte. Encode as UTF-8 without BOM.
    /// Does not validate: the Skill Tool also uses it for undo snapshots of unfinished graphs.
    /// </summary>
    /// <example>
    /// <code>
    /// string xml = SkillXmlWriter.Write(graph);
    /// File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(xml));
    /// </code>
    /// </example>
    public static class SkillXmlWriter
    {
        private const string Header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";

        /// <summary>Writes one skill graph.</summary>
        public static string Write(SkillGraphData g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var sb = new StringBuilder(4096);
            sb.Append(Header);
            sb.Append("<skill schema_version=\"").Append(SkillXmlReader.SkillSchemaVersion).Append('"');
            Attr(sb, "owner", g.Owner == SkillOwner.Creature ? "creature" : "player");
            sb.Append(">\n");
            sb.Append("  <meta");
            Attr(sb, "payload_hash", "");
            sb.Append("/>\n");

            foreach (SkillNodeData n in g.Nodes.OrderBy(n => n.Id))
            {
                sb.Append("  <node");
                Attr(sb, "id", n.Id.ToString(CultureInfo.InvariantCulture));
                Attr(sb, "type", n.Kind.ToString());
                Attr(sb, "x", Pos(n.X));
                Attr(sb, "y", Pos(n.Y));
                var fields = new List<string>();
                foreach (SkillFieldDef f in n.Def.Fields)
                {
                    if (n.Refs.TryGetValue(f.Name, out string r) && !string.IsNullOrEmpty(r))
                        fields.Add($"    <f n=\"{Esc(f.Name)}\" ref=\"{Esc(r)}\"/>\n");
                    else if (n.Values.TryGetValue(f.Name, out string v) && v != f.Default)
                        fields.Add($"    <f n=\"{Esc(f.Name)}\" v=\"{Esc(v)}\"/>\n");
                }
                if (fields.Count == 0)
                {
                    sb.Append("/>\n");
                    continue;
                }
                sb.Append(">\n");
                foreach (string line in fields) sb.Append(line);
                sb.Append("  </node>\n");
            }

            foreach (SkillLink l in g.Links.OrderBy(l => l.From).ThenBy(l => l.To))
            {
                sb.Append("  <link");
                Attr(sb, "from", l.From.ToString(CultureInfo.InvariantCulture));
                Attr(sb, "to", l.To.ToString(CultureInfo.InvariantCulture));
                Attr(sb, "when", SkillSchema.NameOf(l.When));
                sb.Append("/>\n");
            }

            foreach (SkillMemo m in g.Memos)
            {
                sb.Append("  <memo");
                Attr(sb, "x", Pos(m.X));
                Attr(sb, "y", Pos(m.Y));
                Attr(sb, "w", Pos(m.W));
                Attr(sb, "h", Pos(m.H));
                if (!string.IsNullOrEmpty(m.Title)) Attr(sb, "title", m.Title);
                sb.Append('>').Append(Esc(m.Text ?? "")).Append("</memo>\n");
            }
            sb.Append("</skill>\n");
            return Stamp(sb.ToString());
        }

        /// <summary>Writes one command table (combo system data).</summary>
        public static string Write(CommandTableDraft t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            var sb = new StringBuilder(1024);
            sb.Append(Header);
            sb.Append("<command_table schema_version=\"").Append(SkillXmlReader.SchemaVersion).Append('"');
            Attr(sb, "weapon_family", t.WeaponFamily);
            sb.Append(">\n");
            sb.Append("  <meta");
            if (!string.IsNullOrEmpty(t.GraphHash)) Attr(sb, "graph_hash", t.GraphHash);
            Attr(sb, "payload_hash", "");
            sb.Append("/>\n");
            foreach (CommandRowDraft r in t.Rows)
            {
                sb.Append("  <row");
                Attr(sb, "skill", r.SkillId);
                Attr(sb, "context", r.Context == SkillXmlReader.AllContexts ? "any" : Contexts(r.Context));
                Attr(sb, "moving", r.Moving == MovingFilter.Still ? "still" : r.Moving == MovingFilter.Moving ? "moving" : "any");
                Attr(sb, "dir", r.AnyDir ? "any" : Dir(r.Dir));
                if (r.After.Count > 0) Attr(sb, "after", string.Join(",", r.After));
                Attr(sb, "link_window", Num(r.LinkWindow));
                Attr(sb, "priority", r.Priority.ToString(CultureInfo.InvariantCulture));
                sb.Append("/>\n");
            }
            sb.Append("</command_table>\n");
            return Stamp(sb.ToString());
        }

        /// <summary>Formats a number the way skill files do: invariant, rounded to 1e-6, no trailing zeros.</summary>
        public static string Num(double value) => SkillSchema.Num(value);

        // Editor positions are rounded to whole pixels so moving a node by a sub-pixel does not churn the diff.
        private static string Pos(float v) => Math.Round(v, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

        private static string Stamp(string xml)
        {
            string hash = SkillHash.PayloadHash(xml);
            const string empty = "payload_hash=\"\"";
            int at = xml.IndexOf(empty, StringComparison.Ordinal);
            return xml.Substring(0, at) + "payload_hash=\"" + hash + "\"" + xml.Substring(at + empty.Length);
        }

        private static string Contexts(SkillContext c)
        {
            var parts = new List<string>(3);
            if ((c & SkillContext.Ground) != 0) parts.Add("ground");
            if ((c & SkillContext.Air) != 0) parts.Add("air");
            if ((c & SkillContext.Dash) != 0) parts.Add("dash");
            return string.Join(",", parts);
        }

        private static string Dir(SkillDir d)
        {
            switch (d)
            {
                case SkillDir.Up: return "up";
                case SkillDir.Down: return "down";
                case SkillDir.Forward: return "forward";
                case SkillDir.Back: return "back";
                default: return "neutral";
            }
        }

        private static void Attr(StringBuilder sb, string name, string value) => sb.Append(' ').Append(name).Append("=\"").Append(Esc(value)).Append('"');

        private static string Esc(string value)
        {
            var sb = new StringBuilder((value ?? "").Length);
            foreach (char c in value ?? "")
            {
                switch (c)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    case '\n': sb.Append("&#10;"); break;
                    case '\r': sb.Append("&#13;"); break;
                    case '\t': sb.Append("&#9;"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}

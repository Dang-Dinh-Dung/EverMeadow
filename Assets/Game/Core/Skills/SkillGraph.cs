using System;
using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>Who a skill is made for (GDD Skill &amp; Moveset Rule 4a). Declared on the skill file.</summary>
    public enum SkillOwner
    {
        /// <summary>The player.</summary>
        Player,
        /// <summary>A creature (Enemy AI).</summary>
        Creature
    }

    /// <summary>
    /// One node of a skill graph: its kind, id ("11" in "11:Animation"), editor position, field values in canonical text
    /// (absent = schema default) and field wires (<c>field → "nodeId.Output"</c>).
    /// </summary>
    public sealed class SkillNodeData
    {
        /// <summary>Node id, unique in the skill; Start is 0.</summary>
        public int Id;
        /// <summary>Kind.</summary>
        public SkillNodeKind Kind;
        /// <summary>Editor position (layout only; ignored at run time).</summary>
        public float X, Y;
        /// <summary>Literal field values in canonical text form.</summary>
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>Wired fields: field name → <c>"nodeId.OutputName"</c>.</summary>
        public readonly Dictionary<string, string> Refs = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Definition of this node's kind.</summary>
        public SkillNodeDef Def => SkillSchema.Get(Kind);

        /// <summary>Value of a field (its literal, or the schema default).</summary>
        public string Get(string field) =>
            Values.TryGetValue(field, out string v) ? v : Def.Field(field)?.Default ?? "";

        /// <summary>Deep copy.</summary>
        public SkillNodeData Clone()
        {
            var c = new SkillNodeData { Id = Id, Kind = Kind, X = X, Y = Y };
            foreach (KeyValuePair<string, string> kv in Values) c.Values[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, string> kv in Refs) c.Refs[kv.Key] = kv.Value;
            return c;
        }
    }

    /// <summary>A link parent → child with the condition that starts the child (written on the edge in the tool).</summary>
    public sealed class SkillLink
    {
        /// <summary>Parent node id.</summary>
        public int From;
        /// <summary>Child node id.</summary>
        public int To;
        /// <summary>When the child starts.</summary>
        public SkillTrigger When = SkillTrigger.Start;
    }

    /// <summary>A yellow memo on the graph (layout only).</summary>
    public sealed class SkillMemo
    {
        /// <summary>Position and size.</summary>
        public float X, Y, W = 300, H = 120;
        /// <summary>Title.</summary>
        public string Title = "";
        /// <summary>Text.</summary>
        public string Text = "";
    }

    /// <summary>
    /// A skill as an event graph (GDD Rule 5, user decision 2026-11-09): Start (id 0) and nodes linked parent → child; each
    /// link says when the child starts (parent starts / ends / each hit / search found / not found), then the child waits
    /// its own <c>ActionDelay</c>. A node has at most one parent; a parent may have many children. Fields hold literals or
    /// wires to outputs of Start or of an ancestor. Mutable: the Skill Tool edits it, the writer saves it, the reader fills it.
    /// </summary>
    /// <example>
    /// <code>
    /// var g = SkillGraphData.CreateDefault("100001");          // Start → Animation, Start → Hit
    /// string xml = SkillXmlWriter.Write(g);
    /// </code>
    /// </example>
    public sealed class SkillGraphData
    {
        /// <summary>Skill id = file name (six digits).</summary>
        public string Id = "";
        /// <summary>Player or creature skill.</summary>
        public SkillOwner Owner = SkillOwner.Player;
        /// <summary>Nodes (Start included).</summary>
        public readonly List<SkillNodeData> Nodes = new List<SkillNodeData>();
        /// <summary>Links parent → child.</summary>
        public readonly List<SkillLink> Links = new List<SkillLink>();
        /// <summary>Memos.</summary>
        public readonly List<SkillMemo> Memos = new List<SkillMemo>();
        /// <summary>Payload hash read from the file (empty for a new graph).</summary>
        public string PayloadHash = "";

        /// <summary>Node by id, or null.</summary>
        public SkillNodeData Node(int id)
        {
            foreach (SkillNodeData n in Nodes)
                if (n.Id == id) return n;
            return null;
        }

        /// <summary>The link into <paramref name="id"/>, or null (Start and loose nodes have none).</summary>
        public SkillLink ParentLink(int id)
        {
            foreach (SkillLink l in Links)
                if (l.To == id) return l;
            return null;
        }

        /// <summary>True when <paramref name="ancestor"/> is reached by walking parents up from <paramref name="id"/>.</summary>
        public bool IsAncestor(int ancestor, int id)
        {
            var seen = new HashSet<int>();
            SkillLink link = ParentLink(id);
            while (link != null && seen.Add(link.From))
            {
                if (link.From == ancestor) return true;
                link = ParentLink(link.From);
            }
            return false;
        }

        /// <summary>Next free node id (max + 1).</summary>
        public int NextNodeId()
        {
            int max = 0;
            foreach (SkillNodeData n in Nodes) max = Math.Max(max, n.Id);
            return max + 1;
        }

        /// <summary>A new skill: Start → Animation ("attack") and Start → Hit after 0.04 s.</summary>
        public static SkillGraphData CreateDefault(string id)
        {
            var g = new SkillGraphData { Id = id };
            g.Nodes.Add(new SkillNodeData { Id = 0, Kind = SkillNodeKind.Start, X = 0, Y = 0 });
            var anim = new SkillNodeData { Id = 1, Kind = SkillNodeKind.Animation, X = 320, Y = 0 };
            anim.Values["AnimationType"] = "attack";
            var hit = new SkillNodeData { Id = 2, Kind = SkillNodeKind.Hit, X = 320, Y = 260 };
            hit.Values["ActionDelay"] = "0.04";
            g.Nodes.Add(anim);
            g.Nodes.Add(hit);
            g.Links.Add(new SkillLink { From = 0, To = 1 });
            g.Links.Add(new SkillLink { From = 0, To = 2 });
            return g;
        }

        /// <summary>Splits <c>"6.LastHitCreatureGID"</c> into node id and output name.</summary>
        public static bool TrySplitRef(string reference, out int nodeId, out string output)
        {
            nodeId = -1;
            output = null;
            if (string.IsNullOrEmpty(reference)) return false;
            int dot = reference.IndexOf('.');
            if (dot <= 0 || dot == reference.Length - 1) return false;
            output = reference.Substring(dot + 1);
            return int.TryParse(reference.Substring(0, dot), System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out nodeId);
        }
    }
}

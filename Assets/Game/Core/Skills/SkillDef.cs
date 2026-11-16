using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// One validated skill file (ADR-0002, graph form since 2026-11-09): the event graph a runtime interpreter executes.
    /// Built by <see cref="SkillXmlReader.TryParse"/> only after <see cref="SkillGraphValidator"/> passed. Callers must not
    /// mutate <see cref="Graph"/>.
    /// </summary>
    public sealed class SkillDef
    {
        private readonly Dictionary<int, SkillNodeData> _byId = new Dictionary<int, SkillNodeData>();
        private readonly Dictionary<int, List<SkillLink>> _children = new Dictionary<int, List<SkillLink>>();
        private static readonly List<SkillLink> NoLinks = new List<SkillLink>();

        /// <summary>Skill id: the XML file name, six digits 100000–999999 (built-in fallbacks use <c>fallback.*</c>).</summary>
        public string Id { get; }
        /// <summary>Player or creature skill.</summary>
        public SkillOwner Owner => Graph.Owner;
        /// <summary>The graph (read-only by convention).</summary>
        public SkillGraphData Graph { get; }
        /// <summary>Payload hash from the file, or empty.</summary>
        public string PayloadHash => Graph.PayloadHash;

        internal SkillDef(string id, SkillGraphData graph)
        {
            Id = id;
            Graph = graph;
            foreach (SkillNodeData n in graph.Nodes) _byId[n.Id] = n;
            foreach (SkillLink l in graph.Links)
            {
                if (!_children.TryGetValue(l.From, out List<SkillLink> list)) _children[l.From] = list = new List<SkillLink>();
                list.Add(l);
            }
        }

        /// <summary>The Start node.</summary>
        public SkillNodeData Start => _byId.TryGetValue(0, out SkillNodeData s) ? s : null;

        /// <summary>Node by id, or null.</summary>
        public SkillNodeData Node(int id) => _byId.TryGetValue(id, out SkillNodeData n) ? n : null;

        /// <summary>Links leaving node <paramref name="id"/>.</summary>
        public List<SkillLink> Children(int id) => _children.TryGetValue(id, out List<SkillLink> l) ? l : NoLinks;

        /// <summary>True when any node is of <paramref name="kind"/>.</summary>
        public bool Contains(SkillNodeKind kind)
        {
            foreach (SkillNodeData n in Graph.Nodes)
                if (n.Kind == kind) return true;
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => Id;
    }
}

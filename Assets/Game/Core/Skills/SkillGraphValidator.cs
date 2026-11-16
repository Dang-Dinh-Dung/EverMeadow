using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Semantic checks of a skill graph (ADR-0002 Decision 5, graph form). The reader keeps parsing after structural problems
    /// so the Skill Tool can still open a broken file; this validator decides whether the game may use it.
    /// Rules: one Start with id 0; unique node ids; links between existing nodes, never into Start, at most one parent per
    /// node, no cycles, triggers allowed by the parent kind; every value parses as its field type; every wire points at an
    /// output of Start or of an ancestor, with a compatible type. Loose nodes (not reachable from Start) are allowed and
    /// ignored at run time.
    /// </summary>
    /// <example>
    /// <code>
    /// var errors = new List&lt;SkillError&gt;();
    /// bool usable = SkillGraphValidator.Validate(graph, errors);
    /// </code>
    /// </example>
    public static class SkillGraphValidator
    {
        /// <summary>Validates <paramref name="g"/>; returns false when any error was added.</summary>
        public static bool Validate(SkillGraphData g, List<SkillError> errors)
        {
            int before = errors.Count;
            string s = string.IsNullOrEmpty(g.Id) ? "?" : g.Id;
            void Err(string code, string message) => errors.Add(new SkillError(s, code, message));

            // nodes
            var ids = new HashSet<int>();
            int starts = 0;
            foreach (SkillNodeData n in g.Nodes)
            {
                if (n.Id < 0) Err("BadNodeId", $"node id {n.Id} must be ≥ 0");
                if (!ids.Add(n.Id)) Err("DuplicateNodeId", $"two nodes have id {n.Id}");
                if (n.Kind == SkillNodeKind.Start)
                {
                    starts++;
                    if (n.Id != 0) Err("StartNotZero", $"Start must have id 0 (has {n.Id})");
                }
                else if (n.Id == 0) Err("StartNotZero", $"id 0 is reserved for Start ({n.Kind} uses it)");
            }
            if (starts == 0) Err("MissingStart", "the skill needs a Start node (id 0)");
            if (starts > 1) Err("DuplicateStart", "only one Start node is allowed");

            // links
            var parents = new Dictionary<int, int>();
            foreach (SkillLink l in g.Links)
            {
                SkillNodeData from = g.Node(l.From), to = g.Node(l.To);
                if (from == null || to == null)
                {
                    Err("BadLink", $"link {l.From} → {l.To} uses a missing node");
                    continue;
                }
                if (l.From == l.To) Err("BadLink", $"node {l.From} links to itself");
                if (to.Kind == SkillNodeKind.Start) Err("LinkIntoStart", $"nothing may link into Start (from {l.From})");
                if (parents.ContainsKey(l.To)) Err("MultipleParents", $"node {l.To} has more than one parent ({parents[l.To]} and {l.From})");
                else parents[l.To] = l.From;
                if (from.Kind == SkillNodeKind.End)
                    Err("EndHasChildren", $"End {l.From} → {l.To}: End finishes the skill, nothing can follow it");
                else if (!from.Def.Allows(l.When))
                    Err("BadTrigger", $"{from.Kind} {l.From} → {l.To}: '{SkillSchema.NameOf(l.When)}' is not a condition a {from.Kind} offers");
            }
            foreach (int id in parents.Keys)
            {
                var seen = new HashSet<int> { id };
                int cur = id;
                while (parents.TryGetValue(cur, out int p))
                {
                    if (!seen.Add(p))
                    {
                        Err("Cycle", $"node {id} is inside a loop of links");
                        break;
                    }
                    cur = p;
                }
            }

            // fields and wires
            foreach (SkillNodeData n in g.Nodes)
            {
                SkillNodeDef def = n.Def;
                foreach (KeyValuePair<string, string> kv in n.Values)
                {
                    SkillFieldDef f = def.Field(kv.Key);
                    if (f == null) Err("UnknownField", $"{n.Kind} {n.Id} has no field '{kv.Key}'");
                    else if (!SkillSchema.TryNormalize(f.Type, f.EnumType, kv.Value, out _))
                        Err("BadValue", $"{n.Kind} {n.Id}.{kv.Key} = '{kv.Value}' is not a valid {f.Type}");
                }
                foreach (KeyValuePair<string, string> kv in n.Refs)
                {
                    SkillFieldDef f = def.Field(kv.Key);
                    string at = $"{n.Kind} {n.Id}.{kv.Key}";
                    if (f == null)
                    {
                        Err("UnknownField", $"{n.Kind} {n.Id} has no field '{kv.Key}'");
                        continue;
                    }
                    if (!SkillGraphData.TrySplitRef(kv.Value, out int srcId, out string output))
                    {
                        Err("BadRef", $"{at}: '{kv.Value}' must be nodeId.Output");
                        continue;
                    }
                    SkillNodeData src = g.Node(srcId);
                    SkillOutputDef o = src?.Def.Output(output);
                    if (o == null)
                    {
                        Err("BadRef", $"{at}: '{kv.Value}' is not an output of an existing node");
                        continue;
                    }
                    if (!SkillSchema.CanAssign(o.Type, o.EnumType, f.Type, f.EnumType))
                        Err("BadRefType", $"{at}: {o.Type} output '{kv.Value}' cannot feed a {f.Type} field");
                    if (src.Kind != SkillNodeKind.Start && !g.IsAncestor(srcId, n.Id))
                        Err("RefNotAncestor", $"{at}: '{kv.Value}' comes from a node that does not run before this one (wire from Start or an ancestor)");
                }
            }
            return errors.Count == before;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Non-blocking advice on a skill graph (the Skill Tool shows it after Open / Save; saving is not refused). Today: an End
    /// that is reached at a known time while an Animation node may still be playing cuts that animation back to idle — the
    /// usual cause of "the attack animation does not show" (End chained after an instant Hit, End with no delay…).
    /// Start / end times come from the node fields like <see cref="SkillRun"/>; an Animation's length depends on the rig, so
    /// it is unknown here, and an End placed after an Animation's end (trigger <c>end</c>) is never flagged.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (string w in SkillLint.Warnings(graph)) status.Append("⚠ " + w);
    /// </code>
    /// </example>
    public static class SkillLint
    {
        /// <summary>Advice lines for <paramref name="g"/> (empty when nothing to say).</summary>
        public static List<string> Warnings(SkillGraphData g)
        {
            var result = new List<string>();
            if (g == null) return result;
            var start = new Dictionary<int, double>();
            var end = new Dictionary<int, double>();
            var children = new Dictionary<int, List<SkillLink>>();
            foreach (SkillLink l in g.Links)
            {
                if (!children.TryGetValue(l.From, out List<SkillLink> list)) children[l.From] = list = new List<SkillLink>();
                list.Add(l);
            }
            SkillNodeData root = g.Node(0);
            if (root == null) return result;
            Walk(g, root, 0.0, start, end, children, 0);

            foreach (SkillNodeData n in g.Nodes)
            {
                if (n.Kind != SkillNodeKind.Hit && n.Kind != SkillNodeKind.Search) continue;
                string[] p = (n.Get("SubValue") ?? "").Split(',');
                double V(int i) => i < p.Length && double.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
                bool box = n.Get("AreaType") == nameof(SkillAreaType.Box);
                bool empty = box ? V(1) <= 0 || (V(2) <= 0 && V(0) <= 0) : V(0) <= 0 || V(1) <= 0;
                if (empty || SkillRun.Number(n, "AreaScale") <= 0)
                    result.Add($"{n.Kind} {n.Id}: vùng có kích thước 0 (SubValue {n.Get("SubValue")}, " +
                               (box ? "Box cần Dài > 0 và Rộng hoặc Cao > 0" : "Arc cần Góc > 0 và Bán kính (số thứ hai) > 0") + ") → không trúng gì, không vẽ được.");
            }

            foreach (SkillNodeData e in g.Nodes)
            {
                if (e.Kind != SkillNodeKind.End || !start.TryGetValue(e.Id, out double te) || double.IsNaN(te)) continue;
                foreach (SkillNodeData a in g.Nodes)
                {
                    if (a.Kind != SkillNodeKind.Animation || !start.TryGetValue(a.Id, out double ta) || double.IsNaN(ta) || ta > te) continue;
                    double ea = end.TryGetValue(a.Id, out double x) ? x : double.NaN;
                    if (!double.IsNaN(ea) && ea <= te) continue;
                    result.Add($"End {e.Id} kết thúc kỹ năng lúc {Num(te)} s trong khi Animation {a.Id} ({a.Get("AnimationType")}, bắt đầu {Num(ta)} s) " +
                               "có thể chưa phát xong → animation bị cắt về idle. Nối End từ node Animation với điều kiện \"Khi node cha kết thúc\", " +
                               "hoặc tăng ActionDelay của End.");
                }
            }
            return result;
        }

        // Start / end time of every node reachable from Start; NaN = unknown (after an Animation's end, or a hit / search branch).
        private static void Walk(SkillGraphData g, SkillNodeData n, double t, Dictionary<int, double> start, Dictionary<int, double> end,
            Dictionary<int, List<SkillLink>> children, int depth)
        {
            if (depth > 256 || start.ContainsKey(n.Id)) return;
            start[n.Id] = t;
            double e;
            if (n.Kind == SkillNodeKind.Animation) e = n.Get("IsLoop") == "true" ? double.PositiveInfinity : double.NaN;
            else if (n.Kind == SkillNodeKind.Start || n.Kind == SkillNodeKind.End) e = t;
            else e = t + SkillRun.SuggestedSeconds(n);
            end[n.Id] = e;
            if (!children.TryGetValue(n.Id, out List<SkillLink> links)) return;
            foreach (SkillLink l in links)
            {
                SkillNodeData c = g.Node(l.To);
                if (c == null) continue;
                double delay = SkillTime.TickOf(SkillRun.Number(c, "ActionDelay")) / (double)Evermeadow.Foundation.Time.Ticks.PerSecond;
                double at = l.When == SkillTrigger.Start ? t + delay : l.When == SkillTrigger.End ? e + delay : double.NaN;
                Walk(g, c, at, start, end, children, depth + 1);
            }
        }

        private static string Num(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    }
}

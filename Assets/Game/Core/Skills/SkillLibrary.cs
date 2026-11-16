using System;
using System.Collections.Generic;

namespace Evermeadow.Core.Skills
{
    /// <summary>State the Controller passes when it consumes an Attack press (GDD Skill &amp; Moveset Rule 13–14).</summary>
    public readonly struct SelectionQuery
    {
        /// <summary>Context at consumption (exactly one flag; <c>LandRecovery</c> counts as ground).</summary>
        public readonly SkillContext Context;
        /// <summary>Horizontal input in the press record (meaningful on the ground only).</summary>
        public readonly bool Moving;
        /// <summary>Direction in the press record (Plunge forces <see cref="SkillDir.Down"/>).</summary>
        public readonly SkillDir Dir;
        /// <summary><c>chain_ref</c>: the normal skill that last ended or was cancelled, or null.</summary>
        public readonly SkillDef ChainRef;
        /// <summary>Game-clock tick at which <see cref="ChainRef"/> ended or was cancelled.</summary>
        public readonly long ChainEndTicks;
        /// <summary>Game-clock tick of consumption.</summary>
        public readonly long NowTicks;

        /// <summary>Creates a query.</summary>
        public SelectionQuery(SkillContext context, bool moving, SkillDir dir, SkillDef chainRef, long chainEndTicks, long nowTicks)
        {
            Context = context;
            Moving = moving;
            Dir = dir;
            ChainRef = chainRef;
            ChainEndTicks = chainEndTicks;
            NowTicks = nowTicks;
        }
    }

    /// <summary>Tells selection which skills are still cooling down (rows pointing at them are skipped, Rule 13.1).</summary>
    public interface ISkillCooldowns
    {
        /// <summary>True while <paramref name="skill"/> cannot start because of its cooldown.</summary>
        bool IsCoolingDown(SkillDef skill);
    }

    /// <summary>
    /// Every loaded skill and command table, plus the built-in fallback skills (GDD Rule 22). <see cref="Select"/> is the pure
    /// selection function of Rule 13: it always returns a skill and does not allocate. Build it once at boot (see
    /// <see cref="SkillLoader"/>); single-thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// var q = new SelectionQuery(SkillContext.Ground, moving: false, SkillDir.Neutral, chainRef, chainEndTick, nowTick);
    /// SkillDef next = library.Select("spear", in q, cooldowns);   // never null
    /// </code>
    /// </example>
    public sealed class SkillLibrary
    {
        /// <summary>Ids of the built-in fallback skills.</summary>
        public const string FallbackGroundId = "fallback.ground", FallbackAirId = "fallback.air", FallbackDashId = "fallback.dash";

        private readonly Dictionary<string, SkillDef> _skills = new Dictionary<string, SkillDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, CommandTable> _tables = new Dictionary<string, CommandTable>(StringComparer.Ordinal);
        private readonly SkillDef _fallbackGround, _fallbackAir, _fallbackDash;

        /// <summary>Creates an empty library holding only the fallback skills.</summary>
        public SkillLibrary()
        {
            // Prototype rhythm F-G / F-A / F-D (GDD Rule 22): used when a table is missing or no row matches.
            _fallbackGround = BuiltIn(FallbackGroundId, 0.04, 0.06, 0.20);
            _fallbackAir = BuiltIn(FallbackAirId, 0.03, 0.07, 0.18);
            _fallbackDash = BuiltIn(FallbackDashId, 0.02, 0.10, 0.26);
        }

        /// <summary>Number of loaded skills (fallbacks not counted).</summary>
        public int SkillCount => _skills.Count;

        /// <summary>Loaded skills, for validation and tools.</summary>
        public IEnumerable<SkillDef> Skills => _skills.Values;

        /// <summary>Loaded command tables, for validation and tools.</summary>
        public IEnumerable<CommandTable> Tables => _tables.Values;

        /// <summary>Adds a skill. Returns false (and keeps the first) when the id is already present.</summary>
        public bool Add(SkillDef skill)
        {
            if (skill == null || _skills.ContainsKey(skill.Id)) return false;
            _skills.Add(skill.Id, skill);
            return true;
        }

        /// <summary>
        /// Adds or overwrites <paramref name="skill"/> (hot reload). A <see cref="SkillDef"/> is immutable, so a cast already
        /// running on the old definition finishes on it; the next lookup by id gets the new one.
        /// </summary>
        public void Replace(SkillDef skill)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            _skills[skill.Id] = skill;
        }

        /// <summary>Removes a skill (its file was deleted). Returns false when the id was unknown.</summary>
        public bool Remove(string id) => id != null && _skills.Remove(id);

        /// <summary>Adds or overwrites the command table of its weapon family (hot reload).</summary>
        public void ReplaceTable(CommandTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            _tables[table.WeaponFamily] = table;
        }

        /// <summary>Removes the command table of <paramref name="family"/>. Returns false when there was none.</summary>
        public bool RemoveTable(string family) => family != null && _tables.Remove(family);

        /// <summary>Adds a command table. Returns false (and keeps the first) when the family already has one.</summary>
        public bool AddTable(CommandTable table)
        {
            if (table == null || _tables.ContainsKey(table.WeaponFamily)) return false;
            _tables.Add(table.WeaponFamily, table);
            return true;
        }

        /// <summary>Looks up a loaded skill by id.</summary>
        public bool TryGet(string id, out SkillDef skill)
        {
            if (id != null) return _skills.TryGetValue(id, out skill);
            skill = null;
            return false;
        }

        /// <summary>Looks up the command table of a weapon family.</summary>
        public bool TryGetTable(string family, out CommandTable table)
        {
            if (family != null) return _tables.TryGetValue(family, out table);
            table = null;
            return false;
        }

        /// <summary>Built-in fallback skill for <paramref name="context"/> (ground for anything else).</summary>
        public SkillDef Fallback(SkillContext context) =>
            context == SkillContext.Air ? _fallbackAir : context == SkillContext.Dash ? _fallbackDash : _fallbackGround;

        /// <summary>
        /// Picks the skill for one Attack press (GDD Rule 13): keep rows matching every condition, then compare priority →
        /// matching follow-up link → specificity (id link beats tag link) → declaration order; no match gives the fallback.
        /// </summary>
        public SkillDef Select(string weaponFamily, in SelectionQuery query, ISkillCooldowns cooldowns)
        {
            if (!TryGetTable(weaponFamily, out CommandTable table)) return Fallback(query.Context);

            CommandRow[] rows = table.Rows;
            SkillDef best = null;
            CommandRow bestRow = null;
            bool bestById = false;
            for (int i = 0; i < rows.Length; i++)
            {
                CommandRow row = rows[i];
                if ((row.Context & query.Context) == 0) continue;
                if (row.Moving == MovingFilter.Moving && !query.Moving) continue;
                if (row.Moving == MovingFilter.Still && query.Moving) continue;
                if (!row.AnyDir && row.Dir != query.Dir) continue;

                bool byId = false;
                if (row.HasAfter && !LinkMatches(row, in query, out byId)) continue;

                if (!_skills.TryGetValue(row.SkillId, out SkillDef skill)) continue;      // missing or rejected at load (Rule 22)
                if (cooldowns != null && cooldowns.IsCoolingDown(skill)) continue;

                if (bestRow == null || Beats(row, byId, bestRow, bestById))
                {
                    best = skill;
                    bestRow = row;
                    bestById = byId;
                }
            }
            return best ?? Fallback(query.Context);
        }

        private static bool LinkMatches(CommandRow row, in SelectionQuery query, out bool byId)
        {
            byId = false;
            SkillDef chain = query.ChainRef;
            if (chain == null) return false;
            if (query.NowTicks - query.ChainEndTicks > row.LinkWindowTicks) return false;
            for (int i = 0; i < row.AfterIds.Length; i++)
                if (string.Equals(row.AfterIds[i], chain.Id, StringComparison.Ordinal))
                {
                    byId = true;
                    return true;
                }
            return false;
        }

        private static bool Beats(CommandRow a, bool aById, CommandRow b, bool bById)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            if (a.HasAfter != b.HasAfter) return a.HasAfter;
            if (a.Specificity != b.Specificity) return a.Specificity > b.Specificity;
            if (aById != bById) return aById;
            return a.Order < b.Order;
        }

        // Built-in fallback (GDD Rule 22): Start → Animation "attack" and Start → Hit after hitAt (prototype rhythm).
        private static SkillDef BuiltIn(string id, double hitAt, double hitDuration, double endAt)
        {
            var g = new SkillGraphData { Id = id };
            g.Nodes.Add(new SkillNodeData { Id = 0, Kind = SkillNodeKind.Start });
            var anim = new SkillNodeData { Id = 1, Kind = SkillNodeKind.Animation };
            anim.Values["AnimationType"] = "attack";
            var hit = new SkillNodeData { Id = 2, Kind = SkillNodeKind.Hit };
            hit.Values["ActionDelay"] = SkillSchema.Num(hitAt);
            hit.Values["HitEffectAddressable"] = "hp.fallback";
            g.Nodes.Add(anim);
            g.Nodes.Add(hit);
            g.Links.Add(new SkillLink { From = 0, To = 1 });
            g.Links.Add(new SkillLink { From = 0, To = 2 });
            return new SkillDef(id, g);
        }
    }
}

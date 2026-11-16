using System;

namespace Evermeadow.Core.Skills
{
    /// <summary>Row filter on whether the character was moving in the press record (ground only).</summary>
    public enum MovingFilter
    {
        /// <summary>Matches both.</summary>
        Any,
        /// <summary>Standing still.</summary>
        Still,
        /// <summary>Walking or running.</summary>
        Moving
    }

    /// <summary>One row of a command table (GDD Skill &amp; Moveset Rule 11).</summary>
    public sealed class CommandRow
    {
        /// <summary>Skill id the row starts.</summary>
        public string SkillId { get; }
        /// <summary>Contexts the row matches (any = all three).</summary>
        public SkillContext Context { get; }
        /// <summary>Moving filter.</summary>
        public MovingFilter Moving { get; }
        /// <summary>True when the row matches every direction.</summary>
        public bool AnyDir { get; }
        /// <summary>Direction the row needs when <see cref="AnyDir"/> is false.</summary>
        public SkillDir Dir { get; }
        /// <summary>Skill ids the previous skill must be one of (follow-up link).</summary>
        public string[] AfterIds { get; }
        /// <summary>True when the row has an <c>after</c> condition.</summary>
        public bool HasAfter => AfterIds.Length > 0;
        /// <summary>Link window in unscaled seconds.</summary>
        public double LinkWindowSeconds { get; }
        /// <summary>Link window snapped to the step grid (<see cref="SkillTime.TickOf"/>), compared with ≤.</summary>
        public long LinkWindowTicks { get; }
        /// <summary>Higher wins.</summary>
        public int Priority { get; }
        /// <summary>Declaration order in the table (0-based); lower wins the last tie.</summary>
        public int Order { get; }
        /// <summary>Number of constrained fields (context, moving, dir, after).</summary>
        public int Specificity { get; }

        internal CommandRow(string skillId, SkillContext context, MovingFilter moving, bool anyDir, SkillDir dir,
            string[] afterIds, double linkWindowSeconds, int priority, int order)
        {
            SkillId = skillId;
            Context = context;
            Moving = moving;
            AnyDir = anyDir;
            Dir = dir;
            AfterIds = afterIds ?? Array.Empty<string>();
            LinkWindowSeconds = linkWindowSeconds;
            LinkWindowTicks = SkillTime.TickOf(linkWindowSeconds);
            Priority = priority;
            Order = order;
            Specificity = (context != SkillXmlReader.AllContexts ? 1 : 0) + (moving != MovingFilter.Any ? 1 : 0) +
                          (anyDir ? 0 : 1) + (HasAfter ? 1 : 0);
        }
    }

    /// <summary>The command table of one weapon family; rows keep declaration order.</summary>
    public sealed class CommandTable
    {
        /// <summary>Weapon family tag.</summary>
        public string WeaponFamily { get; }
        /// <summary>Rows in declaration order.</summary>
        public CommandRow[] Rows { get; }

        internal CommandTable(string weaponFamily, CommandRow[] rows)
        {
            WeaponFamily = weaponFamily;
            Rows = rows ?? Array.Empty<CommandRow>();
        }
    }
}

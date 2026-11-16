using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>
    /// One editable row of a moveset (Moveset Tool, reader, writer): when the player gives <see cref="Input"/> under these
    /// conditions, cast <see cref="Skill"/>. Validated by <see cref="MovesetValidator"/>, compiled into <see cref="MovesetRow"/>.
    /// </summary>
    public sealed class MovesetRowData
    {
        /// <summary>Button + gesture.</summary>
        public MoveInput Input = MoveInput.Press(MoveButton.Button1);
        /// <summary>Skill id (six digits, the Skill Tool file name) that shows this move.</summary>
        public string Skill = "";
        /// <summary>Where the move can be used (ground / air / dash; several allowed).</summary>
        public SkillContext Context = MovesetXml.AllContexts;
        /// <summary>Standing still or moving (ground rows only).</summary>
        public MovingFilter Moving = MovingFilter.Any;
        /// <summary>True when the held direction does not matter.</summary>
        public bool AnyDir = true;
        /// <summary>Held direction relative to the facing (when <see cref="AnyDir"/> is false).</summary>
        public SkillDir Dir = SkillDir.Neutral;
        /// <summary>Skill ids the previous move must be (combo follow-up); empty = opener.</summary>
        public List<string> After = new List<string>();
        /// <summary>Seconds after the previous move ends during which this follow-up is still possible.</summary>
        public double LinkWindow = MovesetXml.DefaultLinkWindow;
        /// <summary>Higher wins when several rows match.</summary>
        public int Priority;
        /// <summary>Seconds before this row can be used again (0 = none). Starts when the move starts.</summary>
        public double Cooldown;

        /// <summary>Deep copy.</summary>
        public MovesetRowData Clone()
        {
            var c = (MovesetRowData)MemberwiseClone();
            c.After = new List<string>(After);
            return c;
        }
    }

    /// <summary>
    /// An editable moveset: the moves one socket stone gives. Id = file name (six digits from 100000, own namespace in
    /// <c>StreamingAssets/Movesets/</c>). <see cref="AnyWeapon"/> or <see cref="Weapons"/> say which weapon families can use it.
    /// </summary>
    /// <example>
    /// <code>
    /// MovesetData m = MovesetData.CreateDefault("100000");    // one row: Button1 → skill 100000
    /// m.Rows[0].Context = SkillContext.Ground;
    /// string xml = MovesetXml.Write(m);
    /// </code>
    /// </example>
    public sealed class MovesetData
    {
        /// <summary>Moveset id (file name).</summary>
        public string Id = "";
        /// <summary>Designer label shown in tools (not player-facing text).</summary>
        public string Name = "";
        /// <summary>Usable with every weapon family.</summary>
        public bool AnyWeapon = true;
        /// <summary>Weapon families that can use it (when not <see cref="AnyWeapon"/>).</summary>
        public List<string> Weapons = new List<string>();
        /// <summary>Free note for designers.</summary>
        public string Note = "";
        /// <summary>Rows in authoring order (the order breaks ties).</summary>
        public List<MovesetRowData> Rows = new List<MovesetRowData>();

        /// <summary>A new moveset with one Button1 row.</summary>
        public static MovesetData CreateDefault(string id) =>
            new MovesetData { Id = id ?? "", Rows = { new MovesetRowData { Skill = "100000" } } };

        /// <summary>Deep copy.</summary>
        public MovesetData Clone()
        {
            var c = (MovesetData)MemberwiseClone();
            c.Weapons = new List<string>(Weapons);
            c.Rows = new List<MovesetRowData>(Rows.Count);
            foreach (MovesetRowData r in Rows) c.Rows.Add(r.Clone());
            return c;
        }
    }

    /// <summary>A validated, immutable moveset row (see <see cref="MovesetRowData"/> for the meaning of each field).</summary>
    public sealed class MovesetRow
    {
        internal MovesetRow(MovesetRowData d, int order)
        {
            Input = d.Input;
            SkillId = d.Skill;
            Context = d.Context;
            Moving = d.Moving;
            AnyDir = d.AnyDir;
            Dir = d.Dir;
            AfterIds = d.After.ToArray();
            LinkWindowTicks = SkillTime.TickOf(d.LinkWindow);
            Priority = d.Priority;
            CooldownTicks = SkillTime.TickOf(d.Cooldown);
            Order = order;
            Specificity = (Context != MovesetXml.AllContexts ? 1 : 0) + (Moving != MovingFilter.Any ? 1 : 0) + (AnyDir ? 0 : 1) + (HasAfter ? 1 : 0);
        }

        /// <summary>Button + gesture.</summary>
        public MoveInput Input { get; }
        /// <summary>Skill id to cast.</summary>
        public string SkillId { get; }
        /// <summary>Allowed contexts.</summary>
        public SkillContext Context { get; }
        /// <summary>Still / moving filter.</summary>
        public MovingFilter Moving { get; }
        /// <summary>Any direction.</summary>
        public bool AnyDir { get; }
        /// <summary>Required direction (when not <see cref="AnyDir"/>).</summary>
        public SkillDir Dir { get; }
        /// <summary>Previous skill ids for a follow-up.</summary>
        public string[] AfterIds { get; }
        /// <summary>True for a follow-up row.</summary>
        public bool HasAfter => AfterIds.Length > 0;
        /// <summary>Link window in clock ticks (1 µs).</summary>
        public long LinkWindowTicks { get; }
        /// <summary>Priority.</summary>
        public int Priority { get; }
        /// <summary>Cooldown in ticks (0 = none).</summary>
        public long CooldownTicks { get; }
        /// <summary>Index in the moveset.</summary>
        public int Order { get; }
        /// <summary>Number of constrained fields (more specific wins a tie).</summary>
        public int Specificity { get; }
    }

    /// <summary>A validated, immutable moveset (compiled from <see cref="MovesetData"/>).</summary>
    public sealed class MovesetDef
    {
        private readonly string[] _weapons;

        internal MovesetDef(MovesetData d)
        {
            Id = d.Id;
            Name = d.Name ?? "";
            AnyWeapon = d.AnyWeapon;
            _weapons = d.AnyWeapon ? Array.Empty<string>() : d.Weapons.ToArray();
            var rows = new MovesetRow[d.Rows.Count];
            for (int i = 0; i < rows.Length; i++) rows[i] = new MovesetRow(d.Rows[i], i);
            Rows = rows;
        }

        /// <summary>Moveset id.</summary>
        public string Id { get; }
        /// <summary>Designer label.</summary>
        public string Name { get; }
        /// <summary>Usable with every weapon family.</summary>
        public bool AnyWeapon { get; }
        /// <summary>Allowed weapon families (empty when <see cref="AnyWeapon"/>).</summary>
        public IReadOnlyList<string> Weapons => _weapons;
        /// <summary>Rows in authoring order.</summary>
        public MovesetRow[] Rows { get; }

        /// <summary>True when a weapon of <paramref name="family"/> can use this moveset.</summary>
        public bool AllowsWeapon(string family)
        {
            if (AnyWeapon) return true;
            for (int i = 0; i < _weapons.Length; i++)
                if (string.Equals(_weapons[i], family, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}

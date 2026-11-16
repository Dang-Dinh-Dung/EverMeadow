using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.Moveset
{
    /// <summary>Outcome of socketing a stone (its moveset) into a weapon.</summary>
    public enum SocketResult
    {
        /// <summary>Socketed (or would be).</summary>
        Ok,
        /// <summary>Slot index outside the weapon's sockets.</summary>
        BadSlot,
        /// <summary>The moveset does not allow this weapon family.</summary>
        WeaponNotAllowed,
        /// <summary>The same moveset is already in another socket.</summary>
        AlreadySocketed,
        /// <summary>A row uses the same input under overlapping conditions as a row of another socket (see the conflicts).</summary>
        Conflict
    }

    /// <summary>Two rows of two sockets that the player could not tell apart.</summary>
    public readonly struct MoveConflict
    {
        /// <summary>Row of the moveset being socketed.</summary>
        public readonly int Row;
        /// <summary>Socket already holding the other moveset.</summary>
        public readonly int OtherSlot;
        /// <summary>Id of the other moveset.</summary>
        public readonly string OtherMovesetId;
        /// <summary>Row of the other moveset.</summary>
        public readonly int OtherRow;
        /// <summary>The shared input.</summary>
        public readonly MoveInput Input;

        /// <summary>Creates a conflict record.</summary>
        public MoveConflict(int row, int otherSlot, string otherMovesetId, int otherRow, MoveInput input)
        {
            Row = row;
            OtherSlot = otherSlot;
            OtherMovesetId = otherMovesetId;
            OtherRow = otherRow;
            Input = input;
        }

        /// <inheritdoc />
        public override string ToString() => $"row {Row} and socket {OtherSlot} (moveset {OtherMovesetId}) row {OtherRow} both use {Input}";
    }

    /// <summary>
    /// The sockets of the equipped weapon (2–4) and the moveset of the stone in each. Every move comes from a stone (user
    /// decision 2026-11-13): an empty weapon has no moves. Socketing is refused when the moveset does not allow the weapon
    /// family, is already socketed, or has a row the player could not tell apart from a row of another socket
    /// (<see cref="RowsOverlap"/>). Sockets hold moveset ids, so a moveset edited while playing is picked up by the next
    /// lookup. Main thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// var loadout = new MovesetLoadout("spear", socketCount: 3);
    /// var conflicts = new List&lt;MoveConflict&gt;();
    /// if (loadout.TrySocket(0, basicAttack, movesets, conflicts) != SocketResult.Ok) ShowWhy(conflicts);
    /// </code>
    /// </example>
    public sealed class MovesetLoadout
    {
        /// <summary>Fewest sockets a weapon has.</summary>
        public const int MinSockets = 2;
        /// <summary>Most sockets a weapon has.</summary>
        public const int MaxSockets = 4;

        private readonly string[] _sockets;

        /// <summary>Creates the sockets of a weapon of <paramref name="weaponFamily"/>.</summary>
        public MovesetLoadout(string weaponFamily, int socketCount)
        {
            if (socketCount < MinSockets || socketCount > MaxSockets)
                throw new ArgumentOutOfRangeException(nameof(socketCount), $"a weapon has {MinSockets}–{MaxSockets} sockets");
            WeaponFamily = weaponFamily ?? "";
            _sockets = new string[socketCount];
        }

        /// <summary>Weapon family of the equipped weapon.</summary>
        public string WeaponFamily { get; }

        /// <summary>Number of sockets.</summary>
        public int SocketCount => _sockets.Length;

        /// <summary>Moveset id in <paramref name="slot"/>, or null when empty.</summary>
        public string Get(int slot) => slot >= 0 && slot < _sockets.Length ? _sockets[slot] : null;

        /// <summary>Checks socketing <paramref name="def"/> into <paramref name="slot"/> (replacing what is there) without changing anything.</summary>
        public SocketResult CanSocket(int slot, MovesetDef def, MovesetLibrary library, List<MoveConflict> conflicts)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (slot < 0 || slot >= _sockets.Length) return SocketResult.BadSlot;
            if (!def.AllowsWeapon(WeaponFamily)) return SocketResult.WeaponNotAllowed;
            for (int s = 0; s < _sockets.Length; s++)
                if (s != slot && _sockets[s] == def.Id) return SocketResult.AlreadySocketed;

            bool conflict = false;
            for (int s = 0; s < _sockets.Length; s++)
            {
                if (s == slot || _sockets[s] == null || library == null || !library.TryGet(_sockets[s], out MovesetDef other)) continue;
                if (FindConflicts(def, other, s, conflicts)) conflict = true;
            }
            return conflict ? SocketResult.Conflict : SocketResult.Ok;
        }

        /// <summary>Sockets <paramref name="def"/> when <see cref="CanSocket"/> says <see cref="SocketResult.Ok"/>.</summary>
        public SocketResult TrySocket(int slot, MovesetDef def, MovesetLibrary library, List<MoveConflict> conflicts)
        {
            SocketResult r = CanSocket(slot, def, library, conflicts);
            if (r == SocketResult.Ok) _sockets[slot] = def.Id;
            return r;
        }

        /// <summary>Empties a socket; false when it was already empty or out of range.</summary>
        public bool Unsocket(int slot)
        {
            if (slot < 0 || slot >= _sockets.Length || _sockets[slot] == null) return false;
            _sockets[slot] = null;
            return true;
        }

        /// <summary>
        /// Re-checks the sockets against the current movesets (after a hot reload or a save-game load): missing movesets,
        /// weapon no longer allowed, new conflicts. Socket order still decides at runtime; this only reports.
        /// </summary>
        public void Validate(MovesetLibrary library, List<string> problems)
        {
            for (int s = 0; s < _sockets.Length; s++)
            {
                if (_sockets[s] == null) continue;
                if (library == null || !library.TryGet(_sockets[s], out MovesetDef def))
                {
                    problems.Add($"socket {s}: moveset {_sockets[s]} is missing");
                    continue;
                }
                if (!def.AllowsWeapon(WeaponFamily)) problems.Add($"socket {s}: moveset {def.Id} does not allow weapon family '{WeaponFamily}'");
                var conflicts = new List<MoveConflict>();
                for (int o = s + 1; o < _sockets.Length; o++)
                    if (_sockets[o] != null && library.TryGet(_sockets[o], out MovesetDef other)) FindConflicts(def, other, o, conflicts);
                foreach (MoveConflict c in conflicts) problems.Add($"socket {s}: {c}");
            }
        }

        /// <summary>Adds every pair of overlapping rows of <paramref name="a"/> and <paramref name="b"/> (b sits in <paramref name="slotB"/>).</summary>
        public static bool FindConflicts(MovesetDef a, MovesetDef b, int slotB, List<MoveConflict> conflicts)
        {
            bool any = false;
            for (int i = 0; i < a.Rows.Length; i++)
                for (int j = 0; j < b.Rows.Length; j++)
                {
                    if (!RowsOverlap(a.Rows[i], b.Rows[j])) continue;
                    any = true;
                    conflicts?.Add(new MoveConflict(i, slotB, b.Id, j, a.Rows[i].Input));
                }
            return any;
        }

        /// <summary>
        /// True when one press could match both rows and neither is a clear winner: same input, a shared context, compatible
        /// still / moving and direction, and either both openers or follow-ups of a shared previous skill. Priority does not
        /// separate rows of two stones (the player could not know which wins). A follow-up never conflicts with an opener:
        /// the follow-up always wins inside its link window.
        /// </summary>
        public static bool RowsOverlap(MovesetRow a, MovesetRow b)
        {
            if (a.Input != b.Input) return false;
            if ((a.Context & b.Context) == SkillContext.None) return false;
            if (a.Moving != MovingFilter.Any && b.Moving != MovingFilter.Any && a.Moving != b.Moving) return false;
            if (!a.AnyDir && !b.AnyDir && a.Dir != b.Dir) return false;
            if (a.HasAfter != b.HasAfter) return false;
            if (!a.HasAfter) return true;
            foreach (string x in a.AfterIds)
                if (Array.IndexOf(b.AfterIds, x) >= 0) return true;
            return false;
        }
    }

    /// <summary>What the player did, as the Controller sees it when it consumes the input.</summary>
    public readonly struct MoveQuery
    {
        /// <summary>Recognised input.</summary>
        public readonly MoveInput Input;
        /// <summary>Context at consumption (one of ground / air / dash).</summary>
        public readonly SkillContext Context;
        /// <summary>Moving horizontally (from the press record).</summary>
        public readonly bool Moving;
        /// <summary>Held direction relative to the facing (from the press record).</summary>
        public readonly SkillDir Dir;
        /// <summary>Skill id of the move that just ended (empty = none).</summary>
        public readonly string ChainSkillId;
        /// <summary>Clock tick when that move ended or was cancelled.</summary>
        public readonly long ChainEndTicks;
        /// <summary>Clock tick now.</summary>
        public readonly long NowTicks;

        /// <summary>Creates a query.</summary>
        public MoveQuery(MoveInput input, SkillContext context, bool moving, SkillDir dir, string chainSkillId, long chainEndTicks, long nowTicks)
        {
            Input = input;
            Context = context;
            Moving = moving;
            Dir = dir;
            ChainSkillId = chainSkillId ?? "";
            ChainEndTicks = chainEndTicks;
            NowTicks = nowTicks;
        }
    }

    /// <summary>The chosen move, or <see cref="None"/>.</summary>
    public readonly struct MoveChoice
    {
        /// <summary>Socket the move came from (-1 = none).</summary>
        public readonly int Slot;
        /// <summary>Its moveset.</summary>
        public readonly MovesetDef Moveset;
        /// <summary>Its row.</summary>
        public readonly MovesetRow Row;

        /// <summary>Creates a choice.</summary>
        public MoveChoice(int slot, MovesetDef moveset, MovesetRow row)
        {
            Slot = slot;
            Moveset = moveset;
            Row = row;
        }

        /// <summary>No move matches (the input does nothing).</summary>
        public static MoveChoice None => new MoveChoice(-1, null, null);

        /// <summary>True when a move was chosen.</summary>
        public bool Found => Row != null;

        /// <summary>Skill id to cast (empty when none).</summary>
        public string SkillId => Row != null ? Row.SkillId : "";
    }

    /// <summary>Cooldowns of moveset rows (owned by the runtime that casts).</summary>
    public interface IMoveCooldowns
    {
        /// <summary>True while row <paramref name="row"/> of <paramref name="movesetId"/> is cooling down.</summary>
        bool IsCoolingDown(string movesetId, int row);
    }

    /// <summary>
    /// Picks the move for one recognised input (pure, no allocation): rows of every socketed moveset that allow the weapon
    /// and match input, context, still / moving, direction and follow-up (previous skill in <c>after</c> and still inside the
    /// row's link window); rows cooling down or pointing to a skill that is not loaded are skipped. Best by priority, then a
    /// matched follow-up over an opener, then more specific, then lower socket, then row order. No match = no move (there
    /// is no fallback: every move comes from a stone).
    /// </summary>
    /// <example>
    /// <code>
    /// var q = new MoveQuery(MoveInput.Press(MoveButton.Button1), SkillContext.Ground, moving: false, SkillDir.Neutral, chainId, chainEnd, now);
    /// MoveChoice c = MoveSelector.Select(loadout, movesets, in q, cooldowns, skills);
    /// if (c.Found) Cast(c.SkillId);
    /// </code>
    /// </example>
    public static class MoveSelector
    {
        /// <summary>Chooses a move; see the class summary for the rules.</summary>
        public static MoveChoice Select(MovesetLoadout loadout, MovesetLibrary movesets, in MoveQuery q, IMoveCooldowns cooldowns = null, SkillLibrary skills = null)
        {
            if (loadout == null || movesets == null) return MoveChoice.None;
            MoveChoice best = MoveChoice.None;
            bool bestById = false;
            for (int s = 0; s < loadout.SocketCount; s++)
            {
                string id = loadout.Get(s);
                if (id == null || !movesets.TryGet(id, out MovesetDef def) || !def.AllowsWeapon(loadout.WeaponFamily)) continue;
                MovesetRow[] rows = def.Rows;
                for (int i = 0; i < rows.Length; i++)
                {
                    MovesetRow row = rows[i];
                    if (row.Input != q.Input) continue;
                    if ((row.Context & q.Context) == SkillContext.None) continue;
                    if (row.Moving == MovingFilter.Moving && !q.Moving) continue;
                    if (row.Moving == MovingFilter.Still && q.Moving) continue;
                    if (!row.AnyDir && row.Dir != q.Dir) continue;
                    bool byId = false;
                    if (row.HasAfter)
                    {
                        if (q.ChainSkillId.Length == 0 || q.NowTicks - q.ChainEndTicks > row.LinkWindowTicks) continue;
                        for (int a = 0; a < row.AfterIds.Length; a++)
                            if (string.Equals(row.AfterIds[a], q.ChainSkillId, StringComparison.Ordinal))
                            {
                                byId = true;
                                break;
                            }
                        if (!byId) continue;
                    }
                    if (cooldowns != null && cooldowns.IsCoolingDown(def.Id, i)) continue;
                    if (skills != null && !skills.TryGet(row.SkillId, out _)) continue;

                    if (!best.Found || Beats(row, byId, best.Row, bestById))
                    {
                        best = new MoveChoice(s, def, row);
                        bestById = byId;
                    }
                }
            }
            return best;
        }

        // Socket order and row order are visited in ascending order, so "earlier wins" is the default when nothing else differs.
        private static bool Beats(MovesetRow a, bool aById, MovesetRow b, bool bById)
        {
            if (a.Priority != b.Priority) return a.Priority > b.Priority;
            if (aById != bById) return aById;
            if (a.Specificity != b.Specificity) return a.Specificity > b.Specificity;
            return false;
        }
    }
}

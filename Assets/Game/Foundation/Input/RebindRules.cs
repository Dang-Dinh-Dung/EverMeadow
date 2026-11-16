using System;
using System.Collections.Generic;

namespace Evermeadow.Foundation.Input
{
    /// <summary>One binding slot as seen by the rebind rules (GDD Input Rule 17). Built by the adapter from the action asset.</summary>
    public struct BindingSlot
    {
        /// <summary>Action name, or "Action/part" for a composite part (each key of Move is its own key).</summary>
        public string Key;
        /// <summary>Button action, or None for an axis part.</summary>
        public InputActionId Action;
        /// <summary>Modes the slot is active in (<see cref="ActionCatalog.ModeMask"/>).</summary>
        public int Modes;
        /// <summary>Device family of the slot.</summary>
        public DeviceKind Device;
        /// <summary>Effective control path; empty when unbound.</summary>
        public string Path;
        /// <summary>Fixed Pause/Cancel slot (Esc, Menu/Options; decision I-7). Never rebound.</summary>
        public bool Fixed;
        /// <summary>Navigate, Confirm or Cancel: must keep at least one key per device.</summary>
        public bool MenuEssential;
    }

    /// <summary>Result of evaluating a rebind.</summary>
    public enum RebindOutcome
    {
        /// <summary>Legal; apply it.</summary>
        Apply,
        /// <summary>Another action sharing a mode uses the key; Settings chooses Swap or Cancel.</summary>
        Conflict,
        /// <summary>The slot is a fixed Pause/Cancel slot.</summary>
        RejectedFixedSlot,
        /// <summary>The key is reserved for Pause/Cancel (Esc, Menu/Options).</summary>
        RejectedFixedKey,
        /// <summary>Navigate, Confirm or Cancel would lose its last key on a device.</summary>
        RejectedMenuUnbound,
        /// <summary>A swap would create a new conflict for the swapped slot.</summary>
        RejectedSwapConflict
    }

    /// <summary>
    /// Pure rebind rules (GDD Input Rule 17; ADR-0001 Decision 1 puts conflict handling in the core).
    /// Two slots conflict when they share a path, belong to different keys and share at least one mode;
    /// two fixed slots never conflict with each other (Esc is both Pause and Cancel by design).
    /// </summary>
    public static class RebindRules
    {
        /// <summary>Keys reserved for Pause and Cancel (I-7).</summary>
        public static readonly string[] FixedKeys = { "<Keyboard>/escape", "<Gamepad>/start" };

        /// <summary>True for Esc and Menu/Options.</summary>
        public static bool IsFixedKey(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            foreach (string k in FixedKeys)
            {
                if (string.Equals(k, path, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates binding slot <paramref name="target"/> to <paramref name="newPath"/> (empty = remove the key).
        /// On <see cref="RebindOutcome.Conflict"/>, <paramref name="conflicts"/> lists the conflicting slot indices.
        /// </summary>
        public static RebindOutcome Evaluate(IReadOnlyList<BindingSlot> slots, int target, string newPath, List<int> conflicts)
        {
            conflicts.Clear();
            BindingSlot t = slots[target];
            if (t.Fixed) return RebindOutcome.RejectedFixedSlot;
            if (IsFixedKey(newPath) && !OwnsFixedKeys(t.Action)) return RebindOutcome.RejectedFixedKey;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i != target && Conflicts(slots[i], t, newPath)) conflicts.Add(i);
            }
            if (conflicts.Count > 0) return RebindOutcome.Conflict;

            var after = Copy(slots);
            after[target] = WithPath(t, newPath);
            return KeepsMenuReachable(slots, after) ? RebindOutcome.Apply : RebindOutcome.RejectedMenuUnbound;
        }

        /// <summary>
        /// Evaluates the Swap answer to a conflict: <paramref name="target"/> takes <paramref name="newPath"/> and every slot in
        /// <paramref name="conflicts"/> takes the target's old path.
        /// </summary>
        public static RebindOutcome EvaluateSwap(IReadOnlyList<BindingSlot> slots, int target, string newPath, IReadOnlyList<int> conflicts)
        {
            string oldPath = slots[target].Path ?? string.Empty;
            foreach (int c in conflicts)
            {
                if (slots[c].Fixed) return RebindOutcome.RejectedFixedKey;
                if (IsFixedKey(oldPath) && !OwnsFixedKeys(slots[c].Action)) return RebindOutcome.RejectedFixedKey;
            }

            var after = Copy(slots);
            after[target] = WithPath(slots[target], newPath);
            foreach (int c in conflicts) after[c] = WithPath(slots[c], oldPath);

            if (!KeepsMenuReachable(slots, after)) return RebindOutcome.RejectedMenuUnbound;
            foreach (int c in conflicts)
            {
                if (HasConflict(after, c)) return RebindOutcome.RejectedSwapConflict;
            }
            return RebindOutcome.Apply;
        }

        /// <summary>Pairs of conflicting slots in <paramref name="slots"/> (validation of defaults and loaded data).</summary>
        public static void FindConflicts(IReadOnlyList<BindingSlot> slots, List<(int a, int b)> pairs)
        {
            pairs.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (Conflicts(slots[j], slots[i], slots[i].Path)) pairs.Add((i, j));
                }
            }
        }

        private static bool OwnsFixedKeys(InputActionId action) => action == InputActionId.Pause || action == InputActionId.Cancel;

        private static bool HasConflict(IReadOnlyList<BindingSlot> slots, int index)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (i != index && Conflicts(slots[i], slots[index], slots[index].Path)) return true;
            }
            return false;
        }

        private static bool Conflicts(BindingSlot other, BindingSlot target, string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(other.Path)) return false;
            if (!string.Equals(other.Path, path, StringComparison.OrdinalIgnoreCase)) return false;
            if (other.Key == target.Key) return false;
            if (other.Fixed && target.Fixed) return false;
            return (other.Modes & target.Modes) != 0;
        }

        private static bool KeepsMenuReachable(IReadOnlyList<BindingSlot> before, IReadOnlyList<BindingSlot> after)
        {
            for (int i = 0; i < before.Count; i++)
            {
                BindingSlot s = before[i];
                if (!s.MenuEssential || string.IsNullOrEmpty(s.Path)) continue;
                if (CountBound(before, s.Key, s.Device) > 0 && CountBound(after, s.Key, s.Device) == 0) return false;
            }
            return true;
        }

        private static int CountBound(IReadOnlyList<BindingSlot> slots, string key, DeviceKind device)
        {
            int n = 0;
            foreach (BindingSlot s in slots)
            {
                if (s.Key == key && s.Device == device && !string.IsNullOrEmpty(s.Path)) n++;
            }
            return n;
        }

        private static BindingSlot WithPath(BindingSlot s, string path)
        {
            s.Path = path ?? string.Empty;
            return s;
        }

        private static List<BindingSlot> Copy(IReadOnlyList<BindingSlot> slots)
        {
            var list = new List<BindingSlot>(slots.Count);
            for (int i = 0; i < slots.Count; i++) list.Add(slots[i]);
            return list;
        }
    }
}

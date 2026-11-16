namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Which actions and axes belong to which modes (GDD Input Rule 2), chord definitions (Rule 4) and hold actions (Rule 9).
    /// Immutable data; no state.
    /// </summary>
    public static class ActionCatalog
    {
        private static readonly int[] ActionModes = BuildActionModes();

        private static int Bit(InputMode m) => 1 << (int)m;

        private static int[] BuildActionModes()
        {
            var t = new int[(int)InputActionId.Count];
            int gameplay = Bit(InputMode.Gameplay);
            t[(int)InputActionId.Jump] = gameplay;
            t[(int)InputActionId.Attack] = gameplay;
            t[(int)InputActionId.Dodge] = gameplay;
            t[(int)InputActionId.Plunge] = gameplay;
            t[(int)InputActionId.DropThrough] = gameplay;
            t[(int)InputActionId.Interact] = gameplay;
            t[(int)InputActionId.Heal] = gameplay;
            t[(int)InputActionId.EatBuff] = gameplay;
            t[(int)InputActionId.Run] = gameplay;
            t[(int)InputActionId.Map] = gameplay | Bit(InputMode.Map);
            t[(int)InputActionId.Pause] = gameplay; // I-8: Esc/Menu pause only when no menu is open; elsewhere they are Cancel
            t[(int)InputActionId.Confirm] = Bit(InputMode.Menu) | Bit(InputMode.Dialogue);
            t[(int)InputActionId.Cancel] = Bit(InputMode.Menu) | Bit(InputMode.Dialogue) | Bit(InputMode.Map) | Bit(InputMode.Fishing);
            t[(int)InputActionId.TabLeft] = Bit(InputMode.Menu);
            t[(int)InputActionId.TabRight] = Bit(InputMode.Menu);
            t[(int)InputActionId.Click] = Bit(InputMode.Menu) | Bit(InputMode.Map);
            t[(int)InputActionId.Reel] = Bit(InputMode.Fishing);
            return t;
        }

        /// <summary>True when <paramref name="action"/> is received while <paramref name="mode"/> is on top. Locked receives nothing.</summary>
        public static bool IsInMode(InputActionId action, InputMode mode)
        {
            if (action <= InputActionId.None || action >= InputActionId.Count) return false;
            return (ActionModes[(int)action] & Bit(mode)) != 0;
        }

        /// <summary>Bit mask of the modes <paramref name="action"/> belongs to (bit = 1 &lt;&lt; (int)InputMode).</summary>
        public static int ModeMask(InputActionId action) =>
            action > InputActionId.None && action < InputActionId.Count ? ActionModes[(int)action] : 0;

        /// <summary>Bit mask of the modes that read <paramref name="axis"/>.</summary>
        public static int AxisModeMask(AxisId axis) =>
            axis == AxisId.Move ? Bit(InputMode.Gameplay) : Bit(InputMode.Menu) | Bit(InputMode.Dialogue) | Bit(InputMode.Map);

        /// <summary>True when <paramref name="axis"/> is read while <paramref name="mode"/> is on top.</summary>
        public static bool IsAxisInMode(AxisId axis, InputMode mode)
        {
            switch (axis)
            {
                case AxisId.Move: return mode == InputMode.Gameplay;
                case AxisId.Navigate: return mode == InputMode.Menu || mode == InputMode.Dialogue || mode == InputMode.Map;
                default: return false;
            }
        }

        /// <summary>True for actions whose held state honours <see cref="HoldMode"/> (Rule 9).</summary>
        public static bool IsHoldAction(InputActionId action) => action == InputActionId.Reel;

        /// <summary>Chord action built on <paramref name="baseAction"/> (Attack → Plunge, Jump → DropThrough), or None.</summary>
        public static InputActionId ChordFor(InputActionId baseAction)
        {
            switch (baseAction)
            {
                case InputActionId.Attack: return InputActionId.Plunge;
                case InputActionId.Jump: return InputActionId.DropThrough;
                default: return InputActionId.None;
            }
        }
    }
}

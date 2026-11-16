namespace Evermeadow.Foundation.Input
{
    /// <summary>
    /// Named actions (GDD Input Rule 2). In-memory only: values are never persisted (ADR-0001 Decision 6);
    /// the Unity adapter maps them to <c>InputAction</c>s by action GUID.
    /// </summary>
    public enum InputActionId
    {
        /// <summary>No action.</summary>
        None = 0,
        /// <summary>Jump (Gameplay).</summary>
        Jump,
        /// <summary>Attack (Gameplay).</summary>
        Attack,
        /// <summary>Dodge (Gameplay).</summary>
        Dodge,
        /// <summary>Plunge; chord "Down + Attack" by default (Rule 4).</summary>
        Plunge,
        /// <summary>Drop through a one-way platform; chord "Down + Jump" by default (Rule 4).</summary>
        DropThrough,
        /// <summary>Interact, including casting a fishing line (Gameplay).</summary>
        Interact,
        /// <summary>Use a healing item (Gameplay).</summary>
        Heal,
        /// <summary>Eat a buff meal (Gameplay).</summary>
        EatBuff,
        /// <summary>Toggle the world map (Gameplay, Map).</summary>
        Map,
        /// <summary>Pause (Gameplay only, I-8). Fixed slot Esc / Menu (Rule 17, I-7).</summary>
        Pause,
        /// <summary>Confirm (Menu, Dialogue).</summary>
        Confirm,
        /// <summary>Cancel (Menu, Dialogue, Map, Fishing). Fixed slot Esc / Menu: same keys as Pause, other modes (I-7, I-8).</summary>
        Cancel,
        /// <summary>Previous tab (Menu).</summary>
        TabLeft,
        /// <summary>Next tab (Menu).</summary>
        TabRight,
        /// <summary>Pointer click (Menu, Map).</summary>
        Click,
        /// <summary>Reel: press to strike, hold to lift the catch zone (Fishing).</summary>
        Reel,
        /// <summary>Each press toggles walk ↔ run (Gameplay, I-9).</summary>
        Run,
        /// <summary>Number of values; not an action.</summary>
        Count
    }

    /// <summary>Continuous axes, sampled once per frame (ADR-0001 Decision 3).</summary>
    public enum AxisId
    {
        /// <summary>Character movement (Gameplay).</summary>
        Move,
        /// <summary>Menu navigation (Menu, Dialogue, Map).</summary>
        Navigate
    }

    /// <summary>Phase of a recorded button event.</summary>
    public enum InputPhase
    {
        /// <summary>Button went down.</summary>
        Pressed,
        /// <summary>Button went up.</summary>
        Released
    }

    /// <summary>Device families used for glyphs (Rule 13). The active device never blocks input (Rule 3).</summary>
    public enum DeviceKind
    {
        /// <summary>Keyboard and mouse.</summary>
        KeyboardMouse,
        /// <summary>Any gamepad.</summary>
        Gamepad
    }

    /// <summary>Input modes on the token stack (Rule 11). Gameplay is the permanent bottom.</summary>
    public enum InputMode
    {
        /// <summary>World control.</summary>
        Gameplay,
        /// <summary>Menus, forge, market, pause.</summary>
        Menu,
        /// <summary>Dialogue (shares Menu actions).</summary>
        Dialogue,
        /// <summary>Fishing minigame.</summary>
        Fishing,
        /// <summary>World map overlay.</summary>
        Map,
        /// <summary>Nothing is recorded (banners, cutscenes, region loading).</summary>
        Locked
    }

    /// <summary>Which clock a buffer window is measured on (GDD "Real" / "Game"; ADR-0001 "Real" / "Scaled").</summary>
    public enum ClockKind
    {
        /// <summary>Unscaled time: hit-stop eats into the window.</summary>
        Real,
        /// <summary>Scaled time: hit-stop nearly freezes the window.</summary>
        Scaled
    }

    /// <summary>How a hold action behaves (Rule 9).</summary>
    public enum HoldMode
    {
        /// <summary>Held while the button is down.</summary>
        Hold,
        /// <summary>Each press flips the held state.</summary>
        Toggle
    }

    /// <summary>Kinds of raw device signal that can switch the active device (F4).</summary>
    public enum SignalKind
    {
        /// <summary>Any button press.</summary>
        Button,
        /// <summary>Analog trigger magnitude.</summary>
        Trigger,
        /// <summary>Raw stick magnitude (before dead zone).</summary>
        Stick,
        /// <summary>Mouse button click.</summary>
        MouseClick,
        /// <summary>Mouse movement in pixels normalised to a 1080p reference height.</summary>
        MouseMove
    }

    /// <summary>One buffered button event (Rule 5).</summary>
    public struct InputRecord
    {
        /// <summary>Strictly increasing sequence number within a session.</summary>
        public long Seq;
        /// <summary>Action recorded.</summary>
        public InputActionId Action;
        /// <summary>Base action of a chord record (Attack for Plunge, Jump for DropThrough), otherwise None.</summary>
        public InputActionId BaseAction;
        /// <summary>Pressed or Released.</summary>
        public InputPhase Phase;
        /// <summary>Real time when processed (ticks).</summary>
        public long TReal;
        /// <summary>Scaled time when processed (ticks).</summary>
        public long TScaled;
        /// <summary>Device family that produced the event.</summary>
        public DeviceKind Device;
        /// <summary>Barrier epoch at record time.</summary>
        public long Epoch;
        /// <summary>Move after dead zone at press time (intent snapshot).</summary>
        public Vec2 Move;
        /// <summary>Whether a consumer has taken this record.</summary>
        public bool Consumed;
    }

    /// <summary>Token for a pushed input mode (Rule 11). Carries a generation so a stale handle never pops a reused slot.</summary>
    public readonly struct ModeHandle
    {
        internal readonly InputModeStack Stack;
        internal readonly int Slot;
        internal readonly int Generation;

        internal ModeHandle(InputModeStack stack, int slot, int generation)
        {
            Stack = stack;
            Slot = slot;
            Generation = generation;
        }

        /// <summary>False for <c>default</c> or a refused push.</summary>
        public bool IsValid => Stack != null;

        /// <summary>True when both handles refer to the same pushed entry.</summary>
        public bool SameAs(ModeHandle other) => Stack == other.Stack && Slot == other.Slot && Generation == other.Generation;
    }
}

namespace Evermeadow.Core.CharacterController
{
    /// <summary>Controller states (GDD Character Controller › States and Transitions).</summary>
    public enum CharacterState
    {
        /// <summary>Idle / Run on walkable ground.</summary>
        Grounded,
        /// <summary>Rise / Fall.</summary>
        Airborne,
        /// <summary>Dodge on ground or in the air.</summary>
        Dodge,
        /// <summary>Knocked back by an enemy (Combat).</summary>
        Hitstun,
        /// <summary>Held by a control lock (cutscene, menu, a skill that is running).</summary>
        ControlLocked,
        /// <summary>Out of HP (Defeat &amp; Retry).</summary>
        Defeated
    }

    /// <summary>Kind of jump (event <c>Jumped</c>).</summary>
    public enum JumpKind
    {
        /// <summary>From the ground.</summary>
        Ground,
        /// <summary>Within coyote time.</summary>
        Coyote,
        /// <summary>Double jump.</summary>
        Air
    }

    /// <summary>Strength of a control lock (Rule 24). The strongest active lock applies.</summary>
    public enum ControlLockLevel
    {
        /// <summary>No lock.</summary>
        None,
        /// <summary>No movement, jump or dodge (held by the skill runtime while a skill runs).</summary>
        MoveOnly,
        /// <summary>Nothing.</summary>
        Full
    }

    /// <summary>Token of a control lock; only it can release the lock.</summary>
    public readonly struct ControlLockHandle
    {
        internal readonly PlayerController Owner;
        internal readonly int Slot;
        internal readonly int Generation;

        internal ControlLockHandle(PlayerController owner, int slot, int generation)
        {
            Owner = owner;
            Slot = slot;
            Generation = generation;
        }

        /// <summary>False for default or a refused request.</summary>
        public bool IsValid => Owner != null;
    }

    /// <summary>Token of a movement modifier (Rule 25).</summary>
    public readonly struct MovementModifierHandle
    {
        internal readonly PlayerController Owner;
        internal readonly int Slot;
        internal readonly int Generation;

        internal MovementModifierHandle(PlayerController owner, int slot, int generation)
        {
            Owner = owner;
            Slot = slot;
            Generation = generation;
        }

        /// <summary>False for default or a refused request.</summary>
        public bool IsValid => Owner != null;
    }
}

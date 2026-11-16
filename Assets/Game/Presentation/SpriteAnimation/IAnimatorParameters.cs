namespace Evermeadow.Presentation.SpriteAnimation
{
    /// <summary>
    /// Write side of an Animator's parameters, by name. Drivers (player controller driver, NPC schedules, creature AI) set
    /// parameters; transitions in the Animator Controller pick the clip. Engine-free so drivers are unit-testable; implemented by
    /// the Unity <c>AnimatedSprite</c>.
    /// </summary>
    public interface IAnimatorParameters
    {
        /// <summary>Sets float parameter <paramref name="name"/>.</summary>
        void SetFloat(string name, float value);

        /// <summary>Sets bool parameter <paramref name="name"/>.</summary>
        void SetBool(string name, bool value);

        /// <summary>Sets int parameter <paramref name="name"/>.</summary>
        void SetInteger(string name, int value);

        /// <summary>Sets trigger <paramref name="name"/> (consumed by the next transition that uses it).</summary>
        void SetTrigger(string name);

        /// <summary>Clears trigger <paramref name="name"/>.</summary>
        void ResetTrigger(string name);
    }
}

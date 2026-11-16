using System;

namespace Evermeadow.Foundation
{
    /// <summary>
    /// Debug switches of <c>Evermeadow ▸ Project Settings</c> (user request 2026-11-14). The Editor settings page writes them
    /// (stored in <c>ProjectSettings/EvermeadowDevOptions.json</c>, shared by the team) and raises <see cref="Changed"/>, so a
    /// switch flipped during Play Mode applies at once. Dev aids only: gameplay rules never read them, and in a player build
    /// they keep their defaults. A static holder on purpose — dev flags read from many places, never part of game state.
    /// Main thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// if (DevOptions.ShowHitArea) areas.ShowArc(center, facing, angle, radius, seconds, colour);
    /// DevOptions.Changed += () =&gt; tester.enabled = DevOptions.ShowMovesetTester;
    /// </code>
    /// </example>
    public static class DevOptions
    {
        /// <summary>Draw every Hit node's area (green) for its active time.</summary>
        public static bool ShowHitArea;

        /// <summary>Draw every Search node's area (yellow).</summary>
        public static bool ShowSearchArea;

        /// <summary>Log each skill node event (start / end / skipped and why) to the Console.</summary>
        public static bool LogSkillTrace;

        /// <summary>Log every behaviour tree branch change of creatures to the Console.</summary>
        public static bool LogAiDecisions;

        /// <summary>Show the Moveset Tester panel (F9) in Play Mode.</summary>
        public static bool ShowMovesetTester = true;

        /// <summary>Raised after the settings page changed a switch.</summary>
        public static event Action Changed;

        /// <summary>Tells listeners that switches changed (called by the settings page).</summary>
        public static void NotifyChanged() => Changed?.Invoke();
    }
}

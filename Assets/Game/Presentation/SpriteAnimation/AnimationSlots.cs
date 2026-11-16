using System;
using System.Collections.Generic;

namespace Evermeadow.Presentation.SpriteAnimation
{
    /// <summary>Parameters of NPC / creature Animator Controllers (shared by the builder and the creature runtime).</summary>
    public static class CreatureAnimParams
    {
        /// <summary>float: |horizontal velocity|, u/s. Above 0.01 = walk clip.</summary>
        public const string Speed = "Speed";
        /// <summary>int: 0 = locomotion, 1 = hurt, 2 = die (creatures; NPCs number their actions in list order).</summary>
        public const string Action = "Action";
    }

    /// <summary>
    /// The one set of animation names every rig uses, player and creatures alike (user decision 2026-11-14), so a skill's
    /// Animation node works on whoever casts it. Two kinds:
    /// <list type="bullet">
    /// <item><b>Automatic</b> (<see cref="Automatic"/>): played by the character's state machine from its state — idle1, walk,
    /// run, jump_rise / jump_apex / jump_fall, land, bonk (locomotion) and dodge, hurt, die (reactions).</item>
    /// <item><b>Skill slots</b>: everything else (idle2, attack1…, skill1…). Only skills play them (Skill Tool Animation node);
    /// the moveset system starts the player's skills, a behaviour tree starts a creature's.</item>
    /// </list>
    /// The list itself is data (<see cref="FilePath"/>, one slot per line, <c>#</c> comment); <see cref="Defaults"/> applies
    /// while the file does not exist. A rig provides a clip per slot it supports, named exactly like the slot.
    /// </summary>
    /// <example>
    /// <code>
    /// List&lt;string&gt; slots = AnimationSlots.Parse(File.ReadAllText(path), errors);
    /// bool byStateMachine = AnimationSlots.IsAutomatic("walk");     // true
    /// </code>
    /// </example>
    public static class AnimationSlots
    {
        /// <summary>Project path of the slot list.</summary>
        public const string FilePath = "Assets/StreamingAssets/Animation/animation_slots.txt";

        /// <summary>Standing still (the state machine's idle).</summary>
        public const string Idle = "idle1";
        /// <summary>Walking.</summary>
        public const string Walk = "walk";
        /// <summary>Running.</summary>
        public const string Run = "run";
        /// <summary>Rising after a jump.</summary>
        public const string JumpRise = "jump_rise";
        /// <summary>Top of the jump.</summary>
        public const string JumpApex = "jump_apex";
        /// <summary>Falling.</summary>
        public const string JumpFall = "jump_fall";
        /// <summary>Hard landing.</summary>
        public const string Land = "land";
        /// <summary>Head hit a ceiling.</summary>
        public const string Bonk = "bonk";
        /// <summary>Dodge.</summary>
        public const string Dodge = "dodge";
        /// <summary>Hit by an enemy (hitstun).</summary>
        public const string Hurt = "hurt";
        /// <summary>Defeated.</summary>
        public const string Die = "die";

        /// <summary>Slots the state machine plays by itself (locomotion and reactions); skills never need them.</summary>
        public static readonly string[] Automatic = { Idle, Walk, Run, JumpRise, JumpApex, JumpFall, Land, Bonk, Dodge, Hurt, Die };

        /// <summary>The list used while <see cref="FilePath"/> does not exist.</summary>
        public static readonly string[] Defaults = BuildDefaults();

        /// <summary>Content of a new slot list file.</summary>
        public static string DefaultFileText =>
            "# Animation slots shared by every rig (player, creatures). One per line, [a-z0-9_], '#' = comment.\n" +
            "# Automatic (state machine): " + string.Join(", ", Automatic) + ".\n" +
            "# Every other slot is played only by skills (Skill Tool > Animation node).\n" +
            string.Join("\n", Defaults) + "\n";

        /// <summary>True for a slot the state machine plays by itself.</summary>
        public static bool IsAutomatic(string slot) => Array.IndexOf(Automatic, slot) >= 0;

        /// <summary>The slots of <paramref name="all"/> that only skills play, in list order.</summary>
        public static List<string> SkillSlots(IEnumerable<string> all)
        {
            var list = new List<string>();
            foreach (string s in all)
                if (!IsAutomatic(s)) list.Add(s);
            return list;
        }

        /// <summary>
        /// Parses the list: trims, skips blanks and comments, reports bad names and repeats (kept once). Automatic slots missing
        /// from the file are added (the state machine always needs them).
        /// </summary>
        public static List<string> Parse(string text, List<string> errors)
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int hash = line.IndexOf('#');
                if (hash >= 0) line = line.Substring(0, hash);
                line = line.Trim();
                if (line.Length == 0) continue;
                if (!IsName(line)) errors?.Add($"line {i + 1}: '{line}' must be [a-z0-9_]+");
                else if (!seen.Add(line)) errors?.Add($"line {i + 1}: '{line}' is listed twice");
                else list.Add(line);
            }
            foreach (string a in Automatic)
                if (seen.Add(a)) list.Insert(Math.Min(Array.IndexOf(Automatic, a), list.Count), a);
            return list;
        }

        /// <summary>True for <c>[a-z0-9_]+</c>.</summary>
        public static bool IsName(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) return false;
            return true;
        }

        /// <summary>Default loop setting of a new clip for <paramref name="slot"/>.</summary>
        public static bool LoopsByDefault(string slot) =>
            slot.StartsWith("idle", StringComparison.Ordinal) || slot == Walk || slot == Run || slot == JumpRise || slot == JumpApex || slot == JumpFall;

        /// <summary>Default frame length of a new clip in 60 Hz ticks (art bible §8.7).</summary>
        public static int DefaultTicks(string slot)
        {
            if (slot.StartsWith("idle", StringComparison.Ordinal) || slot == Walk) return 6;
            if (slot == Run) return 5;
            if (slot.StartsWith("attack", StringComparison.Ordinal) || slot.StartsWith("skill", StringComparison.Ordinal)) return 3;
            return 4;
        }

        private static string[] BuildDefaults()
        {
            var list = new List<string> { Idle, "idle2", "idle3", Walk, Run, JumpRise, JumpApex, JumpFall, Land, Bonk, Dodge, Hurt, Die };
            for (int i = 1; i <= 12; i++) list.Add("attack" + i);
            for (int i = 1; i <= 8; i++) list.Add("skill" + i);
            return list.ToArray();
        }
    }
}

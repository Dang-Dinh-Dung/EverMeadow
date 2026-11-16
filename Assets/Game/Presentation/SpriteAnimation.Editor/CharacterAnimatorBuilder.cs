using System.Collections.Generic;
using Evermeadow.Presentation.CharacterAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Evermeadow.Presentation.SpriteAnimation.Editor
{
    /// <summary>
    /// Builds the standard Animator Controllers: states, parameters and transitions, one clip per state. Pixel art never blends:
    /// every transition has zero duration and no exit time unless it ends a one-shot. The result is an ordinary Animator Controller
    /// to read and tune in the Animator window.
    /// </summary>
    public static class CharacterAnimatorBuilder
    {
        /// <summary>Parameters shared by NPC and creature controllers.</summary>
        public static class SimpleParams
        {
            /// <summary>float: |horizontal velocity|, u/s. Above 0.01 = move clip.</summary>
            public const string Speed = CreatureAnimParams.Speed;
            /// <summary>int: 0 = locomotion, n = the n-th action state (1-based, in the order they were built).</summary>
            public const string Action = CreatureAnimParams.Action;
        }

        private const float Moving = CharacterAnimDirector.MovingSpeed;

        /// <summary>An empty sprite clip at <paramref name="fps"/> frames per second.</summary>
        public static AnimationClip NewClip(string name, bool loop, float fps)
        {
            var clip = new AnimationClip { name = name, frameRate = fps };
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        /// <summary>
        /// Player controller: only automatic slots are wired (user decision 2026-11-14). Locomotion (idle1, walk, run,
        /// jump_rise, jump_apex, jump_fall) switches on Grounded / Speed / Running / VelocityY with ±<paramref name="apexThreshold"/>
        /// separating rise, apex and fall; land and bonk are one-frame overlays; dodge, hurt and die come from Any State on
        /// Action = 1, 2, 3 and leave through Exit. Each of <paramref name="skillSlots"/> that has a clip becomes a state with
        /// no transitions: skills play those clips over the Animator, and keeping them in the controller lets looks override them.
        /// </summary>
        public static void BuildPlayer(AnimatorController ac, IReadOnlyDictionary<string, AnimationClip> clips, float apexThreshold,
            IEnumerable<string> skillSlots = null)
        {
            ac.AddParameter(PlayerAnimParams.Speed, AnimatorControllerParameterType.Float);
            ac.AddParameter(PlayerAnimParams.Running, AnimatorControllerParameterType.Bool);
            AddBool(ac, PlayerAnimParams.Grounded, true);
            ac.AddParameter(PlayerAnimParams.VelocityY, AnimatorControllerParameterType.Float);
            ac.AddParameter(PlayerAnimParams.Action, AnimatorControllerParameterType.Int);
            ac.AddParameter(PlayerAnimParams.Land, AnimatorControllerParameterType.Trigger);
            ac.AddParameter(PlayerAnimParams.Bonk, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            float t = apexThreshold;
            var locomotion = new List<(AnimatorState state, Cond[] conds)>
            {
                (State(sm, clips, PlayerClips.Idle, 0, 0), new[] { Is(PlayerAnimParams.Grounded), Lt(PlayerAnimParams.Speed, Moving) }),
                (State(sm, clips, PlayerClips.Walk, 0, 1), new[] { Is(PlayerAnimParams.Grounded), Gt(PlayerAnimParams.Speed, Moving), Not(PlayerAnimParams.Running) }),
                (State(sm, clips, PlayerClips.Run, 0, 2), new[] { Is(PlayerAnimParams.Grounded), Gt(PlayerAnimParams.Speed, Moving), Is(PlayerAnimParams.Running) }),
                (State(sm, clips, PlayerClips.Rise, 1, 0), new[] { Not(PlayerAnimParams.Grounded), Gt(PlayerAnimParams.VelocityY, t) }),
                (State(sm, clips, PlayerClips.Apex, 1, 1), new[] { Not(PlayerAnimParams.Grounded), Gt(PlayerAnimParams.VelocityY, -t), Lt(PlayerAnimParams.VelocityY, t) }),
                (State(sm, clips, PlayerClips.Fall, 1, 2), new[] { Not(PlayerAnimParams.Grounded), Lt(PlayerAnimParams.VelocityY, -t) })
            };
            sm.defaultState = locomotion[0].state;
            Locomotion(sm, locomotion);

            AnimatorState land = State(sm, clips, PlayerClips.Land, 2, 0);
            AnyState(sm, land, Trig(PlayerAnimParams.Land), Eq(PlayerAnimParams.Action, 0));
            ExitAtEnd(land);
            ExitWhen(land, Gt(PlayerAnimParams.Speed, Moving));
            ExitWhen(land, Not(PlayerAnimParams.Grounded));

            AnimatorState bonk = State(sm, clips, PlayerClips.Bonk, 2, 1);
            AnyState(sm, bonk, Trig(PlayerAnimParams.Bonk), Eq(PlayerAnimParams.Action, 0));
            ExitAtEnd(bonk);
            ExitWhen(bonk, Is(PlayerAnimParams.Grounded));

            var actions = new (string clip, PlayerAction action)[]
            {
                (PlayerClips.Dodge, PlayerAction.Dodge), (PlayerClips.Hurt, PlayerAction.Hurt), (PlayerClips.Die, PlayerAction.Die)
            };
            for (int i = 0; i < actions.Length; i++)
            {
                AnimatorState s = State(sm, clips, actions[i].clip, 3, i);
                int n = (int)actions[i].action;
                AnyState(sm, s, Eq(PlayerAnimParams.Action, n));
                ExitWhen(s, Ne(PlayerAnimParams.Action, n));
            }
            SkillSlotStates(sm, clips, skillSlots, 5);
        }

        /// <summary>
        /// NPC or creature controller: <paramref name="idle"/> ↔ <paramref name="move"/> on Speed, and each of
        /// <paramref name="actions"/> entered from Any State on Action = its 1-based position, left through Exit when Action changes.
        /// Creatures: idle1 / walk with actions hurt, die; their attacks are skill slots (<paramref name="skillSlots"/>, states with
        /// no transitions) that skills started by the behaviour tree play.
        /// </summary>
        public static void BuildSimple(AnimatorController ac, IReadOnlyDictionary<string, AnimationClip> clips, string idle, string move, string[] actions,
            IEnumerable<string> skillSlots = null)
        {
            ac.AddParameter(SimpleParams.Speed, AnimatorControllerParameterType.Float);
            ac.AddParameter(SimpleParams.Action, AnimatorControllerParameterType.Int);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            var locomotion = new List<(AnimatorState, Cond[])>
            {
                (State(sm, clips, idle, 0, 0), new[] { Lt(SimpleParams.Speed, Moving) }),
                (State(sm, clips, move, 0, 1), new[] { Gt(SimpleParams.Speed, Moving) })
            };
            sm.defaultState = locomotion[0].Item1;
            Locomotion(sm, locomotion);
            for (int i = 0; i < actions.Length; i++)
            {
                AnimatorState s = State(sm, clips, actions[i], 1 + i / 3, i % 3);
                AnyState(sm, s, Eq(SimpleParams.Action, i + 1));
                ExitWhen(s, Ne(SimpleParams.Action, i + 1));
            }
            SkillSlotStates(sm, clips, skillSlots, 3);
        }

        /// <summary>Empties a controller (parameters, states, transitions) so it can be rebuilt in place, keeping its asset and GUID.</summary>
        public static void Clear(AnimatorController ac)
        {
            foreach (AnimatorControllerParameter p in ac.parameters) ac.RemoveParameter(p);
            while (ac.layers.Length > 1) ac.RemoveLayer(ac.layers.Length - 1);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            foreach (AnimatorStateTransition t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
            foreach (AnimatorTransition t in sm.entryTransitions) sm.RemoveEntryTransition(t);
            foreach (ChildAnimatorState s in sm.states) sm.RemoveState(s.state);
            foreach (ChildAnimatorStateMachine m in sm.stateMachines) sm.RemoveStateMachine(m.stateMachine);
        }

        // Skill slot clips sit in the controller as unreachable states (never entered by transitions).
        private static void SkillSlotStates(AnimatorStateMachine sm, IReadOnlyDictionary<string, AnimationClip> clips, IEnumerable<string> skillSlots, int firstRow)
        {
            if (skillSlots == null) return;
            int i = 0;
            foreach (string slot in skillSlots)
            {
                if (!clips.ContainsKey(slot) || AnimationSlots.IsAutomatic(slot)) continue;
                State(sm, clips, slot, firstRow + i / 4, i % 4);
                i++;
            }
        }

        // ---------------------------------------------------------------- helpers

        private struct Cond
        {
            public AnimatorConditionMode Mode;
            public string Param;
            public float Threshold;
        }

        private static Cond Is(string p) => new Cond { Mode = AnimatorConditionMode.If, Param = p };
        private static Cond Not(string p) => new Cond { Mode = AnimatorConditionMode.IfNot, Param = p };
        private static Cond Trig(string p) => new Cond { Mode = AnimatorConditionMode.If, Param = p };
        private static Cond Gt(string p, float v) => new Cond { Mode = AnimatorConditionMode.Greater, Param = p, Threshold = v };
        private static Cond Lt(string p, float v) => new Cond { Mode = AnimatorConditionMode.Less, Param = p, Threshold = v };
        private static Cond Eq(string p, int v) => new Cond { Mode = AnimatorConditionMode.Equals, Param = p, Threshold = v };
        private static Cond Ne(string p, int v) => new Cond { Mode = AnimatorConditionMode.NotEqual, Param = p, Threshold = v };

        private static void AddBool(AnimatorController ac, string name, bool value) =>
            ac.AddParameter(new AnimatorControllerParameter { name = name, type = AnimatorControllerParameterType.Bool, defaultBool = value });

        private static AnimatorState State(AnimatorStateMachine sm, IReadOnlyDictionary<string, AnimationClip> clips, string name, int row, int col)
        {
            AnimatorState s = sm.AddState(name, new Vector3(300f + col * 220f, row * 80f, 0f));
            s.motion = clips.TryGetValue(name, out AnimationClip c) ? c : null;
            s.writeDefaultValues = false; // an empty clip keeps the last sprite instead of blanking it
            return s;
        }

        private static void Apply(AnimatorStateTransition tr, Cond[] conds)
        {
            tr.hasExitTime = false;
            tr.duration = 0f;
            tr.offset = 0f;
            tr.hasFixedDuration = true;
            foreach (Cond c in conds) tr.AddCondition(c.Mode, c.Threshold, c.Param);
        }

        // Every locomotion state can reach every other; Entry picks the right one after an action or overlay exits.
        private static void Locomotion(AnimatorStateMachine sm, List<(AnimatorState state, Cond[] conds)> states)
        {
            foreach ((AnimatorState from, _) in states)
            {
                foreach ((AnimatorState to, Cond[] conds) in states)
                {
                    if (from == to) continue;
                    Apply(from.AddTransition(to), conds);
                }
            }
            foreach ((AnimatorState to, Cond[] conds) in states)
            {
                AnimatorTransition entry = sm.AddEntryTransition(to);
                foreach (Cond c in conds) entry.AddCondition(c.Mode, c.Threshold, c.Param);
            }
        }

        private static void AnyState(AnimatorStateMachine sm, AnimatorState to, params Cond[] conds)
        {
            AnimatorStateTransition tr = sm.AddAnyStateTransition(to);
            tr.canTransitionToSelf = false;
            Apply(tr, conds);
        }

        private static void ExitWhen(AnimatorState from, params Cond[] conds) => Apply(from.AddExitTransition(), conds);

        private static void ExitAtEnd(AnimatorState from)
        {
            AnimatorStateTransition tr = from.AddExitTransition();
            tr.hasExitTime = true;
            tr.exitTime = 1f;
            tr.duration = 0f;
            tr.hasFixedDuration = true;
        }
    }
}

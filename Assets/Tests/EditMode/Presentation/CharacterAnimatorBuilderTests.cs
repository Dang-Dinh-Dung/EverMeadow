using System.Collections.Generic;
using System.Linq;
using Evermeadow.Presentation.CharacterAnimation;
using Evermeadow.Presentation.SpriteAnimation.Editor;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>
    /// The generated Animator Controllers (in memory, no asset written): every state, parameter and transition the drivers rely on,
    /// with no blending (pixel art) and attack states on motion time.
    /// </summary>
    public class CharacterAnimatorBuilderTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private AnimatorController NewController(string[] clipNames, out Dictionary<string, AnimationClip> clips)
        {
            var ac = new AnimatorController();
            ac.AddLayer("Base Layer");
            _created.Add(ac);
            clips = new Dictionary<string, AnimationClip>();
            foreach (string n in clipNames)
            {
                AnimationClip c = CharacterAnimatorBuilder.NewClip(n, true, 10f);
                clips.Add(n, c);
                _created.Add(c);
            }
            return ac;
        }

        private static readonly string[] Skill = { "attack1", "attack2", "skill1" };

        private AnimatorStateMachine Player(out AnimatorController ac)
        {
            ac = NewController(PlayerClips.All.Concat(Skill).ToArray(), out Dictionary<string, AnimationClip> clips);
            CharacterAnimatorBuilder.BuildPlayer(ac, clips, 3.46f, Skill.Concat(new[] { "attack9" }));
            return ac.layers[0].stateMachine;
        }

        private static AnimatorState StateOf(AnimatorStateMachine sm, string name) => sm.states.Single(s => s.state.name == name).state;

        [Test]
        public void BuildPlayer_EveryClipHasItsStateAndEveryParameterExists()
        {
            AnimatorStateMachine sm = Player(out AnimatorController ac);

            CollectionAssert.AreEquivalent(PlayerClips.All.Concat(Skill), sm.states.Select(s => s.state.name), "attack9 has no clip: no state");
            foreach (ChildAnimatorState s in sm.states) Assert.AreEqual(s.state.name, s.state.motion.name, "clip assigned");
            CollectionAssert.AreEquivalent(
                new[] { PlayerAnimParams.Speed, PlayerAnimParams.Running, PlayerAnimParams.Grounded, PlayerAnimParams.VelocityY,
                        PlayerAnimParams.Action, PlayerAnimParams.Land, PlayerAnimParams.Bonk },
                ac.parameters.Select(p => p.name));
            Assert.AreEqual(PlayerClips.Idle, sm.defaultState.name);
        }

        [Test, Description("2026-11-14: skill slot states have no transitions in or out (only skills play them)")]
        public void BuildPlayer_SkillSlots_Unreachable()
        {
            AnimatorStateMachine sm = Player(out _);

            foreach (string slot in Skill)
            {
                Assert.IsEmpty(StateOf(sm, slot).transitions, slot);
                Assert.IsFalse(sm.anyStateTransitions.Any(t => t.destinationState?.name == slot), slot);
                Assert.IsFalse(sm.entryTransitions.Any(t => t.destinationState?.name == slot), slot);
                Assert.IsFalse(sm.states.SelectMany(s => s.state.transitions).Any(t => t.destinationState?.name == slot), slot);
            }
        }

        [Test, Description("Each action is entered from Any State on Action = n and left through Exit when Action changes")]
        public void BuildPlayer_Actions_AnyStateInExitOut()
        {
            AnimatorStateMachine sm = Player(out _);
            var actions = new (string clip, PlayerAction action)[]
            {
                (PlayerClips.Dodge, PlayerAction.Dodge), (PlayerClips.Hurt, PlayerAction.Hurt), (PlayerClips.Die, PlayerAction.Die)
            };

            foreach ((string clip, PlayerAction action) in actions)
            {
                AnimatorStateTransition into = sm.anyStateTransitions.Single(t => t.destinationState != null && t.destinationState.name == clip);
                AnimatorCondition c = into.conditions.Single();
                Assert.AreEqual((AnimatorConditionMode.Equals, PlayerAnimParams.Action, (float)(int)action), (c.mode, c.parameter, c.threshold), clip);
                Assert.IsFalse(into.canTransitionToSelf, clip);
                AnimatorStateTransition exit = StateOf(sm, clip).transitions.Single(t => t.isExit);
                Assert.AreEqual(AnimatorConditionMode.NotEqual, exit.conditions.Single().mode, clip);
            }
        }

        [Test, Description("Every locomotion state reaches every other; Entry can pick each of them")]
        public void BuildPlayer_Locomotion_FullyConnected()
        {
            AnimatorStateMachine sm = Player(out _);
            string[] loco = { PlayerClips.Idle, PlayerClips.Walk, PlayerClips.Run, PlayerClips.Rise, PlayerClips.Apex, PlayerClips.Fall };

            foreach (string from in loco)
            {
                var targets = StateOf(sm, from).transitions.Where(t => !t.isExit).Select(t => t.destinationState.name);
                CollectionAssert.AreEquivalent(loco.Where(n => n != from), targets, from);
            }
            CollectionAssert.AreEquivalent(loco, sm.entryTransitions.Select(t => t.destinationState.name));
        }

        [Test, Description("Walk and run are told apart by Running; rise / apex / fall by ±threshold on VelocityY")]
        public void BuildPlayer_LocomotionConditions()
        {
            AnimatorStateMachine sm = Player(out _);
            AnimatorCondition[] ToState(string to) =>
                StateOf(sm, PlayerClips.Idle == to ? PlayerClips.Walk : PlayerClips.Idle).transitions.Single(t => t.destinationState?.name == to).conditions;

            Assert.Contains(AnimatorConditionMode.IfNot, ToState(PlayerClips.Walk).Where(c => c.parameter == PlayerAnimParams.Running).Select(c => c.mode).ToList());
            Assert.Contains(AnimatorConditionMode.If, ToState(PlayerClips.Run).Where(c => c.parameter == PlayerAnimParams.Running).Select(c => c.mode).ToList());
            AnimatorCondition riseVy = ToState(PlayerClips.Rise).Single(c => c.parameter == PlayerAnimParams.VelocityY);
            AnimatorCondition fallVy = ToState(PlayerClips.Fall).Single(c => c.parameter == PlayerAnimParams.VelocityY);
            Assert.AreEqual((AnimatorConditionMode.Greater, 3.46f), (riseVy.mode, riseVy.threshold));
            Assert.AreEqual((AnimatorConditionMode.Less, -3.46f), (fallVy.mode, fallVy.threshold));
        }

        [Test, Description("Pixel art never blends: every transition is instant")]
        public void BuildPlayer_AllTransitions_ZeroDuration()
        {
            AnimatorStateMachine sm = Player(out _);

            IEnumerable<AnimatorStateTransition> all = sm.anyStateTransitions.Concat(sm.states.SelectMany(s => s.state.transitions));
            foreach (AnimatorStateTransition t in all) Assert.AreEqual(0f, t.duration, t.name);
            Assert.IsTrue(sm.states.All(s => !s.state.writeDefaultValues), "empty clips keep the last sprite");
        }

        [Test, Description("NPC: idle/walk on Speed, actions numbered from 1 in list order")]
        public void BuildSimple_Npc_ActionsNumberedFromOne()
        {
            string[] names = new[] { "idle1", "walk" }.Concat(AnimationAssetMenu.NpcActions).ToArray();
            AnimatorController ac = NewController(names, out Dictionary<string, AnimationClip> clips);

            CharacterAnimatorBuilder.BuildSimple(ac, clips, "idle1", "walk", AnimationAssetMenu.NpcActions);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            for (int i = 0; i < AnimationAssetMenu.NpcActions.Length; i++)
            {
                string action = AnimationAssetMenu.NpcActions[i];
                AnimatorCondition c = sm.anyStateTransitions.Single(t => t.destinationState.name == action).conditions.Single();
                Assert.AreEqual((float)(i + 1), c.threshold, action);
            }
            Assert.AreEqual("idle1", sm.defaultState.name);
        }

        [Test, Description("2026-11-14: creature = idle1 / walk + hurt (1), die (2); attacks are unreachable skill slot states")]
        public void BuildSimple_Creature_ReactionsAndSkillSlots()
        {
            string[] names = new[] { "idle1", "walk" }.Concat(AnimationAssetMenu.CreatureActions).Concat(new[] { "attack1" }).ToArray();
            AnimatorController ac = NewController(names, out Dictionary<string, AnimationClip> clips);

            CharacterAnimatorBuilder.BuildSimple(ac, clips, "idle1", "walk", AnimationAssetMenu.CreatureActions, new[] { "attack1", "attack2" });
            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            CollectionAssert.AreEquivalent(names, sm.states.Select(s => s.state.name));
            Assert.AreEqual(1f, sm.anyStateTransitions.Single(t => t.destinationState.name == "hurt").conditions.Single().threshold);
            Assert.AreEqual(2f, sm.anyStateTransitions.Single(t => t.destinationState.name == "die").conditions.Single().threshold);
            Assert.IsEmpty(StateOf(sm, "attack1").transitions);
        }

        [Test, Description("Rebuild in place: Clear empties the controller, building again gives the same states")]
        public void Clear_ThenBuild_SameResult()
        {
            AnimatorStateMachine sm = Player(out AnimatorController ac);
            int states = sm.states.Length, any = sm.anyStateTransitions.Length, parameters = ac.parameters.Length;

            CharacterAnimatorBuilder.Clear(ac);
            Assert.IsEmpty(ac.layers[0].stateMachine.states);
            Assert.IsEmpty(ac.parameters);
            var clips = sm.states.Length == 0 ? new Dictionary<string, AnimationClip>() : null;
            clips = new Dictionary<string, AnimationClip>();
            foreach (AnimationClip c in _created.OfType<AnimationClip>()) clips[c.name] = c;
            CharacterAnimatorBuilder.BuildPlayer(ac, clips, 3.46f, Skill);

            Assert.AreEqual(states, ac.layers[0].stateMachine.states.Length);
            Assert.AreEqual(any, ac.layers[0].stateMachine.anyStateTransitions.Length);
            Assert.AreEqual(parameters, ac.parameters.Length);
        }
    }
}

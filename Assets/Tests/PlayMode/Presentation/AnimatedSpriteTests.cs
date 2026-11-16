#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Evermeadow.Presentation.SpriteAnimation.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Evermeadow.Tests.PlayMode.Presentation
{
    /// <summary>
    /// AnimatedSprite on the real Animator, with a controller and sprite clips built in memory: parameters drive the clip, a look
    /// (override controller) swaps every sprite while keeping the state and parameters, art drawn facing left is mirrored, and
    /// Time.timeScale = 0 (pause, hit-stop floor) freezes the frame.
    /// </summary>
    public class AnimatedSpriteTests
    {
        private readonly List<Object> _created = new List<Object>();
        private Texture2D _tex;

        [SetUp]
        public void SetUp() => _tex = Track(new Texture2D(8, 8));

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _created.Add(o);
            return o;
        }

        private Sprite NewSprite(string name)
        {
            Sprite s = Track(Sprite.Create(_tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0f), 16f));
            s.name = name;
            return s;
        }

        /// <summary>A looping clip keying <paramref name="frames"/> sprites "{prefix}_{i}" at 10 fps.</summary>
        private AnimationClip Clip(string prefix, int frames)
        {
            AnimationClip clip = Track(new AnimationClip { name = prefix, frameRate = 10f });
            var keys = new ObjectReferenceKeyframe[frames + 1];
            for (int i = 0; i <= frames; i++) keys[i] = new ObjectReferenceKeyframe { time = i * 0.1f, value = NewSprite($"{prefix}_{System.Math.Min(i, frames - 1)}") };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        /// <summary>idle ↔ walk on Speed > 0.01.</summary>
        private AnimatorController Controller(AnimationClip idle, AnimationClip walk)
        {
            var ac = Track(new AnimatorController());
            ac.AddLayer("Base Layer");
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState i = sm.AddState("idle");
            i.motion = idle;
            AnimatorState w = sm.AddState("walk");
            w.motion = walk;
            sm.defaultState = i;
            AnimatorStateTransition toWalk = i.AddTransition(w);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
            AnimatorStateTransition toIdle = w.AddTransition(i);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            return ac;
        }

        private AnimatedSprite NewActor(RuntimeAnimatorController controller, params AnimatedSprite.Look[] looks)
        {
            var go = Track(new GameObject("actor"));
            go.SetActive(false);
            var actor = go.AddComponent<AnimatedSprite>();
            actor.Animator.runtimeAnimatorController = controller;
            actor.SetLooks(looks);
            go.SetActive(true);
            return actor;
        }

        [UnityTest, Description("A parameter change moves the Animator to the walk clip and its sprite")]
        public IEnumerator Parameter_DrivesTransition()
        {
            AnimatedSprite actor = NewActor(Controller(Clip("idle", 2), Clip("walk", 3)));
            yield return null;

            actor.SetFloat("Speed", 4f);
            yield return null;
            yield return null;

            StringAssert.StartsWith("walk_", actor.Renderer.sprite.name);
        }

        [UnityTest, Description("Weapon look: every clip shows the look's sprites; the state, its progress and the parameters are kept")]
        public IEnumerator SetLook_SwapsSpritesKeepsState()
        {
            AnimationClip idle = Clip("idle", 2), walk = Clip("walk", 3);
            AnimatorController ac = Controller(idle, walk);
            var spear = Track(new AnimatorOverrideController(ac));
            spear[idle] = Clip("spear_idle", 2);
            spear[walk] = Clip("spear_walk", 3);
            AnimatedSprite actor = NewActor(ac, new AnimatedSprite.Look { Key = "spear", Controller = spear });
            actor.SetFloat("Speed", 4f);
            for (int f = 0; f < 10; f++) yield return null;
            float before = actor.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            actor.SetLook("spear");
            AnimatorStateInfo after = actor.Animator.GetCurrentAnimatorStateInfo(0);
            string spearSprite = actor.Renderer.sprite.name;
            actor.SetLook(null);
            yield return null;

            Assert.IsTrue(after.IsName("walk"), "still walking");
            Assert.AreEqual(before, after.normalizedTime, 0.05f, "same progress");
            StringAssert.StartsWith("spear_walk_", spearSprite);
            Assert.AreEqual(4f, actor.Animator.GetFloat("Speed"), "parameters kept");
            StringAssert.StartsWith("walk_", actor.Renderer.sprite.name, "base sprites back");
        }

        [Test, Description("Art drawn facing left is flipped when the character faces right, not when it faces left")]
        public void ArtFacesLeft_FlipsOpposite()
        {
            AnimatedSprite actor = NewActor(Controller(Clip("idle", 2), Clip("walk", 3)));

            actor.ArtFacesLeft = true;
            actor.FacingLeft = false;
            bool flippedFacingRight = actor.Renderer.flipX;
            actor.FacingLeft = true;

            Assert.IsTrue(flippedFacingRight);
            Assert.IsFalse(actor.Renderer.flipX);
        }

        [UnityTest, Description("Time.timeScale = 0 (written by the frame driver for pause and hit-stop) freezes the animation")]
        public IEnumerator TimeScaleZero_FreezesFrame()
        {
            AnimatedSprite actor = NewActor(Controller(Clip("idle", 2), Clip("walk", 3)));
            yield return null;
            Time.timeScale = 0f;
            float t0 = actor.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            for (int f = 0; f < 10; f++) yield return null;

            Assert.AreEqual(t0, actor.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1e-6f);
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.CharacterAnimation;
using Evermeadow.Presentation.CharacterAnimation.Unity;
using Evermeadow.Presentation.SpriteAnimation.Unity;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Evermeadow.Tests.PlayMode.Presentation
{
    /// <summary>PlayerAnimationDriver wiring on the real engine.</summary>
    public class PlayerAnimationDriverTests
    {
        private readonly List<Object> _created = new List<Object>();

        private sealed class NullLog : ILogSink
        {
            public void Warn(string key, string message) { }
            public void Error(string key, string message) { }
        }

        private sealed class FixedClock : IFrameClock
        {
            public long RealNow => 0;
            public long ScaledNow => 0;
            public long RealDelta => 16_667;
            public long ScaledDelta => 16_667;
            public double Scale => 1.0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [UnityTest, Description("Regression: Actor left empty in the Inspector → the driver uses the AnimatedSprite in its children " +
                                "(it used to do nothing, silently: idle forever, no flip)")]
        public IEnumerator ActorNotAssigned_UsesChildAnimatedSprite()
        {
            var ac = new AnimatorController();
            ac.AddLayer("Base Layer");
            ac.AddParameter(PlayerAnimParams.Speed, AnimatorControllerParameterType.Float);
            ac.AddParameter(PlayerAnimParams.Action, AnimatorControllerParameterType.Int);
            _created.Add(ac);

            var player = new GameObject("Player");
            _created.Add(player);
            player.SetActive(false);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            var body = player.AddComponent<CharacterBody>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            var actor = visual.AddComponent<AnimatedSprite>();
            actor.Animator.runtimeAnimatorController = ac;
            var driver = player.AddComponent<PlayerAnimationDriver>(); // body and actor not assigned, as in a hand-built scene
            player.SetActive(true);
            var input = new InputService(new FixedClock(), new InputConfig(), new NullLog(), false);
            body.Init(new FixedClock(), input, new NullLog());

            input.BeginStep();
            input.OnAxisSample(AxisId.Move, new Vec2(-1f, 0f), DeviceKind.KeyboardMouse);
            input.EndStep();
            for (int i = 0; i < 3; i++) yield return null;

            Assert.IsNotNull(driver.Director, "driver initialised");
            Assert.AreSame(actor, driver.Actor);
            Assert.IsTrue(actor.FacingLeft, "facing follows the controller");
            Assert.Greater(actor.Animator.GetFloat(PlayerAnimParams.Speed), 0f, "parameters are written");
        }

        [UnityTest, Description("Regression: effect renderers live outside the player, so dust and dodge afterimages stay where they " +
                                "spawn (they used to be children of Player and moved with it)")]
        public IEnumerator Effects_NotChildrenOfPlayer()
        {
            var ac = new AnimatorController();
            ac.AddLayer("Base Layer");
            _created.Add(ac);
            var player = new GameObject("Player");
            _created.Add(player);
            player.SetActive(false);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            var body = player.AddComponent<CharacterBody>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.AddComponent<AnimatedSprite>().Animator.runtimeAnimatorController = ac;
            player.AddComponent<PlayerAnimationDriver>();
            player.SetActive(true);
            body.Init(new FixedClock(), new InputService(new FixedClock(), new InputConfig(), new NullLog(), false), new NullLog());

            yield return null;
            GameObject fx = GameObject.Find(PlayerAnimationDriver.EffectsRootName);
            if (fx != null) _created.Add(fx);

            Assert.AreEqual(1, player.GetComponentsInChildren<SpriteRenderer>(true).Length, "only the character's own renderer");
            Assert.IsNotNull(fx, "effects grouped under one object");
            Assert.IsNull(fx.transform.parent, "in world space");
        }
    }
}
#endif

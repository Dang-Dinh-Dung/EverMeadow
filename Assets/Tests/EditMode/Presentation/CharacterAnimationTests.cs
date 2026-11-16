using System.Collections.Generic;
using Evermeadow.Core.CharacterController;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Presentation.CharacterAnimation;
using Evermeadow.Presentation.SpriteAnimation;
using Evermeadow.Tests.EditMode.Core;
using Evermeadow.Tests.EditMode.Foundation;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>
    /// The player animation director on the real controller: the Animator parameters it writes each frame (the Animator
    /// Controller's transitions are checked in <see cref="CharacterAnimatorBuilderTests"/>).
    /// </summary>
    public class CharacterAnimationTests
    {
        /// <summary>Records the last value of every parameter and the triggers set since the last frame.</summary>
        private sealed class FakeParams : IAnimatorParameters
        {
            public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
            public readonly Dictionary<string, bool> Bools = new Dictionary<string, bool>();
            public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
            public readonly HashSet<string> Triggers = new HashSet<string>();

            public void SetFloat(string name, float value) => Floats[name] = value;
            public void SetBool(string name, bool value) => Bools[name] = value;
            public void SetInteger(string name, int value) => Ints[name] = value;
            public void SetTrigger(string name) => Triggers.Add(name);
            public void ResetTrigger(string name) => Triggers.Remove(name);

            public PlayerAction Action => (PlayerAction)Ints[PlayerAnimParams.Action];
        }

        private static (ControllerSim sim, CharacterAnimDirector dir, FakeParams p) Setup()
        {
            var sim = new ControllerSim(ControllerSim.Floor()).Spawn(0f, 0f);
            var p = new FakeParams();
            var dir = new CharacterAnimDirector(sim.Ctl, p);
            return (sim, dir, p);
        }

        private static void Frame(ControllerSim sim, CharacterAnimDirector dir, System.Action<InputService> input = null)
        {
            sim.Step(input);
            dir.Update();
        }

        private static void Move(InputService i, float x) => i.OnAxisSample(AxisId.Move, new Vec2(x, 0f), DeviceKind.KeyboardMouse);

        [Test, Description("C-1, C-13: Speed, Running and Grounded follow the controller the same frame")]
        public void Director_WalkRunStop_ParametersSameFrame()
        {
            var (sim, dir, p) = Setup();

            Frame(sim, dir, i => Move(i, 1f));
            float walkSpeed = p.Floats[PlayerAnimParams.Speed];
            bool runningWhileWalking = p.Bools[PlayerAnimParams.Running];
            Frame(sim, dir, i => i.Press(InputActionId.Run));
            float runSpeed = p.Floats[PlayerAnimParams.Speed];
            bool running = p.Bools[PlayerAnimParams.Running];
            Frame(sim, dir, i => { i.Release(InputActionId.Run); Move(i, 0f); });
            bool runningAfterRelease = p.Bools[PlayerAnimParams.Running];

            Assert.AreEqual(sim.Ctl.Config.WalkSpeed, walkSpeed, 1e-4f);
            Assert.IsFalse(runningWhileWalking);
            Assert.AreEqual(sim.Ctl.Config.MoveSpeed, runSpeed, 1e-4f);
            Assert.IsTrue(running);
            Assert.IsTrue(runningAfterRelease, "toggle: releasing Ctrl keeps running");
            Assert.AreEqual(0f, p.Floats[PlayerAnimParams.Speed]);
            Assert.IsTrue(p.Bools[PlayerAnimParams.Grounded]);
            Assert.AreEqual(PlayerAction.None, p.Action);
        }

        [Test, Description("Jump: Grounded off and VelocityY from + to −; a hard landing raises Land once, while standing")]
        public void Director_FullJump_VelocityYAndLandTrigger()
        {
            var (sim, dir, p) = Setup();
            var vys = new List<float>();
            int landTriggers = 0;
            bool airborneSeen = false;

            Frame(sim, dir, i => i.Press(InputActionId.Jump));
            for (int f = 0; f < 70; f++)
            {
                Frame(sim, dir);
                if (!p.Bools[PlayerAnimParams.Grounded]) { airborneSeen = true; vys.Add(p.Floats[PlayerAnimParams.VelocityY]); }
                if (p.Triggers.Contains(PlayerAnimParams.Land)) landTriggers++;
            }

            Assert.IsTrue(airborneSeen);
            Assert.Greater(vys[0], sim.Ctl.Config.ApexHangThreshold, "rise");
            Assert.Less(vys[vys.Count - 1], -sim.Ctl.Config.ApexHangThreshold, "fall");
            Assert.AreEqual(1, landTriggers);
            Assert.IsTrue(p.Bools[PlayerAnimParams.Grounded]);
        }

        [Test, Description("Landing while walking shows no Land overlay")]
        public void Director_LandWhileMoving_NoLandTrigger()
        {
            var (sim, dir, p) = Setup();
            int landTriggers = 0;

            Frame(sim, dir, i => { Move(i, 1f); i.Press(InputActionId.Jump); });
            for (int f = 0; f < 70; f++)
            {
                Frame(sim, dir);
                if (p.Triggers.Contains(PlayerAnimParams.Land)) landTriggers++;
            }

            Assert.AreEqual(0, landTriggers);
        }

        [Test, Description("AC-VI03: Action = Dodge and the i-frame look match the flag every frame")]
        public void Director_Dodge_ActionAndIFrameFlagSameFrame()
        {
            var (sim, dir, p) = Setup();
            bool mismatch = false;

            Frame(sim, dir, i => i.Press(InputActionId.Dodge));
            PlayerAction action = p.Action;
            for (int f = 0; f < 20; f++)
            {
                Frame(sim, dir);
                if (dir.Invulnerable != sim.Ctl.IsInvulnerable) mismatch = true;
            }

            Assert.AreEqual(PlayerAction.Dodge, action);
            Assert.IsFalse(mismatch);
            Assert.AreEqual(PlayerAction.None, p.Action, "back to locomotion");
        }


        [Test, Description("Each controller state maps to its Animator action")]
        public void ActionOf_States_MapToActions()
        {
            var (sim, _, _) = Setup();

            PlayerAction standing = CharacterAnimDirector.ActionOf(sim.Ctl);
            sim.Ctl.ApplyHit(new Vec2(-5f, 0f), 0.25);
            PlayerAction hit = CharacterAnimDirector.ActionOf(sim.Ctl);

            Assert.AreEqual(PlayerAction.None, standing);
            Assert.AreEqual(PlayerAction.Hurt, hit);
        }

        [Test, Description("Facing left mirrors the sprite")]
        public void Director_FacingLeft()
        {
            var (sim, dir, _) = Setup();

            Frame(sim, dir, i => Move(i, -1f));

            Assert.IsTrue(dir.FacingLeft);
        }
    }
}

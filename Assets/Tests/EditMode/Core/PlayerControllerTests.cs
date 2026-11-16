using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.CharacterController;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using Evermeadow.Tests.EditMode.Foundation;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Core
{
    public class PlayerControllerTests
    {
        private static readonly ControllerConfig Cfg = new ControllerConfig();
        private const float K = ControllerConfig.K; // GDD prototype numbers × K = runtime tile units (C-12)
        private static readonly Vec2 Right = new Vec2(1f, 0f);
        private static readonly Vec2 Left = new Vec2(-1f, 0f);
        private static readonly Vec2 Down = new Vec2(0f, -1f);

        private static ControllerSim OnFloor() => new ControllerSim(ControllerSim.Floor()).Spawn(0f, 0f);

        private static void Press(InputService i, InputActionId a) => i.Press(a);
        private static void Release(InputService i, InputActionId a) => i.Release(a);

        // ------------------------------------------------------------ validator

        [Test, Description("Validator: defaults pass")]
        public void Validate_Defaults_NoViolation()
        {
            ControllerConfigValidator.Validate(new ControllerConfig(), out List<string> v);

            CollectionAssert.IsEmpty(v);
        }

        [Test, Description("Validator: out-of-range knob → default, one violation each")]
        public void Validate_OutOfRange_ResetWithOneViolation()
        {
            ControllerConfig c = ControllerConfigValidator.Validate(new ControllerConfig { MoveSpeed = 100f }, out List<string> v1);
            ControllerConfigValidator.Validate(new ControllerConfig { JumpSpeed = 12.5f * K, DoubleJumpSpeed = 12.4f * K }, out List<string> v2);
            ControllerConfigValidator.Validate(new ControllerConfig { ApexHangMaxTime = 0.10f, ApexHangMult = 0.3f }, out List<string> v3);

            Assert.AreEqual(Cfg.MoveSpeed, c.MoveSpeed);
            Assert.AreEqual(1, v1.Count);
            Assert.AreEqual(0, v2.Count, "12.4 K < 12.5 K is legal");
            Assert.AreEqual(1, v3.Count, "validator 7: 0.10 × 0.7 > 0.05 s resets the pair");
        }

        // ------------------------------------------------------------ movement

        [Test, Description("C-13: walking is the default gait; each Run press toggles run, releasing changes nothing")]
        public void Gait_WalkByDefault_RunPressToggles()
        {
            ControllerSim sim = OnFloor();

            sim.Hold(Right);
            float walking = sim.Ctl.Velocity.X;
            bool runningFlagWhileWalking = sim.Ctl.IsRunning;
            sim.Step(i => Press(i, InputActionId.Run));
            float running = sim.Ctl.Velocity.X;
            sim.Step(i => Release(i, InputActionId.Run));
            sim.Run(30);
            float stillRunning = sim.Ctl.Velocity.X;
            sim.Step(i => Press(i, InputActionId.Run));

            Assert.AreEqual(Cfg.WalkSpeed, walking, 1e-4f);
            Assert.IsFalse(runningFlagWhileWalking);
            Assert.AreEqual(Cfg.MoveSpeed, running, 1e-4f, "press: run, same frame");
            Assert.AreEqual(Cfg.MoveSpeed, stillRunning, 1e-4f, "release and time: still running");
            Assert.AreEqual(Cfg.WalkSpeed, sim.Ctl.Velocity.X, 1e-4f, "second press: walk");
            Assert.IsFalse(sim.Ctl.IsRunning);
        }

        [Test, Description("C-13: the toggle survives stopping, jumping and hitstun")]
        public void Gait_Toggle_PersistsThroughStopJumpAndHit()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Run));

            sim.Hold(Vec2.Zero);
            sim.Step(i => Press(i, InputActionId.Jump));
            sim.Run(60);
            sim.Ctl.ApplyHit(new Vec2(-5f, 0f), 0.2);
            sim.Run(30);
            sim.Hold(Right);

            Assert.IsTrue(sim.Ctl.IsRunning);
            Assert.AreEqual(Cfg.MoveSpeed, sim.Ctl.Velocity.X, 1e-4f);
        }

        [Test, Description("C-13: a Run press during hitstun still toggles (the gait is a setting, not an action)")]
        public void Gait_PressDuringHitstun_Toggles()
        {
            ControllerSim sim = OnFloor();
            sim.Ctl.ApplyHit(new Vec2(-5f, 0f), 0.25);

            sim.Step(i => Press(i, InputActionId.Run));

            Assert.AreEqual(CharacterState.Hitstun, sim.Ctl.State);
            Assert.IsTrue(sim.Ctl.IsRunning);
        }

        [Test, Description("C-13: walk speed outside 3–6 u/s falls back to the default with one violation")]
        public void Validator_WalkSpeedOutOfRange_Reset()
        {
            ControllerConfig c = ControllerConfigValidator.Validate(new ControllerConfig { WalkSpeed = 0f }, out List<string> v);

            Assert.AreEqual(Cfg.WalkSpeed, c.WalkSpeed);
            Assert.AreEqual(1, v.Count);
        }

        [Test, Description("Rule 1 (C-1): horizontal speed is instant")]
        public void Run_PressAndRelease_SpeedChangesSameFrame()
        {
            ControllerSim sim = OnFloor();

            sim.Step(i => { i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse); Press(i, InputActionId.Run); });
            float running = sim.Ctl.Velocity.X;
            sim.Hold(Vec2.Zero);

            Assert.AreEqual(Cfg.MoveSpeed, running, 1e-4f);
            Assert.AreEqual(0f, sim.Ctl.Velocity.X);
            Assert.AreEqual(CharacterState.Grounded, sim.Ctl.State);
        }

        [Test, Description("Rule 23: walls cut vx")]
        public void Run_IntoWall_StopsAtWall()
        {
            ConvexWorld w = ControllerSim.Floor();
            w.AddBox(5f * K, 0f, 6f * K, 10f * K);
            var sim = new ControllerSim(w).Spawn(0f, 0f);

            sim.Step(i => { i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse); Press(i, InputActionId.Run); });
            sim.Run(120);

            Assert.LessOrEqual(sim.Ctl.Position.X, 5f * K - Cfg.ColliderWidth * 0.5f);
            Assert.Greater(sim.Ctl.Position.X, 5f * K - Cfg.ColliderWidth * 0.5f - 0.05f * K);
            Assert.IsTrue(sim.Ctl.IsGrounded);
        }

        // ------------------------------------------------------------ jump (F1, F2)

        [Test, Description("F1: full jump apex 2.727 u with apex hang")]
        public void Jump_Held_ApexMatchesF1()
        {
            ControllerSim sim = OnFloor();
            float start = sim.Feet;

            sim.Step(i => Press(i, InputActionId.Jump));
            float best = sim.Feet;
            for (int f = 0; f < 60; f++) { sim.Step(); if (sim.Feet > best) best = sim.Feet; }

            Assert.AreEqual(2.727f * K, best - start, 0.01f * K);
            Assert.Contains("Jumped:Ground", sim.Events);
        }

        [Test, Description("F1: release at once → 0.673 u")]
        public void Jump_ReleasedSameFrame_ApexMatchesF1Cut()
        {
            ControllerSim sim = OnFloor();
            float start = sim.Feet;

            sim.Step(i => { Press(i, InputActionId.Jump); Release(i, InputActionId.Jump); });
            float best = sim.Feet;
            for (int f = 0; f < 40; f++) { sim.Step(); if (sim.Feet > best) best = sim.Feet; }

            Assert.AreEqual(0.673f * K, best - start, 0.01f * K);
        }

        [Test, Description("F1: full jump air time 0.784 s (±1 frame)")]
        public void Jump_Held_AirTimeMatchesF1()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Jump));
            int frames = 1;

            while (!sim.Ctl.IsGrounded && frames < 120) { sim.Step(); frames++; }

            Assert.AreEqual(0.784, frames * ControllerSim.Frame / 1e6, 1.5 * ControllerSim.Frame / 1e6);
        }

        [Test, Description("F2: jump + double jump at the apex reach 5.056 u; one air jump per flight (C-3)")]
        public void DoubleJump_AtApex_HeightMatchesF2AndOnlyOnce()
        {
            ControllerSim sim = OnFloor();
            float start = sim.Feet;
            sim.Step(i => Press(i, InputActionId.Jump));
            while (sim.Ctl.Velocity.Y > 0f) sim.Step();

            sim.Step(i => { Release(i, InputActionId.Jump); Press(i, InputActionId.Jump); });
            float best = sim.Feet;
            for (int f = 0; f < 60; f++)
            {
                sim.Step(f == 30 ? (System.Action<InputService>)(i => { Release(i, InputActionId.Jump); Press(i, InputActionId.Jump); }) : null);
                if (sim.Feet > best) best = sim.Feet;
            }

            Assert.AreEqual(5.056f * K, best - start, 0.03f * K);
            Assert.AreEqual(1, sim.Count("Jumped:Air"), "second air jump refused");
        }

        [Test, Description("Rule 7 + F9: coyote jump after leaving a ledge keeps the air jump and reaches edge + 2.727")]
        public void Coyote_JumpShortlyAfterLedge_GroundJumpWithHeightCompensation()
        {
            var w = new ConvexWorld();
            w.AddBox(-20f * K, -1f, 0f, 0f);
            var sim = new ControllerSim(w).Spawn(-2f * K, 0f);
            sim.Step(i => i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse));
            while (sim.Ctl.IsGrounded) sim.Step();
            float edgeFeet = 0f;

            sim.Run(4); // ~0.067 s < coyote 0.10 s
            sim.Step(i => { i.OnAxisSample(AxisId.Move, Vec2.Zero, DeviceKind.KeyboardMouse); Press(i, InputActionId.Jump); });
            float best = sim.Feet;
            for (int f = 0; f < 60; f++) { sim.Step(); if (sim.Feet > best) best = sim.Feet; }

            Assert.Contains("Jumped:Coyote", sim.Events);
            Assert.AreEqual(1, sim.Ctl.AirJumpsLeft);
            Assert.AreEqual(2.727f * K + edgeFeet + Cfg.Skin, best, 0.03f * K);
        }

        [Test, Description("Rule 7: after coyote time the jump is the double jump")]
        public void Coyote_Expired_JumpIsAirJump()
        {
            var w = new ConvexWorld();
            w.AddBox(-20f * K, -1f, 0f, 0f);
            var sim = new ControllerSim(w).Spawn(-2f * K, 0f);
            sim.Step(i => i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse));
            while (sim.Ctl.IsGrounded) sim.Step();

            sim.Run(8); // ~0.133 s > 0.10 s
            sim.Step(i => Press(i, InputActionId.Jump));

            Assert.Contains("Jumped:Air", sim.Events);
            Assert.AreEqual(0, sim.Ctl.AirJumpsLeft);
        }

        [Test, Description("Rule 23: ceiling bonk sets vy to 0 without sticking")]
        public void Jump_UnderLowCeiling_Bonks()
        {
            ConvexWorld w = ControllerSim.Floor();
            w.AddBox(-3f * K, 3.5f * K, 3f * K, 5f * K);
            var sim = new ControllerSim(w).Spawn(0f, 0f);

            sim.Step(i => Press(i, InputActionId.Jump));
            sim.Run(60);

            Assert.AreEqual(1, sim.Count("Bonked"));
            Assert.IsTrue(sim.Ctl.IsGrounded, "fell back down");
        }

        [Test, Description("Rule 23: corner correction slides past a ceiling edge")]
        public void Jump_HeadClipsCeilingEdge_CornerCorrected()
        {
            ConvexWorld w = ControllerSim.Floor();
            w.AddBox(0.4f * K, 3.0f * K, 5f * K, 4f * K); // its left edge overlaps the head by 0.05 × K u
            var sim = new ControllerSim(w).Spawn(0f, 0f);

            sim.Step(i => Press(i, InputActionId.Jump));
            float best = sim.Feet;
            for (int f = 0; f < 40; f++) { sim.Step(); if (sim.Feet > best) best = sim.Feet; }

            Assert.AreEqual(0, sim.Count("Bonked"));
            Assert.Greater(best, 3.0f * K - Cfg.ColliderHeight + 0.2f * K, "went past the ceiling edge");
        }

        // ------------------------------------------------------------ dodge (F4)

        [Test, Description("Rule 9, F4: ground dodge 2.8 u, i-frames for the whole dodge")]
        public void Dodge_OnGround_DistanceAndIFrames()
        {
            ControllerSim sim = OnFloor();
            float x0 = sim.Ctl.Position.X;

            sim.Step(i => Press(i, InputActionId.Dodge));
            bool invulnerable = sim.Ctl.IsInvulnerable;
            sim.Run(15);

            Assert.IsTrue(invulnerable);
            Assert.IsFalse(sim.Ctl.IsInvulnerable);
            Assert.AreEqual(Cfg.DodgeSpeed * Cfg.DodgeDuration, sim.Ctl.Position.X - x0, 0.25f * K);
            Assert.Contains("DodgeGround", sim.Events);
        }

        [Test, Description("Rule 11 (C-7): cooldown counts from the end of the dodge")]
        public void Dodge_DuringCooldown_RefusedThenAllowed()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Dodge));
            sim.Run(13); // dodge ends at 0.20 s

            sim.Step(i => { Release(i, InputActionId.Dodge); Press(i, InputActionId.Dodge); });
            int started = sim.Count("DodgeGround");
            sim.Run(30); // cooldown 0.35 s; the buffered press (0.15 s) expires first
            sim.Step(i => { Release(i, InputActionId.Dodge); Press(i, InputActionId.Dodge); });

            Assert.AreEqual(1, started);
            Assert.AreEqual(2, sim.Count("DodgeGround"));
        }

        [Test, Description("Rule 10, C-3: one air dodge per flight, restored on landing")]
        public void AirDodge_OncePerFlight_RestoredOnLanding()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Jump));
            sim.Run(5);

            sim.Step(i => Press(i, InputActionId.Dodge));
            int afterFirst = sim.Ctl.AirDodgesLeft;
            sim.Run(40);

            Assert.AreEqual(0, afterFirst);
            Assert.IsTrue(sim.Ctl.IsGrounded);
            Assert.AreEqual(1, sim.Ctl.AirDodgesLeft);
            Assert.AreEqual(1, sim.Ctl.AirJumpsLeft);
        }

        [Test, Description("Rule 15: Jump then Dodge in one frame → jump this step, air dodge next step")]
        public void JumpThenDodge_SameFrame_JumpFirstThenAirDodge()
        {
            ControllerSim sim = OnFloor();

            sim.Step(i => { Press(i, InputActionId.Jump); Press(i, InputActionId.Dodge); });
            string first = sim.Events.Find(e => e.StartsWith("Jumped") || e.StartsWith("Dodge"));
            sim.Step();

            Assert.AreEqual("Jumped:Ground", first);
            Assert.Contains("DodgeAir", sim.Events);
        }

        [Test, Description("Rule 25 (C-10): root cuts a running dodge and its i-frames")]
        public void Root_DuringDodge_EndsDodgeAndIFrames()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Dodge));
            sim.Run(2);

            MovementModifierHandle h = sim.Ctl.AddMovementModifier(new object(), () => true, 1f, 1f, root: true);

            Assert.IsTrue(h.IsValid);
            Assert.AreNotEqual(CharacterState.Dodge, sim.Ctl.State);
            Assert.IsFalse(sim.Ctl.IsInvulnerable);
            sim.Hold(Right);
            Assert.AreEqual(0f, sim.Ctl.Velocity.X, "rooted: no movement");
        }

        [Test, Description("Rule 24 (C-11): a lock waits for the dodge to finish")]
        public void ControlLock_DuringDodge_AppliedAfterDodge()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Dodge));

            ControlLockHandle h = sim.Ctl.RequestControlLock(new object(), () => true, ControlLockLevel.Full);
            sim.Step();
            CharacterState during = sim.Ctl.State;
            sim.Run(15);

            Assert.AreEqual(CharacterState.Dodge, during);
            Assert.AreEqual(CharacterState.ControlLocked, sim.Ctl.State);
            Assert.AreEqual(1, sim.Count("LockApplied"));
            Assert.IsTrue(sim.Ctl.ReleaseControlLock(h));
            sim.Step();
            Assert.AreEqual(CharacterState.Grounded, sim.Ctl.State);
        }

        // ------------------------------------------------------------ attacks
        // Attacks left the Controller on 2026-11-14 (they are skills now); see MovesetTests / SkillRunTests.

        [Test, Description("2026-11-14: the Controller ignores Attack and Plunge presses (attacks are skills)")]
        public void AttackAndPlungePresses_ChangeNothing()
        {
            var sim = new ControllerSim(ControllerSim.Floor());
            sim.Spawn(0f, 0f);
            sim.Step();
            CharacterState before = sim.Ctl.State;
            Vec2 at = sim.Ctl.Position;

            Press(sim.Input, InputActionId.Attack);
            sim.Step();
            Press(sim.Input, InputActionId.Plunge);
            sim.Step();

            Assert.AreEqual(before, sim.Ctl.State);
            Assert.AreEqual(at, sim.Ctl.Position);
        }










        // ------------------------------------------------------------ one-way platforms (Rule 18, 22)

        [Test, Description("Rule 22: jump up through a one-way platform and land on it")]
        public void OneWay_JumpFromBelow_PassesAndLandsOnTop()
        {
            ConvexWorld w = ControllerSim.Floor();
            w.AddBox(-3f * K, 1.6f * K, 3f * K, 1.8f * K, oneWay: true);
            var sim = new ControllerSim(w).Spawn(0f, 0f);

            sim.Step(i => Press(i, InputActionId.Jump));
            sim.Run(60);

            Assert.IsTrue(sim.Ctl.IsGrounded);
            Assert.AreEqual(1.8f * K + Cfg.Skin, sim.Feet, 0.02f * K);
        }

        [Test, Description("Rule 18: DropThrough on a one-way falls through; on solid ground it is a jump")]
        public void DropThrough_OneWayFallsSolidJumps()
        {
            ConvexWorld w = ControllerSim.Floor();
            w.AddBox(-3f * K, 1.6f * K, 3f * K, 1.8f * K, oneWay: true);
            var sim = new ControllerSim(w).Spawn(0f, 1.8f * K);
            Assert.IsTrue(sim.Ctl.IsGrounded, "spawned on the platform");

            sim.Step(i => { i.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse); Press(i, InputActionId.Jump); });
            sim.Run(60, i => i.OnAxisSample(AxisId.Move, Vec2.Zero, DeviceKind.KeyboardMouse));
            float afterDrop = sim.Feet;
            sim.Step(i => { Release(i, InputActionId.Jump); i.OnAxisSample(AxisId.Move, Down, DeviceKind.KeyboardMouse); Press(i, InputActionId.Jump); });

            Assert.AreEqual(Cfg.Skin, afterDrop, 0.02f * K, "landed on the floor below");
            Assert.AreEqual(1, sim.Count("Jumped:Ground"), "DropThrough on solid ground became a jump");
        }

        // ------------------------------------------------------------ slopes (Rule 21, F6)

        [Test, Description("Rule 21, F6: running down a 45° slope stays grounded")]
        public void Slope_RunDown45_StaysGrounded()
        {
            var w = new ConvexWorld();
            w.AddBox(-30f * K, -1f, 10f * K, 0f);
            w.AddPolygon(false, new Vec2(10f * K, 0f), new Vec2(20f * K, 0f), new Vec2(20f * K, 10f * K));
            w.AddBox(20f * K, 0f, 40f * K, 10f * K);
            var sim = new ControllerSim(w).Spawn(30f * K, 10f * K);
            sim.Events.Clear();

            sim.Step(i => { i.OnAxisSample(AxisId.Move, Left, DeviceKind.KeyboardMouse); Press(i, InputActionId.Run); });
            sim.Run(220);

            Assert.AreEqual(0, sim.Count("LeftGround"));
            Assert.IsTrue(sim.Ctl.IsGrounded);
            Assert.Less(sim.Ctl.Position.X, 10f * K, "reached the bottom");
            Assert.AreEqual(Cfg.Skin, sim.Feet, 0.03f * K);
        }

        [Test, Description("Rule 21: horizontal speed on a slope equals flat ground; standing on it does not slide")]
        public void Slope_RunUpAndStand_SameHorizontalSpeedNoSliding()
        {
            var w = new ConvexWorld();
            w.AddBox(-30f * K, -1f, 10f * K, 0f);
            w.AddPolygon(false, new Vec2(10f * K, 0f), new Vec2(30f * K, 0f), new Vec2(30f * K, 20f * K));
            var sim = new ControllerSim(w).Spawn(9f * K, 0f);
            sim.Step(i => { i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse); Press(i, InputActionId.Run); });
            sim.Run(30);
            float x0 = sim.Ctl.Position.X;

            sim.Run(60);
            float perSecond = sim.Ctl.Position.X - x0;
            sim.Step(i => i.OnAxisSample(AxisId.Move, Vec2.Zero, DeviceKind.KeyboardMouse));
            Vec2 standing = sim.Ctl.Position;
            sim.Run(60);

            Assert.AreEqual(Cfg.MoveSpeed, perSecond, 0.1f * K);
            Assert.AreEqual(standing.X, sim.Ctl.Position.X, 1e-3f);
            Assert.AreEqual(standing.Y, sim.Ctl.Position.Y, 1e-3f);
        }

        // ------------------------------------------------------------ hitstun, time, teleport

        [Test, Description("Rule 19: hitstun consumes nothing; a dodge buffered in its last 0.15 s comes out")]
        public void Hitstun_BufferedDodge_ComesOutAfterStun()
        {
            ControllerSim sim = OnFloor();
            sim.Ctl.ApplyHit(new Vec2(-7f * K, 0f), 0.25);
            sim.Run(9); // 0.15 s

            sim.Step(i => Press(i, InputActionId.Dodge)); // 0.10 s before stun ends
            bool stunned = sim.Ctl.State == CharacterState.Hitstun;
            sim.Run(8);

            Assert.IsTrue(stunned);
            Assert.Contains("DodgeGround", sim.Events);
        }

        [Test, Description("Rule 30: a 1.5 s frame is clamped to max_frame_dt")]
        public void Step_LongFrame_ClampedToMaxFrameDt()
        {
            ControllerSim sim = OnFloor();
            sim.Hold(Right);
            float x0 = sim.Ctl.Position.X;

            sim.Step(null, Ticks.FromSeconds(1.5));

            Assert.AreEqual(Cfg.WalkSpeed * Cfg.MaxFrameDt, sim.Ctl.Position.X - x0, 1e-3f);
        }

        [Test, Description("Rule 24: Teleport restores air resources and zeroes velocity")]
        public void Teleport_InAir_RestoresResources()
        {
            ControllerSim sim = OnFloor();
            sim.Step(i => Press(i, InputActionId.Jump));
            sim.Run(3);
            sim.Step(i => { Release(i, InputActionId.Jump); Press(i, InputActionId.Jump); });

            sim.Ctl.Teleport(new Vec2(5f * K, 3f * K), -1);

            Assert.AreEqual(1, sim.Ctl.AirJumpsLeft);
            Assert.AreEqual(Vec2.Zero, sim.Ctl.Velocity);
            Assert.AreEqual(-1, sim.Ctl.Facing);
            Assert.AreEqual(CharacterState.Airborne, sim.Ctl.State);
        }

        [Test, Description("Rule 23: falling below kill_y raises FellOutOfWorld once and stops")]
        public void Fall_BelowKillY_EventOnceAndStops()
        {
            var sim = new ControllerSim(new ConvexWorld()).Spawn(0f, 0f);
            sim.Ctl.SetKillY(-5f);

            sim.Run(120);
            Vec2 stopped = sim.Ctl.Position;
            sim.Run(10);

            Assert.AreEqual(1, sim.Count("FellOut"));
            Assert.AreEqual(stopped, sim.Ctl.Position);
        }

        [Test, Description("Rule 30: depenetration on spawn inside terrain")]
        public void Spawn_InsideTerrain_PushedOut()
        {
            ConvexWorld w = ControllerSim.Floor();
            var sim = new ControllerSim(w);

            sim.Ctl = new PlayerController(new ControllerConfig(), sim.Input, w, sim.Log, new Vec2(0f, 0.3f * K));

            Assert.GreaterOrEqual(sim.Ctl.Position.Y - ControllerSim.HalfHeight, 0f);
            Assert.IsTrue(sim.Ctl.IsGrounded);
        }

        [Test, Description("Rule 30: deterministic")]
        public void SameScript_TwoRuns_IdenticalResult()
        {
            Vec2 RunScript()
            {
                ControllerSim s = OnFloor();
                s.Step(i => { i.OnAxisSample(AxisId.Move, Right, DeviceKind.KeyboardMouse); Press(i, InputActionId.Jump); });
                s.Run(20);
                s.Step(i => Press(i, InputActionId.Dodge));
                s.Run(40);
                return s.Ctl.Position;
            }

            Assert.AreEqual(RunScript(), RunScript());
        }

    }
}

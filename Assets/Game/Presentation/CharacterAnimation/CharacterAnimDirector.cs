using System;
using Evermeadow.Core.CharacterController;
using Evermeadow.Presentation.SpriteAnimation;

namespace Evermeadow.Presentation.CharacterAnimation
{
    /// <summary>
    /// State names of the player Animator Controller = the automatic animation slots (<see cref="AnimationSlots"/>, user
    /// decision 2026-11-14). Attacks are not states any more: skills play skill slots (attack1, skill1…) over the Animator.
    /// </summary>
    public static class PlayerClips
    {
        public const string Idle = AnimationSlots.Idle;
        public const string Walk = AnimationSlots.Walk;
        public const string Run = AnimationSlots.Run;
        public const string Rise = AnimationSlots.JumpRise;
        public const string Apex = AnimationSlots.JumpApex;
        public const string Fall = AnimationSlots.JumpFall;
        public const string Land = AnimationSlots.Land;
        public const string Bonk = AnimationSlots.Bonk;
        public const string Dodge = AnimationSlots.Dodge;
        public const string Hurt = AnimationSlots.Hurt;
        public const string Die = AnimationSlots.Die;

        /// <summary>The automatic player clips in the recommended row order.</summary>
        public static readonly string[] All = { Idle, Walk, Run, Rise, Apex, Fall, Land, Bonk, Dodge, Hurt, Die };
    }

    /// <summary>Value of the player Animator's <see cref="PlayerAnimParams.Action"/> parameter (reactions only).</summary>
    public enum PlayerAction
    {
        /// <summary>Moving or standing: locomotion transitions apply.</summary>
        None = 0,
        /// <summary>Dodge.</summary>
        Dodge = 1,
        /// <summary>Hitstun.</summary>
        Hurt = 2,
        /// <summary>Defeated.</summary>
        Die = 3
    }

    /// <summary>Parameter names of the player Animator Controller.</summary>
    public static class PlayerAnimParams
    {
        /// <summary>float: |horizontal velocity|, u/s.</summary>
        public const string Speed = "Speed";
        /// <summary>bool: the Run toggle is on (C-13): run clip instead of walk.</summary>
        public const string Running = "Running";
        /// <summary>bool: standing on walkable ground.</summary>
        public const string Grounded = "Grounded";
        /// <summary>float: vertical velocity, u/s (rise / apex / fall).</summary>
        public const string VelocityY = "VelocityY";
        /// <summary>int: <see cref="PlayerAction"/>; non-zero states are entered from Any State.</summary>
        public const string Action = "Action";
        /// <summary>trigger: hard landing while standing still.</summary>
        public const string Land = "Land";
        /// <summary>trigger: head hit a ceiling while airborne.</summary>
        public const string Bonk = "Bonk";
    }

    /// <summary>
    /// Feeds the player's Animator from the Controller (GDD Character Controller Rule 26: Visual reacts to state and events, it never
    /// drives gameplay). Each frame it writes <see cref="PlayerAnimParams"/>; the Animator Controller's transitions pick the clip.
    /// Land and Bonk are one-frame triggers raised by Controller events, only when the overlay makes sense (Land while standing
    /// still, Bonk while airborne and not acting). Engine-free: works on any <see cref="IAnimatorParameters"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var director = new CharacterAnimDirector(controller, animatedSprite);
    /// // each frame after the controller step, before the Animator evaluates:
    /// director.Update();
    /// animatedSprite.FacingLeft = director.FacingLeft;
    /// </code>
    /// </example>
    public sealed class CharacterAnimDirector : IDisposable
    {
        /// <summary>Fall speed (u/s) below which landing shows no squash.</summary>
        public const float SoftLandingSpeed = 6f;

        /// <summary>|vx| above which the character counts as moving.</summary>
        public const float MovingSpeed = 0.01f;

        private readonly PlayerController _ctl;
        private readonly IAnimatorParameters _params;
        private readonly Action<float> _onLanded;
        private readonly Action _onBonked;
        private bool _landPending, _bonkPending;

        /// <summary>Creates a director for <paramref name="controller"/> writing to <paramref name="parameters"/>.</summary>
        public CharacterAnimDirector(PlayerController controller, IAnimatorParameters parameters)
        {
            _ctl = controller ?? throw new ArgumentNullException(nameof(controller));
            _params = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _onLanded = speed => { if (speed >= SoftLandingSpeed) _landPending = true; };
            _onBonked = () => _bonkPending = true;
            _ctl.Landed += _onLanded;
            _ctl.Bonked += _onBonked;
            Update();
        }

        /// <summary>Mirror the sprite (facing left).</summary>
        public bool FacingLeft => _ctl.Facing < 0;

        /// <summary>Dodge i-frames active: show the i-frame look this frame (AC-VI03: 0 frames of lag).</summary>
        public bool Invulnerable => _ctl.IsInvulnerable;

        /// <summary>Writes this frame's parameters (call after the controller step, before the Animator evaluates).</summary>
        public void Update()
        {
            PlayerAction action = ActionOf(_ctl);
            float speed = Math.Abs(_ctl.Velocity.X);
            _params.SetFloat(PlayerAnimParams.Speed, speed);
            _params.SetBool(PlayerAnimParams.Running, _ctl.IsRunning);
            _params.SetBool(PlayerAnimParams.Grounded, _ctl.IsGrounded);
            _params.SetFloat(PlayerAnimParams.VelocityY, _ctl.Velocity.Y);
            _params.SetInteger(PlayerAnimParams.Action, (int)action);

            _params.ResetTrigger(PlayerAnimParams.Land);
            _params.ResetTrigger(PlayerAnimParams.Bonk);
            if (_landPending && action == PlayerAction.None && _ctl.IsGrounded && speed <= MovingSpeed) _params.SetTrigger(PlayerAnimParams.Land);
            if (_bonkPending && action == PlayerAction.None && !_ctl.IsGrounded) _params.SetTrigger(PlayerAnimParams.Bonk);
            _landPending = _bonkPending = false;
        }

        /// <summary>Unsubscribes from the controller.</summary>
        public void Dispose()
        {
            _ctl.Landed -= _onLanded;
            _ctl.Bonked -= _onBonked;
        }

        /// <summary>Animator action for the controller's state (pure; test hook).</summary>
        public static PlayerAction ActionOf(PlayerController c)
        {
            switch (c.State)
            {
                case CharacterState.Defeated: return PlayerAction.Die;
                case CharacterState.Hitstun: return PlayerAction.Hurt;
                case CharacterState.Dodge: return PlayerAction.Dodge;
                default: return PlayerAction.None;
            }
        }
    }
}

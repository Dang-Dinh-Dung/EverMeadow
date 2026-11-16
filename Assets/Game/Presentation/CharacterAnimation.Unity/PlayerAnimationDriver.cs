using Evermeadow.Core.CharacterController;
using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.SpriteAnimation.Unity;
using UnityEngine;

namespace Evermeadow.Presentation.CharacterAnimation.Unity
{
    /// <summary>
    /// Connects the player Controller to its <see cref="AnimatedSprite"/> (GDD Character Controller › Visual/Audio): Animator
    /// parameters from the controller state, facing, i-frame look in the same frame as the flag (AC-VI03), on-body air-resource pips
    /// (AC-VI04), landing dust scaled by fall speed, double-jump ring, dodge afterimages and the i-frames-ended sparkle; and the
    /// weapon look (<see cref="SetWeapon"/>: each weapon is an Animator Override Controller with its own sprites).
    /// Runs in Update after the CharacterBody (order 100), so the Animator evaluates this frame's parameters the same frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const long ClockTicksPerAnimTick = Ticks.PerSecond / 60; // effects step in 60 Hz ticks (art bible §8.7)
        private static readonly Color IFrameTint = new Color(0.70f, 0.85f, 1f, 0.55f);
        private static readonly Color PipJump = new Color(0.91f, 0.64f, 0.23f);   // scarf amber
        private static readonly Color PipDodge = new Color(0.80f, 0.93f, 0.97f);
        private static readonly Color GhostTint = new Color(0.80f, 0.90f, 1f, 0.45f);

        [SerializeField] private CharacterBody body;
        [SerializeField] private AnimatedSprite actor;
        [Header("On-body indicators (optional)")]
        [SerializeField] private SpriteRenderer pipJump;
        [SerializeField] private SpriteRenderer pipDodge;
        [Header("Effects (optional flipbooks)")]
        [SerializeField] private Sprite[] dust;
        [SerializeField] private Sprite[] airJumpRing;
        [SerializeField] private Sprite[] iframeSparkle;
        [SerializeField, Tooltip("Ticks between dodge afterimages.")] private int ghostEveryTicks = 3;

        private PlayerController _ctl;
        private CharacterAnimDirector _director;
        private SpriteFxPool _fx;
        private long _lastTime;
        private float _ghostClock;
        private bool _warned;
        private Transform _fxRoot;

        /// <summary>Name of the world-space object holding the player's effect renderers.</summary>
        public const string EffectsRootName = "Player FX";

        /// <summary>Director (test hook); null until the controller exists.</summary>
        public CharacterAnimDirector Director => _director;

        /// <summary>The animated sprite.</summary>
        public AnimatedSprite Actor => actor;

        /// <summary>Shows weapon <paramref name="weaponId"/> (a look key of the actor); null = the base (unarmed) sprites.</summary>
        public void SetWeapon(string weaponId)
        {
            if (actor == null) actor = GetComponentInChildren<AnimatedSprite>();
            actor.SetLook(weaponId);
        }

        private void Update()
        {
            if (_director == null && !TryInit()) return;

            long now = _ctl.Time;
            long dt = now - _lastTime;
            _lastTime = now;
            float animTicks = dt / (float)ClockTicksPerAnimTick;

            _director.Update();
            actor.FacingLeft = _director.FacingLeft;
            actor.Tint = _director.Invulnerable ? IFrameTint : Color.white;
            UpdatePips();

            if (_ctl.State == CharacterState.Dodge)
            {
                _ghostClock += animTicks;
                if (_ghostClock >= ghostEveryTicks)
                {
                    _ghostClock = 0f;
                    SpriteRenderer src = actor.Renderer;
                    if (src.sprite != null) _fx.Ghost(src.sprite, src.transform.position, src.flipX, GhostTint, 8f);
                }
            }
            else _ghostClock = ghostEveryTicks; // first ghost right when the next dodge starts

            _fx.Advance(animTicks);
        }

        // Empty Inspector slots fall back to the CharacterBody on this object and the AnimatedSprite below it; anything still
        // missing is reported once instead of leaving the character silently stuck in its first clip.
        private bool TryInit()
        {
            if (body == null) body = GetComponentInParent<CharacterBody>();
            if (actor == null) actor = GetComponentInChildren<AnimatedSprite>();
            if (body == null || actor == null || actor.Animator.runtimeAnimatorController == null)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning(body == null ? "[PlayerAnimationDriver] No CharacterBody on this object or its parents: animation is off." :
                        actor == null ? "[PlayerAnimationDriver] No Animated Sprite on this object or its children: animation is off." :
                        $"[PlayerAnimationDriver] The Animator on '{actor.name}' has no Controller: assign player.controller.", this);
                }
                return false;
            }
            if (body.Controller == null) return false; // the bootstrap has not built the controller yet
            _ctl = body.Controller;
            _director = new CharacterAnimDirector(_ctl, actor);
            // World-space container: dust and afterimages stay where they spawn instead of following the player.
            _fxRoot = new GameObject(EffectsRootName).transform;
            _fx = new SpriteFxPool(_fxRoot, 12, 9);
            _lastTime = _ctl.Time;
            _ctl.Landed += OnLanded;
            _ctl.Jumped += OnJumped;
            _ctl.IFrameEnded += OnIFrameEnded;
            return true;
        }

        private void OnDestroy()
        {
            if (_fxRoot != null) Destroy(_fxRoot.gameObject);
            if (_ctl == null) return;
            _ctl.Landed -= OnLanded;
            _ctl.Jumped -= OnJumped;
            _ctl.IFrameEnded -= OnIFrameEnded;
            _director?.Dispose();
        }

        private Vector3 Feet => new Vector3(_ctl.Position.X, _ctl.Position.Y - _ctl.Config.ColliderHeight * 0.5f, 0f);

        private void OnLanded(float fallSpeed)
        {
            if (fallSpeed < CharacterAnimDirector.SoftLandingSpeed) return;
            float strength = Mathf.InverseLerp(CharacterAnimDirector.SoftLandingSpeed, _ctl.Config.MaxFallSpeed, fallSpeed);
            _fx.Play(dust, Feet, 3, Color.white, Mathf.Round(Mathf.Lerp(1f, 2f, strength)));
        }

        private void OnJumped(JumpKind kind)
        {
            if (kind == JumpKind.Air) _fx.Play(airJumpRing, Feet, 3, Color.white);
        }

        private void OnIFrameEnded() =>
            _fx.Play(iframeSparkle, new Vector3(_ctl.Position.X, _ctl.Position.Y + 0.25f, 0f), 2, Color.white);

        private void UpdatePips()
        {
            bool air = !_ctl.IsGrounded && _ctl.State != CharacterState.Defeated;
            if (pipJump != null)
            {
                pipJump.enabled = air && _ctl.AirJumpsLeft > 0;
                pipJump.color = PipJump;
            }
            if (pipDodge != null)
            {
                pipDodge.enabled = air && _ctl.AirDodgesLeft > 0;
                pipDodge.color = PipDodge;
            }
        }
    }
}

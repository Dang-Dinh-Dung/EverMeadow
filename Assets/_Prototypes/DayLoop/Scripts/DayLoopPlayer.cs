// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a boss-gated chain of single-biome maps with a far second village?
// (Round 1 question, 2026-10-28: do the two halves feed each other at all? -- still measured.)
// Date: 2026-10-28
//
// Controller COPIED from CombatFeel's final round-4 tuning (not imported) and
// trimmed: ground string, air attack, dodge, dash attack. Plunge and the
// combo-chain instrumentation are cut -- combat feel is not this prototype's
// question. Added: damage numbers from the weapon/meal, multiple targets,
// player HP via DayLoopGame, input blocked while a menu is open.

using System.Collections.Generic;
using UnityEngine;

namespace DayLoopProto
{
    public class DayLoopPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6.5f;
        public float jumpForce = 13f;
        public float gravityScale = 3.2f;

        [Header("Ground string (seconds, before weapon speed multiplier)")]
        public float windup = 0.04f;
        public float active = 0.06f;
        public float recovery = 0.10f;
        public float cancelLockout = 0.03f;
        public float attackMoveMultiplier = 0.6f;
        public int maxComboHits = 3;
        public Vector2 groundHitOffset = new Vector2(0.9f, 0f);
        public Vector2 groundHitboxSize = new Vector2(1.1f, 1.1f);

        [Header("Air attack")]
        public float airWindup = 0.03f;
        public float airActive = 0.07f;
        public float airRecovery = 0.08f;
        public float airCancelLockout = 0.03f;
        public Vector2 airHitOffset = new Vector2(0.85f, 0.15f);
        public Vector2 airHitboxSize = new Vector2(1.3f, 1.3f);

        [Header("Dash attack")]
        public float dashAtkWindup = 0.02f;
        public float dashAtkActive = 0.10f;
        public float dashAtkRecovery = 0.14f;
        public float dashAtkCancelLockout = 0.06f;
        public float dashAtkCarry = 0.8f;
        public float dashAtkDecay = 6f;
        public Vector2 dashAtkHitOffset = new Vector2(1.0f, 0f);
        public Vector2 dashAtkHitboxSize = new Vector2(1.6f, 1.0f);

        [Header("Dodge")]
        public float dodgeSpeed = 14f;
        public float dodgeDuration = 0.20f;
        public float dodgeCooldown = 0.35f;

        [Header("Hitstun")]
        public float hitstunDuration = 0.25f;
        public float knockbackSpeed = 7f;

        enum State { Idle, Attacking, Dodging, Hitstun }
        enum MoveKind { Ground, Air, Dash }

        State _state = State.Idle;
        MoveKind _move;
        float _stateTimer;
        int _comboIndex;
        bool _queuedNextHit, _queuedDodge, _resolvedThisSwing;
        float _dashCarrySpeed, _dashDir;
        float _dodgeCooldownTimer;
        int _facing = 1;
        readonly HashSet<IHittable> _hitThisSwing = new HashSet<IHittable>();

        Rigidbody2D _rb;
        BoxCollider2D _col;
        SpriteRenderer _sr;
        Transform _nose;
        readonly Color _baseColor = new Color(0.25f, 0.75f, 0.95f);

        public int Facing => _facing;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _col = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = Visuals.Solid(Color.white);
            _sr.color = _baseColor;

            // Facing marker -- the player is a plain rectangle otherwise.
            _nose = Visuals.Box("Nose", Vector2.zero, new Vector2(0.3f, 0.2f), Color.white, 5, transform).transform;
        }

        public void ResetForNewDay(Vector2 position)
        {
            transform.position = position;
            _rb.linearVelocity = Vector2.zero;
            _state = State.Idle;
            _facing = 1;
            _sr.color = _baseColor;
        }

        void Update()
        {
            var game = DayLoopGame.I;
            if (game != null && game.InputBlocked)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                if (_state != State.Hitstun) _state = State.Idle;
                return;
            }

            float dt = Time.deltaTime;

            if (!(_state == State.Attacking && _move == MoveKind.Dash)) HandleFacing();

            switch (_state)
            {
                case State.Idle: TickIdle(); break;
                case State.Attacking: TickAttacking(dt); break;
                case State.Dodging: TickDodging(dt); break;
                case State.Hitstun: TickHitstun(dt); break;
            }

            if (_dodgeCooldownTimer > 0f) _dodgeCooldownTimer -= dt;
            _rb.gravityScale = _state == State.Dodging ? 0f : gravityScale;
            _nose.localPosition = new Vector3(0.45f * _facing, 0.15f, 0f);
        }

        static bool AttackPressed() => Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        static bool DodgePressed() => Input.GetKeyDown(KeyCode.LeftShift);
        static bool JumpPressed() => Input.GetKeyDown(KeyCode.Space);
        bool CanDodge() => _dodgeCooldownTimer <= 0f;

        void HandleFacing()
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > 0.01f) _facing = h > 0 ? 1 : -1;
        }

        void TickIdle()
        {
            float h = Input.GetAxisRaw("Horizontal");
            _rb.linearVelocity = new Vector2(h * moveSpeed, _rb.linearVelocity.y);

            if (IsGrounded() && JumpPressed())
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

            if (AttackPressed())
                StartMove(IsGrounded() ? MoveKind.Ground : MoveKind.Air, 0);
            else if (DodgePressed() && CanDodge())
                StartDodge();
        }

        void StartMove(MoveKind kind, int comboIndex)
        {
            _state = State.Attacking;
            _move = kind;
            _comboIndex = comboIndex;
            _stateTimer = 0f;
            _resolvedThisSwing = false;
            _queuedNextHit = false;
            _queuedDodge = false;
            _hitThisSwing.Clear();
        }

        // The forge's fast/strong branch scales every timing by one multiplier.
        void GetTiming(out float w, out float a, out float r, out float lockout)
        {
            switch (_move)
            {
                case MoveKind.Air: w = airWindup; a = airActive; r = airRecovery; lockout = airCancelLockout; break;
                case MoveKind.Dash: w = dashAtkWindup; a = dashAtkActive; r = dashAtkRecovery; lockout = dashAtkCancelLockout; break;
                default: w = windup; a = active; r = recovery; lockout = cancelLockout; break;
            }
            float m = DayLoopGame.I != null ? DayLoopGame.I.WeaponSpeedMult : 1f;
            w *= m; a *= m; r *= m; lockout *= m;
        }

        void TickAttacking(float dt)
        {
            _stateTimer += dt;
            GetTiming(out float w, out float a, out float r, out float lockout);
            float activeEnd = w + a;
            float recoveryEnd = activeEnd + r;
            float cancelStart = activeEnd + lockout;

            float h = Input.GetAxisRaw("Horizontal");
            if (_move == MoveKind.Dash)
            {
                _dashCarrySpeed = Mathf.MoveTowards(_dashCarrySpeed, 0f, dashAtkDecay * dodgeSpeed * dt);
                _rb.linearVelocity = new Vector2(_dashDir * _dashCarrySpeed, _rb.linearVelocity.y);
            }
            else
            {
                float mult = _move == MoveKind.Air ? 1f : attackMoveMultiplier;
                _rb.linearVelocity = new Vector2(h * moveSpeed * mult, _rb.linearVelocity.y);
            }

            if (IsGrounded() && JumpPressed())
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

            // Active frames: hits every target in the box once per swing.
            if (_stateTimer >= w && _stateTimer < activeEnd && !_resolvedThisSwing)
            {
                ResolveHits(CurrentHitOffset(), CurrentHitSize());
                _resolvedThisSwing = true;
            }

            if (_stateTimer >= cancelStart && _stateTimer < recoveryEnd)
            {
                if (DodgePressed() && CanDodge()) _queuedDodge = true;
                else if (!_queuedNextHit && AttackPressed()) _queuedNextHit = true;
            }

            if (_queuedDodge) { StartDodge(); return; }

            if (_stateTimer >= recoveryEnd)
            {
                if (!_queuedNextHit) { _state = State.Idle; return; }
                if (!IsGrounded()) StartMove(MoveKind.Air, 0);
                else if (_move == MoveKind.Ground && _comboIndex + 1 < maxComboHits) StartMove(MoveKind.Ground, _comboIndex + 1);
                else StartMove(MoveKind.Ground, 0);
            }
        }

        Vector2 CurrentHitOffset() =>
            _move == MoveKind.Air ? airHitOffset : _move == MoveKind.Dash ? dashAtkHitOffset : groundHitOffset;

        Vector2 CurrentHitSize() =>
            _move == MoveKind.Air ? airHitboxSize : _move == MoveKind.Dash ? dashAtkHitboxSize : groundHitboxSize;

        void ResolveHits(Vector2 offset, Vector2 size)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(offset.x * _facing, offset.y);
            int damage = DayLoopGame.I != null ? DayLoopGame.I.CurrentDamage : 1;
            bool any = false;
            foreach (var c in Physics2D.OverlapBoxAll(origin, size, 0f))
            {
                if (c == null || c == _col) continue;
                var target = c.GetComponent<IHittable>();
                if (target == null || _hitThisSwing.Contains(target)) continue;
                _hitThisSwing.Add(target);
                target.TakeHit(damage, transform.position);
                any = true;
            }
            if (any) HitStop.Trigger(this, 0.05f, 0.08f);
        }

        void StartDodge()
        {
            _state = State.Dodging;
            _stateTimer = 0f;
            _dodgeCooldownTimer = dodgeCooldown;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        }

        void TickDodging(float dt)
        {
            _stateTimer += dt;
            float dir = Input.GetAxisRaw("Horizontal");
            float dashDir = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : _facing;
            _rb.linearVelocity = new Vector2(dashDir * dodgeSpeed, 0f);

            if (AttackPressed())
            {
                _dashDir = dashDir;
                _facing = dashDir > 0 ? 1 : -1;
                _dashCarrySpeed = dodgeSpeed * dashAtkCarry;
                StartMove(MoveKind.Dash, 0);
                return;
            }

            if (_stateTimer >= dodgeDuration) _state = State.Idle;
        }

        public void ReceiveHit(int damage, Vector2 fromPosition)
        {
            if (_state == State.Dodging) return; // i-frames

            _state = State.Hitstun;
            _stateTimer = 0f;
            float dir = Mathf.Sign(transform.position.x - fromPosition.x);
            if (dir == 0f) dir = -_facing;
            _rb.linearVelocity = new Vector2(dir * knockbackSpeed, 4f);

            HitStop.Trigger(this, 0.07f, 0.05f);
            _sr.color = Color.red;
            CancelInvoke(nameof(ClearFlash));
            Invoke(nameof(ClearFlash), 0.1f);

            DayLoopGame.I?.DamagePlayer(damage);
        }

        void ClearFlash() => _sr.color = _baseColor;

        void TickHitstun(float dt)
        {
            _stateTimer += dt;
            if (_stateTimer >= hitstunDuration) _state = State.Idle;
        }

        // Round 2: a thin box under the feet instead of one centre ray -- on a
        // slope only the uphill corner touches, and the centre ray missed it.
        // Still skips our own collider and triggers (CombatFeel round-3 fix).
        bool IsGrounded()
        {
            var b = _col.bounds;
            var feet = new Vector2(b.center.x, b.min.y - 0.08f);
            foreach (var c in Physics2D.OverlapBoxAll(feet, new Vector2(b.size.x * 0.9f, 0.2f), 0f))
            {
                if (c == null || c == _col || c.isTrigger) continue;
                if (_rb.linearVelocity.y > 0.1f && c.usedByEffector) continue; // rising through a one-way platform
                return true;
            }
            return false;
        }
    }
}

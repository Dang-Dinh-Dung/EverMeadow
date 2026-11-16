// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// Hypothesis: dodge -> close distance -> 2-3 hits -> retreat, with a weapon
// paced slightly heavier than Hollow Knight but cancel-able into dodge, will
// feel agile and weighted. Measured by: the player dodges an enemy attack
// while it is actually active, and chains 3+ dodge->attack->retreat cycles
// within the first 2 minutes, unprompted.
//
// Riskiest assumption this prototype exists to test: "slightly heavier than
// Hollow Knight" is a feel that must be tuned by NUMBERS (hit-stop duration,
// cancel-window timing, move speed) below -- not something GDD prose can
// settle. Expect to change these constants between playtest passes; that is
// the point of this file.
//
// Playtest log (details in production/session-state/active.md):
// - Round 2 (2026-10-25): "heavier than HK" refuted -> base speed HK-or-faster;
//   momentum kept while swinging; hit-stop double-trigger fixed.
// - Round 2b: jump allowed mid-swing.
// - Round 3 (2026-10-25): design rule "no state locks attacking" -> three
//   distinct moves: air attack, dash attack (attack during dodge), plunge /
//   landing attack (Down + attack in the air). Hitstun is the ONE remaining
//   lockout (set hitstunDuration = 0 to remove it). Also fixed IsGrounded,
//   which hit the player's own collider (Physics2D "Queries Start In
//   Colliders" is on) and so always reported grounded.

using UnityEngine;

public class CombatFeelPlayer : MonoBehaviour
{
    // ---- Tuning knobs (the actual subject of this prototype) ----
    [Header("Movement")]
    public float moveSpeed = 6.5f;
    public float jumpForce = 13f;          // round 4: 9.5 felt low (apex ~1.4 -> ~2.7 units)
    public float gravityScale = 3.2f;

    [Header("Ground string (per-hit timing, seconds)")]
    // Round 2: ~0.28s/hit felt slower than Hollow Knight; now ~0.20s/hit.
    public float windup = 0.04f;
    public float active = 0.06f;
    public float recovery = 0.10f;
    // Within recovery: [0, cancelLockout) cannot cancel; [cancelLockout, recovery)
    // can cancel into the next attack OR a dodge.
    public float cancelLockout = 0.03f;
    // Fraction of moveSpeed kept while swinging. 0 = rooted (round 1), 1 = full.
    public float attackMoveMultiplier = 0.6f;
    public int maxComboHits = 3;
    public Vector2 groundHitOffset = new Vector2(0.9f, 0f);
    public Vector2 groundHitboxSize = new Vector2(1.0f, 1.0f);

    [Header("Air attack (attack while airborne)")]
    public float airWindup = 0.03f;
    public float airActive = 0.07f;
    public float airRecovery = 0.08f;
    public float airCancelLockout = 0.03f;
    public float airMoveMultiplier = 1f;   // full air control, HK-style
    public Vector2 airHitOffset = new Vector2(0.85f, 0.15f);
    public Vector2 airHitboxSize = new Vector2(1.3f, 1.3f);

    [Header("Dash attack (attack pressed during a dodge)")]
    public float dashAtkWindup = 0.02f;
    public float dashAtkActive = 0.10f;
    public float dashAtkRecovery = 0.14f;
    public float dashAtkCancelLockout = 0.06f;
    public float dashAtkCarry = 0.8f;      // fraction of dodgeSpeed carried into the lunge
    public float dashAtkDecay = 6f;        // how fast the carried speed bleeds off (units/s per s, as a factor)
    public bool dashAtkKeepsIframes = false; // design knob: does lunging out of a dodge stay invulnerable?
    public Vector2 dashAtkHitOffset = new Vector2(1.0f, 0f);
    public Vector2 dashAtkHitboxSize = new Vector2(1.6f, 1.0f);

    [Header("Plunge / landing attack (Down + attack while airborne)")]
    public float plungeSpeed = 18f;
    public Vector2 plungeHitOffset = new Vector2(0f, -0.7f);
    public Vector2 plungeHitboxSize = new Vector2(0.9f, 0.8f);
    public float shockwaveRadius = 1.5f;
    public float landRecovery = 0.16f;
    public float landCancelLockout = 0.08f;

    [Header("Dodge")]
    public float dodgeSpeed = 14f;         // round 4: 10 felt slow
    public float dodgeDuration = 0.20f;    // round 4: 0.22 -> 0.20 (distance 2.2 -> 2.8 units)
    public float dodgeCooldown = 0.35f;

    [Header("Hitstun (taking a hit) -- the only remaining attack lockout")]
    public float hitstunDuration = 0.25f;
    public float knockbackSpeed = 7f;

    public DummyEnemy enemy;

    enum State { Idle, Attacking, Dodging, Plunging, PlungeLanding, Hitstun }
    enum MoveKind { Ground, Air, Dash }

    State _state = State.Idle;
    MoveKind _move;
    float _stateTimer;
    int _comboIndex;          // which hit in the ground string (0-based)
    bool _queuedNextHit;
    bool _queuedDodgeFromCancel;
    bool _hasHitThisSwing;
    bool _shockwaveDone;
    float _dashCarrySpeed;
    float _dashDir;

    float _dodgeCooldownTimer;
    int _facing = 1;          // +1 right, -1 left

    Rigidbody2D _rb;
    BoxCollider2D _col;
    SpriteRenderer _sr;
    Color _baseColor = new Color(0.25f, 0.75f, 0.95f);

    // ---- Instrumentation for the hypothesis's measurable signal ----
    public int SuccessfulDodges { get; private set; }
    public int DodgeAttemptsDuringEnemyActive { get; private set; }
    public int LandedHits { get; private set; }
    public int GroundHits { get; private set; }
    public int AirHits { get; private set; }
    public int DashHits { get; private set; }
    public int PlungeHits { get; private set; }
    public int ComboChains { get; private set; }
    float _sessionStart;

    // dodge -> hit -> retreat cycle detector
    enum CycleStage { None, Dodged, DodgedThenHit }
    CycleStage _cycle = CycleStage.None;
    float _cycleDeadline;
    float _retreatTimer;
    const float CycleStepWindow = 1.5f;
    const float RetreatHoldNeeded = 0.3f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = gravityScale;
        _rb.freezeRotation = true;
        _col = GetComponent<BoxCollider2D>();
        _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = PrototypeVisuals.SolidSquare(_baseColor);
        _sessionStart = Time.time;
    }

    void Update()
    {
        float dt = Time.deltaTime; // intentionally scaled by hit-stop too -- that IS the feel being tested

        if (!FacingLocked()) HandleFacing();

        switch (_state)
        {
            case State.Idle: TickIdle(); break;
            case State.Attacking: TickAttacking(dt); break;
            case State.Dodging: TickDodging(dt); break;
            case State.Plunging: TickPlunging(); break;
            case State.PlungeLanding: TickPlungeLanding(dt); break;
            case State.Hitstun: TickHitstun(dt); break;
        }

        if (_dodgeCooldownTimer > 0f) _dodgeCooldownTimer -= dt;

        // Round 4 (playtest: dash arced instead of going straight): no gravity
        // during a dodge, so it is a flat line; falling starts when it ends.
        // Set here, after the state switch, so every exit from Dodging restores it.
        _rb.gravityScale = _state == State.Dodging ? 0f : gravityScale;

        TickCycleDetector(dt);
    }

    // ---------------- Input helpers ----------------
    static bool AttackPressed() => Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
    static bool DodgePressed() => Input.GetKeyDown(KeyCode.LeftShift);
    static bool JumpPressed() => Input.GetKeyDown(KeyCode.Space);
    static bool DownHeld() => Input.GetAxisRaw("Vertical") < -0.5f;
    bool CanDodge() => _dodgeCooldownTimer <= 0f;

    void HandleFacing()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.01f) _facing = h > 0 ? 1 : -1;
    }

    bool FacingLocked() =>
        (_state == State.Attacking && _move == MoveKind.Dash) ||
        _state == State.Plunging || _state == State.PlungeLanding;

    // Picks the right attack for the current situation -- the "no state locks
    // attacking" rule lives here.
    void StartContextAttack()
    {
        if (!IsGrounded())
        {
            if (DownHeld()) StartPlunge();
            else StartMove(MoveKind.Air, 0);
        }
        else
        {
            StartMove(MoveKind.Ground, 0);
        }
    }

    // ---------------- Idle / free movement ----------------
    void TickIdle()
    {
        float h = Input.GetAxisRaw("Horizontal");
        _rb.linearVelocity = new Vector2(h * moveSpeed, _rb.linearVelocity.y);

        if (IsGrounded() && JumpPressed())
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

        if (AttackPressed())
            StartContextAttack();
        else if (DodgePressed() && CanDodge())
            StartDodge();
    }

    // ---------------- Timed attacks (ground string, air, dash) ----------------
    void StartMove(MoveKind kind, int comboIndex)
    {
        _state = State.Attacking;
        _move = kind;
        _comboIndex = comboIndex;
        _stateTimer = 0f;
        _hasHitThisSwing = false;
        _queuedNextHit = false;
        _queuedDodgeFromCancel = false;
    }

    void GetTiming(out float w, out float a, out float r, out float lockout)
    {
        switch (_move)
        {
            case MoveKind.Air: w = airWindup; a = airActive; r = airRecovery; lockout = airCancelLockout; break;
            case MoveKind.Dash: w = dashAtkWindup; a = dashAtkActive; r = dashAtkRecovery; lockout = dashAtkCancelLockout; break;
            default: w = windup; a = active; r = recovery; lockout = cancelLockout; break;
        }
    }

    void TickAttacking(float dt)
    {
        _stateTimer += dt;
        GetTiming(out float w, out float a, out float r, out float lockout);
        float activeEnd = w + a;
        float recoveryEnd = activeEnd + r;
        float cancelStart = activeEnd + lockout;

        // Movement during the swing.
        float h = Input.GetAxisRaw("Horizontal");
        if (_move == MoveKind.Dash)
        {
            _dashCarrySpeed = Mathf.MoveTowards(_dashCarrySpeed, 0f, dashAtkDecay * dodgeSpeed * dt);
            _rb.linearVelocity = new Vector2(_dashDir * _dashCarrySpeed, _rb.linearVelocity.y);
        }
        else
        {
            float mult = _move == MoveKind.Air ? airMoveMultiplier : attackMoveMultiplier;
            _rb.linearVelocity = new Vector2(h * moveSpeed * mult, _rb.linearVelocity.y);
        }

        // Jump allowed mid-swing (round 2b).
        if (IsGrounded() && JumpPressed())
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

        // Down + attack in the air always goes to the plunge, mid-swing or not.
        if (_move != MoveKind.Dash && !IsGrounded() && DownHeld() && AttackPressed())
        {
            StartPlunge();
            return;
        }

        // Active frames: resolve the hit once per swing.
        if (_stateTimer >= w && _stateTimer < activeEnd && !_hasHitThisSwing)
        {
            if (TryHitBox(CurrentHitOffset(), CurrentHitSize())) CountHit(_move);
            _hasHitThisSwing = true;
        }

        // Cancel window: buffer a dodge or the next attack.
        if (_stateTimer >= cancelStart && _stateTimer < recoveryEnd)
        {
            if (DodgePressed() && CanDodge())
                _queuedDodgeFromCancel = true;
            else if (!_queuedNextHit && AttackPressed())
                _queuedNextHit = true;
        }

        if (_queuedDodgeFromCancel)
        {
            StartDodge();
            return;
        }

        if (_stateTimer >= recoveryEnd)
        {
            if (!_queuedNextHit) { _state = State.Idle; return; }

            if (!IsGrounded()) StartMove(MoveKind.Air, 0);
            else if (_move == MoveKind.Ground && _comboIndex + 1 < maxComboHits) StartMove(MoveKind.Ground, _comboIndex + 1);
            else StartMove(MoveKind.Ground, 0);
        }
    }

    Vector2 CurrentHitOffset()
    {
        switch (_move)
        {
            case MoveKind.Air: return airHitOffset;
            case MoveKind.Dash: return dashAtkHitOffset;
            default: return groundHitOffset;
        }
    }

    Vector2 CurrentHitSize()
    {
        switch (_move)
        {
            case MoveKind.Air: return airHitboxSize;
            case MoveKind.Dash: return dashAtkHitboxSize;
            default: return groundHitboxSize;
        }
    }

    // ---------------- Plunge / landing attack ----------------
    void StartPlunge()
    {
        _state = State.Plunging;
        _stateTimer = 0f;
        _hasHitThisSwing = false;
        _shockwaveDone = false;
    }

    void TickPlunging()
    {
        _rb.linearVelocity = new Vector2(0f, -plungeSpeed);

        // Hit on the way down.
        if (!_hasHitThisSwing && TryHitBox(plungeHitOffset, plungeHitboxSize))
        {
            CountHit(null);
            _hasHitThisSwing = true;
        }

        if (IsGrounded())
        {
            _state = State.PlungeLanding;
            _stateTimer = 0f;
            _queuedNextHit = false;
            _queuedDodgeFromCancel = false;
        }
    }

    void TickPlungeLanding(float dt)
    {
        _stateTimer += dt;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        // Landing shockwave, once, on the first frame on the ground.
        if (!_shockwaveDone)
        {
            _shockwaveDone = true;
            if (TryHitCircle(shockwaveRadius)) CountHit(null);
        }

        if (_stateTimer >= landCancelLockout)
        {
            if (DodgePressed() && CanDodge()) { StartDodge(); return; }
            if (AttackPressed()) { StartContextAttack(); return; }
            if (JumpPressed()) { _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce); _state = State.Idle; return; }
        }

        if (_stateTimer >= landRecovery)
            _state = State.Idle;
    }

    // ---------------- Hit resolution ----------------
    bool TryHitBox(Vector2 offset, Vector2 size)
    {
        if (enemy == null || enemy.IsKo) return false;
        Vector2 origin = (Vector2)transform.position + new Vector2(offset.x * _facing, offset.y);
        return OverlapsEnemy(Physics2D.OverlapBoxAll(origin, size, 0f));
    }

    bool TryHitCircle(float radius)
    {
        if (enemy == null || enemy.IsKo) return false;
        return OverlapsEnemy(Physics2D.OverlapCircleAll(transform.position, radius));
    }

    bool OverlapsEnemy(Collider2D[] hits)
    {
        foreach (var h in hits)
        {
            if (h != null && h.gameObject == enemy.gameObject)
            {
                enemy.TryReceiveHit(transform.position);
                HitStop.Trigger(this, 0.05f, 0.08f);
                return true;
            }
        }
        return false;
    }

    // kind == null means the plunge (fall hit or landing shockwave).
    void CountHit(MoveKind? kind)
    {
        LandedHits++;
        if (kind == null) PlungeHits++;
        else if (kind == MoveKind.Air) AirHits++;
        else if (kind == MoveKind.Dash) DashHits++;
        else GroundHits++;
        NotifyCycle_Hit();
    }

    // ---------------- Dodge ----------------
    void StartDodge()
    {
        _state = State.Dodging;
        _stateTimer = 0f;
        _dodgeCooldownTimer = dodgeCooldown;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);

        bool enemyWasActive = enemy != null && enemy.IsActiveWindow;
        if (enemyWasActive) DodgeAttemptsDuringEnemyActive++;
        // "Successful" = you were invulnerable while the enemy's attack was live.
        if (enemyWasActive) { SuccessfulDodges++; NotifyCycle_Dodge(); }
    }

    void TickDodging(float dt)
    {
        _stateTimer += dt;
        float dir = Input.GetAxisRaw("Horizontal");
        float dashDir = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : _facing;
        _rb.linearVelocity = new Vector2(dashDir * dodgeSpeed, 0f);

        // Attack at any point in the dodge -> dash attack (round 3).
        if (AttackPressed())
        {
            _dashDir = dashDir;
            _facing = dashDir > 0 ? 1 : -1;
            _dashCarrySpeed = dodgeSpeed * dashAtkCarry;
            StartMove(MoveKind.Dash, 0);
            return;
        }

        if (_stateTimer >= dodgeDuration)
            _state = State.Idle;
    }

    bool IsInvulnerable() =>
        _state == State.Dodging ||
        (dashAtkKeepsIframes && _state == State.Attacking && _move == MoveKind.Dash);

    // ---------------- Receiving a hit ----------------
    public void ReceiveHit(Vector2 fromPosition)
    {
        if (IsInvulnerable()) return; // already scored in StartDodge

        _state = State.Hitstun;
        _stateTimer = 0f;
        float dir = Mathf.Sign(transform.position.x - fromPosition.x);
        if (dir == 0f) dir = -_facing;
        _rb.linearVelocity = new Vector2(dir * knockbackSpeed, _rb.linearVelocity.y);

        HitStop.Trigger(this, 0.07f, 0.05f);
        FlashRed();
        ResetCycle();
    }

    void TickHitstun(float dt)
    {
        _stateTimer += dt;
        if (_stateTimer >= hitstunDuration)
            _state = State.Idle;
    }

    void FlashRed()
    {
        _sr.color = Color.red;
        CancelInvoke(nameof(ClearFlash));
        Invoke(nameof(ClearFlash), 0.08f);
    }

    void ClearFlash() => _sr.color = _baseColor;

    // ---------------- Combo-chain detector (the hypothesis's headline number) ----------------
    void NotifyCycle_Dodge()
    {
        _cycle = CycleStage.Dodged;
        _cycleDeadline = Time.time + CycleStepWindow;
    }

    void NotifyCycle_Hit()
    {
        if (_cycle == CycleStage.Dodged && Time.time <= _cycleDeadline)
        {
            _cycle = CycleStage.DodgedThenHit;
            _cycleDeadline = Time.time + CycleStepWindow;
            _retreatTimer = 0f;
        }
    }

    void TickCycleDetector(float dt)
    {
        if (_cycle == CycleStage.None) return;
        if (Time.time > _cycleDeadline) { ResetCycle(); return; }

        if (_cycle == CycleStage.DodgedThenHit)
        {
            // "Retreat" = moving away from the enemy while not mid-swing.
            bool movingAway = false;
            if (enemy != null && _state != State.Attacking)
            {
                float away = Mathf.Sign(transform.position.x - enemy.transform.position.x);
                float input = Input.GetAxisRaw("Horizontal");
                movingAway = Mathf.Abs(input) > 0.01f && Mathf.Sign(input) == away;
            }

            _retreatTimer = movingAway ? _retreatTimer + dt : 0f;
            if (_retreatTimer >= RetreatHoldNeeded)
            {
                ComboChains++;
                ResetCycle();
            }
        }
    }

    void ResetCycle()
    {
        _cycle = CycleStage.None;
        _retreatTimer = 0f;
    }

    // Round 3 fix: the old single Raycast started inside the player's own
    // collider and, with "Queries Start In Colliders" on, always hit it.
    bool IsGrounded()
    {
        float halfHeight = _col != null ? _col.bounds.extents.y : 0.65f;
        var hits = Physics2D.RaycastAll(transform.position, Vector2.down, halfHeight + 0.08f);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider == _col || h.collider.isTrigger) continue;
            return true;
        }
        return false;
    }

    public float SessionSeconds => Time.time - _sessionStart;
    public string DebugState => _state == State.Attacking ? $"Attacking/{_move} (combo {_comboIndex})" : _state.ToString();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 off = _state == State.Attacking ? CurrentHitOffset() : groundHitOffset;
        Vector2 size = _state == State.Attacking ? CurrentHitSize() : groundHitboxSize;
        Gizmos.DrawWireCube((Vector2)transform.position + new Vector2(off.x * _facing, off.y), size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}

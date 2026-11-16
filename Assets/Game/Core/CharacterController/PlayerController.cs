using System;
using System.Collections.Generic;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;

namespace Evermeadow.Core.CharacterController
{
    /// <summary>
    /// Engine-free Character Controller (GDD Character Controller). Owns the single action state machine of the player:
    /// movement, jumps, dodge, hitstun, locks. Attacks are not here any more (user decision 2026-11-14): every attack is a
    /// skill chosen by the moveset system and played by the skill runtime, which holds a <see cref="ControlLockLevel.MoveOnly"/>
    /// lock while the skill runs.
    /// The adapter calls <see cref="Step"/> once per frame with the scaled frame delta (FrameClock ticks), after the input step.
    /// Each frame is clamped to <c>max_frame_dt</c> and split into sub-steps ≤ 1/30 s; every sub-step runs
    /// time → input → integrate → contacts (Rule 30). All timers are integer ticks.
    /// </summary>
    /// <example>
    /// <code>
    /// var controller = new PlayerController(config, input, world, log, spawn);
    /// // every frame, after FrameDriver:
    /// controller.Step(clock.ScaledDelta);
    /// body.position = controller.Position;
    /// </code>
    /// </example>
    public sealed class PlayerController
    {
        private const long MaxSubStepTicks = Ticks.PerSecond / 30;
        private const int MaxHandles = 8;

        private struct LockEntry
        {
            public bool Used;
            public int Generation;
            public ControlLockLevel Level;
            public object Owner;
            public Func<bool> IsAlive;
        }

        private struct ModifierEntry
        {
            public bool Used;
            public int Generation;
            public float Speed;
            public float Jump;
            public bool Root;
            public object Owner;
            public Func<bool> IsAlive;
        }

        private enum Act
        {
            None, GroundJump, CoyoteJump, AirJump, DropThrough, Dodge
        }

        private readonly ControllerConfig _c;
        private readonly IInputQueries _input;
        private readonly ILogSink _log;
        private readonly CharacterMotor _motor;
        private readonly LockEntry[] _locks = new LockEntry[MaxHandles];
        private readonly ModifierEntry[] _mods = new ModifierEntry[MaxHandles];

        // tick constants
        private readonly long _dodgeDurationT, _dodgeCooldownT, _dropIgnoreT, _maxFrameT;
        private long _coyoteT, _jumpWinT, _dropWinT, _dodgeWinT, _runWinT;

        private float _bufferScale = 1f, _coyoteScale = 1f, _iframeScale = 1f;
        private bool _alwaysFullJump;

        private long _t;
        private Vec2 _pos;
        private float _vx, _vy;
        private int _facing = 1;
        private CharacterState _state = CharacterState.Airborne;
        private bool _running;
        private bool _grounded;
        private GroundInfo _ground;
        private int _airJumps = 1, _airDodges = 1;

        private bool _jumpRising, _hangEligible, _jumpActive, _jumpCutDone;
        private double _hangTime;
        private long _jumpSeq;
        private long _coyoteUntil = -1;
        private float _coyoteEdgeFeet;

        private long _dodgeStart, _dodgeEnd, _cooldownEnd, _iframeEnd;
        private int _dodgeDir = 1;
        private bool _dodgeFromGround, _iframeOn, _dodgeReadyPending;

        private long _hitstunEnd, _ignoreUntil;
        private float _killY = float.NegativeInfinity;
        private bool _fellOut;
        private Vec2 _lastSafeGround;

        /// <summary>Creates the controller at <paramref name="spawn"/>; config is validated here (violations logged as errors).</summary>
        public PlayerController(ControllerConfig config, IInputQueries input, ICollisionWorld world, ILogSink log, Vec2 spawn)
        {
            _log = log ?? new NullLogSink();
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _c = ControllerConfigValidator.Validate(config, out List<string> violations);
            foreach (string v in violations) _log.Error("Controller.Config", v);

            _motor = new CharacterMotor(world, new Vec2(_c.ColliderWidth, _c.ColliderHeight), _c.Skin, _c.MaxWalkAngle);
            _dodgeDurationT = Ticks.FromSeconds(_c.DodgeDuration);
            _dodgeCooldownT = Ticks.FromSeconds(_c.DodgeCooldown);
            _dropIgnoreT = Ticks.FromSeconds(_c.DropIgnoreTime);
            _maxFrameT = Ticks.FromSeconds(_c.MaxFrameDt);
            RecomputeWindows();
            RegisterWindows();
            PlaceAt(spawn);
            _state = _grounded ? CharacterState.Grounded : CharacterState.Airborne;
        }

        // ---------------------------------------------------------------- events (Rule 26)

        /// <summary>A jump started.</summary>
        public event Action<JumpKind> Jumped;
        /// <summary>Left the ground without jumping.</summary>
        public event Action LeftGround;
        /// <summary>Landed with the given fall speed.</summary>
        public event Action<float> Landed;
        /// <summary>Dodge started (direction, in the air).</summary>
        public event Action<int, bool> DodgeStarted;
        /// <summary>Dodge ended.</summary>
        public event Action DodgeEnded;
        /// <summary>Dodge cooldown elapsed.</summary>
        public event Action DodgeReady;
        /// <summary>I-frames started.</summary>
        public event Action IFrameStarted;
        /// <summary>I-frames ended.</summary>
        public event Action IFrameEnded;
        /// <summary>Hitstun started.</summary>
        public event Action HitstunStarted;
        /// <summary>Hitstun ended.</summary>
        public event Action HitstunEnded;
        /// <summary>Head hit a ceiling while rising.</summary>
        public event Action Bonked;
        /// <summary>Facing changed (+1 right, −1 left).</summary>
        public event Action<int> FacingChanged;
        /// <summary>Teleported or respawned.</summary>
        public event Action Teleported;
        /// <summary>Fell below kill_y; simulation stops until Teleport or Respawn.</summary>
        public event Action FellOutOfWorld;
        /// <summary>A pending control lock took effect.</summary>
        public event Action ControlLockApplied;
        /// <summary>State changed (old, new).</summary>
        public event Action<CharacterState, CharacterState> StateChanged;
        /// <summary>Air jumps or air dodges left changed.</summary>
        public event Action AirResourcesChanged;

        // ---------------------------------------------------------------- read side

        /// <summary>Box centre.</summary>
        public Vec2 Position => _pos;
        /// <summary>Velocity, u/s.</summary>
        public Vec2 Velocity => new Vec2(_vx, _vy);
        /// <summary>+1 right, −1 left.</summary>
        public int Facing => _facing;
        /// <summary>Current state.</summary>
        public CharacterState State => _state;
        /// <summary>Standing on walkable ground.</summary>
        public bool IsGrounded => _grounded;
        /// <summary>
        /// Run toggle is on (C-13, toggled by each Run press): horizontal speed uses MoveSpeed, otherwise WalkSpeed. Animation picks
        /// run or walk from it.
        /// </summary>
        public bool IsRunning => _running;
        /// <summary>Dodge i-frames active (Combat reads this).</summary>
        public bool IsInvulnerable => _iframeOn;
        /// <summary>Double jumps left (0 or 1).</summary>
        public int AirJumpsLeft => _airJumps;
        /// <summary>Air dodges left (0 or 1).</summary>
        public int AirDodgesLeft => _airDodges;

        /// <summary>Effective configuration after validation.</summary>
        public ControllerConfig Config => _c;
        /// <summary>Controller time (scaled ticks since creation), for tests and telemetry.</summary>
        public long Time => _t;
        /// <summary>Last position standing on solid ground.</summary>
        public Vec2 LastSafeGround => _lastSafeGround;

        // ---------------------------------------------------------------- API for other systems


        /// <summary>Accessibility hooks pushed by Settings (Rule 29).</summary>
        public void SetAccessibility(float bufferScale, float coyoteScale, bool alwaysFullJump, float iframeScale)
        {
            _bufferScale = Clamp(bufferScale, 1f, 2f);
            _coyoteScale = Clamp(coyoteScale, 1f, 2f);
            _iframeScale = Clamp(iframeScale, 1f, 1.5f);
            _alwaysFullJump = alwaysFullJump;
            RecomputeWindows();
            RegisterWindows();
        }

        /// <summary>World floor (Region Loading): below it the controller raises <see cref="FellOutOfWorld"/>.</summary>
        public void SetKillY(float killY) => _killY = killY;

        /// <summary>Hit from Combat (Rule 19): knockback velocity and stun time on the scaled clock.</summary>
        public void ApplyHit(Vec2 knockback, double stunSeconds)
        {
            if (_state == CharacterState.Defeated) return;
            CancelActions();
            _vx = knockback.X;
            _vy = knockback.Y;
            if (_vy > 0f) _grounded = false;
            _jumpRising = _vy > 0f;
            _hangEligible = false;
            _jumpActive = false;
            _coyoteUntil = -1;
            _hitstunEnd = _t + Ticks.FromSeconds(Math.Max(0.0, stunSeconds));
            SetState(CharacterState.Hitstun);
            HitstunStarted?.Invoke();
        }

        /// <summary>Teleport (Rule 24): cancels the current action, zero velocity, restores air resources.</summary>
        public void Teleport(Vec2 position, int facing)
        {
            CancelActions();
            if (facing != 0) SetFacing(facing);
            PlaceAt(position);
            if (_state != CharacterState.Hitstun && _state != CharacterState.Defeated)
                SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
            Teleported?.Invoke();
        }

        /// <summary>Respawn (Rule 24): Teleport, plus clears Hitstun and Defeated.</summary>
        public void Respawn(Vec2 position)
        {
            if (_state == CharacterState.Hitstun) HitstunEnded?.Invoke();
            _state = CharacterState.Airborne;
            Teleport(position, 0);
            SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
        }

        /// <summary>Enters Defeated from any state (Defeat &amp; Retry).</summary>
        public void EnterDefeated()
        {
            CancelActions();
            SetState(CharacterState.Defeated);
        }

        /// <summary>Requests a control lock (Rule 24). Dodge and Hitstun finish first; the skill runtime holds MoveOnly while a skill runs.</summary>
        public ControlLockHandle RequestControlLock(object owner, Func<bool> isAlive, ControlLockLevel level)
        {
            if (level == ControlLockLevel.None) return default;
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_locks[i].Used) continue;
                _locks[i] = new LockEntry { Used = true, Generation = _locks[i].Generation + 1, Level = level, Owner = owner, IsAlive = isAlive };
                return new ControlLockHandle(this, i, _locks[i].Generation);
            }
            _log.Error("Controller.LocksFull", "Too many control locks; request refused.");
            return default;
        }

        /// <summary>Releases a control lock. False for an invalid, stale or foreign handle.</summary>
        public bool ReleaseControlLock(ControlLockHandle handle)
        {
            if (handle.Owner != this || handle.Slot < 0 || handle.Slot >= MaxHandles || !_locks[handle.Slot].Used || _locks[handle.Slot].Generation != handle.Generation)
            {
                _log.Warn("Controller.BadLockHandle", "Release with an invalid, stale or foreign lock handle ignored.");
                return false;
            }
            _locks[handle.Slot].Used = false;
            _locks[handle.Slot].Owner = null;
            _locks[handle.Slot].IsAlive = null;
            return true;
        }

        /// <summary>
        /// Adds a movement modifier (Rule 25). Multipliers combine by product, then clamp (speed [0.3, 1.5], jump by height
        /// [0.5, 1.2]). Root blocks move, jump and dodge, and cuts a running dodge immediately (C-10).
        /// </summary>
        public MovementModifierHandle AddMovementModifier(object owner, Func<bool> isAlive, float speedMult, float jumpMult, bool root)
        {
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_mods[i].Used) continue;
                _mods[i] = new ModifierEntry
                {
                    Used = true, Generation = _mods[i].Generation + 1, Speed = speedMult, Jump = jumpMult, Root = root,
                    Owner = owner, IsAlive = isAlive
                };
                if (root) OnRooted();
                return new MovementModifierHandle(this, i, _mods[i].Generation);
            }
            _log.Error("Controller.ModifiersFull", "Too many movement modifiers; request refused.");
            return default;
        }

        /// <summary>Removes a movement modifier. False for an invalid, stale or foreign handle.</summary>
        public bool RemoveMovementModifier(MovementModifierHandle handle)
        {
            if (handle.Owner != this || handle.Slot < 0 || handle.Slot >= MaxHandles || !_mods[handle.Slot].Used || _mods[handle.Slot].Generation != handle.Generation)
            {
                _log.Warn("Controller.BadModifierHandle", "Remove with an invalid, stale or foreign modifier handle ignored.");
                return false;
            }
            _mods[handle.Slot].Used = false;
            _mods[handle.Slot].Owner = null;
            _mods[handle.Slot].IsAlive = null;
            return true;
        }

        // ---------------------------------------------------------------- step

        /// <summary>Advances by <paramref name="scaledDeltaTicks"/> (FrameClock.ScaledDelta). Clamped to max_frame_dt, split into sub-steps ≤ 1/30 s.</summary>
        public void Step(long scaledDeltaTicks)
        {
            if (scaledDeltaTicks <= 0) return;
            SweepDeadHandles();
            if (_fellOut) return;

            long total = Math.Min(scaledDeltaTicks, _maxFrameT);
            int n = (int)((total + MaxSubStepTicks - 1) / MaxSubStepTicks);
            long baseSub = total / n;
            long rem = total % n;
            for (int i = 0; i < n && !_fellOut; i++) SubStep(baseSub + (i < rem ? 1 : 0));
        }

        private void SubStep(long dt)
        {
            _t += dt;
            UpdateTimers();
            ToggleGait();
            ConsumeInput();
            Integrate(dt / (double)Ticks.PerSecond);
        }

        // Each Run press flips walk ↔ run (C-13), in any state: the gait is a setting, not an action, so hitstun and locks do not
        // swallow it. One record per press, so sub-steps cannot flip it twice.
        private void ToggleGait()
        {
            if (_input.TryConsume(InputActionId.Run, _runWinT, ClockKind.Scaled, out _)) _running = !_running;
        }

        // (1) time-driven transitions
        private void UpdateTimers()
        {
            if (_iframeOn && _state != CharacterState.Dodge && _t >= _iframeEnd) SetIFrames(false);

            if (_state == CharacterState.Dodge && _t >= _dodgeEnd) EndDodge();

            if (_dodgeReadyPending && _t >= _cooldownEnd)
            {
                _dodgeReadyPending = false;
                DodgeReady?.Invoke();
            }

            if (_state == CharacterState.Hitstun && _t >= _hitstunEnd)
            {
                HitstunEnded?.Invoke();
                SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
            }

            ControlLockLevel lockLevel = EffectiveLock();
            if (lockLevel != ControlLockLevel.None && CanApplyLockNow())
            {
                SetState(CharacterState.ControlLocked);
                ControlLockApplied?.Invoke();
            }
            else if (_state == CharacterState.ControlLocked && lockLevel == ControlLockLevel.None)
            {
                SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
            }

            if (_motor.IgnoredCount > 0 && _t >= _ignoreUntil) _motor.ReleaseIgnoredBelowFeet(_pos);
        }

        private bool CanApplyLockNow()
        {
            switch (_state)
            {
                case CharacterState.Dodge:
                case CharacterState.Hitstun:
                case CharacterState.Defeated:
                case CharacterState.ControlLocked:
                    return false;
                default:
                    return true;
            }
        }

        // (2) input — at most one action per sub-step, lowest seq first among allowed presses (Rule 15)
        private void ConsumeInput()
        {
            if (_state == CharacterState.Hitstun || _state == CharacterState.Defeated) return;

            Act bestAct = Act.None;
            InputActionId bestAction = InputActionId.None;
            long bestWindow = 0, bestSeq = long.MaxValue;

            Consider(InputActionId.Jump, _jumpWinT, ref bestAct, ref bestAction, ref bestWindow, ref bestSeq);
            Consider(InputActionId.DropThrough, _dropWinT, ref bestAct, ref bestAction, ref bestWindow, ref bestSeq);
            Consider(InputActionId.Dodge, _dodgeWinT, ref bestAct, ref bestAction, ref bestWindow, ref bestSeq);

            if (bestAct == Act.None) return;
            if (!_input.TryConsume(bestAction, bestWindow, ClockKind.Scaled, out InputRecord record)) return;
            Execute(bestAct, record);
        }

        private void Consider(InputActionId action, long window, ref Act bestAct, ref InputActionId bestAction, ref long bestWindow, ref long bestSeq)
        {
            if (!_input.Peek(action, window, ClockKind.Scaled, out InputRecord r) || r.Seq >= bestSeq) return;
            Act act = Decide(r);
            if (act == Act.None) return;
            bestAct = act;
            bestAction = action;
            bestWindow = window;
            bestSeq = r.Seq;
        }

        private Act Decide(in InputRecord r)
        {
            bool moveBlocked = MovementBlocked();
            switch (r.Action)
            {
                case InputActionId.Jump:
                case InputActionId.DropThrough:
                    if (moveBlocked || !JumpAllowed()) return Act.None;
                    if (r.Action == InputActionId.DropThrough && _grounded && _ground.OneWayOnly && _state == CharacterState.Grounded) return Act.DropThrough;
                    if (_grounded) return Act.GroundJump;
                    if (CoyoteActive()) return Act.CoyoteJump;
                    return _airJumps > 0 ? Act.AirJump : Act.None;

                case InputActionId.Dodge:
                    if (moveBlocked || _t < _cooldownEnd || !DodgeAllowed()) return Act.None;
                    return _grounded || _airDodges > 0 ? Act.Dodge : Act.None;

                default:
                    return Act.None;
            }
        }

        private bool JumpAllowed()
        {
            switch (_state)
            {
                case CharacterState.Grounded:
                case CharacterState.Airborne:
                    return true;
                default:
                    return false;
            }
        }

        private bool DodgeAllowed() => _state == CharacterState.Grounded || _state == CharacterState.Airborne;

        private void Execute(Act act, in InputRecord r)
        {
            switch (act)
            {
                case Act.GroundJump:
                    DoJump(JumpSpeedEffective(_c.JumpSpeed), JumpKind.Ground, r.Seq);
                    break;
                case Act.CoyoteJump:
                    double dy = Math.Max(0.0, _coyoteEdgeFeet - _motor.Feet(_pos));
                    double vj = JumpSpeedEffective(_c.JumpSpeed);
                    DoJump((float)Math.Sqrt(vj * vj + 2.0 * _c.Gravity * dy), JumpKind.Coyote, r.Seq);
                    break;
                case Act.AirJump:
                    _airJumps = 0;
                    AirResourcesChanged?.Invoke();
                    DoJump(JumpSpeedEffective(_c.DoubleJumpSpeed), JumpKind.Air, r.Seq);
                    break;
                case Act.DropThrough:
                    _motor.IgnoreSupports(_ground);
                    _ignoreUntil = _t + _dropIgnoreT;
                    _grounded = false;
                    _vy = 0f;
                    _coyoteUntil = -1;
                    SetState(CharacterState.Airborne);
                    LeftGround?.Invoke();
                    break;
                case Act.Dodge:
                    StartDodge(r);
                    break;
            }
        }

        private void DoJump(float speed, JumpKind kind, long seq)
        {
            _vy = speed;
            _grounded = false;
            _jumpRising = true;
            _hangEligible = true;
            _hangTime = 0.0;
            _jumpActive = true;
            _jumpCutDone = false;
            _jumpSeq = seq;
            _coyoteUntil = -1;
            SetState(CharacterState.Airborne);
            Jumped?.Invoke(kind);
        }

        private void StartDodge(in InputRecord r)
        {
            int dir = r.Move.X > 0f ? 1 : r.Move.X < 0f ? -1 : _facing;
            SetFacing(dir);
            bool air = !_grounded;
            if (air)
            {
                _airDodges = 0;
                AirResourcesChanged?.Invoke();
            }
            _dodgeDir = dir;
            _dodgeFromGround = _grounded;
            _dodgeStart = _t;
            _dodgeEnd = _t + _dodgeDurationT;
            _vy = 0f;
            _jumpRising = false;
            _hangEligible = false;
            _jumpActive = false;
            _iframeEnd = _t + (long)(_dodgeDurationT * (double)_iframeScale);
            SetIFrames(true);
            SetState(CharacterState.Dodge);
            DodgeStarted?.Invoke(dir, air);
        }

        private void EndDodge()
        {
            _cooldownEnd = _t + _dodgeCooldownT;
            _dodgeReadyPending = true;
            if (_iframeScale <= 1f || _t >= _iframeEnd) SetIFrames(false);
            DodgeEnded?.Invoke();
            _vy = 0f;
            if (!_grounded && _dodgeFromGround)
            {
                if (_airDodges > 0)
                {
                    _airDodges = 0; // a ground dodge that slid off a ledge counts as the air dodge (C-9)
                    AirResourcesChanged?.Invoke();
                }
                _coyoteUntil = _t + _coyoteT;
                _coyoteEdgeFeet = _motor.Feet(_pos);
            }
            SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
        }

        // (3) + (4) integrate, collide, contact transitions
        private void Integrate(double dt)
        {
            bool moveBlocked = MovementBlocked();
            float mx = moveBlocked ? 0f : Clamp(_input.ReadMove().X, -1f, 1f);
            float run = mx * (_running ? _c.MoveSpeed : _c.WalkSpeed) * SpeedMultiplier();

            if ((_state == CharacterState.Grounded || _state == CharacterState.Airborne) && mx != 0f)
                SetFacing(mx > 0f ? 1 : -1);

            switch (_state)
            {
                case CharacterState.Grounded:
                case CharacterState.Airborne:
                    _vx = run;
                    break;
                case CharacterState.Dodge:
                    _vx = _dodgeDir * _c.DodgeSpeed;
                    break;
                case CharacterState.Hitstun:
                    break; // keeps knockback
                default:
                    _vx = 0f;
                    break;
            }

            bool wasGrounded = _grounded;
            var contacts = new MoveContacts();

            if (wasGrounded && !_jumpRising)
            {
                _vy = 0f;
                Vec2 n = _ground.Normal;
                float dx = (float)(_vx * dt);
                float slopeDy = n.Y > 0f ? -dx * n.X / n.Y : 0f;
                _pos = _motor.Move(_pos, new Vec2(dx, slopeDy), ref contacts);

                float snap = Math.Max(_c.SnapMin, (float)(Math.Abs(_vx) * dt * Math.Tan(_c.MaxWalkAngle * Math.PI / 180.0)) + _c.Skin);
                if (_motor.ProbeGround(_pos, snap, out GroundInfo g))
                {
                    _pos = new Vec2(_pos.X, _pos.Y - g.Gap);
                    _ground = g;
                    if (!g.OneWayOnly) _lastSafeGround = _pos;
                }
                else
                {
                    _grounded = false;
                    _vy = Math.Min(_vy, 0f);
                    if (_state == CharacterState.Grounded) SetState(CharacterState.Airborne);
                    if (_state != CharacterState.Dodge)
                    {
                        _coyoteUntil = _t + _coyoteT;
                        _coyoteEdgeFeet = _motor.Feet(_pos);
                    }
                    LeftGround?.Invoke();
                }
            }
            else
            {
                float dy;
                if (_state == CharacterState.Dodge) dy = 0f;
                else
                {
                    ApplyJumpCut();
                    dy = IntegrateVertical(dt);
                }

                _pos = _motor.Move(_pos, new Vec2((float)(_vx * dt), dy), ref contacts);

                if (contacts.Ceiling && _vy > 0f)
                {
                    if (_state != CharacterState.Dodge && _motor.TryCornerCorrect(_pos, _c.CornerCorrection, Math.Max(0.02f, _vy * (float)dt), out Vec2 shifted))
                    {
                        _pos = shifted;
                    }
                    else
                    {
                        _vy = 0f;
                        Bonked?.Invoke();
                    }
                }

                if (_vy <= 0f)
                {
                    bool landed = contacts.Ground;
                    if (_motor.ProbeGround(_pos, _c.Skin, out GroundInfo g))
                    {
                        landed = true;
                        _pos = new Vec2(_pos.X, _pos.Y - g.Gap);
                        _ground = g;
                    }
                    else if (landed && _motor.ProbeGround(_pos, _c.Skin * 4f, out g)) _ground = g;
                    if (landed) Land();
                }
            }

            if (_jumpRising && _vy <= 0f) _jumpRising = false;
            if (contacts.Wall && _state == CharacterState.Hitstun) _vx = 0f;

            if (_pos.Y < _killY && !_fellOut)
            {
                _fellOut = true;
                FellOutOfWorld?.Invoke();
            }
        }

        private void ApplyJumpCut()
        {
            if (!_jumpActive || _jumpCutDone || _alwaysFullJump || _vy <= 0f) return;
            if (!_input.WasReleasedAfter(InputActionId.Jump, _jumpSeq)) return;
            _vy *= _c.JumpCutMult;
            _jumpCutDone = true;
        }

        // Exact piecewise integration (Rule 30): split at ±hang threshold, 0 and −max_fall so gravity is constant per piece.
        private float IntegrateVertical(double dt)
        {
            double g = _c.Gravity;
            double vy = _vy, dy = 0.0, rem = dt;
            double thr = _c.ApexHangThreshold, vmax = _c.MaxFallSpeed;
            bool holding = _alwaysFullJump || _input.IsHeld(InputActionId.Jump);

            for (int guard = 0; guard < 8 && rem > 1e-12; guard++)
            {
                if (vy <= -vmax)
                {
                    vy = -vmax;
                    dy += vy * rem;
                    break;
                }

                double probe = vy - 1e-9; // gravity of the interval just below the current speed
                bool hang = _hangEligible && holding && _c.ApexHangMult < 1f && Math.Abs(probe) < thr && _hangTime < _c.ApexHangMaxTime;
                double a = hang ? g * _c.ApexHangMult : probe < 0.0 ? g * _c.FallGravityMult : g;

                double next = vy > thr ? thr : vy > 0.0 ? 0.0 : vy > -thr ? -thr : -vmax;
                double tb = (vy - next) / a;
                double seg = Math.Min(rem, tb);
                if (hang) seg = Math.Min(seg, _c.ApexHangMaxTime - _hangTime);

                dy += vy * seg - 0.5 * a * seg * seg;
                vy = seg >= tb ? next : vy - a * seg;
                rem -= seg;
                if (hang) _hangTime += seg;
            }
            _vy = (float)vy;
            return (float)dy;
        }

        private void Land()
        {
            float fallSpeed = -_vy;
            _grounded = true;
            _vy = 0f;
            _jumpRising = false;
            _hangEligible = false;
            _jumpActive = false;
            _coyoteUntil = -1;
            if (!_ground.OneWayOnly) _lastSafeGround = _pos;
            if (_airJumps != 1 || _airDodges != 1)
            {
                _airJumps = 1;
                _airDodges = 1;
                AirResourcesChanged?.Invoke();
            }
            Landed?.Invoke(fallSpeed);

            if (_state == CharacterState.Airborne) SetState(CharacterState.Grounded);
        }

        // ---------------------------------------------------------------- helpers

        private void PlaceAt(Vec2 position)
        {
            _pos = position;
            _vx = 0f;
            _vy = 0f;
            _fellOut = false;
            _jumpRising = false;
            _hangEligible = false;
            _jumpActive = false;
            _coyoteUntil = -1;
            _motor.ClearIgnored();
            _airJumps = 1;
            _airDodges = 1;
            if (!_motor.Depenetrate(ref _pos, _c.DepenMax))
            {
                _pos = _lastSafeGround;
                if (!_motor.Depenetrate(ref _pos, _c.DepenMax))
                    _log.Error("Controller.Stuck", $"Could not push the character out of terrain at {position}.");
            }
            _grounded = _motor.ProbeGround(_pos, _c.SnapMin, out _ground);
            if (_grounded)
            {
                _pos = new Vec2(_pos.X, _pos.Y - _ground.Gap);
                if (!_ground.OneWayOnly) _lastSafeGround = _pos;
            }
            AirResourcesChanged?.Invoke();
        }

        private void CancelActions()
        {
            if (_state == CharacterState.Dodge)
            {
                _cooldownEnd = _t + _dodgeCooldownT;
                _dodgeReadyPending = true;
                DodgeEnded?.Invoke();
            }
            if (_iframeOn) SetIFrames(false);
            if (_state == CharacterState.Dodge)
                SetState(_grounded ? CharacterState.Grounded : CharacterState.Airborne);
        }

        private void OnRooted()
        {
            if (_state == CharacterState.Dodge) EndDodge(); // C-10: root cuts the dodge now, i-frames end
            if (_iframeOn) SetIFrames(false);
        }

        private bool MovementBlocked() => Rooted() || EffectiveLock() != ControlLockLevel.None;

        private bool Rooted()
        {
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_mods[i].Used && _mods[i].Root) return true;
            }
            return false;
        }

        private float SpeedMultiplier()
        {
            float p = 1f;
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_mods[i].Used) p *= _mods[i].Speed;
            }
            return Clamp(p, _c.ModifierSpeedMin, _c.ModifierSpeedMax);
        }

        private float JumpSpeedEffective(float baseSpeed)
        {
            float p = 1f;
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_mods[i].Used) p *= _mods[i].Jump;
            }
            return baseSpeed * (float)Math.Sqrt(Clamp(p, _c.ModifierJumpMin, _c.ModifierJumpMax));
        }

        private ControlLockLevel EffectiveLock()
        {
            ControlLockLevel level = ControlLockLevel.None;
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_locks[i].Used && _locks[i].Level > level) level = _locks[i].Level;
            }
            return level;
        }

        private bool CoyoteActive() => !_grounded && _coyoteUntil >= 0 && _t <= _coyoteUntil;

        private void SweepDeadHandles()
        {
            for (int i = 0; i < MaxHandles; i++)
            {
                if (_locks[i].Used && _locks[i].IsAlive != null && !_locks[i].IsAlive())
                {
                    _log.Error("Controller.DeadLockOwner", $"Control lock owned by a dead owner ({_locks[i].Owner}) removed.");
                    _locks[i].Used = false;
                    _locks[i].Owner = null;
                    _locks[i].IsAlive = null;
                }
                if (_mods[i].Used && _mods[i].IsAlive != null && !_mods[i].IsAlive())
                {
                    _log.Error("Controller.DeadModifierOwner", $"Movement modifier owned by a dead owner ({_mods[i].Owner}) removed.");
                    _mods[i].Used = false;
                    _mods[i].Owner = null;
                    _mods[i].IsAlive = null;
                }
            }
        }

        private void SetFacing(int facing)
        {
            if (facing == 0 || facing == _facing) return;
            _facing = facing;
            FacingChanged?.Invoke(facing);
        }

        private void SetIFrames(bool on)
        {
            if (_iframeOn == on) return;
            _iframeOn = on;
            if (on) IFrameStarted?.Invoke();
            else IFrameEnded?.Invoke();
        }

        private void SetState(CharacterState next)
        {
            if (next == _state) return;
            CharacterState old = _state;
            _state = next;
            StateChanged?.Invoke(old, next);
        }

        private void RecomputeWindows()
        {
            _coyoteT = Ticks.FromSeconds(_c.CoyoteTime * _coyoteScale);
            _jumpWinT = Ticks.FromSeconds(_c.JumpWindow * _bufferScale);
            _dropWinT = Ticks.FromSeconds(_c.DropThroughWindow * _bufferScale);
            _dodgeWinT = Ticks.FromSeconds(_c.DodgeWindow * _bufferScale);
            _runWinT = Ticks.FromSeconds(_c.RunWindow * _bufferScale);
        }

        private void RegisterWindows()
        {
            _input.RegisterConsumer(this, InputActionId.Jump, _jumpWinT, ClockKind.Scaled);
            _input.RegisterConsumer(this, InputActionId.DropThrough, _dropWinT, ClockKind.Scaled);
            _input.RegisterConsumer(this, InputActionId.Dodge, _dodgeWinT, ClockKind.Scaled);
            _input.RegisterConsumer(this, InputActionId.Run, _runWinT, ClockKind.Scaled);
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}

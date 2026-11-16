// PROTOTYPE - NOT FOR PRODUCTION
// Question: Do the farm half and the outing half still feed each other when the world is a boss-gated chain of single-biome maps with a far second village?
// Date: 2026-10-29
//
// Minimal walker driven by an EnemyDef: notice the player, walk over,
// telegraph -> active -> recover. Feet follow the map's ground height.
// Bosses are the same walker, bigger, leashed to their arena, with one extra
// move: a telegraphed charge across the floor. Drops come from the def:
// bone (fertilizer), ore (forge), gold (tickets), boss core (forge tier 3+).
// Killed enemies stay dead until the player sleeps; bosses stay dead forever.

using UnityEngine;

namespace DayLoopProto
{
    public class DayLoopEnemy : MonoBehaviour, IHittable
    {
        public EnemyDef def;
        public string key;

        public float aggroRange = 7f;
        public float bossLeash = 9f;
        public float chargeSpeed = 11f;
        public float chargeTime = 0.6f;
        public float chargeCooldown = 3f;

        enum State { Idle, Chase, Telegraph, Active, Recover, ChargeTelegraph, Charging, Dead }
        State _state = State.Idle;
        float _timer, _chargeReadyAt, _chargeDir;
        int _hp;
        bool _resolved;
        float _homeX, _knockVel;
        MapLayout _map;

        SpriteRenderer _sr;
        Collider2D _col;

        public bool IsDead => _state == State.Dead;
        public bool IsBoss => def != null && def.boss;
        public bool IsEngaged => _state != State.Idle && _state != State.Dead;
        public int Hp => _hp;

        public void Configure(EnemyDef d, string k, MapLayout map)
        {
            def = d;
            key = k;
            _map = map;
            _hp = d.hp;
            _homeX = transform.position.x;
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
            if (d.boss) aggroRange = 10f;
            _chargeReadyAt = Time.time + 1.5f;
        }

        void Update()
        {
            if (_state == State.Dead) return;
            var game = DayLoopGame.I;
            if (game == null || game.Player == null) return;
            if (game.InputBlocked) return; // freeze while a menu or a banner is up

            float dt = Time.deltaTime;
            Transform p = game.Player.transform;
            float dx = p.position.x - transform.position.x;
            float dy = Mathf.Abs(p.position.y - transform.position.y);
            float reach = def.range;

            if (Mathf.Abs(_knockVel) > 0.01f)
            {
                MoveTo(transform.position.x + _knockVel * dt);
                _knockVel = Mathf.MoveTowards(_knockVel, 0f, 30f * dt);
            }

            switch (_state)
            {
                case State.Idle:
                    _sr.color = def.color;
                    if (Mathf.Abs(dx) < aggroRange && dy < 4f) _state = State.Chase;
                    else WalkToward(_homeX, dt);
                    break;

                case State.Chase:
                    _sr.color = def.color;
                    if (Mathf.Abs(dx) > aggroRange * 1.5f) { _state = State.Idle; break; }
                    if (IsBoss && Time.time >= _chargeReadyAt && Mathf.Abs(dx) > 3f && Mathf.Abs(dx) < 9f)
                    {
                        _state = State.ChargeTelegraph;
                        _timer = 0.6f;
                        _chargeDir = Mathf.Sign(dx);
                    }
                    else if (Mathf.Abs(dx) <= reach * 0.8f && dy < 1.5f + def.size.y * 0.5f)
                    {
                        _state = State.Telegraph;
                        _timer = def.telegraph;
                    }
                    else WalkToward(p.position.x, dt);
                    break;

                case State.Telegraph:
                    _sr.color = Color.Lerp(def.color, Color.yellow, Mathf.PingPong(Time.time * 10f, 1f));
                    if ((_timer -= dt) <= 0f) { _state = State.Active; _timer = def.active; _resolved = false; }
                    break;

                case State.Active:
                    _sr.color = new Color(1f, 0.95f, 0.6f);
                    if (!_resolved && Mathf.Abs(dx) <= reach && dy < 1.5f + def.size.y * 0.5f)
                    {
                        game.Player.ReceiveHit(def.damage, transform.position);
                        _resolved = true;
                    }
                    if ((_timer -= dt) <= 0f) { _state = State.Recover; _timer = def.recover; }
                    break;

                case State.Recover:
                    _sr.color = def.color * 0.75f;
                    if ((_timer -= dt) <= 0f) _state = State.Chase;
                    break;

                case State.ChargeTelegraph:
                    _sr.color = Color.Lerp(def.color, Color.red, Mathf.PingPong(Time.time * 12f, 1f));
                    if ((_timer -= dt) <= 0f) { _state = State.Charging; _timer = chargeTime; _resolved = false; }
                    break;

                case State.Charging:
                    _sr.color = new Color(1f, 0.6f, 0.5f);
                    MoveTo(transform.position.x + _chargeDir * chargeSpeed * dt);
                    if (!_resolved && Mathf.Abs(dx) <= def.size.x * 0.5f + 0.5f && dy < 1.5f + def.size.y * 0.5f)
                    {
                        game.Player.ReceiveHit(def.damage, transform.position);
                        _resolved = true;
                    }
                    if ((_timer -= dt) <= 0f)
                    {
                        _state = State.Recover;
                        _timer = def.recover + 0.4f;   // the punish window after a charge
                        _chargeReadyAt = Time.time + chargeCooldown;
                    }
                    break;
            }
        }

        void WalkToward(float x, float dt)
        {
            float d = x - transform.position.x;
            if (Mathf.Abs(d) < 0.05f) return;
            MoveTo(transform.position.x + Mathf.Sign(d) * def.speed * dt);
        }

        // Horizontal move, clamped to the map (and the arena for bosses); feet stay on the ground.
        void MoveTo(float x)
        {
            float lo = _map.playMin + 0.5f, hi = _map.playMax - 0.5f;
            if (IsBoss) { lo = Mathf.Max(lo, _homeX - bossLeash); hi = Mathf.Min(hi, _homeX + bossLeash); }
            x = Mathf.Clamp(x, lo, hi);
            transform.position = new Vector3(x, _map.GroundY(x) + def.size.y / 2f, 0f);
        }

        public void TakeHit(int dmg, Vector2 fromPosition)
        {
            if (_state == State.Dead) return;
            _hp -= dmg;
            _sr.color = Color.white;
            _knockVel = Mathf.Sign(transform.position.x - fromPosition.x) * def.knock;
            if (_state == State.Idle) _state = State.Chase;

            if (_hp <= 0)
            {
                _state = State.Dead;
                _sr.enabled = false;
                _col.enabled = false;
                DayLoopGame.I?.OnEnemyKilled(this);
            }
        }

        public string HpText => _state == State.Dead ? "" : $"{def.name} {Mathf.Max(_hp, 0)}/{def.hp}";
    }
}

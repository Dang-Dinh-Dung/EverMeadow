// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the combat feel agile and weighted (Hollow Knight-paced, one notch heavier)?
// Date: 2026-10-24
//
// No AI. A fixed telegraph -> active -> recover loop, something to read and
// dodge/punish. "KO" after a few hits, then it resets so testing stays
// continuous instead of ending the session.

using UnityEngine;

public class DummyEnemy : MonoBehaviour
{
    public Transform player;

    public float idleTime = 1.0f;
    public float telegraphTime = 0.5f;
    public float activeTime = 0.25f;
    public float recoverTime = 0.4f;
    public float attackRange = 1.6f;
    public int hitsToKo = 5;
    public float koRespawnDelay = 1.5f;

    enum State { Idle, Telegraph, Active, Recover, Ko }
    State _state = State.Idle;
    float _timer;
    int _hitsTaken;
    bool _hasHitThisSwing;

    SpriteRenderer _sr;
    Color _baseColor = new Color(0.85f, 0.35f, 0.25f);
    Vector3 _baseScale;

    // Read by CombatFeelPlayer / ComboTracker to score dodge timing against
    // this enemy's actual active window, rather than guessing.
    public bool IsActiveWindow => _state == State.Active;
    public float ActiveWindowStart { get; private set; }

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = PrototypeVisuals.SolidSquare(_baseColor);
        _baseScale = transform.localScale;
        _timer = idleTime;
    }

    void Update()
    {
        if (_state == State.Ko)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f) Respawn();
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) { Tick(); return; }

        switch (_state)
        {
            case State.Idle:
                _state = State.Telegraph;
                _timer = telegraphTime;
                break;
            case State.Telegraph:
                _state = State.Active;
                _timer = activeTime;
                ActiveWindowStart = Time.time;
                _hasHitThisSwing = false;
                break;
            case State.Active:
                _state = State.Recover;
                _timer = recoverTime;
                break;
            case State.Recover:
                _state = State.Idle;
                _timer = idleTime;
                break;
        }
    }

    void Tick()
    {
        // Visual telegraph: flash yellow before the hit lands, so a player has
        // something real to react to.
        if (_state == State.Telegraph)
        {
            float t = Mathf.PingPong(Time.time * 10f, 1f);
            _sr.color = Color.Lerp(_baseColor, Color.yellow, t);
        }
        else
        {
            _sr.color = _baseColor;
        }

        if (_state == State.Active && !_hasHitThisSwing && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                var pc = player.GetComponent<CombatFeelPlayer>();
                if (pc != null) pc.ReceiveHit(transform.position);
                // Resolved once per swing either way (hit landed or dodged) —
                // an enemy attack does not retry mid-swing.
                _hasHitThisSwing = true;
            }
        }
    }

    public void TryReceiveHit(Vector2 fromPosition)
    {
        if (_state == State.Ko) return;

        _hitsTaken++;
        // Hit-stop is triggered once, by the attacker (CombatFeelPlayer.TryHitEnemy).
        StartFlashWhite();

        if (_hitsTaken >= hitsToKo)
        {
            _state = State.Ko;
            _timer = koRespawnDelay;
            transform.localScale = _baseScale * 0.4f;
        }
    }

    void StartFlashWhite()
    {
        _sr.color = Color.white;
        CancelInvoke(nameof(ClearFlash));
        Invoke(nameof(ClearFlash), 0.08f);
    }

    void ClearFlash()
    {
        if (_state != State.Ko) _sr.color = _baseColor;
    }

    void Respawn()
    {
        _hitsTaken = 0;
        transform.localScale = _baseScale;
        _state = State.Idle;
        _timer = idleTime;
    }

    public int HitsTaken => _hitsTaken;
    public int HitsToKo => hitsToKo;
    public bool IsKo => _state == State.Ko;
}

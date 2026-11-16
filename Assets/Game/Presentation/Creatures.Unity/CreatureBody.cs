using System.Collections.Generic;
using System.IO;
using Evermeadow.Core.AI;
using Evermeadow.Core.Skills;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.SkillRuntime.Unity;
using Evermeadow.Presentation.SpriteAnimation;
using UnityEngine;

namespace Evermeadow.Presentation.Creatures.Unity
{
    /// <summary>
    /// A creature in the minimal creature runtime (user decisions 2026-11-14; full Enemy AI GDD later): its Animator plays
    /// only idle1 / walk (and hurt / die once Combat exists) by itself; every attack is a skill (Skill Tool) that its
    /// behaviour tree (<c>StreamingAssets/AI/&lt;behaviour&gt;.xml</c>) casts through a <see cref="SkillCaster"/>, exactly like the
    /// player's moveset does. Moves horizontally only (patrol around home, chase the target), stands still while a skill
    /// runs. Logs every change of branch of the tree's root selector (AI rule: transitions are logged) and draws its patrol
    /// range and target line as gizmos. <c>GameBootstrap</c> calls <see cref="Init"/> for every creature in the scene.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (CreatureBody c in FindObjectsByType&lt;CreatureBody&gt;(FindObjectsSortMode.None))
    ///     c.Init(clock, player.transform, skillFx, skills);
    /// </code>
    /// </example>
    public sealed class CreatureBody : MonoBehaviour, ISkillActor, IBtAgent
    {
        [SerializeField, Tooltip("Behaviour tree file name in StreamingAssets/AI (without .xml).")]
        private string behaviour = "goblin";
        [SerializeField, Tooltip("Walk speed, u/s.")] private float walkSpeed = 2f;
        [SerializeField, Tooltip("Patrol goes this far left and right of where the creature starts, u.")] private float patrolRange = 3f;
        [SerializeField, Tooltip("Animator of the rig (empty = the first one below).")] private Animator animator;
        [SerializeField, Tooltip("Sprite to mirror for facing (empty = the first one below).")] private SpriteRenderer sprite;
        [SerializeField, Tooltip("The art is drawn facing left.")] private bool artFacesLeft;
        [SerializeField, Tooltip("Always log this creature's branch changes (all creatures: Evermeadow ▸ Project Settings ▸ Log AI Decisions).")]
        private bool logDecisions;

        private static readonly int SpeedParam = Animator.StringToHash(CreatureAnimParams.Speed);
        private static readonly int ActionParam = Animator.StringToHash(CreatureAnimParams.Action);

        private IFrameClock _clock;
        private Transform _target;
        private SkillCaster _caster;
        private BtNode _root;
        private float _homeX, _vx;
        private int _facing = 1, _patrolDir = 1, _lastBranch = -2;
        private bool _casting;

        /// <summary>Hands the creature the clock, its target (the player), the shared skill FX and the live skills.</summary>
        public void Init(IFrameClock clock, Transform target, SkillFxHost fx, SkillRepository skills)
        {
            _clock = clock;
            _target = target;
            _homeX = transform.position.x;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            if (!TryGetComponent(out _caster)) _caster = gameObject.AddComponent<SkillCaster>();
            _caster.Init(clock, this, fx, skills);
            _root = LoadTree(skills);
        }

        private BtNode LoadTree(SkillRepository skills)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "AI", behaviour + ".xml");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[AI] {name}: no behaviour file {path} — the creature only stands.");
                return null;
            }
            var errors = new List<SkillError>();
            if (!BehaviourTree.TryParse(File.ReadAllText(path), behaviour, out BehaviourTree tree, errors))
            {
                foreach (SkillError e in errors) Debug.LogWarning($"[AI] {name}: {e}");
                return null;
            }
            foreach (string id in tree.SkillIds)
                if (skills == null || !skills.Library.TryGet(id, out _)) Debug.LogWarning($"[AI] {name}: behaviour '{behaviour}' casts skill {id}, which does not exist.");
            return tree.Build();
        }

        private void Update()
        {
            if (_clock == null) return;
            _vx = 0f;
            if (_root != null)
            {
                _root.Tick(this);
                LogBranch();
            }
            if (_casting) _vx = 0f;
            float dt = (float)(_clock.ScaledDelta / (double)Ticks.PerSecond);
            transform.position += new Vector3(_vx * dt, 0f, 0f);
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedParam, Mathf.Abs(_vx));
                animator.SetInteger(ActionParam, 0);
            }
            if (sprite != null) sprite.flipX = artFacesLeft ? _facing > 0 : _facing < 0;
        }

        private void LogBranch()
        {
            if (!(_root is BtSelector sel) || sel.LastIndex == _lastBranch) return;
            if (!Logging)
            {
                _lastBranch = sel.LastIndex;
                return;
            }
            string Label(int i) => i < 0 ? "(nothing)" : $"#{i} {Describe(sel.Child(i))}";
            Debug.Log($"[AI] {name}: {(_lastBranch == -2 ? "start" : Label(_lastBranch))} → {Label(sel.LastIndex)}");
            _lastBranch = sel.LastIndex;
        }

        private bool Logging => logDecisions || DevOptions.LogAiDecisions;

        private static string Describe(BtNode n) => n is BtSequence ? "sequence" : n.Name;

        // ------------------------------------------------------------------ ISkillActor

        /// <inheritdoc />
        public Vector2 Position => transform.position;

        /// <inheritdoc />
        public int Facing => _facing;

        /// <inheritdoc />
        public Transform Transform => transform;

        /// <inheritdoc />
        public Animator Animator => animator;

        /// <inheritdoc />
        public void SetCasting(bool casting) => _casting = casting;

        // ------------------------------------------------------------------ IBtAgent

        /// <inheritdoc />
        public long Now => _clock != null ? _clock.ScaledNow : 0;

        /// <inheritdoc />
        public bool HasTarget => _target != null;

        /// <inheritdoc />
        public float TargetDistance => _target != null ? Mathf.Abs(_target.position.x - transform.position.x) : float.PositiveInfinity;

        /// <inheritdoc />
        public bool IsCasting => _caster != null && _caster.Busy;

        /// <inheritdoc />
        public bool TryCast(string skillId)
        {
            if (_caster == null) return false;
            if (_caster.TryCast(skillId, out string why)) return true;
            if (Logging) Debug.Log($"[AI] {name}: cast {skillId} refused — {why}");
            return false;
        }

        /// <inheritdoc />
        public void MoveTowardTarget(float speedScale)
        {
            if (_target == null || _casting) return;
            FaceTarget();
            _vx = _facing * walkSpeed * speedScale;
        }

        /// <inheritdoc />
        public void Patrol()
        {
            if (_casting) return;
            float x = transform.position.x;
            if (x > _homeX + patrolRange) _patrolDir = -1;
            else if (x < _homeX - patrolRange) _patrolDir = 1;
            _facing = _patrolDir;
            _vx = _patrolDir * walkSpeed * 0.5f;
        }

        /// <inheritdoc />
        public void Stop() => _vx = 0f;

        /// <inheritdoc />
        public void FaceTarget()
        {
            if (_target == null || _casting) return;
            float dx = _target.position.x - transform.position.x;
            if (dx != 0f) _facing = dx > 0f ? 1 : -1;
        }

        private void OnDrawGizmosSelected()
        {
            float home = Application.isPlaying ? _homeX : transform.position.x;
            Vector3 p = transform.position;
            Gizmos.color = new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawLine(new Vector3(home - patrolRange, p.y, 0f), new Vector3(home + patrolRange, p.y, 0f));
            if (_target != null)
            {
                Gizmos.color = IsCasting ? Color.red : Color.yellow;
                Gizmos.DrawLine(p, _target.position);
            }
        }
    }
}

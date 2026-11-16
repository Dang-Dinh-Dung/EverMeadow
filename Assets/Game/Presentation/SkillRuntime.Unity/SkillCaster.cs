using System;
using System.Collections.Generic;
using Evermeadow.Core.Skills;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Time;
using UnityEngine;

namespace Evermeadow.Presentation.SkillRuntime.Unity
{
    /// <summary>
    /// Casts skills for one actor — the player (moveset) or a creature (behaviour tree) — with the minimal runtime (user
    /// decisions 2026-11-14): <see cref="TryCast"/> starts a <see cref="SkillRun"/> unless one is still before its End; while
    /// it runs the actor holds still (<see cref="ISkillActor.SetCasting"/>); at End it is released, the animation goes back to
    /// idle and the run keeps ticking in the background until its Hit / Effect nodes finish. Time is the game clock.
    /// </summary>
    /// <example>
    /// <code>
    /// caster.Init(clock, new PlayerSkillActor(body.Controller, body.transform, animator), fxHost, skills);
    /// if (caster.TryCast("100000", out string why)) chainEnd = caster.LastEndTick;
    /// </code>
    /// </example>
    [DefaultExecutionOrder(50)]
    public sealed class SkillCaster : MonoBehaviour
    {
        private IFrameClock _clock;
        private ISkillActor _actor;
        private SkillRepository _skills;
        private SkillPerformer _performer;
        private SkillRun _current;
        private readonly List<SkillRun> _finishing = new List<SkillRun>();
        private bool _locked;

        /// <summary>One line per node event of the running skills, prefixed with the time since the cast (dev tools).</summary>
        public event Action<string> Trace;

        private long _castTick;

        /// <summary>True while a skill has not reached its End.</summary>
        public bool Busy => _current != null && !_current.Ended;

        /// <summary>Id of the skill that ran last (empty before the first cast).</summary>
        public string LastSkillId { get; private set; } = "";

        /// <summary>Tick the last skill ended (long.MaxValue while it still runs): follow-up link windows count from here.</summary>
        public long LastEndTick => _current == null ? 0 : _current.Ended ? _current.EndTick : long.MaxValue;

        /// <summary>Hands the caster its actor, the shared skill FX (may be null) and the live skills.</summary>
        public void Init(IFrameClock clock, ISkillActor actor, SkillFxHost fx, SkillRepository skills)
        {
            _clock = clock;
            _actor = actor;
            _skills = skills;
            _performer?.Dispose();
            _performer = actor != null ? new SkillPerformer(actor, fx != null ? fx.Vfx : null, fx != null ? fx.Areas : null) : null;
            if (_performer != null)
                _performer.Trace += line =>
                {
                    string stamped = $"   {(_clock.ScaledNow - _castTick) / (double)Ticks.PerSecond:0.00}s {line}";
                    Trace?.Invoke(stamped);
                    if (DevOptions.LogSkillTrace) Debug.Log($"[Skill] {actor.Transform?.name ?? "actor"} · skill {LastSkillId}{stamped}");
                };
            if (actor != null && actor.Animator == null) Debug.LogWarning($"[Skill] {actor.Transform?.name ?? "actor"} has no Animator: Animation nodes will not show.");
        }

        /// <summary>Starts skill <paramref name="skillId"/> now; false with a reason when it cannot.</summary>
        public bool TryCast(string skillId, out string reason)
        {
            reason = "";
            if (_performer == null || _clock == null) { reason = "chưa có người tung"; return false; }
            if (Busy) { reason = $"đang ra đòn {_current.Skill.Id} (chưa tới End)"; return false; }
            if (_skills == null || !_skills.Library.TryGet(skillId, out SkillDef def)) { reason = $"không có file skill {skillId}"; return false; }
            if (_current != null && !_current.Finished) _finishing.Add(_current);
            _current = new SkillRun(def, _performer);
            LastSkillId = def.Id;
            _castTick = _clock.ScaledNow;
            if (!_locked)
            {
                _actor.SetCasting(true);
                _locked = true;
            }
            _current.Start(_clock.ScaledNow);
            AfterTick();
            return true;
        }

        private void Update()
        {
            if (_clock == null) return;
            long now = _clock.ScaledNow;
            for (int i = _finishing.Count - 1; i >= 0; i--)
            {
                _finishing[i].Tick(now);
                if (_finishing[i].Finished) _finishing.RemoveAt(i);
            }
            _current?.Tick(now);
            AfterTick();
            _performer?.Tick(_clock.Scale);
        }

        private void AfterTick()
        {
            if (_locked && (_current == null || _current.Ended))
            {
                _actor.SetCasting(false);
                _locked = false;
            }
        }

        private void OnDestroy()
        {
            long now = _clock != null ? _clock.ScaledNow : 0;
            _current?.Abort(now);
            foreach (SkillRun r in _finishing) r.Abort(now);
            _performer?.Dispose();
        }
    }
}

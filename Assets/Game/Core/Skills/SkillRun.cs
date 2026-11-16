using System;
using System.Collections.Generic;
using System.Globalization;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// What a running skill asks the game to show or do (presentation / gameplay side of <see cref="SkillRun"/>). Every call
    /// gets the node so the performer can read its fields. Durations are seconds; <see cref="double.PositiveInfinity"/>
    /// means "until the skill ends".
    /// </summary>
    public interface ISkillPerformer
    {
        /// <summary>
        /// A node starts. <paramref name="suggestedSeconds"/> comes from the node's own fields (Effect Duration, steady Hit
        /// SteadyDuration, …; 0 for Animation, whose length only the performer knows). Returns how long the node runs.
        /// </summary>
        double Begin(SkillNodeData node, double suggestedSeconds);

        /// <summary>A node stops: its duration ran out, or <paramref name="interrupted"/> (skill ended / aborted).</summary>
        void Finish(SkillNodeData node, bool interrupted);

        /// <summary>A Search node runs: true when it finds something (chooses the Found / NotFound children).</summary>
        bool Search(SkillNodeData node);

        /// <summary>The skill ended (End reached or nothing left): the owner's animation goes back to idle.</summary>
        void ReturnToIdle();
    }

    /// <summary>
    /// Runs one cast of a skill graph in time (GDD Skill &amp; Moveset Rule 4d; End node and the minimal runtime: user decision
    /// 2026-11-14). Start begins at the cast tick; a child starts <c>ActionDelay</c> after its parent starts (trigger
    /// <c>start</c>) or ends (<c>end</c>); Search picks <c>found</c> / <c>not_found</c> children; <c>hit</c> children need
    /// Combat and never fire yet. <b>End</b>: when an End node starts, the skill ends — animation back to idle, nodes not
    /// started are dropped, nodes already running finish their own duration (their <c>end</c> children no longer start).
    /// Without an End the skill ends when nothing is waiting and only open-ended nodes (looping animation) are left.
    /// Times use the 1/60 s step rule of every skill time (<see cref="SkillTime"/>). Deterministic; no allocation per
    /// <see cref="Tick"/> once the queues have grown; single-thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// var run = new SkillRun(def, performer);
    /// run.Start(clock.ScaledNow);
    /// void Update() { run.Tick(clock.ScaledNow); if (run.Ended) UnlockControls(); if (run.Finished) run = null; }
    /// </code>
    /// </example>
    public sealed class SkillRun
    {
        private const byte BeginEvent = 0, FinishEvent = 1;

        private struct Ev
        {
            public long Tick;
            public long Seq;
            public int Node;
            public byte Type;
        }

        private readonly SkillDef _def;
        private readonly ISkillPerformer _performer;
        private readonly List<Ev> _queue = new List<Ev>(16);
        private readonly List<int> _running = new List<int>(8);
        private readonly List<bool> _openEnded = new List<bool>(8);
        private long _seq;
        private bool _started;

        /// <summary>Prepares a run of <paramref name="def"/>; nothing happens until <see cref="Start"/>.</summary>
        public SkillRun(SkillDef def, ISkillPerformer performer)
        {
            _def = def ?? throw new ArgumentNullException(nameof(def));
            _performer = performer ?? throw new ArgumentNullException(nameof(performer));
        }

        /// <summary>The skill being run.</summary>
        public SkillDef Skill => _def;

        /// <summary>True once the skill ended (End reached, nothing left, or aborted): the owner is free again.</summary>
        public bool Ended { get; private set; }

        /// <summary>Tick the skill ended (valid when <see cref="Ended"/>): the follow-up link window counts from here.</summary>
        public long EndTick { get; private set; }

        /// <summary>True when the skill ended and every node that was still running has finished too.</summary>
        public bool Finished => Ended && _running.Count == 0;

        /// <summary>Number of nodes running now (debug).</summary>
        public int RunningCount => _running.Count;

        /// <summary>Starts the cast at <paramref name="now"/> and processes everything due at that tick.</summary>
        public void Start(long now)
        {
            if (_started) throw new InvalidOperationException("a SkillRun starts once");
            _started = true;
            Push(now, 0, BeginEvent);
            Tick(now);
        }

        /// <summary>Processes every event due at or before <paramref name="now"/>, in time order (ties: scheduling order).</summary>
        public void Tick(long now)
        {
            if (!_started) return;
            long last = now;
            bool any = false;
            while (TryPop(now, out Ev e))
            {
                any = true;
                last = e.Tick;
                if (e.Type == BeginEvent) Begin(e.Node, e.Tick);
                else Finish(e.Node, e.Tick, false);
            }
            if (!Ended && !HasPendingBegin() && OnlyOpenEndedRunning()) EndSkill(any ? last : now);
        }

        /// <summary>Stops everything now (another skill took over, hitstun, death): running nodes are interrupted.</summary>
        public void Abort(long now)
        {
            if (!_started) return;
            if (!Ended) EndSkill(now);
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                SkillNodeData n = _def.Node(_running[i]);
                _running.RemoveAt(i);
                _openEnded.RemoveAt(i);
                if (n != null && n.Kind != SkillNodeKind.Start) _performer.Finish(n, true);
            }
            _queue.Clear();
        }

        private void Begin(int id, long t)
        {
            SkillNodeData node = _def.Node(id);
            if (node == null) return;
            if (node.Kind == SkillNodeKind.End)
            {
                EndSkill(t);
                return;
            }

            double seconds = 0;
            if (node.Kind != SkillNodeKind.Start) seconds = _performer.Begin(node, SuggestedSeconds(node));
            bool openEnded = double.IsPositiveInfinity(seconds);
            _running.Add(id);
            _openEnded.Add(openEnded);

            bool? found = null;
            if (node.Kind == SkillNodeKind.Search) found = _performer.Search(node);
            List<SkillLink> children = _def.Children(id);
            for (int i = 0; i < children.Count; i++)
            {
                SkillLink l = children[i];
                bool now = l.When == SkillTrigger.Start
                           || (found == true && l.When == SkillTrigger.Found)
                           || (found == false && l.When == SkillTrigger.NotFound);
                if (now) Push(t + Delay(l.To), l.To, BeginEvent);
            }
            if (!openEnded) Push(t + SkillTime.TickOf(Math.Max(0, seconds)), id, FinishEvent);
        }

        private void Finish(int id, long t, bool interrupted)
        {
            int index = _running.IndexOf(id);
            if (index < 0) return;
            _running.RemoveAt(index);
            _openEnded.RemoveAt(index);
            SkillNodeData node = _def.Node(id);
            if (node == null) return;
            if (node.Kind != SkillNodeKind.Start) _performer.Finish(node, interrupted);
            if (Ended || interrupted) return;
            List<SkillLink> children = _def.Children(id);
            for (int i = 0; i < children.Count; i++)
                if (children[i].When == SkillTrigger.End) Push(t + Delay(children[i].To), children[i].To, BeginEvent);
        }

        private void EndSkill(long t)
        {
            Ended = true;
            EndTick = t;
            for (int i = _queue.Count - 1; i >= 0; i--)
                if (_queue[i].Type == BeginEvent) _queue.RemoveAt(i);   // nodes not started yet are dropped
            // Animations and open-ended nodes stop with the skill; Hit, Effect … keep their remaining duration.
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                SkillNodeData n = _def.Node(_running[i]);
                if (n == null || !(_openEnded[i] || n.Kind == SkillNodeKind.Animation || n.Kind == SkillNodeKind.Start)) continue;
                _running.RemoveAt(i);
                _openEnded.RemoveAt(i);
                if (n.Kind != SkillNodeKind.Start) _performer.Finish(n, true);
            }
            _performer.ReturnToIdle();
        }

        private bool HasPendingBegin()
        {
            for (int i = 0; i < _queue.Count; i++)
                if (_queue[i].Type == BeginEvent) return true;
            return false;
        }

        private bool OnlyOpenEndedRunning()
        {
            for (int i = 0; i < _openEnded.Count; i++)
                if (!_openEnded[i]) return false;
            return true;
        }

        private long Delay(int nodeId)
        {
            SkillNodeData n = _def.Node(nodeId);
            return n == null ? 0 : SkillTime.TickOf(Number(n, "ActionDelay"));
        }

        private void Push(long tick, int node, byte type) => _queue.Add(new Ev { Tick = tick, Seq = _seq++, Node = node, Type = type });

        private bool TryPop(long now, out Ev e)
        {
            int best = -1;
            for (int i = 0; i < _queue.Count; i++)
            {
                Ev q = _queue[i];
                if (q.Tick > now) continue;
                if (best < 0 || q.Tick < _queue[best].Tick || q.Tick == _queue[best].Tick && q.Seq < _queue[best].Seq) best = i;
            }
            if (best < 0)
            {
                e = default;
                return false;
            }
            e = _queue[best];
            _queue.RemoveAt(best);
            return true;
        }

        /// <summary>
        /// Duration a node's own fields give: Effect <c>Duration</c>; Hit <c>SteadyDuration</c> when steady (instant otherwise);
        /// Move <c>Value</c> when it is time-based; MoveControl / CameraAction / Knockback <c>Duration</c>; 0 for the rest
        /// (Animation: the performer knows the clip length).
        /// </summary>
        public static double SuggestedSeconds(SkillNodeData node)
        {
            switch (node.Kind)
            {
                case SkillNodeKind.Effect:
                case SkillNodeKind.MoveControl:
                case SkillNodeKind.CameraAction:
                case SkillNodeKind.Knockback:
                    return Math.Max(0, Number(node, "Duration"));
                case SkillNodeKind.Hit:
                    return node.Get("IsSteady") == "true" ? Math.Max(0, Number(node, "SteadyDuration")) : 0;
                case SkillNodeKind.Move:
                    return node.Get("MoveStandard") == nameof(SkillMoveStandard.Time) ? Math.Max(0, Number(node, "Value")) : 0;
                default:
                    return 0;
            }
        }

        /// <summary>Number field of a node (its default when absent or not a number).</summary>
        public static double Number(SkillNodeData node, string field) =>
            double.TryParse(node.Get(field), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v) ? v : 0;
    }
}

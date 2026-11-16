using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Evermeadow.Core.Skills;

namespace Evermeadow.Core.AI
{
    /// <summary>Result of one tick of a behaviour tree node.</summary>
    public enum BtStatus
    {
        /// <summary>Done, it worked.</summary>
        Success,
        /// <summary>Done, it did not work (a selector tries the next child).</summary>
        Failure,
        /// <summary>Still going: tick again next frame.</summary>
        Running
    }

    /// <summary>
    /// What a creature can sense and do, as the behaviour tree sees it (implemented by the creature runtime). Distances are
    /// horizontal, in world units; times are clock ticks (1 µs).
    /// </summary>
    public interface IBtAgent
    {
        /// <summary>Clock now (game clock: stops on pause, slows on hit-stop).</summary>
        long Now { get; }
        /// <summary>True when there is a target (the player) to react to.</summary>
        bool HasTarget { get; }
        /// <summary>Horizontal distance to the target (∞ without one).</summary>
        float TargetDistance { get; }
        /// <summary>True while one of the creature's skills has not reached its End.</summary>
        bool IsCasting { get; }
        /// <summary>Starts skill <paramref name="skillId"/> facing the target; false when it cannot (casting, missing skill…).</summary>
        bool TryCast(string skillId);
        /// <summary>Walks toward the target this frame at <paramref name="speedScale"/> × its walk speed.</summary>
        void MoveTowardTarget(float speedScale);
        /// <summary>Walks back and forth around its home this frame.</summary>
        void Patrol();
        /// <summary>Stands still this frame.</summary>
        void Stop();
        /// <summary>Turns to face the target.</summary>
        void FaceTarget();
    }

    /// <summary>One node of a behaviour tree. Nodes keep per-creature state, so every creature builds its own tree.</summary>
    public abstract class BtNode
    {
        /// <summary>Runs the node once.</summary>
        public abstract BtStatus Tick(IBtAgent agent);

        /// <summary>Forgets running state (a parent moved on to another branch).</summary>
        public virtual void Reset() { }

        /// <summary>XML element name (for errors / debug).</summary>
        public abstract string Name { get; }
    }

    /// <summary>
    /// Tries children in order every tick; the first that does not fail decides (Success / Running). A child that was running
    /// and is now skipped because an earlier child took over is reset. Fails when every child fails.
    /// </summary>
    public sealed class BtSelector : BtNode
    {
        private readonly BtNode[] _children;
        private int _running = -1;

        /// <summary>Creates a selector.</summary>
        public BtSelector(BtNode[] children) => _children = children;

        /// <inheritdoc />
        public override string Name => "selector";

        /// <summary>Index of the child that is running (-1 = none): debug hook for "which branch is the creature in".</summary>
        public int ActiveIndex => _running;

        /// <summary>Last child that did not fail this tick (debug), or -1.</summary>
        public int LastIndex { get; private set; } = -1;

        /// <summary>Child <paramref name="i"/> (debug).</summary>
        public BtNode Child(int i) => _children[i];

        /// <summary>Number of children.</summary>
        public int Count => _children.Length;

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent agent)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                BtStatus s = _children[i].Tick(agent);
                if (s == BtStatus.Failure) continue;
                LastIndex = i;
                if (_running >= 0 && _running != i) _children[_running].Reset();
                _running = s == BtStatus.Running ? i : -1;
                return s;
            }
            if (_running >= 0) _children[_running].Reset();
            _running = -1;
            LastIndex = -1;
            return BtStatus.Failure;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            if (_running >= 0) _children[_running].Reset();
            _running = -1;
        }
    }

    /// <summary>
    /// Runs children in order and remembers where it is: a running child is ticked again next frame (earlier children are not
    /// re-checked meanwhile); a failing child fails the sequence; all succeed = success.
    /// </summary>
    public sealed class BtSequence : BtNode
    {
        private readonly BtNode[] _children;
        private int _index;

        /// <summary>Creates a sequence.</summary>
        public BtSequence(BtNode[] children) => _children = children;

        /// <inheritdoc />
        public override string Name => "sequence";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent agent)
        {
            while (_index < _children.Length)
            {
                BtStatus s = _children[_index].Tick(agent);
                if (s == BtStatus.Running) return BtStatus.Running;
                if (s == BtStatus.Failure)
                {
                    _index = 0;
                    return BtStatus.Failure;
                }
                _index++;
            }
            _index = 0;
            return BtStatus.Success;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            if (_index < _children.Length) _children[_index].Reset();
            _index = 0;
        }
    }

    /// <summary>Succeeds while the target is within [min, max] horizontally; fails otherwise or without a target.</summary>
    public sealed class BtInRange : BtNode
    {
        private readonly float _min, _max;

        /// <summary>Creates the condition.</summary>
        public BtInRange(float min, float max)
        {
            _min = min;
            _max = max;
        }

        /// <inheritdoc />
        public override string Name => "in_range";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a) =>
            a.HasTarget && a.TargetDistance >= _min && a.TargetDistance <= _max ? BtStatus.Success : BtStatus.Failure;
    }

    /// <summary>
    /// Casts a skill (the attack animations of a creature only come from skills, user decision 2026-11-14): fails while its
    /// cooldown runs or when the cast is refused; Running until the skill reaches its End; then Success. The cooldown starts
    /// at the cast.
    /// </summary>
    public sealed class BtCast : BtNode
    {
        private readonly string _skill;
        private readonly long _cooldown;
        private long _readyAt = long.MinValue;
        private bool _casting;

        /// <summary>Creates the action.</summary>
        public BtCast(string skillId, double cooldownSeconds)
        {
            _skill = skillId;
            _cooldown = SkillTime.TickOf(cooldownSeconds);
        }

        /// <summary>Skill id.</summary>
        public string SkillId => _skill;

        /// <inheritdoc />
        public override string Name => "cast";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a)
        {
            if (_casting)
            {
                if (a.IsCasting) return BtStatus.Running;
                _casting = false;
                return BtStatus.Success;
            }
            if (a.Now < _readyAt || a.IsCasting) return BtStatus.Failure;
            a.Stop();
            a.FaceTarget();
            if (!a.TryCast(_skill)) return BtStatus.Failure;
            _readyAt = a.Now + _cooldown;
            _casting = true;
            return a.IsCasting ? BtStatus.Running : Done();
        }

        private BtStatus Done()
        {
            _casting = false;
            return BtStatus.Success;
        }

        // A cast that was interrupted by another branch still runs to its End on the creature; the node just stops waiting.
        /// <inheritdoc />
        public override void Reset() => _casting = false;
    }

    /// <summary>Walks toward the target until within <c>stop</c> (Success); Running meanwhile; fails without a target.</summary>
    public sealed class BtChase : BtNode
    {
        private readonly float _stop, _speed;

        /// <summary>Creates the action.</summary>
        public BtChase(float stopDistance, float speedScale)
        {
            _stop = stopDistance;
            _speed = speedScale;
        }

        /// <inheritdoc />
        public override string Name => "chase";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a)
        {
            if (!a.HasTarget) return BtStatus.Failure;
            if (a.TargetDistance <= _stop)
            {
                a.Stop();
                return BtStatus.Success;
            }
            a.MoveTowardTarget(_speed);
            return BtStatus.Running;
        }
    }

    /// <summary>Walks back and forth around home; always Running (a natural last child of the root selector).</summary>
    public sealed class BtPatrol : BtNode
    {
        /// <inheritdoc />
        public override string Name => "patrol";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a)
        {
            a.Patrol();
            return BtStatus.Running;
        }
    }

    /// <summary>Stands still for a time (Running), then Success.</summary>
    public sealed class BtWait : BtNode
    {
        private readonly long _length;
        private long _until = long.MinValue;

        /// <summary>Creates the action.</summary>
        public BtWait(double seconds) => _length = SkillTime.TickOf(seconds);

        /// <inheritdoc />
        public override string Name => "wait";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a)
        {
            if (_until == long.MinValue) _until = a.Now + _length;
            a.Stop();
            if (a.Now < _until) return BtStatus.Running;
            _until = long.MinValue;
            return BtStatus.Success;
        }

        /// <inheritdoc />
        public override void Reset() => _until = long.MinValue;
    }

    /// <summary>Turns toward the target; Success with one, Failure without.</summary>
    public sealed class BtFace : BtNode
    {
        /// <inheritdoc />
        public override string Name => "face";

        /// <inheritdoc />
        public override BtStatus Tick(IBtAgent a)
        {
            if (!a.HasTarget) return BtStatus.Failure;
            a.FaceTarget();
            return BtStatus.Success;
        }
    }

    /// <summary>
    /// A creature's behaviour tree, read from XML (<c>StreamingAssets/AI/&lt;name&gt;.xml</c>). Minimal node set (user decision
    /// 2026-11-14, full Enemy AI GDD later): <c>selector</c>, <c>sequence</c>, <c>in_range min max</c>, <c>cast skill cooldown</c>,
    /// <c>chase stop speed</c>, <c>patrol</c>, <c>wait seconds</c>, <c>face</c>.
    /// <code>
    /// &lt;behaviour schema_version="1"&gt;
    ///   &lt;selector&gt;
    ///     &lt;sequence&gt;&lt;in_range max="1.5"/&gt;&lt;cast skill="100001" cooldown="1.5"/&gt;&lt;/sequence&gt;
    ///     &lt;sequence&gt;&lt;in_range max="7"/&gt;&lt;chase stop="1.2"/&gt;&lt;/sequence&gt;
    ///     &lt;patrol/&gt;
    ///   &lt;/selector&gt;
    /// &lt;/behaviour&gt;
    /// </code>
    /// <see cref="Build"/> makes a fresh node tree (state is per creature). Main thread only.
    /// </summary>
    /// <example>
    /// <code>
    /// if (BehaviourTree.TryParse(xml, "goblin", out BehaviourTree bt, errors)) root = bt.Build();
    /// root.Tick(agent);   // every frame
    /// </code>
    /// </example>
    public sealed class BehaviourTree
    {
        /// <summary>Format version.</summary>
        public const int SchemaVersion = 1;

        private readonly Spec _root;

        private BehaviourTree(string name, Spec root, List<string> skills)
        {
            Name = name;
            _root = root;
            SkillIds = skills;
        }

        /// <summary>Tree name (file name).</summary>
        public string Name { get; }

        /// <summary>Every skill id the tree casts (to check they exist).</summary>
        public IReadOnlyList<string> SkillIds { get; }

        /// <summary>A new node tree with its own running state.</summary>
        public BtNode Build() => _root.Build();

        /// <summary>Reads and checks a tree. False (and errors) on anything wrong.</summary>
        public static bool TryParse(string xml, string name, out BehaviourTree tree, List<SkillError> errors)
        {
            tree = null;
            var ctx = new Ctx(errors, "behaviour:" + name);
            Spec root = null;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true, IgnoreWhitespace = true };
                using (XmlReader r = XmlReader.Create(new StringReader(xml ?? ""), settings))
                {
                    if (r.MoveToContent() != XmlNodeType.Element || r.LocalName != "behaviour")
                    {
                        ctx.Error("UnknownNode", "root must be <behaviour>");
                        return false;
                    }
                    string version = r.GetAttribute("schema_version");
                    if (!int.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out int v) || v < 1) ctx.Error("SchemaMissing", $"schema_version '{version}' must be a positive integer");
                    else if (v > SchemaVersion) ctx.Error("SchemaTooNew", $"schema_version {v} is newer than {SchemaVersion}");
                    var children = ReadChildren(r, ctx, "behaviour");
                    if (children.Count != 1) ctx.Error("RootCount", $"<behaviour> needs exactly one root node, found {children.Count}");
                    else root = children[0];
                }
            }
            catch (XmlException e)
            {
                ctx.Error("XmlUnreadable", e.Message);
            }
            if (ctx.Failed || root == null) return false;
            tree = new BehaviourTree(name, root, ctx.Skills);
            return true;
        }

        // ------------------------------------------------------------------ parsing

        private sealed class Spec
        {
            public string Kind;
            public float A, B;
            public string Skill;
            public List<Spec> Children = new List<Spec>();

            public BtNode Build()
            {
                switch (Kind)
                {
                    case "selector": return new BtSelector(BuildAll());
                    case "sequence": return new BtSequence(BuildAll());
                    case "in_range": return new BtInRange(A, B);
                    case "cast": return new BtCast(Skill, A);
                    case "chase": return new BtChase(A, B);
                    case "patrol": return new BtPatrol();
                    case "wait": return new BtWait(A);
                    default: return new BtFace();
                }
            }

            private BtNode[] BuildAll()
            {
                var nodes = new BtNode[Children.Count];
                for (int i = 0; i < nodes.Length; i++) nodes[i] = Children[i].Build();
                return nodes;
            }
        }

        private static List<Spec> ReadChildren(XmlReader r, Ctx ctx, string parent)
        {
            var list = new List<Spec>();
            if (r.IsEmptyElement) return list;
            int depth = r.Depth;
            r.Read();
            while (!(r.NodeType == XmlNodeType.EndElement && r.Depth == depth) && !r.EOF)
            {
                if (r.NodeType != XmlNodeType.Element)
                {
                    r.Read();
                    continue;
                }
                Spec s = ReadNode(r, ctx);
                if (s != null) list.Add(s);
                r.Read();
            }
            return list;
        }

        private static Spec ReadNode(XmlReader r, Ctx ctx)
        {
            var s = new Spec { Kind = r.LocalName };
            switch (s.Kind)
            {
                case "selector":
                case "sequence":
                    Attrs(r, ctx);
                    s.Children = ReadChildren(r, ctx, s.Kind);
                    if (s.Children.Count == 0) ctx.Error("EmptyComposite", $"<{s.Kind}> needs at least one child");
                    return s;
                case "in_range":
                    Attrs(r, ctx, "min", "max");
                    s.A = Num(r, ctx, "min", 0f);
                    s.B = Num(r, ctx, "max", float.PositiveInfinity);
                    if (s.A < 0 || s.B < s.A) ctx.Error("BadNumber", "<in_range>: need 0 ≤ min ≤ max");
                    break;
                case "cast":
                    Attrs(r, ctx, "skill", "cooldown");
                    s.Skill = r.GetAttribute("skill") ?? "";
                    if (!SkillXmlReader.IsSkillId(s.Skill)) ctx.Error("IdFormat", $"<cast>: skill '{s.Skill}' must be a six-digit skill id");
                    else ctx.Skills.Add(s.Skill);
                    s.A = Num(r, ctx, "cooldown", 0f);
                    if (s.A < 0) ctx.Error("BadNumber", "<cast>: cooldown must be ≥ 0");
                    break;
                case "chase":
                    Attrs(r, ctx, "stop", "speed");
                    s.A = Num(r, ctx, "stop", 1f);
                    s.B = Num(r, ctx, "speed", 1f);
                    if (s.A < 0 || s.B <= 0) ctx.Error("BadNumber", "<chase>: stop ≥ 0 and speed > 0");
                    break;
                case "wait":
                    Attrs(r, ctx, "seconds");
                    s.A = Num(r, ctx, "seconds", 1f);
                    if (s.A < 0) ctx.Error("BadNumber", "<wait>: seconds must be ≥ 0");
                    break;
                case "patrol":
                case "face":
                    Attrs(r, ctx);
                    break;
                default:
                    ctx.Error("UnknownNode", $"unknown node <{s.Kind}>");
                    r.Skip();
                    return null;
            }
            if (!r.IsEmptyElement)
            {
                ctx.Error("LeafHasChildren", $"<{s.Kind}> cannot have children");
                r.Skip();
            }
            return s;
        }

        private static void Attrs(XmlReader r, Ctx ctx, params string[] allowed)
        {
            if (!r.MoveToFirstAttribute()) return;
            do
            {
                if (Array.IndexOf(allowed, r.LocalName) < 0) ctx.Error("UnknownAttribute", $"attribute '{r.LocalName}' is not allowed here");
            } while (r.MoveToNextAttribute());
            r.MoveToElement();
        }

        private static float Num(XmlReader r, Ctx ctx, string attribute, float fallback)
        {
            string text = r.GetAttribute(attribute);
            if (text == null) return fallback;
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && !float.IsNaN(v)) return v;
            ctx.Error("BadNumber", $"<{r.LocalName}>: {attribute} '{text}' must be a number");
            return fallback;
        }

        private sealed class Ctx
        {
            private readonly List<SkillError> _errors;
            private readonly string _subject;
            public bool Failed;
            public readonly List<string> Skills = new List<string>();

            public Ctx(List<SkillError> errors, string subject)
            {
                _errors = errors;
                _subject = subject;
            }

            public void Error(string code, string message)
            {
                Failed = true;
                _errors?.Add(new SkillError(_subject, code, message));
            }
        }
    }
}

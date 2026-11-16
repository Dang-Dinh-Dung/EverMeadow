using System;
using System.Collections.Generic;
using System.Globalization;

namespace Evermeadow.Core.Skills
{
    /// <summary>Data type of a node field or output (GDD Skill &amp; Moveset Rule 6, 2026-11-09).</summary>
    public enum SkillFieldType
    {
        /// <summary>Number with decimals.</summary>
        Float,
        /// <summary>true / false.</summary>
        Bool,
        /// <summary>32-bit integer (ids of kinds: TID, levels, counts).</summary>
        Int,
        /// <summary>64-bit integer (runtime entity ids: GID).</summary>
        Long,
        /// <summary>Text (prefab, animation, buff keys).</summary>
        String,
        /// <summary>3D vector "x,y,z" (2D game: x, y play plane; z depth).</summary>
        Vec3,
        /// <summary>One value of an enum.</summary>
        Enum,
        /// <summary>A set of flags of a [Flags] enum.</summary>
        Flags,
        /// <summary>List of GIDs (outputs only).</summary>
        LongArray,
        /// <summary>List of positions (outputs only).</summary>
        Vec3Array
    }

    /// <summary>When a child node starts, relative to its parent (the condition written on a link).</summary>
    public enum SkillTrigger
    {
        /// <summary>When the parent node starts.</summary>
        Start,
        /// <summary>When the parent node ends.</summary>
        End,
        /// <summary>Every time the parent Hit hits a creature.</summary>
        Hit,
        /// <summary>When the parent Search finds at least one creature.</summary>
        Found,
        /// <summary>When the parent Search finds nothing.</summary>
        NotFound
    }

    /// <summary>Node kinds of a skill graph.</summary>
    public enum SkillNodeKind
    {
        /// <summary>Entry point; outputs the values known when the skill is cast.</summary>
        Start,
        /// <summary>Plays an animation.</summary>
        Animation,
        /// <summary>Spawns a visual effect prefab.</summary>
        Effect,
        /// <summary>Finds creatures in an area.</summary>
        Search,
        /// <summary>Damage area (instant or steady).</summary>
        Hit,
        /// <summary>Moves the owner to a destination.</summary>
        Move,
        /// <summary>Controls the owner's own movement / rotation while it runs.</summary>
        MoveControl,
        /// <summary>Turns the owner toward a target.</summary>
        LookAt,
        /// <summary>Camera shake / zoom / move.</summary>
        CameraAction,
        /// <summary>Pushes or pulls a creature.</summary>
        Knockback,
        /// <summary>Applies a buff to a creature.</summary>
        Buff,
        /// <summary>
        /// Ends the skill when it starts (user decision 2026-11-14): the owner's animation returns to idle, nodes not started
        /// yet are dropped, nodes already running (Hit, Effect, …) run to the end of their duration. No children.
        /// </summary>
        End
    }

    /// <summary>How a skill was triggered (Start output).</summary>
    public enum SkillInputType { Press, Hold, Release, Channel }

    /// <summary>What a pivot is attached to.</summary>
    public enum SkillPivotType { Creature, Position }

    /// <summary>How a pivot offset is oriented.</summary>
    public enum SkillOffsetType { None, LookOfTarget, LookOfOwner }

    /// <summary>Space of a front direction.</summary>
    public enum SkillSpaceType { Local, World }

    /// <summary>Shape of an area: Arc uses SubValue (angle°, radius, -), Box uses SubValue (width, length, height).</summary>
    public enum SkillAreaType { Arc, Box }

    /// <summary>Which creatures a Search or Hit considers.</summary>
    [Flags]
    public enum SkillSearchFlags
    {
        /// <summary>Nobody.</summary>
        None = 0,
        /// <summary>Hostile creatures.</summary>
        Enemies = 1,
        /// <summary>Friendly creatures.</summary>
        Allies = 2,
        /// <summary>The owner itself.</summary>
        Owner = 4,
        /// <summary>Neutral creatures (NPCs, critters).</summary>
        Neutral = 8
    }

    /// <summary>Rotation motion of an effect.</summary>
    public enum SkillRotateMotion { None, Spin }

    /// <summary>How an effect follows its pivot.</summary>
    public enum SkillBindType { None, AttachToTarget, Chase, Orbit }

    /// <summary>Damage formula family (defined by Combat).</summary>
    public enum SkillFactorType { Type1, Type2, Type3 }

    /// <summary>Hit material (picks hit sounds / sparks).</summary>
    public enum SkillMaterialType { Default, Metal, Wood, Flesh, Stone }

    /// <summary>Owner movement control while a MoveControl runs.</summary>
    public enum SkillMoveControlType { FixDirection, LockMove, SpeedRatio }

    /// <summary>Path of a Move.</summary>
    public enum SkillMovingType { Straight, Jump }

    /// <summary>What Move.Value means: seconds (Time) or tiles per second (Speed).</summary>
    public enum SkillMoveStandard { Time, Speed }

    /// <summary>Camera action.</summary>
    public enum SkillCameraAction { Shake, Zoom, Move }

    /// <summary>What the camera follows during the action.</summary>
    public enum SkillCameraFollow { Player, Owner, None }

    /// <summary>How the camera move offset is oriented.</summary>
    public enum SkillCameraMove { PlayerLookVector, Fixed }

    /// <summary>Direction of a knockback.</summary>
    public enum SkillKnockbackType { PushInDirection, PushFromCenter, PullToCenter }

    /// <summary>One editable field of a node; every field can also be wired from an output.</summary>
    public sealed class SkillFieldDef
    {
        /// <summary>Field name (XML <c>n</c>).</summary>
        public readonly string Name;
        /// <summary>Data type.</summary>
        public readonly SkillFieldType Type;
        /// <summary>Default value in canonical text form.</summary>
        public readonly string Default;
        /// <summary>Section header shown above the field in the tool (empty = common block).</summary>
        public readonly string Group;
        /// <summary>Enum type for Enum / Flags fields.</summary>
        public readonly Type EnumType;

        /// <summary>Creates a field definition.</summary>
        public SkillFieldDef(string name, SkillFieldType type, string def, string group = "", Type enumType = null)
        {
            Name = name;
            Type = type;
            Default = def;
            Group = group;
            EnumType = enumType;
        }
    }

    /// <summary>One output of a node, readable by later nodes as <c>"&lt;nodeId&gt;.&lt;Name&gt;"</c>.</summary>
    public sealed class SkillOutputDef
    {
        /// <summary>Output name.</summary>
        public readonly string Name;
        /// <summary>Data type.</summary>
        public readonly SkillFieldType Type;
        /// <summary>Enum type for Enum outputs.</summary>
        public readonly Type EnumType;

        /// <summary>Creates an output definition.</summary>
        public SkillOutputDef(string name, SkillFieldType type, Type enumType = null)
        {
            Name = name;
            Type = type;
            EnumType = enumType;
        }
    }

    /// <summary>Definition of a node kind: fields, outputs, and which triggers its outgoing links may use.</summary>
    public sealed class SkillNodeDef
    {
        /// <summary>Kind.</summary>
        public readonly SkillNodeKind Kind;
        /// <summary>Fields in display order.</summary>
        public readonly SkillFieldDef[] Fields;
        /// <summary>Outputs in display order.</summary>
        public readonly SkillOutputDef[] Outputs;
        /// <summary>Triggers allowed on links leaving this node.</summary>
        public readonly SkillTrigger[] Triggers;

        private readonly Dictionary<string, SkillFieldDef> _fields = new Dictionary<string, SkillFieldDef>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillOutputDef> _outputs = new Dictionary<string, SkillOutputDef>(StringComparer.Ordinal);

        /// <summary>Creates a node definition.</summary>
        public SkillNodeDef(SkillNodeKind kind, SkillFieldDef[] fields, SkillOutputDef[] outputs, SkillTrigger[] triggers)
        {
            Kind = kind;
            Fields = fields;
            Outputs = outputs;
            Triggers = triggers;
            foreach (SkillFieldDef f in fields) _fields.Add(f.Name, f);
            foreach (SkillOutputDef o in outputs) _outputs.Add(o.Name, o);
        }

        /// <summary>Field by name, or null.</summary>
        public SkillFieldDef Field(string name) => name != null && _fields.TryGetValue(name, out SkillFieldDef f) ? f : null;

        /// <summary>Output by name, or null.</summary>
        public SkillOutputDef Output(string name) => name != null && _outputs.TryGetValue(name, out SkillOutputDef o) ? o : null;

        /// <summary>True when links leaving this node may use <paramref name="trigger"/>.</summary>
        public bool Allows(SkillTrigger trigger) => Array.IndexOf(Triggers, trigger) >= 0;
    }

    /// <summary>
    /// The node catalogue of the Skill Tool (user decision 2026-11-09, modelled on the reference tool): every node kind with
    /// its fields, outputs and allowed link triggers, plus canonical text formats. Reader, writer, validator and the editor UI
    /// are all driven by this table, so a new node kind is one entry here (plus its runtime behaviour).
    /// </summary>
    /// <example>
    /// <code>
    /// SkillNodeDef hit = SkillSchema.Get(SkillNodeKind.Hit);
    /// foreach (SkillFieldDef f in hit.Fields) Console.WriteLine($"{f.Group} / {f.Name} : {f.Type} = {f.Default}");
    /// </code>
    /// </example>
    public static class SkillSchema
    {
        private static readonly SkillTrigger[] StartOnly = { SkillTrigger.Start };
        private static readonly SkillTrigger[] StartEnd = { SkillTrigger.Start, SkillTrigger.End };
        private static readonly Dictionary<SkillNodeKind, SkillNodeDef> Defs = new Dictionary<SkillNodeKind, SkillNodeDef>();

        /// <summary>Every node kind, in palette order.</summary>
        public static readonly SkillNodeKind[] Kinds =
        {
            SkillNodeKind.Animation, SkillNodeKind.Effect, SkillNodeKind.Search, SkillNodeKind.Hit, SkillNodeKind.Move,
            SkillNodeKind.MoveControl, SkillNodeKind.LookAt, SkillNodeKind.CameraAction, SkillNodeKind.Knockback, SkillNodeKind.Buff,
            SkillNodeKind.End
        };

        static SkillSchema()
        {
            Add(SkillNodeKind.Start, new SkillFieldDef[0], new[]
            {
                O("SkillID", SkillFieldType.Int), O("SkillLevel", SkillFieldType.Int), O("CastRatio", SkillFieldType.Int),
                new SkillOutputDef("SkillInputType", SkillFieldType.Enum, typeof(SkillInputType)), O("IsChanneling", SkillFieldType.Bool),
                O("OwnerGID", SkillFieldType.Long), O("OwnerTID", SkillFieldType.Int), O("OwnerPosition", SkillFieldType.Vec3),
                O("OwnerDirection", SkillFieldType.Vec3), O("OwnerLevel", SkillFieldType.Int), O("ExternalTargetGID", SkillFieldType.Long),
                O("ExternalPosition", SkillFieldType.Vec3)
            }, StartOnly);

            Add(SkillNodeKind.Animation, Fields(
                F("AnimationType", SkillFieldType.String, "", "Animation"),
                F("IsLoop", SkillFieldType.Bool, "false", "Animation"),
                F("TimeScale", SkillFieldType.Float, "1", "Animation")), Outs(), StartEnd);

            Add(SkillNodeKind.Effect, Fields(Concat(new[]
            {
                F("PrefabName", SkillFieldType.String, "", "Effect"),
                F("Duration", SkillFieldType.Float, "1", "Effect"),
                F("Scale", SkillFieldType.Float, "1", "Effect")
            }, Pivot("", "Effect spawn position"), Direction(), new[]
            {
                E("RotateMotion", typeof(SkillRotateMotion), "None", "Rotation"),
                F("RotateDegreePerSec", SkillFieldType.Float, "0", "Rotation"),
                F("IsReverse", SkillFieldType.Bool, "false", "Rotation"),
                F("IsRotateSteady", SkillFieldType.Bool, "false", "Rotation"),
                E("BindType", typeof(SkillBindType), "AttachToTarget", "Position"),
                F("ChaseSpeed", SkillFieldType.Float, "1", "Position"),
                F("OrbitRadius", SkillFieldType.Float, "0", "Position"),
                F("OrbitDegreePerSec", SkillFieldType.Float, "0", "Position"),
                F("OrbitStartAngle", SkillFieldType.Float, "0", "Position"),
                F("IsReleaseWhenOwnerDie", SkillFieldType.Bool, "true", "Release")
            })), Outs(O("LastEffectPosition", SkillFieldType.Vec3)), StartEnd);

            Add(SkillNodeKind.Search, Fields(Concat(new[]
            {
                Flags("SearchObjectFlag", "Enemies", "Search target"),
                F("TargetTID", SkillFieldType.Int, "0", "Search target"),
                F("IsPrioritizeOwnerLook", SkillFieldType.Bool, "false", "Search target"),
                F("MaxCount", SkillFieldType.Int, "1", "Search target")
            }, Pivot("", "Search position"), Area("Search area"), Direction())),
                Outs(O("Count", SkillFieldType.Int), O("CreatureGIDs", SkillFieldType.LongArray), O("CreaturePositions", SkillFieldType.Vec3Array)),
                new[] { SkillTrigger.Start, SkillTrigger.End, SkillTrigger.Found, SkillTrigger.NotFound });

            Add(SkillNodeKind.Hit, Fields(Concat(new[]
            {
                Flags("SearchObjectFlag", "Enemies", "Target"),
                F("IsSteady", SkillFieldType.Bool, "false", "Steady"),
                F("IsFollowPivot", SkillFieldType.Bool, "true", "Steady"),
                F("SteadyDuration", SkillFieldType.Float, "1", "Steady"),
                F("RepeatTime", SkillFieldType.Float, "0.1", "Steady"),
                F("IsReleaseWhenOwnerDie", SkillFieldType.Bool, "false", "Steady"),
                F("IsRotate", SkillFieldType.Bool, "false", "Steady"),
                F("Radius", SkillFieldType.Float, "0", "Steady"),
                F("RotateDegreePerSec", SkillFieldType.Float, "0", "Steady"),
                F("StartAngle", SkillFieldType.Float, "0", "Steady"),
                E("FactorType", typeof(SkillFactorType), "Type1", "Factor"),
                F("FactorRatio", SkillFieldType.Int, "10000", "Factor"),
                E("SkillMaterialType", typeof(SkillMaterialType), "Default", "Hit effect"),
                F("HitEffectAddressable", SkillFieldType.String, "", "Hit effect"),
                F("HitEffectDuration", SkillFieldType.Float, "0.5", "Hit effect")
            }, Pivot("", "Area"), Area("Area"), new[]
            {
                F("BlockerTIDs", SkillFieldType.String, "", "Area")
            }, Direction(), new[]
            {
                F("InstantKill", SkillFieldType.Bool, "false", "Instant kill"),
                F("InstantKillHpThreshold", SkillFieldType.Int, "1000", "Instant kill"),
                F("InstantKillRatio", SkillFieldType.Int, "0", "Instant kill")
            })), Outs(O("LastHitCreatureGID", SkillFieldType.Long), O("LastHitCreaturePosition", SkillFieldType.Vec3),
                O("DistinctHitCreatureGIDs", SkillFieldType.LongArray), O("DistinctHitCreaturePositions", SkillFieldType.Vec3Array),
                O("IndistinctHitCount", SkillFieldType.Int), O("LastInstantKillPosition", SkillFieldType.Vec3)),
                new[] { SkillTrigger.Start, SkillTrigger.End, SkillTrigger.Hit });

            Add(SkillNodeKind.Move, Fields(Concat(Pivot("", "Destination"), new[]
            {
                E("MovingType", typeof(SkillMovingType), "Straight", "Moving"),
                E("MoveStandard", typeof(SkillMoveStandard), "Time", "Moving"),
                F("Value", SkillFieldType.Float, "1", "Moving")
            })), Outs(O("StartPosition", SkillFieldType.Vec3), O("EndPosition", SkillFieldType.Vec3)), StartEnd);

            Add(SkillNodeKind.MoveControl, Fields(
                E("MoveType", typeof(SkillMoveControlType), "FixDirection", "Movement and rotation control"),
                F("Ratio", SkillFieldType.Float, "1", "Acceleration / deceleration"),
                F("Duration", SkillFieldType.Float, "0", "Acceleration / deceleration")), Outs(), StartEnd);

            Add(SkillNodeKind.LookAt, Fields(Pivot("", "Look target")), Outs(O("Direction", SkillFieldType.Vec3)), StartEnd);

            Add(SkillNodeKind.CameraAction, Fields(
                E("ActionType", typeof(SkillCameraAction), "Shake", "Camera"),
                F("Duration", SkillFieldType.Float, "1", "Camera"),
                E("FollowType", typeof(SkillCameraFollow), "Player", "Camera"),
                E("MoveType", typeof(SkillCameraMove), "PlayerLookVector", "Camera"),
                F("MoveOffset", SkillFieldType.Vec3, "0,0,0", "Camera"),
                F("DampingX", SkillFieldType.Float, "0.15", "Camera"),
                F("DampingY", SkillFieldType.Float, "0.3", "Camera"),
                F("DampingZ", SkillFieldType.Float, "0.15", "Camera"),
                F("IsShakeBroadcast", SkillFieldType.Bool, "true", "Shake"),
                F("ShakeAmplitude", SkillFieldType.Float, "0.05", "Shake"),
                F("ShakeFrequency", SkillFieldType.Float, "10", "Shake"),
                F("ZoomSize", SkillFieldType.Float, "0", "Zoom"),
                F("KnockbackOffset", SkillFieldType.Vec3, "0,0,0", "Zoom")), Outs(), StartEnd);

            Add(SkillNodeKind.Knockback, Fields(Concat(new[]
            {
                F("TargetCreatureGID", SkillFieldType.Long, "-1", "Knockback"),
                F("Distance", SkillFieldType.Float, "1", "Knockback"),
                F("Duration", SkillFieldType.Float, "0.3", "Knockback"),
                E("KnockbackType", typeof(SkillKnockbackType), "PushInDirection", "Knockback")
            }, Direction(), Pivot("Center", "Knockback center"))), Outs(), StartEnd);

            Add(SkillNodeKind.Buff, Fields(
                F("TargetCreatureGID", SkillFieldType.Long, "-1", "Buff"),
                F("StackCount", SkillFieldType.Int, "1", "Buff"),
                F("BuffType", SkillFieldType.String, "", "Buff"),
                F("Duration", SkillFieldType.Float, "5", "Buff"),
                F("Parameter0", SkillFieldType.Int, "0", "Parameters"),
                F("Parameter1", SkillFieldType.Int, "0", "Parameters"),
                F("Parameter2", SkillFieldType.Int, "0", "Parameters"),
                F("Parameter3", SkillFieldType.Int, "0", "Parameters"),
                F("Parameter4", SkillFieldType.Int, "0", "Parameters"),
                F("Parameter5", SkillFieldType.Int, "0", "Parameters")), Outs(), StartEnd);

            Add(SkillNodeKind.End, Fields(), Outs(), NoChildren);
        }

        /// <summary>Definition of a node kind.</summary>
        public static SkillNodeDef Get(SkillNodeKind kind) => Defs[kind];

        private static readonly SkillTrigger[] NoChildren = new SkillTrigger[0];

        /// <summary>Parses a node kind name (XML <c>type</c>).</summary>
        public static bool TryParseKind(string name, out SkillNodeKind kind) =>
            Enum.TryParse(name, false, out kind) && Enum.IsDefined(typeof(SkillNodeKind), kind) && !int.TryParse(name, out _);

        /// <summary>XML name of a trigger.</summary>
        public static string NameOf(SkillTrigger t) => t == SkillTrigger.NotFound ? "not_found" : t.ToString().ToLowerInvariant();

        /// <summary>Parses a trigger name.</summary>
        public static bool TryParseTrigger(string name, out SkillTrigger t)
        {
            switch (name)
            {
                case "start": t = SkillTrigger.Start; return true;
                case "end": t = SkillTrigger.End; return true;
                case "hit": t = SkillTrigger.Hit; return true;
                case "found": t = SkillTrigger.Found; return true;
                case "not_found": t = SkillTrigger.NotFound; return true;
                default: t = SkillTrigger.Start; return false;
            }
        }

        /// <summary>True when an output of type <paramref name="from"/> may be wired into a field of type <paramref name="to"/>.</summary>
        public static bool CanAssign(SkillFieldType from, Type fromEnum, SkillFieldType to, Type toEnum)
        {
            if (from == to) return from != SkillFieldType.Enum && from != SkillFieldType.Flags || fromEnum == toEnum;
            return from == SkillFieldType.Int && (to == SkillFieldType.Float || to == SkillFieldType.Long);
        }

        /// <summary>Checks a value in canonical text form; on success <paramref name="canonical"/> is the normalised text.</summary>
        public static bool TryNormalize(SkillFieldType type, Type enumType, string text, out string canonical)
        {
            canonical = null;
            text = text?.Trim() ?? "";
            switch (type)
            {
                case SkillFieldType.Float:
                    if (!TryFloat(text, out double d)) return false;
                    canonical = Num(d);
                    return true;
                case SkillFieldType.Bool:
                    if (text != "true" && text != "false") return false;
                    canonical = text;
                    return true;
                case SkillFieldType.Int:
                    if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int i)) return false;
                    canonical = i.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SkillFieldType.Long:
                    if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long l)) return false;
                    canonical = l.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SkillFieldType.String:
                    canonical = text;
                    return true;
                case SkillFieldType.Vec3:
                    string[] p = text.Split(',');
                    if (p.Length != 3 || !TryFloat(p[0], out double x) || !TryFloat(p[1], out double y) || !TryFloat(p[2], out double z)) return false;
                    canonical = Num(x) + "," + Num(y) + "," + Num(z);
                    return true;
                case SkillFieldType.Enum:
                    if (enumType == null || !Enum.IsDefined(enumType, text) || int.TryParse(text, out _)) return false;
                    canonical = text;
                    return true;
                case SkillFieldType.Flags:
                    return TryFlags(enumType, text, out canonical);
                default:
                    return false;   // arrays are outputs only
            }
        }

        /// <summary>Formats a number the way skill files do: invariant, rounded to 1e-6, no trailing zeros.</summary>
        public static string Num(double value) =>
            Math.Round(value, 6, MidpointRounding.AwayFromZero).ToString("0.######", CultureInfo.InvariantCulture);

        /// <summary>Parses a [Flags] value "A|B" (or "None") into its canonical text, in declaration order.</summary>
        public static bool TryFlags(Type enumType, string text, out string canonical)
        {
            canonical = null;
            if (enumType == null) return false;
            long bits = 0;
            foreach (string part in (text ?? "").Split('|'))
            {
                string name = part.Trim();
                if (name.Length == 0 || !Enum.IsDefined(enumType, name) || int.TryParse(name, out _)) return false;
                bits |= Convert.ToInt64(Enum.Parse(enumType, name));
            }
            canonical = FlagsText(enumType, bits);
            return true;
        }

        /// <summary>Canonical text of a flags value.</summary>
        public static string FlagsText(Type enumType, long bits)
        {
            if (bits == 0) return Enum.GetName(enumType, 0) ?? "None";
            var names = new List<string>();
            foreach (object v in Enum.GetValues(enumType))
            {
                long b = Convert.ToInt64(v);
                if (b != 0 && (bits & b) == b) names.Add(Enum.GetName(enumType, v));
            }
            return string.Join("|", names);
        }

        private static bool TryFloat(string s, out double v) =>
            double.TryParse(s?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);

        // ------------------------------------------------------------------ table helpers

        private static void Add(SkillNodeKind kind, SkillFieldDef[] fields, SkillOutputDef[] outputs, SkillTrigger[] triggers) =>
            Defs.Add(kind, new SkillNodeDef(kind, fields, outputs, triggers));

        // Every node except Start starts with the common block and ends with NodeID.
        private static SkillFieldDef[] Fields(params SkillFieldDef[] own) => Concat(new[]
        {
            F("ActionDelay", SkillFieldType.Float, "0"),
            F("CanCancel", SkillFieldType.Bool, "true"),
            F("UseAttackSpeed", SkillFieldType.Bool, "true")
        }, own);

        private static SkillOutputDef[] Outs(params SkillOutputDef[] own)
        {
            var all = new SkillOutputDef[own.Length + 1];
            all[0] = O("NodeID", SkillFieldType.Int);
            own.CopyTo(all, 1);
            return all;
        }

        private static SkillFieldDef[] Pivot(string prefix, string group) => new[]
        {
            E(prefix + "PivotType", typeof(SkillPivotType), "Creature", group),
            F(prefix + "PivotCreatureGID", SkillFieldType.Long, "-1", group),
            F(prefix + "PivotPosition", SkillFieldType.Vec3, "0,0,0", group),
            E(prefix + "PivotOffsetType", typeof(SkillOffsetType), "None", group),
            F(prefix + "PivotOffsetNormalized", SkillFieldType.Vec3, "0,0,0", group),
            F(prefix + "PivotOffset", SkillFieldType.Vec3, "0,0,0", group)
        };

        private static SkillFieldDef[] Direction() => new[]
        {
            E("SpaceType", typeof(SkillSpaceType), "Local", "Direction"),
            F("SpaceFrontDirection", SkillFieldType.Vec3, "0,0,1", "Direction")
        };

        private static SkillFieldDef[] Area(string group) => new[]
        {
            E("AreaType", typeof(SkillAreaType), "Arc", group),
            F("SubValue", SkillFieldType.Vec3, "360,3,0", group),
            F("AreaScale", SkillFieldType.Float, "1", group)
        };

        private static SkillFieldDef[] Concat(params SkillFieldDef[][] parts)
        {
            var list = new List<SkillFieldDef>();
            foreach (SkillFieldDef[] p in parts) list.AddRange(p);
            return list.ToArray();
        }

        private static SkillFieldDef F(string name, SkillFieldType type, string def, string group = "") => new SkillFieldDef(name, type, def, group);

        private static SkillFieldDef E(string name, Type enumType, string def, string group) => new SkillFieldDef(name, SkillFieldType.Enum, def, group, enumType);

        private static SkillFieldDef Flags(string name, string def, string group) =>
            new SkillFieldDef(name, SkillFieldType.Flags, def, group, typeof(SkillSearchFlags));

        private static SkillOutputDef O(string name, SkillFieldType type) => new SkillOutputDef(name, type);
    }
}

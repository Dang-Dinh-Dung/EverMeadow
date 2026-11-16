using System;

namespace Evermeadow.Core.Skills
{
    /// <summary>
    /// Which fields of a node take effect given its other fields (user request 2026-11-14: the Skill Tool greys out and locks
    /// fields that would be ignored, e.g. the Steady fields of a Hit while IsSteady is off). Values of inactive fields are kept
    /// in the file but have no effect. Pure, no allocation; the Skill Tool asks after every change.
    /// </summary>
    /// <example>
    /// <code>
    /// bool editable = SkillFieldRules.IsActive(hitNode, "SteadyDuration", out string why);   // false, "chỉ dùng khi IsSteady bật"
    /// </code>
    /// </example>
    public static class SkillFieldRules
    {
        private static readonly string[] SteadyFields = { "IsFollowPivot", "SteadyDuration", "RepeatTime", "IsReleaseWhenOwnerDie", "IsRotate", "Radius", "RotateDegreePerSec", "StartAngle" };
        private static readonly string[] RotateFields = { "Radius", "RotateDegreePerSec", "StartAngle" };
        private static readonly string[] OrbitFields = { "OrbitRadius", "OrbitDegreePerSec", "OrbitStartAngle" };
        private static readonly string[] SpinFields = { "RotateDegreePerSec", "IsReverse", "IsRotateSteady" };
        private static readonly string[] CameraMoveFields = { "MoveType", "MoveOffset", "DampingX", "DampingY", "DampingZ" };
        private static readonly string[] CameraShakeFields = { "IsShakeBroadcast", "ShakeAmplitude", "ShakeFrequency" };
        private static readonly string[] CameraZoomFields = { "ZoomSize", "KnockbackOffset" };
        private static readonly string[] DirectionFields = { "SpaceType", "SpaceFrontDirection" };

        /// <summary>True when <paramref name="field"/> of <paramref name="node"/> takes effect.</summary>
        public static bool IsActive(SkillNodeData node, string field) => IsActive(node, field, out _);

        /// <summary>True when <paramref name="field"/> takes effect; otherwise <paramref name="why"/> says what enables it.</summary>
        public static bool IsActive(SkillNodeData node, string field, out string why)
        {
            why = "";
            if (node == null || string.IsNullOrEmpty(field)) return true;

            // Pivot of any prefix ("", "Center"): a creature pivot ignores the position, a position pivot ignores the creature.
            if (field.EndsWith("PivotPosition", StringComparison.Ordinal))
                return Need(node.Get(field.Substring(0, field.Length - "PivotPosition".Length) + "PivotType") == nameof(SkillPivotType.Position),
                    "chỉ dùng khi PivotType = Position", out why);
            if (field.EndsWith("PivotCreatureGID", StringComparison.Ordinal))
                return Need(node.Get(field.Substring(0, field.Length - "PivotCreatureGID".Length) + "PivotType") != nameof(SkillPivotType.Position),
                    "chỉ dùng khi PivotType = Creature", out why);

            switch (node.Kind)
            {
                case SkillNodeKind.Hit:
                    if (Is(field, RotateFields) && On(node, "IsSteady") && !On(node, "IsRotate"))
                        return Need(false, "chỉ dùng khi IsRotate bật", out why);
                    if (Is(field, SteadyFields)) return Need(On(node, "IsSteady"), "chỉ dùng khi IsSteady bật (đánh kéo dài)", out why);
                    if (field == "InstantKillHpThreshold" || field == "InstantKillRatio") return Need(On(node, "InstantKill"), "chỉ dùng khi InstantKill bật", out why);
                    if (field == "HitEffectDuration") return Need(node.Get("HitEffectAddressable").Length > 0, "chỉ dùng khi đã chọn HitEffectAddressable", out why);
                    return true;

                case SkillNodeKind.Effect:
                    if (Is(field, SpinFields)) return Need(node.Get("RotateMotion") != nameof(SkillRotateMotion.None), "chỉ dùng khi RotateMotion khác None", out why);
                    if (field == "ChaseSpeed") return Need(node.Get("BindType") == nameof(SkillBindType.Chase), "chỉ dùng khi BindType = Chase", out why);
                    if (Is(field, OrbitFields)) return Need(node.Get("BindType") == nameof(SkillBindType.Orbit), "chỉ dùng khi BindType = Orbit", out why);
                    return true;

                case SkillNodeKind.Knockback:
                    bool inDirection = node.Get("KnockbackType") == nameof(SkillKnockbackType.PushInDirection);
                    if (field.StartsWith("CenterPivot", StringComparison.Ordinal)) return Need(!inDirection, "chỉ dùng khi KnockbackType đẩy ra / kéo về tâm", out why);
                    if (Is(field, DirectionFields)) return Need(inDirection, "chỉ dùng khi KnockbackType = PushInDirection", out why);
                    return true;

                case SkillNodeKind.CameraAction:
                    string action = node.Get("ActionType");
                    if (Is(field, CameraMoveFields)) return Need(action == nameof(SkillCameraAction.Move), "chỉ dùng khi ActionType = Move", out why);
                    if (Is(field, CameraShakeFields)) return Need(action == nameof(SkillCameraAction.Shake), "chỉ dùng khi ActionType = Shake", out why);
                    if (Is(field, CameraZoomFields)) return Need(action == nameof(SkillCameraAction.Zoom), "chỉ dùng khi ActionType = Zoom", out why);
                    return true;

                case SkillNodeKind.MoveControl:
                    if (field == "Ratio") return Need(node.Get("MoveType") == nameof(SkillMoveControlType.SpeedRatio), "chỉ dùng khi MoveType = SpeedRatio", out why);
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>Which of SubValue's three numbers take effect: an Arc uses angle and radius only, a Box all three.</summary>
        public static bool SubValuePartActive(SkillNodeData node, int part) =>
            part < 2 || node == null || node.Get("AreaType") == nameof(SkillAreaType.Box);

        private static bool On(SkillNodeData n, string field) => n.Get(field) == "true";

        private static bool Is(string field, string[] set) => Array.IndexOf(set, field) >= 0;

        private static bool Need(bool active, string reason, out string why)
        {
            why = active ? "" : reason;
            return active;
        }
    }
}

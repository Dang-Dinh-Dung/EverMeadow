using Evermeadow.Core.Skills;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>Which node fields take effect (user request 2026-11-14: the Skill Tool greys out fields that would be ignored).</summary>
    public class SkillFieldRulesTests
    {
        private static SkillNodeData N(SkillNodeKind kind, params (string field, string value)[] values)
        {
            var n = new SkillNodeData { Id = 1, Kind = kind };
            foreach ((string f, string v) in values) n.Values[f] = v;
            return n;
        }

        [TestCase("SteadyDuration")]
        [TestCase("RepeatTime")]
        [TestCase("IsFollowPivot")]
        [TestCase("IsReleaseWhenOwnerDie")]
        [TestCase("IsRotate")]
        [TestCase("Radius")]
        [TestCase("RotateDegreePerSec")]
        [TestCase("StartAngle")]
        public void Hit_SteadyFields_OnlyWithIsSteady(string field)
        {
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), field, out string why), "IsSteady defaults to off");
            StringAssert.Contains("IsSteady", why);
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit, ("IsSteady", "true"), ("IsRotate", "true")), field));
        }

        [Test]
        public void Hit_RotateFields_NeedIsRotateToo()
        {
            SkillNodeData steady = N(SkillNodeKind.Hit, ("IsSteady", "true"));
            Assert.IsTrue(SkillFieldRules.IsActive(steady, "SteadyDuration"));
            Assert.IsTrue(SkillFieldRules.IsActive(steady, "IsRotate"));
            Assert.IsFalse(SkillFieldRules.IsActive(steady, "Radius", out string why));
            StringAssert.Contains("IsRotate", why);
        }

        [Test]
        public void Hit_InstantKillAndHitEffect()
        {
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), "InstantKillRatio"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit, ("InstantKill", "true")), "InstantKillHpThreshold"));
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), "HitEffectDuration"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit, ("HitEffectAddressable", "vfx_cmn_hit")), "HitEffectDuration"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), "SubValue"), "fields without a rule are always active");
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), "ActionDelay"));
        }

        [Test]
        public void Pivot_CreatureOrPosition_AnyPrefix()
        {
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Effect), "PivotPosition"), "default PivotType = Creature");
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Effect), "PivotCreatureGID"));
            SkillNodeData pos = N(SkillNodeKind.Hit, ("PivotType", "Position"));
            Assert.IsTrue(SkillFieldRules.IsActive(pos, "PivotPosition"));
            Assert.IsFalse(SkillFieldRules.IsActive(pos, "PivotCreatureGID"));
            SkillNodeData kb = N(SkillNodeKind.Knockback, ("KnockbackType", "PushFromCenter"), ("CenterPivotType", "Position"));
            Assert.IsTrue(SkillFieldRules.IsActive(kb, "CenterPivotPosition"));
            Assert.IsFalse(SkillFieldRules.IsActive(kb, "CenterPivotCreatureGID"));
        }

        [Test]
        public void Effect_RotationAndBind()
        {
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Effect), "IsReverse"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Effect, ("RotateMotion", "Spin")), "IsReverse"));
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Effect), "ChaseSpeed"), "default BindType = AttachToTarget");
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Effect), "OrbitRadius"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Effect, ("BindType", "Chase")), "ChaseSpeed"));
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.Effect, ("BindType", "Chase")), "OrbitStartAngle"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Effect, ("BindType", "Orbit")), "OrbitDegreePerSec"));
        }

        [Test]
        public void Knockback_DirectionOrCenter()
        {
            SkillNodeData push = N(SkillNodeKind.Knockback);
            Assert.IsTrue(SkillFieldRules.IsActive(push, "SpaceType"));
            Assert.IsFalse(SkillFieldRules.IsActive(push, "CenterPivotType"));
            Assert.IsFalse(SkillFieldRules.IsActive(push, "CenterPivotOffset"));
            SkillNodeData pull = N(SkillNodeKind.Knockback, ("KnockbackType", "PullToCenter"));
            Assert.IsFalse(SkillFieldRules.IsActive(pull, "SpaceFrontDirection"));
            Assert.IsTrue(SkillFieldRules.IsActive(pull, "CenterPivotType"));
            Assert.IsTrue(SkillFieldRules.IsActive(pull, "Distance"));
        }

        [TestCase("Shake", "ShakeAmplitude", true)]
        [TestCase("Shake", "ZoomSize", false)]
        [TestCase("Shake", "DampingX", false)]
        [TestCase("Zoom", "KnockbackOffset", true)]
        [TestCase("Zoom", "IsShakeBroadcast", false)]
        [TestCase("Move", "MoveOffset", true)]
        [TestCase("Move", "ShakeFrequency", false)]
        [TestCase("Move", "Duration", true)]
        [TestCase("Zoom", "FollowType", true)]
        public void Camera_OnlyTheChosenAction(string action, string field, bool active) =>
            Assert.AreEqual(active, SkillFieldRules.IsActive(N(SkillNodeKind.CameraAction, ("ActionType", action)), field));

        [Test]
        public void MoveControl_RatioOnlyForSpeedRatio()
        {
            Assert.IsFalse(SkillFieldRules.IsActive(N(SkillNodeKind.MoveControl), "Ratio"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.MoveControl, ("MoveType", "SpeedRatio")), "Ratio"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.MoveControl), "Duration"));
        }

        [Test]
        public void SubValue_ArcIgnoresThirdNumber()
        {
            SkillNodeData arc = N(SkillNodeKind.Hit);
            Assert.IsTrue(SkillFieldRules.SubValuePartActive(arc, 0));
            Assert.IsTrue(SkillFieldRules.SubValuePartActive(arc, 1));
            Assert.IsFalse(SkillFieldRules.SubValuePartActive(arc, 2));
            Assert.IsTrue(SkillFieldRules.SubValuePartActive(N(SkillNodeKind.Hit, ("AreaType", "Box")), 2));
        }

        [Test]
        public void NullOrEmpty_Active()
        {
            Assert.IsTrue(SkillFieldRules.IsActive(null, "x"));
            Assert.IsTrue(SkillFieldRules.IsActive(N(SkillNodeKind.Hit), ""));
        }
    }
}

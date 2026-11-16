using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.AI;
using Evermeadow.Core.Skills;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.AI
{
    /// <summary>
    /// Minimal behaviour tree for creatures (user decision 2026-11-14): creatures attack only through skills, chosen by a
    /// tree read from data (selector, sequence, in_range, cast, chase, patrol, wait, face).
    /// </summary>
    public class BehaviourTreeTests
    {
        private static long T(double s) => SkillTime.TickOf(s);

        private sealed class Agent : IBtAgent
        {
            public long Now { get; set; }
            public bool HasTarget { get; set; } = true;
            public float TargetDistance { get; set; } = 10f;
            public bool IsCasting { get; set; }
            public bool RefuseCast;
            public readonly List<string> Log = new List<string>();

            public bool TryCast(string skillId)
            {
                Log.Add("cast " + skillId);
                if (RefuseCast) return false;
                IsCasting = true;
                return true;
            }

            public void MoveTowardTarget(float speedScale) => Log.Add("chase");
            public void Patrol() => Log.Add("patrol");
            public void Stop() => Log.Add("stop");
            public void FaceTarget() => Log.Add("face");
        }

        private const string Goblin = @"<behaviour schema_version=""1"">
  <selector>
    <sequence><in_range max=""1.5""/><cast skill=""100001"" cooldown=""2""/></sequence>
    <sequence><in_range max=""7""/><chase stop=""1.2""/></sequence>
    <patrol/>
  </selector>
</behaviour>";

        private static BtNode Build(string xml)
        {
            var errors = new List<SkillError>();
            Assert.IsTrue(BehaviourTree.TryParse(xml, "goblin", out BehaviourTree bt, errors), string.Join("\n", errors));
            return bt.Build();
        }

        private static List<string> Codes(string xml)
        {
            var errors = new List<SkillError>();
            BehaviourTree.TryParse(xml, "t", out _, errors);
            return errors.Select(e => e.Code).ToList();
        }

        [Test]
        public void Goblin_FarAway_Patrols()
        {
            BtNode root = Build(Goblin);
            var a = new Agent { TargetDistance = 20 };
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "patrol" }, a.Log);
        }

        [Test]
        public void Goblin_NoTarget_Patrols()
        {
            BtNode root = Build(Goblin);
            var a = new Agent { HasTarget = false };
            root.Tick(a);
            CollectionAssert.AreEqual(new[] { "patrol" }, a.Log);
        }

        [Test]
        public void Goblin_InSight_Chases_ThenAttacksWhenClose()
        {
            BtNode root = Build(Goblin);
            var a = new Agent { TargetDistance = 5 };
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "chase" }, a.Log);

            a.Log.Clear();
            a.TargetDistance = 1;
            Assert.AreEqual(BtStatus.Running, root.Tick(a), "cast started, skill running");
            CollectionAssert.AreEqual(new[] { "stop", "face", "cast 100001" }, a.Log);
        }

        [Test]
        public void Cast_RunningUntilSkillEnds_ThenCooldownBlocksIt()
        {
            BtNode root = Build(Goblin);
            var a = new Agent { TargetDistance = 1 };
            root.Tick(a);
            a.Log.Clear();
            a.Now = T(0.5);
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.IsEmpty(a.Log, "waiting for the skill: no new orders");

            a.IsCasting = false;   // skill reached its End
            a.Now = T(0.8);
            Assert.AreEqual(BtStatus.Success, root.Tick(a));

            a.Log.Clear();
            a.Now = T(1.0);
            Assert.AreEqual(BtStatus.Success, root.Tick(a), "cast on cooldown → next branch: chase, already close = success");
            CollectionAssert.AreEqual(new[] { "stop" }, a.Log);

            a.Log.Clear();
            a.Now = T(2.0);
            root.Tick(a);
            CollectionAssert.AreEqual(new[] { "stop", "face", "cast 100001" }, a.Log, "cooldown counted from the cast (0 + 2 s)");
        }

        [Test]
        public void Cast_Refused_FailsToNextBranch()
        {
            BtNode root = Build(Goblin);
            var a = new Agent { TargetDistance = 1, RefuseCast = true };
            Assert.AreEqual(BtStatus.Success, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "stop", "face", "cast 100001", "stop" }, a.Log, "refused cast → chase branch (already within stop)");
        }

        [TestCase(1.5f, BtStatus.Success)]
        [TestCase(1.5001f, BtStatus.Failure)]
        [TestCase(0.5f, BtStatus.Success)]
        [TestCase(0.4999f, BtStatus.Failure)]
        public void InRange_BoundsInclusive(float distance, BtStatus expected) =>
            Assert.AreEqual(expected, new BtInRange(0.5f, 1.5f).Tick(new Agent { TargetDistance = distance }));

        [Test]
        public void Chase_ExactlyAtStop_StopsWithSuccess()
        {
            var a = new Agent { TargetDistance = 1.2f };
            Assert.AreEqual(BtStatus.Success, new BtChase(1.2f, 1f).Tick(a));
            CollectionAssert.AreEqual(new[] { "stop" }, a.Log);
            a.Log.Clear();
            a.TargetDistance = 1.2001f;
            Assert.AreEqual(BtStatus.Running, new BtChase(1.2f, 1f).Tick(a));
            CollectionAssert.AreEqual(new[] { "chase" }, a.Log);
        }

        [Test]
        public void Cast_WhileAnotherSkillRuns_Fails()
        {
            var cast = new BtCast("100001", 0);
            var a = new Agent { IsCasting = true };
            Assert.AreEqual(BtStatus.Failure, cast.Tick(a));
            CollectionAssert.IsEmpty(a.Log);
        }

        [Test]
        public void Sequence_RemembersRunningChild()
        {
            BtNode root = Build(@"<behaviour schema_version=""1""><sequence><face/><wait seconds=""0.5""/><cast skill=""100002""/></sequence></behaviour>");
            var a = new Agent();
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "face", "stop" }, a.Log);
            a.Log.Clear();
            a.Now = T(0.25);
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "stop" }, a.Log, "face is not re-run while wait runs");
            a.Log.Clear();
            a.Now = T(0.5);
            Assert.AreEqual(BtStatus.Running, root.Tick(a));
            CollectionAssert.AreEqual(new[] { "stop", "stop", "face", "cast 100002" }, a.Log, "wait done → cast in the same tick");
        }

        [Test]
        public void Selector_HigherBranchTakesOver_ResetsRunningOne()
        {
            BtNode root = Build(@"<behaviour schema_version=""1""><selector>
                <sequence><in_range max=""2""/><face/></sequence>
                <sequence><wait seconds=""1""/><cast skill=""100003""/></sequence>
              </selector></behaviour>");
            var a = new Agent { TargetDistance = 5 };
            root.Tick(a);   // wait starts at 0
            a.Now = T(0.6);
            a.TargetDistance = 1;
            root.Tick(a);   // first branch takes over → wait reset
            a.TargetDistance = 5;
            a.Log.Clear();
            a.Now = T(1.2);
            Assert.AreEqual(BtStatus.Running, root.Tick(a), "wait restarted at 1.2, not finished from its old start");
            CollectionAssert.AreEqual(new[] { "stop" }, a.Log);
        }

        [Test]
        public void Selector_DebugHook_ReportsBranch()
        {
            var errors = new List<SkillError>();
            BehaviourTree.TryParse(Goblin, "goblin", out BehaviourTree bt, errors);
            var root = (BtSelector)bt.Build();
            root.Tick(new Agent { TargetDistance = 5 });
            Assert.AreEqual(1, root.LastIndex);
            Assert.AreEqual(1, root.ActiveIndex);
            Assert.AreEqual("sequence", root.Child(1).Name);
            CollectionAssert.AreEqual(new[] { "100001" }, bt.SkillIds);
        }

        [TestCase("<behaviour schema_version=\"1\"/>", "RootCount")]
        [TestCase("<behaviour schema_version=\"1\"><patrol/><patrol/></behaviour>", "RootCount")]
        [TestCase("<behaviour><patrol/></behaviour>", "SchemaMissing")]
        [TestCase("<behaviour schema_version=\"9\"><patrol/></behaviour>", "SchemaTooNew")]
        [TestCase("<tree schema_version=\"1\"><patrol/></tree>", "UnknownNode")]
        [TestCase("<behaviour schema_version=\"1\"><fly/></behaviour>", "UnknownNode")]
        [TestCase("<behaviour schema_version=\"1\"><selector/></behaviour>", "EmptyComposite")]
        [TestCase("<behaviour schema_version=\"1\"><cast skill=\"12\"/></behaviour>", "IdFormat")]
        [TestCase("<behaviour schema_version=\"1\"><cast skill=\"100001\" cooldown=\"-1\"/></behaviour>", "BadNumber")]
        [TestCase("<behaviour schema_version=\"1\"><in_range min=\"3\" max=\"1\"/></behaviour>", "BadNumber")]
        [TestCase("<behaviour schema_version=\"1\"><chase speed=\"0\"/></behaviour>", "BadNumber")]
        [TestCase("<behaviour schema_version=\"1\"><wait seconds=\"x\"/></behaviour>", "BadNumber")]
        [TestCase("<behaviour schema_version=\"1\"><patrol speed=\"2\"/></behaviour>", "UnknownAttribute")]
        [TestCase("<behaviour schema_version=\"1\"><face><patrol/></face></behaviour>", "LeafHasChildren")]
        [TestCase("not xml", "XmlUnreadable")]
        public void Parse_BadTree_Reported(string xml, string code) => CollectionAssert.Contains(Codes(xml), code);

        [Test]
        public void Build_EachCreatureGetsItsOwnState()
        {
            var errors = new List<SkillError>();
            BehaviourTree.TryParse(Goblin, "goblin", out BehaviourTree bt, errors);
            BtNode a = bt.Build(), b = bt.Build();
            var near = new Agent { TargetDistance = 1 };
            a.Tick(near);   // a casts, cooldown starts
            var other = new Agent { TargetDistance = 1 };
            b.Tick(other);
            CollectionAssert.Contains(other.Log, "cast 100001", "b's cooldown is its own");
        }
    }
}

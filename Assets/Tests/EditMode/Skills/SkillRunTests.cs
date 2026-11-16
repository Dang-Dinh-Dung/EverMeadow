using System.Collections.Generic;
using System.Linq;
using Evermeadow.Core.Skills;
using NUnit.Framework;
using static Evermeadow.Tests.EditMode.Skills.SkillsTestSupport;

namespace Evermeadow.Tests.EditMode.Skills
{
    /// <summary>
    /// Skill runtime (user decision 2026-11-14): the event tree runs in time; End ends the skill — animation back to idle,
    /// nodes not started are dropped, running Hit / Effect finish their own duration; no End = ends when nothing is left.
    /// </summary>
    public class SkillRunTests
    {
        private static long T(double s) => SkillTime.TickOf(s);

        private sealed class Recorder : ISkillPerformer
        {
            public readonly List<string> Log = new List<string>();
            public long Now;
            public double AnimationSeconds = 0.5;
            public bool Found;

            public double Begin(SkillNodeData node, double suggested)
            {
                Log.Add($"{Now}:begin {node.Kind}{node.Id}");
                if (node.Kind == SkillNodeKind.Animation) return node.Get("IsLoop") == "true" ? double.PositiveInfinity : AnimationSeconds;
                return suggested;
            }

            public void Finish(SkillNodeData node, bool interrupted) => Log.Add($"{Now}:{(interrupted ? "stop" : "finish")} {node.Kind}{node.Id}");
            public bool Search(SkillNodeData node) => Found;
            public void ReturnToIdle() => Log.Add($"{Now}:idle");
        }

        private static SkillDef Def(string inner)
        {
            var errors = new List<SkillError>();
            Assert.IsTrue(SkillXmlReader.TryParse(Body(inner), "100000", out SkillDef def, errors), string.Join("\n", errors));
            return def;
        }

        // Advances in 1/60 s steps up to "until", setting the recorder clock so the log carries the tick of each call.
        private static void RunTo(SkillRun run, Recorder r, double until)
        {
            for (int step = 0; SkillTime.TickOfStep(step) <= T(until); step++)
            {
                long t = SkillTime.TickOfStep(step);
                r.Now = t;
                run.Tick(t);
            }
        }

        private static (SkillRun run, Recorder rec) Started(string inner, double animationSeconds = 0.5, bool found = false)
        {
            var rec = new Recorder { AnimationSeconds = animationSeconds, Found = found };
            var run = new SkillRun(Def(inner), rec);
            run.Start(0);
            return (run, rec);
        }

        private const string StartNode = "<node id=\"0\" type=\"Start\"/>";

        [Test]
        public void NoEnd_EndsWhenLastNodeFinishes_ThenIdle()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Animation\"/>" +
                "<node id=\"2\" type=\"Hit\"><f n=\"ActionDelay\" v=\"0.1\"/><f n=\"IsSteady\" v=\"true\"/><f n=\"SteadyDuration\" v=\"0.2\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"2\" when=\"start\"/>");

            RunTo(run, rec, 1);

            CollectionAssert.AreEqual(new[]
            {
                $"0:begin Animation1",
                $"{T(0.1)}:begin Hit2",
                $"{T(0.3)}:finish Hit2",
                $"{T(0.5)}:finish Animation1",
                $"{T(0.5)}:idle"
            }, rec.Log);
            Assert.IsTrue(run.Ended);
            Assert.AreEqual(T(0.5), run.EndTick);
            Assert.IsTrue(run.Finished);
        }

        [Test]
        public void End_StopsAnimationDropsPendingButRunningHitAndEffectFinish()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Animation\"/>" +
                "<node id=\"2\" type=\"Hit\"><f n=\"ActionDelay\" v=\"0.1\"/><f n=\"IsSteady\" v=\"true\"/><f n=\"SteadyDuration\" v=\"0.5\"/></node>" +
                "<node id=\"3\" type=\"End\"><f n=\"ActionDelay\" v=\"0.3\"/></node>" +
                "<node id=\"4\" type=\"Effect\"><f n=\"ActionDelay\" v=\"0.4\"/></node>" +
                "<node id=\"5\" type=\"Effect\"><f n=\"ActionDelay\" v=\"0.2\"/><f n=\"Duration\" v=\"1\"/></node>" +
                "<node id=\"6\" type=\"Effect\"><f n=\"ActionDelay\" v=\"0\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"2\" when=\"start\"/><link from=\"0\" to=\"3\" when=\"start\"/>" +
                "<link from=\"0\" to=\"4\" when=\"start\"/><link from=\"0\" to=\"5\" when=\"start\"/><link from=\"2\" to=\"6\" when=\"end\"/>", animationSeconds: 1);

            RunTo(run, rec, 0.35);
            Assert.IsTrue(run.Ended, "End reached at 0.3");
            Assert.AreEqual(T(0.3), run.EndTick);
            Assert.IsFalse(run.Finished, "Hit and Effect still running");
            RunTo(run, rec, 2);

            CollectionAssert.AreEqual(new[]
            {
                "0:begin Animation1",
                $"{T(0.1)}:begin Hit2",
                $"{T(0.2)}:begin Effect5",
                $"{T(0.3)}:stop Animation1",
                $"{T(0.3)}:idle",
                $"{T(0.6)}:finish Hit2",
                $"{T(1.2)}:finish Effect5"
            }, rec.Log, "Effect4 (pending) and Effect6 (end-child after the skill ended) never start");
            Assert.IsTrue(run.Finished);
        }

        [Test]
        public void LateTick_EndTickIsTheLastEventNotTheFrame()
        {
            var (run, rec) = Started(StartNode + "<node id=\"1\" type=\"Effect\"><f n=\"Duration\" v=\"0.5\"/></node><link from=\"0\" to=\"1\" when=\"start\"/>");
            run.Tick(T(2));   // a long frame (hitch): the effect ended at 0.5
            Assert.IsTrue(run.Ended);
            Assert.AreEqual(T(0.5), run.EndTick, "follow-up link windows must count from the real end");
        }

        [Test]
        public void EndTrigger_ChildStartsAfterParentEndsPlusDelay()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Hit\"><f n=\"IsSteady\" v=\"true\"/><f n=\"SteadyDuration\" v=\"0.2\"/></node>" +
                "<node id=\"2\" type=\"Effect\"><f n=\"ActionDelay\" v=\"0.1\"/><f n=\"Duration\" v=\"0.1\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"2\" when=\"end\"/>");

            RunTo(run, rec, 1);

            CollectionAssert.AreEqual(new[] { "0:begin Hit1", $"{T(0.2)}:finish Hit1", $"{T(0.3)}:begin Effect2", $"{T(0.4)}:finish Effect2", $"{T(0.4)}:idle" }, rec.Log);
        }

        [Test]
        public void InstantHit_FinishesAtOnce_NotSteady()
        {
            var (run, rec) = Started(StartNode + "<node id=\"1\" type=\"Hit\"/><link from=\"0\" to=\"1\" when=\"start\"/>");
            CollectionAssert.AreEqual(new[] { "0:begin Hit1", "0:finish Hit1", "0:idle" }, rec.Log);
            Assert.IsTrue(run.Finished);
        }

        [TestCase(false, "Effect2")]
        [TestCase(true, "Effect1")]
        public void Search_PicksFoundOrNotFoundChildren(bool found, string expected)
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"9\" type=\"Search\"/><node id=\"1\" type=\"Effect\"/><node id=\"2\" type=\"Effect\"/>" +
                "<link from=\"0\" to=\"9\" when=\"start\"/><link from=\"9\" to=\"1\" when=\"found\"/><link from=\"9\" to=\"2\" when=\"not_found\"/>", found: found);
            RunTo(run, rec, 2);
            Assert.IsTrue(rec.Log.Any(l => l.EndsWith("begin " + expected)));
            Assert.AreEqual(1, rec.Log.Count(l => l.Contains("begin Effect")));
        }

        [Test]
        public void HitTriggerChildren_NeverStartWithoutCombat()
        {
            var (run, rec) = Started(StartNode + "<node id=\"1\" type=\"Hit\"/><node id=\"2\" type=\"Knockback\"/>" +
                                     "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"2\" when=\"hit\"/>");
            RunTo(run, rec, 1);
            Assert.IsFalse(rec.Log.Any(l => l.Contains("Knockback")));
        }

        [Test]
        public void LoopingAnimationWithoutEnd_StopsWhenOtherNodesAreDone()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Animation\"><f n=\"IsLoop\" v=\"true\"/></node>" +
                "<node id=\"2\" type=\"Effect\"><f n=\"Duration\" v=\"0.5\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"2\" when=\"start\"/>");
            RunTo(run, rec, 2);
            CollectionAssert.AreEqual(new[] { "0:begin Animation1", "0:begin Effect2", $"{T(0.5)}:finish Effect2", $"{T(0.5)}:stop Animation1", $"{T(0.5)}:idle" }, rec.Log);
        }

        [Test]
        public void SecondAnimation_EndTriggerFromFirst()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Animation\"/><node id=\"2\" type=\"Animation\"/>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"2\" when=\"end\"/>", animationSeconds: 0.25);
            RunTo(run, rec, 2);
            CollectionAssert.AreEqual(new[] { "0:begin Animation1", $"{T(0.25)}:finish Animation1", $"{T(0.25)}:begin Animation2", $"{T(0.5)}:finish Animation2", $"{T(0.5)}:idle" }, rec.Log);
        }

        [Test]
        public void Abort_InterruptsEverythingRunning()
        {
            var (run, rec) = Started(StartNode +
                "<node id=\"1\" type=\"Animation\"/><node id=\"2\" type=\"Effect\"><f n=\"Duration\" v=\"5\"/></node>" +
                "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"0\" to=\"2\" when=\"start\"/>", animationSeconds: 3);
            rec.Now = T(0.1);
            run.Abort(T(0.1));
            Assert.IsTrue(run.Finished);
            CollectionAssert.AreEquivalent(new[] { "0:begin Animation1", "0:begin Effect2", $"{T(0.1)}:stop Animation1", $"{T(0.1)}:idle", $"{T(0.1)}:stop Effect2" }, rec.Log);
            run.Tick(T(10));
            Assert.AreEqual(5, rec.Log.Count, "nothing after an abort");
        }

        [Test]
        public void ActionDelay_RoundsUpToSteps()
        {
            var (run, rec) = Started(StartNode + "<node id=\"1\" type=\"Effect\"><f n=\"ActionDelay\" v=\"0.1\"/><f n=\"Duration\" v=\"0\"/></node><link from=\"0\" to=\"1\" when=\"start\"/>");
            run.Tick(T(0.1) - 1);
            Assert.AreEqual(0, rec.Log.Count);
            rec.Now = T(0.1);
            run.Tick(T(0.1));
            Assert.AreEqual($"{T(0.1)}:begin Effect1", rec.Log[0]);
            Assert.AreEqual(SkillTime.TickOfStep(6), T(0.1), "0.1 s = 6 steps exactly");
        }

        [Test]
        public void Start_Twice_Throws() =>
            Assert.Throws<System.InvalidOperationException>(() => Started(StartNode).run.Start(0));

        [Test]
        public void SuggestedSeconds_FromFields()
        {
            SkillDef d = Def(StartNode +
                "<node id=\"1\" type=\"Effect\"><f n=\"Duration\" v=\"2\"/></node>" +
                "<node id=\"2\" type=\"Hit\"><f n=\"SteadyDuration\" v=\"3\"/></node>" +
                "<node id=\"3\" type=\"Hit\"><f n=\"IsSteady\" v=\"true\"/><f n=\"SteadyDuration\" v=\"3\"/></node>" +
                "<node id=\"4\" type=\"Move\"><f n=\"Value\" v=\"0.4\"/></node>" +
                "<node id=\"5\" type=\"Move\"><f n=\"MoveStandard\" v=\"Speed\"/><f n=\"Value\" v=\"8\"/></node>" +
                "<node id=\"6\" type=\"Animation\"/><node id=\"7\" type=\"End\"/>");
            Assert.AreEqual(2, SkillRun.SuggestedSeconds(d.Node(1)));
            Assert.AreEqual(0, SkillRun.SuggestedSeconds(d.Node(2)), "not steady = instant");
            Assert.AreEqual(3, SkillRun.SuggestedSeconds(d.Node(3)));
            Assert.AreEqual(0.4, SkillRun.SuggestedSeconds(d.Node(4)), 1e-9);
            Assert.AreEqual(0, SkillRun.SuggestedSeconds(d.Node(5)), "speed-based move: performer decides");
            Assert.AreEqual(0, SkillRun.SuggestedSeconds(d.Node(6)));
            Assert.AreEqual(0, SkillRun.SuggestedSeconds(d.Node(7)));
        }

        // ------------------------------------------------------------ End node in data

        [Test]
        public void EndNode_HasNoChildren_Validator()
        {
            var errors = new List<SkillError>();
            SkillXmlReader.TryParse(Body(StartNode + "<node id=\"1\" type=\"End\"/><node id=\"2\" type=\"Effect\"/>" +
                                         "<link from=\"0\" to=\"1\" when=\"start\"/><link from=\"1\" to=\"2\" when=\"start\"/>"), "100000", out _, errors);
            Assert.AreEqual("EndHasChildren", errors.Single().Code);
            CollectionAssert.IsEmpty(SkillSchema.Get(SkillNodeKind.End).Triggers);
            CollectionAssert.Contains(SkillSchema.Kinds, SkillNodeKind.End);
        }

        [Test]
        public void EndNode_RoundTripsThroughWriter()
        {
            SkillDef d = Def(StartNode + "<node id=\"1\" type=\"End\"><f n=\"ActionDelay\" v=\"0.5\"/></node><link from=\"0\" to=\"1\" when=\"start\"/>");
            string xml = SkillXmlWriter.Write(d.Graph);
            StringAssert.Contains("type=\"End\"", xml);
            Assert.IsTrue(SkillXmlReader.TryParse(xml, "100000", out SkillDef back, null));
            Assert.AreEqual("0.5", back.Node(1).Get("ActionDelay"));
        }
    }
}

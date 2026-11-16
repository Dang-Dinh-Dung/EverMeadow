using System.Collections.Generic;
using Evermeadow.Foundation.Input;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Foundation
{
    public class InputModeStackTests
    {
        private RecordingLogSink _log;
        private List<(ModeHandle oldTop, ModeHandle newTop)> _changes;
        private InputModeStack _stack;

        [SetUp]
        public void SetUp()
        {
            _log = new RecordingLogSink();
            _changes = new List<(ModeHandle, ModeHandle)>();
            _stack = new InputModeStack((o, n) => _changes.Add((o, n)), _log);
        }

        [Test]
        public void Push_Menu_BecomesTopAndNotifiesOnce()
        {
            ModeHandle h = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);

            Assert.AreEqual(InputMode.Menu, _stack.TopMode);
            Assert.IsTrue(_stack.IsTop(h));
            Assert.AreEqual(1, _changes.Count);
            Assert.IsFalse(_changes[0].oldTop.IsValid);
            Assert.IsTrue(_changes[0].newTop.SameAs(h));
        }

        [Test, Description("AC-R21")]
        public void Push_Gameplay_RefusedWithWarning()
        {
            ModeHandle h = _stack.Push(InputMode.Gameplay, InputFixture.Owner(), InputFixture.Alive);

            Assert.IsFalse(h.IsValid);
            Assert.AreEqual(0, _stack.Depth);
            Assert.AreEqual(1, _log.Count("Input.PushGameplay"));
        }

        [Test, Description("AC-R22")]
        public void Pop_StaleHandleWithReusedSlot_ReturnsFalseAndWarnsWithoutChange()
        {
            ModeHandle old = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            _stack.Pop(old);
            ModeHandle fresh = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            int changesBefore = _changes.Count;

            bool popped = _stack.Pop(old);

            Assert.IsFalse(popped);
            Assert.IsTrue(_stack.IsTop(fresh));
            Assert.AreEqual(changesBefore, _changes.Count);
            Assert.AreEqual(1, _log.Count("Input.BadModeHandle"));
        }

        [Test, Description("AC-R22")]
        public void Pop_HandleOfOtherInstance_ReturnsFalseAndWarns()
        {
            var other = new InputModeStack(null, null);
            ModeHandle foreign = other.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);

            Assert.IsFalse(_stack.Pop(foreign));
            Assert.IsFalse(_stack.Pop(default));
            Assert.AreEqual(1, _stack.Depth);
            Assert.AreEqual(2, _log.Count("Input.BadModeHandle"));
        }

        [Test, Description("AC-M05")]
        public void Pop_NotTop_RemovesWithoutNotification()
        {
            ModeHandle fishing = _stack.Push(InputMode.Fishing, InputFixture.Owner(), InputFixture.Alive);
            ModeHandle menu = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            int changesBefore = _changes.Count;

            bool popped = _stack.Pop(fishing);

            Assert.IsTrue(popped);
            Assert.IsTrue(_stack.IsTop(menu));
            Assert.AreEqual(1, _stack.Depth);
            Assert.AreEqual(changesBefore, _changes.Count);
        }

        [Test, Description("AC-E03")]
        public void PushThenPop_SameFrame_TwoNotifications()
        {
            ModeHandle h = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);

            _stack.Pop(h);

            Assert.AreEqual(2, _changes.Count);
            Assert.AreEqual(InputMode.Gameplay, _stack.TopMode);
        }

        [Test, Description("AC-E03")]
        public void Pop_TopOfTwoSameModeEntries_NotifiesBecauseHandleChanged()
        {
            ModeHandle lower = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            ModeHandle upper = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            _changes.Clear();

            _stack.Pop(upper);

            Assert.AreEqual(1, _changes.Count);
            Assert.IsTrue(_changes[0].oldTop.SameAs(upper));
            Assert.IsTrue(_changes[0].newTop.SameAs(lower));
        }

        [Test, Description("AC-R23")]
        public void SweepDead_DeadOwnerOnTop_RemovesWithErrorAndNotifies()
        {
            bool alive = true;
            _stack.Push(InputMode.Locked, InputFixture.Owner(), () => alive);
            _changes.Clear();
            alive = false;

            _stack.SweepDead();

            Assert.AreEqual(InputMode.Gameplay, _stack.TopMode);
            Assert.AreEqual(1, _changes.Count);
            Assert.AreEqual(1, _log.Count("Input.DeadModeOwner"));
        }

        [Test, Description("Rule 12: deferred push/pop applied in order")]
        public void EndDefer_QueuedPushAndPop_AppliedInOrderWithTwoNotifications()
        {
            _stack.BeginDefer();
            ModeHandle h = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);
            bool topWhileDeferred = _stack.TopMode == InputMode.Menu;
            _stack.Pop(h);

            _stack.EndDefer();

            Assert.IsFalse(topWhileDeferred);
            Assert.AreEqual(2, _changes.Count);
            Assert.AreEqual(InputMode.Gameplay, _stack.TopMode);
            Assert.IsFalse(_stack.Pop(h), "handle is gone after the queued pop");
        }

        [Test]
        public void Push_Full_RefusedWithError()
        {
            for (int i = 0; i < InputModeStack.Capacity; i++) _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);

            ModeHandle extra = _stack.Push(InputMode.Menu, InputFixture.Owner(), InputFixture.Alive);

            Assert.IsFalse(extra.IsValid);
            Assert.AreEqual(1, _log.Count("Input.StackFull"));
        }
    }
}

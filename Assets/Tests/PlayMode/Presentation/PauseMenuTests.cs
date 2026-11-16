using System.Collections;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using Evermeadow.Presentation.Menus.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Evermeadow.Tests.PlayMode.Presentation
{
    /// <summary>Pause menu on the real engine (UI Toolkit panel built in code), with the core Input service and frame clock.</summary>
    public class PauseMenuTests
    {
        private sealed class NullLog : ILogSink
        {
            public void Warn(string key, string message) { }
            public void Error(string key, string message) { }
        }

        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
        }

        private static void Press(InputService input, InputActionId action)
        {
            input.BeginStep();
            input.OnAction(action, InputPhase.Pressed, DeviceKind.KeyboardMouse, 1, false);
            input.EndStep();
        }

        [UnityTest, Description("I-8: Pause opens the menu, pushes the Menu mode and stops game time; Cancel closes and resumes")]
        public IEnumerator Pause_OpensAndStopsTime_CancelResumes()
        {
            var clock = new FrameClock(new NullLog());
            var input = new InputService(clock, new InputConfig(), new NullLog(), false);
            _go = new GameObject("menu");
            var menu = _go.AddComponent<PauseMenu>();
            menu.Init(input, input, clock, null, null);

            Press(input, InputActionId.Pause);
            yield return null;
            bool open = menu.IsOpen;
            InputMode modeWhileOpen = input.TopMode;
            double scaleWhileOpen = clock.Scale;
            Press(input, InputActionId.Cancel);
            yield return null;

            Assert.IsTrue(open);
            Assert.AreEqual(InputMode.Menu, modeWhileOpen);
            Assert.AreEqual(0.0, scaleWhileOpen);
            Assert.IsFalse(menu.IsOpen);
            Assert.AreEqual(InputMode.Gameplay, input.TopMode);
            Assert.AreEqual(1.0, clock.Scale);
        }

        [UnityTest, Description("Regression: focusing a footer button of the Controls screen (outside its scroll view) threw " +
                                "'Cannot scroll to a VisualElement that's not a child of the ScrollView content-container'")]
        public IEnumerator Controls_HoverFooterButton_NoException()
        {
            var clock = new FrameClock(new NullLog());
            var input = new InputService(clock, new InputConfig(), new NullLog(), false);
            _go = new GameObject("menu");
            var menu = _go.AddComponent<PauseMenu>();
            menu.Init(input, input, clock, null, null);
            menu.Open();
            menu.OpenControls();
            yield return null;
            VisualElement root = _go.GetComponentInChildren<UIDocument>().rootVisualElement;
            Button back = root.Query<Button>().Where(b => b.text == "Quay lại").Last();

            using (PointerEnterEvent e = PointerEnterEvent.GetPooled())
            {
                e.target = back;
                back.SendEvent(e);
            }
            yield return null;

            Assert.IsTrue(menu.IsOpen, "an exception would have failed the test through the log");
        }
    }
}

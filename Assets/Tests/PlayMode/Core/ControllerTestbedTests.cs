#if UNITY_EDITOR
using System.Collections;
using System.IO;
using Evermeadow.Core.CharacterController;
using Evermeadow.Core.CharacterController.Unity;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Evermeadow.Tests.PlayMode.Core
{
    /// <summary>
    /// Runs the real testbed scene end to end (composition root, FrameDriver, Input System, Physics2D, controller) with a simulated
    /// keyboard, and keeps a screenshot as evidence (coding standards: a parse check is not a run).
    /// </summary>
    public class ControllerTestbedTests : InputTestFixture
    {
        private const string ScenePath = "Assets/Scenes/Dev/ControllerTestbed.unity";

        // Waits on the controller's own scaled time: batchmode has long frames that the controller clamps (Rule 30), so real
        // time runs ahead of simulated time.
        private static IEnumerator WaitSim(CharacterBody body, float seconds)
        {
            long end = body.Controller.Time + (long)(seconds * 1_000_000);
            float timeout = Time.unscaledTime + 30f;
            while (body.Controller.Time < end && Time.unscaledTime < timeout) yield return null;
        }

        private static void Capture(string fileName)
        {
            Camera cam = Camera.main;
            var rt = new RenderTexture(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "production", "qa", "evidence"));
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, fileName), tex.EncodeToPNG());
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        [UnityTearDown]
        public IEnumerator UnloadTestbed()
        {
            Scene testbed = SceneManager.GetSceneByPath(ScenePath);
            if (!testbed.isLoaded) yield break;
            SceneManager.SetActiveScene(SceneManager.CreateScene("empty-after-testbed"));
            yield return SceneManager.UnloadSceneAsync(testbed); // InputRoot.OnDestroy disposes the adapter
        }

        [UnityTest, Description("Testbed: stand, run right, jump, land — player-observable run with retained screenshot")]
        public IEnumerator Testbed_RunAndJump_MovesAndLands()
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            Keyboard kb = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            CharacterBody body = Object.FindFirstObjectByType<CharacterBody>();
            Assert.IsNotNull(body?.Controller, "bootstrap built the controller");
            yield return WaitSim(body, 0.5f);
            bool groundedAtStart = body.Controller.IsGrounded;
            float x0 = body.Controller.Position.X;

            Press(kb.dKey);
            yield return WaitSim(body, 0.4f);
            Press(kb.spaceKey);
            yield return WaitSim(body, 0.25f);
            float airborneFeet = body.Controller.Position.Y - body.Controller.Config.ColliderHeight * 0.5f;
            Capture("controller-testbed-jump-2026-10-31.png");
            // Release keys on different frames: two InputTestFixture releases in one frame rebuild the second event from a
            // stale keyboard state and re-press the first key (observed: Jump Released then Pressed at the same timestamp).
            Release(kb.spaceKey);
            yield return WaitSim(body, 0.8f);
            Release(kb.dKey);
            long landBy = body.Controller.Time + 2_000_000;
            while (!body.Controller.IsGrounded && body.Controller.Time < landBy) yield return null;
            yield return WaitSim(body, 0.1f);
            Capture("controller-testbed-landed-2026-10-31.png");

            Assert.IsTrue(groundedAtStart);
            Assert.Greater(airborneFeet, 1.0f, "jumped");
            Assert.Greater(body.Controller.Position.X - x0, 3f, "ran right");
            Assert.IsTrue(body.Controller.IsGrounded, "landed");
            Assert.AreEqual(CharacterState.Grounded, body.Controller.State);
        }
    }
}
#endif

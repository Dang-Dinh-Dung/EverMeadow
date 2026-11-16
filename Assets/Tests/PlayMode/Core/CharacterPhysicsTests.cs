using System.Collections;
using System.Collections.Generic;
using Evermeadow.Core.CharacterController;
using Evermeadow.Core.CharacterController.Unity;
using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Evermeadow.Tests.PlayMode.Core
{
    /// <summary>
    /// Physics2D assumptions of the Character Controller adapter on the real engine (GDD Character Controller OQ-C2):
    /// slope normals, start-inside casts, own-collider filtering, one-way flag, depenetration, standing on a floor.
    /// </summary>
    public class CharacterPhysicsTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly CastHit[] _hits = new CastHit[16];
        private static readonly Vec2 Size = new Vec2(0.9f, 1.3f);

        private sealed class NullLog : ILogSink
        {
            public void Warn(string key, string message) { }
            public void Error(string key, string message) { }
        }

        private sealed class FixedClock : IFrameClock
        {
            public long RealNow => 0;
            public long ScaledNow => 0;
            public long RealDelta => 16_667;
            public long ScaledDelta => 16_667;
            public double Scale => 1.0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private GameObject Box(Vector2 center, Vector2 size, bool oneWay = false)
        {
            var go = new GameObject("box");
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
            if (oneWay) go.AddComponent<OneWayPlatform>();
            _spawned.Add(go);
            return go;
        }

        private Physics2DCollisionWorld PlayerWorld(Vector2 at, out GameObject player)
        {
            player = new GameObject("player");
            player.transform.position = at;
            var rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = player.AddComponent<BoxCollider2D>();
            box.size = new Vector2(Size.X, Size.Y);
            _spawned.Add(player);
            Physics2D.SyncTransforms();
            return new Physics2DCollisionWorld(box, ~0);
        }

        [Test, Description("A 45° PolygonCollider2D reports its face normal to a box cast")]
        public void Cast_OntoSlope_ReportsSlopeNormal()
        {
            var ramp = new GameObject("ramp");
            ramp.AddComponent<PolygonCollider2D>().points = new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10) };
            _spawned.Add(ramp);
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(-20, 0), out _);

            int n = world.Cast(new Vec2(5f, 8f), Size, new Vec2(0f, -1f), 10f, _hits);

            Assert.AreEqual(1, n);
            Assert.AreEqual(-0.7071f, _hits[0].Normal.X, 0.01f);
            Assert.AreEqual(0.7071f, _hits[0].Normal.Y, 0.01f);
        }

        [Test, Description("queriesStartInColliders off: a cast starting inside a collider ignores it")]
        public void Cast_StartingInsideCollider_IgnoresIt()
        {
            Box(new Vector2(0, -0.5f), new Vector2(20, 1));
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(-30, 0), out _);

            int inside = world.Cast(new Vec2(0f, -0.5f), Size, new Vec2(0f, -1f), 5f, _hits);
            int above = world.Cast(new Vec2(0f, 2f), Size, new Vec2(0f, -1f), 5f, _hits);

            Assert.AreEqual(0, inside);
            Assert.AreEqual(1, above);
            Assert.AreEqual(2f - 0.65f, _hits[0].Distance, 0.02f);
        }

        [Test, Description("Regression: resting within Physics2D's contact offset the floor is still seen (was: fell through the testbed floor)")]
        public void Cast_FromWithinContactOffset_StillHitsFloor()
        {
            Box(new Vector2(0, -0.5f), new Vector2(20, 1));
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(-30, 0), out _);

            int n = world.Cast(new Vec2(0f, 0.65f + 0.005f), Size, new Vec2(0f, -1f), 0.1f, _hits);

            Assert.AreEqual(1, n);
            Assert.AreEqual(0f, _hits[0].Distance, 0.02f);
        }

        [Test, Description("The character's own collider is never reported")]
        public void Cast_ThroughOwnCollider_NotReported()
        {
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(0, 0), out _);

            int n = world.Cast(new Vec2(-3f, 0f), Size, new Vec2(1f, 0f), 6f, _hits);

            Assert.AreEqual(0, n);
        }

        [Test, Description("OneWayPlatform is flagged with the collider top")]
        public void Cast_OntoOneWay_FlagAndTop()
        {
            Box(new Vector2(0, 2f), new Vector2(4, 0.2f), oneWay: true);
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(-30, 0), out _);

            int n = world.Cast(new Vec2(0f, 4f), Size, new Vec2(0f, -1f), 5f, _hits);

            Assert.AreEqual(1, n);
            Assert.IsTrue(_hits[0].OneWay);
            Assert.AreEqual(2.1f, _hits[0].Top, 1e-3f);
        }

        [Test, Description("Depenetration pushes a box out of a solid collider along the shortest axis")]
        public void ComputePenetration_OverlappingFloor_PushesUp()
        {
            Box(new Vector2(0, -0.5f), new Vector2(20, 1));
            Physics2DCollisionWorld world = PlayerWorld(new Vector2(-30, 0), out _);

            bool overlapped = world.ComputePenetration(new Vec2(0f, 0.45f), Size, out Vec2 push);

            Assert.IsTrue(overlapped);
            Assert.AreEqual(0f, push.X, 0.01f);
            Assert.AreEqual(0.21f, push.Y, 0.02f, "0.2 overlap + Physics2D contact offsets");
        }

        [UnityTest, Description("CharacterBody falls onto a real floor, stands at skin height, and runs")]
        public IEnumerator CharacterBody_OnFloor_StandsAndRuns()
        {
            Box(new Vector2(0, -0.5f), new Vector2(40, 1));
            var go = new GameObject("player");
            go.transform.position = new Vector3(0, 4, 0);
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            var body = go.AddComponent<CharacterBody>();
            _spawned.Add(go);
            var input = new InputService(new FixedClock(), new InputConfig(), new NullLog(), false);
            body.Init(new FixedClock(), input, new NullLog());

            for (int i = 0; i < 60; i++) yield return null;
            ControllerConfig cfg = body.Controller.Config;
            float feet = body.Controller.Position.Y - cfg.ColliderHeight * 0.5f;
            bool grounded = body.Controller.IsGrounded;
            input.BeginStep();
            input.OnAxisSample(AxisId.Move, new Vec2(1f, 0f), DeviceKind.KeyboardMouse);
            input.EndStep();
            float x0 = body.Controller.Position.X;
            for (int i = 0; i < 30; i++) yield return null;

            Assert.IsTrue(grounded);
            Assert.AreEqual(cfg.Skin, feet, 0.02f);
            Assert.AreEqual(cfg.WalkSpeed * 0.5f, body.Controller.Position.X - x0, 0.2f);
            Assert.AreEqual(body.Controller.Position.X, go.GetComponent<Rigidbody2D>().position.x, 1e-4f, "body follows the core");
        }
    }
}

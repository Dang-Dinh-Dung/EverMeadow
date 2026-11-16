using Evermeadow.Foundation;
using Evermeadow.Foundation.Input;
using Evermeadow.Foundation.Time;
using UnityEngine;

namespace Evermeadow.Core.CharacterController.Unity
{
    /// <summary>
    /// Unity side of the player character: a kinematic Rigidbody2D with a BoxCollider2D (so Combat triggers see the body), stepped
    /// in <c>Update</c> after FrameDriver (order -1000) with the scaled frame delta. The core is the only source of the position;
    /// the body is written once per frame. Physics2D runs in Update mode (project setting), right after every Update.
    /// </summary>
    /// <example>
    /// Composition root: <c>playerBody.Init(clock, inputQueries, log);</c> then other systems read <see cref="Controller"/>.
    /// </example>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class CharacterBody : MonoBehaviour
    {
        [SerializeField] private ControllerConfig config = new ControllerConfig();
        [SerializeField] private LayerMask terrainMask = ~0;
        [SerializeField] private float killY = -50f;

        private Rigidbody2D _body;
        private IFrameClock _clock;

        /// <summary>The controller; null before <see cref="Init"/>.</summary>
        public PlayerController Controller { get; private set; }

        /// <summary>Builds the controller at the current transform position.</summary>
        public void Init(IFrameClock clock, IInputQueries input, ILogSink log)
        {
            _clock = clock;
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.None;
            _body.freezeRotation = true;

            var box = GetComponent<BoxCollider2D>();
            ControllerConfig effective = ControllerConfigValidator.Validate(config, out _);
            box.size = new Vector2(effective.ColliderWidth, effective.ColliderHeight);
            box.offset = Vector2.zero;

            var world = new Physics2DCollisionWorld(box, terrainMask);
            Vector2 p = transform.position;
            Controller = new PlayerController(config, input, world, log, new Vec2(p.x, p.y));
            Controller.SetKillY(killY);
            _body.position = new Vector2(Controller.Position.X, Controller.Position.Y);
        }

        private void Update()
        {
            if (Controller == null) return;
            Controller.Step(_clock.ScaledDelta);
            _body.position = new Vector2(Controller.Position.X, Controller.Position.Y);
        }
    }
}
